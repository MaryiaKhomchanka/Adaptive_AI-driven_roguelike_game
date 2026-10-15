using Generation.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Runtime
{
    public enum RoomState { Unvisited, Active, Frozen }

    /// <summary>
    /// One built room's pieces, plus its fog-of-war state machine (spec 10.2):
    /// Unvisited -(first Activate: spawn)-> Active (-)-> Frozen, Frozen -(Activate)-> Active.
    /// Frozen = SetActive(false) on the whole room object, so physics/animation/AI stop and state is preserved for free.
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

        float enteredAtTime;

        /// <summary>Set by DungeonBuilder; invoked exactly once, the first time this room is activated.</summary>
        public System.Action<RoomController> OnFirstVisit;

        public void Activate()
        {
            var isFirstVisit = State == RoomState.Unvisited;
            gameObject.SetActive(true);
            State = RoomState.Active;
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
            gameObject.SetActive(false);
        }
    }
}
