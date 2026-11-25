using UnityEngine;

// A floor hazard that hurts the player when it's up. It can sit still and pop on and off,
// or slide back and forth along a path.
//
// Based on two MIT-licensed projects:
//  - SpikeTrap from legend-of-z (https://github.com/hueter/legend-of-z, Copyright (c) 2016
//    Michael Hueter): moving between waypoints and hurting the player on touch.
//  - Spikes from 2D-Top-Down-RPG-on-Unity (https://github.com/KirilChingarov/2D-Top-Down-RPG-on-Unity,
//    Copyright (c) 2021 Kiril Chingarov): the timed on/off cycle with a delay before it activates.
// Both were rewritten for this project (no database, no tags). See THIRD_PARTY_NOTICES.md.
[RequireComponent(typeof(Collider2D))]
public class SpikeTrap : MonoBehaviour
{
    public int damage = 8;
    public float startDelay = 0f;
    public float inactiveSeconds = 1.5f;
    public float warnSeconds = 0.5f;     // flashes before it comes up so you can step away
    public float activeSeconds = 1.2f;
    public Vector2[] pathOffsets;        // optional: slide through these points (relative to the start)
    public float moveSpeed = 2f;

    private SpriteRenderer sr;
    private Color baseColor;
    private Vector3 start;
    private int pathIndex;
    private bool active;
    private float cycleStart;
    private float nextHurt;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        GetComponent<Collider2D>().isTrigger = true;
        start = transform.position;
        cycleStart = Time.time + startDelay;
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.State != GameState.Playing) return;

        UpdateCycle();
        UpdateMovement();
    }

    private void UpdateCycle()
    {
        float t = Time.time - cycleStart;
        if (t < 0f) return;

        float period = inactiveSeconds + activeSeconds;
        float phase = t % period;
        active = phase >= inactiveSeconds;
        bool warning = !active && phase >= inactiveSeconds - warnSeconds;

        if (sr == null) return;
        if (active) sr.color = new Color(1f, 0.25f, 0.2f, 1f);
        else if (warning) sr.color = (Time.time * 12f) % 2f < 1f ? new Color(1f, 0.8f, 0.3f, 0.9f) : baseColor;
        else sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f);
    }

    private void UpdateMovement()
    {
        if (pathOffsets == null || pathOffsets.Length == 0) return;

        Vector3 target = start + (Vector3)pathOffsets[pathIndex];
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        if ((transform.position - target).sqrMagnitude < 0.0001f) pathIndex = (pathIndex + 1) % pathOffsets.Length;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!active || Time.time < nextHurt) return;

        var player = other.GetComponent<PlayerMovement>();
        if (player == null) return;

        var health = player.GetComponent<Health>();
        if (health != null && health.TakeDamage(damage, transform.position)) nextHurt = Time.time + 0.8f;
    }
}
