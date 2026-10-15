using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>
    /// Sits in the 2-tile door opening (spec 10.3). One copy lives in each of the two rooms it connects, so
    /// at least one copy stays active even while the other room is frozen - doors never close.
    /// </summary>
    public sealed class DoorTrigger : MonoBehaviour
    {
        public int RoomAId;
        public int RoomBId;
        public RoomTransitionController Controller;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            Controller?.NotifyDoorCrossed(RoomAId, RoomBId);
        }
    }
}
