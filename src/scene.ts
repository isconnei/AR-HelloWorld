import * as THREE from 'three';
import { config } from './config';

/**
 * Escena three.js transparente sobre el vídeo de la cámara.
 * Paso 1: solo prepara el renderer, la cámara virtual y la luz; el contenido llega en los pasos 2 y 3.
 */
export class ArScene {
  readonly renderer: THREE.WebGLRenderer;
  readonly scene = new THREE.Scene();
  readonly camera: THREE.PerspectiveCamera;

  private readonly onFrameCallbacks: Array<(dt: number, time: number) => void> = [];
  private readonly clock = new THREE.Clock();
  private frames = 0;
  private fpsTime = 0;
  fps = 0;

  constructor(canvas: HTMLCanvasElement) {
    this.renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true, powerPreference: 'high-performance' });
    this.renderer.setClearColor(0x000000, 0);
    // Limitar la densidad de píxeles cuida el rendimiento en el iPhone SE.
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.outputColorSpace = THREE.SRGBColorSpace;

    this.camera = new THREE.PerspectiveCamera(config.fov, 1, 0.01, 100);

    // Iluminación simple, sin sombras.
    this.scene.add(new THREE.HemisphereLight(0xdfefff, 0x5a4a3a, 1.6));
    const sun = new THREE.DirectionalLight(0xffffff, 1.4);
    sun.position.set(1, 2, 1);
    this.scene.add(sun);

    this.resize();
    window.addEventListener('resize', () => this.resize());
    window.addEventListener('orientationchange', () => setTimeout(() => this.resize(), 200));
  }

  onFrame(callback: (dt: number, time: number) => void): void {
    this.onFrameCallbacks.push(callback);
  }

  start(): void {
    this.clock.start();
    this.renderer.setAnimationLoop(() => this.frame());
  }

  private frame(): void {
    const dt = Math.min(this.clock.getDelta(), 0.1);
    const time = this.clock.elapsedTime;
    for (const callback of this.onFrameCallbacks) callback(dt, time);
    this.renderer.render(this.scene, this.camera);

    this.frames++;
    this.fpsTime += dt;
    if (this.fpsTime >= 0.5) {
      this.fps = Math.round(this.frames / this.fpsTime);
      this.frames = 0;
      this.fpsTime = 0;
    }
  }

  private resize(): void {
    const width = window.innerWidth;
    const height = window.innerHeight;
    this.renderer.setSize(width, height, false);
    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
  }
}
