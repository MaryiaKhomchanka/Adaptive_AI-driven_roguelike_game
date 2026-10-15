using Generation.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Runtime
{
    /// <summary>
    /// Holds the built pieces of one room. Freeze/activate state machine and spawning are added in stage 4;
    /// for now this is just the data DungeonBuilder fills in while constructing the room's GameObjects.
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
    }
}
