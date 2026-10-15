using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>
    /// Lazy follow camera (spec 10.4): eases toward the target instead of locking to it, like Hollow Knight /
    /// Binding of Isaac. A dead zone stops it twitching on a step or two, and a max-lag clamp stops it ever
    /// falling far enough behind to lose the target.
    /// Room-bounds clamping (don't show past the current room's edge) is a stage-4 addition once room
    /// tracking exists; this component only does the generic smoothing part for now.
    /// </summary>
    public sealed class RoomCamera : MonoBehaviour
    {
        public Transform target;

        [Tooltip("The target can move this far from the camera's look point before the camera starts following at all.")]
        public float deadZoneRadius = 0.5f;

        [Tooltip("Lower = snappier, higher = lazier/more delayed.")]
        public float smoothTime = 0.25f;

        [Tooltip("The camera is never allowed to lag more than this far behind the target; it snaps closer if it would.")]
        public float maxLag = 4f;

        Vector3 velocity;

        void LateUpdate()
        {
            if (target == null) return;

            var cameraZ = transform.position.z;
            var targetPos = target.position;
            targetPos.z = cameraZ;

            var current = transform.position;
            var delta = targetPos - current;
            var planarDistance = new Vector2(delta.x, delta.y).magnitude;

            if (planarDistance <= deadZoneRadius)
            {
                velocity = Vector3.zero;
                return;
            }

            if (planarDistance > maxLag)
            {
                var direction = delta / planarDistance;
                transform.position = targetPos - direction * maxLag;
                velocity = Vector3.zero;
                return;
            }

            var smoothed = Vector3.SmoothDamp(current, targetPos, ref velocity, smoothTime);
            smoothed.z = cameraZ;
            transform.position = smoothed;
        }
    }
}
