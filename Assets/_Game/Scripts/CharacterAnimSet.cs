using System;
using UnityEngine;

public enum Dir8 { Down, LeftDown, Left, LeftUp, Up, RightUp, Right, RightDown }

[Serializable] public class SpriteFrames { public Sprite[] frames; }

[Serializable]
public class AnimClip
{
    public float fps = 8f;
    public bool loop = true;
    public SpriteFrames[] dirs = new SpriteFrames[8];

    static bool Has(SpriteFrames f) => f != null && f.frames != null && f.frames.Length > 0;

    public Sprite[] Get(Dir8 d)
    {
        if (dirs == null || dirs.Length < 8) return null;
        int i = (int)d;
        if (Has(dirs[i])) return dirs[i].frames;
        // Some sets have no pure Left/Right strips, so use the "down" diagonal for those.
        // Anything else that's missing just borrows the closest direction that has frames.
        if (d == Dir8.Left && Has(dirs[(int)Dir8.LeftDown])) return dirs[(int)Dir8.LeftDown].frames;
        if (d == Dir8.Right && Has(dirs[(int)Dir8.RightDown])) return dirs[(int)Dir8.RightDown].frames;
        for (int o = 1; o < 8; o++)
        {
            var next = dirs[(i + o) % 8];
            if (Has(next)) return next.frames;

            var prev = dirs[(i - o + 8) % 8];
            if (Has(prev)) return prev.frames;
        }
        return null;
    }
}

// Animation data for one character. The assets live in Assets/Data/AnimSets.
[CreateAssetMenu(menuName = "Beyond the Woods/Character Anim Set")]
public class CharacterAnimSet : ScriptableObject
{
    public bool directional = true;   // false = one set of sprites that gets mirrored for left/right
    public bool facesRight = true;    // only matters when directional is false
    public AnimClip idle, walk, run, attack, death;
}
