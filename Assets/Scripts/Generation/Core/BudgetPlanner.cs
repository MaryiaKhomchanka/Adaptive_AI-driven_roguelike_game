using System.Collections.Generic;
using System.Linq;

namespace Generation.Core
{
    /// <summary>Pipeline step 5 (spec 7.5): plans enemy/loot/special occupants into template slots, as data only (no spawning).</summary>
    public static class BudgetPlanner
    {
        public static void AssignBudgets(
            GenerationRequest request,
            int attempt,
            DungeonLayout layout,
            IReadOnlyList<RoomTemplate> templates,
            IReadOnlyList<EnemySpawnDef> enemyDefs,
            IReadOnlyList<LootSpawnDef> lootDefs)
        {
            var rng = Rng.ForStep(request.Seed, "budgets", attempt);

            foreach (var room in layout.Rooms.OrderBy(r => r.Id))
            {
                var template = templates[room.TemplateId];
                var slotCountE = CountSlots(template, room.TemplateTransform, 'E');
                var slotCountL = CountSlots(template, room.TemplateTransform, 'L');
                var slotCountS = CountSlots(template, room.TemplateTransform, 'S');

                switch (room.Type)
                {
                    case RoomType.Combat:
                        var chunkCount = room.Chunks.Length;
                        var enemyBudget = request.BaseThreatPerChunk * chunkCount * EnemyShapeMultiplier(room.Shape) * request.DungeonScale * request.ThreatMultiplier;
                        var lootBudget = request.LootPerChunkBase * chunkCount * LootShapeMultiplier(room.Shape) * request.DungeonScale * request.LootMultiplier;

                        room.Enemies = PickEnemies(rng, enemyDefs, request.DungeonIndex, enemyBudget, slotCountE);
                        room.Loot = PickLoot(rng, lootDefs, lootBudget, slotCountL);
                        break;

                    case RoomType.Boss when slotCountS > 0:
                        room.Special = new PlannedSpawn { SlotIndex = ShuffledIndices(rng, slotCountS)[0], Id = "boss" };
                        break;
                    case RoomType.Treasure when slotCountS > 0:
                        room.Special = new PlannedSpawn { SlotIndex = ShuffledIndices(rng, slotCountS)[0], Id = "treasure" };
                        break;
                    case RoomType.Shop when slotCountS > 0:
                        room.Special = new PlannedSpawn { SlotIndex = ShuffledIndices(rng, slotCountS)[0], Id = "shop" };
                        break;
                    case RoomType.Npc when slotCountS > 0:
                        room.Special = new PlannedSpawn { SlotIndex = ShuffledIndices(rng, slotCountS)[0], Id = "npc" };
                        break;
                }
            }
        }

        public static float EnemyShapeMultiplier(RoomShape shape)
        {
            switch (shape.FamilyOf())
            {
                case ShapeFamily.Small: return 1.0f;
                case ShapeFamily.Corridor: return 0.5f;
                case ShapeFamily.Large: return 1.5f;
                default: return 1.25f; // LShape
            }
        }

        public static float LootShapeMultiplier(RoomShape shape)
        {
            switch (shape.FamilyOf())
            {
                case ShapeFamily.Small: return 1.0f;
                case ShapeFamily.Corridor: return 1.25f;
                case ShapeFamily.Large: return 1.0f;
                default: return 1.25f; // LShape
            }
        }

        static List<PlannedSpawn> PickEnemies(Rng rng, IReadOnlyList<EnemySpawnDef> enemyDefs, int dungeonIndex, float budget, int slotCount)
        {
            var result = new List<PlannedSpawn>();
            if (slotCount == 0 || budget <= 0 || enemyDefs == null || enemyDefs.Count == 0) return result;

            var eligible = enemyDefs.Where(d => d.MinDungeonIndex <= dungeonIndex).ToList();
            var slotOrder = ShuffledIndices(rng, slotCount);
            var remaining = budget;

            for (var i = 0; i < slotCount; i++)
            {
                var affordable = eligible.Where(d => d.Cost <= remaining).ToList();
                if (affordable.Count == 0) break;

                var chosen = rng.PickWeighted(affordable, d => d.Weight);
                result.Add(new PlannedSpawn { SlotIndex = slotOrder[i], Id = chosen.Id });
                remaining -= chosen.Cost;
            }

            return result;
        }

        static List<PlannedSpawn> PickLoot(Rng rng, IReadOnlyList<LootSpawnDef> lootDefs, float budget, int slotCount)
        {
            var result = new List<PlannedSpawn>();
            if (slotCount == 0 || budget <= 0 || lootDefs == null || lootDefs.Count == 0) return result;

            var itemCount = System.Math.Min(slotCount, (int)System.Math.Round(budget, System.MidpointRounding.AwayFromZero));
            var slotOrder = ShuffledIndices(rng, slotCount);

            for (var i = 0; i < itemCount; i++)
            {
                var chosen = rng.PickWeighted(lootDefs, d => d.Weight);
                result.Add(new PlannedSpawn { SlotIndex = slotOrder[i], Id = chosen.Id });
            }

            return result;
        }

        static List<int> ShuffledIndices(Rng rng, int count)
        {
            var indices = Enumerable.Range(0, count).ToList();
            for (var i = indices.Count - 1; i > 0; i--)
            {
                var j = rng.NextInt(0, i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }
            return indices;
        }

        public static int CountSlots(RoomTemplate template, Transform2D transform, char symbol)
        {
            var grid = TemplateTransformUtil.ApplyToGrid(template, transform, out var w, out var h);
            var count = 0;
            for (var x = 0; x < w; x++)
            for (var y = 0; y < h; y++)
                if (grid[x, y] == symbol)
                    count++;
            return count;
        }
    }
}
