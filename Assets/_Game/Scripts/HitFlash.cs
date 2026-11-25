using System.Collections;
using UnityEngine;

// Quick red flash on the sprite whenever this object takes damage.
[RequireComponent(typeof(Health), typeof(SpriteRenderer))]
public class HitFlash : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color baseColor;
    private Coroutine routine;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        GetComponent<Health>().Damaged += OnDamaged;
    }

    private void OnDamaged(Vector2 from)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        sr.color = new Color(1f, 0.35f, 0.35f, baseColor.a);
        yield return new WaitForSeconds(0.12f);
        sr.color = baseColor;
        routine = null;
    }
}
