using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovements : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rigidBody; 
    private Vector2 moveInput;
    private Animator animator;
    private bool canMove = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (canMove)
        {
            rigidBody.linearVelocity = moveInput * moveSpeed;
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (!canMove)
        {
            return; 
        }
        moveInput = context.ReadValue<Vector2>();

        animator.SetBool("isMoving", moveInput != Vector2.zero);
        animator.SetFloat("X", moveInput.x);
        animator.SetFloat("Y", moveInput.y);

        if (moveInput != Vector2.zero)
        {
            animator.SetFloat("LastX", moveInput.x);
            animator.SetFloat("LastY", moveInput.y);
        }
    }

    public void DisableMovement()
    {
        canMove = false;
        moveInput = Vector2.zero;
        rigidBody.linearVelocity = Vector2.zero;

        animator.SetBool("isMoving", false); 
    }
}
