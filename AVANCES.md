# Avances — Prototipo WebAR "pájaro en vuelo"

> Documento compartido entre Claude Chat y Claude Code. El brief completo está en `CLAUDE.md`.
> Al terminar cada sesión: actualizar el estado de los pasos, los problemas abiertos y añadir una línea al **Historial**.
>
> Código: rama `feature/webar` de github.com/isconnei/AR-HelloWorld (historial propio, independiente de la app Unity de `main` y `feature/talking-avatar`). Carpeta local en el Mac: `~/avatares-webar`.

## Estado de los pasos

| Paso | Qué | Estado |
|---|---|---|
| 1 | Esqueleto: Vite + TS + three.js, HTTPS local, pantalla de inicio con permisos, cámara de fondo | **Hecho y probado** en iPhone SE (Safari): cámara a pantalla completa, 3 permisos ok, 60 FPS |
| 2 | Orientación: cubo de prueba fijo en el mundo con el giroscopio | **Hecho y probado** en iPhone SE: el cubo se queda en su sitio |
| 3 | Pájaro procedural + seguimiento en vuelo | **Hecho y probado** en iPhone SE |
| 4 | Geocerca con JSON de instancia y modo debug | Hecho, **pendiente de probar en el iPhone** (lógica probada con lecturas simuladas) |
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

## Publicación de prueba en GitHub Pages
- Decisión (Isaías, 2026-11-27): publicar la app web en **GitHub Pages** para probar con un link HTTPS real, sin el Mac ni el aviso de certificado. Azure Static Web Apps (brief) queda pendiente para el paso 6/7: Pages solo sirve archivos estáticos, así que el proxy `/api/chat` tendrá que vivir en otro servicio.
- `.github/workflows/pages.yml`: con cada push a `feature/webar`, GitHub Actions compila (`npm run build -- --base /<repo>/`) y publica `dist/` en `https://isconnei.github.io/AR-HelloWorld/`. También se puede lanzar a mano desde la pestaña Actions ("Run workflow").
- Requisitos en GitHub (los hace Isaías):
  1. El repo es privado: GitHub Pages gratis exige repo público (o GitHub Pro). Revisado: el historial de las tres ramas no contiene claves.
  2. Settings → Pages → Source: **GitHub Actions**.
  3. Settings → Environments → `github-pages` → Deployment branches: agregar `feature/webar` (por defecto solo deja publicar desde `main`).
- La página es pública para quien tenga el link. No contiene claves; la ubicación sigue sin salir del teléfono.

## Paso 4 — qué hay
- `public/instances/parque-el-bosque.json`: lugar, puntos de geocerca, avatar, personalidad (para el paso 6) y guion (saludo, despedida, preguntas frecuentes y respuestas de relleno, para el paso 5). **Las coordenadas del punto son provisionales** (centro de Valdivia): hay que poner las del punto real del parque.
- `src/instance.ts`: carga y valida la instancia elegida con `?i=<id>` (se descarga mientras se lee la pantalla de inicio). Si no existe, muestra un mensaje amable.
- `src/geofence.ts`: `watchPosition` + distancia con haversine al punto más cercano. Para que el GPS no haga aparecer y desaparecer al pájaro en el borde:
  - **Entrar:** distancia ≤ radio + mín(precisión/2, 15 m).
  - **Salir:** distancia > radio + 15 m, con precisión de ±60 m o mejor, durante 8 s seguidos.
- Llegada: el pájaro llega volando desde un lado y saluda. Despedida: aletazo y se aleja volando hacia un lado y hacia arriba; a los 2,6 s desaparece.
- Mientras estás fuera, un aviso abajo dice dónde está el pájaro y cuánto falta ("Te faltan unos 230 m para llegar"); sin GPS, "Buscando tu ubicación…".
- La ubicación solo se usa en el teléfono: no se envía a ningún sitio.
- Modo `?debug=1`: el panel muestra el punto, la distancia, la precisión y el estado de la geocerca. Botones nuevos: **Simular dentro** (alterna), **Fijar punto aquí** (guarda tu posición actual como punto de prueba con radio de 30 m, **solo en este teléfono**, sin escribir coordenadas en el repo) y **Quitar punto** (vuelve a los puntos de la instancia). También se puede pasar un punto por URL: `?lat=…&lon=…&r=…`.

## Cómo probar (paso 4)
1. Recarga `https://<IP-del-Mac>:5173/?debug=1` en el iPhone y toca "Comenzar".
2. Como el punto provisional está lejos, el pájaro **no** aparece y abajo se ve "Martín está en Parque Urbano El Bosque · Te faltan unos … para llegar".
3. **Simular dentro** → llega volando; volver a tocarlo → se despide y se va.
4. En casa, toca **Fijar punto aquí** (la página se recarga): el pájaro aparece porque estás en el centro del punto.
5. Aléjate caminando: a unos 45 m del punto (más 8 s) se despide; al volver, a unos 30–45 m según la precisión, vuelve a llegar.
6. Al terminar, **Quitar punto**.

## Paso 3 — qué hay
- `src/avatar/AvatarBody.ts`: base común de los avatares (`root`, `talking`, `thinking`, `greet()`, `update()`, `dispose()`), igual que en Unity. El seguimiento mueve `root`; el avatar anima su cuerpo dentro.
- `src/avatar/ProceduralKingfisher.ts`: port de `KingfisherAvatar.cs` en pose de **vuelo cernido** (cuerpo inclinado 25°, alas extendidas, patas recogidas, cola abierta). Animaciones: aleteo continuo (7 aletazos/s, hasta 11 cuando te persigue o saluda), el cuerpo sube con cada aletazo, vaivén lento, parpadeo, vistazos, te mira de lado con un ojo alternando, pico al hablar con cabeceo, aleteo fuerte al saludar y cabeza ladeada al pensar.
- `src/avatar/featherTextures.ts`: texturas en `<canvas>` (barrado, vetas, brillo), port de `FeatherTextures.cs`.
- `src/flight.ts` (`FlightFollower`): el objetivo es un punto delante de la cámara (inclinación limitada a ±35°). Se suavizan por separado el ángulo alrededor del usuario, la altura y la distancia con un resorte críticamente amortiguado (port de `SmoothDamp`), así vuela **en arco a tu alrededor** (no te atraviesa en un giro de 180°), sube hasta 18 cm mientras te alcanza y se ladea hacia la curva. Siempre mira al usuario. Al empezar llega volando desde un lado y saluda.
- Parámetros de URL para ajustar en terreno: `?dist=0.85` (m), `?size=1.5` (× tamaño real), `?lag=0.45` (s de retraso), `?test=1` (vuelve a mostrar los objetos del paso 2).
- Con `?debug=1` hay botones abajo: **Saludar**, **Hablar** (alterna), **Pensar** (alterna) y **Llegar** (repite la llegada).
- Vista previa en el escritorio, sin cámara ni permisos: `npm run preview:avatar` y abrir `http://localhost:5174/preview.html` (la cámara gira sola; `?still=1`, `?talk=1`, `?think=1`).

## Cómo probar (paso 3)
1. Con `npm run dev` corriendo, recarga en Safari del iPhone `https://<IP-del-Mac>:5173/?debug=1` y toca "Comenzar".
2. El martín pescador llega volando desde un lado, saluda aleteando fuerte y se queda flotando delante, a unos 85 cm, aleteando.
3. Gira el teléfono a un lado: el pájaro tarda medio segundo, vuela en arco subiendo un poco, inclinado hacia la curva, y vuelve a ponerse delante.
4. Date media vuelta rápido: debe rodearte por arriba, no pasar a través.
5. Prueba los botones Saludar, Hablar y Pensar.
6. Mira los FPS en el panel: la meta es 30 o más.

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
- **Coordenadas reales del punto del parque**: el JSON tiene unas provisionales. Isaías debe conseguir la ubicación del punto (o puntos) con el comité del parque.
- Observación de la prueba: el GPS dio ±54 m en interiores. La geocerca por defecto es de 30 m, así que en el paso 4 habrá que tener en cuenta la precisión (por ejemplo, no despedir al pájaro mientras la precisión sea peor que el radio).

## Historial
- 2026-11-27 · Claude Code: repo creado; paso 1 (esqueleto, HTTPS local, permisos, cámara de fondo).
- 2026-11-27 · Isaías: paso 1 probado en iPhone SE — cámara a pantalla completa, permisos ok, 60 FPS, vídeo 720×1280, GPS ±54 m en interiores.
- 2026-11-27 · Claude Code: paso 2 (orientación con el giroscopio, cubo y cuadrícula de prueba).
- 2026-11-27 · Isaías: paso 2 probado en iPhone SE — todo funciona.
- 2026-11-27 · Claude Code: el código se sube a la rama `feature/webar` de `isconnei/AR-HelloWorld`.
- 2026-11-27 · Claude Code: paso 3 (martín pescador procedural en vuelo cernido y seguimiento en arco), probado en la vista previa de escritorio.
- 2026-11-27 · Isaías: paso 3 probado en iPhone SE — OK.
- 2026-11-27 · Claude Code: paso 4 (JSON de instancia, geocerca con histéresis, llegada y despedida, aviso de distancia, herramientas de debug).
- 2026-11-27 · Claude Code: flujo de GitHub Actions para publicar en GitHub Pages (decisión de Isaías).
