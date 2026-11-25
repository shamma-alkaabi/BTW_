using UnityEngine;

// A patch of ground that changes how the player moves or hurts them:
//   Mud    - slows you down (swamp)
//   Poison - a toxic pool that hurts every second you stand in it (swamp)
//   Ice    - you slide and can't stop or turn quickly (ice cave)
// The first time you touch each kind, a short message tells you what it does.
[RequireComponent(typeof(BoxCollider2D))]
public class TerrainZone : MonoBehaviour
{
    public enum Kind { Mud, Poison, Ice }

    public Kind kind = Kind.Mud;
    public float mudSpeed = 0.5f;
    public int poisonDamage = 3;
    public float poisonInterval = 1f;

    private static readonly bool[] explained = new bool[3];
    private float nextPoison;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetExplained()
    {
        for (int i = 0; i < explained.Length; i++) explained[i] = false;
    }

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerMovement>();
        if (player == null) return;

        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        Explain(gm);

        switch (kind)
        {
            case Kind.Mud:
                player.SetTerrain(mudSpeed, false);
                break;
            case Kind.Ice:
                player.SetTerrain(1f, true);
                break;
            case Kind.Poison:
                HurtPlayer(player);
                break;
        }
    }

    private void HurtPlayer(PlayerMovement player)
    {
        if (Time.time < nextPoison) return;

        var health = player.GetComponent<Health>();
        if (health == null || health.IsDead) return;

        // the "hit came from" position is the player's own, so the poison doesn't shove them anywhere
        if (health.TakeDamage(poisonDamage, player.transform.position))
            nextPoison = Time.time + poisonInterval;
    }

    private void Explain(GameManager gm)
    {
        int i = (int)kind;
        if (explained[i]) return;
        explained[i] = true;

        switch (kind)
        {
            case Kind.Mud: gm.Hud.ShowToast("Thick mud! It slows you down"); break;
            case Kind.Poison: gm.Hud.ShowToast("Toxic water! Get out before it hurts you"); break;
            case Kind.Ice: gm.Hud.ShowToast("Slippery ice! You slide when you move"); break;
        }
    }
}
