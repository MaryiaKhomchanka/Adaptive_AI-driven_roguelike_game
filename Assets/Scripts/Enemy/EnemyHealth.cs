using UnityEngine;
using System.Collections;
using Generation.Runtime;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float deathDuration = 0.8f;
    [SerializeField] private Color fxColor = new Color(0.4f, 0.65f, 1f); // hit/death bits; matches the blue slime
    private int currentHealth;
    private bool dead = false;

    private Animator animator;
    private EnemyMovement movement;

    public bool IsDead => dead;
    public float HealthFraction => (float)currentHealth / maxHealth;

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
        Fx.Flash(gameObject, new Color(1f, 0.45f, 0.45f), 0.1f);
        Fx.Burst(transform.position, fxColor, 6);
        Fx.Float(transform.position + Vector3.up * 0.6f, $"-{damage}", Color.white);

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
        Fx.Burst(transform.position, fxColor, 16, 4.5f, 0.6f);
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
