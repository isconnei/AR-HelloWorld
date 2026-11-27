import * as THREE from 'three';

/**
 * Objetos de prueba del paso 2: un cubo fijo en el mundo, delante de donde mirabas al empezar,
 * y una cuadrícula en el suelo. Si la orientación funciona, al girar el teléfono se quedan "en su sitio".
 * Además hay tres esferas a 90°, 180° y 270° para comprobar que el giro completo cuadra.
 */
export class OrientationTestObjects {
  readonly group = new THREE.Group();
  private placed = false;
  private readonly cube: THREE.Mesh;

  /** Altura aproximada del teléfono sobre el suelo (m). */
  static readonly eyeHeight = 1.4;

  constructor(scene: THREE.Scene) {
    this.cube = new THREE.Mesh(new THREE.BoxGeometry(0.2, 0.2, 0.2), new THREE.MeshNormalMaterial());
    this.group.add(this.cube);

    const markerGeometry = new THREE.SphereGeometry(0.06, 16, 12);
    const markers: Array<[number, number]> = [
      [90, 0xff5a3c],
      [180, 0xffd23c],
      [270, 0x3cff8a],
    ];
    for (const [degrees, color] of markers) {
      const marker = new THREE.Mesh(markerGeometry, new THREE.MeshStandardMaterial({ color, roughness: 0.5 }));
      marker.position.set(0, 0, -1).applyAxisAngle(new THREE.Vector3(0, 1, 0), THREE.MathUtils.degToRad(degrees));
      this.group.add(marker);
    }

    const grid = new THREE.GridHelper(6, 12, 0xffffff, 0xffffff);
    const gridMaterial = grid.material as THREE.Material;
    gridMaterial.transparent = true;
    gridMaterial.opacity = 0.35;
    grid.position.y = -OrientationTestObjects.eyeHeight;
    this.group.add(grid);

    this.group.visible = false;
    scene.add(this.group);
  }

  /** Coloca el grupo según hacia dónde mira la cámara en la primera lectura válida del giroscopio. */
  placeOnce(camera: THREE.Camera): void {
    if (this.placed) return;
    const forward = new THREE.Vector3(0, 0, -1).applyQuaternion(camera.quaternion);
    forward.y = 0;
    if (forward.lengthSq() < 1e-4) return; // mirando justo arriba o abajo: esperar
    const yaw = Math.atan2(-forward.x, -forward.z);
    this.group.rotation.y = yaw;
    this.cube.position.set(0, 0, -1);
    this.group.visible = true;
    this.placed = true;
  }

  update(dt: number): void {
    // Giro lento del cubo para distinguir su propio movimiento del de la cámara.
    this.cube.rotation.y += dt * 0.6;
  }
}
