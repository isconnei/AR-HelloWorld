import * as THREE from 'three';
import { AvatarBody, type FlightMotion } from './AvatarBody';
import { barred, darken, lighten, shimmer, streaked } from './featherTextures';

type RGB = [number, number, number];

/** Colores del martín pescador común (Alcedo atthis), en sRGB. Los mismos que en la app Unity. */
const COLORS = {
  back: [0.1, 0.78, 0.95] as RGB, // franja del lomo, azul eléctrico
  wing: [0.06, 0.45, 0.55] as RGB, // alas verdeazuladas
  crown: [0.07, 0.42, 0.62] as RGB, // píleo y bigotera
  tail: [0.05, 0.3, 0.55] as RGB,
  spot: [0.45, 0.9, 1.0] as RGB, // motas claras de las alas
  belly: [0.93, 0.48, 0.16] as RGB, // pecho y mejillas naranjas
  throat: [0.97, 0.96, 0.92] as RGB, // garganta y mancha del cuello
  beak: [0.08, 0.07, 0.07] as RGB,
  feet: [0.9, 0.28, 0.2] as RGB,
};

const DEG = THREE.MathUtils.DEG2RAD;

/** Inclinación del cuerpo en vuelo cernido: cabeza arriba-adelante, cola abajo-atrás. */
const BODY_LEAN = 25 * DEG;
/** Te mira de lado, con un ojo, como los pájaros reales (y así se ve el perfil del pico). */
const SIDE_EYE = 32 * DEG;
const SIDE_EYE_TALKING = 20 * DEG;

/**
 * Martín pescador procedural portado de KingfisherAvatar.cs (Unity), en pose de vuelo cernido.
 * A tamaño real mide unos 17 cm de pico a cola; `size` lo multiplica. Mira hacia +Z.
 * Animaciones: aleteo continuo (más rápido al perseguirte), vaivén del cuerpo con cada aletazo,
 * parpadeo, vistazos y mirada de lado, pico al hablar, aleteo fuerte al saludar y cabeza ladeada al pensar.
 */
export class ProceduralKingfisher extends AvatarBody {
  private readonly geometries = {
    sphere: new THREE.SphereGeometry(0.5, 20, 14),
    capsule: new THREE.CapsuleGeometry(0.5, 1, 6, 12),
  };
  private readonly materials: THREE.MeshStandardMaterial[] = [];

  private readonly model = new THREE.Group();
  private readonly body = new THREE.Group();
  private readonly head = new THREE.Group();
  private readonly tail = new THREE.Group();
  private readonly leftWing = new THREE.Group();
  private readonly rightWing = new THREE.Group();
  private readonly upperBeak = new THREE.Group();
  private readonly lowerBeak = new THREE.Group();
  private readonly eyes: THREE.Group[] = [];

  private flapPhase = 0;
  private greetTime = -1;
  private beakOpen = 0;
  private talkBlend = 0;
  private thinkBlend = 0;
  private headRotation = new THREE.Quaternion();
  private glance = new THREE.Vector2();
  private nextGlance = 1.5;
  private eyeSide = 1;
  private nextBlink = 2;
  private blinkTime = -1;

  constructor(size = 1.5) {
    super();
    this.model.scale.setScalar(size);
    this.root.add(this.model);
    this.build();
  }

  greet(): void {
    this.greetTime = 0;
  }

  dispose(): void {
    Object.values(this.geometries).forEach((g) => g.dispose());
    for (const m of this.materials) {
      m.map?.dispose();
      m.dispose();
    }
  }

  private build(): void {
    const back = this.material([1, 1, 1], 0.3, shimmer(darken(COLORS.back, 0.75), lighten(COLORS.back, 0.25), 1));
    const wing = this.material([1, 1, 1], 0.45, streaked(COLORS.wing, lighten(COLORS.wing, 0.35), 9, 0.8, 2));
    const wingTip = this.material([1, 1, 1], 0.45, streaked(darken(COLORS.wing, 0.7), COLORS.wing, 9, 0.8, 9));
    const crown = this.material([1, 1, 1], 0.45, barred(COLORS.crown, lighten(COLORS.back, 0.15), 14, 0.32, 3));
    const tail = this.material([1, 1, 1], 0.4, streaked(COLORS.tail, COLORS.back, 10, 0.6, 4));
    const spots = this.material(COLORS.spot, 0.5);
    const belly = this.material([1, 1, 1], 0.75, streaked(COLORS.belly, darken(COLORS.belly, 0.7), 12, 0.7, 5));
    const throat = this.material([1, 1, 1], 0.75, streaked(COLORS.throat, darken(COLORS.throat, 0.88), 16, 0.6, 6));
    const beak = this.material(COLORS.beak, 0.4);
    const eye = this.material([0.02, 0.02, 0.02], 0.05);
    const catchlight = this.material([1, 1, 1], 0.1);
    const feet = this.material(COLORS.feet, 0.7);

    // Cuerpo, centrado en el origen e inclinado como en vuelo cernido.
    this.body.position.set(0, -0.045, 0);
    this.body.rotation.x = BODY_LEAN;
    this.model.add(this.body);
    this.part('sphere', this.body, [0, 0.034, 0.006], [0.046, 0.068, 0.046], belly);
    this.part('sphere', this.body, [0, 0.038, -0.006], [0.05, 0.072, 0.044], wing);
    this.part('sphere', this.body, [0, 0.04, -0.024], [0.016, 0.06, 0.01], back);

    // Patas recogidas bajo el vientre.
    for (const s of [-1, 1]) this.part('sphere', this.body, [0.01 * s, 0.0, 0.012], [0.01, 0.007, 0.016], feet);

    // Cola abierta hacia abajo y atrás.
    this.tail.position.set(0, 0.006, -0.016);
    this.body.add(this.tail);
    this.part('sphere', this.tail, [0, -0.022, -0.008], [0.02, 0.045, 0.008], tail, [30, 0, 0]);

    // Alas extendidas (en Unity estaban plegadas; aquí van abiertas para volar).
    this.buildWing(this.leftWing, -1, wing, wingTip, spots);
    this.buildWing(this.rightWing, 1, wing, wingTip, spots);

    // Cabeza grande con el pico en daga.
    this.head.position.set(0, 0.074, 0.008);
    this.body.add(this.head);
    this.part('sphere', this.head, [0, 0.012, 0.004], [0.042, 0.04, 0.044], crown);
    this.part('sphere', this.head, [0, -0.004, 0.015], [0.026, 0.02, 0.022], throat);
    for (const s of [-1, 1]) {
      this.part('sphere', this.head, [0.017 * s, 0.008, -0.002], [0.012, 0.016, 0.022], belly); // mejilla
      this.part('sphere', this.head, [0.008 * s, 0.011, 0.021], [0.005, 0.005, 0.007], belly); // loreal
      this.part('sphere', this.head, [0.016 * s, 0.001, 0.009], [0.009, 0.007, 0.016], crown); // bigotera
      this.part('sphere', this.head, [0.016 * s, -0.004, -0.012], [0.01, 0.014, 0.014], throat); // cuello

      const eyePivot = new THREE.Group();
      eyePivot.position.set(0.0175 * s, 0.015, 0.009);
      this.head.add(eyePivot);
      this.part('sphere', eyePivot, [0, 0, 0], [0.01, 0.01, 0.01], eye);
      this.part('sphere', eyePivot, [0.004 * s, 0.0018, 0.0025], [0.0022, 0.0022, 0.0022], catchlight);
      this.eyes.push(eyePivot);
    }

    this.upperBeak.position.set(0, 0.009, 0.02);
    this.head.add(this.upperBeak);
    this.part('capsule', this.upperBeak, [0, 0.0015, 0.022], [0.009, 0.022, 0.0055], beak, [90, 0, 0]);
    this.lowerBeak.position.set(0, 0.009, 0.02);
    this.head.add(this.lowerBeak);
    this.part('capsule', this.lowerBeak, [0, -0.002, 0.02], [0.008, 0.02, 0.0045], beak, [90, 0, 0]);

    this.headRotation.setFromEuler(new THREE.Euler(-BODY_LEAN, 0, 0, 'YXZ'));
    this.head.quaternion.copy(this.headRotation);
  }

  private buildWing(pivot: THREE.Group, side: number, feathers: THREE.Material, tip: THREE.Material, spots: THREE.Material): void {
    pivot.position.set(0.022 * side, 0.058, -0.006);
    this.body.add(pivot);
    this.part('sphere', pivot, [0, -0.05, -0.004], [0.008, 0.1, 0.045], feathers, [10, 0, 0]);
    this.part('sphere', pivot, [0, -0.092, -0.012], [0.007, 0.04, 0.032], tip, [14, 0, 0]);
    for (let i = 0; i < 4; i++) {
      this.part('sphere', pivot, [0.004 * side, -0.02 - 0.016 * i, 0.004 - 0.004 * i], [0.005, 0.005, 0.005], spots);
    }
  }

  update(dt: number, time: number, motion: FlightMotion): void {
    this.talkBlend = moveTowards(this.talkBlend, this.talking ? 1 : 0, dt * 4);
    this.thinkBlend = moveTowards(this.thinkBlend, this.thinking ? 1 : 0, dt * 3);

    // Saludo: aleteo más amplio y rápido durante 1,6 s.
    let greet = 0;
    if (this.greetTime >= 0) {
      this.greetTime += dt;
      const p = this.greetTime / 1.6;
      greet = smoothstep(Math.min(1, p * 6)) * smoothstep(Math.min(1, (1 - p) * 4));
      if (p >= 1) this.greetTime = -1;
    }

    // Aleteo continuo: más rápido cuando se esfuerza por alcanzarte o saluda.
    const effort = Math.max(motion.effort, greet);
    const frequency = 7 + 4 * effort; // aletazos por segundo
    this.flapPhase += dt * frequency * Math.PI * 2;
    const beat = Math.sin(this.flapPhase);
    const amplitude = (38 + 22 * effort) * DEG;
    const wingAngle = 78 * DEG + amplitude * beat;
    this.leftWing.rotation.set(0, 0, -wingAngle);
    this.rightWing.rotation.set(0, 0, wingAngle);

    // El cuerpo sube un poco con cada aletazo hacia abajo, más un vaivén lento.
    this.model.position.y = 0.004 * Math.cos(this.flapPhase) + 0.01 * Math.sin(time * 1.3);
    this.tail.rotation.x = (6 * Math.sin(this.flapPhase + 1) - 8 * this.thinkBlend) * DEG;

    this.updateHead(dt, time);
    this.updateBeak(dt, time);
    this.updateBlink(dt);
  }

  private updateHead(dt: number, time: number): void {
    this.nextGlance -= dt;
    if (this.nextGlance <= 0) {
      const idle = !this.talking && Math.random() < 0.5;
      this.glance.set(idle ? THREE.MathUtils.randFloat(-25, 25) * DEG : 0, idle ? THREE.MathUtils.randFloat(-10, 6) * DEG : 0);
      if (Math.random() < 0.35) this.eyeSide = -this.eyeSide;
      this.nextGlance = THREE.MathUtils.randFloat(1.2, 3.5);
    }

    const sideEye = this.eyeSide * THREE.MathUtils.lerp(SIDE_EYE, SIDE_EYE_TALKING, this.talkBlend);
    const nod = 4 * DEG * this.talkBlend * Math.sin(time * 7);
    const look = new THREE.Euler(-BODY_LEAN + this.glance.y + nod, sideEye + this.glance.x, 0, 'YXZ');
    const think = new THREE.Euler(-BODY_LEAN - 15 * DEG, 30 * DEG, 28 * DEG, 'YXZ');
    const target = new THREE.Quaternion()
      .setFromEuler(look)
      .slerp(new THREE.Quaternion().setFromEuler(think), this.thinkBlend);
    this.headRotation.slerp(target, 1 - Math.exp(-14 * dt));
    this.head.quaternion.copy(this.headRotation);
  }

  private updateBeak(dt: number, time: number): void {
    const target = this.talking ? 0.5 + 0.5 * Math.sin(time * 17) * Math.sin(time * 5.3) : 0;
    this.beakOpen += (Math.max(0, target) - this.beakOpen) * (1 - Math.exp(-20 * dt));
    this.lowerBeak.rotation.x = 18 * DEG * this.beakOpen;
    this.upperBeak.rotation.x = -4 * DEG * this.beakOpen;
  }

  private updateBlink(dt: number): void {
    this.nextBlink -= dt;
    if (this.nextBlink <= 0) {
      this.blinkTime = 0;
      this.nextBlink = THREE.MathUtils.randFloat(2, 6);
    }
    let open = 1;
    if (this.blinkTime >= 0) {
      this.blinkTime += dt;
      open = 1 - 0.9 * Math.sin(Math.PI * Math.min(1, this.blinkTime / 0.12));
      if (this.blinkTime >= 0.12) this.blinkTime = -1;
    }
    for (const eye of this.eyes) eye.scale.y = open;
  }

  private material(color: RGB, roughness: number, map?: THREE.Texture): THREE.MeshStandardMaterial {
    const material = new THREE.MeshStandardMaterial({
      color: new THREE.Color().setRGB(color[0], color[1], color[2], THREE.SRGBColorSpace),
      roughness,
      metalness: 0,
      map: map ?? null,
    });
    this.materials.push(material);
    return material;
  }

  private part(
    kind: 'sphere' | 'capsule',
    parent: THREE.Object3D,
    position: [number, number, number],
    scale: [number, number, number],
    material: THREE.Material,
    eulerDegrees: [number, number, number] = [0, 0, 0],
  ): THREE.Mesh {
    const mesh = new THREE.Mesh(this.geometries[kind], material);
    mesh.position.set(...position);
    mesh.scale.set(...scale);
    mesh.rotation.set(eulerDegrees[0] * DEG, eulerDegrees[1] * DEG, eulerDegrees[2] * DEG, 'YXZ');
    parent.add(mesh);
    return mesh;
  }
}

function moveTowards(current: number, target: number, maxDelta: number): number {
  return Math.abs(target - current) <= maxDelta ? target : current + Math.sign(target - current) * maxDelta;
}

function smoothstep(x: number): number {
  const t = Math.min(1, Math.max(0, x));
  return t * t * (3 - 2 * t);
}
