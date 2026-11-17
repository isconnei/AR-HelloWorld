using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace TalkingAvatar
{
    /// <summary>
    /// Conversación con Claude mediante la Messages API (POST /v1/messages) usando UnityWebRequest.
    /// Guarda el historial completo y devuelve cada turno del asistente sin modificarlo
    /// (incluye bloques de thinking/fallback), como pide la API.
    /// </summary>
    public class ClaudeChatClient
    {
        const string Endpoint = "https://api.anthropic.com/v1/messages";
        const string ApiVersion = "2026-10-21";
        const string FallbackBeta = "server-side-fallback-2026-07-01";
        const int TimeoutSeconds = 120;

        public struct Reply
        {
            public bool ok;
            /// <summary>Texto para mostrar en la burbuja (respuesta o mensaje de error amable).</summary>
            public string text;
        }

        readonly AvatarChatSettings m_Settings;
        readonly string m_ApiKey;
        readonly string m_SystemPrompt;
        readonly JArray m_History = new JArray();

        public bool HasApiKey => !string.IsNullOrEmpty(m_ApiKey);
        public bool IsBusy { get; private set; }

        public ClaudeChatClient(AvatarChatSettings settings, string apiKey)
        {
            m_Settings = settings;
            m_ApiKey = apiKey;
            m_SystemPrompt = settings.BuildSystemPrompt();
        }

        public IEnumerator Send(string userText, Action<Reply> onDone)
        {
            if (IsBusy)
                yield break;
            IsBusy = true;

            var userMessage = new JObject { ["role"] = "user", ["content"] = userText };
            m_History.Add(userMessage);

            var body = new JObject
            {
                ["model"] = m_Settings.model,
                ["max_tokens"] = m_Settings.maxTokens,
                ["system"] = m_SystemPrompt,
                ["messages"] = m_History.DeepClone(),
                ["output_config"] = new JObject { ["effort"] = m_Settings.effort },
            };
            if (m_Settings.useRefusalFallbacks)
                body["fallbacks"] = "default";

            using var request = new UnityWebRequest(Endpoint, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body.ToString(Formatting.None)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = TimeoutSeconds;
            request.SetRequestHeader("content-type", "application/json");
            request.SetRequestHeader("x-api-key", m_ApiKey);
            request.SetRequestHeader("anthropic-version", ApiVersion);
            if (m_Settings.useRefusalFallbacks)
                request.SetRequestHeader("anthropic-beta", FallbackBeta);

            yield return request.SendWebRequest();

            Reply reply;
            if (request.result != UnityWebRequest.Result.Success)
            {
                // Quitamos el turno que no obtuvo respuesta para que el historial siga alternando user/assistant.
                m_History.Remove(userMessage);
                reply = new Reply { ok = false, text = DescribeFailure(request) };
            }
            else
            {
                reply = HandleResponse(request.downloadHandler.text, userMessage);
            }

            IsBusy = false;
            onDone?.Invoke(reply);
        }

        Reply HandleResponse(string json, JObject userMessage)
        {
            JObject response;
            try
            {
                response = JObject.Parse(json);
            }
            catch (JsonException e)
            {
                m_History.Remove(userMessage);
                Debug.LogWarning($"[TalkingAvatar] Respuesta no válida de la API: {e.Message}");
                return new Reply { ok = false, text = "No entendí la respuesta del servidor. ¿Lo intentamos otra vez?" };
            }

            var stopReason = (string)response["stop_reason"];
            if (stopReason == "refusal")
            {
                m_History.Remove(userMessage);
                Debug.LogWarning($"[TalkingAvatar] La petición fue rechazada: {response["stop_details"]}");
                return new Reply { ok = false, text = "Prefiero no seguir por ahí. ¿Hablamos de otra cosa?" };
            }

            var content = response["content"] as JArray ?? new JArray();
            m_History.Add(new JObject { ["role"] = "assistant", ["content"] = content });

            var text = new StringBuilder();
            foreach (var block in content)
            {
                if ((string)block["type"] == "text")
                    text.Append((string)block["text"]);
            }

            var spoken = text.ToString().Trim();
            if (spoken.Length == 0)
                spoken = "…";
            return new Reply { ok = true, text = spoken };
        }

        static string DescribeFailure(UnityWebRequest request)
        {
            var apiMessage = string.Empty;
            try
            {
                var error = JObject.Parse(request.downloadHandler?.text ?? string.Empty)["error"];
                apiMessage = $"{error?["type"]}: {error?["message"]}";
            }
            catch (JsonException)
            {
                // El cuerpo no era JSON (p. ej. sin conexión); usamos solo el código.
            }

            Debug.LogWarning($"[TalkingAvatar] Error de la API ({request.responseCode} {request.result}): {request.error} {apiMessage}");

            if (request.result == UnityWebRequest.Result.ConnectionError)
                return "No tengo conexión a internet ahora mismo. ¿Lo intentamos otra vez?";

            switch (request.responseCode)
            {
                case 401:
                case 403:
                    return "Mi API key no funciona (error " + request.responseCode + "). Revisa AnthropicApiKey.txt.";
                case 429:
                case 529:
                    return "Hay mucha demanda ahora mismo. Inténtalo de nuevo en un momento.";
                default:
                    return "Algo falló al pensar mi respuesta (error " + request.responseCode + "). ¿Me lo repites?";
            }
        }
    }
}
