using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>
    /// Room transitions are now explicit door interactions (DoorInteractor), not continuous walking through
    /// an open gap: Teleport() moves the player, freezes the room just left, and activates/enters the new one.
    /// Still ticks EntryGrace every frame and keeps RoomCamera snapped to the current room's bounds.
    /// </summary>
    public sealed class RoomTransitionController : MonoBehaviour
    {
        public DungeonBuilder builder;
        public Transform player;
        public RoomCamera roomCamera;

        public int CurrentRoomId { get; private set; } = -1;

        void Update() => EntryGrace.Tick(Time.deltaTime);

        public void ResetForNewDungeon(int startRoomId)
        {
            CurrentRoomId = -1;
            EnterRoom(startRoomId, snapCamera: true);
        }

        public void Teleport(int fromRoomId, int toRoomId, Vector3 entryPosition)
        {
            if (player != null) player.position = entryPosition;
            if (builder.RoomsById.TryGetValue(fromRoomId, out var fromRoom)) fromRoom.Freeze();
            EnterRoom(toRoomId, snapCamera: true);
        }

        void EnterRoom(int roomId, bool snapCamera)
        {
            CurrentRoomId = roomId;
            if (!builder.RoomsById.TryGetValue(roomId, out var room)) return;

            room.Activate();

            if (roomCamera == null) return;
            roomCamera.RoomBounds = room.TileBounds;
            if (snapCamera && player != null)
            {
                var pos = player.position;
                var camPos = roomCamera.transform.position;
                roomCamera.transform.position = new Vector3(pos.x, pos.y, camPos.z);
            }
        }
    }
}
