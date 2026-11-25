using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState { Intro, Playing, Dialogue, Paused, Victory }

// Overall game flow: state, pause, captions and the victory screen. The Player, camera and Canvas are BTW's,
// this just sits on top of them.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Intro;
    public bool CaptionsEnabled { get; private set; } = true;

    [SerializeField] private HUD hud;   // the HUD object under BTW's Canvas

    public HUD Hud => hud;
    [HideInInspector] public TaskManager Tasks;

    private GameState stateBeforePause = GameState.Playing;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        Tasks = GetComponent<TaskManager>();
    }

    private void Update()
    {
        // NPC conversations run on their own (NPC + DialogueController), so just follow
        // whether the panel is open. Cutscenes use the Intro state and are left alone.
        bool dialogueOpen = DialogueController.Instance != null && DialogueController.Instance.IsOpen;
        if (dialogueOpen && State == GameState.Playing) SetState(GameState.Dialogue);
        else if (!dialogueOpen && State == GameState.Dialogue) SetState(GameState.Playing);
    }

    // --- input callbacks (PlayerInput > Events > Player) ---

    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (State == GameState.Playing)
        {
            stateBeforePause = State;
            SetState(GameState.Paused);
        }
        else if (State == GameState.Paused)
        {
            SetState(stateBeforePause);
        }
    }

    public void OnToggleCaptions(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        CaptionsEnabled = !CaptionsEnabled;
        Hud.RefreshPause();
        if (!CaptionsEnabled) Hud.HideCaption();
    }

    // Interact (E) restarts the game from the victory screen
    public void OnConfirm(InputAction.CallbackContext context)
    {
        if (context.performed && State == GameState.Victory)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Victory()
    {
        SetState(GameState.Victory);
    }

    public void SetState(GameState newState)
    {
        State = newState;
        Time.timeScale = newState == GameState.Paused ? 0f : 1f;
        if (Hud != null) Hud.OnStateChanged(newState);
    }
}
