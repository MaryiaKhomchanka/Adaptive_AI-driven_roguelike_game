using System;
using Generation.Core;
using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>Module events (spec 6.5), for the AI layer, UI and other systems. Static so any system can subscribe without a reference.</summary>
    public static class DungeonEvents
    {
        public static event Action<DungeonLayout> DungeonGenerated;
        public static event Action<int, RoomType, bool> RoomEntered;
        public static event Action<int, float> RoomExited;
        public static event Action<int> RoomCleared;
        public static event Action<int> RoomLooted;
        public static event Action<int> BossResolved;
        public static event Action<DungeonSummary> DungeonCompleted;

        public static DungeonLayout CurrentLayout { get; private set; }
        public static float DungeonStartTime { get; private set; }

        public static void RaiseDungeonGenerated(DungeonLayout layout)
        {
            CurrentLayout = layout;
            DungeonStartTime = Time.time;
            RoomActivity.Clear();
            DungeonGenerated?.Invoke(layout);
        }

        public static void RaiseRoomEntered(int roomId, RoomType type, bool isFirstVisit) => RoomEntered?.Invoke(roomId, type, isFirstVisit);
        public static void RaiseRoomExited(int roomId, float timeSpentSeconds) => RoomExited?.Invoke(roomId, timeSpentSeconds);
        public static void RaiseRoomCleared(int roomId) => RoomCleared?.Invoke(roomId);
        public static void RaiseRoomLooted(int roomId) => RoomLooted?.Invoke(roomId);
        public static void RaiseBossResolved(int roomId) => BossResolved?.Invoke(roomId);
        public static void RaiseDungeonCompleted(DungeonSummary summary) => DungeonCompleted?.Invoke(summary);

        /// <summary>Called by combat when the boss lets the player go without being destroyed (spec 6.4).</summary>
        public static void ReportBossResolved()
        {
            if (CurrentLayout != null) RaiseBossResolved(CurrentLayout.BossRoomId);
        }
    }
}
