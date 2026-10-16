using System.Collections;
using Generation.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Runtime
{
    public enum RoomState { Unvisited, Active, Frozen }

    /// <summary>
    /// One built room's pieces, plus its fog-of-war state machine (spec 10.2).
    /// Two distinct moments, kept separate on purpose:
    /// - Prewarm: SetActive(true) + fade in. Called early, from the door trigger at the room's edge, purely so
    ///   geometry is rendered before the player actually arrives (no visual pop walking through the opening).
    /// - EnterAsCurrent: spawning, RoomEntered, HasBeenVisited, EntryGrace. Called only once the player's
    ///   position is actually inside this room (RoomTransitionController's 1-tile-inset check), so fog-of-war,
    ///   the minimap and spawns can never happen earlier than the player stepping in.
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
        public Transform DoorTriggers { get; internal set; }

        /// <summary>World-space bounds of this room's floor (union of its chunk footprints), in tiles.</summary>
        public RectInt TileBounds { get; internal set; }

        /// <summary>World position of the Boss/Treasure/Shop/Npc special slot, if this room has one. Used to spawn the portal in the same spot later.</summary>
        public Vector3? SpecialSlotWorldPosition { get; internal set; }

        public RoomState State { get; private set; } = RoomState.Unvisited;

        /// <summary>True once the player has actually stood in this room (not merely prewarmed). Drives the minimap's Visited state.</summary>
        public bool HasBeenVisited { get; private set; }

        public float FadeSeconds = 0.25f;

        float enteredAtTime;
        Coroutine fadeRoutine;

        /// <summary>Set by DungeonBuilder; invoked exactly once, the first time this room is entered as current.</summary>
        public System.Action<RoomController> OnFirstVisit;

        public void Prewarm()
        {
            if (State == RoomState.Active) return;
            gameObject.SetActive(true);
            State = RoomState.Active;
            StartFade(0f, 1f);
        }

        public void EnterAsCurrent()
        {
            Prewarm(); // covers the Start room, which has no door trigger to prewarm it

            var isFirstVisit = !HasBeenVisited;
            HasBeenVisited = true;
            enteredAtTime = Time.time;

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
