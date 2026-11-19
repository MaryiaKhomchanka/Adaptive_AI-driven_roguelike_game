using UnityEngine;

namespace Generation.Runtime
{
    public sealed class FxFlash : MonoBehaviour
    {
        SpriteRenderer[] sprites;
        Color[] originals;
        float until;

        public void Begin(Color tint, float duration)
        {
            if (sprites == null)
            {
                sprites = GetComponentsInChildren<SpriteRenderer>();
                originals = new Color[sprites.Length];
                for (var i = 0; i < sprites.Length; i++) originals[i] = sprites[i].color;
            }
            else if (until <= 0f) // flash already over: remember what the sprites have now (another effect may have re-tinted them)
            {
                for (var i = 0; i < sprites.Length; i++) originals[i] = sprites[i].color;
            }

            foreach (var sprite in sprites) sprite.color = tint;
            until = Time.time + duration;
        }

        void Update()
        {
            if (until <= 0f || Time.time < until) return;
            for (var i = 0; i < sprites.Length; i++)
                if (sprites[i] != null) sprites[i].color = originals[i];
            until = 0f;
        }
    }
}
