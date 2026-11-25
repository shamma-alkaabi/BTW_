using System.Collections;
using UnityEngine;

// The final boss. It starts passive, wakes up when you get close, then fights in three phases:
//   phase 1: chases you and slams the ground
//   phase 2 (below phase2At): also charges, and summons minions now and then
//   phase 3 (below enrageAt): enraged, attacks much more often and jumps around the arena
// Every big attack is telegraphed (the boss flashes orange) so it can be dodged.
//
// Based on two MIT-licensed projects, rewritten for this project:
//  - legend-of-z (https://github.com/hueter/legend-of-z, Copyright (c) 2016 Michael Hueter):
//    ForestBoss_Manager (intro countdown, battle starting when the player gets close, moving
//    between waypoints, attacking faster at low health) and MinotaurAttackScript (the charge).
//  - 2D-Top-Down-RPG-on-Unity (https://github.com/KirilChingarov/2D-Top-Down-RPG-on-Unity,
//    Copyright (c) 2021 Kiril Chingarov): DeathBossController (picking between attacks and
//    summoning minions on a cooldown).
// See THIRD_PARTY_NOTICES.md.
[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(DirectionalAnimator))]
public class BossController : MonoBehaviour
{
    private enum Mode { Waiting, Intro, Chase, SlamWindup, ChargeWindup, Charging, Recover, Dead }

    public string bossName = "Bone Warden";
    public string defeatTaskId = "level3";
    public bool endsGame = false;       // off: beating the boss opens the way to the next levels instead of ending the game
    public float aggroDistance = 12f;
    public float revealDistance = 30f;   // the health bar appears when you get this close
    public float introSeconds = 2.5f;
    public float chaseSpeed = 1.8f;
    public int contactDamage = 8;

    [Header("Slam")]
    public float slamRadius = 3f;
    public int slamDamage = 18;
    public float slamWindup = 0.9f;

    [Header("Charge")]
    public float chargeSpeed = 9f;
    public int chargeDamage = 22;
    public float chargeWindup = 0.8f;
    public float chargeSeconds = 0.7f;

    [Header("Phases")]
    [Range(0f, 1f)] public float phase2At = 0.6f;
    [Range(0f, 1f)] public float enrageAt = 0.3f;
    public float attackInterval = 3.5f;
    public float enragedAttackInterval = 1.5f;
    public float summonInterval = 12f;

    [Header("Arena")]
    public EnemyAI[] reserveMinions;   // switched off in the scene, woken up one at a time
    public Transform[] summonPoints;
    public Transform[] wayPoints;      // places the boss jumps to when enraged

    private Rigidbody2D rb;
    private Health hp;
    private DirectionalAnimator anim;
    private SpriteRenderer sr;
    private Transform player;
    private Health playerHealth;

    private Mode mode = Mode.Waiting;
    private Vector2 home;
    private Vector2 velocity;
    private Vector2 chargeDir;
    private Color baseColor;
    private float modeEndTime;
    private float nextAttack;
    private float nextSummon;
    private float nextRelocate;
    private float nextContact;
    private int nextMinion;
    private bool enraged;
    private bool phase2;
    private bool playerWasDead;
    private bool barShown;

    public bool IsDead => mode == Mode.Dead;

    private float HpFraction => (float)hp.HP / hp.maxHP;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.mass = 50f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        hp = GetComponent<Health>();
        anim = GetComponent<DirectionalAnimator>();
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        home = transform.position;

        hp.Died += OnDied;
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
        if (IsDead || mode == Mode.Waiting) return;
        hp.TakeDamage(amount, from);
    }

    private void Update()
    {
        if (mode == Mode.Dead || player == null || playerHealth == null) return;

        // when the player respawns, the fight starts over
        if (playerHealth.IsDead) playerWasDead = true;
        else if (playerWasDead)
        {
            playerWasDead = false;
            ResetFight();
            return;
        }

        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing || playerHealth.IsDead)
        {
            velocity = Vector2.zero;
            return;
        }

        Vector2 pos = transform.position;
        Vector2 toPlayer = (Vector2)player.position - pos;
        float dist = toPlayer.magnitude;

        switch (mode)
        {
            case Mode.Waiting:
                ShowBarIfClose(dist);
                if (dist < aggroDistance) StartFight();
                break;

            case Mode.Intro:
                if (Time.time >= modeEndTime) mode = Mode.Chase;
                break;

            case Mode.Chase:
                UpdateChase(pos, toPlayer, dist);
                break;

            case Mode.SlamWindup:
                velocity = Vector2.zero;
                Telegraph(slamWindup);
                if (Time.time >= modeEndTime) DoSlam(pos, dist);
                break;

            case Mode.ChargeWindup:
                velocity = Vector2.zero;
                chargeDir = toPlayer.normalized;   // aim at where you are right now
                Telegraph(chargeWindup);
                if (Time.time >= modeEndTime)
                {
                    mode = Mode.Charging;
                    modeEndTime = Time.time + chargeSeconds;
                    sr.color = baseColor;
                    AudioManager.Cue("growl", "[The Warden charges!]");
                }
                break;

            case Mode.Charging:
                velocity = chargeDir * chargeSpeed;
                if (dist < 1.2f && Time.time >= nextContact)
                {
                    playerHealth.TakeDamage(chargeDamage, pos);
                    nextContact = Time.time + 1f;
                }
                if (Time.time >= modeEndTime) EnterRecover(0.9f);
                break;

            case Mode.Recover:
                velocity = Vector2.zero;
                if (Time.time >= modeEndTime) mode = Mode.Chase;
                break;
        }

        anim.FaceX(mode == Mode.Charging ? chargeDir.x : toPlayer.x);
        anim.Play(velocity.sqrMagnitude > 0.1f && anim.set.run != null ? anim.set.run : anim.set.idle, Dir8.Down);
    }

    // the bar is up before the fight starts, so you can see what you're walking into
    private void ShowBarIfClose(float dist)
    {
        if (barShown || dist > revealDistance) return;
        barShown = true;
        GameManager.Instance.Hud.ShowBossBar(bossName, hp);
    }

    private void StartFight()
    {
        mode = Mode.Intro;
        modeEndTime = Time.time + introSeconds;
        nextAttack = modeEndTime + attackInterval;
        nextSummon = modeEndTime + summonInterval;
        nextRelocate = modeEndTime + 8f;

        AudioManager.Cue("growl", "[The ground shakes]");
        var gm = GameManager.Instance;
        gm.Hud.ShowToast(bossName + " awakens!");
    }

    private void UpdateChase(Vector2 pos, Vector2 toPlayer, float dist)
    {
        CheckPhases();

        float speedBoost = enraged ? 1.6f : 1f;
        velocity = dist > 1.0f ? toPlayer.normalized * chaseSpeed * speedBoost : Vector2.zero;

        // just touching the boss hurts a little, so you can't hug it
        if (dist < 1.1f && Time.time >= nextContact)
        {
            playerHealth.TakeDamage(contactDamage, pos);
            nextContact = Time.time + 1.2f;
        }

        if (phase2 && Time.time >= nextSummon)
        {
            Summon();
            nextSummon = Time.time + (enraged ? summonInterval * 0.6f : summonInterval);
        }

        if (enraged && wayPoints != null && wayPoints.Length > 0 && Time.time >= nextRelocate)
        {
            Relocate();
            nextRelocate = Time.time + 6f;
            return;
        }

        if (Time.time < nextAttack) return;

        // pick an attack: slam up close, charge from a distance
        bool canCharge = phase2 && dist > 3.5f;
        if (canCharge && (dist > slamRadius + 1f || Random.value < 0.5f))
        {
            mode = Mode.ChargeWindup;
            modeEndTime = Time.time + chargeWindup;
        }
        else if (dist < slamRadius + 1.5f)
        {
            mode = Mode.SlamWindup;
            modeEndTime = Time.time + slamWindup;
        }
        else return;   // too far for a slam, keep chasing

        velocity = Vector2.zero;
        nextAttack = Time.time + (enraged ? enragedAttackInterval : attackInterval);
    }

    private void CheckPhases()
    {
        if (!phase2 && HpFraction <= phase2At)
        {
            phase2 = true;
            GameManager.Instance.Hud.ShowToast(bossName + " grows angry!");
        }
        if (!enraged && HpFraction <= enrageAt)
        {
            enraged = true;
            GameManager.Instance.Hud.ShowToast(bossName + " is enraged!");
            AudioManager.Cue("growl", "[A terrible roar]");
        }
    }

    // fade towards orange so the player can read the attack
    private void Telegraph(float windup)
    {
        float progress = 1f - Mathf.Clamp01((modeEndTime - Time.time) / windup);
        sr.color = Color.Lerp(baseColor, new Color(1f, 0.6f, 0.2f, baseColor.a), progress);
    }

    private void DoSlam(Vector2 pos, float dist)
    {
        sr.color = baseColor;
        AudioManager.Cue("enemy_attack", "[The ground cracks]");
        if (dist < slamRadius) playerHealth.TakeDamage(slamDamage, pos);
        EnterRecover(0.8f);
    }

    private void EnterRecover(float seconds)
    {
        mode = Mode.Recover;
        modeEndTime = Time.time + seconds;
        sr.color = baseColor;
    }

    // wakes the next reserve minion at one of the summon points
    private void Summon()
    {
        if (reserveMinions == null || nextMinion >= reserveMinions.Length) return;

        var minion = reserveMinions[nextMinion++];
        if (minion == null) return;

        if (summonPoints != null && summonPoints.Length > 0)
            minion.transform.position = summonPoints[Random.Range(0, summonPoints.Length)].position;
        minion.gameObject.SetActive(true);

        AudioManager.Cue("growl", "[Bones rattle]");
        GameManager.Instance.Hud.ShowToast("The Warden calls for help!");
    }

    // jump to whichever waypoint is furthest from the player
    private void Relocate()
    {
        Transform best = wayPoints[0];
        float bestDist = -1f;
        foreach (var point in wayPoints)
        {
            float d = Vector2.Distance(point.position, player.position);
            if (d > bestDist)
            {
                bestDist = d;
                best = point;
            }
        }
        transform.position = best.position;
        rb.position = best.position;
        AudioManager.Cue("portal");
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = mode == Mode.Dead ? Vector2.zero : velocity;
    }

    private void OnDied()
    {
        mode = Mode.Dead;
        velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        sr.color = baseColor;

        foreach (var col in GetComponents<Collider2D>()) col.enabled = false;

        // the minions go down with the boss
        if (reserveMinions != null)
            foreach (var minion in reserveMinions)
                if (minion != null && minion.gameObject.activeInHierarchy) minion.Kill();

        var gm = GameManager.Instance;
        gm.Hud.HideBossBar();
        AudioManager.Cue("enemy_die", "[The Warden collapses]");
        if (anim.set.death != null) anim.Play(anim.set.death, Dir8.Down, true);

        StartCoroutine(Victory());
    }

    private IEnumerator Victory()
    {
        var gm = GameManager.Instance;
        if (!string.IsNullOrEmpty(defeatTaskId)) gm.Tasks.Complete(defeatTaskId);

        yield return new WaitForSeconds(2.5f);

        if (endsGame)
        {
            gm.Victory();
        }
        else
        {
            gm.Hud.ShowToast("The Bone Warden is gone! A portal has opened");
            gm.Hud.ShowCaption("[Something is calling from deeper in the hollow]", 4f);
        }
    }

    private void ResetFight()
    {
        transform.position = home;
        rb.position = home;
        velocity = Vector2.zero;
        mode = Mode.Waiting;
        phase2 = false;
        enraged = false;
        nextMinion = 0;
        barShown = false;
        sr.color = baseColor;
        hp.ResetFull();

        if (reserveMinions != null)
            foreach (var minion in reserveMinions)
                if (minion != null) minion.gameObject.SetActive(false);

        GameManager.Instance.Hud.HideBossBar();
    }
}
