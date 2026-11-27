# Brief para Claude Code — Prototipo WebAR "pájaro en vuelo"

## Contexto
Proyecto Avatares AR: avatares (NPC) geolocalizados que conversan con las personas en lugares reales. El piloto es en el Parque Urbano El Bosque (Valdivia), con un pájaro nativo como avatar (la especie se define con el comité del parque).

Hasta ahora existe una app Unity (repo `isconnei/AR-HelloWorld`, rama `feature/talking-avatar`) con Martín, un martín pescador hecho con primitivas que conversa con Claude. Para el piloto, esa app se reemplaza por una versión web: la gente abre un link o escanea un QR, sin descargar nada. La app Unity queda para una fase 2.

Roles: Isaías es PM y prueba en terreno con un iPhone SE (2.ª gen) y una MacBook Air. Tiene unas 4 h al día. Tú escribes el código y lo guías paso a paso.

## Objetivo del prototipo
Una página web que, en el celular:

1. Muestre la cámara trasera a pantalla completa como fondo.
2. Muestre un pájaro 3D volando frente al usuario, a unos 0,7–1 m, que lo sigue: cuando la persona gira el teléfono, el pájaro vuelve a ponerse delante con un pequeño retraso suave, como si volara tras ella. No necesita anclarse a ningún punto del mundo ni detectar superficies.
3. Use el GPS para una geocerca: el pájaro aparece (con una animación de llegada) cuando la persona entra en el radio de un punto configurado, y se despide al salir.
4. Permita conversar con el pájaro mediante texto en un chat superpuesto.

Debe funcionar en Safari de iOS y en Chrome de Android.

## Decisiones técnicas (ya tomadas, no cambiarlas sin consultar)
- No usar WebXR `immersive-ar`: Safari de iOS no lo soporta. Usar el enfoque 3DoF: `getUserMedia` para la cámara, `DeviceOrientationEvent` para la rotación y `navigator.geolocation.watchPosition` para el GPS.
- No usar 8th Wall: cerró en 2026.
- three.js + Vite + TypeScript. Sin frameworks de UI pesados; HTML y CSS simples para el chat.
- Se puede usar LocAR.js si simplifica algo, pero como el pájaro no está anclado a coordenadas (solo hay una geocerca), probablemente basta con la geolocalización del navegador y el cálculo de distancia con la fórmula de haversine.
- Avatar intercambiable: crea una interfaz o clase base `AvatarBody` (igual que en Unity), con `ProceduralKingfisher` como primera implementación y luego `GltfAvatar` (carga un `.glb` con animaciones). La especie final aún no está definida.
- Configuración por JSON (`public/instances/<id>.json`): nombre del lugar, puntos de geocerca (lat, lon, radio en metros), avatar, personalidad y guion. Una instancia se elige por URL, por ejemplo `/?i=parque-el-bosque`.
- Hosting: Azure Static Web Apps, con una función de Azure como proxy del backend (el stack del proyecto es Azure).

## Requisitos importantes de los navegadores (fuentes típicas de errores)
- HTTPS obligatorio para la cámara, el GPS y los sensores, también en desarrollo. Usa `@vitejs/plugin-basic-ssl` y `vite --host` para probar desde el iPhone en la misma red Wi-Fi.
- iOS exige un toque del usuario antes de pedir permisos: `DeviceOrientationEvent.requestPermission()` solo funciona dentro del handler de un clic. Haz una pantalla de inicio con un botón "Comenzar" que pida, en este orden, el movimiento, la cámara y la ubicación, con textos claros en español que expliquen para qué se usa cada uno.
- El `<video>` de la cámara necesita `playsinline`, `muted` y `autoplay`, y `facingMode: "environment"`.
- Convierte correctamente `alpha`, `beta` y `gamma` a cuaterniones considerando la orientación de la pantalla. Usa como referencia el antiguo `DeviceOrientationControls` de three.js. Aplica suavizado para que el pájaro no tiemble.
- El FOV de la cámara virtual debe coincidir aproximadamente con el de la cámara física; deja el valor configurable para ajustarlo en terreno.
- Si se niega un permiso, muestra un mensaje amable con instrucciones para volver a activarlo en Ajustes, y no una pantalla en blanco.

## El pájaro
- Primera versión: porta a three.js el martín pescador procedural de Unity (`KingfisherAvatar` + `FeatherTextures.cs` en el repo `AR-HelloWorld`). Si no tienes acceso a ese repo, recréalo: lomo azul eléctrico, alas verdeazuladas con motas, pecho y mejillas naranjas, garganta blanca, pico negro en forma de daga y patas rojas. Texturas generadas en un `<canvas>`, sin imágenes.
- Animaciones de vuelo: aleteo continuo, leve vaivén vertical, inclinación al girar, parpadeo, abrir el pico al hablar, un aleteo más fuerte al saludar y ladear la cabeza al "pensar" mientras espera la respuesta.
- Movimiento de seguimiento: el objetivo es una posición fija delante de la cámara. El pájaro se acerca a ella con interpolación amortiguada (por ejemplo, `damp` o un resorte crítico), con un retraso de unos 0,3–0,6 s y una ligera trayectoria curva, y siempre mira hacia el usuario.
- Iluminación simple (hemisférica + direccional) y sin sombras, para cuidar el rendimiento.
- Meta de rendimiento: 30+ FPS estables en un iPhone SE 2.ª gen.

## Conversación
- Nunca pongas la API key de Anthropic en el frontend. Crea un endpoint proxy mínimo (`/api/chat`, función de Azure en Node/TypeScript) que reciba el historial, agregue el system prompt de la instancia y llame a la API de Claude. La clave va en una variable de entorno. Incluye un límite simple de peticiones por IP y un máximo de turnos por sesión.
- Respuestas en streaming si no complica demasiado; si complica, déjalo para después.
- Modo sin conexión / guion: si no hay red o falla el proxy, el pájaro responde con el guion precargado del JSON (saludo, preguntas frecuentes con palabras clave y despedida). El MVP del proyecto se basa en un guion; la IA es el plan avanzado.
- Personalidad de ejemplo (Martín): curioso, algo presumido de sus plumas, le encantan los ríos y pescar, y habla de cualquier tema desde su mirada de pájaro. Respuestas cortas (2 a 3 frases), en español de Chile, amables y aptas para todo público.

## PWA y señal débil
En el parque puede haber poca señal. Agrega un service worker (por ejemplo, `vite-plugin-pwa`) que cachee la app, el modelo y el JSON de la instancia, para que todo cargue sin red después de la primera visita. Solo la conversación con IA necesita conexión.

## Privacidad (Ley 21.719)
- Sin cuentas ni datos personales.
- La ubicación se procesa solo en el dispositivo para la geocerca y no se envía al servidor.
- Las conversaciones no se guardan en el servidor; como mucho, métricas anónimas agregadas (por ejemplo, "sesión iniciada" o "número de turnos"), sin IP ni ubicación.
- Muestra una línea breve en la pantalla de inicio que explique esto.

## Plan de trabajo por pasos
Avanza de a un paso. Al terminar cada uno, detente y dile a Isaías exactamente cómo probarlo en el iPhone y qué debería ver. No sigas con el siguiente hasta que confirme.

1. Esqueleto: Vite + TS + three.js, HTTPS local, pantalla de inicio con "Comenzar" y permisos, y la cámara de fondo. Prueba: en el iPhone se ve la cámara a pantalla completa.
2. Orientación: un cubo de prueba fijo en el mundo usando el giroscopio. Prueba: al girar el teléfono, el cubo se queda "en su sitio".
3. Pájaro + seguimiento: el martín pescador procedural con animaciones de vuelo y el comportamiento de seguimiento. Prueba: el pájaro vuela delante y te alcanza con suavidad cuando giras.
4. Geocerca: JSON de instancia, `watchPosition` y la aparición y despedida del pájaro. Incluye un modo debug (`?debug=1`) que muestre la distancia al punto, la precisión del GPS y un botón para simular que se está dentro de la geocerca. Prueba: con un punto puesto en la casa de Isaías, el pájaro aparece al llegar.
5. Chat con guion: la interfaz de chat y las respuestas del guion, sin IA. Prueba: conversación completa sin conexión.
6. Proxy + IA: la función de Azure y la integración con Claude, con el guion como respaldo. Prueba: conversación libre con el pájaro.
7. PWA + despliegue: service worker, Azure Static Web Apps con dominio y la generación del QR. Prueba: abrir desde el QR en modo avión después de una primera visita.

## Criterios de aceptación del prototipo
- Funciona en Safari de iOS (iPhone SE 2.ª gen) y en Chrome de Android.
- Desde el QR hasta ver al pájaro: menos de 3 toques y menos de 10 s con buena señal.
- El pájaro sigue al usuario con fluidez, sin temblores visibles, a 30+ FPS.
- La geocerca funciona con la precisión real del GPS (radio configurable; por defecto, 30 m).
- La conversación funciona con IA y, sin red, con el guion.
- No hay ninguna clave en el código del frontend, y la ubicación no sale del dispositivo.

## Al final de cada sesión
Actualiza un archivo `AVANCES.md` en el repo: qué se hizo, el estado de cada paso, los problemas abiertos y lo que viene. Haz commits pequeños con mensajes claros en español.
