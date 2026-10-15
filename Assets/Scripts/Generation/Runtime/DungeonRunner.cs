using Generation.Core;
using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>Drives DungeonBuilder from a request provider, places the player in the Start room and
    /// activates it through RoomTransitionController. Debug-scene entry point for now.</summary>
    public sealed class DungeonRunner : MonoBehaviour
    {
        public DungeonBuilder builder;
        public GenerationConfig config;
        public Transform player;
        public RoomCamera roomCamera;
        public RoomTransitionController transitionController;

        [Header("Run state")]
        public int dungeonIndex = 1;
        public int seed;

        void Start()
        {
            if (seed == 0) seed = System.Environment.TickCount;
            Regenerate();
        }

        public void Regenerate()
        {
            EntryGrace.Configure(config.entryGraceSeconds);

            var provider = new DefaultRequestProvider(config);
            var request = provider.Create(dungeonIndex, seed);
            var layout = builder.Build(request);

            PlacePlayerAtStart(layout);
            transitionController?.ResetForNewDungeon(layout.StartRoomId);
        }

        void PlacePlayerAtStart(DungeonLayout layout)
        {
            if (player == null) return;
            if (!builder.RoomsById.TryGetValue(layout.StartRoomId, out var startRoom)) return;

            var bounds = startRoom.TileBounds;
            var center = new Vector3(bounds.x + bounds.width / 2f, bounds.y + bounds.height / 2f, 0f);
            player.position = center;

            if (roomCamera != null)
            {
                roomCamera.target = player;
                roomCamera.RoomBounds = bounds;
                var camPos = roomCamera.transform.position;
                roomCamera.transform.position = new Vector3(center.x, center.y, camPos.z); // snap, don't lag, right after a (re)generate
            }
        }
    }
}
