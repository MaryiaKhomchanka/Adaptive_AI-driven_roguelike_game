using System.Collections.Generic;
using System.Linq;

namespace Generation.Core
{
    public static class GraphUtils
    {
        /// <summary>BFS distance (in doors) from startRoomId to every room reachable through doors. Unreachable rooms are absent from the result.</summary>
        public static Dictionary<int, int> BfsDistances(IReadOnlyList<RoomNode> rooms, IReadOnlyList<DoorLink> doors, int startRoomId)
        {
            var adjacency = BuildAdjacency(rooms, doors);
            var distances = new Dictionary<int, int> { [startRoomId] = 0 };
            var queue = new Queue<int>();
            queue.Enqueue(startRoomId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in adjacency[current])
                {
                    if (distances.ContainsKey(neighbor)) continue;
                    distances[neighbor] = distances[current] + 1;
                    queue.Enqueue(neighbor);
                }
            }
            return distances;
        }

        public static Dictionary<int, List<int>> BuildAdjacency(IReadOnlyList<RoomNode> rooms, IReadOnlyList<DoorLink> doors)
        {
            var adjacency = rooms.ToDictionary(r => r.Id, _ => new List<int>());
            foreach (var door in doors)
            {
                adjacency[door.RoomA].Add(door.RoomB);
                adjacency[door.RoomB].Add(door.RoomA);
            }
            return adjacency;
        }

        public static int DoorCount(int roomId, IReadOnlyList<DoorLink> doors)
        {
            var count = 0;
            foreach (var door in doors)
                if (door.RoomA == roomId || door.RoomB == roomId)
                    count++;
            return count;
        }

        public static bool DoorExistsBetween(int roomA, int roomB, IReadOnlyList<DoorLink> doors)
        {
            foreach (var door in doors)
                if ((door.RoomA == roomA && door.RoomB == roomB) || (door.RoomA == roomB && door.RoomB == roomA))
                    return true;
            return false;
        }

        public static IEnumerable<int> NeighborsOf(int roomId, IReadOnlyList<DoorLink> doors)
        {
            foreach (var door in doors)
            {
                if (door.RoomA == roomId) yield return door.RoomB;
                else if (door.RoomB == roomId) yield return door.RoomA;
            }
        }
    }
}
