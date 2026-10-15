namespace Generation.Runtime
{
    /// <summary>Spec 6.5: payload of DungeonCompleted, for the AI layer to read play style from later.</summary>
    public sealed class DungeonSummary
    {
        public int DungeonIndex;
        public int Seed;
        public float TimeSeconds;
        public int RoomsVisited;
        public int RoomsTotal;
        public int RoomsCleared;
        public int EnemiesLeftAlive;
        public int LootLeftUnpicked;
    }
}
