using System.Collections.Generic;
using System.Linq;

namespace Generation.Core
{
    /// <summary>Checks every invariant from spec section 8.3, against a template's grid as transformed for a given RoomShape.</summary>
    public static class TemplateValidator
    {
        static readonly HashSet<char> Blocking = new HashSet<char> { '#', 'P', 'X' };
        static readonly HashSet<char> DoorZoneAllowed = new HashSet<char> { '.', ',', 'E', 'L' };
        static readonly RoomType[] RequireSpecialSlot = { RoomType.Boss, RoomType.Treasure, RoomType.Shop, RoomType.Npc, RoomType.Start };

        /// <summary>Validates every transform allowed by the template's own policy. Used by library-wide checks (spec test 12).</summary>
        public static List<string> ValidateAllTransforms(RoomTemplate template)
        {
            var errors = new List<string>();
            foreach (var t in TemplateTransformUtil.AllowedTransforms(template.TransformPolicy))
            {
                var shape = TemplateTransformUtil.ResultingShape(template, t);
                var grid = TemplateTransformUtil.ApplyToGrid(template, t, out var w, out var h);
                errors.AddRange(Validate(grid, w, h, shape, template.Types, $"{template.Name} [rot={t.RotationDegrees}, mirror={t.Mirrored}]"));
            }
            return errors;
        }

        public static List<string> Validate(char[,] tiles, int width, int height, RoomShape shape, HashSet<RoomType> types, string context)
        {
            var errors = new List<string>();

            if (types.Overlaps(RequireSpecialSlot))
            {
                var hasSlot = false;
                for (var tx = 0; tx < width && !hasSlot; tx++)
                for (var ty = 0; ty < height && !hasSlot; ty++)
                    if (tiles[tx, ty] == 'S') hasSlot = true;
                if (!hasSlot)
                    errors.Add($"{context}: must contain at least one 'S' slot for its room type(s).");
            }

            var doorZoneCells = new List<(int tx, int ty, Dir side)>();
            foreach (var slot in ShapeDefinition.DoorSlots(shape))
            {
                var originX = slot.LocalChunk.X * ShapeDefinition.ChunkTiles;
                var originY = slot.LocalChunk.Y * ShapeDefinition.ChunkTiles;
                foreach (var (dx, dy) in ZoneOffsets(slot.Side))
                {
                    var tx = originX + dx;
                    var ty = originY + dy;
                    doorZoneCells.Add((tx, ty, slot.Side));
                    if (tx < 0 || tx >= width || ty < 0 || ty >= height)
                    {
                        errors.Add($"{context}: door zone for chunk {slot.LocalChunk} side {slot.Side} falls outside the grid at ({tx},{ty}).");
                        continue;
                    }
                    if (!DoorZoneAllowed.Contains(tiles[tx, ty]))
                        errors.Add($"{context}: door zone for chunk {slot.LocalChunk} side {slot.Side} is blocked at ({tx},{ty}) by '{tiles[tx, ty]}'.");
                }
            }

            errors.AddRange(CheckConnectivity(tiles, width, height, doorZoneCells, context));

            return errors;
        }

        /// <summary>Local tile offsets (within a 16x16 chunk) for the 2x2 zone just inside the room behind a door opening on the given side.</summary>
        static IEnumerable<(int dx, int dy)> ZoneOffsets(Dir side)
        {
            switch (side)
            {
                case Dir.N: foreach (var x in new[] { 7, 8 }) foreach (var y in new[] { 13, 14 }) yield return (x, y); break;
                case Dir.S: foreach (var x in new[] { 7, 8 }) foreach (var y in new[] { 1, 2 }) yield return (x, y); break;
                case Dir.E: foreach (var x in new[] { 13, 14 }) foreach (var y in new[] { 7, 8 }) yield return (x, y); break;
                case Dir.W: foreach (var x in new[] { 1, 2 }) foreach (var y in new[] { 7, 8 }) yield return (x, y); break;
            }
        }

        static List<string> CheckConnectivity(char[,] tiles, int width, int height, List<(int tx, int ty, Dir side)> doorZoneCells, string context)
        {
            var errors = new List<string>();
            var inBounds = doorZoneCells.Where(c => c.tx >= 0 && c.tx < width && c.ty >= 0 && c.ty < height).ToList();
            if (inBounds.Count == 0) return errors;

            var componentId = new int[width, height];
            for (var x = 0; x < width; x++) for (var y = 0; y < height; y++) componentId[x, y] = -1;

            var nextId = 0;
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (componentId[x, y] != -1 || Blocking.Contains(tiles[x, y])) continue;
                    FloodFill(tiles, width, height, x, y, nextId, componentId);
                    nextId++;
                }
            }

            var referenceComponent = componentId[inBounds[0].tx, inBounds[0].ty];
            foreach (var cell in inBounds)
            {
                if (componentId[cell.tx, cell.ty] != referenceComponent)
                    errors.Add($"{context}: door zone at ({cell.tx},{cell.ty}) side {cell.side} is not connected to the rest of the room.");
            }

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var c = tiles[x, y];
                    if ((c == 'E' || c == 'L' || c == 'S') && componentId[x, y] != referenceComponent)
                        errors.Add($"{context}: slot '{c}' at ({x},{y}) is not connected to the door zones.");
                }
            }

            return errors;
        }

        static void FloodFill(char[,] tiles, int width, int height, int startX, int startY, int id, int[,] componentId)
        {
            var stack = new Stack<(int x, int y)>();
            stack.Push((startX, startY));
            while (stack.Count > 0)
            {
                var (x, y) = stack.Pop();
                if (x < 0 || x >= width || y < 0 || y >= height) continue;
                if (componentId[x, y] != -1 || Blocking.Contains(tiles[x, y])) continue;
                componentId[x, y] = id;
                stack.Push((x + 1, y));
                stack.Push((x - 1, y));
                stack.Push((x, y + 1));
                stack.Push((x, y - 1));
            }
        }
    }
}
