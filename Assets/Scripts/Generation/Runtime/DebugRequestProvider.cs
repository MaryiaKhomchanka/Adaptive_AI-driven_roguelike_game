using Generation.Core;
using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>Inspector-editable request, standing in for the future AI layer while it doesn't exist yet (spec 6.1).</summary>
    [System.Serializable]
    public sealed class DebugRequestProvider : IGenerationRequestProvider
    {
        public int roomCountMin = 7;
        public int roomCountMax = 9;
        [Range(0, 3)] public float threatMultiplier = 1f;
        [Range(0, 3)] public float lootMultiplier = 1f;
        [Range(0, 1)] public float loopChance = 0.15f;
        [Range(0, 1)] public float treasureChance = 0.30f;
        [Range(0, 1)] public float shopChance = 0.25f;
        [Range(0, 1)] public float npcChance = 0.20f;
        public bool revealBossRoom = true;

        public GenerationRequest Create(int dungeonIndex, int seed)
        {
            return new GenerationRequest
            {
                Seed = seed,
                DungeonIndex = dungeonIndex,
                RoomCountMin = roomCountMin,
                RoomCountMax = roomCountMax,
                ThreatMultiplier = threatMultiplier,
                LootMultiplier = lootMultiplier,
                LoopChance = loopChance,
                TreasureChance = treasureChance,
                ShopChance = shopChance,
                NpcChance = npcChance,
                RevealBossRoom = revealBossRoom,
            };
        }
    }
}
