using System.Linq;
using Generation.Core;
using NUnit.Framework;

namespace Generation.Tests
{
    public class LayoutInvariantTests
    {
        [Test]
        public void ExactlyOneStart_SmallShape()
        {
            for (var seed = 0; seed < 50; seed++)
            {
                var layout = DungeonGenerator.Generate(TestRequests.Default(seed, 4));
                var starts = layout.Rooms.Where(r => r.Type == RoomType.Start).ToList();
                Assert.AreEqual(1, starts.Count, $"seed={seed}");
                Assert.AreEqual(RoomShape.Small, starts[0].Shape, $"seed={seed}");
                Assert.AreEqual(layout.StartRoomId, starts[0].Id, $"seed={seed}");
            }
        }

        [Test]
        public void ExactlyOneBoss_LargeShape_OneDoor_FarthestEligible()
        {
            for (var seed = 0; seed < 50; seed++)
            {
                var layout = DungeonGenerator.Generate(TestRequests.Default(seed, 5));
                var bosses = layout.Rooms.Where(r => r.Type == RoomType.Boss).ToList();
                Assert.AreEqual(1, bosses.Count, $"seed={seed}");

                var boss = bosses[0];
                Assert.AreEqual(RoomShape.Large, boss.Shape, $"seed={seed}");
                Assert.AreEqual(1, GraphUtils.DoorCount(boss.Id, layout.Doors), $"seed={seed}");
                Assert.AreEqual(layout.BossRoomId, boss.Id, $"seed={seed}");

                // Boss selection (7.3) happens on the pre-loop tree; loops added afterwards (7.2) can only
                // add doors, so they can remove other Large rooms from "degree == 1" contention or reshuffle
                // final BFS distances via shortcuts. Reconstruct the tree-only view to check the real invariant.
                var treeDoors = layout.Doors.Where(d => !d.IsLoop).ToList();
                var treeDistances = GraphUtils.BfsDistances(layout.Rooms, treeDoors, layout.StartRoomId);
                var eligible = layout.Rooms.Where(r => r.Shape == RoomShape.Large && GraphUtils.DoorCount(r.Id, treeDoors) == 1);
                var maxDistance = eligible.Max(r => treeDistances[r.Id]);
                Assert.AreEqual(maxDistance, treeDistances[boss.Id], $"seed={seed}");
            }
        }

        [Test]
        public void SpecialRooms_AtMostOneEach_SmallOnly_NotNeighboringStart()
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var layout = DungeonGenerator.Generate(TestRequests.Default(seed, 4));
                var startNeighbors = new System.Collections.Generic.HashSet<int>(GraphUtils.NeighborsOf(layout.StartRoomId, layout.Doors));

                foreach (var type in new[] { RoomType.Treasure, RoomType.Shop, RoomType.Npc })
                {
                    var matches = layout.Rooms.Where(r => r.Type == type).ToList();
                    Assert.LessOrEqual(matches.Count, 1, $"seed={seed}, type={type}");
                    foreach (var m in matches)
                    {
                        Assert.AreEqual(RoomShape.Small, m.Shape, $"seed={seed}, type={type}");
                        Assert.IsFalse(startNeighbors.Contains(m.Id), $"seed={seed}, type={type} is adjacent to Start");
                        Assert.AreNotEqual(layout.StartRoomId, m.Id, $"seed={seed}, type={type}");
                    }
                }
            }
        }

        [Test]
        public void SpecialRoomChance_OneMeansSometimesPresent_ZeroMeansNever()
        {
            var always = TestRequests.Default(1, 4);
            always.TreasureChance = always.ShopChance = always.NpcChance = 1f;
            var sawAllThree = false;
            for (var s = 0; s < 50; s++)
            {
                always.Seed = 10_000 + s;
                var layout = DungeonGenerator.Generate(always);
                var counts = new[] { RoomType.Treasure, RoomType.Shop, RoomType.Npc }
                    .Select(t => layout.Rooms.Count(r => r.Type == t)).ToList();
                if (counts.All(c => c == 1)) sawAllThree = true;
            }
            Assert.IsTrue(sawAllThree, "With all special chances at 1.0, all three should appear together at least once in 50 seeds (candidates permitting).");

            var never = TestRequests.Default(1, 4);
            never.TreasureChance = never.ShopChance = never.NpcChance = 0f;
            for (var s = 0; s < 50; s++)
            {
                never.Seed = 20_000 + s;
                var layout = DungeonGenerator.Generate(never);
                Assert.IsFalse(layout.Rooms.Any(r => r.Type.IsSpecial()), $"seed={never.Seed}");
            }
        }

        [Test]
        public void AtMostOneDoorBetweenAnyPair_AndLoopsNeverTouchBossOrSpecial()
        {
            for (var seed = 0; seed < 50; seed++)
            {
                var request = TestRequests.Default(seed, 6);
                request.LoopChance = 0.5f;
                var layout = DungeonGenerator.Generate(request);

                var pairCounts = new System.Collections.Generic.Dictionary<(int, int), int>();
                foreach (var door in layout.Doors)
                {
                    var key = door.RoomA < door.RoomB ? (door.RoomA, door.RoomB) : (door.RoomB, door.RoomA);
                    pairCounts[key] = pairCounts.GetValueOrDefault(key) + 1;
                }
                Assert.IsTrue(pairCounts.Values.All(c => c == 1), $"seed={seed}: duplicate door between some pair");

                var roomsById = layout.Rooms.ToDictionary(r => r.Id);
                foreach (var door in layout.Doors.Where(d => d.IsLoop))
                {
                    var a = roomsById[door.RoomA];
                    var b = roomsById[door.RoomB];
                    Assert.IsFalse(a.Type == RoomType.Boss || b.Type == RoomType.Boss, $"seed={seed}: loop touches Boss");
                    Assert.IsFalse(a.Type.IsSpecial() || b.Type.IsSpecial(), $"seed={seed}: loop touches a special room");
                }
            }
        }

        [Test]
        public void Corridors_HaveDoorInEachChunk_AndAtLeastTwoNeighbors()
        {
            var corridorsSeen = 0;
            var generationFailures = 0;
            for (var s = 0; s < 200; s++)
            {
                var request = TestRequests.Default(3000 + s, 4);
                request.ShapeWeights = TestRequests.CorridorHeavyWeights();
                try
                {
                    var layout = DungeonGenerator.Generate(request);
                    var violations = DungeonValidator.Validate(layout);
                    Assert.IsEmpty(violations, $"seed={request.Seed}: {string.Join("; ", violations)}");
                    corridorsSeen += layout.Rooms.Count(r => ShapeDefinition.IsCorridor(r.Shape));
                }
                catch (GenerationException)
                {
                    // Spec 7.6: an unfixable corridor dead end is a legitimate, config-dependent failure
                    // after MaxGenerationAttempts, not a code defect. Bounded below.
                    generationFailures++;
                }
            }
            Assert.Greater(corridorsSeen, 0, "Corridor-heavy weighting should actually produce corridor rooms.");
            Assert.Less(generationFailures, 200 / 5, $"Too many outright generation failures: {generationFailures}/200");
        }

        [Test]
        public void LoopChanceZero_ProducesTree_WhenNoCorridorsInvolved()
        {
            // Isolated from Corridor shapes: their dead-end fix-up (7.2) can add doors independently
            // of LoopChance, so the "doors = rooms - 1" tree property only holds without them.
            for (var seed = 0; seed < 50; seed++)
            {
                var request = TestRequests.Default(5000 + seed, 5);
                request.LoopChance = 0f;
                request.ShapeWeights = TestRequests.NoCorridorWeights();
                var layout = DungeonGenerator.Generate(request);
                Assert.AreEqual(layout.Rooms.Count - 1, layout.Doors.Count, $"seed={request.Seed}");
            }
        }
    }
}
