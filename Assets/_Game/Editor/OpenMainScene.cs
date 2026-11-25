using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// A fresh checkout has no "last opened scene", so Unity starts in an empty Untitled scene
// (just a camera and a light). This opens the game scene instead, once per editor session.
// It only ever replaces a blank, unsaved scene, never anything you're working on.
// Delete this file if you don't want it.
[InitializeOnLoad]
public static class OpenMainSceneOnLoad
{
    private const string MainScene = "Assets/_Game/Scenes/Main.unity";
    private const string SessionKey = "BeyondTheWoods.TriedOpeningMainScene";

    static OpenMainSceneOnLoad()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += OpenIfBlank;
    }

    private static void OpenIfBlank()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var active = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(active.path) || active.isDirty) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScene) == null) return;

        EditorSceneManager.OpenScene(MainScene);
    }
}
