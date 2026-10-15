using Generation.Runtime;
using UnityEditor;
using UnityEngine;

namespace Generation.Editor
{
    /// <summary>Colored-box prop prefabs standing in for real art (spec 13): obstacles get a full-footprint
    /// BoxCollider2D, solid decor the same for now (base-only colliders come with real, non-square art later).</summary>
    static class PlaceholderProps
    {
        public struct Result
        {
            public GameObject Obstacle1x1, Obstacle2x2, Obstacle3x1, Obstacle1x3;
            public GameObject SolidDecor1x1, SolidDecor2x2;
        }

        public static Result CreateAll(string folder, PlaceholderSprites.Result sprites)
        {
            return new Result
            {
                Obstacle1x1 = CreatePrefab(folder, "Obstacle_1x1", sprites.Obstacle, 1, 1, hasCollider: true),
                Obstacle2x2 = CreatePrefab(folder, "Obstacle_2x2", sprites.Obstacle, 2, 2, hasCollider: true),
                Obstacle3x1 = CreatePrefab(folder, "Obstacle_3x1", sprites.Obstacle, 3, 1, hasCollider: true),
                Obstacle1x3 = CreatePrefab(folder, "Obstacle_1x3", sprites.Obstacle, 1, 3, hasCollider: true),
                SolidDecor1x1 = CreatePrefab(folder, "SolidDecor_1x1", sprites.SolidDecor, 1, 1, hasCollider: true),
                SolidDecor2x2 = CreatePrefab(folder, "SolidDecor_2x2", sprites.SolidDecor, 2, 2, hasCollider: true),
            };
        }

        static GameObject CreatePrefab(string folder, string name, Sprite sprite, int width, int height, bool hasCollider)
        {
            var go = new GameObject(name);
            try
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.size = new Vector2(width, height);
                renderer.sortingOrder = DungeonBuilder.ActorSortingOrder;

                if (hasCollider)
                {
                    var collider = go.AddComponent<BoxCollider2D>();
                    collider.size = new Vector2(width, height);
                }

                var path = $"{folder}/{name}.prefab";
                return PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
