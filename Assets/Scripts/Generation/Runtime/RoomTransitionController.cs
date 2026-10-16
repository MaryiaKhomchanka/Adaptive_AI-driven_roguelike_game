using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>
    /// Spec 10.3: when the player crosses a door, activate the room on the other side (spawning it on first
    /// visit); once the player's center is fully inside the new room, freeze whichever other room(s) are
    /// still active. Also ticks EntryGrace and keeps RoomCamera's bounds in sync with the current room.
    /// </summary>
    public sealed class RoomTransitionController : MonoBehaviour
    {
        public DungeonBuilder builder;
        public Transform player;
        public RoomCamera roomCamera;

        public int CurrentRoomId { get; private set; } = -1;

        readonly HashSet<int> activeRoomIds = new HashSet<int>();

        public void NotifyDoorCrossed(int roomAId, int roomBId)
        {
            ActivateRoom(roomAId);
            ActivateRoom(roomBId);
        }

        public void ResetForNewDungeon(int startRoomId)
        {
            activeRoomIds.Clear();
            CurrentRoomId = -1;
            ActivateRoom(startRoomId);
            SetCurrentRoom(startRoomId);
        }

        void ActivateRoom(int roomId)
        {
            if (activeRoomIds.Contains(roomId)) return;
            if (!builder.RoomsById.TryGetValue(roomId, out var controller)) return;
            controller.Prewarm();
            activeRoomIds.Add(roomId);
        }

        void Update()
        {
            EntryGrace.Tick(Time.deltaTime);

            if (player == null || builder.CurrentLayout == null) return;

            var pos = new Vector2Int(Mathf.FloorToInt(player.position.x), Mathf.FloorToInt(player.position.y));

            foreach (var id in activeRoomIds)
            {
                if (!builder.RoomsById.TryGetValue(id, out var rc)) continue;
                // Inset by 1 tile: becoming "current" (and freezing the room just left) only once the player
                // is a tile past the opening, not the instant they cross the shared chunk boundary - otherwise
                // the old room's fog-of-war drops before the player has actually stepped into the new one.
                if (!Inset(rc.TileBounds, 1).Contains(pos)) continue;
                if (id != CurrentRoomId) SetCurrentRoom(id);
                return;
            }
            // Player is still within 1 tile of a door opening, straddling two rooms: leave the current room as-is.
        }

        void SetCurrentRoom(int roomId)
        {
            CurrentRoomId = roomId;

            if (!activeRoomIds.Contains(roomId)) ActivateRoom(roomId); // the Start room has no door trigger to prewarm it
            if (builder.RoomsById.TryGetValue(roomId, out var current))
            {
                current.EnterAsCurrent();
                if (roomCamera != null) roomCamera.RoomBounds = current.TileBounds;
            }

            foreach (var id in activeRoomIds.ToList())
            {
                if (id == roomId) continue;
                if (builder.RoomsById.TryGetValue(id, out var rc)) rc.Freeze();
                activeRoomIds.Remove(id);
            }
        }

        static RectInt Inset(RectInt bounds, int amount)
        {
            var size = Mathf.Max(0, bounds.width - amount * 2);
            var height = Mathf.Max(0, bounds.height - amount * 2);
            return new RectInt(bounds.x + amount, bounds.y + amount, size, height);
        }
    }
}
