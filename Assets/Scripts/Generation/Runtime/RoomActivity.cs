using System.Collections.Generic;
using UnityEngine;

namespace Generation.Runtime
{
    public enum RoomMemberKind { Enemy, Loot, Treasure, Shop, Npc, Boss }

    /// <summary>
    /// Live counts of RoomMember instances per room/kind. Fires RoomCleared/RoomLooted/BossResolved when a
    /// count reaches zero, and backs the minimap's "does this room still have X" icons (stage 5).
    /// Logs every register/remove: with no UI yet, the console is the only way to see room activity while testing.
    /// </summary>
    public static class RoomActivity
    {
        static readonly Dictionary<int, Dictionary<RoomMemberKind, int>> counts = new Dictionary<int, Dictionary<RoomMemberKind, int>>();

        public static void Clear() => counts.Clear();

        public static void Register(int roomId, RoomMemberKind kind)
        {
            var dict = GetDict(roomId);
            dict[kind] = dict.TryGetValue(kind, out var c) ? c + 1 : 1;
            Debug.Log($"[Room {roomId}] {kind} spawned (now {dict[kind]})");
        }

        public static void ReportRemoved(int roomId, RoomMemberKind kind)
        {
            var dict = GetDict(roomId);
            if (dict.TryGetValue(kind, out var c) && c > 0) dict[kind] = c - 1;
            Debug.Log($"[Room {roomId}] {kind} removed (remaining {GetCount(roomId, kind)})");

            switch (kind)
            {
                case RoomMemberKind.Enemy when GetCount(roomId, RoomMemberKind.Enemy) == 0:
                    DungeonEvents.RaiseRoomCleared(roomId);
                    break;
                case RoomMemberKind.Loot when GetCount(roomId, RoomMemberKind.Loot) == 0:
                    DungeonEvents.RaiseRoomLooted(roomId);
                    break;
                case RoomMemberKind.Boss:
                    DungeonEvents.RaiseBossResolved(roomId);
                    break;
            }
        }

        public static int GetCount(int roomId, RoomMemberKind kind) =>
            counts.TryGetValue(roomId, out var dict) && dict.TryGetValue(kind, out var c) ? c : 0;

        static Dictionary<RoomMemberKind, int> GetDict(int roomId)
        {
            if (!counts.TryGetValue(roomId, out var dict))
                counts[roomId] = dict = new Dictionary<RoomMemberKind, int>();
            return dict;
        }
    }
}
