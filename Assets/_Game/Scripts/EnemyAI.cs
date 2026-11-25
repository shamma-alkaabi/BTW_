using System.Collections;
using UnityEngine;

// Basic melee enemy. It wanders near its spawn point, chases the player when they get close,
// then winds up (and flashes orange) before swinging. That wind-up is what makes the
// fights fair: you can see the hit coming, step back, and counter-attack.
[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(DirectionalAnimator))]
public class EnemyAI : MonoBehaviour
{
    enum Mode { Idle, Chase, Windup, Recover, Hurt, Dead }

    public float wanderSpeed = 1.1f;
    public float chaseSpeed = 2.6f;
    public float detectRadius = 6f;
    public float giveUpRadius = 11f;
    public float attackRange = 1.0f;
    public float windup = 0.55f;
    public float recover = 0.9f;
    public int damage = 8;
    public bool alwaysChase;
    public bool superArmor;           // boss option: hits don't interrupt its attack
    public string defeatTaskId;       // task to complete when this enemy dies
    public string aggroCaption = "[A growl nearby]";
    public float dropChance = 0f;     // chance of leaving a heart behind (0 to 1)
    public int dropHeal = 15;
    public HeartPickup heartPrefab;   // what to drop (the Heart prefab)

    private Rigidbody2D rb;
    private Health hp;
    private DirectionalAnimator anim;
    private SpriteRenderer sr;
    private Transform player;
    private Health playerHealth;
    private bool playerWasDead;

    private Vector2 home;
    private Vector2 velocity;
    private Vector2 wanderDir;
    private Color baseColor;

    private Mode mode = Mode.Idle;
    private float modeEndTime;
    private float nextWanderTime;
    private bool aggroed;

    public bool IsDead => mode == Mode.Dead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.mass = 10f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        hp = GetComponent<Health>();
        anim = GetComponent<DirectionalAnimator>();
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;

        hp.Died += OnDied;
        home = transform.position;
    }

    private void Start()
    {
        var movement = FindFirstObjectByType<PlayerMovement>();
        if (movement != null)
        {
            player = movement.transform;
            playerHealth = movement.GetComponent<Health>();
        }
    }

    // called by the player's attack
    public void Hit(int amount, Vector2 from)
    {
        if (IsDead) return;
        if (!hp.TakeDamage(amount, from)) return;
        if (hp.IsDead) return;

        // knock back and stagger, unless it's a boss that's mid-swing
        if (!superArmor || mode != Mode.Windup)
        {
            Vector2 away = ((Vector2)transform.position - from).normalized;
            velocity = away * (superArmor ? 2f : 6f);
            mode = Mode.Hurt;
            modeEndTime = Time.time + 0.2f;
            sr.color = baseColor;
        }
        aggroed = true;
    }

    // instantly defeats this enemy (used when a boss falls and its minions go with it)
    public void Kill()
    {
        if (!IsDead) hp.TakeDamage(hp.maxHP * 10, transform.position);
    }

    // Used when the player respawns: everything goes back to where it started.
    public void ResetToHome()
    {
        if (IsDead) return;

        transform.position = home;
        rb.position = home;
        mode = Mode.Idle;
        velocity = Vector2.zero;
        aggroed = false;
        sr.color = baseColor;
        hp.ResetFull();
    }

    private void Update()
    {
        if (mode == Mode.Dead) return;

        // when the player respawns, everything goes back to where it started
        if (playerHealth != null)
        {
            if (playerHealth.IsDead) playerWasDead = true;
            else if (playerWasDead)
            {
                playerWasDead = false;
                ResetToHome();
            }
        }

        var gm = GameManager.Instance;
        bool frozen = gm == null || gm.State != GameState.Playing || player == null || playerHealth == null || playerHealth.IsDead;
        if (frozen)
        {
            velocity = Vector2.zero;
            PlayAnim(false);
            return;
        }

        Vector2 pos = transform.position;
        Vector2 toPlayer = (Vector2)player.position - pos;
        float dist = toPlayer.magnitude;

        switch (mode)
        {
            case Mode.Idle:
                UpdateIdle(pos, dist);
                break;

            case Mode.Chase:
                velocity = toPlayer.normalized * chaseSpeed;
                if (!alwaysChase && dist > giveUpRadius)
                {
                    mode = Mode.Idle;
                    aggroed = false;
                }
                else if (dist < attackRange)
                {
                    mode = Mode.Windup;
                    modeEndTime = Time.time + windup;
                    velocity = Vector2.zero;
                }
                break;

            case Mode.Windup:
                UpdateWindup(pos, dist);
                break;

            case Mode.Recover:
                velocity = Vector2.zero;
                if (Time.time >= modeEndTime) mode = Mode.Chase;
                break;

            case Mode.Hurt:
                velocity = Vector2.Lerp(velocity, Vector2.zero, 10f * Time.deltaTime);
                if (Time.time >= modeEndTime) mode = Mode.Chase;
                break;
        }

        bool facingTarget = mode == Mode.Windup || mode == Mode.Recover;
        anim.FaceX(facingTarget ? toPlayer.x : velocity.x);
        PlayAnim(velocity.sqrMagnitude > 0.1f);
    }

    private void UpdateIdle(Vector2 pos, float dist)
    {
        if (Time.time > nextWanderTime)
        {
            // pick a new direction (or stand still), but drift back if we've wandered too far
            wanderDir = Random.value < 0.4f ? Vector2.zero : Random.insideUnitCircle.normalized;
            if ((pos - home).magnitude > 3.5f) wanderDir = (home - pos).normalized;
            nextWanderTime = Time.time + Random.Range(1.5f, 3.5f);
        }
        velocity = wanderDir * wanderSpeed;

        if (alwaysChase || dist < detectRadius)
        {
            mode = Mode.Chase;
            if (!aggroed)
            {
                aggroed = true;
                AudioManager.Cue("growl", aggroCaption);
            }
        }
    }

    private void UpdateWindup(Vector2 pos, float dist)
    {
        velocity = Vector2.zero;

        // fade towards orange so the player can read the attack
        float progress = 1f - Mathf.Clamp01((modeEndTime - Time.time) / windup);
        sr.color = Color.Lerp(baseColor, new Color(1f, 0.75f, 0.3f, baseColor.a), progress);

        if (Time.time < modeEndTime) return;

        sr.color = baseColor;
        // small grace distance so a player who just stepped back can still get clipped
        if (dist < attackRange * 1.4f) playerHealth.TakeDamage(damage, pos);
        AudioManager.Cue("enemy_attack");

        mode = Mode.Recover;
        modeEndTime = Time.time + recover;
    }

    private void PlayAnim(bool moving)
    {
        var animSet = anim.set;
        anim.Play(moving && animSet.run != null ? animSet.run : animSet.idle, Dir8.Down);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = mode == Mode.Dead ? Vector2.zero : velocity;
    }

    private void OnDied()
    {
        mode = Mode.Dead;
        velocity = Vector2.zero;
        sr.color = baseColor;

        foreach (var col in GetComponents<Collider2D>()) col.enabled = false;
        rb.linearVelocity = Vector2.zero;

        AudioManager.Cue("enemy_die", "[Creature collapses]");
        if (heartPrefab != null && Random.value < dropChance) HeartPickup.Drop(heartPrefab, transform.position, dropHeal);
        if (anim.set.death != null) anim.Play(anim.set.death, Dir8.Down, true);

        StartCoroutine(Vanish());

        if (!string.IsNullOrEmpty(defeatTaskId) && GameManager.Instance != null)
            GameManager.Instance.Tasks.Complete(defeatTaskId);
    }

    private IEnumerator Vanish()
    {
        yield return new WaitForSeconds(1.3f);

        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            var c = baseColor;
            c.a = 1f - t / 0.6f;
            sr.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }
}
