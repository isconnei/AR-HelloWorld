import * as THREE from 'three';

/** Lo que el seguimiento le cuenta al avatar en cada frame. */
export interface FlightMotion {
  /** 0 = flotando quieto, 1 = volando rápido para alcanzarte. */
  effort: number;
}

/**
 * Base común de los avatares (igual que AvatarBody en la app Unity).
 * El seguimiento mueve y orienta `root` (con +Z mirando al usuario); el avatar anima su cuerpo dentro.
 * Implementaciones: ProceduralKingfisher (ahora) y GltfAvatar (más adelante, con un .glb).
 */
export abstract class AvatarBody {
  readonly root = new THREE.Group();

  /** Mientras es true, abre y cierra el pico (o la boca). */
  talking = false;
  /** Mientras es true, ladea la cabeza como pensando (esperando la respuesta). */
  thinking = false;

  /** Gesto de saludo (aleteo más fuerte). */
  abstract greet(): void;

  /** Animación del cuerpo. `time` en segundos. */
  abstract update(dt: number, time: number, motion: FlightMotion): void;

  /** Libera geometrías, materiales y texturas. */
  abstract dispose(): void;
}
