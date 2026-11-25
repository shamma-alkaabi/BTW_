using UnityEngine;

// Spins the portal sprite and makes it pulse a little.
public class Spinner : MonoBehaviour
{
    public float degreesPerSecond = 90f;
    public float pulse = 0.08f;

    private Vector3 baseScale;

    private void Start()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
        transform.localScale = baseScale * (1f + Mathf.Sin(Time.time * 3f) * pulse);
    }
}
