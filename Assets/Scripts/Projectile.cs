using Generation.Runtime;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float livingTime = 3f;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float rotation = 180f;

    private Rigidbody2D rb;
    private LayerMask targetLayers;
    private int damage;
    private bool hit;
    private float nextTrail;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Shoot(Vector2 direction, float speed, int shootDamage, LayerMask targets)
    {
        damage = shootDamage;
        targetLayers = targets;

        rb.linearVelocity = direction.normalized * speed;
        Destroy(gameObject, livingTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hit)
        {
            return;
        }

        int otherLayer = 1 << other.gameObject.layer;

        if ((obstacleLayers.value & otherLayer) != 0)
        {
            DestroyProjectile();
            return;
        }

        if ((targetLayers.value & otherLayer) == 0)
        {
            return;
        }

        IDamageable target = other.GetComponentInParent<IDamageable>();

        if (target != null)
        {
            hit = true;
            target.TakeDamage(damage);
            Fx.Burst(transform.position, new Color(1f, 0.9f, 0.5f), 5, 3f, 0.3f);
            Destroy(gameObject);
        }
    }

    private void DestroyProjectile()
    {
        hit = true;
        Fx.Burst(transform.position, new Color(0.85f, 0.85f, 0.9f), 6, 3f, 0.3f);
        Destroy(gameObject);
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, rotation * Time.deltaTime);

        if (Time.time >= nextTrail)
        {
            nextTrail = Time.time + 0.05f;
            Fx.Spawn(transform.position, Vector2.zero, new Color(1f, 0.9f, 0.6f), 0.25f, 1f, DungeonBuilder.ActorSortingOrder + 1);
        }
    }
}
