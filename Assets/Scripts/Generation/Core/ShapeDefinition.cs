using System.Collections.Generic;

namespace Generation.Core
{
    /// <summary>A possible door location: one chunk of a room, local to its anchor, and the external side it sits on.</summary>
    public readonly struct DoorSlot
    {
        public readonly Cell LocalChunk;
        public readonly Dir Side;

        public DoorSlot(Cell localChunk, Dir side)
        {
            LocalChunk = localChunk;
            Side = side;
        }
    }

    /// <summary>Static chunk footprints for each room shape, anchored at (0,0), plus their door candidate slots.</summary>
    public static class ShapeDefinition
    {
        // Chunk coordinate convention: X+ = East, Y+ = North.
        static readonly Dictionary<RoomShape, Cell[]> ChunkOffsets = new Dictionary<RoomShape, Cell[]>
        {
            { RoomShape.Small, new[] { new Cell(0, 0) } },
            { RoomShape.CorridorH, new[] { new Cell(0, 0), new Cell(1, 0) } },
            { RoomShape.CorridorV, new[] { new Cell(0, 0), new Cell(0, 1) } },
            { RoomShape.Large, new[] { new Cell(0, 0), new Cell(1, 0), new Cell(0, 1), new Cell(1, 1) } },
            // Large with one corner chunk removed; corner named by the removed direction (NE/NW/SE/SW).
            { RoomShape.L_NoNE, new[] { new Cell(0, 0), new Cell(1, 0), new Cell(0, 1) } },
            { RoomShape.L_NoNW, new[] { new Cell(0, 0), new Cell(1, 0), new Cell(1, 1) } },
            { RoomShape.L_NoSE, new[] { new Cell(0, 0), new Cell(0, 1), new Cell(1, 1) } },
            { RoomShape.L_NoSW, new[] { new Cell(1, 0), new Cell(0, 1), new Cell(1, 1) } },
        };

        public static IReadOnlyList<Cell> Chunks(RoomShape shape) => ChunkOffsets[shape];

        public static bool IsCorridor(RoomShape shape) => shape == RoomShape.CorridorH || shape == RoomShape.CorridorV;

        public static bool IsLShape(RoomShape shape) =>
            shape == RoomShape.L_NoNE || shape == RoomShape.L_NoNW || shape == RoomShape.L_NoSE || shape == RoomShape.L_NoSW;

        /// <summary>All door-candidate slots for a shape: one per external side of each chunk (side with no same-room neighbor).</summary>
        public static IEnumerable<DoorSlot> DoorSlots(RoomShape shape)
        {
            var chunks = ChunkOffsets[shape];
            var set = new HashSet<Cell>(chunks);
            foreach (var c in chunks)
            {
                foreach (var dir in DirExtensions.All)
                {
                    var neighbor = c + dir.Step();
                    if (!set.Contains(neighbor))
                        yield return new DoorSlot(c, dir);
                }
            }
        }
    }
}
