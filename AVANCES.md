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
- Avatar: humano estilizado hecho por código con primitivas (sin modelos descargados), para poder cambiarlo más adelante por un modelo real (Mixamo / Ready Player Me).

## Qué está hecho (rama `feature/talking-avatar`, ya en GitHub)
Todo vive en `Assets/TalkingAvatar/`:
- `HumanAvatar.cs`: construye a "Luna" (~1,65 m: cabeza, pelo, ojos, cejas, boca, torso, brazos articulados, piernas). Animaciones: respiración, parpadeo, mirada que sigue a la cámara, saludo con la mano, boca y gestos al hablar, pose de "pensando".
- `SpeechBubble.cs`: burbuja en el mundo que mira a la cámara, muestra "..." mientras piensa y escribe la respuesta letra a letra.
- `ChatInputUI.cs`: barra superior con botón "Aquí" (trae al avatar delante), campo de texto y botón "Enviar". Respeta el notch.
- `ClaudeChatClient.cs`: llamadas a la Messages API con UnityWebRequest + Newtonsoft JSON (el SDK oficial de C# no va bien en builds móviles de Unity). Guarda el historial completo y devuelve cada turno del asistente sin modificar. Incluye fallback del servidor ante rechazos (`fallbacks: "default"`), manejo de `stop_reason: "refusal"` y mensajes de error amables (sin conexión, 401, 429/529).
- `TalkingAvatarController.cs`: se crea solo al cargar `SampleScene` (sin tocar la escena). Busca el suelo con ARPlaneManager y coloca al avatar a 1,6 m delante; si no hay suelo en 8 s, estima la altura. Saluda y empieza la charla. Si me quedo callado ~35 s, retoma la conversación (máximo 2 veces seguidas).
- `AvatarChatSettings` (ScriptableObject en `Resources/`): nombre, personalidad (system prompt en español, respuestas de 1–3 frases en texto plano), modelo, effort, tiempos. Menú: Tools → Talking Avatar → Ajustes del avatar.
- `Editor/TalkingAvatarEditorTools.cs`: crea el asset de ajustes si falta y guarda una vista previa PNG del avatar (Tools → Talking Avatar → Guardar vista previa).
- La API key va en `Assets/TalkingAvatar/Resources/AnthropicApiKey.txt`, ignorado por git (en el Editor también vale la variable `ANTHROPIC_API_KEY`). Se obtiene en platform.claude.com → Settings → API Keys.

## Cómo probarlo
1. `git fetch origin` y `git switch feature/talking-avatar` en la copia local.
2. Crear `Assets/TalkingAvatar/Resources/AnthropicApiKey.txt` con la key.
3. Abrir en Unity 6000.6.9f1, cargar `Assets/Scenes/SampleScene` y darle a Play (XR Simulation en el Editor).

## Verificado
- El proyecto compila sin errores en Unity 6000.6.9f1.
- Una captura renderizada desde el proyecto muestra al avatar con sus colores, saludando, y la burbuja sin solaparse con la mano.

## Pendiente / sin probar
- Una conversación real con la API (falta poner mi API key).
- Probar en Play Mode con XR Simulation y en un móvil real (iOS/Android).
- Revisar si la barra de chat se solapa con botones de la plantilla AR.
- Seguridad: la key va dentro de la app compilada. Vale para pruebas personales; para distribuir haría falta un servidor intermedio.

## Ideas para seguir
- Voz: reconocimiento de voz y texto a voz (necesita plugins nativos por plataforma).
- Sustituir el avatar de primitivas por un modelo humanoide con Animator.
- Respuestas en streaming para que la burbuja empiece a escribir antes.

## Historial
- 2026-11-17 · Claude Code: avatar Luna, chat con Claude, burbuja y barra de texto; rama `feature/talking-avatar` subida a GitHub.
- 2026-11-18 · Claude Code: creado este documento de avances.
