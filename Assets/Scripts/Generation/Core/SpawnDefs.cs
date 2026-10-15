namespace Generation.Core
{
    /// <summary>Abstract spawn-table row (spec 6.3 EnemySpawnTable), without any engine/prefab reference.</summary>
    public sealed class EnemySpawnDef
    {
        public string Id;
        public float Cost;
        public float Weight;
        public int MinDungeonIndex;
    }

    /// <summary>Abstract spawn-table row (spec 6.3 LootTable). No cost: loot budget drives item count directly.</summary>
    public sealed class LootSpawnDef
    {
        public string Id;
        public float Weight;
    }
}
