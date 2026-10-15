using Generation.Core;
using NUnit.Framework;

namespace Generation.Tests
{
    public class MassGenerationTests
    {
        [Test]
        public void ThousandSeeds_AcrossAllDungeonIndices_NeverCrashAndAlwaysValidate()
        {
            for (var dungeonIndex = 1; dungeonIndex <= 10; dungeonIndex++)
            {
                for (var s = 0; s < 1000; s++)
                {
                    var seed = s * 7919 + dungeonIndex; // spread seeds, avoid accidental correlation
                    var request = TestRequests.Default(seed, dungeonIndex);

                    DungeonLayout layout;
                    try
                    {
                        layout = DungeonGenerator.Generate(request, TemplateTestLibrary.Load());
                    }
                    catch (GenerationException ex)
                    {
                        Assert.Fail($"Generation threw for seed={seed}, dungeonIndex={dungeonIndex}: {ex.Message}");
                        return;
                    }

                    var violations = DungeonValidator.Validate(layout);
                    Assert.IsEmpty(violations,
                        $"seed={seed}, dungeonIndex={dungeonIndex}: {string.Join("; ", violations)}");
                }
            }
        }
    }
}
