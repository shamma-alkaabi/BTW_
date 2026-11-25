using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Player melee swing. BTW's Attack script was an empty stub, so this fills it in.
// The weapon animation comes from PlayerSprites (the hero's attack clip); this script
// does the actual hit.
//
// The damage area only exists for the instant of the swing. That idea comes from the
// Damage script in legend-of-z (https://github.com/hueter/legend-of-z, MIT, Copyright (c) 2016
// Michael Hueter), rewritten here to use an overlap check instead of spawning a collider.
// See THIRD_PARTY_NOTICES.md.
[RequireComponent(typeof(Animator))]
public class Attack : MonoBehaviour
{
    public int damage = 2;
    public float reach = 1.0f;
    public float radius = 0.9f;
    public float cooldown = 0.45f;
    public float hitDelay = 0.17f;   // the weapon connects partway through the swing animation

    private Animator animator;
    private PlayerSprites sprites;
    private float nextAttack;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        sprites = GetComponent<PlayerSprites>();
    }

    // PlayerInput > Events > Player > Attack
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed || Time.time < nextAttack) return;

        var gm = GameManager.Instance;
        if (gm != null && gm.State != GameState.Playing) return;

        nextAttack = Time.time + cooldown;

        Vector2 dir = Facing();
        if (context.control.device is Mouse && Camera.main != null && Mouse.current != null)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 toMouse = (Vector2)mouseWorld - (Vector2)transform.position;
            if (toMouse.sqrMagnitude > 0.05f) dir = toMouse.normalized;
        }

        if (sprites != null && sprites.enabled) sprites.PlayAttack(dir);
        StartCoroutine(Swing(dir));
    }

    private Vector2 Facing()
    {
        if (sprites != null && sprites.enabled) return sprites.Facing;

        // without the hero sprites, fall back on BTW's animator
        var moving = new Vector2(animator.GetFloat("InputX"), animator.GetFloat("InputY"));
        if (animator.GetBool("isWalking") && moving.sqrMagnitude > 0.01f) return moving.normalized;

        var last = new Vector2(animator.GetFloat("LastInputX"), animator.GetFloat("LastInputY"));
        return last.sqrMagnitude > 0.01f ? last.normalized : Vector2.down;
    }

    private IEnumerator Swing(Vector2 dir)
    {
        AudioManager.Cue("attack");
        yield return new WaitForSeconds(hitDelay);

        Vector2 center = (Vector2)transform.position + dir * reach;
        var alreadyHit = new HashSet<Object>();

        foreach (var col in Physics2D.OverlapCircleAll(center, radius))
        {
            // one enemy can have several colliders, only count it once per swing
            var enemy = col.GetComponentInParent<EnemyAI>();
            if (enemy != null && !enemy.IsDead && alreadyHit.Add(enemy))
            {
                enemy.Hit(damage, transform.position);
                AudioManager.Cue("hit");
                continue;
            }

            var boss = col.GetComponentInParent<BossController>();
            if (boss != null && !boss.IsDead && alreadyHit.Add(boss))
            {
                boss.Hit(damage, transform.position);
                AudioManager.Cue("hit");
            }
        }
    }
}
