using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the two levels that come after the Bone Warden when the game starts:
//   Level 7, the Murky Bog (swamp): mud that slows you, toxic pools, thorn spikes and bone guards.
//                                   Clear every guard to open the portal.
//   Level 8, the Frozen Hollow (ice): slippery ice, sliding spikes and a few guards, with your
//                                     friend waiting at the far end.
//
// The rooms are made from copies of things that already exist in the scene (the boss arena for the
// walls and floor, a maze guard for the enemies, a spike from level 5, a portal), so the new levels
// look like the rest of the game and nothing has to be placed by hand in the editor.
// Beating the boss unlocks a portal in the boss arena that leads to level 7.
public class ExtraLevels : MonoBehaviour
{
    public Vector2 level7Center = new Vector2(700f, 0f);
    public Vector2 level8Center = new Vector2(800f, 0f);
    public Vector2 bossPortalPosition = new Vector2(600f, 11f);   // where the portal appears in the boss arena

    private Transform bossArena;        // level 6, used as the template for the new rooms
    private EnemyAI guardTemplate;
    private SpikeTrap spikeTemplate;
    private Portal portalTemplate;
    private Transform friendTemplate;

    // make sure it exists in the main scene without having to add it by hand
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (SceneManager.GetActiveScene().name != "Main") return;
        if (FindFirstObjectByType<ExtraLevels>() != null) return;
        new GameObject("ExtraLevels").AddComponent<ExtraLevels>();
    }

    private IEnumerator Start()
    {
        yield return null;   // let every other script finish its own Start first

        if (!FindTemplates())
        {
            Debug.LogWarning("ExtraLevels: couldn't find the objects to copy, so levels 7 and 8 weren't built.");
            yield break;
        }
        Build();
    }

    // ---------- finding what to copy ----------

    private static T Find<T>(string objectName) where T : Component
    {
        foreach (var c in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.name == objectName) return c;
        return null;
    }

    private static T Nearest<T>(string objectName, Vector2 point) where T : Component
    {
        T best = null;
        float bestDist = float.MaxValue;
        foreach (var c in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.name != objectName) continue;
            float d = ((Vector2)c.transform.position - point).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = c; }
        }
        return best;
    }

    private bool FindTemplates()
    {
        bossArena = Find<Transform>("Level6");
        guardTemplate = Find<EnemyAI>("MazeGuard1");
        spikeTemplate = Nearest<SpikeTrap>("SpikeTrap", new Vector2(500f, 0f));
        portalTemplate = Nearest<Portal>("ExitPortal", new Vector2(500f, 0f));
        friendTemplate = Find<DirectionalAnimator>("Friend_Intro")?.transform;
        return bossArena != null && guardTemplate != null && spikeTemplate != null
            && portalTemplate != null && friendTemplate != null;
    }

    // ---------- building ----------

    private void Build()
    {
        Room room7 = BuildRoom("Level7", level7Center, "The Murky Bog",
            new Color(0.62f, 0.75f, 0.42f), new Color(0.04f, 0.07f, 0.03f), true);
        Room room8 = BuildRoom("Level8", level8Center, "The Frozen Hollow",
            new Color(0.7f, 0.9f, 1f), new Color(0.05f, 0.08f, 0.15f), false);

        // boss arena -> level 7 (locked until the Warden is dead)
        Portal bossPortal = MakePortal(bossPortalPosition, room7.start, room7.bounds, null);
        bossPortal.Lock();
        var tasks = GameManager.Instance.Tasks;
        tasks.TaskCompleted += task => { if (task.id == "level6") bossPortal.Unlock(); };
        if (tasks.IsComplete("level6")) bossPortal.Unlock();

        BuildLevel7(room7, room8);
        BuildLevel8(room8);
    }

    private class Room
    {
        public Vector2 center;
        public Transform start;
        public Collider2D bounds;
        public SpriteRenderer floor;   // patches are drawn just above this
    }

    // copies the boss arena's floor, walls, pillars and decor, and leaves out the fight itself
    private Room BuildRoom(string roomName, Vector2 center, string areaName, Color floorTint, Color background, bool keepPillars)
    {
        Vector2 arena = bossArena.position;

        // building the copy under a switched-off parent means none of its scripts wake up half-built
        var holder = new GameObject("_building");
        holder.SetActive(false);
        var copy = Instantiate(bossArena.gameObject, holder.transform);
        copy.name = roomName;

        var keep = new HashSet<string> { "Ground", "Zone", "ArenaWall", "Decor" };
        if (keepPillars) keep.Add("Pillar");

        var toRemove = new List<GameObject>();
        foreach (Transform child in copy.transform)
            if (!keep.Contains(child.name)) toRemove.Add(child.gameObject);
        foreach (var go in toRemove) DestroyImmediate(go);

        SpriteRenderer floor = null;
        var ground = copy.transform.Find("Ground");
        if (ground != null)
        {
            floor = ground.GetComponent<SpriteRenderer>();
            if (floor != null) floor.color = floor.color * floorTint;
        }

        var zone = copy.GetComponentInChildren<AreaZone>(true);
        if (zone != null)
        {
            zone.areaName = areaName;
            zone.background = background;
        }

        copy.transform.SetParent(null);
        copy.transform.position = center;
        Destroy(holder);

        var room = new Room { center = center, floor = floor };

        // camera limits: a copy of the boss arena's shape
        var arenaBounds = Nearest<Collider2D>("Bounds", arena);
        if (arenaBounds != null)
        {
            var boundsCopy = Instantiate(arenaBounds.gameObject, (Vector3)center + (arenaBounds.transform.position - bossArena.position), Quaternion.identity);
            boundsCopy.name = roomName + "_Bounds";
            room.bounds = boundsCopy.GetComponent<Collider2D>();
        }

        // where the player appears (same spot relative to the room as in the boss arena)
        var arenaStart = Nearest<Transform>("Start", arena);
        Vector3 startOffset = arenaStart != null ? arenaStart.position - bossArena.position : new Vector3(0f, -14f, 0f);
        var startObject = new GameObject(roomName + "_Start");
        startObject.transform.position = (Vector3)center + startOffset;
        room.start = startObject.transform;

        return room;
    }

    // ---------- level 7: the swamp ----------

    private void BuildLevel7(Room room, Room next)
    {
        Vector2 c = room.center;

        // mud slows you down, toxic pools hurt. The pools sit between the guards and the exit
        MakeZone(room, c + new Vector2(-8f, -6f), new Vector2(9f, 5f), TerrainZone.Kind.Mud);
        MakeZone(room, c + new Vector2(9f, -8f), new Vector2(8f, 4f), TerrainZone.Kind.Mud);
        MakeZone(room, c + new Vector2(-12f, 5f), new Vector2(7f, 5f), TerrainZone.Kind.Mud);
        MakeZone(room, c + new Vector2(11f, 4f), new Vector2(7f, 5f), TerrainZone.Kind.Mud);
        MakeZone(room, c + new Vector2(0f, -2f), new Vector2(6f, 4f), TerrainZone.Kind.Poison);
        MakeZone(room, c + new Vector2(-4f, 6f), new Vector2(5f, 3f), TerrainZone.Kind.Poison);
        MakeZone(room, c + new Vector2(5f, 8f), new Vector2(5f, 3f), TerrainZone.Kind.Poison);

        var guards = new List<EnemyAI>();
        Vector2[] guardSpots =
        {
            new Vector2(-14f, 1f), new Vector2(-6f, 9f), new Vector2(3f, 3f),
            new Vector2(14f, 0f),  new Vector2(-9f, -6f), new Vector2(10f, -7f)
        };
        for (int i = 0; i < guardSpots.Length; i++)
            guards.Add(SpawnGuard(c + guardSpots[i], "BogGuard" + (i + 1), false, 9f, 10));

        // thorns growing out of the mud
        Vector2[] thornSpots =
        {
            new Vector2(-4f, -9f), new Vector2(4f, -5f), new Vector2(-16f, -3f),
            new Vector2(16f, 8f), new Vector2(0f, 1f)
        };
        for (int i = 0; i < thornSpots.Length; i++)
            SpawnSpike(c + thornSpots[i], 1.4f + 0.2f * (i % 3), 1.1f, null, 0f);

        Portal exit = MakePortal(c + new Vector2(0f, 11f), next.start, next.bounds, null);

        var clearance = gameObject.AddComponent<RoomClearance>();
        clearance.completesTask = "level7";
        clearance.enemies = guards.ToArray();
        clearance.keys = new KeyPickup[0];
        clearance.openWhenCleared = new GameObject[0];
        clearance.exit = exit;
        clearance.clearedMessage = "The bog goes quiet. The portal is open";
    }

    // ---------- level 8: the ice cave ----------

    private void BuildLevel8(Room room)
    {
        Vector2 c = room.center;

        // almost the whole floor is ice, with a little solid ground at the start and by the friend
        MakeZone(room, c + new Vector2(-9f, -5f), new Vector2(16f, 8f), TerrainZone.Kind.Ice);
        MakeZone(room, c + new Vector2(10f, -4f), new Vector2(14f, 10f), TerrainZone.Kind.Ice);
        MakeZone(room, c + new Vector2(-10f, 5f), new Vector2(14f, 7f), TerrainZone.Kind.Ice);
        MakeZone(room, c + new Vector2(9f, 6f), new Vector2(14f, 6f), TerrainZone.Kind.Ice);

        // three guards that only notice you when you get close
        SpawnGuard(c + new Vector2(-12f, 2f), "FrostGuard1", false, 7f, 10);
        SpawnGuard(c + new Vector2(12f, -2f), "FrostGuard2", false, 7f, 10);
        SpawnGuard(c + new Vector2(0f, 6f), "FrostGuard3", false, 7f, 10);

        // three sliding rows of spikes, each leaving a gap on the opposite side to slip through
        SpawnSpike(c + new Vector2(-16f, -6f), 1.0f, 1.2f, new[] { new Vector2(15f, 0f), Vector2.zero }, 3f);
        SpawnSpike(c + new Vector2(16f, 0f), 1.0f, 1.2f, new[] { new Vector2(-15f, 0f), Vector2.zero }, 3f);
        SpawnSpike(c + new Vector2(-16f, 5f), 1.0f, 1.2f, new[] { new Vector2(15f, 0f), Vector2.zero }, 3f);

        // a couple of fixed spikes near the end so the last stretch isn't a free walk
        SpawnSpike(c + new Vector2(-4f, 9f), 1.5f, 1.0f, null, 0f);
        SpawnSpike(c + new Vector2(4f, 9f), 1.5f, 1.0f, null, 0f);

        // the friend
        var friend = Instantiate(friendTemplate.gameObject, (Vector3)(c + new Vector2(0f, 11f)), Quaternion.identity);
        friend.name = "Friend_Found";
        friend.SetActive(true);
        var sr = friend.GetComponent<SpriteRenderer>();
        if (sr != null) { var col = sr.color; col.a = 1f; sr.color = col; }   // the intro fades the original out

        var trigger = friend.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.8f;

        friend.AddComponent<FriendReunion>();
    }

    // ---------- terrain patches ----------

    private static Sprite squareSprite;

    // a coloured rectangle on the floor with a trigger that does the actual work
    private void MakeZone(Room room, Vector2 position, Vector2 size, TerrainZone.Kind kind)
    {
        if (squareSprite == null)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            squareSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        var go = new GameObject(kind + "Patch");
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        switch (kind)
        {
            case TerrainZone.Kind.Mud: sr.color = new Color(0.35f, 0.24f, 0.12f, 0.8f); break;
            case TerrainZone.Kind.Poison: sr.color = new Color(0.3f, 0.75f, 0.2f, 0.65f); break;
            case TerrainZone.Kind.Ice: sr.color = new Color(0.7f, 0.9f, 1f, 0.55f); break;
        }
        if (room.floor != null)
        {
            sr.sharedMaterial = room.floor.sharedMaterial;
            sr.sortingLayerID = room.floor.sortingLayerID;
            sr.sortingOrder = room.floor.sortingOrder + 1;
        }

        // the collider is sized in the square's own units, so it stretches with the scale
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = Vector2.one;

        go.AddComponent<TerrainZone>().kind = kind;
    }

    // ---------- things to copy ----------

    private EnemyAI SpawnGuard(Vector2 position, string guardName, bool alwaysChase, float detectRadius, int damage)
    {
        var guard = Instantiate(guardTemplate, (Vector3)position, Quaternion.identity);
        guard.gameObject.SetActive(true);
        guard.name = guardName;
        guard.alwaysChase = alwaysChase;
        guard.detectRadius = detectRadius;
        guard.giveUpRadius = detectRadius * 1.5f;
        guard.damage = damage;
        return guard;
    }

    private SpikeTrap SpawnSpike(Vector2 position, float inactiveSeconds, float activeSeconds, Vector2[] path, float moveSpeed)
    {
        var spike = Instantiate(spikeTemplate, (Vector3)position, Quaternion.identity);
        spike.gameObject.SetActive(true);
        spike.inactiveSeconds = inactiveSeconds;
        spike.activeSeconds = activeSeconds;
        spike.pathOffsets = path;
        if (path != null) spike.moveSpeed = moveSpeed;
        return spike;
    }

    private Portal MakePortal(Vector2 position, Transform destination, Collider2D destinationBounds, string completesTask)
    {
        var portal = Instantiate(portalTemplate, (Vector3)position, Quaternion.identity);
        portal.gameObject.SetActive(true);
        portal.destination = destination;
        portal.destinationBounds = destinationBounds;
        portal.cameraBounds = portalTemplate.cameraBounds;
        portal.respawn = portalTemplate.respawn;
        portal.completesTask = completesTask;
        portal.disableOnUse = null;
        return portal;
    }
}
