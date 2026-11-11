using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>Placeholder loot behavior (spec 13): destroyed when the player walks onto it. RoomMember.OnDestroy reports the pickup.
    /// Polls with an overlap query instead of OnTriggerEnter2D: the project's Physics2D collision matrix has no
    /// Default x Player entry, so a trigger on a Default-layer pickup never fires.</summary>
    public sealed class LootPlaceholderPickup : MonoBehaviour
    {
        public float pickupRadius = 0.3f;

        static int playerMask = -1;

        void Update()
        {
            if (playerMask < 0) playerMask = LayerMask.GetMask("Player");
            if (Physics2D.OverlapCircle(transform.position, pickupRadius, playerMask) != null) Destroy(gameObject);
        }
    }
}
