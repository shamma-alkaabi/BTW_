using System.Collections;
using UnityEngine;

// A memory puzzle. The runes flash in a secret order, then you have to step on them in the same
// order. A wrong step hurts a little, resets the runes and shows the order again. Solving it
// opens the door and unlocks the exit. The altar in the middle replays the sequence any time.
public class RunePuzzle : MonoBehaviour
{
    public RunePlate[] plates;          // in index order (plates[i].index == i)
    public RunePlate altar;
    public int[] sequence;              // the order to step on the runes
    public Portal exit;
    public GameObject[] doors;          // barriers that disappear when it's solved
    public string completesTask = "level3";
    public float showDistance = 16f;    // the sequence plays by itself when you walk this close
    public int mistakeDamage = 6;

    private Transform player;
    private Health playerHealth;
    private int progress;
    private bool showing;
    private bool solved;
    private bool introShown;
    private bool playerWasDead;

    private void Start()
    {
        var movement = FindFirstObjectByType<PlayerMovement>();
        if (movement == null) return;
        player = movement.transform;
        playerHealth = movement.GetComponent<Health>();
    }

    private void Update()
    {
        if (solved || player == null) return;

        // dying resets the puzzle
        if (playerHealth.IsDead) playerWasDead = true;
        else if (playerWasDead)
        {
            playerWasDead = false;
            progress = 0;
            introShown = false;
            ResetPlates();
        }

        if (!introShown && !showing && Vector2.Distance(transform.position, player.position) < showDistance)
        {
            introShown = true;
            StartCoroutine(ShowSequence(1.0f));
        }
    }

    public void Step(RunePlate plate)
    {
        var gm = GameManager.Instance;
        if (solved || showing || gm == null || gm.State != GameState.Playing) return;

        if (plate == altar)
        {
            StartCoroutine(ShowSequence(0.2f));
            return;
        }

        if (plate.index == sequence[progress])
        {
            plate.SetLit(true);
            AudioManager.Cue("pickup", "[A rune hums]");
            progress++;
            if (progress >= sequence.Length) Solve();
        }
        else
        {
            StartCoroutine(Mistake());
        }
    }

    private IEnumerator ShowSequence(float delay)
    {
        showing = true;
        progress = 0;
        ResetPlates();
        GameManager.Instance.Hud.ShowToast("Watch the runes...");

        yield return new WaitForSeconds(delay);
        foreach (int i in sequence)
        {
            plates[i].Flash(0.7f);
            AudioManager.Cue("pickup", "[A rune glows]");
            yield return new WaitForSeconds(1.0f);
        }

        GameManager.Instance.Hud.ShowToast("Now step on them in the same order");
        showing = false;
    }

    private IEnumerator Mistake()
    {
        showing = true;
        var gm = GameManager.Instance;
        gm.Hud.ShowToast("Wrong rune! Watch again");
        AudioManager.Cue("enemy_attack", "[The runes flare angrily]");
        if (playerHealth != null && mistakeDamage > 0) playerHealth.TakeDamage(mistakeDamage, transform.position);
        ResetPlates();

        yield return new WaitForSeconds(0.8f);
        showing = false;
        yield return StartCoroutine(ShowSequence(0.3f));
    }

    private void ResetPlates()
    {
        foreach (var plate in plates) plate.SetLit(false);
    }

    private void Solve()
    {
        solved = true;
        var gm = GameManager.Instance;
        gm.Hud.ShowToast("The runes align! The way opens");
        AudioManager.Cue("gate_open", "[Stone grinds open]");

        foreach (var door in doors)
            if (door != null) door.SetActive(false);
        if (exit != null) exit.Unlock();
        if (!string.IsNullOrEmpty(completesTask)) gm.Tasks.Complete(completesTask);
    }
}
