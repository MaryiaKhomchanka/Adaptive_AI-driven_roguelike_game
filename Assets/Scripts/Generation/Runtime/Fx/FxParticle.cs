using UnityEngine;

namespace Generation.Runtime
{
    public sealed class FxParticle : MonoBehaviour
    {
        Vector2 velocity;
        Color color;
        float life;
        float age;
        float startScale;
        float drag;
        float startAlpha;
        SpriteRenderer sprite;

        public void Init(Vector2 startVelocity, Color tint, float lifetime, float scale, float damping, float alpha)
        {
            velocity = startVelocity;
            color = tint;
            life = Mathf.Max(0.01f, lifetime);
            startScale = scale;
            drag = damping;
            startAlpha = alpha;
            sprite = GetComponent<SpriteRenderer>();
            Apply(0f);
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= life)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity *= Mathf.Max(0f, 1f - drag * Time.deltaTime);
            Apply(age / life);
        }

        void Apply(float t)
        {
            sprite.color = new Color(color.r, color.g, color.b, color.a * startAlpha * (1f - t));
            transform.localScale = Vector3.one * (startScale * (1f - 0.5f * t));
        }
    }
}
