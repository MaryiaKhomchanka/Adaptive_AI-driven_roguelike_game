using Generation.Core;
using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>Builds a GenerationRequest from GenerationConfig. Later replaced/wrapped by the AI layer via the same IGenerationRequestProvider interface.</summary>
    public sealed class DefaultRequestProvider : IGenerationRequestProvider
    {
        readonly GenerationConfig config;

        public DefaultRequestProvider(GenerationConfig config)
        {
            this.config = config;
        }

        public GenerationRequest Create(int dungeonIndex, int seed)
        {
            return new GenerationRequest
            {
                Seed = seed,
                DungeonIndex = dungeonIndex,
                RoomCountMin = Mathf.Min(config.roomCountBaseMin + config.roomCountPerIndex * dungeonIndex, config.roomCountCap),
                RoomCountMax = Mathf.Min(config.roomCountBaseMax + config.roomCountPerIndex * dungeonIndex, config.roomCountCap),
                LoopChance = config.loopChance,
                TreasureChance = config.treasureChance,
                ShopChance = config.shopChance,
                NpcChance = config.npcChance,
                ShapeWeights = config.ToShapeWeights(),
                MaxGenerationAttempts = config.maxGenerationAttempts,
            };
        }
    }
}
