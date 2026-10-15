using System.Collections.Generic;

namespace Generation.Core
{
    public enum TemplateTransformPolicy { None, All, RotateOnly, MirrorOnly }

    /// <summary>A parsed room template: header metadata plus its tile grid. Tiles[tx, ty] with tx=0 west, ty=0 south (matches world chunk axes: X+ East, Y+ North).</summary>
    public sealed class RoomTemplate
    {
        public string Name;
        public ShapeFamily Family;
        public HashSet<RoomType> Types;
        public List<string> Tags = new List<string>();
        public float Weight = 1f;
        public TemplateTransformPolicy TransformPolicy = TemplateTransformPolicy.All;
        public int Width;
        public int Height;
        public char[,] Tiles;

        /// <summary>Only meaningful when Family == LShape: which quadrant of the full 2x2 footprint is marked X.</summary>
        public Corner LShapeMissingCorner;
    }
}
