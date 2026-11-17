using UnityEngine;

namespace TalkingAvatar
{
    /// <summary>
    /// Humano estilizado construido con primitivas (unos 1,65 m, pies en y = 0, mirando a +Z).
    /// Anima respiración, parpadeo, mirada hacia <see cref="LookTarget"/>, saludo con la mano,
    /// boca y gestos al hablar, y una pose de "pensando".
    /// </summary>
    public class HumanAvatar : MonoBehaviour
    {
        [Header("Colores")]
        public Color skinColor = new Color(0.87f, 0.67f, 0.53f);
        public Color hairColor = new Color(0.20f, 0.12f, 0.08f);
        public Color shirtColor = new Color(0.20f, 0.46f, 0.76f);
        public Color pantsColor = new Color(0.17f, 0.19f, 0.26f);
        public Color shoeColor = new Color(0.12f, 0.12f, 0.12f);
        public Color irisColor = new Color(0.14f, 0.10f, 0.07f);
        public Color lipColor = new Color(0.55f, 0.22f, 0.22f);

        public bool IsTalking { get; set; }
        public bool IsThinking { get; set; }
        public Transform LookTarget { get; set; }

        /// <summary>
        /// Posición local (respecto al avatar) donde va la burbuja: sobre la cabeza, desplazada hacia el lado
        /// izquierdo del avatar para que la mano derecha no la tape al saludar.
        /// </summary>
        public static readonly Vector3 BubbleAnchor = new Vector3(-0.2f, 2.02f, 0f);

        /// <summary>
        /// Desplazamiento horizontal (en metros, en el plano de la burbuja vista desde la cámara) de la cola,
        /// para que apunte a la cabeza. Negativo = hacia la izquierda de la pantalla.
        /// </summary>
        public const float BubbleTailOffset = -0.17f;

        Transform m_Body, m_Head, m_LeftShoulder, m_RightShoulder, m_LeftElbow, m_RightElbow;
        Transform m_LeftEye, m_RightEye, m_Mouth, m_LeftBrow, m_RightBrow;
        Vector3 m_MouthScale, m_LeftBrowPos, m_RightBrowPos, m_BodyPos;
        Material m_BaseMaterial;
        bool m_Built;

        Quaternion m_HeadRotation = Quaternion.identity;
        float m_MouthOpen, m_Expression, m_Gesture, m_ThinkBlend;
        float m_NextBlink = 2f, m_BlinkTime = -1f;
        float m_WaveTime = -1f;
        float m_Appear = 1f;

        const float WaveDuration = 2.4f;
        const float BlinkDuration = 0.14f;

        void Awake() => Build();

        public void Wave() => m_WaveTime = 0f;

        public void PlayAppear()
        {
            m_Appear = 0f;
            transform.localScale = Vector3.one * 0.001f;
        }

        public void Build()
        {
            if (m_Built)
                return;
            m_Built = true;

            var skin = MakeMaterial(skinColor, 0.35f);
            var hair = MakeMaterial(hairColor, 0.45f);
            var shirt = MakeMaterial(shirtColor, 0.15f);
            var pants = MakeMaterial(pantsColor, 0.15f);
            var shoes = MakeMaterial(shoeColor, 0.5f);
            var white = MakeMaterial(new Color(0.97f, 0.97f, 0.97f), 0.7f);
            var iris = MakeMaterial(irisColor, 0.9f);
            var lips = MakeMaterial(lipColor, 0.4f);

            var root = transform;

            // Piernas y zapatos (no se mueven con la respiración).
            Part(PrimitiveType.Capsule, "Leg L", root, new Vector3(-0.09f, 0.47f, 0f), new Vector3(0.15f, 0.45f, 0.15f), pants);
            Part(PrimitiveType.Capsule, "Leg R", root, new Vector3(0.09f, 0.47f, 0f), new Vector3(0.15f, 0.45f, 0.15f), pants);
            Part(PrimitiveType.Cube, "Shoe L", root, new Vector3(-0.09f, 0.04f, 0.035f), new Vector3(0.13f, 0.08f, 0.26f), shoes);
            Part(PrimitiveType.Cube, "Shoe R", root, new Vector3(0.09f, 0.04f, 0.035f), new Vector3(0.13f, 0.08f, 0.26f), shoes);

            // Tronco.
            m_Body = Pivot("Body", root, new Vector3(0f, 0.9f, 0f));
            m_BodyPos = m_Body.localPosition;
            Part(PrimitiveType.Sphere, "Pelvis", m_Body, new Vector3(0f, 0.04f, 0f), new Vector3(0.36f, 0.2f, 0.24f), pants);
            Part(PrimitiveType.Capsule, "Torso", m_Body, new Vector3(0f, 0.33f, 0f), new Vector3(0.38f, 0.34f, 0.24f), shirt);
            Part(PrimitiveType.Cylinder, "Neck", m_Body, new Vector3(0f, 0.68f, 0f), new Vector3(0.085f, 0.06f, 0.085f), skin);

            // Cabeza.
            m_Head = Pivot("Head", m_Body, new Vector3(0f, 0.72f, 0f));
            Part(PrimitiveType.Sphere, "Face", m_Head, new Vector3(0f, 0.12f, 0f), new Vector3(0.21f, 0.25f, 0.22f), skin);
            Part(PrimitiveType.Sphere, "Hair", m_Head, new Vector3(0f, 0.165f, -0.02f), new Vector3(0.235f, 0.22f, 0.235f), hair);
            Part(PrimitiveType.Sphere, "Hair Back", m_Head, new Vector3(0f, 0.08f, -0.06f), new Vector3(0.22f, 0.24f, 0.16f), hair);
            Part(PrimitiveType.Sphere, "Nose", m_Head, new Vector3(0f, 0.1f, 0.112f), new Vector3(0.028f, 0.04f, 0.035f), skin);

            m_LeftEye = Eye("Eye L", new Vector3(-0.042f, 0.135f, 0.093f), white, iris);
            m_RightEye = Eye("Eye R", new Vector3(0.042f, 0.135f, 0.093f), white, iris);

            m_LeftBrow = Part(PrimitiveType.Cube, "Brow L", m_Head, new Vector3(-0.042f, 0.175f, 0.1f), new Vector3(0.045f, 0.009f, 0.012f), hair, new Vector3(0f, 0f, -6f));
            m_RightBrow = Part(PrimitiveType.Cube, "Brow R", m_Head, new Vector3(0.042f, 0.175f, 0.1f), new Vector3(0.045f, 0.009f, 0.012f), hair, new Vector3(0f, 0f, 6f));
            m_LeftBrowPos = m_LeftBrow.localPosition;
            m_RightBrowPos = m_RightBrow.localPosition;

            m_Mouth = Part(PrimitiveType.Sphere, "Mouth", m_Head, new Vector3(0f, 0.058f, 0.1f), new Vector3(0.055f, 0.012f, 0.02f), lips);
            m_MouthScale = m_Mouth.localScale;

            // Brazos: hombro → codo → mano.
            m_LeftShoulder = Arm("L", -1f, shirt, skin, out m_LeftElbow);
            m_RightShoulder = Arm("R", 1f, shirt, skin, out m_RightElbow);

            ApplyPose(0f, 0f, 0f);
        }

        /// <summary>Fija una pose estática (para capturas fuera de Play Mode).</summary>
        public void PoseForPreview(bool waving, float mouthOpen)
        {
            Build();
            m_MouthOpen = mouthOpen;
            m_Expression = 1f;
            ApplyPose(waving ? 1f : 0f, waving ? 0.6f : 0f, 0f);
        }

        void Update()
        {
            var dt = Time.deltaTime;
            var t = Time.time;

            // Aparecer con un pequeño rebote.
            if (m_Appear < 1f)
            {
                m_Appear = Mathf.Min(1f, m_Appear + dt / 0.6f);
                transform.localScale = Vector3.one * EaseOutBack(m_Appear);
            }

            // Respiración y balanceo leve.
            m_Body.localPosition = m_BodyPos + new Vector3(0f, 0.006f * Mathf.Sin(t * 1.7f), 0f);
            m_Body.localRotation = Quaternion.Euler(0f, 0f, 1.2f * Mathf.Sin(t * 0.6f));

            // Mirada.
            var tilt = IsThinking ? 12f : 0f;
            var target = Quaternion.Euler(IsThinking ? -10f : 0f, IsThinking ? 15f : 0f, tilt);
            if (LookTarget != null && !IsThinking)
            {
                var local = m_Body.InverseTransformDirection(LookTarget.position - m_Head.position);
                var yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -55f, 55f);
                var pitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -25f, 25f);
                target = Quaternion.Euler(pitch, yaw, 0f);
            }
            m_HeadRotation = Quaternion.Slerp(m_HeadRotation, target, 1f - Mathf.Exp(-6f * dt));
            m_Head.localRotation = m_HeadRotation;

            // Parpadeo.
            m_NextBlink -= dt;
            if (m_NextBlink <= 0f)
            {
                m_BlinkTime = 0f;
                m_NextBlink = Random.Range(2f, 5f);
            }
            var eyeOpen = 1f;
            if (m_BlinkTime >= 0f)
            {
                m_BlinkTime += dt;
                eyeOpen = 1f - 0.9f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(m_BlinkTime / BlinkDuration));
                if (m_BlinkTime >= BlinkDuration)
                    m_BlinkTime = -1f;
            }
            m_LeftEye.localScale = m_RightEye.localScale = new Vector3(1f, eyeOpen, 1f);

            // Boca, cejas y gestos.
            var mouthTarget = IsTalking ? 0.2f + 0.8f * Mathf.PerlinNoise(t * 9f, 0.3f) : 0f;
            m_MouthOpen = Mathf.Lerp(m_MouthOpen, mouthTarget, 1f - Mathf.Exp(-18f * dt));
            var expressive = IsTalking || m_WaveTime >= 0f;
            m_Expression = Mathf.MoveTowards(m_Expression, expressive ? 1f : 0f, dt * 3f);
            m_Gesture = Mathf.MoveTowards(m_Gesture, IsTalking ? 1f : 0f, dt * 2f);
            m_ThinkBlend = Mathf.MoveTowards(m_ThinkBlend, IsThinking ? 1f : 0f, dt * 2.5f);

            var wave = 0f;
            var waveSwing = 0f;
            if (m_WaveTime >= 0f)
            {
                m_WaveTime += dt;
                var p = m_WaveTime / WaveDuration;
                wave = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p * 5f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - p) * 5f));
                waveSwing = Mathf.Sin(m_WaveTime * 10f);
                if (p >= 1f)
                    m_WaveTime = -1f;
            }

            ApplyPose(wave, waveSwing, t);
        }

        void ApplyPose(float wave, float waveSwing, float t)
        {
            // Boca y cejas.
            m_Mouth.localScale = m_MouthScale + new Vector3(-0.01f * m_MouthOpen, 0.032f * m_MouthOpen, 0.004f * m_MouthOpen);
            var brow = new Vector3(0f, 0.008f * m_Expression, 0f);
            m_LeftBrow.localPosition = m_LeftBrowPos + brow;
            m_RightBrow.localPosition = m_RightBrowPos + brow;

            // Gestos suaves al hablar.
            var gL = m_Gesture * Mathf.PerlinNoise(t * 1.3f, 1.7f);
            var gR = m_Gesture * Mathf.PerlinNoise(t * 1.1f, 5.3f);

            // Brazo izquierdo (lado -X): rotar en -Z lo abre hacia fuera.
            m_LeftShoulder.localRotation = Quaternion.Euler(-10f * gL, 0f, -7f - 8f * gL);
            m_LeftElbow.localRotation = Quaternion.Euler(-12f - 45f * gL, 0f, 0f);

            // Brazo derecho: reposo / gesto / "pensando" (mano hacia la barbilla) / saludo.
            var restShoulder = Quaternion.Euler(-10f * gR, 0f, 7f + 8f * gR);
            var restElbow = Quaternion.Euler(-12f - 45f * gR, 0f, 0f);
            var thinkShoulder = Quaternion.Euler(-35f, 0f, -12f);
            var thinkElbow = Quaternion.Euler(-130f, 0f, 0f);
            var shoulder = Quaternion.Slerp(restShoulder, thinkShoulder, m_ThinkBlend * (1f - wave));
            var elbow = Quaternion.Slerp(restElbow, thinkElbow, m_ThinkBlend * (1f - wave));

            var waveShoulder = Quaternion.Euler(0f, 0f, 150f);
            var waveElbow = Quaternion.Euler(0f, 0f, 20f + 25f * waveSwing);
            m_RightShoulder.localRotation = Quaternion.Slerp(shoulder, waveShoulder, wave);
            m_RightElbow.localRotation = Quaternion.Slerp(elbow, waveElbow, wave);
        }

        Transform Arm(string side, float sign, Material sleeve, Material skin, out Transform elbow)
        {
            var shoulder = Pivot("Shoulder " + side, m_Body, new Vector3(0.22f * sign, 0.6f, 0f));
            Part(PrimitiveType.Capsule, "Upper Arm " + side, shoulder, new Vector3(0f, -0.15f, 0f), new Vector3(0.09f, 0.16f, 0.09f), sleeve);
            elbow = Pivot("Elbow " + side, shoulder, new Vector3(0f, -0.3f, 0f));
            Part(PrimitiveType.Capsule, "Forearm " + side, elbow, new Vector3(0f, -0.13f, 0f), new Vector3(0.075f, 0.14f, 0.075f), skin);
            Part(PrimitiveType.Sphere, "Hand " + side, elbow, new Vector3(0f, -0.29f, 0f), new Vector3(0.075f, 0.095f, 0.05f), skin);
            return shoulder;
        }

        Transform Eye(string name, Vector3 position, Material white, Material iris)
        {
            var eye = Pivot(name, m_Head, position);
            Part(PrimitiveType.Sphere, "White", eye, Vector3.zero, new Vector3(0.042f, 0.042f, 0.03f), white);
            Part(PrimitiveType.Sphere, "Iris", eye, new Vector3(0f, 0f, 0.013f), new Vector3(0.024f, 0.026f, 0.012f), iris);
            return eye;
        }

        static Transform Pivot(string name, Transform parent, Vector3 localPosition)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPosition;
            return pivot;
        }

        static Transform Part(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 scale,
            Material material, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            DestroySafe(go.GetComponent<Collider>());
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            t.localEulerAngles = euler;
            t.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return t;
        }

        Material MakeMaterial(Color color, float smoothness)
        {
            if (m_BaseMaterial == null)
            {
                // El material por defecto de una primitiva es el Lit del pipeline activo (URP aquí),
                // así no dependemos de Shader.Find, que puede fallar si el shader se excluye del build.
                var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                m_BaseMaterial = probe.GetComponent<Renderer>().sharedMaterial;
                DestroySafe(probe);
            }

            var material = new Material(m_BaseMaterial) { name = "Avatar " + ColorUtility.ToHtmlStringRGB(color) };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        static void DestroySafe(Object obj)
        {
            if (obj == null)
                return;
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
