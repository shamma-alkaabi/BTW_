using UnityEngine;

// A little arrow that floats around the player and points at the current objective:
// the quest giver or a mushroom, a key, a lever, the boss, or the closest open portal.
// It hides when you're close.
[RequireComponent(typeof(SpriteRenderer))]
public class ObjectiveArrow : MonoBehaviour
{
    public float orbitRadius = 2.2f;
    public float hideDistance = 4f;

    private SpriteRenderer sr;
    private PlayerMovement player;
    private Health playerHealth;
    private Transform target;
    private float nextSearchTime;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.enabled = false;
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMovement>();
        if (player != null) playerHealth = player.GetComponent<Health>();
    }

    private void LateUpdate()
    {
        var gm = GameManager.Instance;
        if (player == null || gm == null || gm.State != GameState.Playing || (playerHealth != null && playerHealth.IsDead))
        {
            sr.enabled = false;
            return;
        }

        // searching every frame would be wasteful, so only look a few times a second
        if (Time.time >= nextSearchTime)
        {
            target = FindTarget(gm);
            nextSearchTime = Time.time + 0.4f;
        }
        if (target == null)
        {
            sr.enabled = false;
            return;
        }

        Vector2 from = (Vector2)player.transform.position + new Vector2(0f, 0.9f);
        Vector2 toTarget = (Vector2)target.position - from;
        if (toTarget.magnitude < hideDistance)
        {
            sr.enabled = false;
            return;
        }

        sr.enabled = true;
        Vector2 dir = toTarget.normalized;
        float bob = Mathf.Sin(Time.time * 6f) * 0.12f;
        transform.position = from + dir * (orbitRadius + bob);

        // the sprite points up by default, hence the -90
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private Transform FindTarget(GameManager gm)
    {
        Vector2 from = player.transform.position;

        // enemies or a boss that belong to the current objective, if they're nearby
        foreach (var task in gm.Tasks.tasks)
        {
            if (task.status != TaskStatus.Active) continue;

            foreach (var room in FindObjectsByType<RoomClearance>(FindObjectsSortMode.None))
            {
                // rooms with no task (inner chambers) count too
                if (!string.IsNullOrEmpty(room.completesTask) && room.completesTask != task.id) continue;
                var target = room.NearestObjective(from, 80f);
                if (target != null) return target;
            }

            foreach (var quest in FindObjectsByType<QuestGate>(FindObjectsSortMode.None))
            {
                if (quest.completesTask != task.id) continue;
                var target = quest.NearestTarget(from);
                if (target != null) return target;
            }

            foreach (var puzzle in FindObjectsByType<LeverPuzzle>(FindObjectsSortMode.None))
            {
                if (puzzle.completesTask != task.id) continue;
                var target = puzzle.NearestLever(from);
                if (target != null) return target;
            }

            foreach (var friend in FindObjectsByType<FriendReunion>(FindObjectsSortMode.None))
            {
                if (friend.completesTask == task.id && !friend.Found) return friend.transform;
            }

            foreach (var boss in FindObjectsByType<BossController>(FindObjectsSortMode.None))
            {
                if (boss.defeatTaskId == task.id && !boss.IsDead && Vector2.Distance(boss.transform.position, from) < 40f)
                    return boss.transform;
            }
        }

        // otherwise head for the closest portal that's open
        Transform best = null;
        float bestDist = float.MaxValue;
        foreach (var portal in FindObjectsByType<Portal>(FindObjectsSortMode.None))
        {
            if (!portal.IsUsable) continue;
            float d = ((Vector2)portal.transform.position - from).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = portal.transform;
            }
        }
        return best;
    }
}
