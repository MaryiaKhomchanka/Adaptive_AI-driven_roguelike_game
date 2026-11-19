using System.Linq;
using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>Entering the portal finishes the dungeon (spec 10.6). Loading the hub is someone else's job (spec 18).</summary>
    public sealed class PortalTrigger : MonoBehaviour
    {
        public DungeonBuilder builder;
        public float radius = 0.4f;
        bool completed;

        // An overlap query rather than OnTriggerEnter2D: the project's Physics2D matrix has no Default x Player entry,
        // so a trigger on the portal's (Default) layer never fires.
        void Update()
        {
            if (completed || Physics2D.OverlapCircle(transform.position, radius, LayerMask.GetMask("Player")) == null) return;
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

            var roomsVisited = builder.RoomsById.Values.Count(rc => rc.HasBeenVisited);

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
