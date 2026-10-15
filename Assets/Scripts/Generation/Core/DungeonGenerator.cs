using System.Collections.Generic;
using System.Linq;

namespace Generation.Core
{
    /// <summary>
    /// Builds a DungeonLayout from a GenerationRequest: layout -> doors/loops -> typing -> templates -> validate.
    /// Enemy/loot budgets (pipeline step 5) are not part of this stage.
    /// Pure data/algorithms: no UnityEngine, no UnityEngine.Random.
    /// </summary>
    public static class DungeonGenerator
    {
        const string LayoutStep = "layout";
        const string TypingStep = "typing";
        const string LoopsStep = "loops";

        public static DungeonLayout Generate(GenerationRequest request, IReadOnlyList<RoomTemplate> templates)
        {
            string lastFailure = "unknown";
            for (var attempt = 0; attempt < request.MaxGenerationAttempts; attempt++)
            {
                try
                {
                    return GenerateAttempt(request, attempt, templates);
                }
                catch (RetryableGenerationException ex)
                {
                    lastFailure = ex.Message;
                }
            }

            throw new GenerationException(
                $"Dungeon generation failed after {request.MaxGenerationAttempts} attempts. " +
                $"Seed={request.Seed}, DungeonIndex={request.DungeonIndex}. Last failure: {lastFailure}");
        }

        static DungeonLayout GenerateAttempt(GenerationRequest request, int attempt, IReadOnlyList<RoomTemplate> templates)
        {
            var rooms = new List<RoomNode>();
            var doors = new List<DoorLink>();
            var occupied = new Dictionary<Cell, int>();

            BuildLayoutTree(request, attempt, rooms, doors, occupied);
            AssignTypes(request, attempt, rooms, doors);
            AddLoops(request, attempt, rooms, doors, occupied);
            FixCorridors(request, attempt, rooms, doors, occupied);

            var distances = GraphUtils.BfsDistances(rooms, doors, rooms[0].Id);
            foreach (var room in rooms)
                room.DistanceFromStart = distances.TryGetValue(room.Id, out var d) ? d : -1;

            var layout = new DungeonLayout
            {
                Seed = request.Seed,
                DungeonIndex = request.DungeonIndex,
                Rooms = rooms,
                Doors = doors,
                StartRoomId = rooms[0].Id,
                BossRoomId = rooms.First(r => r.Type == RoomType.Boss).Id,
            };

            TemplateSelector.AssignTemplates(request, attempt, layout, templates);

            var violations = DungeonValidator.Validate(layout);
            if (violations.Count > 0)
                throw new RetryableGenerationException(string.Join("; ", violations));

            return layout;
        }

        static void BuildLayoutTree(GenerationRequest request, int attempt, List<RoomNode> rooms, List<DoorLink> doors, Dictionary<Cell, int> occupied)
        {
            var rng = Rng.ForStep(request.Seed, LayoutStep, attempt);

            var start = new RoomNode { Id = 0, Shape = RoomShape.Small, Type = RoomType.Start, Anchor = new Cell(0, 0) };
            start.Chunks = ShapeDefinition.Chunks(start.Shape).Select(o => start.Anchor + o).ToArray();
            rooms.Add(start);
            foreach (var c in start.Chunks) occupied[c] = start.Id;

            var targetCount = rng.NextInt(request.RoomCountMin, request.RoomCountMax + 1);
            var allShapes = request.ShapeWeights.Keys.ToList();

            var safetyBudget = targetCount * 200 + 200;
            while (rooms.Count < targetCount && safetyBudget-- > 0)
            {
                var parent = rng.Pick(rooms);
                var openSlots = OpenSlots(parent, occupied).ToList();
                if (openSlots.Count == 0) continue;

                var slot = rng.Pick(openSlots);
                var neighborChunk = slot.chunk + slot.dir.Step();

                var shape = rng.PickWeighted(allShapes, s => request.ShapeWeights.GetValueOrDefault(s, 0f));
                var shapeChunks = ShapeDefinition.Chunks(shape);

                var anchorCandidates = new List<Cell>();
                foreach (var local in shapeChunks)
                {
                    var anchor = neighborChunk - local;
                    var absoluteChunks = shapeChunks.Select(o => anchor + o).ToArray();
                    if (absoluteChunks.All(c => !occupied.ContainsKey(c)))
                        anchorCandidates.Add(anchor);
                }
                if (anchorCandidates.Count == 0) continue;

                var chosenAnchor = rng.Pick(anchorCandidates);
                var newRoom = new RoomNode
                {
                    Id = rooms.Count,
                    Shape = shape,
                    Anchor = chosenAnchor,
                    Chunks = shapeChunks.Select(o => chosenAnchor + o).ToArray(),
                };
                rooms.Add(newRoom);
                foreach (var c in newRoom.Chunks) occupied[c] = newRoom.Id;
                doors.Add(new DoorLink { RoomA = parent.Id, RoomB = newRoom.Id, ChunkA = slot.chunk, SideA = slot.dir, IsLoop = false });
            }

            if (rooms.Count < targetCount)
                throw new RetryableGenerationException($"Could not place {targetCount} rooms (placed {rooms.Count}).");
        }

        static IEnumerable<(Cell chunk, Dir dir)> OpenSlots(RoomNode room, Dictionary<Cell, int> occupied)
        {
            foreach (var slot in ShapeDefinition.DoorSlots(room.Shape))
            {
                var absChunk = room.Anchor + slot.LocalChunk;
                var neighbor = absChunk + slot.Side.Step();
                if (!occupied.ContainsKey(neighbor))
                    yield return (absChunk, slot.Side);
            }
        }

        static void AssignTypes(GenerationRequest request, int attempt, List<RoomNode> rooms, List<DoorLink> doors)
        {
            var rng = Rng.ForStep(request.Seed, TypingStep, attempt);
            var startId = rooms[0].Id;
            var distances = GraphUtils.BfsDistances(rooms, doors, startId);

            var bossCandidates = rooms
                .Where(r => r.Shape == RoomShape.Large && GraphUtils.DoorCount(r.Id, doors) == 1)
                .ToList();
            if (bossCandidates.Count == 0)
                throw new RetryableGenerationException("No Large room with exactly one door is available to become the Boss room.");

            var maxDistance = bossCandidates.Max(r => distances.GetValueOrDefault(r.Id, -1));
            var farthest = bossCandidates.Where(r => distances.GetValueOrDefault(r.Id, -1) == maxDistance).ToList();
            var boss = rng.Pick(farthest);
            boss.Type = RoomType.Boss;

            var startNeighbors = new HashSet<int>(GraphUtils.NeighborsOf(startId, doors));

            AssignSpecial(rng, rooms, doors, RoomType.Treasure, request.TreasureChance, startId, startNeighbors);
            AssignSpecial(rng, rooms, doors, RoomType.Shop, request.ShopChance, startId, startNeighbors);
            AssignSpecial(rng, rooms, doors, RoomType.Npc, request.NpcChance, startId, startNeighbors);
        }

        static void AssignSpecial(Rng rng, List<RoomNode> rooms, List<DoorLink> doors, RoomType type, float chance, int startId, HashSet<int> startNeighbors)
        {
            if (!rng.Chance(chance)) return;

            var candidates = rooms
                .Where(r => r.Shape == RoomShape.Small && r.Id != startId && r.Type == RoomType.Combat && !startNeighbors.Contains(r.Id))
                .ToList();
            if (candidates.Count == 0) return;

            var deadEnds = candidates.Where(r => GraphUtils.DoorCount(r.Id, doors) == 1).ToList();
            var pool = deadEnds.Count > 0 ? deadEnds : candidates;
            rng.Pick(pool).Type = type;
        }

        static void AddLoops(GenerationRequest request, int attempt, List<RoomNode> rooms, List<DoorLink> doors, Dictionary<Cell, int> occupied)
        {
            var rng = Rng.ForStep(request.Seed, LoopsStep, attempt);
            var roomsById = rooms.ToDictionary(r => r.Id);

            foreach (var pair in AdjacentRoomPairs(occupied))
            {
                var (roomAId, roomBId) = pair.Key;
                if (GraphUtils.DoorExistsBetween(roomAId, roomBId, doors)) continue;

                var roomA = roomsById[roomAId];
                var roomB = roomsById[roomBId];
                if (roomA.Type == RoomType.Boss || roomB.Type == RoomType.Boss) continue;
                if (roomA.Type.IsSpecial() || roomB.Type.IsSpecial()) continue;

                if (rng.Chance(request.LoopChance))
                {
                    var slot = rng.Pick(pair.Value);
                    doors.Add(new DoorLink { RoomA = roomAId, RoomB = roomBId, ChunkA = slot.chunkA, SideA = slot.dir, IsLoop = true });
                }
            }
        }

        /// <summary>All chunk-adjacent room pairs, each with its candidate door slots, in a seed-independent deterministic order.</summary>
        static List<KeyValuePair<(int, int), List<(Cell chunkA, Dir dir)>>> AdjacentRoomPairs(Dictionary<Cell, int> occupied)
        {
            var result = new Dictionary<(int, int), List<(Cell chunkA, Dir dir)>>();
            foreach (var chunk in occupied.Keys.OrderBy(c => c.X).ThenBy(c => c.Y))
            {
                var roomA = occupied[chunk];
                foreach (var dir in DirExtensions.All)
                {
                    var neighbor = chunk + dir.Step();
                    if (!occupied.TryGetValue(neighbor, out var roomB) || roomB == roomA) continue;

                    (Cell chunkA, Dir dir) slot = roomA < roomB ? (chunk, dir) : (neighbor, dir.Opposite());
                    var key = roomA < roomB ? (roomA, roomB) : (roomB, roomA);
                    if (!result.TryGetValue(key, out var list))
                        result[key] = list = new List<(Cell, Dir)>();
                    if (!list.Contains(slot))
                        list.Add(slot);
                }
            }
            return result.OrderBy(kv => kv.Key.Item1).ThenBy(kv => kv.Key.Item2).ToList();
        }

        static void FixCorridors(GenerationRequest request, int attempt, List<RoomNode> rooms, List<DoorLink> doors, Dictionary<Cell, int> occupied)
        {
            // Deterministic by construction: candidates are tried in Dir order, first fixable neighbor wins.
            var roomsById = rooms.ToDictionary(r => r.Id);

            foreach (var room in rooms.Where(r => ShapeDefinition.IsCorridor(r.Shape)))
            {
                var chunkHasDoor = new HashSet<Cell>();
                var neighborIds = new HashSet<int>();
                foreach (var door in doors)
                {
                    if (door.RoomA == room.Id) { chunkHasDoor.Add(door.ChunkA); neighborIds.Add(door.RoomB); }
                    else if (door.RoomB == room.Id) { chunkHasDoor.Add(door.ChunkA + door.SideA.Step()); neighborIds.Add(door.RoomA); }
                }

                foreach (var chunk in room.Chunks.OrderBy(c => c.X).ThenBy(c => c.Y))
                {
                    if (chunkHasDoor.Contains(chunk)) continue;

                    var fixedIt = false;
                    foreach (var dir in DirExtensions.All)
                    {
                        var neighbor = chunk + dir.Step();
                        if (!occupied.TryGetValue(neighbor, out var otherId) || otherId == room.Id) continue;
                        var other = roomsById[otherId];
                        if (other.Type == RoomType.Boss || other.Type.IsSpecial()) continue;
                        if (GraphUtils.DoorExistsBetween(room.Id, otherId, doors)) continue;

                        doors.Add(new DoorLink { RoomA = room.Id, RoomB = otherId, ChunkA = chunk, SideA = dir, IsLoop = true });
                        chunkHasDoor.Add(chunk);
                        neighborIds.Add(otherId);
                        fixedIt = true;
                        break;
                    }
                    if (!fixedIt)
                        throw new RetryableGenerationException($"Corridor room {room.Id}: chunk {chunk} is a dead end with no fixable neighbor.");
                }

                if (neighborIds.Count < 2)
                    throw new RetryableGenerationException($"Corridor room {room.Id} has fewer than 2 distinct neighbors after fixups.");
            }
        }
    }
}
