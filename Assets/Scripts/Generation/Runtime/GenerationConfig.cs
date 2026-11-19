using System.Collections.Generic;
using Generation.Core;
using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>[tunable] defaults from spec section 13, as a ScriptableObject so values live in the project, not in code.</summary>
    [CreateAssetMenu(menuName = "Generation/Generation Config", fileName = "GenerationConfig")]
    public sealed class GenerationConfig : ScriptableObject
    {
        [Header("Room count: min/max = base + perIndex * dungeonIndex, capped")]
        public int roomCountBaseMin = 7;
        public int roomCountBaseMax = 9;
        public int roomCountPerIndex = 2;
        public int roomCountCap = 28;

        [Header("Shape weights (families; Corridor/LShape split evenly across their orientations)")]
        [Range(0, 1)] public float shapeWeightSmall = 0.45f;
        [Range(0, 1)] public float shapeWeightCorridor = 0.2f;
        [Range(0, 1)] public float shapeWeightLarge = 0.15f;
        [Range(0, 1)] public float shapeWeightLShape = 0.2f;

        [Header("Doors & special rooms")]
        [Range(0, 1)] public float loopChance = 0.15f;
        [Range(0, 1)] public float treasureChance = 0.30f;
        [Range(0, 1)] public float shopChance = 0.25f;
        [Range(0, 1)] public float npcChance = 0.20f;

        [Header("Budgets")]
        public float baseThreatPerChunk = 2f;
        public float lootPerChunkBase = 0.5f;

        [Header("Rooms & camera")]
        [Range(0.5f, 1f)] public float entryGraceSeconds = 0.75f;

        [Header("Lighting")]
        [Range(0, 1)] public float ambientLight = 0.4f;
        public Color torchColor = new Color(1f, 0.85f, 0.6f);
        public float torchIntensity = 1.2f;
        public float torchRadius = 7f;

        [Header("Retro light")]
        [Tooltip("Light 'pixels' per world unit (8 = 4x4 art pixels at 32 PPU). 0 = smooth light.")]
        public float lightPixelsPerUnit = 8f;
        [Tooltip("Brightness levels per 1.0 of light: fewer = chunkier ladder. 0 = smooth light.")]
        public float lightSteps = 5f;

        [Header("After a dungeon")]
        [Tooltip("Scene to load when the portal is entered. Empty, or not in Build Settings: generate the next dungeon in place.")]
        public string hubSceneName = "";

        [Header("Generation")]
        public int maxGenerationAttempts = 50;

        public float DungeonScale(int dungeonIndex) => 1f + 0.2f * (dungeonIndex - 1);

        public Dictionary<RoomShape, float> ToShapeWeights()
        {
            return new Dictionary<RoomShape, float>
            {
                { RoomShape.Small, shapeWeightSmall },
                { RoomShape.CorridorH, shapeWeightCorridor / 2f },
                { RoomShape.CorridorV, shapeWeightCorridor / 2f },
                { RoomShape.Large, shapeWeightLarge },
                { RoomShape.L_NoNE, shapeWeightLShape / 4f },
                { RoomShape.L_NoNW, shapeWeightLShape / 4f },
                { RoomShape.L_NoSE, shapeWeightLShape / 4f },
                { RoomShape.L_NoSW, shapeWeightLShape / 4f },
            };
        }
    }
}
