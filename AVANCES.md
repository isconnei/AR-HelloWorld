# Avances: avatar conversacional en AR (Unity) — proyecto AR-HelloWorld

> Documento compartido entre Claude Chat y Claude Code. Cuando haya cambios, actualiza las secciones
> correspondientes y añade una línea al **Historial** al final, con la fecha y quién lo cambió.

## Contexto
- Repo: github.com/isconnei/AR-HelloWorld (plantilla Mobile AR de Unity).
- Unity 6000.6.9f1, URP 17.6, AR Foundation 6.6.2 (ARKit + ARCore), XR Interaction Toolkit 3.6.1, Input System.
- Objetivo: que en la escena AR aparezca un avatar humano que quiera conversar conmigo.

## Decisiones tomadas
- Cerebro: Claude API (Messages API), modelo `claude-opus-5-5`, effort `low` para respuestas rápidas.
- Interacción: texto. Escribo en una barra arriba de la pantalla; el avatar responde en una burbuja sobre su cabeza.
- Avatar: hecho por código con primitivas (sin modelos descargados). Desde el 2026-11-24 el avatar por defecto es **Martín, un martín pescador** posado en una ramita a la altura de los ojos; la humana **Luna** sigue disponible en los ajustes (`avatarKind`).
- Siguiente paso previsto: sustituir el martín pescador de primitivas por un modelo 3D realista (FBX/GLB riggeado, <50k triángulos, licencia que permita usarlo en la app).

## Qué está hecho (rama `feature/talking-avatar`, ya en GitHub)
Todo vive en `Assets/TalkingAvatar/`:
- `AvatarBody.cs`: clase base común de los avatares (hablar, pensar, mirar, saludar, aparecer, dónde va la burbuja) y utilidades para construir con primitivas. El controlador solo usa esta clase. Todos los materiales parten de `Resources/AvatarBaseMaterial.mat` (URP Lit), para que el shader entre en el build del móvil (sin él, el avatar se veía magenta en el iPhone).
- `FeatherTextures.cs`: texturas de plumaje generadas por código, sin archivos de imagen: barrado (píleo), vetas (pecho, alas, cola, garganta), brillo (lomo), corteza (rama) y nervios (hojas).
- `KingfisherAvatar.cs`: construye a "Martín", martín pescador común (Alcedo atthis) con sus colores reales: lomo azul eléctrico, alas verdeazuladas con motas, pecho y mejillas naranjas, garganta y mancha del cuello blancas, pico negro en daga y patas rojas, sobre una ramita con hojas. ~1,5× tamaño real (configurable). Animaciones: respiración, parpadeo, vistazos rápidos de pájaro, te mira de lado con un ojo (alternando, para que se vea el pico), abre el pico al hablar, aletea al saludar, golpes de cola y cabeza ladeada al pensar.
- `HumanAvatar.cs`: construye a "Luna" (~1,65 m: cabeza, pelo, ojos, cejas, boca, torso, brazos articulados, piernas). Animaciones: respiración, parpadeo, mirada que sigue a la cámara, saludo con la mano, boca y gestos al hablar, pose de "pensando".
- `SpeechBubble.cs`: burbuja en el mundo que mira a la cámara, muestra "..." mientras piensa y escribe la respuesta letra a letra.
- `ChatInputUI.cs`: barra superior con botón "Aquí" (trae al avatar delante), campo de texto y botón "Enviar". Respeta el notch.
- `ClaudeChatClient.cs`: llamadas a la Messages API con UnityWebRequest + Newtonsoft JSON (el SDK oficial de C# no va bien en builds móviles de Unity). Guarda el historial completo y devuelve cada turno del asistente sin modificar. Incluye fallback del servidor ante rechazos (`fallbacks: "default"`), manejo de `stop_reason: "refusal"` y mensajes de error amables (sin conexión, 401, 429/529).
- `TalkingAvatarController.cs`: se crea solo al cargar `SampleScene` (sin tocar la escena) y crea el avatar elegido en los ajustes. El martín pescador aparece posado en el aire a 0,7 m delante y 12 cm por debajo de la cámara en cuanto AR rastrea; la humana busca el suelo con ARPlaneManager a 1,6 m (si no hay suelo en 8 s, estima la altura). Saluda y empieza la charla. Si me quedo callado ~35 s, retoma la conversación (máximo 2 veces seguidas).
- `AvatarChatSettings` (ScriptableObject en `Resources/`): tipo de avatar, nombre y personalidad de cada uno (system prompt en español, respuestas de 1–3 frases en texto plano), tamaño y distancia del martín pescador, modelo, effort, tiempos. Menú: Tools → Talking Avatar → Ajustes del avatar.
- `Editor/TalkingAvatarEditorTools.cs`: crea el asset de ajustes y el material base si faltan, quita de la escena las funciones de figuras 3D de la plantilla (Tools → Talking Avatar → Quitar funciones de figuras 3D de la plantilla) y guarda una vista previa PNG del avatar con encuadre automático (Tools → Talking Avatar → Guardar vista previa).
- `SampleScene` ya no tiene las funciones de la plantilla Mobile AR: se quitaron `UI` (botones de crear/borrar/opciones, menú de objetos, avisos de coaching, saludo y menú de debug), `Object Spawner` y `Screen Space Ray Interactor`. Quedan `AR Session`, `XR Origin` (cámara y detección de planos), la luz y el `EventSystem`. Los scripts de la plantilla siguen en `Assets/MobileARTemplateAssets` pero ya no se usan.
- La API key va en `Assets/TalkingAvatar/Resources/AnthropicApiKey.txt`, ignorado por git (en el Editor también vale la variable `ANTHROPIC_API_KEY`). Se obtiene en platform.claude.com → Settings → API Keys.

## Cómo probarlo
1. `git fetch origin` y `git switch feature/talking-avatar` en la copia local.
2. Crear `Assets/TalkingAvatar/Resources/AnthropicApiKey.txt` con la key.
3. Abrir en Unity 6000.6.9f1, cargar `Assets/Scenes/SampleScene` y darle a Play (XR Simulation en el Editor).

## Verificado
- El proyecto compila sin errores en Unity 6000.6.9f1.
- Una captura renderizada desde el proyecto muestra al avatar con sus colores, saludando, y la burbuja sin solaparse con la mano.
- 2026-11-24: compila con el martín pescador; capturas del martín pescador (aleteando, pico abierto, de perfil) y de la humana renderizadas correctamente.
- 2026-11-24: probado en iPhone por el usuario: Martín aparece y conversa con Claude, pero se veía magenta (shader no incluido en el build). Corregido con el material base URP; falta confirmarlo en el iPhone.

## Pendiente / sin probar
- Confirmar en el iPhone que Martín ya sale con sus colores y texturas (no magenta) y que no queda nada de la plantilla.
- Los puntos blancos de la visualización de planos AR siguen apareciendo; se pueden ocultar si molestan (el martín pescador no los necesita).
- Probar en Play Mode con XR Simulation y en un móvil real (iOS/Android), incluida la altura y distancia a la que se posa el martín pescador.
- Las animaciones (aleteo, vistazos, pico) solo se han visto en capturas estáticas, no en movimiento.
- Revisar si la barra de chat se solapa con botones de la plantilla AR.
- Seguridad: la key va dentro de la app compilada. Vale para pruebas personales; para distribuir haría falta un servidor intermedio.

## Ideas para seguir
- Voz: reconocimiento de voz y texto a voz (necesita plugins nativos por plataforma).
- Sustituir el avatar de primitivas por un modelo humanoide con Animator.
- Respuestas en streaming para que la burbuja empiece a escribir antes.

## Historial
- 2026-11-17 · Claude Code: avatar Luna, chat con Claude, burbuja y barra de texto; rama `feature/talking-avatar` subida a GitHub.
- 2026-11-18 · Claude Code: creado este documento de avances.
- 2026-11-24 · Claude Code: nuevo avatar por defecto, Martín el martín pescador (primitivas), con clase base `AvatarBody`; Luna sigue disponible en los ajustes.
- 2026-11-24 · Claude Code: quitadas de la escena las funciones de figuras 3D de la plantilla; arreglado el magenta en el móvil con un material URP en Resources; texturas de plumaje procedurales para Martín.
