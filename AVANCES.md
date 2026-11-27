# Avances — Prototipo WebAR "pájaro en vuelo"

> Documento compartido entre Claude Chat y Claude Code. El brief completo está en `CLAUDE.md`.
> Al terminar cada sesión: actualizar el estado de los pasos, los problemas abiertos y añadir una línea al **Historial**.

## Estado de los pasos

| Paso | Qué | Estado |
|---|---|---|
| 1 | Esqueleto: Vite + TS + three.js, HTTPS local, pantalla de inicio con permisos, cámara de fondo | **Hecho y probado** en iPhone SE (Safari): cámara a pantalla completa, 3 permisos ok, 60 FPS |
| 2 | Orientación: cubo de prueba fijo en el mundo con el giroscopio | Hecho, **pendiente de probar en el iPhone** |
| 3 | Pájaro procedural + seguimiento en vuelo | Pendiente |
| 4 | Geocerca con JSON de instancia y modo debug | Pendiente |
| 5 | Chat con guion (sin IA) | Pendiente |
| 6 | Proxy en Azure Functions + Claude, con guion de respaldo | Pendiente |
| 7 | PWA, despliegue en Azure Static Web Apps y QR | Pendiente |

## Paso 1 — qué hay
- `npm run dev` levanta Vite con HTTPS (certificado autofirmado de `@vitejs/plugin-basic-ssl`) y `--host`, para abrirlo desde el iPhone en la misma red.
- `index.html`: vídeo de cámara (`playsinline muted autoplay`), canvas de three.js transparente encima, pantalla de inicio, pantalla de error y panel de debug.
- `src/permissions.ts`: pide **movimiento → cámara → ubicación**. El permiso de movimiento de iOS se pide como primera instrucción del clic en "Comenzar" (antes de cualquier `await`), que es lo que exige Safari. Si se deniega algo, se muestran instrucciones concretas para iOS o Android en lugar de una pantalla en blanco.
- `src/scene.ts`: renderer three.js transparente, cámara virtual con FOV configurable, luz hemisférica + direccional, sin sombras, densidad de píxeles limitada a 2× y bucle de animación con contador de FPS.
- `src/config.ts`: parámetros de URL: `?debug=1` (panel de estado), `?fov=63` (FOV vertical de la cámara virtual), `?i=<instancia>` (para el paso 4).
- Línea de privacidad en la pantalla de inicio. La ubicación solo se lee en el teléfono.

## Paso 2 — qué hay
- `src/orientation.ts` (`DeviceOrientationTracker`): convierte `alpha`, `beta` y `gamma` en un cuaternión con la misma fórmula del antiguo `DeviceOrientationControls` de three.js (Euler `YXZ` + giro de −90° en X + compensación del ángulo de pantalla) y lo suaviza con `slerp` amortiguado (`smoothing = 18`) para quitar el temblor sin añadir retraso visible.
- `src/testObjects.ts`: cubo de colores a 1 m delante de donde mirabas al empezar, tres esferas a 90° (roja), 180° (amarilla) y 270° (verde), y una cuadrícula en el suelo 1,4 m por debajo del teléfono. Se colocan con la primera lectura válida del giroscopio.
- El panel `?debug=1` muestra además las lecturas por segundo del giroscopio y los ángulos α, β, γ.

## Cómo probar (paso 2)
1. Con `npm run dev` corriendo, abre en Safari del iPhone `https://<IP-del-Mac>:5173/?debug=1` (o recarga la página si ya estaba abierta) y toca "Comenzar".
2. Debe aparecer un cubo de colores girando lentamente a 1 m delante y una cuadrícula blanca en el suelo.
3. Gira el teléfono a izquierda y derecha, arriba y abajo: el cubo y la cuadrícula deben quedarse en su sitio respecto a la habitación, sin temblar.
4. Date una vuelta completa: verás la esfera roja a tu derecha (90°), la amarilla detrás (180°), la verde a tu izquierda (270°) y vuelves al cubo.
5. Es normal que, en varios minutos, el cubo se desplace unos grados: el giroscopio acumula deriva (no hay brújula ni anclaje). Para el pájaro no importa, porque te sigue.

## Cómo probar (paso 1)
1. En el Mac: `npm run dev` dentro de esta carpeta. Muestra una dirección `https://<IP-del-Mac>:5173/`.
2. El iPhone y el Mac deben estar en la misma red (o el Mac conectado al hotspot del iPhone).
3. En Safari del iPhone, abre `https://<IP-del-Mac>:5173/?debug=1`. Safari avisa de que el certificado no es de confianza: "Mostrar detalles" > "visitar este sitio web".
4. Toca "Comenzar" y acepta movimiento, cámara y ubicación.
5. Debe verse la cámara trasera a pantalla completa y, arriba a la izquierda, el panel de debug con FPS, permisos y precisión del GPS.

## Problemas abiertos
- Repo de GitHub pendiente: Isaías crea `isconnei/avatares-webar` (privado, vacío) y se conecta como `origin`.
- Observación de la prueba: el GPS dio ±54 m en interiores. La geocerca por defecto es de 30 m, así que en el paso 4 habrá que tener en cuenta la precisión (por ejemplo, no despedir al pájaro mientras la precisión sea peor que el radio).

## Historial
- 2026-11-27 · Claude Code: repo creado; paso 1 (esqueleto, HTTPS local, permisos, cámara de fondo).
- 2026-11-27 · Isaías: paso 1 probado en iPhone SE — cámara a pantalla completa, permisos ok, 60 FPS, vídeo 720×1280, GPS ±54 m en interiores.
- 2026-11-27 · Claude Code: paso 2 (orientación con el giroscopio, cubo y cuadrícula de prueba).
