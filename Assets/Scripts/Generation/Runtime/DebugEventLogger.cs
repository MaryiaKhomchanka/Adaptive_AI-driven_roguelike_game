using Generation.Core;
using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>Debug-scene only: logs every module event to the console. There's no UI yet, so this is
    /// currently the only way to see room enter/exit/clear/loot/boss/complete while testing.</summary>
    public sealed class DebugEventLogger : MonoBehaviour
    {
        void OnEnable()
        {
            DungeonEvents.DungeonGenerated += OnDungeonGenerated;
            DungeonEvents.RoomEntered += OnRoomEntered;
            DungeonEvents.RoomExited += OnRoomExited;
            DungeonEvents.RoomCleared += OnRoomCleared;
            DungeonEvents.RoomLooted += OnRoomLooted;
            DungeonEvents.BossResolved += OnBossResolved;
            DungeonEvents.DungeonCompleted += OnDungeonCompleted;
        }

        void OnDisable()
        {
            DungeonEvents.DungeonGenerated -= OnDungeonGenerated;
            DungeonEvents.RoomEntered -= OnRoomEntered;
            DungeonEvents.RoomExited -= OnRoomExited;
            DungeonEvents.RoomCleared -= OnRoomCleared;
            DungeonEvents.RoomLooted -= OnRoomLooted;
            DungeonEvents.BossResolved -= OnBossResolved;
            DungeonEvents.DungeonCompleted -= OnDungeonCompleted;
        }

        static void OnDungeonGenerated(DungeonLayout layout) =>
            Debug.Log($"[Dungeon] generated: seed={layout.Seed}, dungeonIndex={layout.DungeonIndex}, rooms={layout.Rooms.Count}, boss=room{layout.BossRoomId}");

        static void OnRoomEntered(int roomId, RoomType type, bool isFirstVisit) =>
            Debug.Log($"[Room {roomId}] entered ({type}){(isFirstVisit ? ", first visit" : "")}");

        static void OnRoomExited(int roomId, float timeSpent) =>
            Debug.Log($"[Room {roomId}] exited after {timeSpent:0.00}s");

        static void OnRoomCleared(int roomId) => Debug.Log($"[Room {roomId}] cleared (no enemies left)");
        static void OnRoomLooted(int roomId) => Debug.Log($"[Room {roomId}] looted (no loot left)");
        static void OnBossResolved(int roomId) => Debug.Log($"[Room {roomId}] boss resolved");

        static void OnDungeonCompleted(DungeonSummary summary) => Debug.Log(
            $"[Dungeon] completed: time={summary.TimeSeconds:0.0}s, visited={summary.RoomsVisited}/{summary.RoomsTotal}, " +
            $"cleared={summary.RoomsCleared}, enemiesLeft={summary.EnemiesLeftAlive}, lootLeft={summary.LootLeftUnpicked}");
    }
}
