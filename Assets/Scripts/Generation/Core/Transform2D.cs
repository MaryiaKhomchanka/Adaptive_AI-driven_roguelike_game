namespace Generation.Core
{
    /// <summary>Rotation/mirror applied to a room template when it is stamped into a room. Used from stage 2 on.</summary>
    public readonly struct Transform2D
    {
        public readonly int RotationDegrees; // 0, 90, 180 or 270, clockwise
        public readonly bool Mirrored;

        public Transform2D(int rotationDegrees, bool mirrored)
        {
            RotationDegrees = rotationDegrees;
            Mirrored = mirrored;
        }

        public static readonly Transform2D Identity = new Transform2D(0, false);
    }
}
