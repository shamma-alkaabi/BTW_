using System;
using System.Collections.Generic;
using UnityEngine;

public enum TaskStatus { Locked, Active, Complete }

public class GameTask
{
    public string id;
    public string title;
    public string activatesNext;   // id of the task that unlocks when this one is done
    public int goal = 1;
    public int progress;
    public TaskStatus status = TaskStatus.Locked;

    public string Label => goal > 1 ? $"{title} ({progress}/{goal})" : title;
}

// Keeps track of the objectives. Add more in Define() and the HUD picks them up automatically.
public class TaskManager : MonoBehaviour
{
    public readonly List<GameTask> tasks = new List<GameTask>();

    public event Action Changed;
    public event Action<GameTask> TaskCompleted;

    private void Awake()
    {
        Define();
    }

    private void Define()
    {
        tasks.Clear();
        Add("escape", "Escape the creature: follow the path east to the glowing portal", 1, "level1");
        Add("level1", "Level 1: talk to the lady and bring her the mushrooms", 1, "level2");
        Add("level2", "Level 2: find the 3 keys and escape the maze", 1, "level3");
        Add("level3", "Level 3: open all 5 gates. Only 3 of the 5 levers are needed, so pull one again to undo it", 1, "level4");
        Add("level4", "Level 4: repeat the rune sequence to open the door", 1, "level5");
        Add("level5", "Level 5: cross the spike gauntlet", 1, "level6");
        Add("level6", "Level 6: defeat the Bone Warden", 1, "level7");
        Add("level7", "Level 7: cross the Murky Bog, beat its guards and take the portal", 1, "level8");
        Add("level8", "Level 8: cross the Frozen Hollow and find your friend", 1, null);
    }

    private void Add(string id, string title, int goal, string next)
    {
        tasks.Add(new GameTask { id = id, title = title, goal = goal, activatesNext = next });
    }

    public GameTask Get(string id) => tasks.Find(t => t.id == id);

    public bool IsActive(string id)
    {
        var t = Get(id);
        return t != null && t.status == TaskStatus.Active;
    }

    public bool IsComplete(string id)
    {
        var t = Get(id);
        return t != null && t.status == TaskStatus.Complete;
    }

    public void Activate(string id)
    {
        var t = Get(id);
        if (t == null || t.status != TaskStatus.Locked) return;

        t.status = TaskStatus.Active;
        Changed?.Invoke();

        // progress can already be full if the player got there early
        if (t.progress >= t.goal)
        {
            Complete(id);
            return;
        }

        var hud = GameManager.Instance != null ? GameManager.Instance.Hud : null;
        if (hud != null) hud.ShowToast("New objective: " + t.title);
    }

    public void Progress(string id, int amount = 1)
    {
        var t = Get(id);
        if (t == null || t.status == TaskStatus.Complete) return;

        t.progress = Mathf.Min(t.goal, t.progress + amount);
        Changed?.Invoke();

        if (t.status == TaskStatus.Active && t.progress >= t.goal) Complete(id);
    }

    public void Complete(string id)
    {
        var t = Get(id);
        if (t == null || t.status == TaskStatus.Complete) return;

        t.progress = t.goal;
        t.status = TaskStatus.Complete;
        Changed?.Invoke();
        TaskCompleted?.Invoke(t);

        var hud = GameManager.Instance != null ? GameManager.Instance.Hud : null;
        if (hud != null) hud.ShowToast("Objective complete!");

        if (!string.IsNullOrEmpty(t.activatesNext)) Activate(t.activatesNext);
    }
}
