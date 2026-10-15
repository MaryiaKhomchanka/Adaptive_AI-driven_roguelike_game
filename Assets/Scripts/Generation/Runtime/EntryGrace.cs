using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>
    /// Spec 10.5: flag only. Applying it is PlayerHealth's job (exception 3): add
    /// "if (EntryGrace.IsActive) return;" as the first line of TakeDamage once feature/enemy-system is merged.
    /// Something must call Tick(Time.deltaTime) every frame; RoomTransitionController does that.
    /// </summary>
    public static class EntryGrace
    {
        static float duration = 0.75f;
        public static float Remaining { get; private set; }
        public static bool IsActive => Remaining > 0f;

        public static void Configure(float seconds) => duration = Mathf.Max(0f, seconds);
        public static void Begin() => Remaining = duration;
        public static void Tick(float deltaTime) => Remaining = Mathf.Max(0f, Remaining - deltaTime);
    }
}
