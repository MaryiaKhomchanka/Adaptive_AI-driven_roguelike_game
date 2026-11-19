using TMPro;
using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>
    /// Small pixel-style effects built from code (no prefabs): bursts of square bits, fading puddles/trails, rising
    /// damage numbers and a sprite flash. Bits are 1/8 unit squares so they match the retro light and 32-PPU art.
    /// </summary>
    public static class Fx
    {
        const int BitsOrder = DungeonBuilder.ActorSortingOrder + 2;
        static Sprite pixel;
        static TMP_FontAsset font;

        /// <summary>Menu/UI text is drawn this much smaller than a regular font size: Press Start 2P is a wide monospace face.</summary>
        public const float UiFontScale = 0.62f;

        /// <summary>The game's pixel font (Resources/Fonts/PressStart2P-Regular, SIL OFL), as a dynamic TMP asset.
        /// Null if the font file is missing, in which case TMP's default font is used.</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                if (font != null) return font;
                var source = Resources.Load<Font>("Fonts/PressStart2P-Regular");
                if (source != null) font = TMP_FontAsset.CreateFontAsset(source);
                return font;
            }
        }

        /// <summary>A 4x4 white sprite at 32 PPU = one 1/8-unit square. Also handy for placeholder art.</summary>
        public static Sprite Pixel
        {
            get
            {
                if (pixel != null) return pixel;
                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var white = new Color32[16];
                for (var i = 0; i < white.Length; i++) white[i] = Color.white;
                texture.SetPixels32(white);
                texture.Apply();
                pixel = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 32f);
                return pixel;
            }
        }

        public static void Burst(Vector3 position, Color color, int count = 8, float speed = 3f, float life = 0.45f)
        {
            for (var i = 0; i < count; i++)
            {
                var angle = Random.value * Mathf.PI * 2f;
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed * Random.Range(0.4f, 1f);
                Spawn(position, velocity, color, life * Random.Range(0.6f, 1f), Random.Range(1f, 2f), BitsOrder, drag: 4f);
            }
        }

        /// <summary>A single bit/puddle: moves with velocity, fades out and shrinks over its life, then destroys itself.</summary>
        public static FxParticle Spawn(Vector3 position, Vector2 velocity, Color color, float life, float scale, int sortingOrder,
            float drag = 0f, float startAlpha = 1f, Transform parent = null)
        {
            var go = new GameObject("Fx");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Pixel;
            renderer.sortingOrder = sortingOrder;
            PixelLit.World(renderer);

            var particle = go.AddComponent<FxParticle>();
            particle.Init(velocity, color, life, scale, drag, startAlpha);
            return particle;
        }

        /// <summary>Rising, fading world-space text (damage numbers).</summary>
        public static void Float(Vector3 position, string text, Color color)
        {
            var go = new GameObject("FloatingText");
            go.transform.position = position;

            var label = go.AddComponent<TextMeshPro>();
            label.text = text;
            if (Font != null) label.font = Font;
            label.fontSize = 2.4f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.sortingOrder = BitsOrder + 1;
            label.rectTransform.sizeDelta = new Vector2(4f, 1f);

            go.AddComponent<FxFloatingText>().label = label;
        }

        /// <summary>Tints every sprite under target for a moment, then restores what each one had.</summary>
        public static void Flash(GameObject target, Color tint, float duration = 0.1f)
        {
            var flash = target.GetComponent<FxFlash>() ?? target.AddComponent<FxFlash>();
            flash.Begin(tint, duration);
        }
    }
}
