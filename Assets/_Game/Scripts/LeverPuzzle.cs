using UnityEngine;

// A switch puzzle. Every lever flips several gates at once, and you have to find the combination
// that leaves all of them open. Which gates a lever flips is set in leverMasks (bit 0 is gate 0, and so on).
// The levers are all identical on purpose, so you have to experiment, but the game tells you
// up front that only 3 of them are the right ones and shows how you're doing after every pull.
public class LeverPuzzle : MonoBehaviour
{
    public Lever[] levers;
    public GameObject[] gates;          // a closed gate is active, an open one is switched off
    public int[] leverMasks;            // gates flipped by each lever
    public int startOpenMask;           // which gates start open
    public Portal exit;
    public string completesTask = "level3";

    [Header("Player help")]
    public int leversNeeded = 3;        // how many levers the solution uses (only used for the messages)
    public float hintDistance = 14f;    // how close you get before the explanation pops up

    private int openMask;
    private bool solved;
    private bool[] pulled;
    private bool hintShown;
    private PlayerMovement player;
    private Vector2 center;

    private void Start()
    {
        openMask = startOpenMask;
        pulled = new bool[levers.Length];
        Apply();

        // the middle of the levers is a good "you've arrived" spot
        foreach (var lever in levers) center += (Vector2)lever.transform.position;
        center /= Mathf.Max(1, levers.Length);

        player = FindFirstObjectByType<PlayerMovement>();
    }

    private void Update()
    {
        if (hintShown || solved || player == null) return;

        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        if (Vector2.Distance(player.transform.position, center) < hintDistance)
        {
            hintShown = true;
            gm.Hud.ShowToast("There are " + levers.Length + " levers but you only need to pull " + leversNeeded + " of them");
            gm.Hud.ShowCaption("[Each lever opens or closes several gates. Pull it again to undo it]", 5f);
        }
    }

    public void Pull(int lever)
    {
        if (solved) return;

        pulled[lever] = !pulled[lever];
        openMask ^= leverMasks[lever];
        Apply();

        if (openMask == (1 << gates.Length) - 1)
        {
            Solve();
            return;
        }

        ShowProgress();
    }

    private void ShowProgress()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        int pulledCount = 0;
        foreach (bool p in pulled) if (p) pulledCount++;

        int open = 0;
        for (int i = 0; i < gates.Length; i++)
            if ((openMask & (1 << i)) != 0) open++;

        string message = "Levers pulled: " + pulledCount + "/" + leversNeeded + "   Gates open: " + open + "/" + gates.Length;
        if (pulledCount > leversNeeded)
            message += "\nToo many! Only " + leversNeeded + " levers are needed. Pull one again to undo it";

        gm.Hud.ShowToast(message);
    }

    private void Apply()
    {
        for (int i = 0; i < gates.Length; i++)
            gates[i].SetActive((openMask & (1 << i)) == 0);
    }

    private void Solve()
    {
        solved = true;
        var gm = GameManager.Instance;
        gm.Hud.ShowToast("Every gate is open! That was the right " + leversNeeded + " levers");
        AudioManager.Cue("gate_open", "[The vault unseals]");

        if (exit != null) exit.Unlock();
        if (!string.IsNullOrEmpty(completesTask)) gm.Tasks.Complete(completesTask);
    }

    // used by the objective arrow: the closest lever
    public Transform NearestLever(Vector2 from)
    {
        if (solved) return null;

        Transform best = null;
        float bestDist = float.MaxValue;
        foreach (var lever in levers)
        {
            float d = ((Vector2)lever.transform.position - from).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = lever.transform;
            }
        }
        return best;
    }
}
