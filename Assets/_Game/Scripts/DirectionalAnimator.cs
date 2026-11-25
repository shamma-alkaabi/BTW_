using UnityEngine;

// Plays the frame animations from a CharacterAnimSet, in 8 directions
// (or just mirrored left/right for the simpler sprites).
[RequireComponent(typeof(SpriteRenderer))]
public class DirectionalAnimator : MonoBehaviour
{
    public CharacterAnimSet set;
    private SpriteRenderer sr;
    private AnimClip current;
    private Sprite[] frames;
    private float t;
    private Dir8 curDir;
    private bool finished;

    public bool Finished => finished;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public static Dir8 FromVector(Vector2 v)
    {
        if (v.sqrMagnitude < 0.0001f) return Dir8.Down;
        // snap the angle to the nearest 45 degrees, then map that to a direction
        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        int slice = ((Mathf.RoundToInt(angle / 45f) % 8) + 8) % 8;
        switch (slice)
        {
            case 0: return Dir8.Right;
            case 1: return Dir8.RightUp;
            case 2: return Dir8.Up;
            case 3: return Dir8.LeftUp;
            case 4: return Dir8.Left;
            case 5: return Dir8.LeftDown;
            case 6: return Dir8.Down;
            default: return Dir8.RightDown;
        }
    }

    // For non-directional sprites: flip the image to face left or right.
    public void FaceX(float x)
    {
        if (set == null || set.directional || Mathf.Abs(x) < 0.05f) return;
        bool left = x < 0f;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        sr.flipX = set.facesRight ? left : !left;
    }

    public void Play(AnimClip clip, Dir8 dir, bool restart = false)
    {
        if (clip == null) return;
        bool changed = clip != current || restart;
        if (!changed && dir == curDir) return;

        if (changed)
        {
            t = 0f;
            finished = false;
        }
        current = clip;
        curDir = dir;
        frames = clip.Get(dir);
        if (frames != null && frames.Length > 0)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            sr.sprite = frames[Mathf.Clamp((int)t, 0, frames.Length - 1)];
        }
    }

    private void Update()
    {
        if (current == null || frames == null || frames.Length == 0) return;
        t += Time.deltaTime * current.fps;
        int index = (int)t;
        if (current.loop)
        {
            index %= frames.Length;
        }
        else if (index >= frames.Length)
        {
            index = frames.Length - 1;
            finished = true;
        }
        sr.sprite = frames[index];
    }
}
