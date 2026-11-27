import * as THREE from 'three';
import type { AvatarBody } from './avatar/AvatarBody';

export interface FollowOptions {
  /** Distancia al usuario (m). */
  distance: number;
  /** Retraso del seguimiento (s): tiempo aproximado en alcanzar el objetivo. */
  smoothTime: number;
  /** Altura respecto a la línea de los ojos (m); negativo = un poco más abajo. */
  heightOffset: number;
}

const DEG = THREE.MathUtils.DEG2RAD;
const MAX_PITCH = 35 * DEG;

/**
 * Hace que el avatar vuele delante del usuario y lo siga cuando gira el teléfono.
 *
 * El objetivo es un punto fijo delante de la cámara. En lugar de mover el pájaro en línea recta hacia él
 * (atravesaría al usuario en un giro de 180°), se suavizan por separado el ángulo alrededor del usuario,
 * la altura y la distancia con un resorte críticamente amortiguado (como SmoothDamp de Unity).
 * Así vuela en arco a tu alrededor, sube un poco mientras te alcanza y se inclina hacia la curva.
 * Siempre mira hacia el usuario.
 */
export class FlightFollower {
  private yaw = 0;
  private pitch = 0;
  private distance: number;
  private readonly yawVelocity = { v: 0 };
  private readonly pitchVelocity = { v: 0 };
  private readonly distanceVelocity = { v: 0 };
  private roll = 0;
  private effort = 0;
  private initialized = false;
  /** Si no es null, se está despidiendo: vuela hacia este ángulo, lejos y hacia arriba. */
  private leaving: { yaw: number; startTime: number } | null = null;
  private time = 0;

  private readonly forward = new THREE.Vector3();
  private readonly direction = new THREE.Vector3();

  constructor(
    private readonly avatar: AvatarBody,
    private readonly camera: THREE.Camera,
    private readonly options: FollowOptions,
  ) {
    this.distance = options.distance;
  }

  /** Esfuerzo actual (0–1) para que el avatar aletee más rápido cuando persigue. */
  get currentEffort(): number {
    return this.effort;
  }

  /** Empieza lejos, a un lado y un poco arriba, para que llegue volando. */
  arrive(fromSide = 1): void {
    const { yaw, pitch } = this.cameraAngles();
    this.yaw = yaw + fromSide * 70 * DEG;
    this.pitch = Math.min(MAX_PITCH, pitch + 15 * DEG);
    this.distance = 2.6;
    this.yawVelocity.v = this.pitchVelocity.v = this.distanceVelocity.v = 0;
    this.leaving = null;
    this.initialized = true;
  }

  /** Despedida: se aleja volando hacia un lado y hacia arriba. Tras ~2,5 s ya está lejos. */
  leave(toSide = 1): void {
    this.leaving = { yaw: this.yaw + toSide * 75 * DEG, startTime: this.time };
  }

  /** Segundos desde que empezó a despedirse (null si no se está despidiendo). */
  get leavingFor(): number | null {
    return this.leaving ? this.time - this.leaving.startTime : null;
  }

  update(dt: number, time: number): void {
    if (dt <= 0) return;
    this.time = time;
    const target = this.cameraAngles();
    if (!this.initialized) {
      this.yaw = target.yaw;
      this.pitch = target.pitch;
      this.initialized = true;
    }

    // Al despedirse: rumbo fijo hacia un lado, más lejos y más alto.
    const leaving = this.leaving;
    const targetYaw = leaving ? leaving.yaw : target.yaw;
    const targetPitch = leaving ? MAX_PITCH : target.pitch;
    const targetDistance = leaving ? 4 : this.options.distance;

    const { smoothTime } = this.options;
    this.yaw = smoothDampAngle(this.yaw, targetYaw, this.yawVelocity, smoothTime * (leaving ? 2 : 1), dt);
    this.pitch = smoothDamp(this.pitch, targetPitch, this.pitchVelocity, smoothTime * (leaving ? 2 : 1), dt);
    this.distance = smoothDamp(this.distance, targetDistance, this.distanceVelocity, smoothTime * (leaving ? 1.2 : 1.6), dt);

    // Trayectoria curva: cuanto más rápido rodea al usuario, más sube.
    const angularSpeed = Math.abs(this.yawVelocity.v) + Math.abs(this.pitchVelocity.v);
    const arc = Math.min(0.18, angularSpeed * 0.05);
    this.effort = THREE.MathUtils.clamp(angularSpeed / 2.5 + Math.abs(this.distanceVelocity.v) / 3, 0, 1);

    const cosPitch = Math.cos(this.pitch);
    this.direction.set(-Math.sin(this.yaw) * cosPitch, Math.sin(this.pitch), -Math.cos(this.yaw) * cosPitch);
    const root = this.avatar.root;
    root.position
      .copy(this.camera.position)
      .addScaledVector(this.direction, this.distance)
      .add(new THREE.Vector3(0, this.options.heightOffset + arc + 0.012 * Math.sin(time * 0.9), 0));

    // Siempre mira al usuario (+Z del avatar hacia la cámara)…
    root.lookAt(this.camera.position);
    // …y se inclina hacia el lado al que vuela (ladeo de alas en la curva).
    const targetRoll = THREE.MathUtils.clamp(this.yawVelocity.v * 0.35, -0.6, 0.6);
    this.roll += (targetRoll - this.roll) * (1 - Math.exp(-8 * dt));
    root.rotateZ(this.roll);
  }

  /** Hacia dónde mira la cámara, como ángulo horizontal (0 = −Z) y vertical limitado. */
  private cameraAngles(): { yaw: number; pitch: number } {
    this.forward.set(0, 0, -1).applyQuaternion(this.camera.quaternion);
    const yaw = Math.atan2(-this.forward.x, -this.forward.z);
    const pitch = THREE.MathUtils.clamp(Math.asin(THREE.MathUtils.clamp(this.forward.y, -1, 1)), -MAX_PITCH, MAX_PITCH);
    return { yaw, pitch };
  }
}

/** Resorte críticamente amortiguado (port de Mathf.SmoothDamp de Unity). */
export function smoothDamp(current: number, target: number, velocity: { v: number }, smoothTime: number, dt: number): number {
  const time = Math.max(0.0001, smoothTime);
  const omega = 2 / time;
  const x = omega * dt;
  const exp = 1 / (1 + x + 0.48 * x * x + 0.235 * x * x * x);
  const change = current - target;
  const temp = (velocity.v + omega * change) * dt;
  velocity.v = (velocity.v - omega * temp) * exp;
  let output = target + (change + temp) * exp;
  // Evitar pasarse del objetivo.
  if (target - current > 0 === output > target) {
    output = target;
    velocity.v = 0;
  }
  return output;
}

/** Como smoothDamp, pero por el camino angular más corto. */
export function smoothDampAngle(current: number, target: number, velocity: { v: number }, smoothTime: number, dt: number): number {
  const delta = Math.atan2(Math.sin(target - current), Math.cos(target - current));
  return smoothDamp(current, current + delta, velocity, smoothTime, dt);
}
