using UnityEngine;
using UnityEngine.InputSystem; 

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private Projectile projectileObject;
    [SerializeField] private LayerMask targetLayers;

    [SerializeField] private int damage = 1;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float cooldown = 0.25f;
    [SerializeField] private float spawnOffset = 0.5f;

    private PlayerMovements movement;
    private Animator animator;
    private float nextFire;


    private void Awake()
    {
        movement = GetComponent<PlayerMovements>();
        animator = GetComponent<Animator>();
    }
   
    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }
        if (Keyboard.current.fKey.isPressed && Time.time >= nextFire)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        if(projectileObject == null)
        {
            return;
        }

        Vector2 direction = movement.FaceDirection;

        animator.SetFloat("LastX", direction.x);
        animator.SetFloat("LastY", direction.y);
        animator.SetTrigger("Attack");

        Vector2 spawnPosition = (Vector2)transform.position + direction * spawnOffset;

        Projectile projectile = Instantiate(projectileObject, spawnPosition, Quaternion.identity);

        projectile.Shoot(direction, speed, damage, targetLayers);

        nextFire = Time.time + cooldown;
    }
}
