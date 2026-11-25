using UnityEngine;

// Draws things lower on the screen in front of things higher up.
[RequireComponent(typeof(SpriteRenderer))]
public class YSorter : MonoBehaviour
{
    public bool isStatic;   // tick this for anything that never moves
    public int offset;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        Apply();
    }

    private void LateUpdate()
    {
        if (!isStatic) Apply();
    }

    private void Apply()
    {
        sr.sortingOrder = -Mathf.RoundToInt(transform.position.y * 100f) + offset;
    }
}
