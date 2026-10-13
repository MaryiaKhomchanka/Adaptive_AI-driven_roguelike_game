using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;

public class SlimeMovement : MonoBehaviour
{
    private enum SlimeState
    {
        Wander,
        Notice,
        Chase
    }

    [SerializeField] private float detectionRange = 4f;

    [SerializeField] private float minWander = 1f;
    [SerializeField] private float maxWander = 3f;

    [SerializeField] private float minIdle = 1f;
    [SerializeField] private float maxIdle = 2f;

    [SerializeField] private float noticeDuration = 1f;

    private SlimeState state = SlimeState.Wander;

    private EnemyMovement movement;
    private Animator animator;
    private Transform player;

    private float nextWander;

    private bool isWalking = false;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        movement.LookDirection(Vector2.down);
        Walk();
    }

    private bool PlayerInRange()
    {
        if (player == null)
        {
            return false;
        }
        float distance = Vector2.Distance(transform.position, player.position);

        return distance <= detectionRange;
    }

    private void Walk()
    {
        isWalking = true;

        float wanderTime = Random.Range(minWander, maxWander);
        nextWander = Time.time + wanderTime;

        int randomDirection = Random.Range(0, 4);

        if (randomDirection == 0)
        {
            movement.SetDirection(Vector2.up);
        }
        else if (randomDirection == 1)
        {
            movement.SetDirection(Vector2.down);
        }
        else if (randomDirection == 2)
        {
            movement.SetDirection(Vector2.left);
        }
        else
        {
            movement.SetDirection(Vector2.right);
        }
    }

    private void Idle() 
    {
        isWalking = false;
        movement.Stop();
        float idleTime = Random.Range(minIdle, maxIdle);
        nextWander = Time.time + idleTime; 
    }

    private void Wander()
    {
        if (Time.time >= nextWander)
        {
            if (isWalking)
            {
                Idle();
            }
            else 
            {
                Walk();
            }
        }
    }

    private IEnumerator NoticePlayer() 
    {
        state = SlimeState.Notice;
        movement.Stop();
        Vector2 lookDirection = player.position - transform.position;
        movement.LookDirection(lookDirection);
        animator.SetTrigger("Notice");
        yield return new WaitForSeconds(noticeDuration);
        state = SlimeState.Chase;
    }

    private void ChasePlayer() 
    {
        if (player == null) 
        {
            movement.Stop();
            return;
        }

        movement.MoveTo(player.position);
    }


    private void Update()
    {
        if (state == SlimeState.Wander)
        {
            Wander();

            if (PlayerInRange())
            {
                StartCoroutine(NoticePlayer());
            }
        }
        else if (state == SlimeState.Chase)
        {
            ChasePlayer();
        }
    }

}
