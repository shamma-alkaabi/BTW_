using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    public static DialogueController Instance { get; private set; }
    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    public Image portraitImage;
    public Transform choiceContainer;
    public GameObject choiceButtonPF;

    private bool cutsceneActive;
    private bool advanceRequested;

    public bool IsOpen => dialoguePanel != null && dialoguePanel.activeSelf;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowDialogueUI(bool show)
    {
        dialoguePanel.SetActive(show);
    }
    public void SetNPCInfo(string npcName, Sprite portrait)
    {
        nameText.text = npcName;
        portraitImage.sprite = portrait;
    }
    public void SetDialogueText(string text)
    {
        dialogueText.text = text;
    }
    public void ClearChoices()
    {
        foreach (Transform child in choiceContainer) Destroy(child.gameObject);
    }
    public GameObject CreateChoiceButton(string choiceText, UnityEngine.Events.UnityAction onClick)
    {
        GameObject choiceButton = Instantiate(choiceButtonPF, choiceContainer);
        choiceButton.GetComponentInChildren<TMP_Text>().text = choiceText;
        choiceButton.GetComponent<Button>().onClick.AddListener(onClick);
        return choiceButton;
    }

    // Hooked up to the Interact action next to InteractionDetector. NPCs advance
    // themselves, so this only matters while a cutscene is talking.
    public void OnAdvance(InputAction.CallbackContext context)
    {
        if (cutsceneActive && context.performed) advanceRequested = true;
    }

    // Plays a scripted conversation (the intro uses this). The first press of
    // Interact finishes the line that's typing, the next one moves on.
    public IEnumerator PlayLines(string speaker, Sprite portrait, string[] lines, float typingSpeed = 0.03f)
    {
        cutsceneActive = true;
        ClearChoices();
        SetNPCInfo(speaker, portrait);
        ShowDialogueUI(true);

        foreach (string line in lines)
        {
            advanceRequested = false;
            SetDialogueText("");

            for (int i = 1; i <= line.Length && !advanceRequested; i++)
            {
                SetDialogueText(line.Substring(0, i));
                yield return new WaitForSecondsRealtime(typingSpeed);
            }
            SetDialogueText(line);

            advanceRequested = false;
            while (!advanceRequested) yield return null;
        }

        advanceRequested = false;
        cutsceneActive = false;
        SetDialogueText("");
        ShowDialogueUI(false);
    }
}
