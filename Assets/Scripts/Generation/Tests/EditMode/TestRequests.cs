using System.Collections.Generic;
using Generation.Core;

namespace Generation.Tests
{
    /// <summary>Builds GenerationRequest instances matching the [tunable] defaults from spec section 13, for tests only.</summary>
    static class TestRequests
    {
        public static GenerationRequest Default(int seed, int dungeonIndex = 1)
        {
            return new GenerationRequest
            {
                Seed = seed,
                DungeonIndex = dungeonIndex,
                RoomCountMin = System.Math.Min(7 + 2 * dungeonIndex, 28),
                RoomCountMax = System.Math.Min(9 + 2 * dungeonIndex, 28),
                LoopChance = 0.15f,
                TreasureChance = 0.30f,
                ShopChance = 0.25f,
                NpcChance = 0.20f,
                ShapeWeights = GenerationRequest.DefaultShapeWeights(),
            };
        }

        public static Dictionary<RoomShape, float> NoCorridorWeights()
        {
            return new Dictionary<RoomShape, float>
            {
                { RoomShape.Small, 0.6f },
                { RoomShape.CorridorH, 0f },
                { RoomShape.CorridorV, 0f },
                { RoomShape.Large, 0.2f },
                { RoomShape.L_NoNE, 0.05f },
                { RoomShape.L_NoNW, 0.05f },
                { RoomShape.L_NoSE, 0.05f },
                { RoomShape.L_NoSW, 0.05f },
            };
        }

        public static Dictionary<RoomShape, float> CorridorHeavyWeights()
        {
            return new Dictionary<RoomShape, float>
            {
                { RoomShape.Small, 0.35f },
                { RoomShape.CorridorH, 0.2f },
                { RoomShape.CorridorV, 0.2f },
                { RoomShape.Large, 0.15f },
                { RoomShape.L_NoNE, 0.025f },
                { RoomShape.L_NoNW, 0.025f },
                { RoomShape.L_NoSE, 0.025f },
                { RoomShape.L_NoSW, 0.025f },
            };
        }
    }
}
