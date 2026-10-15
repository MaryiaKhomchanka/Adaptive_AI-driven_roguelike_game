using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Generation.Runtime
{
    public enum PropClass { Obstacle, SolidDecor, FloorDecor }

    [Serializable]
    public sealed class PropEntry
    {
        public PropClass propClass;
        public int width = 1;
        public int height = 1;
        public float weight = 1f;
        public GameObject[] variants;
    }

    /// <summary>Prop footprints and prefab variants per class (spec section 9). Placeholders for now (section 13).</summary>
    [CreateAssetMenu(menuName = "Generation/Prop Library", fileName = "PropLibrary")]
    public sealed class PropLibrary : ScriptableObject
    {
        public List<PropEntry> entries = new List<PropEntry>();

        public IEnumerable<PropEntry> For(PropClass propClass) => entries.Where(e => e.propClass == propClass);
    }
}
