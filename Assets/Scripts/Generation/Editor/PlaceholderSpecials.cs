using Generation.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Generation.Editor
{
    /// <summary>Enemy/loot/special placeholders (spec 13): a tagged, killable circle for enemies; a pickup for
    /// loot; TMP-labeled markers standing in for chest/vendor/npc/boss/portal until real art and logic arrive.</summary>
    static class PlaceholderSpecials
    {
        public struct Result
        {
            public GameObject Enemy, Loot, Boss, Treasure, Shop, Npc, Portal;
        }

        public static Result CreateAll(string folder, PlaceholderSprites.Result sprites)
        {
            return new Result
            {
                Enemy = CreateEnemy(folder, sprites.Enemy),
                Loot = CreateLoot(folder, sprites.Loot),
                Boss = CreateLabeledMarker(folder, "Boss", sprites.Marker, new Color32(140, 20, 20, 255)),
                Treasure = CreateLabeledMarker(folder, "Treasure", sprites.Marker, new Color32(210, 170, 40, 255)),
                Shop = CreateLabeledMarker(folder, "Shop", sprites.Marker, new Color32(40, 90, 200, 255)),
                Npc = CreateLabeledMarker(folder, "Npc", sprites.Marker, new Color32(40, 160, 70, 255)),
                Portal = CreateLabeledMarker(folder, "Portal", sprites.Marker, new Color32(150, 40, 200, 255)),
            };
        }

        static GameObject CreateEnemy(string folder, Sprite sprite)
        {
            var go = new GameObject("Enemy");
            try
            {
                go.tag = "Enemy";
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = DungeonBuilder.ActorSortingOrder;
                var collider = go.AddComponent<CircleCollider2D>();
                collider.radius = 0.4f;
                return PrefabUtility.SaveAsPrefabAsset(go, $"{folder}/Enemy.prefab");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        static GameObject CreateLoot(string folder, Sprite sprite)
        {
            var go = new GameObject("Loot");
            try
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = DungeonBuilder.ActorSortingOrder;
                var collider = go.AddComponent<CircleCollider2D>();
                collider.radius = 0.3f;
                collider.isTrigger = true;
                go.AddComponent<LootPlaceholderPickup>();
                return PrefabUtility.SaveAsPrefabAsset(go, $"{folder}/Loot.prefab");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        static GameObject CreateLabeledMarker(string folder, string name, Sprite sprite, Color32 tint)
        {
            var go = new GameObject(name);
            try
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = tint;
                renderer.sortingOrder = DungeonBuilder.ActorSortingOrder;

                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(go.transform, false);
                labelGO.transform.localPosition = new Vector3(0, 0.8f, 0);
                var label = labelGO.AddComponent<TextMeshPro>();
                label.text = name;
                label.fontSize = 3;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                labelGO.GetComponent<MeshRenderer>().sortingOrder = DungeonBuilder.ActorSortingOrder + 1;

                return PrefabUtility.SaveAsPrefabAsset(go, $"{folder}/{name}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
