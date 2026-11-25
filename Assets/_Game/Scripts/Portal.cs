using System.Collections;
using UnityEngine;

// A doorway between areas. Walking in fades out, moves the player to the destination, swaps the
// camera bounds and saves the new respawn point. It can start locked (RoomClearance opens it).
[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    public Transform destination;
    public Collider2D destinationBounds;   // camera limits for the destination area
    public CameraBounds cameraBounds;
    public Respawn respawn;                // BTW's checkpoint component on the Player
    public string completesTask;
    public GameObject disableOnUse;        // the creature that's chasing you, so it doesn't follow
    public bool startLocked;
    public string lockedMessage = "It's sealed. Defeat everything in this area first";

    private SpriteRenderer[] visuals;
    private Color[] originalColors;
    private bool used;
    private bool locked;
    private float nextHint;

    public bool IsUsable => !locked && !used;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        visuals = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[visuals.Length];
        for (int i = 0; i < visuals.Length; i++) originalColors[i] = visuals[i].color;
        if (startLocked) Lock();
    }

    public void Lock()
    {
        locked = true;
        for (int i = 0; i < visuals.Length; i++) visuals[i].color = new Color(0.35f, 0.35f, 0.4f, 0.55f);
    }

    public void Unlock()
    {
        locked = false;
        for (int i = 0; i < visuals.Length; i++) visuals[i].color = originalColors[i];
        AudioManager.Cue("portal", "[The seal fades]");
    }

    // Stay (not Enter) so a portal that unlocks while you're standing in it still works
    private void OnTriggerStay2D(Collider2D other)
    {
        if (used) return;

        var player = other.GetComponent<PlayerMovement>();
        var gm = GameManager.Instance;
        if (player == null || gm == null || gm.State != GameState.Playing) return;

        if (locked)
        {
            if (Time.time >= nextHint)
            {
                nextHint = Time.time + 3f;
                gm.Hud.ShowToast(lockedMessage);
            }
            return;
        }

        StartCoroutine(Travel(player));
    }

    private IEnumerator Travel(PlayerMovement player)
    {
        used = true;
        var gm = GameManager.Instance;

        gm.SetState(GameState.Intro);   // just to lock input during the transition
        AudioManager.Cue("portal", "[A deep hum fills the air]");
        yield return gm.Hud.FadeTo(1f, 0.6f);

        var body = player.GetComponent<Rigidbody2D>();
        Vector3 before = player.transform.position;
        player.transform.position = destination.position;
        body.position = destination.position;
        body.linearVelocity = Vector2.zero;

        if (respawn != null) respawn.SetCheckpoint(destination.position);
        if (disableOnUse != null) disableOnUse.SetActive(false);

        if (cameraBounds != null)
        {
            cameraBounds.SetBounds(destinationBounds);
            cameraBounds.Warp(player.transform, destination.position - before);
        }

        if (!string.IsNullOrEmpty(completesTask)) gm.Tasks.Complete(completesTask);

        yield return new WaitForSecondsRealtime(0.35f);   // lets the camera settle
        yield return gm.Hud.FadeTo(0f, 0.9f);
        gm.SetState(GameState.Playing);
    }
}
