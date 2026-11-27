import './style.css';
import { ProceduralKingfisher } from './avatar/ProceduralKingfisher';
import { config, params } from './config';
import { FlightFollower } from './flight';
import { Geofence, formatDistance, type FenceState } from './geofence';
import { applyPointOverride, clearPointOverride, loadInstance, savePointOverride, type InstanceConfig } from './instance';
import { DeviceOrientationTracker } from './orientation';
import {
  PermissionError,
  helpFor,
  requestCamera,
  requestLocation,
  requestMotionPermission,
  type ErrorHelp,
  type PermissionKind,
} from './permissions';
import { ArScene } from './scene';
import { OrientationTestObjects } from './testObjects';

const video = document.getElementById('camera') as HTMLVideoElement;
const canvas = document.getElementById('scene') as HTMLCanvasElement;
const startScreen = document.getElementById('start-screen')!;
const startButton = document.getElementById('start-button') as HTMLButtonElement;
const errorScreen = document.getElementById('error-screen')!;
const retryButton = document.getElementById('retry-button') as HTMLButtonElement;
const debugPanel = document.getElementById('debug')!;
const notice = document.getElementById('notice')!;
const noticeTitle = document.getElementById('notice-title')!;
const noticeDetail = document.getElementById('notice-detail')!;

const status = { motion: 'pendiente', camera: 'pendiente', location: 'pendiente' };

// La instancia se descarga mientras la persona lee la pantalla de inicio.
const instancePromise = loadInstance(config.instance);
instancePromise.catch(() => undefined); // el error se muestra al tocar "Comenzar"

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

    await requestLocation();
    status.location = 'ok';
  } catch (error) {
    const kind: PermissionKind = error instanceof PermissionError ? error.kind : 'unsupported';
    if (kind === 'motion' || kind === 'camera' || kind === 'location') status[kind] = 'denegado';
    console.warn('[permisos]', error);
    showError(helpFor(kind));
    return;
  }

  let instance: InstanceConfig;
  try {
    instance = await instancePromise;
  } catch (error) {
    console.warn('[instancia]', error);
    showError({
      title: 'No encontramos este lugar',
      message: 'El enlace o el código QR apunta a un lugar que no existe o no se pudo cargar.',
      steps: ['Revisa tu conexión e inténtalo de nuevo.', 'Si escaneaste un QR, vuelve a escanearlo.'],
    });
    return;
  }

  startScreen.classList.add('hidden');
  startAr(instance);
}

async function attachCamera(stream: MediaStream): Promise<void> {
  video.srcObject = stream;
  video.setAttribute('playsinline', '');
  video.muted = true;
  await video.play();
  video.classList.add('visible');
}

function startAr(instance: InstanceConfig): void {
  const ar = new ArScene(canvas);
  arScene = ar;
  const birdName = instance.avatar.name;

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

  // El pájaro: oculto hasta que entras en la geocerca.
  const bird = new ProceduralKingfisher(config.size ?? instance.avatar.size ?? 1.5);
  bird.root.visible = false;
  ar.scene.add(bird.root);
  const follower = new FlightFollower(bird, ar.camera, {
    distance: config.distance ?? instance.avatar.distance ?? 0.85,
    smoothTime: config.lag,
    heightOffset: -0.06,
  });

  // Geocerca: un punto de prueba local (URL o botón del modo debug) reemplaza a los de la instancia.
  const override = applyPointOverride(instance, params);
  const geofence = new Geofence(override ? [override] : instance.points);

  /** Lo que la geocerca pide: que el pájaro esté presente o no. */
  let wantPresent = false;
  /** Lo que se ve: 'away' oculto, 'here' volando contigo, 'leaving' despidiéndose. */
  let presence: 'away' | 'here' | 'leaving' = 'away';

  geofence.onChange = (state: FenceState) => {
    wantPresent = state === 'inside';
    updateNotice();
  };
  geofence.onReading = () => updateNotice();
  geofence.onError = (error) => {
    if (error.code === error.PERMISSION_DENIED) {
      status.location = 'denegado';
      showError(helpFor('location'));
    } else {
      updateNotice();
    }
  };
  geofence.start();

  ar.onFrame((dt, time) => {
    // Llegada: hace falta una lectura del giroscopio para saber hacia dónde miras.
    if (wantPresent && presence !== 'here' && orientation.hasData) {
      follower.arrive(Math.random() < 0.5 ? -1 : 1);
      bird.greet();
      bird.root.visible = true;
      presence = 'here';
    } else if (!wantPresent && presence === 'here') {
      bird.greet(); // aletazo de despedida
      follower.leave(Math.random() < 0.5 ? -1 : 1);
      presence = 'leaving';
    }
    if (presence === 'leaving' && (follower.leavingFor ?? 0) > 2.6) {
      bird.root.visible = false;
      presence = 'away';
    }
    if (presence === 'away') return;
    follower.update(dt, time);
    bird.update(dt, time, { effort: follower.currentEffort });
  });

  function updateNotice(): void {
    if (geofence.current === 'inside') {
      notice.classList.add('hidden');
      return;
    }
    const reading = geofence.last;
    const error = geofence.lastError;
    if (!reading) {
      noticeTitle.textContent = 'Buscando tu ubicación…';
      noticeDetail.textContent = error
        ? 'No hay señal de GPS. Prueba al aire libre, lejos de edificios altos.'
        : `En cuanto la tengamos, sabremos si ${birdName} anda cerca.`;
    } else {
      const toEdge = Math.max(0, reading.distance - reading.point.radius);
      noticeTitle.textContent = `${birdName} está en ${override ? reading.point.name : instance.placeName}`;
      noticeDetail.textContent = `Te faltan unos ${formatDistance(toEdge)} para llegar (${reading.point.name}).`;
    }
    notice.classList.remove('hidden');
  }
  updateNotice();

  ar.start();
  if (config.debug) {
    document.body.classList.add('debug');
    debugPanel.classList.remove('hidden');
    ar.onFrame((dt) => updateDebug(dt, geofence, override !== null, () => presence));
    setupDebugActions(bird, geofence, instance.id);
  }
}

/** Botones para probar sin moverse ni chatear (solo con ?debug=1). */
function setupDebugActions(bird: ProceduralKingfisher, geofence: Geofence, instanceId: string): void {
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
      case 'simulate':
        geofence.setSimulated(!geofence.isSimulated);
        button.classList.toggle('active', geofence.isSimulated);
        break;
      case 'pin': {
        const last = geofence.last;
        if (!last) {
          alert('Todavía no hay lectura de GPS. Espera unos segundos.');
          return;
        }
        // Se guarda solo en este teléfono.
        savePointOverride(instanceId, { id: 'local', name: 'Punto de prueba', lat: last.lat, lon: last.lon, radius: 30 });
        window.location.reload();
        break;
      }
      case 'unpin':
        clearPointOverride(instanceId);
        window.location.reload();
        break;
    }
  });
}

let debugTimer = 0;
let orientationRate = 0;
function updateDebug(dt: number, geofence: Geofence, localPoint: boolean, presence: () => string): void {
  debugTimer += dt;
  if (debugTimer < 0.5 || !arScene) return;
  orientationRate = Math.round(orientation.takeEventCount() / debugTimer);
  debugTimer = 0;
  const { alpha, beta, gamma } = orientation.raw;
  const reading = geofence.last;
  const fenceState = { searching: 'buscando GPS', outside: 'fuera', inside: 'dentro' }[geofence.current];
  debugPanel.textContent = [
    `FPS ${arScene.fps} · pájaro: ${presence()}`,
    `Movimiento: ${status.motion} · Cámara: ${status.camera} · Ubicación: ${status.location}`,
    orientation.hasData
      ? `Giroscopio ${orientationRate}/s · α ${alpha.toFixed(0)}° β ${beta.toFixed(0)}° γ ${gamma.toFixed(0)}°`
      : 'Giroscopio: sin datos todavía',
    reading
      ? `Punto: ${reading.point.name}${localPoint ? ' (local)' : ''}${reading.point.pending ? ' (provisional)' : ''} · r ${reading.point.radius} m`
      : 'Punto: esperando GPS',
    reading
      ? `Distancia ${formatDistance(reading.distance)} · precisión ±${Math.round(reading.accuracy)} m`
      : geofence.lastError
        ? `GPS: error ${geofence.lastError.code} (${geofence.lastError.message})`
        : 'GPS: buscando…',
    `Geocerca: ${fenceState}${geofence.isSimulated ? ' (simulado)' : ''}`,
  ].join('\n');
}

function showError(help: ErrorHelp): void {
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
if (!window.isSecureContext) showError(helpFor('insecure'));
