using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Editor
{
    /// <summary>Tile/RuleTile assets wrapping the placeholder sprites. The wall RuleTile has no neighbor rules yet
    /// (spec 13: "RuleTile на одном спрайте") so it just renders its own sprite until real art and rules are added.</summary>
    static class PlaceholderTiles
    {
        public struct Result
        {
            public TileBase[] FloorVariants;
            public TileBase FloorDecor;
            public TileBase Wall;
        }

        public static Result CreateAll(string folder, PlaceholderSprites.Result sprites)
        {
            return new Result
            {
                FloorVariants = new TileBase[]
                {
                    CreateTile(folder, "FloorTile0", sprites.Floor0),
                    CreateTile(folder, "FloorTile1", sprites.Floor1),
                    CreateTile(folder, "FloorTile2", sprites.Floor2),
                },
                FloorDecor = CreateTile(folder, "FloorDecorTile", sprites.FloorDecor),
                Wall = CreateWallRuleTile(folder, sprites.Wall),
            };
        }

        static Tile CreateTile(string folder, string name, Sprite sprite)
        {
            var path = $"{folder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        static RuleTile CreateWallRuleTile(string folder, Sprite sprite)
        {
            var path = $"{folder}/WallRuleTile.asset";
            var tile = AssetDatabase.LoadAssetAtPath<RuleTile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<RuleTile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.m_DefaultSprite = sprite;
            tile.m_DefaultColliderType = Tile.ColliderType.Grid;
            tile.m_TilingRules = new System.Collections.Generic.List<RuleTile.TilingRule>();
            EditorUtility.SetDirty(tile);
            return tile;
        }
    }
}
