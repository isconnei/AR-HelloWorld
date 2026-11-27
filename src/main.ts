import './style.css';
import { config } from './config';
import { ArScene } from './scene';
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
  arScene = new ArScene(canvas);
  arScene.start();
  if (config.debug) {
    debugPanel.classList.remove('hidden');
    arScene.onFrame(updateDebug);
  }
}

let debugTimer = 0;
function updateDebug(dt: number): void {
  debugTimer += dt;
  if (debugTimer < 0.25 || !arScene) return;
  debugTimer = 0;
  const track = (video.srcObject as MediaStream | null)?.getVideoTracks()[0];
  const settings = track?.getSettings();
  debugPanel.textContent = [
    `FPS ${arScene.fps}`,
    `Movimiento: ${status.motion} · Cámara: ${status.camera} · Ubicación: ${status.location}`,
    `GPS: ${status.gps}`,
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
