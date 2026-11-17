using System;
using UnityEngine;

namespace TalkingAvatar
{
    /// <summary>
    /// Ajustes del avatar conversacional. El asset vive en Assets/TalkingAvatar/Resources/AvatarChatSettings.asset
    /// (se crea solo al abrir el proyecto) y se edita desde el Inspector.
    /// La API key NO se guarda aquí: va en Resources/AnthropicApiKey.txt (ignorado por git)
    /// o, dentro del Editor, en la variable de entorno ANTHROPIC_API_KEY.
    /// </summary>
    [CreateAssetMenu(menuName = "Talking Avatar/Chat Settings", fileName = "AvatarChatSettings")]
    public class AvatarChatSettings : ScriptableObject
    {
        public const string ResourcePath = "AvatarChatSettings";
        public const string ApiKeyResourcePath = "AnthropicApiKey";
        const string NamePlaceholder = "{nombre}";

        [Header("Personaje")]
        [Tooltip("Nombre del avatar. Sustituye a {nombre} en la personalidad.")]
        public string avatarName = "Luna";

        [Tooltip("Instrucciones de sistema que definen cómo es y cómo habla el avatar.")]
        [TextArea(8, 30)]
        public string persona =
            "Eres {nombre}, un personaje humano virtual que aparece en realidad aumentada, a través de la cámara del " +
            "teléfono, en el espacio de la persona con quien hablas. Eres cálida, curiosa y con buen humor, y de verdad " +
            "quieres conocer a esa persona y conversar con ella.\n\n" +
            "Cómo hablas:\n" +
            "- Respondes en el idioma de la persona (español por defecto).\n" +
            "- Tus respuestas aparecen en una burbuja de diálogo pequeña sobre tu cabeza, así que usas de una a tres " +
            "frases cortas y naturales, como en una charla cara a cara.\n" +
            "- Escribes solo texto plano: sin markdown, listas, emojis ni acciones entre asteriscos, porque la burbuja " +
            "no puede mostrarlos.\n" +
            "- Mantienes viva la conversación: te interesas por lo que te cuenta y casi siempre terminas con una " +
            "pregunta o un comentario que invite a seguir.\n\n" +
            "Los mensajes entre corchetes que empiezan con \"Evento:\" no los escribe la persona: te avisan de lo que " +
            "pasa en la escena (por ejemplo, que acabas de aparecer o que la persona lleva un rato en silencio). " +
            "Reacciona a ellos con naturalidad y sin mencionar que son eventos.";

        [Header("Claude API")]
        public string model = "claude-opus-5-5";

        [Tooltip("low | medium | high | xhigh | max. 'low' da respuestas más rápidas, ideal para charla.")]
        public string effort = "low";

        [Tooltip("Límite de tokens de salida por respuesta (solo se cobra lo que se usa).")]
        public int maxTokens = 16000;

        [Tooltip("Si el modelo rechaza una petición, la API la reintenta con un modelo de respaldo.")]
        public bool useRefusalFallbacks = true;

        [Header("Conversación")]
        [Tooltip("Segundos de silencio tras los que el avatar retoma la charla por iniciativa propia.")]
        public float idleNudgeSeconds = 35f;

        [Tooltip("Cuántas veces seguidas puede retomar la charla si no le contestas.")]
        public int maxNudgesInARow = 2;

        [Header("Colocación en AR")]
        [Tooltip("Distancia (m) delante de la cámara a la que aparece el avatar.")]
        public float placementDistance = 1.6f;

        [Tooltip("Segundos esperando un suelo detectado antes de colocar el avatar a una altura estimada.")]
        public float placementTimeout = 8f;

        [Header("Arranque")]
        public bool autoStartInScene = true;
        public string sceneName = "SampleScene";

        public string BuildSystemPrompt() => persona.Replace(NamePlaceholder, avatarName);

        public static AvatarChatSettings Load()
        {
            var settings = Resources.Load<AvatarChatSettings>(ResourcePath);
            return settings != null ? settings : CreateInstance<AvatarChatSettings>();
        }

        public static string LoadApiKey()
        {
#if UNITY_EDITOR
            var fromEnv = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            if (!string.IsNullOrWhiteSpace(fromEnv))
                return fromEnv.Trim();
#endif
            var keyAsset = Resources.Load<TextAsset>(ApiKeyResourcePath);
            return keyAsset != null ? keyAsset.text.Trim() : string.Empty;
        }
    }
}
