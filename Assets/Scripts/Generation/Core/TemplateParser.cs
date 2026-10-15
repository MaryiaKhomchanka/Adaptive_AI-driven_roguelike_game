using System;
using System.Collections.Generic;
using System.Linq;

namespace Generation.Core
{
    public sealed class TemplateParseException : Exception
    {
        public TemplateParseException(string message) : base(message) { }
    }

    /// <summary>Parses the text template format from spec 8.1/8.2. Accepts a plain string, no Unity asset types.</summary>
    public static class TemplateParser
    {
        const int TileSize = 16;
        static readonly HashSet<char> AllowedChars = new HashSet<char> { '.', '#', 'P', ',', 'E', 'L', 'S', 'X' };

        public static RoomTemplate Parse(string text, string name)
        {
            if (string.IsNullOrEmpty(text))
                throw new TemplateParseException($"Template '{name}': file is empty.");

            var rawLines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

            string shapeValue = null, typesValue = null, tagsValue = null, weightValue = null, transformsValue = null;
            var gridLines = new List<string>();
            var inGrid = false;

            foreach (var rawLine in rawLines)
            {
                var line = rawLine;
                if (!inGrid)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.TrimStart().StartsWith("#"))
                    {
                        var (key, value) = ParseHeaderLine(line, name);
                        switch (key)
                        {
                            case "shape": shapeValue = value; break;
                            case "types": typesValue = value; break;
                            case "tags": tagsValue = value; break;
                            case "weight": weightValue = value; break;
                            case "transforms": transformsValue = value; break;
                            default: throw new TemplateParseException($"Template '{name}': unknown header key '{key}'.");
                        }
                        continue;
                    }
                    inGrid = true;
                }

                if (inGrid)
                {
                    if (line.Length == 0) continue; // allow trailing blank lines at EOF
                    gridLines.Add(line);
                }
            }

            if (shapeValue == null)
                throw new TemplateParseException($"Template '{name}': missing required '# shape:' header.");
            if (typesValue == null)
                throw new TemplateParseException($"Template '{name}': missing required '# types:' header.");

            if (!Enum.TryParse(shapeValue, true, out ShapeFamily family))
                throw new TemplateParseException($"Template '{name}': unknown shape '{shapeValue}'. Expected Small, Corridor, Large or LShape.");

            var types = new HashSet<RoomType>();
            foreach (var part in typesValue.Split(','))
            {
                var trimmed = part.Trim();
                if (trimmed.Length == 0) continue;
                if (!Enum.TryParse(trimmed, true, out RoomType type))
                    throw new TemplateParseException($"Template '{name}': unknown room type '{trimmed}' in '# types:'.");
                types.Add(type);
            }
            if (types.Count == 0)
                throw new TemplateParseException($"Template '{name}': '# types:' must list at least one room type.");

            var tags = (tagsValue ?? string.Empty)
                .Split(',')
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToList();

            var weight = 1f;
            if (weightValue != null)
            {
                if (!float.TryParse(weightValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out weight))
                    throw new TemplateParseException($"Template '{name}': '# weight:' value '{weightValue}' is not a number.");
            }

            var transformPolicy = TemplateTransformPolicy.All;
            if (transformsValue != null)
            {
                switch (transformsValue.Trim().ToLowerInvariant())
                {
                    case "all": transformPolicy = TemplateTransformPolicy.All; break;
                    case "none": transformPolicy = TemplateTransformPolicy.None; break;
                    case "rotate": transformPolicy = TemplateTransformPolicy.RotateOnly; break;
                    case "mirror": transformPolicy = TemplateTransformPolicy.MirrorOnly; break;
                    default: throw new TemplateParseException($"Template '{name}': unknown '# transforms:' value '{transformsValue}'. Expected all, none, rotate or mirror.");
                }
            }

            var (expectedWidth, expectedHeight) = ExpectedSize(family);

            if (gridLines.Count != expectedHeight)
                throw new TemplateParseException($"Template '{name}': expected {expectedHeight} grid rows for shape {family}, found {gridLines.Count}.");

            var tiles = new char[expectedWidth, expectedHeight];
            for (var lineIndex = 0; lineIndex < gridLines.Count; lineIndex++)
            {
                var line = gridLines[lineIndex];
                if (line.Length != expectedWidth)
                    throw new TemplateParseException($"Template '{name}': grid row {lineIndex + 1} has length {line.Length}, expected {expectedWidth}.");

                var ty = expectedHeight - 1 - lineIndex; // file line 0 is the northmost (topmost) row
                for (var tx = 0; tx < expectedWidth; tx++)
                {
                    var c = line[tx];
                    if (!AllowedChars.Contains(c))
                        throw new TemplateParseException($"Template '{name}': row {lineIndex + 1}, column {tx + 1}: unknown symbol '{c}'.");
                    tiles[tx, ty] = c;
                }
            }

            var template = new RoomTemplate
            {
                Name = name,
                Family = family,
                Types = types,
                Tags = tags,
                Weight = weight,
                TransformPolicy = transformPolicy,
                Width = expectedWidth,
                Height = expectedHeight,
                Tiles = tiles,
            };

            ValidateXPlacement(template, name);

            return template;
        }

        static (string key, string value) ParseHeaderLine(string line, string name)
        {
            var trimmed = line.TrimStart().TrimStart('#').Trim();
            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex < 0)
                throw new TemplateParseException($"Template '{name}': header line '{line}' is missing ':'.");
            var key = trimmed.Substring(0, colonIndex).Trim().ToLowerInvariant();
            var value = trimmed.Substring(colonIndex + 1).Trim();
            return (key, value);
        }

        static (int width, int height) ExpectedSize(ShapeFamily family)
        {
            switch (family)
            {
                case ShapeFamily.Small: return (TileSize, TileSize);
                case ShapeFamily.Corridor: return (TileSize * 2, TileSize); // authored horizontal
                case ShapeFamily.Large: return (TileSize * 2, TileSize * 2);
                case ShapeFamily.LShape: return (TileSize * 2, TileSize * 2);
                default: throw new ArgumentOutOfRangeException(nameof(family));
            }
        }

        static void ValidateXPlacement(RoomTemplate template, string name)
        {
            if (template.Family != ShapeFamily.LShape)
            {
                for (var tx = 0; tx < template.Width; tx++)
                for (var ty = 0; ty < template.Height; ty++)
                    if (template.Tiles[tx, ty] == 'X')
                        throw new TemplateParseException($"Template '{name}': 'X' is only valid in LShape templates.");
                return;
            }

            Corner? found = null;
            foreach (var corner in new[] { Corner.NE, Corner.NW, Corner.SE, Corner.SW })
            {
                if (QuadrantIsAllX(template, corner))
                {
                    if (found != null)
                        throw new TemplateParseException($"Template '{name}': more than one quadrant is marked X; exactly one corner must be cut.");
                    found = corner;
                }
            }
            if (found == null)
                throw new TemplateParseException($"Template '{name}': LShape template must have exactly one 16x16 quadrant fully marked 'X'.");

            // No X outside that single quadrant.
            for (var tx = 0; tx < template.Width; tx++)
            for (var ty = 0; ty < template.Height; ty++)
                if (template.Tiles[tx, ty] == 'X' && !InQuadrant(tx, ty, found.Value, TileSize))
                    throw new TemplateParseException($"Template '{name}': 'X' found outside the single cut corner at ({tx},{ty}).");

            template.LShapeMissingCorner = found.Value;
        }

        static bool QuadrantIsAllX(RoomTemplate template, Corner corner)
        {
            for (var tx = 0; tx < template.Width; tx++)
            for (var ty = 0; ty < template.Height; ty++)
                if (InQuadrant(tx, ty, corner, TileSize) && template.Tiles[tx, ty] != 'X')
                    return false;
            return true;
        }

        static bool InQuadrant(int tx, int ty, Corner corner, int tileSize)
        {
            var east = tx >= tileSize;
            var north = ty >= tileSize;
            switch (corner)
            {
                case Corner.NE: return east && north;
                case Corner.NW: return !east && north;
                case Corner.SE: return east && !north;
                case Corner.SW: return !east && !north;
                default: throw new ArgumentOutOfRangeException(nameof(corner));
            }
        }
    }
}
