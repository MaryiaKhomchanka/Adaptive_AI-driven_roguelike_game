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
            Destroy(gameObject);
        }
    }

    private void DestroyProjectile()
    {
        hit = true;
        Destroy(gameObject);
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, rotation * Time.deltaTime);
    }
}
