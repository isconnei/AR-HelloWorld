import './style.css';
import { ProceduralKingfisher } from './avatar/ProceduralKingfisher';
import { config } from './config';
import { FlightFollower } from './flight';
import { DeviceOrientationTracker } from './orientation';
import { ArScene } from './scene';
import { OrientationTestObjects } from './testObjects';
import {
  PermissionError,
  helpFor,
  requestCamera,
  requestLocation,
  requestMotionPermission,
  type PermissionKind,
} from './permissions';

const video = document.getElementById('camera') as HTMLVideoElement;
const canvas = document.getElementById('scene') as HTMLCanvasElement;
const startScreen = document.getElementById('start-screen')!;
const startButton = document.getElementById('start-button') as HTMLButtonElement;
const errorScreen = document.getElementById('error-screen')!;
const retryButton = document.getElementById('retry-button') as HTMLButtonElement;
const debugPanel = document.getElementById('debug')!;

const status = {
  motion: 'pendiente',
  camera: 'pendiente',
  location: 'pendiente',
  gps: '—',
};

let arScene: ArScene | null = null;
const orientation = new DeviceOrientationTracker();

startButton.addEventListener('click', onStart);
retryButton.addEventListener('click', () => window.location.reload());

function onStart(): void {
  // iOS: el permiso de movimiento debe pedirse aquí mismo, antes de cualquier await.
  const motion = requestMotionPermission();
  startButton.disabled = true;
  startButton.textContent = 'Preparando…';
  void runPermissionFlow(motion);
}

async function runPermissionFlow(motion: Promise<void>): Promise<void> {
  try {
    // Orden pedido: movimiento → cámara → ubicación.
    await motion;
    status.motion = 'ok';

    const stream = await requestCamera();
    await attachCamera(stream);
    status.camera = 'ok';

    const position = await requestLocation();
    status.location = 'ok';
    status.gps = position ? `±${Math.round(position.coords.accuracy)} m` : 'sin señal aún';

    startScreen.classList.add('hidden');
    startAr();
  } catch (error) {
    const kind: PermissionKind = error instanceof PermissionError ? error.kind : 'unsupported';
    if (kind === 'motion' || kind === 'camera' || kind === 'location') status[kind] = 'denegado';
    console.warn('[permisos]', error);
    showError(kind);
  }
}

async function attachCamera(stream: MediaStream): Promise<void> {
  video.srcObject = stream;
  video.setAttribute('playsinline', '');
  video.muted = true;
  await video.play();
  video.classList.add('visible');
}

function startAr(): void {
  const ar = new ArScene(canvas);
  arScene = ar;

  // La cámara virtual gira con el teléfono.
  orientation.start();
  ar.onFrame((dt) => orientation.apply(ar.camera, dt));

  // Objetos de prueba del paso 2 (solo con ?test=1).
  if (config.test) {
    const testObjects = new OrientationTestObjects(ar.scene);
    ar.onFrame((dt) => {
      if (orientation.hasData) testObjects.placeOnce(ar.camera);
      testObjects.update(dt);
    });
  }

  // Paso 3: el martín pescador llega volando y te sigue.
  const bird = new ProceduralKingfisher(config.size);
  bird.root.visible = false;
  ar.scene.add(bird.root);
  const follower = new FlightFollower(bird, ar.camera, {
    distance: config.distance,
    smoothTime: config.lag,
    heightOffset: -0.06,
  });
  let arrived = false;
  const arrive = () => {
    follower.arrive(Math.random() < 0.5 ? -1 : 1);
    bird.greet();
    bird.root.visible = true;
    arrived = true;
  };
  ar.onFrame((dt, time) => {
    // Esperar a la primera lectura del giroscopio para saber hacia dónde miras.
    if (!arrived && orientation.hasData) arrive();
    if (!arrived) return;
    follower.update(dt, time);
    bird.update(dt, time, { effort: follower.currentEffort });
  });

  ar.start();
  if (config.debug) {
    debugPanel.classList.remove('hidden');
    ar.onFrame(updateDebug);
    setupDebugActions(bird, arrive);
  }
}

/** Botones para probar las animaciones sin chat (solo con ?debug=1). */
function setupDebugActions(bird: ProceduralKingfisher, arrive: () => void): void {
  const panel = document.getElementById('debug-actions')!;
  panel.classList.remove('hidden');
  panel.addEventListener('click', (event) => {
    const button = (event.target as HTMLElement).closest('button');
    if (!button) return;
    switch (button.dataset.action) {
      case 'greet':
        bird.greet();
        break;
      case 'talk':
        bird.talking = !bird.talking;
        button.classList.toggle('active', bird.talking);
        break;
      case 'think':
        bird.thinking = !bird.thinking;
        button.classList.toggle('active', bird.thinking);
        break;
      case 'arrive':
        arrive();
        break;
    }
  });
}

let debugTimer = 0;
let orientationRate = 0;
function updateDebug(dt: number): void {
  debugTimer += dt;
  if (debugTimer < 0.5 || !arScene) return;
  orientationRate = Math.round(orientation.takeEventCount() / debugTimer);
  debugTimer = 0;
  const track = (video.srcObject as MediaStream | null)?.getVideoTracks()[0];
  const settings = track?.getSettings();
  const { alpha, beta, gamma } = orientation.raw;
  debugPanel.textContent = [
    `FPS ${arScene.fps}`,
    `Movimiento: ${status.motion} · Cámara: ${status.camera} · Ubicación: ${status.location}`,
    `GPS: ${status.gps}`,
    orientation.hasData
      ? `Giroscopio ${orientationRate}/s · α ${alpha.toFixed(0)}° β ${beta.toFixed(0)}° γ ${gamma.toFixed(0)}°`
      : 'Giroscopio: sin datos todavía',
    `Vídeo ${settings?.width ?? '?'}×${settings?.height ?? '?'} · FOV ${config.fov}°`,
    `Pantalla ${window.innerWidth}×${window.innerHeight} @${window.devicePixelRatio}x`,
  ].join('\n');
}

function showError(kind: PermissionKind): void {
  const help = helpFor(kind);
  document.getElementById('error-title')!.textContent = help.title;
  document.getElementById('error-message')!.textContent = help.message;
  const steps = document.getElementById('error-steps')!;
  steps.replaceChildren(
    ...help.steps.map((text) => {
      const li = document.createElement('li');
      li.textContent = text;
      return li;
    }),
  );
  startScreen.classList.add('hidden');
  errorScreen.classList.remove('hidden');
}

// Aviso temprano si la página no es https (los permisos fallarían igual).
if (!window.isSecureContext) showError('insecure');
