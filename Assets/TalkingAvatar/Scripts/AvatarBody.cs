using UnityEngine;

namespace TalkingAvatar
{
    /// <summary>
    /// Base común de los avatares (humano, martín pescador…). El controlador solo habla con esta clase:
    /// le dice si está hablando o pensando, a quién mirar, y le pide saludar y aparecer.
    /// Incluye utilidades para construir cuerpos con primitivas.
    /// </summary>
    public abstract class AvatarBody : MonoBehaviour
    {
        /// <summary>Material URP Lit en Resources (lo crea el Editor). Garantiza que el shader entra en el build.</summary>
        public const string BaseMaterialResourcePath = "AvatarBaseMaterial";

        public bool IsTalking { get; set; }
        public bool IsThinking { get; set; }
        public Transform LookTarget { get; set; }

        /// <summary>True si el avatar se apoya en el suelo; false si flota a la altura de los ojos (p. ej. en una rama).</summary>
        public virtual bool PlacesOnFloor => true;

        /// <summary>Posición local de la burbuja de diálogo respecto al avatar.</summary>
        public abstract Vector3 BubbleAnchor { get; }

        /// <summary>Desplazamiento horizontal (m) de la cola de la burbuja vista desde la cámara; negativo = izquierda.</summary>
        public virtual float BubbleTailOffset => 0f;

        /// <summary>Escala de la burbuja (1 = unos 0,7 m de ancho).</summary>
        public virtual float BubbleScale => 1f;

        /// <summary>Construye el cuerpo. Se puede llamar varias veces; solo construye la primera.</summary>
        public abstract void Build();

        /// <summary>Gesto de saludo (mano, aleteo…).</summary>
        public abstract void Greet();

        /// <summary>Animación de aparición.</summary>
        public abstract void PlayAppear();

        /// <summary>Pose estática para capturas fuera de Play Mode.</summary>
        public abstract void PoseForPreview(bool greeting, float mouthOpen);

        Material m_BaseMaterial;

        protected static Transform Pivot(string name, Transform parent, Vector3 localPosition, Vector3 euler = default)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPosition;
            pivot.localEulerAngles = euler;
            return pivot;
        }

        protected static Transform Part(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
            Vector3 scale, Material material, Vector3 euler = default)
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

        /// <param name="texture">Textura opcional; el color la tiñe (usa blanco para verla tal cual).</param>
        protected Material MakeMaterial(Color color, float smoothness, Texture2D texture = null)
        {
            if (m_BaseMaterial == null)
            {
                // Material URP guardado en el proyecto: en el móvil, el material por defecto de las primitivas
                // usa un shader que URP no soporta y se ve magenta.
                m_BaseMaterial = Resources.Load<Material>(BaseMaterialResourcePath);
                if (m_BaseMaterial == null)
                {
                    Debug.LogWarning($"[TalkingAvatar] Falta Resources/{BaseMaterialResourcePath}.mat; " +
                                     "uso el material por defecto (en el móvil puede verse magenta).");
                    var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    m_BaseMaterial = probe.GetComponent<Renderer>().sharedMaterial;
                    DestroySafe(probe);
                }
            }

            var material = new Material(m_BaseMaterial) { name = "Avatar " + ColorUtility.ToHtmlStringRGB(color) };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            if (texture != null)
            {
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", texture);
            }
            return material;
        }

        protected static void DestroySafe(Object obj)
        {
            if (obj == null)
                return;
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        protected static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        /// <summary>Factor de suavizado independiente de los FPS.</summary>
        protected static float Smooth(float speed, float dt) => 1f - Mathf.Exp(-speed * dt);
    }
}
