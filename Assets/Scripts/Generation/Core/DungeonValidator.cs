using System.Collections.Generic;
using System.Linq;

namespace Generation.Core
{
    /// <summary>Checks every invariant from spec section 7.6. All must hold for any dungeon the generator returns.</summary>
    public static class DungeonValidator
    {
        public static List<string> Validate(DungeonLayout layout)
        {
            var violations = new List<string>();
            var rooms = layout.Rooms;
            var doors = layout.Doors;
            var roomsById = rooms.ToDictionary(r => r.Id);

            // Chunks: no overlaps.
            var chunkOwner = new Dictionary<Cell, int>();
            foreach (var room in rooms)
            {
                foreach (var chunk in room.Chunks)
                {
                    if (chunkOwner.TryGetValue(chunk, out var otherId))
                        violations.Add($"Chunk {chunk} is occupied by both room {otherId} and room {room.Id}.");
                    else
                        chunkOwner[chunk] = room.Id;
                }
            }

            // Connectivity: every room reachable from Start.
            var distances = GraphUtils.BfsDistances(rooms, doors, layout.StartRoomId);
            foreach (var room in rooms)
            {
                if (!distances.ContainsKey(room.Id))
                    violations.Add($"Room {room.Id} is not reachable from Start.");
            }

            // Exactly one Start (Small).
            var startRooms = rooms.Where(r => r.Type == RoomType.Start).ToList();
            if (startRooms.Count != 1)
                violations.Add($"Expected exactly 1 Start room, found {startRooms.Count}.");
            else if (startRooms[0].Shape != RoomShape.Small)
                violations.Add("Start room must be Small.");
            else if (startRooms[0].Id != layout.StartRoomId)
                violations.Add("Start room id does not match layout.StartRoomId.");

            // Exactly one Boss (Large, exactly one door).
            var bossRooms = rooms.Where(r => r.Type == RoomType.Boss).ToList();
            if (bossRooms.Count != 1)
            {
                violations.Add($"Expected exactly 1 Boss room, found {bossRooms.Count}.");
            }
            else
            {
                var boss = bossRooms[0];
                if (boss.Shape != RoomShape.Large)
                    violations.Add("Boss room must be Large.");
                if (GraphUtils.DoorCount(boss.Id, doors) != 1)
                    violations.Add("Boss room must have exactly one door.");
                if (boss.Id != layout.BossRoomId)
                    violations.Add("Boss room id does not match layout.BossRoomId.");
            }

            // Treasure/Shop/Npc: at most one each, Small only.
            foreach (var specialType in new[] { RoomType.Treasure, RoomType.Shop, RoomType.Npc })
            {
                var matches = rooms.Where(r => r.Type == specialType).ToList();
                if (matches.Count > 1)
                    violations.Add($"Expected at most 1 {specialType} room, found {matches.Count}.");
                foreach (var m in matches)
                    if (m.Shape != RoomShape.Small)
                        violations.Add($"{specialType} room {m.Id} must be Small.");
            }

            // At most one door between any pair of rooms.
            var pairCounts = new Dictionary<(int, int), int>();
            foreach (var door in doors)
            {
                var key = door.RoomA < door.RoomB ? (door.RoomA, door.RoomB) : (door.RoomB, door.RoomA);
                pairCounts[key] = pairCounts.GetValueOrDefault(key) + 1;
            }
            foreach (var kv in pairCounts)
                if (kv.Value > 1)
                    violations.Add($"More than one door between room {kv.Key.Item1} and room {kv.Key.Item2}.");

            // Each door sits on adjacent chunks actually owned by RoomA/RoomB.
            foreach (var door in doors)
            {
                var chunkB = door.ChunkA + door.SideA.Step();
                if (!chunkOwner.TryGetValue(door.ChunkA, out var ownerA) || ownerA != door.RoomA)
                    violations.Add($"Door {door.RoomA}-{door.RoomB}: ChunkA {door.ChunkA} is not owned by RoomA.");
                if (!chunkOwner.TryGetValue(chunkB, out var ownerB) || ownerB != door.RoomB)
                    violations.Add($"Door {door.RoomA}-{door.RoomB}: opposite chunk {chunkB} is not owned by RoomB.");
            }

            // Loops never touch Boss or special rooms.
            foreach (var door in doors.Where(d => d.IsLoop))
            {
                var a = roomsById[door.RoomA];
                var b = roomsById[door.RoomB];
                if (a.Type == RoomType.Boss || b.Type == RoomType.Boss)
                    violations.Add($"Loop door {door.RoomA}-{door.RoomB} touches the Boss room.");
                if (a.Type.IsSpecial() || b.Type.IsSpecial())
                    violations.Add($"Loop door {door.RoomA}-{door.RoomB} touches a special room.");
            }

            // Corridors: a door in each of its 2 chunks, and at least 2 distinct neighbors.
            foreach (var room in rooms.Where(r => ShapeDefinition.IsCorridor(r.Shape)))
            {
                var chunkHasDoor = new HashSet<Cell>();
                var neighborIds = new HashSet<int>();
                foreach (var door in doors)
                {
                    if (door.RoomA == room.Id) { chunkHasDoor.Add(door.ChunkA); neighborIds.Add(door.RoomB); }
                    else if (door.RoomB == room.Id) { chunkHasDoor.Add(door.ChunkA + door.SideA.Step()); neighborIds.Add(door.RoomA); }
                }
                foreach (var chunk in room.Chunks)
                    if (!chunkHasDoor.Contains(chunk))
                        violations.Add($"Corridor room {room.Id}: chunk {chunk} has no door.");
                if (neighborIds.Count < 2)
                    violations.Add($"Corridor room {room.Id} has fewer than 2 distinct neighbors.");
            }

            return violations;
        }
    }
}
