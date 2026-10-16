using System.Collections.Generic;

namespace Generation.Runtime
{
    /// <summary>
    /// The one exception to "doors never close": the boss room's door locks once the player steps through it,
    /// and releases once the boss is resolved. Keyed by the boss room's id (there is only ever one).
    /// </summary>
    public static class DoorLockRegistry
    {
        static readonly HashSet<int> lockedBossRooms = new HashSet<int>();

        public static void Clear() => lockedBossRooms.Clear();
        public static void Lock(int bossRoomId) => lockedBossRooms.Add(bossRoomId);
        public static void Unlock(int bossRoomId) => lockedBossRooms.Remove(bossRoomId);
        public static bool IsLocked(int bossRoomId) => lockedBossRooms.Contains(bossRoomId);
    }
}
