using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Runtime
{
    /// <summary>
    /// Materials for the "Generation/Pixel Lit" shader (Resources/PixelLit): the stepped, world-pixel-snapped light.
    /// World = tiles, props, doors, player (always visible, just lit). Actor = enemies, loot, specials, portal:
    /// additionally faded out where the light does not reach, so they are cut off along the light/shadow edge.
    /// </summary>
    public static class PixelLit
    {
        static Material world;
        static Material actor;

        public static void World(Renderer renderer) => Apply(renderer, ref world, 0f);

        public static void World(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (r is SpriteRenderer || r is TilemapRenderer) World(r);
        }

        public static void Actor(GameObject root)
        {
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                Apply(sr, ref actor, 0.6f);
        }

        static void Apply(Renderer renderer, ref Material cache, float revealRange)
        {
            if (cache == null)
            {
                var shader = Resources.Load<Shader>("PixelLit");
                if (shader == null || !shader.isSupported)
                {
                    Debug.LogWarning("[PixelLit] shader missing or unsupported; keeping the default sprite material.");
                    return;
                }
                cache = new Material(shader) { name = "PixelLit (runtime)" };
                cache.SetFloat("_RevealRange", revealRange);
            }
            renderer.sharedMaterial = cache;
        }
    }
}
