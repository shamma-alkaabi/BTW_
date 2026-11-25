using UnityEngine;

// BTW's PlayerMovement doesn't know about cutscenes, dialogue or the pause menu, so this
// holds the player still whenever the game isn't in the Playing state.
[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerLock : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // Runs after PlayerMovement.Update has set the velocity, before the physics step
    private void LateUpdate()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State == GameState.Playing) return;

        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isWalking", false);
    }

    // turn the idle pose to face a direction (the intro uses this)
    public void Face(Vector2 dir)
    {
        animator.SetFloat("LastInputX", dir.x);
        animator.SetFloat("LastInputY", dir.y);
    }
}
