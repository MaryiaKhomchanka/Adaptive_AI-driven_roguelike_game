using Generation.Runtime;
using UnityEngine;

/// <summary>Leaves a slime puddle behind while moving; each puddle fades and shrinks away on its own.</summary>
public class SlimeTrail : MonoBehaviour
{
    [SerializeField] private Color color = new Color(0.35f, 0.6f, 1f);
    [SerializeField] private float interval = 0.18f;
    [SerializeField] private float minSpeed = 0.3f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float size = 3f;

    private Rigidbody2D body;
    private float next;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Time.time < next || body.linearVelocity.sqrMagnitude < minSpeed * minSpeed) return;
        next = Time.time + interval;

        // parent = the room's Spawned container, so puddles freeze and get cleaned up with the room
        var jitter = Random.insideUnitCircle * 0.15f * transform.lossyScale.x;
        Fx.Spawn(transform.position + (Vector3)jitter + Vector3.down * 0.2f, Vector2.zero, color, lifetime,
            size * transform.lossyScale.x / 1.5f, 1, startAlpha: 0.55f, parent: transform.parent);
    }
}
