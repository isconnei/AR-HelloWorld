import * as THREE from 'three';

/**
 * Convierte los sensores de orientación del teléfono (alpha, beta, gamma) en la rotación de la cámara 3D.
 * Es la misma conversión del antiguo DeviceOrientationControls de three.js, con dos añadidos:
 * tiene en cuenta el giro de la pantalla y suaviza la rotación para quitar el temblor del sensor.
 *
 * Mundo resultante: Y hacia arriba, el usuario mira inicialmente hacia -Z (como en three.js).
 * El "norte" del mundo es arbitrario (alpha en iOS es relativo), lo que da igual para un pájaro que te sigue.
 */
export class DeviceOrientationTracker {
  /** Rapidez del suavizado: más alto = responde antes pero tiembla más. */
  smoothing = 18;

  /** Últimos valores crudos, para el panel de debug. */
  readonly raw = { alpha: 0, beta: 0, gamma: 0 };
  /** True cuando ha llegado al menos una lectura válida. */
  hasData = false;

  private readonly target = new THREE.Quaternion();
  private readonly current = new THREE.Quaternion();
  private initialized = false;
  private eventCount = 0;

  private readonly euler = new THREE.Euler();
  private readonly screenTwist = new THREE.Quaternion();
  private static readonly zee = new THREE.Vector3(0, 0, 1);
  // Gira -90° en X: el teléfono "acostado" pasa a mirar hacia el horizonte.
  private static readonly backToFront = new THREE.Quaternion(-Math.sqrt(0.5), 0, 0, Math.sqrt(0.5));

  private readonly onOrientation = (event: DeviceOrientationEvent) => {
    if (event.alpha === null || event.beta === null || event.gamma === null) return;
    this.raw.alpha = event.alpha;
    this.raw.beta = event.beta;
    this.raw.gamma = event.gamma;
    this.eventCount++;
    this.computeTarget();
    if (!this.initialized) {
      this.current.copy(this.target);
      this.initialized = true;
    }
    this.hasData = true;
  };

  start(): void {
    window.addEventListener('deviceorientation', this.onOrientation);
  }

  stop(): void {
    window.removeEventListener('deviceorientation', this.onOrientation);
  }

  /** Lecturas por segundo recibidas desde la última llamada (para el debug). */
  takeEventCount(): number {
    const count = this.eventCount;
    this.eventCount = 0;
    return count;
  }

  /** Aplica la orientación suavizada a la cámara. Llamar en cada frame. */
  apply(camera: THREE.Camera, dt: number): void {
    if (!this.initialized) return;
    const t = 1 - Math.exp(-this.smoothing * dt);
    this.current.slerp(this.target, t);
    camera.quaternion.copy(this.current);
  }

  private computeTarget(): void {
    const deg = THREE.MathUtils.DEG2RAD;
    const alpha = this.raw.alpha * deg;
    const beta = this.raw.beta * deg;
    const gamma = this.raw.gamma * deg;
    const screen = screenAngle() * deg;

    // Orden 'YXZ' como en DeviceOrientationControls.
    this.euler.set(beta, alpha, -gamma, 'YXZ');
    this.target.setFromEuler(this.euler);
    this.target.multiply(DeviceOrientationTracker.backToFront);
    this.target.multiply(this.screenTwist.setFromAxisAngle(DeviceOrientationTracker.zee, -screen));
  }
}

/** Ángulo de la pantalla (0, 90, 180, 270) para compensar si el teléfono está en horizontal. */
function screenAngle(): number {
  if (screen.orientation && typeof screen.orientation.angle === 'number') return screen.orientation.angle;
  const legacy = (window as unknown as { orientation?: number }).orientation;
  return typeof legacy === 'number' ? legacy : 0;
}
