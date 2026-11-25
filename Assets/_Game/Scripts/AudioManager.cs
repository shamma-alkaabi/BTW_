using System.Collections.Generic;
using UnityEngine;

// Audio is optional. Put clips in Assets/_Game/Resources/Audio/ with these names and they get used:
//   music: music_forest
//   sfx:   footstep, attack, hit, player_hurt, enemy_attack, enemy_die, growl, portal
// Missing clips are just skipped. Each cue can also carry a caption for the subtitles.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource music;   // looping background music (on the Game object)
    [SerializeField] private AudioSource sfx;     // one-shot sound effects (on the Game object)
    private readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    private string currentMusic;

    private void Awake()
    {
        Instance = this;
    }

    private AudioClip Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        if (!cache.TryGetValue(key, out var clip))
        {
            clip = Resources.Load<AudioClip>("Audio/" + key);
            cache[key] = clip;   // cache misses too, so we don't hit Resources every footstep
        }
        return clip;
    }

    // Shortcut so other scripts don't have to null-check the manager.
    public static void Cue(string key, string caption = null, float volume = 1f)
    {
        if (Instance != null) Instance.Play(key, caption, volume);
    }

    public void Play(string key, string caption = null, float volume = 1f)
    {
        var clip = Get(key);
        if (clip != null) sfx.PlayOneShot(clip, volume);

        var gm = GameManager.Instance;
        if (!string.IsNullOrEmpty(caption) && gm != null && gm.Hud != null) gm.Hud.ShowCaption(caption);
    }

    public void PlayMusic(string key)
    {
        if (key == currentMusic) return;
        currentMusic = key;

        var clip = Get(key);
        if (clip == null)
        {
            music.Stop();
            return;
        }
        music.clip = clip;
        music.Play();
    }
}
