using UnityEngine;

// One glowing rune on the floor of the puzzle room. Stepping on it tells the RunePuzzle.
// Index -1 is the altar in the middle, which just replays the sequence.
[RequireComponent(typeof(Collider2D))]
public class RunePlate : MonoBehaviour
{
    public RunePuzzle puzzle;
    public int index;
    public Color color = Color.white;

    private SpriteRenderer sr;
    private Color dim;
    private float litUntil;
    private bool lit;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        dim = new Color(color.r * 0.4f, color.g * 0.4f, color.b * 0.4f, 1f);
        sr.color = dim;
    }

    public void Flash(float seconds)
    {
        litUntil = Time.time + seconds;
    }

    public void SetLit(bool value)
    {
        lit = value;
    }

    private void Update()
    {
        sr.color = (lit || Time.time < litUntil) ? color : dim;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerMovement>() != null) puzzle.Step(this);
    }
}
