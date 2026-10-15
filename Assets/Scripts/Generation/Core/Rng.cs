using System;
using System.Collections.Generic;

namespace Generation.Core
{
    /// <summary>
    /// Deterministic RNG wrapper over System.Random. Never backed by UnityEngine.Random.
    /// Each pipeline step derives its own seed from (runSeed, stepName, attempt) so that
    /// changing one step's logic does not shift the random stream of any other step.
    /// </summary>
    public sealed class Rng
    {
        readonly Random random;

        public Rng(int seed)
        {
            random = new Random(seed);
        }

        public static int DeriveSeed(int seed, string stepName, int attempt = 0)
        {
            unchecked
            {
                uint hash = 2166136261;
                hash = HashInt(hash, seed);
                hash = HashString(hash, stepName);
                hash = HashInt(hash, attempt);
                return (int)hash;
            }
        }

        public static Rng ForStep(int seed, string stepName, int attempt = 0) =>
            new Rng(DeriveSeed(seed, stepName, attempt));

        static uint HashInt(uint hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                hash *= 16777619;
                return hash;
            }
        }

        static uint HashString(uint hash, string value)
        {
            unchecked
            {
                foreach (var c in value)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return hash;
            }
        }

        public int NextInt(int minInclusive, int maxExclusive) => random.Next(minInclusive, maxExclusive);

        public double NextDouble() => random.NextDouble();

        public bool Chance(float probability) => random.NextDouble() < probability;

        public T Pick<T>(IReadOnlyList<T> items)
        {
            if (items.Count == 0) throw new InvalidOperationException("Cannot pick from an empty collection.");
            return items[random.Next(items.Count)];
        }

        public T PickWeighted<T>(IReadOnlyList<T> items, Func<T, float> weightFn)
        {
            float total = 0f;
            foreach (var item in items) total += Math.Max(0f, weightFn(item));
            if (total <= 0f) return Pick(items);

            var roll = random.NextDouble() * total;
            double cumulative = 0;
            foreach (var item in items)
            {
                cumulative += Math.Max(0f, weightFn(item));
                if (roll < cumulative) return item;
            }
            return items[items.Count - 1];
        }
    }
}
