using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Slime boss: the regular slime (sprite, animations, health, contact damage) scaled up in the prefab, plus two
/// abilities that alternate - a telegraphed dash at the player and a summon of small slimes. Below enrageBelow
/// of its health the pause between abilities gets shorter. Everything is plain movement/spawning, no new art.
/// </summary>
[RequireComponent(typeof(EnemyHealth), typeof(EnemyMovement), typeof(EnemyBehaviour))]
public class BossController : MonoBehaviour
{
    [SerializeField] private string bossName = "Slime King";
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private float abilityCooldown = 4f;
    [SerializeField] private float enragedCooldown = 2.5f;
    [SerializeField, Range(0f, 1f)] private float enrageBelow = 0.5f;
    [SerializeField] private float telegraphSeconds = 0.6f;

    [Header("Dash")]
    [SerializeField] private float dashSeconds = 0.7f;
    [SerializeField] private float dashSpeed = 9f;

    [Header("Summon")]
    [SerializeField] private int minionsPerSummon = 2;
    [SerializeField] private int maxMinions = 4;
    [SerializeField] private float summonRadius = 3f;

    private Rigidbody2D body;
    private EnemyHealth health;
    private EnemyMovement movement;
    private EnemyBehaviour behaviour;
    private SpriteRenderer[] sprites;
    private Color[] baseColors;
    private Transform player;
    private int wallMask;
    private readonly List<GameObject> minions = new List<GameObject>();

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        movement = GetComponent<EnemyMovement>();
        behaviour = GetComponent<EnemyBehaviour>();
        sprites = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[sprites.Length];
        for (var i = 0; i < sprites.Length; i++) baseColors[i] = sprites[i].color;
        wallMask = LayerMask.GetMask("Walls");
    }

    // Rooms are frozen with SetActive(false), which kills coroutines: restart the loop and undo a half-done ability.
    private void OnEnable()
    {
        Resume();
        StartCoroutine(AbilityLoop());
    }

    private void Update()
    {
        if (GameFlow.Instance == null) return;
        if (health.IsDead) GameFlow.Instance.HideBossBar();
        else GameFlow.Instance.SetBossBar(bossName, health.HealthFraction);
    }

    private void OnDisable()
    {
        if (GameFlow.Instance != null) GameFlow.Instance.HideBossBar();
    }

    private void OnDestroy()
    {
        foreach (var minion in minions)
        {
            if (minion != null) Destroy(minion);
        }
    }

    private IEnumerator AbilityLoop()
    {
        var dashNext = true;
        while (!health.IsDead)
        {
            yield return new WaitForSeconds(health.HealthFraction < enrageBelow ? enragedCooldown : abilityCooldown);

            if (health.IsDead) yield break;
            if (player == null)
            {
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject == null) continue;
                player = playerObject.transform;
            }

            yield return dashNext ? Dash() : Summon();
            dashNext = !dashNext;
        }
    }

    private IEnumerator Dash()
    {
        Pause(Color.red);
        yield return new WaitForSeconds(telegraphSeconds);

        if (health.IsDead || player == null)
        {
            Resume();
            yield break;
        }

        var direction = ((Vector2)player.position - body.position).normalized;
        movement.LookDirection(direction);
        for (var t = 0f; t < dashSeconds && !health.IsDead; t += Time.deltaTime)
        {
            body.linearVelocity = direction * dashSpeed;
            yield return null;
        }

        body.linearVelocity = Vector2.zero;
        Resume();
    }

    private IEnumerator Summon()
    {
        Pause(Color.green);
        yield return new WaitForSeconds(telegraphSeconds * 0.66f);

        if (!health.IsDead && minionPrefab != null)
        {
            minions.RemoveAll(m => m == null);
            for (var i = 0; i < minionsPerSummon && minions.Count < maxMinions; i++)
            {
                var position = (Vector2)transform.position + Random.insideUnitCircle.normalized * summonRadius;
                if (Physics2D.OverlapCircle(position, 0.4f, wallMask) != null) continue;

                // parent = the room's Spawned container (not the boss), so minions keep the room's freeze/destroy lifecycle
                var minion = Instantiate(minionPrefab, position, Quaternion.identity, transform.parent);
                Generation.Runtime.PixelLit.Actor(minion);
                minions.Add(minion);
            }
        }

        Resume();
    }

    // EnemyMovement.FixedUpdate rewrites velocity every step, so it has to be off while the boss drives the body itself.
    private void Pause(Color tint)
    {
        movement.Stop();
        movement.enabled = false;
        behaviour.enabled = false;
        body.linearVelocity = Vector2.zero;
        foreach (var sprite in sprites) sprite.color = tint;
    }

    private void Resume()
    {
        for (var i = 0; i < sprites.Length; i++) sprites[i].color = baseColors[i];
        if (health.IsDead) return;
        movement.enabled = true;
        behaviour.enabled = true;
    }
}
