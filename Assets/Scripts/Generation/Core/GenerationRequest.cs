using System.Collections.Generic;

namespace Generation.Core
{
    public sealed class GenerationRequest
    {
        public int Seed;
        public int DungeonIndex = 1;
        public int RoomCountMin = 7;
        public int RoomCountMax = 9;
        public float ThreatMultiplier = 1f;
        public float LootMultiplier = 1f;
        public float LoopChance = 0.15f;
        public float TreasureChance = 0.30f;
        public float ShopChance = 0.25f;
        public float NpcChance = 0.20f;
        public Dictionary<RoomShape, float> ShapeWeights = DefaultShapeWeights();
        public Dictionary<string, float> TemplateTagWeights = new Dictionary<string, float>();
        public Dictionary<string, float> EnemyWeights = new Dictionary<string, float>();
        public bool RevealBossRoom = true;
        public int MaxGenerationAttempts = 50;

        /// <summary>[tunable] section 13 defaults; DungeonScale is the already-evaluated dungeonScale(dungeonIndex) curve, computed by the provider.</summary>
        public float BaseThreatPerChunk = 2f;
        public float LootPerChunkBase = 0.5f;
        public float DungeonScale = 1f;

        /// <summary>
        /// [tunable] defaults, split from the design table's 4 shape families (Small/Corridor/Large/LShape)
        /// into the 8 concrete RoomShape orientations this dictionary is keyed by.
        /// </summary>
        public static Dictionary<RoomShape, float> DefaultShapeWeights()
        {
            return new Dictionary<RoomShape, float>
            {
                { RoomShape.Small, 0.45f },
                { RoomShape.CorridorH, 0.10f },
                { RoomShape.CorridorV, 0.10f },
                { RoomShape.Large, 0.15f },
                { RoomShape.L_NoNE, 0.05f },
                { RoomShape.L_NoNW, 0.05f },
                { RoomShape.L_NoSE, 0.05f },
                { RoomShape.L_NoSW, 0.05f },
            };
        }
    }

    public interface IGenerationRequestProvider
    {
        GenerationRequest Create(int dungeonIndex, int seed);
    }
}
