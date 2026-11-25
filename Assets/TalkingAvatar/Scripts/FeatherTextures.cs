using UnityEngine;

namespace TalkingAvatar
{
    /// <summary>
    /// Texturas de plumaje generadas por código (sin archivos de imagen). Están pensadas para las esferas
    /// de Unity, cuya UV es tipo mapa: U da la vuelta alrededor y V va de abajo (0) a arriba (1).
    /// </summary>
    public static class FeatherTextures
    {
        const int Size = 128;

        /// <summary>Barras horizontales finas e irregulares (píleo y bigotera del martín pescador).</summary>
        public static Texture2D Barred(Color baseColor, Color barColor, float bars, float barWidth, int seed)
        {
            return Generate("Barred", seed, (u, v, rnd) =>
            {
                var wobble = 0.08f * (Mathf.PerlinNoise(u * 6f + rnd, v * 2f) - 0.5f);
                var phase = Mathf.Repeat((v + wobble) * bars, 1f);
                // Cada barra está partida en trocitos, como las puntas claras de las plumas.
                var broken = Mathf.PerlinNoise(u * 24f + rnd, Mathf.Floor((v + wobble) * bars) * 3.1f);
                var bar = phase < barWidth && broken > 0.42f ? 1f : 0f;
                return Shade(Color.Lerp(baseColor, barColor, bar), u, v, rnd, 0.12f);
            });
        }

        /// <summary>Motas claras redondeadas sobre fondo oscuro (alas).</summary>
        public static Texture2D Spotted(Color baseColor, Color spotColor, int cells, float spotRadius, int seed)
        {
            return Generate("Spotted", seed, (u, v, rnd) =>
            {
                var cu = u * cells;
                var cv = v * cells;
                var iu = Mathf.Floor(cu);
                var iv = Mathf.Floor(cv);
                // Un punto por celda, desplazado al azar dentro de ella.
                var ou = 0.25f + 0.5f * Hash(iu, iv, rnd);
                var ov = 0.25f + 0.5f * Hash(iv, iu, rnd + 7f);
                var d = new Vector2(cu - iu - ou, (cv - iv - ov) * 1.4f).magnitude;
                var hasSpot = Hash(iu + 13f, iv, rnd) > 0.35f;
                var spot = hasSpot ? 1f - Mathf.SmoothStep(spotRadius * 0.7f, spotRadius, d) : 0f;
                return Shade(Color.Lerp(baseColor, spotColor, spot), u, v, rnd, 0.15f);
            });
        }

        /// <summary>Vetas verticales suaves (pecho, lomo, cola) que imitan plumas finas.</summary>
        public static Texture2D Streaked(Color baseColor, Color streakColor, float streaks, float strength, int seed)
        {
            return Generate("Streaked", seed, (u, v, rnd) =>
            {
                var n = Mathf.PerlinNoise(u * streaks + rnd, v * 3f);
                var fine = Mathf.PerlinNoise(u * streaks * 3f + rnd, v * 8f);
                var t = Mathf.Clamp01((n * 0.7f + fine * 0.3f - 0.35f) * 1.6f) * strength;
                return Shade(Color.Lerp(baseColor, streakColor, t), u, v, rnd, 0.08f);
            });
        }

        /// <summary>Lomo brillante: degradado de azul eléctrico con reflejos claros y vetas.</summary>
        public static Texture2D Shimmer(Color deep, Color bright, int seed)
        {
            return Generate("Shimmer", seed, (u, v, rnd) =>
            {
                var glow = Mathf.PerlinNoise(u * 3f + rnd, v * 5f);
                var streak = Mathf.PerlinNoise(u * 30f + rnd, v * 4f);
                var c = Color.Lerp(deep, bright, Mathf.Clamp01(glow * 1.2f + (streak - 0.5f) * 0.6f));
                return Shade(c, u, v, rnd, 0.06f);
            });
        }

        /// <summary>Corteza: marrón con grietas a lo largo de la rama.</summary>
        public static Texture2D Bark(Color baseColor, Color crackColor, int seed)
        {
            return Generate("Bark", seed, (u, v, rnd) =>
            {
                // En el cilindro, V va a lo largo de la rama.
                var ridge = Mathf.Abs(Mathf.Sin((u * 14f + 0.6f * Mathf.PerlinNoise(v * 5f + rnd, u * 2f)) * Mathf.PI));
                var crack = 1f - Mathf.SmoothStep(0.05f, 0.25f, ridge);
                var knot = Mathf.PerlinNoise(u * 4f + rnd, v * 4f);
                var c = Color.Lerp(baseColor, crackColor, Mathf.Max(crack * 0.8f, (knot - 0.6f) * 1.5f));
                return Shade(c, u, v, rnd, 0.15f);
            });
        }

        /// <summary>Hoja: verde con nervio central y nervios laterales.</summary>
        public static Texture2D Leaf(Color baseColor, Color veinColor, int seed)
        {
            return Generate("Leaf", seed, (u, v, rnd) =>
            {
                var x = Mathf.Abs(Mathf.Repeat(u * 2f, 1f) - 0.5f);
                var mid = 1f - Mathf.SmoothStep(0.0f, 0.03f, Mathf.Abs(v - 0.5f));
                var side = 1f - Mathf.SmoothStep(0.0f, 0.04f, Mathf.Abs(Mathf.Repeat(x * 6f + Mathf.Abs(v - 0.5f) * 3f, 1f) - 0.5f) * 0.5f);
                var vein = Mathf.Max(mid, side * 0.5f);
                return Shade(Color.Lerp(baseColor, veinColor, vein * 0.7f), u, v, rnd, 0.1f);
            });
        }

        delegate Color Pixel(float u, float v, float random);

        static Texture2D Generate(string name, int seed, Pixel pixel)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true)
            {
                name = $"Feathers {name} {seed}",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 2,
            };
            var rnd = seed * 17.31f;
            var pixels = new Color[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var u = (x + 0.5f) / Size;
                    var v = (y + 0.5f) / Size;
                    pixels[y * Size + x] = pixel(u, v, rnd);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(true, true);
            return texture;
        }

        /// <summary>Variación sutil de luminosidad para que no se vea plano.</summary>
        static Color Shade(Color c, float u, float v, float rnd, float amount)
        {
            var n = Mathf.PerlinNoise(u * 40f + rnd * 0.5f, v * 40f) - 0.5f;
            var k = 1f + n * amount * 2f;
            return new Color(c.r * k, c.g * k, c.b * k, 1f);
        }

        static float Hash(float a, float b, float seed)
        {
            var h = Mathf.Sin(a * 127.1f + b * 311.7f + seed * 74.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }
    }
}
