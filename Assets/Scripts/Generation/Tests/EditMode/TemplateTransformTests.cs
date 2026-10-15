using System.Collections.Generic;
using System.Linq;
using Generation.Core;
using NUnit.Framework;

namespace Generation.Tests
{
    public class TemplateTransformTests
    {
        static RoomTemplate MakeLShape(Corner missingCorner, TemplateTransformPolicy policy = TemplateTransformPolicy.All)
        {
            var tiles = new char[32, 32];
            for (var x = 0; x < 32; x++)
            for (var y = 0; y < 32; y++)
                tiles[x, y] = '.';

            var eastRange = Enumerable.Range(16, 16);
            var northRange = Enumerable.Range(16, 16);
            foreach (var x in missingCorner == Corner.NE || missingCorner == Corner.SE ? eastRange : Enumerable.Range(0, 16))
            foreach (var y in missingCorner == Corner.NE || missingCorner == Corner.NW ? northRange : Enumerable.Range(0, 16))
                tiles[x, y] = 'X';

            return new RoomTemplate
            {
                Name = "Test",
                Family = ShapeFamily.LShape,
                Types = new HashSet<RoomType> { RoomType.Combat },
                Width = 32,
                Height = 32,
                Tiles = tiles,
                LShapeMissingCorner = missingCorner,
                TransformPolicy = policy,
            };
        }

        [Test]
        public void RotatingLShapeTemplate_CyclesThroughAllFourCorners()
        {
            var template = MakeLShape(Corner.NE);
            Assert.AreEqual(RoomShape.L_NoNE, TemplateTransformUtil.ResultingShape(template, new Transform2D(0, false)));
            Assert.AreEqual(RoomShape.L_NoSE, TemplateTransformUtil.ResultingShape(template, new Transform2D(90, false)));
            Assert.AreEqual(RoomShape.L_NoSW, TemplateTransformUtil.ResultingShape(template, new Transform2D(180, false)));
            Assert.AreEqual(RoomShape.L_NoNW, TemplateTransformUtil.ResultingShape(template, new Transform2D(270, false)));
        }

        [Test]
        public void MirroringLShapeTemplate_SwapsEastWestCorners()
        {
            var template = MakeLShape(Corner.NE);
            Assert.AreEqual(RoomShape.L_NoNW, TemplateTransformUtil.ResultingShape(template, new Transform2D(0, true)));
            Assert.AreEqual(RoomShape.L_NoNE, TemplateTransformUtil.ResultingShape(template, new Transform2D(0, false)));
        }

        [Test]
        public void CorridorTemplate_TogglesOrientationOnlyOnOddRotations()
        {
            var template = new RoomTemplate { Family = ShapeFamily.Corridor, Width = 32, Height = 16, Tiles = new char[32, 16] };
            Assert.AreEqual(RoomShape.CorridorH, TemplateTransformUtil.ResultingShape(template, new Transform2D(0, false)));
            Assert.AreEqual(RoomShape.CorridorV, TemplateTransformUtil.ResultingShape(template, new Transform2D(90, false)));
            Assert.AreEqual(RoomShape.CorridorH, TemplateTransformUtil.ResultingShape(template, new Transform2D(180, false)));
            Assert.AreEqual(RoomShape.CorridorV, TemplateTransformUtil.ResultingShape(template, new Transform2D(270, false)));
        }

        [TestCase(0, false)]
        [TestCase(90, false)]
        [TestCase(180, false)]
        [TestCase(270, false)]
        [TestCase(0, true)]
        [TestCase(90, true)]
        [TestCase(180, true)]
        [TestCase(270, true)]
        public void GridTransform_PreservesSymbolCountsAndDimensions(int degrees, bool mirrored)
        {
            var template = MakeLShape(Corner.NE);
            var grid = TemplateTransformUtil.ApplyToGrid(template, new Transform2D(degrees, mirrored), out var w, out var h);

            var swapped = degrees == 90 || degrees == 270;
            Assert.AreEqual(swapped ? template.Height : template.Width, w);
            Assert.AreEqual(swapped ? template.Width : template.Height, h);

            int CountX(char[,] g, int width, int height)
            {
                var count = 0;
                for (var x = 0; x < width; x++) for (var y = 0; y < height; y++) if (g[x, y] == 'X') count++;
                return count;
            }
            Assert.AreEqual(CountX(template.Tiles, template.Width, template.Height), CountX(grid, w, h));
        }

        [Test]
        public void ParseException_ReportsTemplateNameAndReason()
        {
            var ex = Assert.Throws<TemplateParseException>(() => TemplateParser.Parse("# shape: Small\n# types: Combat\ntoo_short\n", "Broken"));
            Assert.IsTrue(ex.Message.Contains("Broken"));
        }

        [Test]
        public void ParseException_OnMissingShapeHeader()
        {
            Assert.Throws<TemplateParseException>(() => TemplateParser.Parse("# types: Combat\n" + string.Join("\n", System.Linq.Enumerable.Repeat(new string('.', 16), 16)), "NoShape"));
        }
    }
}
