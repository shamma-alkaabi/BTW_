using UnityEngine;

// Walk into it once to save your progress. After that, dying puts you back here (BTW's Respawn).
[RequireComponent(typeof(Collider2D))]
public class CheckpointZone : MonoBehaviour
{
    public Respawn respawn;
    public Transform point;   // where you come back; the zone itself if empty

    private bool used;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used || other.GetComponent<PlayerMovement>() == null) return;

        used = true;
        respawn.SetCheckpoint(point != null ? point.position : transform.position);
        AudioManager.Cue("pickup", "[Checkpoint saved]");
        if (GameManager.Instance != null) GameManager.Instance.Hud.ShowToast("Checkpoint saved");
    }
}
