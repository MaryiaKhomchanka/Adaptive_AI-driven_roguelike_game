namespace Generation.Core
{
    public enum RoomType
    {
        Start,
        Combat,
        Boss,
        Treasure,
        Shop,
        Npc,
    }

    public static class RoomTypeExtensions
    {
        /// <summary>Treasure/Shop/Npc: capped at one per dungeon, excluded from loop doors.</summary>
        public static bool IsSpecial(this RoomType type) =>
            type == RoomType.Treasure || type == RoomType.Shop || type == RoomType.Npc;
    }
}
