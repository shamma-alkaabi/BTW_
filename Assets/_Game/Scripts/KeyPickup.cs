using UnityEngine;

// A key lying in the maze. Walk over it to pick it up. RoomClearance counts them and opens the exit.
[RequireComponent(typeof(Collider2D))]
public class KeyPickup : MonoBehaviour
{
    private float bobStart;
    private Vector3 basePos;

    private void Start()
    {
        basePos = transform.position;
        bobStart = Random.value * 6f;
    }

    // gentle bobbing so it's easy to spot
    private void Update()
    {
        transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * 3f + bobStart) * 0.12f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerMovement>() == null) return;

        AudioManager.Cue("pickup", "[A key clinks]");
        gameObject.SetActive(false);   // RoomClearance treats an inactive key as collected
    }
}
