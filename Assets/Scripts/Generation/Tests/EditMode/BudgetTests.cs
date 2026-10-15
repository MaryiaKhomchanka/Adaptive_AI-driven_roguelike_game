using System.Collections.Generic;
using System.Linq;
using Generation.Core;
using NUnit.Framework;

namespace Generation.Tests
{
    public class BudgetTests
    {
        static List<EnemySpawnDef> MakeEnemyDefs() => new List<EnemySpawnDef>
        {
            new EnemySpawnDef { Id = "slime", Cost = 1f, Weight = 1f, MinDungeonIndex = 1 },
            new EnemySpawnDef { Id = "bigSlime", Cost = 3f, Weight = 0.3f, MinDungeonIndex = 1 },
            new EnemySpawnDef { Id = "lateGame", Cost = 1f, Weight = 1f, MinDungeonIndex = 5 },
        };

        static List<LootSpawnDef> MakeLootDefs() => new List<LootSpawnDef>
        {
            new LootSpawnDef { Id = "coin", Weight = 1f },
            new LootSpawnDef { Id = "potion", Weight = 0.5f },
        };

        static GenerationRequest MakeRequest(int seed, int dungeonIndex)
        {
            var request = TestRequests.Default(seed, dungeonIndex);
            request.BaseThreatPerChunk = 2f;
            request.LootPerChunkBase = 0.5f;
            request.DungeonScale = 1f + 0.2f * (dungeonIndex - 1);
            return request;
        }

        [Test]
        public void EnemyCost_NeverExceedsBudget_AndNeverExceedsSlotCount()
        {
            var enemyDefs = MakeEnemyDefs();
            var lootDefs = MakeLootDefs();
            var templates = TemplateTestLibrary.Load();

            for (var seed = 0; seed < 100; seed++)
            {
                var request = MakeRequest(seed, 4);
                var layout = DungeonGenerator.Generate(request, templates, enemyDefs, lootDefs);

                foreach (var room in layout.Rooms.Where(r => r.Type == RoomType.Combat))
                {
                    var slotCount = BudgetPlanner.CountSlots(templates[room.TemplateId], room.TemplateTransform, 'E');
                    Assert.LessOrEqual(room.Enemies.Count, slotCount, $"room {room.Id}");

                    var budget = request.BaseThreatPerChunk * room.Chunks.Length * BudgetPlanner.EnemyShapeMultiplier(room.Shape) * request.DungeonScale * request.ThreatMultiplier;
                    var spentCost = room.Enemies.Sum(e => enemyDefs.First(d => d.Id == e.Id).Cost);
                    Assert.LessOrEqual(spentCost, budget + 1e-4f, $"room {room.Id}: spent {spentCost} > budget {budget}");
                }
            }
        }

        [Test]
        public void NonCombatRooms_NeverGetEnemiesOrLoot()
        {
            var enemyDefs = MakeEnemyDefs();
            var lootDefs = MakeLootDefs();
            var templates = TemplateTestLibrary.Load();

            for (var seed = 0; seed < 100; seed++)
            {
                var request = MakeRequest(seed, 4);
                var layout = DungeonGenerator.Generate(request, templates, enemyDefs, lootDefs);

                foreach (var room in layout.Rooms.Where(r => r.Type != RoomType.Combat))
                {
                    Assert.IsEmpty(room.Enemies, $"room {room.Id} ({room.Type}) should have no enemies");
                    Assert.IsEmpty(room.Loot, $"room {room.Id} ({room.Type}) should have no loot");
                }
            }
        }

        [Test]
        public void EnemiesBelowMinDungeonIndex_AreNeverPicked()
        {
            var enemyDefs = MakeEnemyDefs();
            var templates = TemplateTestLibrary.Load();

            for (var seed = 0; seed < 100; seed++)
            {
                var request = MakeRequest(seed, 2); // below lateGame's MinDungeonIndex of 5
                var layout = DungeonGenerator.Generate(request, templates, enemyDefs, new List<LootSpawnDef>());

                foreach (var room in layout.Rooms)
                    Assert.IsFalse(room.Enemies.Any(e => e.Id == "lateGame"), $"room {room.Id}, seed {seed}");
            }
        }

        [Test]
        public void Density_GrowsWithDungeonIndex()
        {
            var enemyDefs = new List<EnemySpawnDef> { new EnemySpawnDef { Id = "slime", Cost = 1f, Weight = 1f, MinDungeonIndex = 1 } };
            var templates = TemplateTestLibrary.Load();

            float TotalEnemiesAcrossSeeds(int dungeonIndex)
            {
                float total = 0;
                for (var seed = 0; seed < 100; seed++)
                {
                    var request = MakeRequest(1000 + seed, dungeonIndex);
                    var layout = DungeonGenerator.Generate(request, templates, enemyDefs, new List<LootSpawnDef>());
                    total += layout.Rooms.Sum(r => r.Enemies.Count);
                }
                return total;
            }

            var low = TotalEnemiesAcrossSeeds(1);
            var high = TotalEnemiesAcrossSeeds(8);
            Assert.Greater(high, low, "higher dungeonIndex should yield more enemies on average");
        }
    }
}
