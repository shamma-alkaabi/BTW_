using System.Collections;
using UnityEngine;

// Opening cutscene: the dare, the friend wandering off, then the creature showing up.
// The player gets control back when it's done.
public class IntroSequence : MonoBehaviour
{
    public DirectionalAnimator friend;
    public EnemyAI creature;           // starts disabled, switched on at the end
    public PlayerLock player;
    public float friendWalkDistance = 7f;
    public Sprite friendPortrait;
    public Sprite playerPortrait;

    private IEnumerator Start()
    {
        var gm = GameManager.Instance;
        gm.SetState(GameState.Intro);
        gm.Hud.SetFadeInstant(1f);
        yield return null;

        player.Face(Vector2.right);
        friend.Play(friend.set.idle, DirectionalAnimator.FromVector(player.transform.position - friend.transform.position));

        yield return gm.Hud.ShowTitle("BEYOND THE WOODS", "Group 15  -  SWEB451", 2.8f);
        yield return gm.Hud.FadeTo(0f, 1.2f);

        yield return Say("Friend", friendPortrait, new[]
        {
            "That's it. The old Whisperwood. Everyone says it's haunted.",
            "You said you weren't scared, right? Then prove it. We go in together. That's the dare."
        });
        yield return Say("You", playerPortrait, new[]
        {
            "Fine. But if I hear anything weird, we're leaving."
        });
        yield return Say("Friend", friendPortrait, new[]
        {
            "Deal. Hang on a sec, I think I saw something over by that old tree."
        });

        yield return FriendWalksOff();

        AudioManager.Cue("growl", "[A distant shout... then silence]");
        yield return new WaitForSeconds(1.6f);
        yield return Say("You", playerPortrait, new[]
        {
            "Hey...? Where'd you go? Hey!"
        });

        AudioManager.Cue("growl", "[A low growl right behind you]");
        creature.gameObject.SetActive(true);
        gm.Tasks.Activate("escape");
        gm.SetState(GameState.Playing);
    }

    // the friend walks north and fades out in the fog
    private IEnumerator FriendWalksOff()
    {
        var sr = friend.GetComponent<SpriteRenderer>();
        float walked = 0f;

        while (walked < friendWalkDistance)
        {
            float step = 2.2f * Time.deltaTime;
            walked += step;
            friend.transform.position += Vector3.up * step;
            friend.Play(friend.set.walk, Dir8.Up);

            var color = sr.color;
            color.a = Mathf.Clamp01((friendWalkDistance - walked) / 2.5f);
            sr.color = color;
            yield return null;
        }
        friend.gameObject.SetActive(false);
    }

    private IEnumerator Say(string speaker, Sprite portrait, string[] lines)
    {
        yield return StartCoroutine(DialogueController.Instance.PlayLines(speaker, portrait, lines));
    }
}
