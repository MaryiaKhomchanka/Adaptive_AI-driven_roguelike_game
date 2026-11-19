using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace Generation.Runtime
{
    /// <summary>
    /// Light-blocking boxes for a room's wall tilemap: wall cells are merged into rectangles and each becomes a
    /// ShadowCaster2D. URP only exposes the caster's outline to its editor tools, so the path is set by reflection.
    /// shortcut: tied to URP 17's private m_ShapePath/m_ShapePathHash; if a URP update renames them this logs a
    /// warning and rooms simply have no wall shadows - switch to the public API once URP offers one.
    /// </summary>
    public static class WallShadows
    {
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        static readonly FieldInfo PathField = typeof(ShadowCaster2D).GetField("m_ShapePath", Private);
        static readonly FieldInfo HashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash", Private);

        public static void Build(Tilemap walls)
        {
            if (PathField == null || HashField == null)
            {
                Debug.LogWarning("[WallShadows] ShadowCaster2D fields not found (URP changed?); wall shadows disabled.");
                return;
            }

            foreach (var rect in MergeIntoRects(walls))
                AddBox(walls.transform, rect);
        }

        /// <summary>Greedy merge: horizontal runs per row, then identical runs on consecutive rows are stacked.</summary>
        static List<RectInt> MergeIntoRects(Tilemap walls)
        {
            var rects = new List<RectInt>();
            var bounds = walls.cellBounds;
            var open = new Dictionary<(int x, int w), int>(); // runs in the previous row -> index in rects

            for (var y = bounds.yMin; y < bounds.yMax; y++)
            {
                var next = new Dictionary<(int x, int w), int>();
                var runStart = int.MinValue;
                for (var x = bounds.xMin; x <= bounds.xMax; x++)
                {
                    var solid = x < bounds.xMax && walls.HasTile(new Vector3Int(x, y, 0));
                    if (solid)
                    {
                        if (runStart == int.MinValue) runStart = x;
                        continue;
                    }
                    if (runStart == int.MinValue) continue;

                    var key = (runStart, x - runStart);
                    if (open.TryGetValue(key, out var index))
                    {
                        var rect = rects[index];
                        rect.height++;
                        rects[index] = rect;
                        next[key] = index;
                    }
                    else
                    {
                        rects.Add(new RectInt(key.Item1, y, key.Item2, 1));
                        next[key] = rects.Count - 1;
                    }
                    runStart = int.MinValue;
                }
                open = next;
            }
            return rects;
        }

        /// <summary>A light-blocking box covering whole grid cells; parent's local space must equal world space.
        /// Props use this with their collider footprint, not their art, so taller art can overhang without shading its top row.</summary>
        public static void AddBox(Transform parent, RectInt rect)
        {
            if (PathField == null || HashField == null) return;

            var go = new GameObject("WallShadow");
            go.transform.SetParent(parent, false); // tilemap cells are 1 unit, so cell coords are the tilemap's local coords

            var caster = go.AddComponent<ShadowCaster2D>();
            caster.useRendererSilhouette = false;
            caster.selfShadows = false;

            var path = new[]
            {
                new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMin, rect.yMax),
                new Vector3(rect.xMax, rect.yMax), new Vector3(rect.xMax, rect.yMin),
            };
            PathField.SetValue(caster, path);

            // any value different from the caster's initial hash makes it rebuild its mesh from the new path
            var hash = unchecked(rect.x * 73856093 ^ rect.y * 19349663 ^ rect.width * 83492791 ^ rect.height * 2654435);
            HashField.SetValue(caster, hash == 0 ? 1 : hash);
        }
    }
}
