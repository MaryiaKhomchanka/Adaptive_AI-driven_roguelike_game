using System.Collections.Generic;

namespace Generation.Core
{
    public sealed class DoorLink
    {
        public int RoomA;
        public int RoomB;

        /// <summary>The chunk belonging to RoomA that the door sits against; RoomB's chunk is ChunkA + SideA.Step().</summary>
        public Cell ChunkA;
        public Dir SideA;
        public bool IsLoop;
    }

    /// <summary>A single planned enemy/loot/special occupant, referencing a template slot by index and a table id.</summary>
    public sealed class PlannedSpawn
    {
        public int SlotIndex;
        public string Id;
    }

    public sealed class RoomNode
    {
        public int Id;
        public RoomShape Shape;
        public RoomType Type = RoomType.Combat;
        public Cell Anchor;
        public Cell[] Chunks;

        public int TemplateId = -1;
        public Transform2D TemplateTransform = Transform2D.Identity;

        public List<PlannedSpawn> Enemies = new List<PlannedSpawn>();
        public List<PlannedSpawn> Loot = new List<PlannedSpawn>();
        public PlannedSpawn Special;

        public int DistanceFromStart = -1;
    }

    public sealed class DungeonLayout
    {
        public int Seed;
        public int DungeonIndex;
        public List<RoomNode> Rooms = new List<RoomNode>();
        public List<DoorLink> Doors = new List<DoorLink>();
        public int StartRoomId;
        public int BossRoomId;
    }
}
