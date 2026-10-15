using System.IO;
using UnityEditor;
using UnityEngine;

namespace Generation.Editor
{
    /// <summary>Flat-colored 32x32 PNGs imported as PPU-32, point-filtered, uncompressed sprites (spec 13).</summary>
    static class PlaceholderSprites
    {
        public struct Result
        {
            public Sprite Floor0, Floor1, Floor2, FloorDecor, Wall, Obstacle, SolidDecor;
        }

        public static Result CreateAll(string folder)
        {
            return new Result
            {
                Floor0 = CreateSolid(folder, "Floor0", new Color32(214, 206, 190, 255)),
                Floor1 = CreateSolid(folder, "Floor1", new Color32(204, 196, 180, 255)),
                Floor2 = CreateSolid(folder, "Floor2", new Color32(222, 214, 198, 255)),
                FloorDecor = CreateSolid(folder, "FloorDecor", new Color32(170, 160, 140, 180)),
                Wall = CreateSolid(folder, "Wall", new Color32(90, 82, 78, 255)),
                Obstacle = CreateSolid(folder, "Obstacle", new Color32(70, 40, 35, 255)),
                SolidDecor = CreateSolid(folder, "SolidDecor", new Color32(60, 120, 120, 170)),
            };
        }

        static Sprite CreateSolid(string folder, string name, Color32 color)
        {
            const int size = 32;
            var path = $"{folder}/{name}.png";

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = color;

            // 1px darker border so adjacent placeholder tiles are visually distinguishable.
            var border = new Color32((byte)(color.r * 0.7f), (byte)(color.g * 0.7f), (byte)(color.b * 0.7f), color.a);
            for (var x = 0; x < size; x++)
            {
                pixels[x] = border;
                pixels[(size - 1) * size + x] = border;
            }
            for (var y = 0; y < size; y++)
            {
                pixels[y * size] = border;
                pixels[y * size + (size - 1)] = border;
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Directory.CreateDirectory(folder);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
