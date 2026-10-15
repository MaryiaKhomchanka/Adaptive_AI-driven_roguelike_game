using System.Linq;
using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>Entering the portal finishes the dungeon (spec 10.6). Loading the hub is someone else's job (spec 18).</summary>
    public sealed class PortalTrigger : MonoBehaviour
    {
        public DungeonBuilder builder;
        bool completed;

        void Awake()
        {
            if (GetComponent<Collider2D>() == null)
            {
                var collider = gameObject.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = 0.4f;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (completed || !other.CompareTag("Player")) return;
            var layout = builder != null ? builder.CurrentLayout : null;
            if (layout == null) return;

            completed = true;
            DungeonEvents.RaiseDungeonCompleted(BuildSummary(builder, layout));
        }

        static DungeonSummary BuildSummary(DungeonBuilder builder, Generation.Core.DungeonLayout layout)
        {
            var enemiesLeftAlive = 0;
            var lootLeftUnpicked = 0;
            var roomsCleared = 0;

            foreach (var room in layout.Rooms)
            {
                enemiesLeftAlive += RoomActivity.GetCount(room.Id, RoomMemberKind.Enemy);
                lootLeftUnpicked += RoomActivity.GetCount(room.Id, RoomMemberKind.Loot);
                if (room.Enemies.Count > 0 && RoomActivity.GetCount(room.Id, RoomMemberKind.Enemy) == 0)
                    roomsCleared++;
            }

            var roomsVisited = builder.RoomsById.Values.Count(rc => rc.State != RoomState.Unvisited);

            return new DungeonSummary
            {
                DungeonIndex = layout.DungeonIndex,
                Seed = layout.Seed,
                TimeSeconds = Time.time - DungeonEvents.DungeonStartTime,
                RoomsTotal = layout.Rooms.Count,
                RoomsVisited = roomsVisited,
                RoomsCleared = roomsCleared,
                EnemiesLeftAlive = enemiesLeftAlive,
                LootLeftUnpicked = lootLeftUnpicked,
            };
        }
    }
}
