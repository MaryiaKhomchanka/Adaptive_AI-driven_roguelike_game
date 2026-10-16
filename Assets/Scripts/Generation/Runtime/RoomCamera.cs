using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>
    /// Lazy follow camera (spec 10.4): eases toward the target instead of locking to it, like Hollow Knight /
    /// Binding of Isaac. A dead zone stops it twitching on a step or two, and a max-lag clamp stops it ever
    /// falling far enough behind to lose the target. Once RoomBounds is set (by RoomTransitionController),
    /// the result is also clamped to the current room, centering on any axis where the room is smaller than the view.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class RoomCamera : MonoBehaviour
    {
        public Transform target;

        [Tooltip("The target can move this far from the camera's look point before the camera starts following at all.")]
        public float deadZoneRadius = 0.5f;

        [Tooltip("Lower = snappier, higher = lazier/more delayed.")]
        public float smoothTime = 0.25f;

        [Tooltip("The camera is never allowed to lag more than this far behind the target; it snaps closer if it would.")]
        public float maxLag = 4f;

        /// <summary>Current room's tile bounds, in world units (1 tile = 1 unit). Null = no clamping (e.g. before the first room is active). Set at runtime only, not inspector-editable.</summary>
        [System.NonSerialized] public RectInt? RoomBounds;

        Vector3 velocity;
        Camera cam;

        void Awake() => cam = GetComponent<Camera>();

        void LateUpdate()
        {
            if (target == null) return;

            var cameraZ = transform.position.z;
            var targetPos = target.position;
            targetPos.z = cameraZ;

            var current = transform.position;
            var delta = targetPos - current;
            var planarDistance = new Vector2(delta.x, delta.y).magnitude;

            Vector3 desired;
            if (planarDistance <= deadZoneRadius)
            {
                desired = current;
                velocity = Vector3.zero;
            }
            else if (planarDistance > maxLag)
            {
                desired = targetPos - delta / planarDistance * maxLag;
                velocity = Vector3.zero;
            }
            else
            {
                desired = Vector3.SmoothDamp(current, targetPos, ref velocity, smoothTime);
            }
            desired.z = cameraZ;

            transform.position = ClampToRoom(desired);
        }

        Vector3 ClampToRoom(Vector3 position)
        {
            if (!RoomBounds.HasValue) return position;
            var bounds = RoomBounds.Value;

            var halfHeight = cam.orthographicSize;
            var halfWidth = halfHeight * cam.aspect;

            position.x = ClampAxis(position.x, bounds.xMin, bounds.xMax, halfWidth);
            position.y = ClampAxis(position.y, bounds.yMin, bounds.yMax, halfHeight);
            return position;
        }

        static float ClampAxis(float value, float min, float max, float halfExtent)
        {
            var lo = min + halfExtent;
            var hi = max - halfExtent;
            return lo > hi ? (min + max) / 2f : Mathf.Clamp(value, lo, hi); // room smaller than the view on this axis: center on it
        }
    }
}
