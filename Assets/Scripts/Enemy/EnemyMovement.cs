using System.Runtime.CompilerServices;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    private Rigidbody2D rigidBody;
    private Animator animator;
    private Vector2 direction;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        rigidBody.linearVelocity = direction * moveSpeed;
    }

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
        animator.SetBool("isMoving", direction != Vector2.zero);
        animator.SetFloat("X", direction.x);
        animator.SetFloat("Y", direction.y);

        if (direction != Vector2.zero)
        {
            LookDirection(direction);
        }
    }

    public void MoveTo(Vector2 target) 
    {
        Vector2 newDirection = target - rigidBody.position;
        SetDirection(newDirection);
    }

    public void LookDirection(Vector2 lookDirection) 
    {
        if(lookDirection == Vector2.zero) 
        {
            return;
        }

        lookDirection = lookDirection.normalized;

        animator.SetFloat("LastX", lookDirection.x);
        animator.SetFloat("LastY", lookDirection.y);

    }

    public void Stop() 
    {
        SetDirection(Vector2.zero);
    }

}
