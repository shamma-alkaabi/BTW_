using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

// The extra UI this game adds on top of BTW's Canvas: objectives, area banner, toasts,
// captions, boss bar, and the pause / title / victory / fade screens.
// All of these are real objects under the Canvas in the scene (see UI > HUD elements),
// this script just drives them. The HP bar and dialogue box are BTW's own (HealthBarUI, DialogueController).
public class HUD : MonoBehaviour
{
    [Header("Objectives")]
    [SerializeField] private TMP_Text objectivesText;

    [Header("Messages")]
    [SerializeField] private TMP_Text bannerText;
    [SerializeField] private CanvasGroup bannerGroup;
    [SerializeField] private TMP_Text toastText;
    [SerializeField] private CanvasGroup toastGroup;
    [SerializeField] private TMP_Text captionText;
    [SerializeField] private CanvasGroup captionGroup;

    [Header("Boss bar")]
    [SerializeField] private GameObject bossPanel;
    [SerializeField] private RectTransform bossFill;
    [SerializeField] private TMP_Text bossLabel;

    [Header("Screens")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TMP_Text pauseCaptionText;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private TMP_Text titleMain;
    [SerializeField] private TMP_Text titleSub;
    [SerializeField] private CanvasGroup fadeGroup;

    private Coroutine bannerRoutine, toastRoutine, captionRoutine, bossGrow;
    private Health bossHealth;

    private void Start()
    {
        GameManager.Instance.Tasks.Changed += RefreshObjectives;
        RefreshObjectives();
        RefreshPause();
    }

    // --- objectives ---

    private void RefreshObjectives()
    {
        var sb = new StringBuilder("<b>OBJECTIVES</b>\n\n");

        // only the latest finished objective, so the list stays short
        GameTask lastDone = null;
        foreach (var task in GameManager.Instance.Tasks.tasks)
            if (task.status == TaskStatus.Complete) lastDone = task;
        if (lastDone != null)
            sb.Append("<color=#8fd18f>[done] ").Append(lastDone.title).Append("</color>\n\n");

        foreach (var task in GameManager.Instance.Tasks.tasks)
            if (task.status == TaskStatus.Active) sb.Append("> ").Append(task.Label).Append("\n\n");

        objectivesText.text = sb.ToString();
    }

    // --- boss bar ---

    public void ShowBossBar(string bossName, Health health)
    {
        HideBossBar();
        bossHealth = health;
        bossLabel.text = bossName;
        health.Changed += OnBossHealth;
        OnBossHealth(health.HP, health.maxHP);

        bossPanel.SetActive(true);
        bossGrow = StartCoroutine(GrowBossBar());
    }

    public void HideBossBar()
    {
        if (bossHealth != null)
        {
            bossHealth.Changed -= OnBossHealth;
            bossHealth = null;
        }
        if (bossGrow != null) StopCoroutine(bossGrow);
        bossPanel.SetActive(false);
    }

    private void OnBossHealth(int hp, int max)
    {
        bossFill.anchorMax = new Vector2(Mathf.Clamp01((float)hp / max), 1f);
    }

    // the bar swells in from the middle when the fight starts
    // (the grow-in idea is from BossHealthBar in 2D-Top-Down-RPG-on-Unity, MIT, Kiril Chingarov)
    private IEnumerator GrowBossBar()
    {
        var rt = (RectTransform)bossPanel.transform;
        var group = bossPanel.GetComponent<CanvasGroup>();

        float t = 0f;
        while (t < 1.2f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 1.2f);
            rt.localScale = new Vector3(k, 1f, 1f);
            group.alpha = k;
            yield return null;
        }
        rt.localScale = Vector3.one;
        group.alpha = 1f;
    }

    // --- banner, toast, captions ---

    public void ShowAreaBanner(string text)
    {
        bannerText.text = text;
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(FadeGroup(bannerGroup, 3.2f));
    }

    public void ShowToast(string text)
    {
        toastText.text = text;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(FadeGroup(toastGroup, 2.6f));
    }

    public void ShowCaption(string text, float seconds = 2.6f)
    {
        if (GameManager.Instance == null || !GameManager.Instance.CaptionsEnabled) return;

        captionText.text = text;
        if (captionRoutine != null) StopCoroutine(captionRoutine);
        captionRoutine = StartCoroutine(FadeGroup(captionGroup, seconds));
    }

    public void HideCaption()
    {
        if (captionRoutine != null) StopCoroutine(captionRoutine);
        captionGroup.alpha = 0;
    }

    // Full opacity for a while, then fade out. Unscaled time so it works while paused.
    private IEnumerator FadeGroup(CanvasGroup group, float holdSeconds)
    {
        group.alpha = 1f;

        float t = 0f;
        while (t < holdSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - t / 0.5f;
            yield return null;
        }
        group.alpha = 0f;
    }

    // --- fades and the title card ---

    public IEnumerator FadeTo(float alpha, float duration)
    {
        float start = fadeGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(start, alpha, t / duration);
            yield return null;
        }
        fadeGroup.alpha = alpha;
    }

    public void SetFadeInstant(float alpha)
    {
        fadeGroup.alpha = alpha;
    }

    public IEnumerator ShowTitle(string main, string sub, float seconds)
    {
        titleMain.text = main;
        titleSub.text = sub;
        titlePanel.SetActive(true);
        fadeGroup.alpha = 0f;

        yield return new WaitForSecondsRealtime(seconds);

        titlePanel.SetActive(false);
        fadeGroup.alpha = 0f;
    }

    // --- screens that depend on game state ---

    public void OnStateChanged(GameState state)
    {
        pausePanel.SetActive(state == GameState.Paused);
        victoryPanel.SetActive(state == GameState.Victory);
    }

    public void RefreshPause()
    {
        bool on = GameManager.Instance != null && GameManager.Instance.CaptionsEnabled;
        pauseCaptionText.text = "Captions: " + (on ? "ON" : "OFF") + "   [C] to toggle";
    }
}
