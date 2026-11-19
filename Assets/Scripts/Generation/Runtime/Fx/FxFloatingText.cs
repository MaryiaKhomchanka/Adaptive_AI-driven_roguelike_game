using TMPro;
using UnityEngine;

namespace Generation.Runtime
{
    public sealed class FxFloatingText : MonoBehaviour
    {
        public TMP_Text label;
        const float Life = 0.7f;
        float age;

        void Update()
        {
            age += Time.deltaTime;
            if (age >= Life)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += Vector3.up * (1.2f * Time.deltaTime);
            var color = label.color;
            color.a = 1f - age / Life;
            label.color = color;
        }
    }
}
