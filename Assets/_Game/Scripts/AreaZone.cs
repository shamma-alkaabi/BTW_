using UnityEngine;

// One trigger box per map area. When the player walks in, the camera background,
// music and area banner switch over.
[RequireComponent(typeof(BoxCollider2D))]
public class AreaZone : MonoBehaviour
{
    public static AreaZone Current { get; private set; }

    public string areaName = "Area";
    public Color background = Color.black;
    public string musicKey;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerMovement>() == null) return;
        Enter();
    }

    public void Enter()
    {
        if (Current == this) return;
        Current = this;

        if (Camera.main != null) Camera.main.backgroundColor = background;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(musicKey);

        var gm = GameManager.Instance;
        if (gm != null && gm.Hud != null) gm.Hud.ShowAreaBanner(areaName);
    }
}
