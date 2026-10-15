using System.Collections.Generic;
using System.Linq;

namespace Generation.Core
{
    public readonly struct PropSize
    {
        public readonly int Width;
        public readonly int Height;
        public readonly float Weight;

        public PropSize(int width, int height, float weight)
        {
            Width = width;
            Height = height;
            Weight = weight;
        }
    }

    /// <summary>A prop footprint placed within one connected symbol region, anchored at its west-south tile (X,Y).</summary>
    public sealed class PlacedProp
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public int VariantIndex;
    }

    /// <summary>
    /// Fills connected regions of a template symbol ('#', 'P' or ',') with the largest fitting prop
    /// footprint repeatedly, falling back to 1x1 for whatever is left over (spec section 9).
    /// </summary>
    public static class PropPlacer
    {
        public static List<PlacedProp> Plan(char[,] tiles, int width, int height, char symbol, IReadOnlyList<PropSize> sizes, int variantCount, Rng rng)
        {
            var result = new List<PlacedProp>();
            foreach (var region in FindRegions(tiles, width, height, symbol))
                result.AddRange(PlanRegion(region, sizes, variantCount, rng));
            return result;
        }

        static List<PlacedProp> PlanRegion(HashSet<(int x, int y)> region, IReadOnlyList<PropSize> sizes, int variantCount, Rng rng)
        {
            var effectiveSizes = sizes.Any(s => s.Width == 1 && s.Height == 1)
                ? sizes.ToList()
                : sizes.Append(new PropSize(1, 1, 1f)).ToList();

            var remaining = new HashSet<(int x, int y)>(region);
            var placed = new List<PlacedProp>();

            var tiers = effectiveSizes.GroupBy(s => s.Width * s.Height).OrderByDescending(g => g.Key).ToList();

            while (remaining.Count > 0)
            {
                var placedThisRound = false;
                foreach (var tier in tiers)
                {
                    var candidates = new List<(PropSize size, int x, int y)>();
                    foreach (var size in tier)
                    foreach (var origin in remaining.OrderBy(c => c.y).ThenBy(c => c.x))
                        if (FitsFully(remaining, origin.x, origin.y, size))
                            candidates.Add((size, origin.x, origin.y));

                    if (candidates.Count == 0) continue;

                    var chosen = rng.PickWeighted(candidates, c => c.size.Weight);
                    for (var dx = 0; dx < chosen.size.Width; dx++)
                    for (var dy = 0; dy < chosen.size.Height; dy++)
                        remaining.Remove((chosen.x + dx, chosen.y + dy));

                    placed.Add(new PlacedProp
                    {
                        X = chosen.x,
                        Y = chosen.y,
                        Width = chosen.size.Width,
                        Height = chosen.size.Height,
                        VariantIndex = variantCount > 0 ? rng.NextInt(0, variantCount) : 0,
                    });
                    placedThisRound = true;
                    break;
                }

                if (!placedThisRound) break; // only possible if 1x1 somehow missing; defensive
            }

            return placed;
        }

        static bool FitsFully(HashSet<(int x, int y)> remaining, int x, int y, PropSize size)
        {
            for (var dx = 0; dx < size.Width; dx++)
            for (var dy = 0; dy < size.Height; dy++)
                if (!remaining.Contains((x + dx, y + dy)))
                    return false;
            return true;
        }

        static List<HashSet<(int x, int y)>> FindRegions(char[,] tiles, int width, int height, char symbol)
        {
            var visited = new bool[width, height];
            var regions = new List<HashSet<(int x, int y)>>();

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (visited[x, y] || tiles[x, y] != symbol) continue;

                    var region = new HashSet<(int x, int y)>();
                    var stack = new Stack<(int x, int y)>();
                    stack.Push((x, y));
                    while (stack.Count > 0)
                    {
                        var (cx, cy) = stack.Pop();
                        if (cx < 0 || cx >= width || cy < 0 || cy >= height) continue;
                        if (visited[cx, cy] || tiles[cx, cy] != symbol) continue;
                        visited[cx, cy] = true;
                        region.Add((cx, cy));
                        stack.Push((cx + 1, cy));
                        stack.Push((cx - 1, cy));
                        stack.Push((cx, cy + 1));
                        stack.Push((cx, cy - 1));
                    }
                    regions.Add(region);
                }
            }

            return regions;
        }
    }
}
