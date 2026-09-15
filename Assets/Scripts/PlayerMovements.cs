using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovements : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rigidBody; 
    private Vector2 moveInput;
    private Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        rigidBody.linearVelocity = moveInput * moveSpeed;

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            animator.SetTrigger("Hurt");
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            animator.SetTrigger("Attack");
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
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
}
