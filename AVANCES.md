# Avances — Prototipo WebAR "pájaro en vuelo"

> Documento compartido entre Claude Chat y Claude Code. El brief completo está en `CLAUDE.md`.
> Al terminar cada sesión: actualizar el estado de los pasos, los problemas abiertos y añadir una línea al **Historial**.

## Estado de los pasos

| Paso | Qué | Estado |
|---|---|---|
| 1 | Esqueleto: Vite + TS + three.js, HTTPS local, pantalla de inicio con permisos, cámara de fondo | Hecho, **pendiente de probar en el iPhone** |
| 2 | Orientación: cubo de prueba fijo en el mundo con el giroscopio | Pendiente |
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

## Cómo probar (paso 1)
1. En el Mac: `npm run dev` dentro de esta carpeta. Muestra una dirección `https://<IP-del-Mac>:5173/`.
2. El iPhone y el Mac deben estar en la misma red (o el Mac conectado al hotspot del iPhone).
3. En Safari del iPhone, abre `https://<IP-del-Mac>:5173/?debug=1`. Safari avisa de que el certificado no es de confianza: "Mostrar detalles" > "visitar este sitio web".
4. Toca "Comenzar" y acepta movimiento, cámara y ubicación.
5. Debe verse la cámara trasera a pantalla completa y, arriba a la izquierda, el panel de debug con FPS, permisos y precisión del GPS.

## Problemas abiertos
- Ninguno conocido todavía; falta la prueba en el iPhone.

## Historial
- 2026-11-27 · Claude Code: repo creado; paso 1 (esqueleto, HTTPS local, permisos, cámara de fondo).
