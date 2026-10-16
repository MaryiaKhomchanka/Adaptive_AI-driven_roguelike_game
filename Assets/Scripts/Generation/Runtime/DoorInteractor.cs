using UnityEngine;
using UnityEngine.InputSystem;

namespace Generation.Runtime
{
    /// <summary>
    /// E near a door uses it: teleports into the connected room (spec: doors never close, you go through
    /// them via interaction rather than walking through an open gap). The one exception is the boss door -
    /// entering it asks for confirmation first, and leaving it is refused while DoorLockRegistry says it's locked.
    /// </summary>
    public sealed class DoorInteractor : MonoBehaviour
    {
        public Transform player;
        public DungeonBuilder builder;
        public RoomTransitionController transitionController;
        public BossEntryPrompt bossPrompt;
        public float interactRadius = 1.2f;

        Door pendingBossDoor;

        void Update()
        {
            if (Keyboard.current == null || player == null) return;

            if (pendingBossDoor != null)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame) ConfirmBossDoor();
                else if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame) CancelBossDoor();
                return;
            }

            if (!Keyboard.current.eKey.wasPressedThisFrame) return;

            foreach (var hit in Physics2D.OverlapCircleAll(player.position, interactRadius))
            {
                var door = hit.GetComponent<Door>();
                if (door == null) continue;
                TryUseDoor(door);
                break;
            }
        }

        void TryUseDoor(Door door)
        {
            var bossRoomId = builder.CurrentLayout?.BossRoomId ?? -1;

            if (door.ThisRoomId == bossRoomId && DoorLockRegistry.IsLocked(bossRoomId))
            {
                Debug.Log("[Door] locked until the boss is resolved");
                return;
            }

            if (door.OtherRoomId == bossRoomId && door.ThisRoomId != bossRoomId)
            {
                pendingBossDoor = door;
                bossPrompt?.Show();
                Debug.Log("[Door] boss room ahead - E to confirm, Q/Esc to cancel");
                return;
            }

            UseDoor(door);
        }

        void ConfirmBossDoor()
        {
            var door = pendingBossDoor;
            pendingBossDoor = null;
            bossPrompt?.Hide();

            UseDoor(door);
            DoorLockRegistry.Lock(door.OtherRoomId); // OtherRoomId is the boss room we just stepped into
            Debug.Log("[Door] boss room door locked behind you");
        }

        void CancelBossDoor()
        {
            pendingBossDoor = null;
            bossPrompt?.Hide();
        }

        void UseDoor(Door door)
        {
            Debug.Log($"[Door] room {door.ThisRoomId} -> room {door.OtherRoomId}");
            transitionController.Teleport(door.ThisRoomId, door.OtherRoomId, door.EntryPositionInOtherRoom);
        }
    }
}
