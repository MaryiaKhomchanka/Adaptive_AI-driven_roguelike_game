using UnityEngine;
using UnityEngine.InputSystem;

namespace Generation.Runtime
{
    /// <summary>
    /// Debug-scene helper (spec 13/14), unrelated to the real attack input (F, PlayerMovements): left-click
    /// destroys the Enemy-tagged placeholder under the cursor; K destroys every enemy in the current room.
    /// A cheat so the generation module's clear-room/minimap flow can be tested before combat exists.
    /// </summary>
    public sealed class DebugEnemyKiller : MonoBehaviour
    {
        public Camera targetCamera;
        public DungeonBuilder builder;
        public RoomTransitionController transitionController;

        void Update()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                KillUnderCursor();

            if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
                KillAllInCurrentRoom();
        }

        void KillUnderCursor()
        {
            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;

            var worldPoint = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            var hit = Physics2D.OverlapPoint(worldPoint);
            if (hit != null && hit.CompareTag("Enemy"))
            {
                Debug.Log($"[Debug] killed {hit.name} via click cheat");
                Destroy(hit.gameObject);
            }
        }

        void KillAllInCurrentRoom()
        {
            if (builder == null || transitionController == null) return;
            if (!builder.RoomsById.TryGetValue(transitionController.CurrentRoomId, out var room)) return;

            Debug.Log($"[Debug] killing all enemies in room {room.Room.Id} via K cheat");
            foreach (Transform child in room.Spawned)
                if (child.CompareTag("Enemy"))
                    Destroy(child.gameObject);
        }
    }
}
