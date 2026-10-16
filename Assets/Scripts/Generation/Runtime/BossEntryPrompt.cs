using UnityEngine;

namespace Generation.Runtime
{
    /// <summary>The "Next is the boss room. Enter?" confirmation shown before using the boss door.</summary>
    public sealed class BossEntryPrompt : MonoBehaviour
    {
        public GameObject panel;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        public void Show()
        {
            if (panel != null) panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
