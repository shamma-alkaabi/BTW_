using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float iceAcceleration = 6f;   // how slowly you speed up and stop on ice
    private Rigidbody2D body;
    private Vector2 moveInput;
    private Animator animator;

    // set by TerrainZone (mud and ice). They only count for a moment, so stepping off goes back to normal
    private float terrainSpeed = 1f;
    private bool terrainIce;
    private float terrainUntil;
    private Vector2 slideVelocity;

    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        bool onTerrain = Time.time < terrainUntil;
        float speed = onTerrain ? terrainSpeed : 1f;
        Vector2 wanted = moveInput * moveSpeed * speed;

        if (onTerrain && terrainIce)
        {
            // on ice the velocity only drifts towards what you're asking for
            slideVelocity = Vector2.MoveTowards(slideVelocity, wanted, iceAcceleration * Time.deltaTime);
            body.linearVelocity = slideVelocity;
        }
        else
        {
            slideVelocity = wanted;
            body.linearVelocity = wanted;
        }
    }

    public void SetTerrain(float speedMultiplier, bool ice)
    {
        terrainSpeed = speedMultiplier;
        terrainIce = ice;
        terrainUntil = Time.time + 0.1f;
    }

    public void Move(InputAction.CallbackContext context)
    {
        animator.SetBool("isWalking", true);
        if (context.canceled)
        {
            animator.SetBool("isWalking", false);
            animator.SetFloat("LastInputX", moveInput.x);
            animator.SetFloat("LastInputY", moveInput.y);
        }
        moveInput = context.ReadValue<Vector2>();
        animator.SetFloat("InputX", moveInput.x);
        animator.SetFloat("InputY", moveInput.y);

    }
}
