using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageInterval = 1f;

    private float nextDamageTime;
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time < nextDamageTime)
        {
            return;
        }
        PlayerHealth player = collision.gameObject.GetComponentInParent<PlayerHealth>();

        if (player == null)
        {
            return;
        }

        player.TakeDamage(damage);

        nextDamageTime = Time.time + damageInterval; 
    }
}
