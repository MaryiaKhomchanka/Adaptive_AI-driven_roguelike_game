using System.Collections;
using Generation.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Runtime
{
    public enum RoomState { Unvisited, Active, Frozen }

    /// <summary>
    /// One built room's pieces, plus its fog-of-war state machine (spec 10.2): Unvisited -(Activate: spawn on
    /// first visit)-> Active (-)-> Frozen, Frozen -(Activate)-> Active. Transitions are instant door-interaction
    /// teleports (RoomTransitionController.Teleport), so Activate/Freeze fade the room in/out rather than
    /// popping it, but there's no longer an earlier "approaching" phase to pre-warm against.
    /// Freeze fades out, then SetActive(false); physics/animation/AI stop once inactive and state is preserved for free.
    /// </summary>
    public sealed class RoomController : MonoBehaviour
    {
        public RoomNode Room { get; internal set; }
        public Tilemap Floor { get; internal set; }
        public Tilemap FloorDecor { get; internal set; }
        public Tilemap Walls { get; internal set; }
        public Transform Props { get; internal set; }
        public Transform Spawned { get; internal set; }
        public Transform Doors { get; internal set; }

        /// <summary>World-space bounds of this room's floor (union of its chunk footprints), in tiles.</summary>
        public RectInt TileBounds { get; internal set; }

        /// <summary>World position of the Boss/Treasure/Shop/Npc special slot, if this room has one. Used to spawn the portal in the same spot later.</summary>
        public Vector3? SpecialSlotWorldPosition { get; internal set; }

        public RoomState State { get; private set; } = RoomState.Unvisited;

        /// <summary>True once the player has entered this room. Drives the minimap's Visited state.</summary>
        public bool HasBeenVisited { get; private set; }

        public float FadeSeconds = 0.25f;

        float enteredAtTime;
        Coroutine fadeRoutine;

        /// <summary>Set by DungeonBuilder; invoked exactly once, the first time this room is activated.</summary>
        public System.Action<RoomController> OnFirstVisit;

        public void Activate()
        {
            var isFirstVisit = !HasBeenVisited;
            HasBeenVisited = true;

            gameObject.SetActive(true);
            State = RoomState.Active;
            enteredAtTime = Time.time;
            StartFade(0f, 1f);

            if (isFirstVisit) OnFirstVisit?.Invoke(this);

            DungeonEvents.RaiseRoomEntered(Room.Id, Room.Type, isFirstVisit);
            EntryGrace.Begin();
        }

        public void Freeze()
        {
            if (State != RoomState.Active) return;
            DungeonEvents.RaiseRoomExited(Room.Id, Time.time - enteredAtTime);
            State = RoomState.Frozen;
            StartFade(1f, 0f, () => gameObject.SetActive(false));
        }

        void StartFade(float from, float to, System.Action onComplete = null)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(from, to, onComplete));
        }

        IEnumerator FadeRoutine(float from, float to, System.Action onComplete)
        {
            var tilemaps = GetComponentsInChildren<Tilemap>(true);
            var sprites = GetComponentsInChildren<SpriteRenderer>(true);

            SetAlpha(tilemaps, sprites, from);
            var t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.deltaTime;
                SetAlpha(tilemaps, sprites, Mathf.Lerp(from, to, t / FadeSeconds));
                yield return null;
            }
            SetAlpha(tilemaps, sprites, to);
            fadeRoutine = null;
            onComplete?.Invoke();
        }

        static void SetAlpha(Tilemap[] tilemaps, SpriteRenderer[] sprites, float alpha)
        {
            foreach (var tilemap in tilemaps)
            {
                var c = tilemap.color;
                c.a = alpha;
                tilemap.color = c;
            }
            foreach (var sprite in sprites)
            {
                var c = sprite.color;
                c.a = alpha;
                sprite.color = c;
            }
        }
    }
}
