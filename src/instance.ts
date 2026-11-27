// Configuración de una instancia (un lugar con su avatar), cargada desde public/instances/<id>.json.

export interface GeoPoint {
  id: string;
  name: string;
  lat: number;
  lon: number;
  /** Radio de la geocerca en metros (por defecto 30). */
  radius: number;
  /** Si existe, las coordenadas son provisionales y esto explica por qué. */
  pending?: string;
}

export interface FaqEntry {
  keywords: string[];
  answer: string;
}

export interface InstanceConfig {
  id: string;
  placeName: string;
  city?: string;
  points: GeoPoint[];
  avatar: {
    type: 'procedural-kingfisher';
    name: string;
    species?: string;
    size?: number;
    distance?: number;
  };
  /** System prompt de la IA (paso 6). */
  personality: string;
  /** Guion sin conexión (paso 5). */
  script: {
    greeting: string[];
    farewell: string[];
    faq: FaqEntry[];
    fallback: string[];
  };
}

const DEFAULT_RADIUS = 30;

export async function loadInstance(id: string): Promise<InstanceConfig> {
  if (!/^[a-z0-9-]+$/i.test(id)) throw new Error(`Id de instancia no válido: ${id}`);
  const response = await fetch(`${import.meta.env.BASE_URL}instances/${id}.json`, { cache: 'no-cache' });
  if (!response.ok) throw new Error(`No se encontró la instancia "${id}" (${response.status}).`);
  const data = (await response.json()) as InstanceConfig;
  if (!Array.isArray(data.points) || data.points.length === 0) throw new Error(`La instancia "${id}" no tiene puntos.`);
  for (const point of data.points) {
    if (!Number.isFinite(point.lat) || !Number.isFinite(point.lon)) throw new Error(`Punto sin coordenadas en "${id}".`);
    point.radius = Number.isFinite(point.radius) && point.radius > 0 ? point.radius : DEFAULT_RADIUS;
  }
  return data;
}

// --- Punto de prueba local ---------------------------------------------------------------------
// Para probar en casa sin escribir coordenadas en el repo: el botón "Fijar punto aquí" (modo debug)
// guarda un punto solo en este teléfono (localStorage). También se puede pasar por URL: ?lat=..&lon=..&r=..

const overrideKey = (instanceId: string) => `geofence-override:${instanceId}`;

export function applyPointOverride(instance: InstanceConfig, params: URLSearchParams): GeoPoint | null {
  const lat = Number(params.get('lat'));
  const lon = Number(params.get('lon'));
  if (params.has('lat') && params.has('lon') && Number.isFinite(lat) && Number.isFinite(lon)) {
    const radius = Number(params.get('r'));
    return { id: 'url', name: 'Punto de la URL', lat, lon, radius: radius > 0 ? radius : DEFAULT_RADIUS };
  }
  try {
    const saved = localStorage.getItem(overrideKey(instance.id));
    if (saved) return JSON.parse(saved) as GeoPoint;
  } catch {
    // Sin almacenamiento (modo privado): se usan los puntos de la instancia.
  }
  return null;
}

export function savePointOverride(instanceId: string, point: GeoPoint): void {
  try {
    localStorage.setItem(overrideKey(instanceId), JSON.stringify(point));
  } catch {
    // Ignorar: sin almacenamiento solo dura esta visita.
  }
}

export function clearPointOverride(instanceId: string): void {
  try {
    localStorage.removeItem(overrideKey(instanceId));
  } catch {
    // Ignorar.
  }
}
