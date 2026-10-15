using System;
using System.Collections.Generic;
using System.Linq;
using Generation.Core;
using UnityEngine;

namespace Generation.Runtime
{
    [Serializable]
    public sealed class EnemySpawnEntry
    {
        public string id;
        public GameObject prefab;
        public float cost = 1f;
        public float weight = 1f;
        public int minDungeonIndex = 1;
    }

    [CreateAssetMenu(menuName = "Generation/Enemy Spawn Table", fileName = "EnemySpawnTable")]
    public sealed class EnemySpawnTable : ScriptableObject
    {
        public List<EnemySpawnEntry> entries = new List<EnemySpawnEntry>();

        public GameObject Find(string id) => entries.FirstOrDefault(e => e.id == id)?.prefab;

        public List<EnemySpawnDef> ToDefs() =>
            entries.Select(e => new EnemySpawnDef { Id = e.id, Cost = e.cost, Weight = e.weight, MinDungeonIndex = e.minDungeonIndex }).ToList();
    }
}
