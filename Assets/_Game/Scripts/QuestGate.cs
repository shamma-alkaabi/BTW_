using UnityEngine;

// Level 1 is BTW's map and its mushroom quest. This watches BTW's ShroomQuest: it announces each
// mushroom as you pick it up, and when the lady says the quest is complete it opens the exit
// and finishes the level's objective.
public class QuestGate : MonoBehaviour
{
    public string completesTask = "level1";
    public Portal exit;
    public QuestCollectible[] mushrooms;   // the mushrooms placed in the map
    public Transform lady;                 // the NPC who gives the quest

    private int lastCount;
    private bool done;

    private void Start()
    {
        if (exit != null) exit.Lock();
    }

    private void Update()
    {
        var quest = ShroomQuest.Instance;
        var gm = GameManager.Instance;
        if (quest == null || gm == null || done) return;

        if (quest.collectedMushrooms != lastCount)
        {
            lastCount = quest.collectedMushrooms;
            if (lastCount < quest.requiredMushrooms)
                gm.Hud.ShowToast("Mushroom " + lastCount + "/" + quest.requiredMushrooms);
            else
                gm.Hud.ShowToast("That's all of them! Take them back to the lady");
        }

        if (quest.questCompleted)
        {
            done = true;
            if (exit != null) exit.Unlock();
            gm.Hud.ShowToast("Quest complete! The gate has opened");
            gm.Tasks.Complete(completesTask);
        }
    }

    // used by the objective arrow: the lady first, then the nearest mushroom, then the lady again
    public Transform NearestTarget(Vector2 from)
    {
        var quest = ShroomQuest.Instance;
        if (quest == null || done) return null;
        if (!quest.questStarted || quest.collectedMushrooms >= quest.requiredMushrooms) return lady;

        Transform best = null;
        float bestDist = float.MaxValue;
        foreach (var mushroom in mushrooms)
        {
            if (mushroom == null || !mushroom.gameObject.activeInHierarchy) continue;
            float d = ((Vector2)mushroom.transform.position - from).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = mushroom.transform;
            }
        }
        return best != null ? best : lady;
    }
}
