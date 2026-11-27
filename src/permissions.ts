// Permisos de movimiento, cámara y ubicación.
//
// En iOS, DeviceOrientationEvent.requestPermission() solo funciona si se llama directamente dentro del
// handler de un toque, antes de cualquier `await`. Por eso requestMotionPermission() devuelve la promesa
// tal cual y main.ts la llama como primera instrucción del clic en "Comenzar".

export type PermissionKind = 'motion' | 'camera' | 'location' | 'insecure' | 'unsupported';

export class PermissionError extends Error {
  constructor(
    public readonly kind: PermissionKind,
    message: string,
  ) {
    super(message);
  }
}

type IOSOrientationEvent = typeof DeviceOrientationEvent & {
  requestPermission?: () => Promise<'granted' | 'denied'>;
};

/** Llamar como PRIMERA línea del handler del clic (sin await antes). */
export function requestMotionPermission(): Promise<void> {
  const Orientation = (window as unknown as { DeviceOrientationEvent?: IOSOrientationEvent }).DeviceOrientationEvent;
  if (!Orientation) {
    return Promise.reject(new PermissionError('unsupported', 'Este navegador no entrega los sensores de movimiento.'));
  }
  if (typeof Orientation.requestPermission !== 'function') {
    // Android y navegadores de escritorio: no piden permiso.
    return Promise.resolve();
  }
  return Orientation.requestPermission().then(
    (state) => {
      if (state !== 'granted') throw new PermissionError('motion', 'Permiso de movimiento denegado.');
    },
    () => {
      throw new PermissionError('motion', 'No se pudo pedir el permiso de movimiento.');
    },
  );
}

/** Cámara trasera. Devuelve el stream para conectarlo al <video>. */
export async function requestCamera(): Promise<MediaStream> {
  if (!navigator.mediaDevices?.getUserMedia) {
    throw new PermissionError(window.isSecureContext ? 'unsupported' : 'insecure', 'Cámara no disponible.');
  }
  try {
    return await navigator.mediaDevices.getUserMedia({
      audio: false,
      video: {
        facingMode: { ideal: 'environment' },
        width: { ideal: 1280 },
        height: { ideal: 720 },
      },
    });
  } catch (error) {
    const name = (error as DOMException)?.name;
    if (name === 'NotAllowedError' || name === 'SecurityError') {
      throw new PermissionError('camera', 'Permiso de cámara denegado.');
    }
    throw new PermissionError('unsupported', `No se pudo abrir la cámara (${name ?? 'error'}).`);
  }
}

/**
 * Pide el permiso de ubicación con una primera lectura. La posición no sale del teléfono.
 * Si el GPS tarda en responder no es un error: se seguirá intentando con watchPosition más adelante.
 */
export function requestLocation(): Promise<GeolocationPosition | null> {
  if (!('geolocation' in navigator)) {
    return Promise.reject(new PermissionError('unsupported', 'Ubicación no disponible.'));
  }
  return new Promise((resolve, reject) => {
    navigator.geolocation.getCurrentPosition(
      (position) => resolve(position),
      (error) => {
        if (error.code === error.PERMISSION_DENIED) {
          reject(new PermissionError('location', 'Permiso de ubicación denegado.'));
        } else {
          resolve(null); // sin señal o tiempo agotado: seguimos igual
        }
      },
      { enableHighAccuracy: true, timeout: 15000, maximumAge: 60000 },
    );
  });
}

export interface ErrorHelp {
  title: string;
  message: string;
  steps: string[];
}

const isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);

/** Textos amables con instrucciones para volver a activar cada permiso. */
export function helpFor(kind: PermissionKind): ErrorHelp {
  switch (kind) {
    case 'motion':
      return {
        title: 'Falta el permiso de movimiento',
        message: 'Sin él, el pájaro no puede seguirte cuando giras el teléfono.',
        steps: isIOS
          ? [
              'Cierra esta pestaña y vuelve a abrir el enlace.',
              'Toca "Comenzar" y elige "Permitir" cuando pregunte por "Movimiento y orientación".',
              'Si no vuelve a preguntar: Ajustes > Apps > Safari > Avanzado > Datos del sitio web, borra este sitio y vuelve a intentarlo.',
            ]
          : ['Toca el candado junto a la dirección > Permisos > activa "Sensores de movimiento".', 'Vuelve a cargar la página.'],
      };
    case 'camera':
      return {
        title: 'Falta el permiso de cámara',
        message: 'La cámara se usa solo para mostrar tu entorno detrás del pájaro. No se graba ni se envía nada.',
        steps: isIOS
          ? [
              'En Safari, toca "aA" en la barra de dirección > Ajustes del sitio web > Cámara > Permitir.',
              'O en Ajustes > Apps > Safari > Cámara > Permitir.',
              'Vuelve aquí y toca "Intentar de nuevo".',
            ]
          : ['Toca el candado junto a la dirección > Permisos > Cámara > Permitir.', 'Vuelve a cargar la página.'],
      };
    case 'location':
      return {
        title: 'Falta el permiso de ubicación',
        message: 'La ubicación se usa solo en tu teléfono para saber si llegaste al lugar del pájaro. No se envía a ningún servidor.',
        steps: isIOS
          ? [
              'Revisa que la ubicación esté activa: Ajustes > Privacidad y seguridad > Localización.',
              'En esa misma pantalla, entra en "Sitios web de Safari" y elige "Al usar la app".',
              'En Safari, toca "aA" > Ajustes del sitio web > Ubicación > Permitir, y toca "Intentar de nuevo".',
            ]
          : ['Toca el candado junto a la dirección > Permisos > Ubicación > Permitir.', 'Revisa que la ubicación del teléfono esté activada y vuelve a cargar la página.'],
      };
    case 'insecure':
      return {
        title: 'La página no es segura',
        message: 'La cámara y los sensores solo funcionan en páginas https://.',
        steps: ['Abre la dirección que empieza con https://.'],
      };
    default:
      return {
        title: 'Este navegador no es compatible',
        message: 'No pudimos acceder a la cámara o a los sensores.',
        steps: isIOS
          ? ['Abre el enlace en Safari (no dentro de otra app como Instagram o WhatsApp).']
          : ['Abre el enlace en Chrome actualizado.'],
      };
  }
}
