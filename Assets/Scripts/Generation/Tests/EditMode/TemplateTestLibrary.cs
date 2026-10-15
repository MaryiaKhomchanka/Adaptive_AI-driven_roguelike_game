using System.Collections.Generic;
using System.IO;
using System.Linq;
using Generation.Core;
using UnityEngine;

namespace Generation.Tests
{
    /// <summary>Loads the real template library from Assets/Generation/Templates for use by every test that needs a library.</summary>
    static class TemplateTestLibrary
    {
        static List<RoomTemplate> cached;

        public static IReadOnlyList<RoomTemplate> Load()
        {
            if (cached != null) return cached;

            var root = Path.Combine(Application.dataPath, "Generation", "Templates");
            cached = Directory.GetFiles(root, "*.txt", SearchOption.AllDirectories)
                .OrderBy(f => f)
                .Select(file => TemplateParser.Parse(File.ReadAllText(file), Path.GetFileNameWithoutExtension(file)))
                .ToList();
            return cached;
        }
    }
}
