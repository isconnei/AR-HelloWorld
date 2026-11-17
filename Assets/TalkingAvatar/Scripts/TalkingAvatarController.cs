using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace TalkingAvatar
{
    /// <summary>
    /// Se crea solo al cargar la escena AR: coloca el avatar sobre el suelo detectado delante de la cámara,
    /// lo hace saludar y conversar por iniciativa propia, y conecta la barra de chat con Claude.
    /// </summary>
    public class TalkingAvatarController : MonoBehaviour
    {
        const string GreetingEvent =
            "[Evento: Acabas de aparecer en el espacio de la persona y te está mirando a través de su teléfono. " +
            "Salúdala, preséntate y empieza la conversación.]";

        const string IdleEvent =
            "[Evento: La persona lleva un rato sin decir nada. Retoma la conversación con algo breve y amable.]";

        AvatarChatSettings m_Settings;
        ClaudeChatClient m_Client;
        HumanAvatar m_Avatar;
        SpeechBubble m_Bubble;
        ChatInputUI m_UI;
        ARPlaneManager m_PlaneManager;

        bool m_Placed;
        bool m_PlacedOnFloor;
        float m_StartTime;
        float m_LastActivity;
        int m_NudgesInARow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var settings = AvatarChatSettings.Load();
            if (!settings.autoStartInScene || SceneManager.GetActiveScene().name != settings.sceneName)
                return;
            if (FindAnyObjectByType<TalkingAvatarController>() != null)
                return;
            new GameObject("Talking Avatar").AddComponent<TalkingAvatarController>();
        }

        void Start()
        {
            m_Settings = AvatarChatSettings.Load();
            m_Client = new ClaudeChatClient(m_Settings, AvatarChatSettings.LoadApiKey());
            m_PlaneManager = FindAnyObjectByType<ARPlaneManager>();

            m_Avatar = new GameObject(m_Settings.avatarName).AddComponent<HumanAvatar>();
            m_Avatar.transform.SetParent(transform, false);
            m_Bubble = SpeechBubble.Create(m_Avatar.transform, HumanAvatar.BubbleAnchor);
            m_Avatar.gameObject.SetActive(false);

            m_UI = ChatInputUI.Create(m_Settings.avatarName);
            m_UI.transform.SetParent(transform, false);
            m_UI.Submitted += OnUserMessage;
            m_UI.CallAvatarPressed += () =>
            {
                if (m_Placed)
                    PlaceInFront(m_Avatar.transform.position.y);
            };
            m_UI.Typing += () => m_LastActivity = Time.time;

            if (!m_Client.HasApiKey)
                Debug.LogWarning("[TalkingAvatar] Falta la API key: créala en Assets/TalkingAvatar/Resources/AnthropicApiKey.txt " +
                                 "(o define ANTHROPIC_API_KEY al abrir el Editor).");

            m_StartTime = Time.time;
        }

        void Update()
        {
            var cam = Camera.main;
            if (cam == null)
                return;

            if (!m_Placed || !m_PlacedOnFloor)
                TryPlace(cam);
            if (!m_Placed)
                return;

            m_Avatar.LookTarget = cam.transform;
            TurnTowards(cam.transform.position);

            m_Avatar.IsTalking = m_Bubble.IsRevealing;
            if (m_Bubble.IsRevealing || m_Client.IsBusy)
                m_LastActivity = Time.time;

            // Si no le contestas, retoma la charla por su cuenta (unas pocas veces seguidas como mucho).
            if (m_Client.HasApiKey && !m_UI.IsComposing && m_NudgesInARow < m_Settings.maxNudgesInARow &&
                Time.time - m_LastActivity > m_Settings.idleNudgeSeconds)
            {
                m_NudgesInARow++;
                Ask(IdleEvent);
            }
        }

        void TryPlace(Camera cam)
        {
            if (TryFindFloor(cam, out var floorY))
            {
                if (!m_Placed)
                {
                    PlaceInFront(floorY);
                    OnFirstPlaced();
                }
                else if (Mathf.Abs(m_Avatar.transform.position.y - floorY) > 0.05f)
                {
                    // Se colocó a altura estimada; ahora que hay suelo real, lo apoyamos en él.
                    var p = m_Avatar.transform.position;
                    m_Avatar.transform.position = new Vector3(p.x, floorY, p.z);
                }
                m_PlacedOnFloor = true;
            }
            else if (!m_Placed && Time.time - m_StartTime > m_Settings.placementTimeout)
            {
                // Sin suelo detectado: altura típica con el móvil en la mano.
                PlaceInFront(cam.transform.position.y - 1.45f);
                OnFirstPlaced();
            }
        }

        bool TryFindFloor(Camera cam, out float floorY)
        {
            floorY = 0f;
            if (m_PlaneManager == null)
                return false;

            var found = false;
            var camY = cam.transform.position.y;
            foreach (var plane in m_PlaneManager.trackables)
            {
                if (plane.alignment != PlaneAlignment.HorizontalUp)
                    continue;
                var drop = camY - plane.center.y;
                if (drop < 0.4f || drop > 2.2f)
                    continue;
                if (!found || plane.center.y < floorY)
                    floorY = plane.center.y;
                found = true;
            }
            return found;
        }

        void PlaceInFront(float floorY)
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            var forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f)
                forward = Vector3.ProjectOnPlane(cam.transform.up, Vector3.up);
            forward.Normalize();

            var position = cam.transform.position + forward * m_Settings.placementDistance;
            position.y = floorY;
            m_Avatar.transform.SetPositionAndRotation(position, Quaternion.LookRotation(-forward, Vector3.up));
            if (m_Placed)
                m_Avatar.PlayAppear();
        }

        void OnFirstPlaced()
        {
            m_Placed = true;
            m_Avatar.gameObject.SetActive(true);
            m_Avatar.PlayAppear();
            m_Avatar.Wave();
            m_LastActivity = Time.time;

            if (m_Client.HasApiKey)
                Ask(GreetingEvent);
            else
                m_Bubble.Say($"¡Hola! Soy {m_Settings.avatarName}. Para poder conversar contigo necesito una API key de " +
                             "Anthropic en Assets/TalkingAvatar/Resources/AnthropicApiKey.txt.");
        }

        void OnUserMessage(string text)
        {
            m_NudgesInARow = 0;
            m_LastActivity = Time.time;
            if (!m_Client.HasApiKey)
            {
                m_Bubble.Say("Me encantaría contestarte, pero aún no tengo API key. Ponla en AnthropicApiKey.txt.");
                return;
            }
            if (Random.value < 0.25f)
                m_Avatar.Wave();
            Ask(text);
        }

        void Ask(string message)
        {
            if (m_Client.IsBusy)
                return;

            m_Avatar.IsThinking = true;
            m_Bubble.ShowThinking();
            m_UI.SetBusy(true);
            StartCoroutine(m_Client.Send(message, reply =>
            {
                m_Avatar.IsThinking = false;
                m_UI.SetBusy(false);
                m_LastActivity = Time.time;
                m_Bubble.Say(reply.text);
            }));
        }

        void TurnTowards(Vector3 point)
        {
            var avatar = m_Avatar.transform;
            var flat = Vector3.ProjectOnPlane(point - avatar.position, Vector3.up);
            if (flat.sqrMagnitude < 1e-4f)
                return;
            var target = Quaternion.LookRotation(flat.normalized, Vector3.up);
            avatar.rotation = Quaternion.RotateTowards(avatar.rotation, target, 90f * Time.deltaTime);
        }
    }
}
