using UnityEngine;

namespace TalkingAvatar
{
    /// <summary>
    /// Martín pescador común (Alcedo atthis) estilizado, hecho con primitivas y posado en una ramita.
    /// A tamaño real mide unos 12 cm de alto posado (17 cm de pico a cola); <see cref="Size"/> lo multiplica.
    /// Mira hacia +Z con las patas en y = 0. Anima: respiración, parpadeo, vistazos rápidos de pájaro,
    /// cabeza hacia la cámara, pico al hablar, aleteo al saludar, golpes de cola y cabeza ladeada al pensar.
    /// </summary>
    public class KingfisherAvatar : AvatarBody
    {
        [Header("Colores (plumaje del martín pescador común)")]
        public Color backColor = new Color(0.10f, 0.78f, 0.95f);   // franja del lomo, azul eléctrico
        public Color wingColor = new Color(0.06f, 0.45f, 0.55f);   // alas verdeazuladas
        public Color crownColor = new Color(0.07f, 0.42f, 0.62f);  // píleo y bigotera
        public Color tailColor = new Color(0.05f, 0.30f, 0.55f);
        public Color spotColor = new Color(0.45f, 0.90f, 1.00f);   // motas claras de las alas
        public Color bellyColor = new Color(0.93f, 0.48f, 0.16f);  // pecho y mejillas naranja
        public Color throatColor = new Color(0.97f, 0.96f, 0.92f); // garganta y mancha del cuello
        public Color beakColor = new Color(0.08f, 0.07f, 0.07f);
        public Color feetColor = new Color(0.90f, 0.28f, 0.20f);
        public Color barkColor = new Color(0.38f, 0.26f, 0.17f);
        public Color leafColor = new Color(0.30f, 0.55f, 0.22f);

        float m_Size = 1.5f;

        /// <summary>Multiplicador del tamaño real.</summary>
        public float Size
        {
            get => m_Size;
            set
            {
                m_Size = Mathf.Max(0.1f, value);
                if (m_Model != null)
                    m_Model.localScale = Vector3.one * m_Size;
            }
        }

        public override bool PlacesOnFloor => false;
        public override Vector3 BubbleAnchor => new Vector3(0f, 0.14f * m_Size + 0.02f, 0f);
        public override float BubbleScale => 0.45f;

        Transform m_Model, m_Body, m_Head, m_LeftWing, m_RightWing, m_Tail, m_UpperBeak, m_LowerBeak, m_LeftEye, m_RightEye;
        Vector3 m_BodyPos;
        bool m_Built;

        Quaternion m_HeadRotation = Quaternion.identity;
        Vector2 m_Glance;
        float m_EyeSide = 1f;
        float m_NextGlance = 1.5f;
        float m_BeakOpen, m_ThinkBlend, m_TalkBlend;
        float m_NextBlink = 2f, m_BlinkTime = -1f;
        float m_NextFlick = 4f, m_FlickTime = -1f;
        float m_FlapTime = -1f;
        float m_Appear = 1f;

        const float BodyLean = 12f;

        // Los pájaros te miran con un ojo, de lado: así además se ve el perfil del pico.
        const float SideEyeAngle = 40f;
        const float SideEyeAngleTalking = 25f;
        const float FlapDuration = 1.6f;
        const float FlickDuration = 0.3f;
        const float BlinkDuration = 0.12f;

        public override void Greet() => m_FlapTime = 0f;

        public override void PlayAppear()
        {
            m_Appear = 0f;
            if (m_Model != null)
                m_Model.localScale = Vector3.one * 0.001f;
        }

        public override void Build()
        {
            if (m_Built)
                return;
            m_Built = true;

            var back = MakeMaterial(backColor, 0.55f);
            var wing = MakeMaterial(wingColor, 0.5f);
            var crown = MakeMaterial(crownColor, 0.5f);
            var tail = MakeMaterial(tailColor, 0.5f);
            var spots = MakeMaterial(spotColor, 0.5f);
            var belly = MakeMaterial(bellyColor, 0.3f);
            var throat = MakeMaterial(throatColor, 0.3f);
            var beak = MakeMaterial(beakColor, 0.6f);
            var eye = MakeMaterial(new Color(0.02f, 0.02f, 0.02f), 0.95f);
            var catchlight = MakeMaterial(Color.white, 0.9f);
            var feet = MakeMaterial(feetColor, 0.3f);
            var bark = MakeMaterial(barkColor, 0.1f);
            var leaf = MakeMaterial(leafColor, 0.3f);

            // Todo el modelo cuelga de "Model" para poder escalarlo sin tocar la burbuja.
            m_Model = Pivot("Model", transform, Vector3.zero);
            m_Model.localScale = Vector3.one * m_Size;

            // Rama con una ramita y un par de hojas.
            Part(PrimitiveType.Cylinder, "Branch", m_Model, new Vector3(0f, -0.012f, 0f), new Vector3(0.024f, 0.17f, 0.024f), bark, new Vector3(0f, 0f, 90f));
            Part(PrimitiveType.Cylinder, "Twig", m_Model, new Vector3(0.1f, 0.018f, 0f), new Vector3(0.011f, 0.045f, 0.011f), bark, new Vector3(0f, 0f, -40f));
            Part(PrimitiveType.Sphere, "Leaf A", m_Model, new Vector3(0.128f, 0.055f, 0f), new Vector3(0.036f, 0.008f, 0.02f), leaf, new Vector3(0f, 0f, -30f));
            Part(PrimitiveType.Sphere, "Leaf B", m_Model, new Vector3(-0.14f, 0.0f, 0.012f), new Vector3(0.032f, 0.007f, 0.018f), leaf, new Vector3(0f, 30f, 15f));

            // Patas rojas agarradas a la rama.
            Part(PrimitiveType.Sphere, "Foot L", m_Model, new Vector3(-0.012f, 0.004f, 0.006f), new Vector3(0.012f, 0.009f, 0.022f), feet);
            Part(PrimitiveType.Sphere, "Foot R", m_Model, new Vector3(0.012f, 0.004f, 0.006f), new Vector3(0.012f, 0.009f, 0.022f), feet);

            // Cuerpo: pecho naranja delante, lomo y alas azules detrás, algo inclinado hacia delante.
            m_Body = Pivot("Body", m_Model, new Vector3(0f, 0.012f, 0f), new Vector3(BodyLean, 0f, 0f));
            m_BodyPos = m_Body.localPosition;
            Part(PrimitiveType.Sphere, "Belly", m_Body, new Vector3(0f, 0.034f, 0.006f), new Vector3(0.046f, 0.068f, 0.046f), belly);
            Part(PrimitiveType.Sphere, "Back", m_Body, new Vector3(0f, 0.038f, -0.006f), new Vector3(0.05f, 0.072f, 0.044f), wing);
            Part(PrimitiveType.Sphere, "Back Stripe", m_Body, new Vector3(0f, 0.04f, -0.024f), new Vector3(0.016f, 0.06f, 0.01f), back);

            m_Tail = Pivot("Tail", m_Body, new Vector3(0f, 0.006f, -0.016f));
            Part(PrimitiveType.Sphere, "Tail Feathers", m_Tail, new Vector3(0f, -0.018f, -0.007f), new Vector3(0.017f, 0.038f, 0.007f), tail, new Vector3(20f, 0f, 0f));

            m_LeftWing = Wing("L", -1f, wing, spots);
            m_RightWing = Wing("R", 1f, wing, spots);

            // Cabeza grande con el pico en daga.
            m_Head = Pivot("Head", m_Body, new Vector3(0f, 0.074f, 0.008f));
            Part(PrimitiveType.Sphere, "Crown", m_Head, new Vector3(0f, 0.012f, 0.004f), new Vector3(0.042f, 0.04f, 0.044f), crown);
            Part(PrimitiveType.Sphere, "Throat", m_Head, new Vector3(0f, -0.004f, 0.015f), new Vector3(0.026f, 0.02f, 0.022f), throat);
            for (var s = -1; s <= 1; s += 2)
            {
                var side = s < 0 ? "L" : "R";
                Part(PrimitiveType.Sphere, "Cheek " + side, m_Head, new Vector3(0.017f * s, 0.008f, -0.002f), new Vector3(0.012f, 0.016f, 0.022f), belly);
                Part(PrimitiveType.Sphere, "Lore " + side, m_Head, new Vector3(0.008f * s, 0.011f, 0.021f), new Vector3(0.005f, 0.005f, 0.007f), belly);
                Part(PrimitiveType.Sphere, "Moustache " + side, m_Head, new Vector3(0.016f * s, 0.001f, 0.009f), new Vector3(0.009f, 0.007f, 0.016f), crown);
                Part(PrimitiveType.Sphere, "Neck Patch " + side, m_Head, new Vector3(0.016f * s, -0.004f, -0.012f), new Vector3(0.01f, 0.014f, 0.014f), throat);
            }

            m_LeftEye = Eye("Eye L", -1f, eye, catchlight);
            m_RightEye = Eye("Eye R", 1f, eye, catchlight);

            m_UpperBeak = Pivot("Upper Beak", m_Head, new Vector3(0f, 0.009f, 0.02f));
            Part(PrimitiveType.Capsule, "Upper Mandible", m_UpperBeak, new Vector3(0f, 0.0015f, 0.022f), new Vector3(0.009f, 0.022f, 0.0055f), beak, new Vector3(90f, 0f, 0f));
            m_LowerBeak = Pivot("Lower Beak", m_Head, new Vector3(0f, 0.009f, 0.02f));
            Part(PrimitiveType.Capsule, "Lower Mandible", m_LowerBeak, new Vector3(0f, -0.002f, 0.02f), new Vector3(0.008f, 0.02f, 0.0045f), beak, new Vector3(90f, 0f, 0f));

            m_HeadRotation = Quaternion.Euler(-BodyLean, 0f, 0f);
            m_Head.localRotation = m_HeadRotation;
        }

        public override void PoseForPreview(bool greeting, float mouthOpen)
        {
            Build();
            var wingAngle = greeting ? 55f : 3f;
            m_LeftWing.localRotation = Quaternion.Euler(0f, 0f, -wingAngle);
            m_RightWing.localRotation = Quaternion.Euler(0f, 0f, wingAngle);
            m_LowerBeak.localRotation = Quaternion.Euler(18f * mouthOpen, 0f, 0f);
            m_UpperBeak.localRotation = Quaternion.Euler(-4f * mouthOpen, 0f, 0f);
            m_Head.localRotation = Quaternion.Euler(-BodyLean - 5f, -SideEyeAngle, 8f);
        }

        void Update()
        {
            if (!m_Built)
                return;

            var dt = Time.deltaTime;
            var t = Time.time;

            // Aparecer con un pequeño rebote.
            if (m_Appear < 1f)
            {
                m_Appear = Mathf.Min(1f, m_Appear + dt / 0.5f);
                m_Model.localScale = Vector3.one * (m_Size * EaseOutBack(m_Appear));
            }

            m_TalkBlend = Mathf.MoveTowards(m_TalkBlend, IsTalking ? 1f : 0f, dt * 4f);
            m_ThinkBlend = Mathf.MoveTowards(m_ThinkBlend, IsThinking ? 1f : 0f, dt * 3f);

            // Aleteo de saludo (con un saltito del cuerpo).
            var flap = 0f;
            var flapBeat = 0f;
            if (m_FlapTime >= 0f)
            {
                m_FlapTime += dt;
                var p = m_FlapTime / FlapDuration;
                flap = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p * 6f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - p) * 4f));
                flapBeat = Mathf.Abs(Mathf.Sin(m_FlapTime * 22f));
                if (p >= 1f)
                    m_FlapTime = -1f;
            }
            var wingAngle = 3f + flap * (15f + 55f * flapBeat);
            m_LeftWing.localRotation = Quaternion.Euler(0f, 0f, -wingAngle);
            m_RightWing.localRotation = Quaternion.Euler(0f, 0f, wingAngle);

            // Respiración, más el saltito al aletear.
            var breathe = 0.0012f * Mathf.Sin(t * 2.4f);
            m_Body.localPosition = m_BodyPos + new Vector3(0f, breathe + 0.006f * flap * flapBeat, 0f);

            // Golpe de cola de vez en cuando.
            m_NextFlick -= dt;
            if (m_NextFlick <= 0f)
            {
                m_FlickTime = 0f;
                m_NextFlick = Random.Range(3f, 7f);
            }
            var flick = 0f;
            if (m_FlickTime >= 0f)
            {
                m_FlickTime += dt;
                flick = Mathf.Sin(Mathf.PI * Mathf.Clamp01(m_FlickTime / FlickDuration));
                if (m_FlickTime >= FlickDuration)
                    m_FlickTime = -1f;
            }
            m_Tail.localRotation = Quaternion.Euler(-25f * flick, 0f, 0f);

            // Cabeza: mira a la cámara con vistazos rápidos, cabecea al hablar y se ladea al pensar.
            m_NextGlance -= dt;
            if (m_NextGlance <= 0f)
            {
                m_Glance = IsTalking || Random.value < 0.5f
                    ? Vector2.zero
                    : new Vector2(Random.Range(-35f, 35f), Random.Range(-12f, 8f));
                if (Random.value < 0.35f)
                    m_EyeSide = -m_EyeSide;
                m_NextGlance = Random.Range(1.2f, 3.5f);
            }

            var yaw = m_Glance.x + m_EyeSide * Mathf.Lerp(SideEyeAngle, SideEyeAngleTalking, m_TalkBlend);
            var pitch = m_Glance.y - BodyLean;
            if (LookTarget != null)
            {
                var local = m_Body.InverseTransformDirection(LookTarget.position - m_Head.position);
                yaw += Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -80f, 80f);
                pitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -45f, 40f) + m_Glance.y;
            }
            pitch += 4f * m_TalkBlend * Mathf.Sin(t * 7f);
            var lookRotation = Quaternion.Euler(pitch, yaw, 0f);
            var thinkRotation = Quaternion.Euler(-BodyLean - 15f, 30f, 28f);
            var target = Quaternion.Slerp(lookRotation, thinkRotation, m_ThinkBlend);
            m_HeadRotation = Quaternion.Slerp(m_HeadRotation, target, Smooth(14f, dt));
            m_Head.localRotation = m_HeadRotation;

            // Pico: se abre y cierra al hablar.
            var beakTarget = IsTalking ? Mathf.PerlinNoise(t * 10f, 0.7f) : 0f;
            m_BeakOpen = Mathf.Lerp(m_BeakOpen, beakTarget, Smooth(20f, dt));
            m_LowerBeak.localRotation = Quaternion.Euler(18f * m_BeakOpen, 0f, 0f);
            m_UpperBeak.localRotation = Quaternion.Euler(-4f * m_BeakOpen, 0f, 0f);

            // Parpadeo.
            m_NextBlink -= dt;
            if (m_NextBlink <= 0f)
            {
                m_BlinkTime = 0f;
                m_NextBlink = Random.Range(2f, 6f);
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
        }

        Transform Wing(string side, float sign, Material feathers, Material spots)
        {
            var wing = Pivot("Wing " + side, m_Body, new Vector3(0.022f * sign, 0.055f, -0.004f));
            Part(PrimitiveType.Sphere, "Wing Feathers " + side, wing, new Vector3(0f, -0.026f, -0.004f), new Vector3(0.012f, 0.058f, 0.03f), feathers, new Vector3(12f, 0f, 0f));
            for (var i = 0; i < 3; i++)
                Part(PrimitiveType.Sphere, $"Wing Spot {side}{i}", wing, new Vector3(0.0055f * sign, -0.01f - 0.012f * i, 0.002f - 0.003f * i), Vector3.one * 0.005f, spots);
            return wing;
        }

        Transform Eye(string name, float sign, Material eye, Material catchlight)
        {
            var pivot = Pivot(name, m_Head, new Vector3(0.0175f * sign, 0.015f, 0.009f));
            Part(PrimitiveType.Sphere, "Eyeball", pivot, Vector3.zero, Vector3.one * 0.01f, eye);
            Part(PrimitiveType.Sphere, "Catchlight", pivot, new Vector3(0.004f * sign, 0.0018f, 0.0025f), Vector3.one * 0.0022f, catchlight);
            return pivot;
        }
    }
}
