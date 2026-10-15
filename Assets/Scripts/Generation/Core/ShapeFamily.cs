namespace Generation.Core
{
    /// <summary>The 4 template families from spec 8.1, as opposed to RoomShape's 8 concrete oriented footprints.</summary>
    public enum ShapeFamily { Small, Corridor, Large, LShape }

    /// <summary>Which 16x16 quadrant of a 2x2 (Large) footprint is missing in an LShape template/room.</summary>
    public enum Corner { NE, NW, SE, SW }

    public static class ShapeFamilyExtensions
    {
        public static ShapeFamily FamilyOf(this RoomShape shape)
        {
            switch (shape)
            {
                case RoomShape.Small: return ShapeFamily.Small;
                case RoomShape.CorridorH:
                case RoomShape.CorridorV: return ShapeFamily.Corridor;
                case RoomShape.Large: return ShapeFamily.Large;
                default: return ShapeFamily.LShape;
            }
        }
    }
}
