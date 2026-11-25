using UnityEngine;

// Draws the player with the hero's own animation set (idle, walk, weapon attack, death) on a child
// object, and hides BTW's single-sprite renderer. BTW's PlayerMovement, Health, Respawn and Animator
// keep working underneath, this only changes what is drawn. Untick this component to go back to
// BTW's look (the attack then has no animation).
[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Health))]
public class PlayerSprites : MonoBehaviour
{
    public DirectionalAnimator hero;      // the child object that draws the hero
    public SpriteRenderer btwRenderer;    // BTW's renderer on this object, hidden while the hero is shown
    public float attackSeconds = 0.43f;   // how long the weapon swing animation plays

    private Rigidbody2D rb;
    private Animator animator;
    private Health health;
    private Vector2 facing = Vector2.down;
    private Vector2 attackFacing;
    private float attackUntil;

    public Vector2 Facing => facing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (hero != null) hero.gameObject.SetActive(true);
        if (btwRenderer != null) btwRenderer.enabled = false;
    }

    private void OnDisable()
    {
        if (hero != null) hero.gameObject.SetActive(false);
        if (btwRenderer != null) btwRenderer.enabled = true;
    }

    public void PlayAttack(Vector2 dir)
    {
        facing = dir.normalized;
        attackFacing = facing;
        attackUntil = Time.time + attackSeconds;
        hero.Play(hero.set.attack, GetFourDirection(attackFacing), true);
    }
    private Dir8 GetFourDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        {
            return dir.x > 0 ? Dir8.RightDown : Dir8.LeftDown;
        }

        return dir.y > 0 ? Dir8.Up : Dir8.Down;
    }
    private void Update()
    {
        if (health.IsDead)
        {
            hero.Play(hero.set.death, GetFourDirection(facing));
            return;
        }

        if (Time.time < attackUntil)
        {
            hero.Play(hero.set.attack, DirectionalAnimator.FromVector(attackFacing));
            return;
        }

        Vector2 velocity = rb.linearVelocity;
        bool moving = velocity.sqrMagnitude > 0.05f;

        if (moving)
        {
            facing = velocity.normalized;
        }
        else
        {
            // standing still: BTW's animator remembers which way we last faced (the intro sets this too)
            var last = new Vector2(animator.GetFloat("LastInputX"), animator.GetFloat("LastInputY"));
            if (last.sqrMagnitude > 0.01f) facing = last.normalized;
        }

        hero.Play(moving ? hero.set.walk : hero.set.idle, GetFourDirection(facing));
    }
}
