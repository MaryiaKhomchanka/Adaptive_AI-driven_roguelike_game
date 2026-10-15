using System.Collections.Generic;
using System.Linq;
using Generation.Core;
using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>A list of template TextAssets (spec 8.1), parsed into Generation.Core.RoomTemplate on demand.</summary>
    [CreateAssetMenu(menuName = "Generation/Template Library", fileName = "TemplateLibrary")]
    public sealed class TemplateLibrary : ScriptableObject
    {
        public List<TextAsset> templateFiles = new List<TextAsset>();

        List<RoomTemplate> parsed;

        public IReadOnlyList<RoomTemplate> Templates
        {
            get
            {
                if (parsed == null)
                    parsed = templateFiles.Select(f => TemplateParser.Parse(f.text, f.name)).ToList();
                return parsed;
            }
        }

        void OnValidate() => parsed = null;
    }
}
