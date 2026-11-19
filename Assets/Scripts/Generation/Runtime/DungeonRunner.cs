using System.Collections;
using Generation.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [Tooltip("Off when something else (the game flow's victory screen) decides what happens after the portal.")]
        public bool advanceOnCompletion = true;

        /// <summary>Set by the game flow before loading the dungeon scene; 0 = use the inspector value.</summary>
        public static int StartDungeonIndex;

        [Header("Run state")]
        public int dungeonIndex = 1;
        public int seed;

        void OnEnable() => DungeonEvents.DungeonCompleted += OnDungeonCompleted;
        void OnDisable() => DungeonEvents.DungeonCompleted -= OnDungeonCompleted;

        // Deferred a frame: this fires from the portal's physics callback, and Regenerate destroys/creates colliders.
        void OnDungeonCompleted(DungeonSummary summary)
        {
            if (advanceOnCompletion) StartCoroutine(Advance());
        }

        IEnumerator Advance()
        {
            yield return null;
            if (!string.IsNullOrEmpty(config.hubSceneName) && Application.CanStreamedLevelBeLoaded(config.hubSceneName))
            {
                SceneManager.LoadScene(config.hubSceneName);
                yield break;
            }

            if (!string.IsNullOrEmpty(config.hubSceneName))
                Debug.LogWarning($"[Dungeon] hub scene '{config.hubSceneName}' is not in Build Settings; generating the next dungeon instead.");
            dungeonIndex++;
            seed = System.Environment.TickCount;
            Regenerate();
        }

        void Start()
        {
            if (StartDungeonIndex > 0) dungeonIndex = StartDungeonIndex;
            if (seed == 0) seed = System.Environment.TickCount;
            Regenerate();
        }

        public void Regenerate()
        {
            EntryGrace.Configure(config.entryGraceSeconds);
            DungeonLighting.Setup(config, player, builder.wallShadows);

            var provider = new DefaultRequestProvider(config);
            var request = provider.Create(dungeonIndex, seed);
            var layout = builder.Build(request);

            PlacePlayerAtStart(layout);
            transitionController?.ResetForNewDungeon(layout.StartRoomId);
        }

        void PlacePlayerAtStart(DungeonLayout layout)
        {
            if (player == null) return;
            // the scene's player may keep sorting order 0 (same as the floor), which hides it under tiles by Y-sort
            foreach (var sr in player.GetComponentsInChildren<SpriteRenderer>())
                sr.sortingOrder = DungeonBuilder.ActorSortingOrder;
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
