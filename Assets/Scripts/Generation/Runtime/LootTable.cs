using System;
using System.Collections.Generic;
using System.Linq;
using Generation.Core;
using UnityEngine;

namespace Generation.Runtime
{
    [Serializable]
    public sealed class LootEntry
    {
        public string id;
        public GameObject prefab;
        public float weight = 1f;
    }

    [CreateAssetMenu(menuName = "Generation/Loot Table", fileName = "LootTable")]
    public sealed class LootTable : ScriptableObject
    {
        public List<LootEntry> entries = new List<LootEntry>();

        public GameObject Find(string id) => entries.FirstOrDefault(e => e.id == id)?.prefab;

        public List<LootSpawnDef> ToDefs() =>
            entries.Select(e => new LootSpawnDef { Id = e.id, Weight = e.weight }).ToList();
    }
}
