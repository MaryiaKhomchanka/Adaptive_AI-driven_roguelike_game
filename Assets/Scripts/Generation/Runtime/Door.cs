using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>
    /// One side of a door: always solid (blocks walking), used via the E interaction (DoorInteractor) rather
    /// than walked through. One copy lives in each of the two rooms it connects - same as the old DoorTrigger -
    /// so whichever room is currently active, its own copy is there to use.
    /// </summary>
    public sealed class Door : MonoBehaviour
    {
        public int ThisRoomId;
        public int OtherRoomId;

        /// <summary>Where the player lands just inside the other room, near its matching door.</summary>
        public Vector3 EntryPositionInOtherRoom;
    }
}
