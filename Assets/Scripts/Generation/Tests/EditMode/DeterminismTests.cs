using System.Linq;
using System.Text;
using Generation.Core;
using NUnit.Framework;

namespace Generation.Tests
{
    public class DeterminismTests
    {
        static string Serialize(DungeonLayout layout)
        {
            var sb = new StringBuilder();
            foreach (var r in layout.Rooms.OrderBy(r => r.Id))
                sb.Append($"R{r.Id}:{r.Shape}:{r.Type}:{r.Anchor}:{r.DistanceFromStart}|");
            foreach (var d in layout.Doors.OrderBy(d => d.RoomA).ThenBy(d => d.RoomB).ThenBy(d => d.SideA))
                sb.Append($"D{d.RoomA}-{d.RoomB}:{d.ChunkA}:{d.SideA}:{d.IsLoop}|");
            return sb.ToString();
        }

        [Test]
        public void SameRequest_ProducesIdenticalLayout_Over100Seeds()
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var request = TestRequests.Default(seed, dungeonIndex: 3);
                var a = DungeonGenerator.Generate(request);
                var b = DungeonGenerator.Generate(request);
                Assert.AreEqual(Serialize(a), Serialize(b), $"Mismatch for seed={seed}");
            }
        }
    }
}
