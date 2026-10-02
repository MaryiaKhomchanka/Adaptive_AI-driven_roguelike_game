using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    private bool playerInRange;
    private PlayerHealth playerHealth;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerHealth = other.GetComponent<PlayerHealth>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerHealth = null; 
        }
    }

    public void Attack()
    {
        if (playerInRange && playerHealth != null)
        {
            playerHealth.TakeDamage(1);
        }
    }
}
