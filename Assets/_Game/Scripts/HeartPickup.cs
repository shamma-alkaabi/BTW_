using UnityEngine;

// A heart that enemies sometimes drop when they die. Walk over it to heal.
// Enemies dropping hearts is from legend-of-z (https://github.com/hueter/legend-of-z, MIT,
// Copyright (c) 2016 Michael Hueter). The heart itself is the Heart prefab in Assets/_Game/Prefabs.
// See THIRD_PARTY_NOTICES.md.
[RequireComponent(typeof(CircleCollider2D))]
public class HeartPickup : MonoBehaviour
{
    public int healAmount = 15;
    public float lifetime = 20f;

    public static void Drop(HeartPickup prefab, Vector3 position, int heal)
    {
        var heart = Instantiate(prefab, position, Quaternion.identity);
        heart.healAmount = heal;
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // small bob so it catches the eye
        transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 6f) * 0.08f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerMovement>() == null) return;

        var health = other.GetComponent<Health>();
        if (health == null || health.HP >= health.maxHP) return;

        health.Heal(healAmount);
        AudioManager.Cue("pickup", "[Chime]");
        if (GameManager.Instance != null) GameManager.Instance.Hud.ShowToast("+" + healAmount + " HP");
        Destroy(gameObject);
    }
}
