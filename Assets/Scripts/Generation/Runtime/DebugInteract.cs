using UnityEngine;
using UnityEngine.InputSystem;

namespace Generation.Runtime
{
    /// <summary>
    /// Debug-scene only, not real interaction logic (that's out of this module's scope - spec 3/18, "задача
    /// команды"). E near a Treasure/Shop/Npc placeholder just logs that the trigger fired, so whoever builds
    /// the real pickup/shop/dialog can see the slot wiring already works.
    /// </summary>
    public sealed class DebugInteract : MonoBehaviour
    {
        public Transform player;
        public float radius = 1f;

        void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) return;
            if (player == null) return;

            foreach (var hit in Physics2D.OverlapCircleAll(player.position, radius))
            {
                var member = hit.GetComponent<RoomMember>();
                if (member == null) continue;

                switch (member.Kind)
                {
                    case RoomMemberKind.Treasure:
                        Debug.Log($"[Interact] E near Treasure in room {member.RoomId} - pickup logic belongs to the loot system");
                        return;
                    case RoomMemberKind.Shop:
                        Debug.Log($"[Interact] E near Shop in room {member.RoomId} - shop UI belongs to the trader system");
                        return;
                    case RoomMemberKind.Npc:
                        Debug.Log($"[Interact] E near Npc in room {member.RoomId} - dialog belongs to the NPC/dialogue system");
                        return;
                }
            }
        }
    }
}
