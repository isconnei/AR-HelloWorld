import type { GeoPoint } from './instance';

export type FenceState = 'searching' | 'outside' | 'inside';

export interface FenceReading {
  point: GeoPoint;
  /** Distancia al centro del punto (m). */
  distance: number;
  /** Precisión declarada del GPS (m, radio del 68 %). */
  accuracy: number;
  /** Coordenadas actuales: solo se usan en el teléfono (nunca se envían). */
  lat: number;
  lon: number;
}

export interface GeofenceOptions {
  /** Al entrar se tolera hasta la mitad de la precisión del GPS, con este máximo (m). */
  enterTolerance: number;
  /** Para salir hay que alejarse este margen más allá del radio (m). */
  exitMargin: number;
  /** Y seguir fuera durante este tiempo antes de despedirse (ms). */
  exitDelayMs: number;
  /** Con una precisión peor que esta (m) no se decide una salida. */
  maxAccuracyForExit: number;
}

const DEFAULTS: GeofenceOptions = {
  enterTolerance: 15,
  exitMargin: 15,
  exitDelayMs: 8000,
  maxAccuracyForExit: 60,
};

/**
 * Geocerca con el GPS del navegador. La ubicación se procesa solo en el teléfono.
 *
 * El GPS salta varios metros (en interiores, ±50 m o más), así que hay histéresis:
 *  - Entrar: distancia ≤ radio + min(precisión/2, enterTolerance).
 *  - Salir: distancia > radio + exitMargin, con precisión aceptable, durante exitDelayMs seguidos.
 * Así el pájaro no aparece y desaparece en el borde.
 */
export class Geofence {
  onChange?: (state: FenceState, previous: FenceState) => void;
  onReading?: (reading: FenceReading) => void;
  onError?: (error: GeolocationPositionError) => void;

  last: FenceReading | null = null;
  lastError: GeolocationPositionError | null = null;

  private state: FenceState = 'searching';
  private simulated = false;
  private outsideSince = 0;
  private watchId: number | null = null;
  private readonly options: GeofenceOptions;

  constructor(
    private points: GeoPoint[],
    options: Partial<GeofenceOptions> = {},
  ) {
    this.options = { ...DEFAULTS, ...options };
  }

  /** Estado efectivo (incluye la simulación del modo debug). */
  get current(): FenceState {
    return this.simulated ? 'inside' : this.state;
  }

  get isSimulated(): boolean {
    return this.simulated;
  }

  start(): void {
    if (this.watchId !== null || !('geolocation' in navigator)) return;
    this.watchId = navigator.geolocation.watchPosition(
      (position) => this.handlePosition(position),
      (error) => {
        this.lastError = error;
        this.onError?.(error);
      },
      { enableHighAccuracy: true, maximumAge: 3000, timeout: 20000 },
    );
  }

  stop(): void {
    if (this.watchId !== null) navigator.geolocation.clearWatch(this.watchId);
    this.watchId = null;
  }

  setPoints(points: GeoPoint[]): void {
    this.points = points;
    if (this.last) this.evaluate(this.last.lat, this.last.lon, this.last.accuracy);
  }

  /** Modo debug: forzar "dentro" sin moverse. */
  setSimulated(simulated: boolean): void {
    const before = this.current;
    this.simulated = simulated;
    this.emitIfChanged(before);
  }

  private handlePosition(position: GeolocationPosition): void {
    this.lastError = null;
    this.evaluate(position.coords.latitude, position.coords.longitude, position.coords.accuracy);
  }

  private evaluate(lat: number, lon: number, accuracy: number): void {
    // El punto más cercano (medido desde su borde).
    let best: { point: GeoPoint; distance: number } | null = null;
    for (const point of this.points) {
      const distance = haversine(lat, lon, point.lat, point.lon);
      if (!best || distance - point.radius < best.distance - best.point.radius) best = { point, distance };
    }
    if (!best) return;

    const reading: FenceReading = { point: best.point, distance: best.distance, accuracy, lat, lon };
    this.last = reading;
    this.onReading?.(reading);

    const before = this.current;
    const { enterTolerance, exitMargin, exitDelayMs, maxAccuracyForExit } = this.options;
    const radius = best.point.radius;
    const insideNow = best.distance <= radius + Math.min(accuracy / 2, enterTolerance);
    const clearlyOutside = best.distance > radius + exitMargin && accuracy <= maxAccuracyForExit;

    if (insideNow) {
      this.state = 'inside';
      this.outsideSince = 0;
    } else if (this.state === 'inside') {
      if (clearlyOutside) {
        this.outsideSince ||= Date.now();
        if (Date.now() - this.outsideSince >= exitDelayMs) this.state = 'outside';
      } else {
        this.outsideSince = 0;
      }
    } else {
      this.state = 'outside';
    }
    this.emitIfChanged(before);
  }

  private emitIfChanged(before: FenceState): void {
    const after = this.current;
    if (after !== before) this.onChange?.(after, before);
  }
}

/** Distancia en metros entre dos coordenadas (fórmula de haversine). */
export function haversine(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const R = 6371000;
  const toRad = Math.PI / 180;
  const dLat = (lat2 - lat1) * toRad;
  const dLon = (lon2 - lon1) * toRad;
  const a = Math.sin(dLat / 2) ** 2 + Math.cos(lat1 * toRad) * Math.cos(lat2 * toRad) * Math.sin(dLon / 2) ** 2;
  return 2 * R * Math.asin(Math.min(1, Math.sqrt(a)));
}

/** "230 m" o "1,4 km". */
export function formatDistance(meters: number): string {
  if (meters < 1000) return `${Math.round(meters)} m`;
  return `${(meters / 1000).toLocaleString('es-CL', { maximumFractionDigits: 1 })} km`;
}
