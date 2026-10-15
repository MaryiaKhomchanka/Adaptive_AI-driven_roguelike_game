using System.Collections.Generic;

namespace Generation.Core
{
    /// <summary>
    /// System.Collections.Generic.CollectionExtensions.GetValueOrDefault is unavailable under Unity's
    /// legacy ".NET Framework" API compatibility level, so this module provides its own.
    /// </summary>
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue fallback = default)
        {
            return dict.TryGetValue(key, out var value) ? value : fallback;
        }
    }
}
