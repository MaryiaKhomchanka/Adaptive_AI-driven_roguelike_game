using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>Placeholder loot behavior (spec 13): destroyed on touch. RoomMember.OnDestroy reports the pickup.</summary>
    public sealed class LootPlaceholderPickup : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player")) Destroy(gameObject);
        }
    }
}
