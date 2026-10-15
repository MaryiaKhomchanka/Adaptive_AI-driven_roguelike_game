using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>Spec 6.3: chest/vendor/npc/boss/portal placeholders, swapped for real prefabs later without code changes.</summary>
    [CreateAssetMenu(menuName = "Generation/Special Prefabs", fileName = "SpecialPrefabs")]
    public sealed class SpecialPrefabs : ScriptableObject
    {
        public GameObject treasureChest;
        public GameObject shopVendor;
        public GameObject npc;
        public GameObject boss;
        public GameObject portal;

        public GameObject ForId(string id)
        {
            switch (id)
            {
                case "boss": return boss;
                case "treasure": return treasureChest;
                case "shop": return shopVendor;
                case "npc": return npc;
                default: return null;
            }
        }
    }
}
