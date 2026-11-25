using UnityEngine;

// A small health bar that floats above a character (the boss), so you can always see how much is left.
// It's built from two sprites in the scene: a dark background and a red fill that shrinks.
public class WorldHealthBar : MonoBehaviour
{
    public Health health;
    public Transform fill;      // the red bar, a child of the background
    public float width = 1f;    // the fill's full width, in the background's local units

    private void Start()
    {
        health.Changed += OnChanged;
        OnChanged(health.HP, health.maxHP);
    }

    private void OnDestroy()
    {
        if (health != null) health.Changed -= OnChanged;
    }

    private void OnChanged(int hp, int max)
    {
        float f = Mathf.Clamp01((float)hp / max);
        // shrink towards the left edge
        fill.localScale = new Vector3(width * f, fill.localScale.y, 1f);
        fill.localPosition = new Vector3(-(1f - f) * width * 0.5f, fill.localPosition.y, fill.localPosition.z);
    }
}
