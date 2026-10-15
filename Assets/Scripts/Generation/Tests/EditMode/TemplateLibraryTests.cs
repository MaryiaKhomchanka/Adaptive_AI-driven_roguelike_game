using System.Collections.Generic;
using System.Linq;
using Generation.Core;
using NUnit.Framework;

namespace Generation.Tests
{
    public class TemplateLibraryTests
    {
        [Test]
        public void AllRealTemplateFiles_ValidateUnderEveryAllowedTransform()
        {
            var templates = TemplateTestLibrary.Load();
            Assert.Greater(templates.Count, 0, "No template files found under Assets/Generation/Templates.");

            var errors = new List<string>();
            foreach (var t in templates)
                errors.AddRange(TemplateValidator.ValidateAllTransforms(t));

            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        static readonly (RoomShape shape, RoomType type)[] RequiredCombinations =
        {
            (RoomShape.Small, RoomType.Start),
            (RoomShape.Small, RoomType.Combat),
            (RoomShape.Small, RoomType.Treasure),
            (RoomShape.Small, RoomType.Shop),
            (RoomShape.Small, RoomType.Npc),
            (RoomShape.CorridorH, RoomType.Combat),
            (RoomShape.CorridorV, RoomType.Combat),
            (RoomShape.Large, RoomType.Combat),
            (RoomShape.Large, RoomType.Boss),
            (RoomShape.L_NoNE, RoomType.Combat),
            (RoomShape.L_NoNW, RoomType.Combat),
            (RoomShape.L_NoSE, RoomType.Combat),
            (RoomShape.L_NoSW, RoomType.Combat),
        };

        [Test]
        public void StarterSet_CoversEveryShapeTypeCombinationTheGeneratorCanProduce()
        {
            var templates = TemplateTestLibrary.Load();
            foreach (var (shape, type) in RequiredCombinations)
            {
                var family = shape.FamilyOf();
                var hasCandidate = templates.Any(t =>
                    t.Family == family && t.Types.Contains(type) &&
                    TemplateTransformUtil.AllowedTransforms(t.TransformPolicy)
                        .Any(tr => TemplateTransformUtil.ResultingShape(t, tr) == shape));
                Assert.IsTrue(hasCandidate, $"No template covers shape={shape}, type={type}");
            }
        }
    }
}
