using System;
using Generation.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private HeartUI heartUI;
    private int currentHealth;

    private Animator animator;
    private bool isDead = false;

    public static event Action Died;

    private void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();

        heartUI.UpdateHearts(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (isDead || EntryGrace.IsActive)
        {
            return;
        }
        currentHealth -= damage;

        Fx.Flash(gameObject, new Color(1f, 0.35f, 0.35f), 0.15f);
        Fx.Burst(transform.position, new Color(0.9f, 0.1f, 0.15f), 10, 3.5f);
        Fx.Float(transform.position + Vector3.up * 0.8f, $"-{damage}", new Color(1f, 0.3f, 0.3f));
        RoomCamera.Shake(0.25f, 0.25f);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            animator.SetTrigger("Hurt");
        }

        heartUI.UpdateHearts(currentHealth, maxHealth);
    }

    private void Die()
    {
        isDead = true;
        currentHealth = 0;

        GetComponent<PlayerMovements>().DisableMovement();
        animator.SetTrigger("Dead");
        Died?.Invoke();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            TakeDamage(1);
        }
    }
    
}
