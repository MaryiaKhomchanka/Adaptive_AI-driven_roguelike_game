using System.Collections.Generic;
using System.Linq;
using Generation.Core;
using NUnit.Framework;

namespace Generation.Tests
{
    public class PropPlacerTests
    {
        static char[,] MakeGrid(int width, int height, IEnumerable<(int x, int y)> symbolCells, char symbol)
        {
            var grid = new char[width, height];
            for (var x = 0; x < width; x++) for (var y = 0; y < height; y++) grid[x, y] = '.';
            foreach (var (x, y) in symbolCells) grid[x, y] = symbol;
            return grid;
        }

        static IEnumerable<(int x, int y)> Rect(int x0, int y0, int w, int h)
        {
            for (var x = x0; x < x0 + w; x++)
            for (var y = y0; y < y0 + h; y++)
                yield return (x, y);
        }

        [Test]
        public void PlacedProps_ExactlyCoverTheRegion_NoOverlap()
        {
            var region = Rect(2, 2, 7, 5).ToList(); // 35 cells, not evenly divisible by 2x2/3x1
            var grid = MakeGrid(16, 16, region, '#');
            var sizes = new[] { new PropSize(2, 2, 2f), new PropSize(3, 1, 1f), new PropSize(1, 3, 1f) };

            var placed = PropPlacer.Plan(grid, 16, 16, '#', sizes, variantCount: 3, rng: new Rng(1));

            var covered = new HashSet<(int, int)>();
            foreach (var p in placed)
            {
                for (var dx = 0; dx < p.Width; dx++)
                for (var dy = 0; dy < p.Height; dy++)
                {
                    var cell = (p.X + dx, p.Y + dy);
                    Assert.IsTrue(region.Contains(cell), $"placed prop cell {cell} outside the region");
                    Assert.IsFalse(covered.Contains(cell), $"cell {cell} covered by more than one prop");
                    covered.Add(cell);
                }
            }
            Assert.AreEqual(region.Count, covered.Count, "placed props must cover every region cell exactly once");
        }

        [Test]
        public void Determinism_SameInputs_ProduceIdenticalPlacement()
        {
            var region = Rect(1, 1, 10, 6).ToList();
            var grid = MakeGrid(16, 16, region, 'P');
            var sizes = new[] { new PropSize(3, 3, 1f), new PropSize(2, 2, 2f), new PropSize(1, 1, 1f) };

            string Serialize(IEnumerable<PlacedProp> props) =>
                string.Join("|", props.Select(p => $"{p.X},{p.Y},{p.Width}x{p.Height},v{p.VariantIndex}"));

            var a = PropPlacer.Plan(grid, 16, 16, 'P', sizes, 4, new Rng(42));
            var b = PropPlacer.Plan(grid, 16, 16, 'P', sizes, 4, new Rng(42));
            Assert.AreEqual(Serialize(a), Serialize(b));
        }

        [Test]
        public void TwoDisjointRegions_AreFilledIndependently()
        {
            var regionA = Rect(0, 0, 3, 3).ToList();
            var regionB = Rect(10, 10, 4, 2).ToList();
            var grid = MakeGrid(16, 16, regionA.Concat(regionB), ',');

            var placed = PropPlacer.Plan(grid, 16, 16, ',', new[] { new PropSize(2, 2, 1f) }, 1, new Rng(7));

            var covered = new HashSet<(int, int)>(placed.SelectMany(p => Rect(p.X, p.Y, p.Width, p.Height)));
            Assert.AreEqual(regionA.Count + regionB.Count, covered.Count);
        }

        [Test]
        public void NoSizesProvided_FallsBackToOneByOne()
        {
            var region = Rect(5, 5, 3, 1).ToList();
            var grid = MakeGrid(16, 16, region, '#');
            var placed = PropPlacer.Plan(grid, 16, 16, '#', System.Array.Empty<PropSize>(), 1, new Rng(3));
            Assert.AreEqual(3, placed.Count);
            Assert.IsTrue(placed.All(p => p.Width == 1 && p.Height == 1));
        }
    }
}
