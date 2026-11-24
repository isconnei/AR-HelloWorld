using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TalkingAvatar
{
    /// <summary>
    /// Burbuja de diálogo en el mundo, sobre la cabeza del avatar. Siempre mira a la cámara,
    /// muestra "..." mientras el avatar piensa y escribe la respuesta letra a letra.
    /// </summary>
    public class SpeechBubble : MonoBehaviour
    {
        const float Width = 640f;
        const float PixelSize = 0.0011f; // 640 px ≈ 0,7 m

        public float charactersPerSecond = 32f;

        /// <summary>True mientras la respuesta se está "diciendo" (el avatar mueve la boca).</summary>
        public bool IsRevealing { get; private set; }

        TextMeshProUGUI m_Label;
        CanvasGroup m_Group;
        RectTransform m_Panel;
        bool m_Thinking;
        float m_RevealStart;

        /// <summary>Crea la burbuja colocada sobre un avatar, con su tamaño y cola.</summary>
        public static SpeechBubble Create(AvatarBody avatar) =>
            Create(avatar.transform, avatar.BubbleAnchor, avatar.BubbleScale, avatar.BubbleTailOffset);

        /// <param name="scale">1 = unos 0,7 m de ancho.</param>
        /// <param name="tailOffset">Desplazamiento horizontal de la cola en metros (negativo = izquierda).</param>
        public static SpeechBubble Create(Transform parent, Vector3 localPosition, float scale = 1f, float tailOffset = 0f)
        {
            var go = new GameObject("Speech Bubble", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var bubble = go.AddComponent<SpeechBubble>();
            bubble.Build(scale, tailOffset);
            return bubble;
        }

        void Build(float scale, float tailOffset)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var root = (RectTransform)transform;
            root.sizeDelta = new Vector2(Width, 10f);
            root.pivot = new Vector2(0.5f, 0f);
            var metersPerPixel = PixelSize * Mathf.Max(0.01f, scale);
            root.localScale = Vector3.one * metersPerPixel;
            m_Group = gameObject.AddComponent<CanvasGroup>();

            var fill = new Color(1f, 1f, 1f, 0.94f);

            // "Cola" de la burbuja: un rombo que asoma bajo el panel.
            var tail = NewChild("Tail", root).gameObject;
            var tailRect = (RectTransform)tail.transform;
            tailRect.anchorMin = tailRect.anchorMax = new Vector2(0.5f, 0f);
            tailRect.sizeDelta = new Vector2(34f, 34f);
            tailRect.anchoredPosition = new Vector2(tailOffset / metersPerPixel, 0f);
            tailRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tail.AddComponent<Image>().color = fill;

            m_Panel = NewChild("Panel", root);
            m_Panel.anchorMin = m_Panel.anchorMax = new Vector2(0.5f, 0f);
            m_Panel.pivot = new Vector2(0.5f, 0f);
            m_Panel.sizeDelta = new Vector2(Width, 100f);
            m_Panel.gameObject.AddComponent<Image>().color = fill;
            var layout = m_Panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 22, 22);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = m_Panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var labelRect = NewChild("Text", m_Panel);
            m_Label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            m_Label.fontSize = 36f;
            m_Label.color = new Color(0.12f, 0.12f, 0.16f);
            m_Label.alignment = TextAlignmentOptions.Left;
            m_Label.textWrappingMode = TextWrappingModes.Normal;
            m_Label.raycastTarget = false;

            Hide();
        }

        public void ShowThinking()
        {
            m_Thinking = true;
            IsRevealing = false;
            m_Label.maxVisibleCharacters = int.MaxValue;
            SetText("...");
            m_Group.alpha = 1f;
        }

        public void Say(string text)
        {
            m_Thinking = false;
            SetText(text);
            m_Label.maxVisibleCharacters = 0;
            m_RevealStart = Time.time;
            IsRevealing = true;
            m_Group.alpha = 1f;
        }

        /// <summary>Muestra el texto completo de golpe (capturas fuera de Play Mode).</summary>
        public void SayInstant(string text)
        {
            m_Thinking = false;
            IsRevealing = false;
            m_Label.maxVisibleCharacters = int.MaxValue;
            SetText(text);
            m_Group.alpha = 1f;
        }

        public void Hide()
        {
            m_Thinking = false;
            IsRevealing = false;
            m_Group.alpha = 0f;
        }

        public void FaceCamera(Camera cam)
        {
            if (cam == null)
                return;
            var away = transform.position - cam.transform.position;
            if (away.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }

        void LateUpdate()
        {
            FaceCamera(Camera.main);

            if (m_Thinking)
            {
                var dots = 1 + (int)(Time.time * 3f) % 3;
                m_Label.text = new string('.', dots).PadRight(3, ' ');
            }
            else if (IsRevealing)
            {
                var visible = Mathf.FloorToInt((Time.time - m_RevealStart) * charactersPerSecond);
                m_Label.maxVisibleCharacters = visible;
                if (visible >= m_Label.textInfo.characterCount)
                {
                    m_Label.maxVisibleCharacters = int.MaxValue;
                    IsRevealing = false;
                }
            }
        }

        void SetText(string text)
        {
            m_Label.text = text;
            m_Label.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_Panel);
        }

        static RectTransform NewChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
    }
}
