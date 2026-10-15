using System;

namespace Generation.Core
{
    public enum Dir { N, E, S, W }

    public static class DirExtensions
    {
        public static readonly Dir[] All = { Dir.N, Dir.E, Dir.S, Dir.W };

        public static Dir Opposite(this Dir dir)
        {
            switch (dir)
            {
                case Dir.N: return Dir.S;
                case Dir.S: return Dir.N;
                case Dir.E: return Dir.W;
                case Dir.W: return Dir.E;
                default: throw new ArgumentOutOfRangeException(nameof(dir));
            }
        }

        /// <summary>Unit chunk offset when moving one chunk in this direction.</summary>
        public static Cell Step(this Dir dir)
        {
            switch (dir)
            {
                case Dir.N: return new Cell(0, 1);
                case Dir.S: return new Cell(0, -1);
                case Dir.E: return new Cell(1, 0);
                case Dir.W: return new Cell(-1, 0);
                default: throw new ArgumentOutOfRangeException(nameof(dir));
            }
        }
    }
}
