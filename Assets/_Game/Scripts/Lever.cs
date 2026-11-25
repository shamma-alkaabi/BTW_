using UnityEngine;

// A lever on the wall or floor. Press Enter next to it to pull it (BTW's InteractionDetector
// shows the icon). What it does is up to the LeverPuzzle it belongs to.
[RequireComponent(typeof(Collider2D))]
public class Lever : MonoBehaviour, IInteractable
{
    public LeverPuzzle puzzle;
    public int index;

    private SpriteRenderer sr;
    private bool pulled;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public bool CanInteract()
    {
        return true;
    }

    public void Interact()
    {
        pulled = !pulled;
        sr.flipX = pulled;   // the handle swings to the other side
        AudioManager.Cue("gate_open", "[A lever clunks]");
        puzzle.Pull(index);
    }
}
