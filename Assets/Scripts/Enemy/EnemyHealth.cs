using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float deathDuration = 0.8f;
    private int currentHealth;
    private bool dead = false;

    private Animator animator;
    private EnemyMovement movement;

    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        movement = GetComponent<EnemyMovement>();
    }

    public void TakeDamage(int damage)
    {
        if (dead || damage <= 0)
        {
            return;
        }

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }

        else
        {
            animator.SetTrigger("Hurt");
        }
    }

    private void Die()
    {
        dead = true;
        if (movement != null)
        {
            movement.Stop();
            movement.enabled = false;
        }

        EnemyBehaviour enemy = GetComponent<EnemyBehaviour>();
        if (enemy != null)
        {
            enemy.enabled = false; 
        }

        animator.ResetTrigger("Hurt");
        animator.SetTrigger("Dead");
        StartCoroutine(Death());
    }

    private IEnumerator Death()
    {
        yield return new WaitForSeconds(deathDuration);
        Destroy(gameObject);
    }
}
