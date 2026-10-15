using System.Linq;
using Generation.Core;
using NUnit.Framework;

namespace Generation.Tests
{
    public class ShapeDefinitionTests
    {
        [TestCase(RoomShape.Small, 1, 4)]
        [TestCase(RoomShape.CorridorH, 2, 6)]
        [TestCase(RoomShape.CorridorV, 2, 6)]
        [TestCase(RoomShape.Large, 4, 8)]
        [TestCase(RoomShape.L_NoNE, 3, 8)]
        [TestCase(RoomShape.L_NoNW, 3, 8)]
        [TestCase(RoomShape.L_NoSE, 3, 8)]
        [TestCase(RoomShape.L_NoSW, 3, 8)]
        public void FootprintAndDoorSlotCounts_MatchSpec(RoomShape shape, int expectedChunks, int expectedDoorSlots)
        {
            Assert.AreEqual(expectedChunks, ShapeDefinition.Chunks(shape).Count);
            Assert.AreEqual(expectedDoorSlots, ShapeDefinition.DoorSlots(shape).Count());
        }

        [Test]
        public void LShapes_EachOmitsExactlyOneCornerOfTheLargeFootprint()
        {
            var large = new System.Collections.Generic.HashSet<Cell>(ShapeDefinition.Chunks(RoomShape.Large));
            foreach (var l in new[] { RoomShape.L_NoNE, RoomShape.L_NoNW, RoomShape.L_NoSE, RoomShape.L_NoSW })
            {
                var chunks = ShapeDefinition.Chunks(l);
                Assert.AreEqual(3, chunks.Count);
                Assert.IsTrue(chunks.All(c => large.Contains(c)), $"{l} must be a subset of the Large footprint");
            }
        }
    }
}
