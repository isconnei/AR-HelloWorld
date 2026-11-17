using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TalkingAvatar
{
    /// <summary>
    /// Barra superior en pantalla: botón "Aquí" (traer al avatar delante), campo de texto y botón "Enviar".
    /// </summary>
    public class ChatInputUI : MonoBehaviour
    {
        public event Action<string> Submitted;
        public event Action CallAvatarPressed;
        public event Action Typing;

        TMP_InputField m_Input;
        Button m_Send;
        Button m_Call;
        RectTransform m_Bar;
        Canvas m_Canvas;
        bool m_Busy;

        public bool IsComposing => m_Input.isFocused || !string.IsNullOrEmpty(m_Input.text);

        public static ChatInputUI Create(string avatarName)
        {
            var go = new GameObject("Talking Avatar UI", typeof(RectTransform));
            var ui = go.AddComponent<ChatInputUI>();
            ui.Build(avatarName);
            return ui;
        }

        void Build(string avatarName)
        {
            m_Canvas = gameObject.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Canvas.sortingOrder = 50;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            var barGo = new GameObject("Chat Bar", typeof(RectTransform));
            barGo.transform.SetParent(transform, false);
            m_Bar = (RectTransform)barGo.transform;
            m_Bar.anchorMin = new Vector2(0f, 1f);
            m_Bar.anchorMax = new Vector2(1f, 1f);
            m_Bar.pivot = new Vector2(0.5f, 1f);
            m_Bar.sizeDelta = new Vector2(0f, 150f);
            barGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            var row = barGo.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(24, 24, 24, 24);
            row.spacing = 16f;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            var resources = new TMP_DefaultControls.Resources();

            m_Call = MakeButton(resources, "Aquí", 170f, new Color(0.95f, 0.95f, 0.97f), new Color(0.15f, 0.15f, 0.2f));
            m_Call.onClick.AddListener(() => CallAvatarPressed?.Invoke());

            var inputGo = TMP_DefaultControls.CreateInputField(resources);
            inputGo.name = "Message Input";
            inputGo.transform.SetParent(m_Bar, false);
            inputGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            inputGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.96f);
            m_Input = inputGo.GetComponent<TMP_InputField>();
            m_Input.lineType = TMP_InputField.LineType.SingleLine;
            m_Input.characterLimit = 500;
            m_Input.pointSize = 40f;
            m_Input.textComponent.fontSize = 40f;
            if (m_Input.placeholder is TMP_Text placeholder)
            {
                placeholder.text = $"Escríbele a {avatarName}…";
                placeholder.fontSize = 40f;
            }
            m_Input.onSubmit.AddListener(_ => TrySubmit());
            m_Input.onValueChanged.AddListener(_ => Typing?.Invoke());

            m_Send = MakeButton(resources, "Enviar", 200f, new Color(0.22f, 0.5f, 0.95f), Color.white);
            m_Send.onClick.AddListener(TrySubmit);
        }

        public void SetBusy(bool busy)
        {
            m_Busy = busy;
            m_Send.interactable = !busy;
        }

        void TrySubmit()
        {
            var text = m_Input.text.Trim();
            if (m_Busy || text.Length == 0)
                return;
            m_Input.text = string.Empty;
            Submitted?.Invoke(text);
        }

        void LateUpdate()
        {
            // Bajar la barra por debajo del notch / zona no segura.
            var safe = Screen.safeArea;
            var topInset = (Screen.height - safe.yMax) / Mathf.Max(0.01f, m_Canvas.scaleFactor);
            m_Bar.anchoredPosition = new Vector2(0f, -topInset);
        }

        Button MakeButton(TMP_DefaultControls.Resources resources, string label, float width, Color background, Color textColor)
        {
            var go = TMP_DefaultControls.CreateButton(resources);
            go.name = label + " Button";
            go.transform.SetParent(m_Bar, false);
            go.AddComponent<LayoutElement>().preferredWidth = width;
            go.GetComponent<Image>().color = background;
            var text = go.GetComponentInChildren<TMP_Text>();
            text.text = label;
            text.fontSize = 38f;
            text.color = textColor;
            return go.GetComponent<Button>();
        }
    }
}
