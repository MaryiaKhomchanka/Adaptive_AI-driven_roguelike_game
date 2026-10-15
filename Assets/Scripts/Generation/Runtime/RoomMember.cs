using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>
    /// Tags a spawned enemy/loot/special object with the room it belongs to (spec 6.4). Only OnDestroy reports
    /// removal (OnDisable fires on freeze and must not count). MarkConsumed() is for content whose "gone" state
    /// isn't destruction (a looted-but-not-destroyed shop, say) - other systems call it; this module never does.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomMember : MonoBehaviour
    {
        public int RoomId { get; private set; }
        public RoomMemberKind Kind { get; private set; }

        bool reported;
        bool initialized;

        public void Initialize(int roomId, RoomMemberKind kind)
        {
            RoomId = roomId;
            Kind = kind;
            initialized = true;
        }

        public void MarkConsumed()
        {
            if (!initialized || reported) return;
            reported = true;
            RoomActivity.ReportRemoved(RoomId, Kind);
        }

        void OnDestroy()
        {
            if (!initialized || reported) return;
            reported = true;
            RoomActivity.ReportRemoved(RoomId, Kind);
        }
    }
}
