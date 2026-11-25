using UnityEngine;

// Keeps an exit (or inner doors) closed until everything in the area is dead, then opens them.
// The idea (exits that open once the room is cleared) is from 2D-Top-Down-RPG-on-Unity
// (https://github.com/KirilChingarov/2D-Top-Down-RPG-on-Unity, MIT, Copyright (c) 2021 Kiril Chingarov),
// where RoomClearance and Door look for objects tagged "Enemy". This one watches an explicit list
// instead. See THIRD_PARTY_NOTICES.md.
public class RoomClearance : MonoBehaviour
{
    public string completesTask;
    public EnemyAI[] enemies;
    public KeyPickup[] keys;               // optional: keys that must be collected too (the maze)
    public Portal exit;
    public GameObject[] openWhenCleared;   // doors and barriers that disappear once the room is clear
    public string clearedMessage = "Area cleared! The way forward is open";

    private bool cleared;
    private int keysCollected;

    private void Start()
    {
        if (exit != null) exit.Lock();
    }

    private void Update()
    {
        if (cleared) return;

        // an inactive key means it has been picked up
        int collected = 0;
        foreach (var key in keys)
            if (key == null || !key.gameObject.activeSelf) collected++;

        var hud = GameManager.Instance != null ? GameManager.Instance.Hud : null;
        if (keys.Length > 0 && collected != keysCollected)
        {
            keysCollected = collected;
            if (hud != null && collected < keys.Length) hud.ShowToast("Key " + collected + "/" + keys.Length);
        }

        foreach (var enemy in enemies)
            if (enemy != null && !enemy.IsDead) return;
        if (collected < keys.Length) return;

        cleared = true;
        if (exit != null) exit.Unlock();

        foreach (var barrier in openWhenCleared)
        {
            if (barrier == null) continue;
            barrier.SetActive(false);
            AudioManager.Cue("gate_open", "[Stone grinds open]");
        }

        var gm = GameManager.Instance;
        if (gm == null) return;
        if (!string.IsNullOrEmpty(clearedMessage)) gm.Hud.ShowToast(clearedMessage);
        if (!string.IsNullOrEmpty(completesTask)) gm.Tasks.Complete(completesTask);
    }

    // used by the objective arrow: the closest living enemy or uncollected key within range
    public Transform NearestObjective(Vector2 from, float maxDistance)
    {
        Transform best = null;
        float bestDist = maxDistance * maxDistance;

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy) continue;
            float d = ((Vector2)enemy.transform.position - from).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = enemy.transform; }
        }

        foreach (var key in keys)
        {
            if (key == null || !key.gameObject.activeSelf) continue;
            float d = ((Vector2)key.transform.position - from).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = key.transform; }
        }
        return best;
    }
}
