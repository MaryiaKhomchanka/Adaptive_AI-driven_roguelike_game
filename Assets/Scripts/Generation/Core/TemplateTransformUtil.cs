using System;
using System.Collections.Generic;

namespace Generation.Core
{
    /// <summary>
    /// Applies a Transform2D (mirror east-west, then rotate clockwise by 0/90/180/270) to a template:
    /// to its tile grid, and to the resulting concrete RoomShape (Corridor orientation / LShape cut corner).
    /// </summary>
    public static class TemplateTransformUtil
    {
        public static IEnumerable<Transform2D> AllowedTransforms(TemplateTransformPolicy policy)
        {
            switch (policy)
            {
                case TemplateTransformPolicy.None:
                    yield return Transform2D.Identity;
                    break;
                case TemplateTransformPolicy.RotateOnly:
                    foreach (var d in new[] { 0, 90, 180, 270 })
                        yield return new Transform2D(d, false);
                    break;
                case TemplateTransformPolicy.MirrorOnly:
                    yield return new Transform2D(0, false);
                    yield return new Transform2D(0, true);
                    break;
                case TemplateTransformPolicy.All:
                    foreach (var d in new[] { 0, 90, 180, 270 })
                    foreach (var m in new[] { false, true })
                        yield return new Transform2D(d, m);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(policy));
            }
        }

        public static RoomShape ResultingShape(RoomTemplate template, Transform2D t)
        {
            switch (template.Family)
            {
                case ShapeFamily.Small: return RoomShape.Small;
                case ShapeFamily.Large: return RoomShape.Large;
                case ShapeFamily.Corridor:
                    var swapped = t.RotationDegrees == 90 || t.RotationDegrees == 270;
                    return swapped ? RoomShape.CorridorV : RoomShape.CorridorH;
                case ShapeFamily.LShape:
                    switch (TransformCorner(template.LShapeMissingCorner, t))
                    {
                        case Corner.NE: return RoomShape.L_NoNE;
                        case Corner.NW: return RoomShape.L_NoNW;
                        case Corner.SE: return RoomShape.L_NoSE;
                        default: return RoomShape.L_NoSW;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>Mirror east-west, then rotate the corner vector clockwise by t.RotationDegrees.</summary>
        public static Corner TransformCorner(Corner corner, Transform2D t)
        {
            var (x, y) = CornerVector(corner);
            if (t.Mirrored) x = -x;
            for (var i = 0; i < t.RotationDegrees / 90; i++)
            {
                var nx = y;
                var ny = -x;
                x = nx;
                y = ny;
            }
            return VectorToCorner(x, y);
        }

        static (int x, int y) CornerVector(Corner c)
        {
            switch (c)
            {
                case Corner.NE: return (1, 1);
                case Corner.NW: return (-1, 1);
                case Corner.SE: return (1, -1);
                default: return (-1, -1);
            }
        }

        static Corner VectorToCorner(int x, int y)
        {
            if (x > 0 && y > 0) return Corner.NE;
            if (x < 0 && y > 0) return Corner.NW;
            if (x > 0 && y < 0) return Corner.SE;
            return Corner.SW;
        }

        /// <summary>Mirror east-west, then rotate the tile grid clockwise by t.RotationDegrees. Tiles[tx,ty]: tx=0 west, ty=0 south.</summary>
        public static char[,] ApplyToGrid(RoomTemplate template, Transform2D t, out int newWidth, out int newHeight)
        {
            var width = template.Width;
            var height = template.Height;
            var src = template.Tiles;

            var rotations = (t.RotationDegrees / 90) % 4;
            var swapDims = rotations == 1 || rotations == 3;
            newWidth = swapDims ? height : width;
            newHeight = swapDims ? width : height;

            var dst = new char[newWidth, newHeight];
            for (var tx = 0; tx < width; tx++)
            {
                for (var ty = 0; ty < height; ty++)
                {
                    // Centered, doubled integer coordinates (odd values, since width/height are even) avoid fractions.
                    var cx = 2 * tx - (width - 1);
                    var cy = 2 * ty - (height - 1);
                    if (t.Mirrored) cx = -cx;

                    for (var i = 0; i < rotations; i++)
                    {
                        var nx = cy;
                        var ny = -cx;
                        cx = nx;
                        cy = ny;
                    }

                    var dtx = (cx + (newWidth - 1)) / 2;
                    var dty = (cy + (newHeight - 1)) / 2;
                    dst[dtx, dty] = src[tx, ty];
                }
            }
            return dst;
        }
    }
}
