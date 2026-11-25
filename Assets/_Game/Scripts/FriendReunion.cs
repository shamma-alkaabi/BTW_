using System.Collections;
using UnityEngine;

// The friend from the intro, waiting at the end of the last level. Press the interact key next to
// them to talk. Finding them finishes the final objective, then they tag along behind you.
// The game keeps going afterwards, there is no ending screen.
[RequireComponent(typeof(DirectionalAnimator))]
public class FriendReunion : MonoBehaviour, IInteractable
{
    public string completesTask = "level8";
    public float followSpeed = 3.2f;
    public float followDistance = 2.4f;

    public bool Found { get; private set; }

    private DirectionalAnimator anim;
    private PlayerMovement player;
    private bool talking;

    private void Awake()
    {
        anim = GetComponent<DirectionalAnimator>();
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMovement>();
    }

    public bool CanInteract()
    {
        return !Found && !talking;
    }

    public void Interact()
    {
        if (!CanInteract()) return;
        StartCoroutine(Reunion());
    }

    private IEnumerator Reunion()
    {
        talking = true;
        var gm = GameManager.Instance;
        var intro = FindFirstObjectByType<IntroSequence>();
        Sprite friendFace = intro != null ? intro.friendPortrait : null;
        Sprite playerFace = intro != null ? intro.playerPortrait : null;

        gm.SetState(GameState.Intro);   // locks the controls while we talk

        yield return DialogueController.Instance.PlayLines("Friend", friendFace, new[]
        {
            "You made it! I heard the fighting all the way down here.",
            "I'm so sorry I ran off. I thought I saw a light in the trees and I just kept walking.",
            "Then the ground opened up and I couldn't find my way back."
        });

        yield return DialogueController.Instance.PlayLines("You", playerFace, new[]
        {
            "I'm just glad you're okay. Come on, let's get out of here together."
        });

        yield return DialogueController.Instance.PlayLines("Friend", friendFace, new[]
        {
            "Lead the way. I'll stay right behind you this time."
        });

        Found = true;
        talking = false;
        gm.SetState(GameState.Playing);

        gm.Hud.ShowToast("You found your friend!");
        if (!string.IsNullOrEmpty(completesTask)) gm.Tasks.Complete(completesTask);
    }

    // once found, the friend follows the player around
    private void Update()
    {
        if (!Found || player == null) return;

        var gm = GameManager.Instance;
        if (gm != null && gm.State != GameState.Playing) return;

        Vector2 toPlayer = player.transform.position - transform.position;
        Dir8 facing = DirectionalAnimator.FromVector(toPlayer);

        if (toPlayer.magnitude > followDistance)
        {
            transform.position += (Vector3)(toPlayer.normalized * followSpeed * Time.deltaTime);
            anim.Play(anim.set.walk, facing);
        }
        else
        {
            anim.Play(anim.set.idle, facing);
        }
    }
}
