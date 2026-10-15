using System.Collections.Generic;
using System.Linq;

namespace Generation.Core
{
    /// <summary>Pipeline step 4 (spec 7.4): pick a template + transform for each room.</summary>
    public static class TemplateSelector
    {
        public static void AssignTemplates(GenerationRequest request, int attempt, DungeonLayout layout, IReadOnlyList<RoomTemplate> library)
        {
            var rng = Rng.ForStep(request.Seed, "templates", attempt);
            var adjacency = GraphUtils.BuildAdjacency(layout.Rooms, layout.Doors);
            var chosenTemplateByRoom = new Dictionary<int, string>();

            foreach (var room in layout.Rooms.OrderBy(r => r.Id))
            {
                var candidates = Candidates(request, library, room.Shape, room.Type).ToList();
                if (candidates.Count == 0)
                    throw new RetryableGenerationException($"No template available for room {room.Id} (shape={room.Shape}, type={room.Type}).");

                var neighborTemplates = adjacency.TryGetValue(room.Id, out var neighbors)
                    ? new HashSet<string>(neighbors.Where(chosenTemplateByRoom.ContainsKey).Select(n => chosenTemplateByRoom[n]))
                    : new HashSet<string>();

                var preferred = candidates.Where(c => !neighborTemplates.Contains(c.template.Name)).ToList();
                var pool = preferred.Count > 0 ? preferred : candidates;

                var pick = rng.PickWeighted(pool, c => Weight(request, c.template));
                room.TemplateId = IndexOf(library, pick.template);
                room.TemplateTransform = pick.transform;
                chosenTemplateByRoom[room.Id] = pick.template.Name;
            }
        }

        static IEnumerable<(RoomTemplate template, Transform2D transform)> Candidates(
            GenerationRequest request, IReadOnlyList<RoomTemplate> library, RoomShape shape, RoomType type)
        {
            var family = shape.FamilyOf();
            foreach (var template in library)
            {
                if (template.Family != family) continue;
                if (!template.Types.Contains(type)) continue;
                foreach (var t in TemplateTransformUtil.AllowedTransforms(template.TransformPolicy))
                {
                    if (TemplateTransformUtil.ResultingShape(template, t) == shape)
                        yield return (template, t);
                }
            }
        }

        static int IndexOf(IReadOnlyList<RoomTemplate> library, RoomTemplate template)
        {
            for (var i = 0; i < library.Count; i++)
                if (library[i] == template)
                    return i;
            return -1;
        }

        static float Weight(GenerationRequest request, RoomTemplate template)
        {
            var weight = template.Weight;
            foreach (var tag in template.Tags)
                weight *= request.TemplateTagWeights.GetValueOrDefault(tag, 1f);
            return weight;
        }
    }
}
