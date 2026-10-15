using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>Spec 10.6: spawns the portal in the boss room's special slot once the boss is resolved (destroyed or let the player go).</summary>
    public sealed class PortalSpawner : MonoBehaviour
    {
        public DungeonBuilder builder;
        public SpecialPrefabs specialPrefabs;

        void OnEnable() => DungeonEvents.BossResolved += OnBossResolved;
        void OnDisable() => DungeonEvents.BossResolved -= OnBossResolved;

        void OnBossResolved(int roomId)
        {
            var layout = builder.CurrentLayout;
            if (layout == null || roomId != layout.BossRoomId) return;
            if (specialPrefabs == null || specialPrefabs.portal == null) return;
            if (!builder.RoomsById.TryGetValue(roomId, out var controller)) return;
            if (controller.transform.Find("Spawned/Portal") != null) return; // already spawned (e.g. boss destroyed then this fires again)

            var position = controller.SpecialSlotWorldPosition ?? controller.transform.position;
            var portal = Instantiate(specialPrefabs.portal, position, Quaternion.identity, controller.Spawned);
            portal.name = "Portal";

            var trigger = portal.GetComponent<PortalTrigger>() ?? portal.AddComponent<PortalTrigger>();
            trigger.builder = builder;
        }
    }
}
