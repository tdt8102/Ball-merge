using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Whole game in one component: builds the playfield, handles drop input, merging,
/// score and game over. Put it on an empty GameObject in a scene with an orthographic camera.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ---- Layout (world units). Container inner width = 6, floor top at FloorY. ----
    const float HalfWidth = 3f;
    const float FloorY = -5.2f;
    const float WallTopY = 3.8f;
    const float DangerY = 3.0f;       // cats resting above this line for too long = game over
    const float SpawnY = 4.5f;        // dropper height
    const float WallThickness = 0.5f;
    const float ViewWidth = 7.4f;     // world width always visible on screen
    const float MinViewHeightHalf = 6.6f;

    static float DropCooldown => Balance.Current.dropCooldown;
    static float GameOverDelay => Balance.Current.gameOverDelay;
    static float DangerGrace => Balance.Current.dangerGrace;   // a freshly dropped cat may be above the line this long

    public int Score { get; private set; }
    public int Best { get; private set; }
    public int NextLevel { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool Paused { get; set; }               // settings panel open
    public int RunCoins { get; private set; }      // coins earned in the current game
    public int CatCount => _cats.Count;

    GameUI _ui;
    PhysicMaterial _mat;
    readonly List<Cat> _cats = new List<Cat>();
    Cat _held;
    SpriteRenderer _aimLine, _dangerLine;
    float _aimX, _dangerTimer, _cooldown;
    bool _aiming;
    int _combo;
    float _lastMergeTime = -100f, _warnT;
    static float ComboWindow => Balance.Current.comboWindow;   // max gap between merges to count as a combo

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;                // in case the previous scene was left while paused
#if !UNITY_WEBGL
        Application.targetFrameRate = 60;   // WebGL: keep the default (requestAnimationFrame); a fixed target makes it use setTimeout and stutter on phones
#endif
        Time.maximumDeltaTime = 0.06f;      // slow frames slow the game down instead of piling up physics steps
        Physics.gravity = new Vector3(0f, -Balance.Current.gravity, 0f);
        Physics.defaultSolverIterations = 12;              // cats are created after this, so they all get the better solver
        Physics.defaultSolverVelocityIterations = 4;
        Best = PlayerData.Best;

        _mat = new PhysicMaterial("Cat")
        {
            dynamicFriction = Balance.Current.friction, staticFriction = Balance.Current.friction, bounciness = Balance.Current.bounciness,
            frictionCombine = PhysicMaterialCombine.Average, bounceCombine = PhysicMaterialCombine.Average
        };

        SetupCamera();
        new GameObject("Audio").AddComponent<AudioManager>();
        new GameObject("Fx").AddComponent<Fx>();
        BuildPlayfield();
        _ui = new GameObject("UI").AddComponent<GameUI>();
        _ui.Build(this);
    }

    void Start()
    {
        var run = PlayerData.ResumeRequested ? PlayerData.Run : null;
        PlayerData.ResumeRequested = false;
        if (run != null) ResumeRun(run); else NewGame();
    }

    // ------------------------------------------------------------------ setup

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
        }
        cam.orthographic = true;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.backgroundColor = Cosmetics.Bg.Outer;
        cam.clearFlags = CameraClearFlags.SolidColor;
        // Fit width first; make sure the full height is visible too.
        cam.orthographicSize = Mathf.Max(ViewWidth / cam.aspect * 0.5f, MinViewHeightHalf);
    }

    void BuildPlayfield()
    {
        var root = new GameObject("Playfield").transform;
        var bg = Cosmetics.Bg;
        var frame = Cosmetics.Frame;

        // background panel (+ the repeating motif of the chosen background)
        float h = WallTopY - FloorY;
        var panelPos = new Vector2(0, FloorY + h * 0.5f);
        Quad(root, "Panel", panelPos, new Vector2(HalfWidth * 2, h), bg.Panel, -10);
        var tile = CatSprites.MotifTile(bg, 1.5f);
        if (tile != null)
        {
            var go = new GameObject("Motif");
            go.transform.SetParent(root, false);
            go.transform.position = panelPos;
            var m = go.AddComponent<SpriteRenderer>();
            m.sprite = tile;
            m.drawMode = SpriteDrawMode.Tiled;
            m.size = new Vector2(HalfWidth * 2, h);
            m.sortingOrder = -9;
        }

        float wallH = 20f;

        // visible frame: thin tray with round wall tops and a softly rounded outer bottom corner.
        // The colliders below are thicker invisible boxes along the same inner edge, so physics is unchanged.
        const float t = 0.22f;
        float xc = HalfWidth + t * 0.5f, topC = WallTopY - t * 0.5f, botC = FloorY - t * 0.5f;
        foreach (float s in new[] { -1f, 1f })
        {
            FrameBar(root, new Vector2(s * xc, (topC + botC) * 0.5f), new Vector2(t, topC - botC), true, frame);
            Disc(root, new Vector2(s * xc, topC), t, frame.First, 5);     // round top
            Disc(root, new Vector2(s * xc, botC), t, frame.First, 5);     // round outer bottom corner
        }
        FrameBar(root, new Vector2(0, botC), new Vector2(xc * 2f, t), false, frame);

        // left / right / floor (colliders only)
        Wall(root, "WallL", new Vector2(-HalfWidth - WallThickness * 0.5f, FloorY + wallH * 0.5f - WallThickness), new Vector2(WallThickness, wallH));
        Wall(root, "WallR", new Vector2(HalfWidth + WallThickness * 0.5f, FloorY + wallH * 0.5f - WallThickness), new Vector2(WallThickness, wallH));
        Wall(root, "Floor", new Vector2(0, FloorY - WallThickness * 0.5f), new Vector2(HalfWidth * 2 + WallThickness * 2, WallThickness));

        // danger line
        _dangerLine = Quad(root, "DangerLine", new Vector2(0, DangerY), new Vector2(HalfWidth * 2, 0.06f),
                           new Color(0.9f, 0.2f, 0.2f, 0.45f), 0);
        // aim guide (dark on light backgrounds, light on dark ones)
        var aim = bg.Ink;
        aim.a = 0.22f;
        _aimLine = Quad(root, "Aim", new Vector2(0, (SpawnY + FloorY) * 0.5f), new Vector2(0.04f, SpawnY - FloorY), aim, 0);
    }

    /// <summary>One side of the tray. Frames with bands are cut into alternating coloured segments; some have a thin accent line down the middle.</summary>
    static void FrameBar(Transform parent, Vector2 center, Vector2 size, bool vertical, FrameStyle f)
    {
        float length = vertical ? size.y : size.x;
        if (f.Bands == null)
            Quad(parent, "WallVisual", center, size, f.Color, 5);
        else
        {
            const float seg = 0.45f;
            int n = Mathf.CeilToInt(length / seg);
            for (int i = 0; i < n; i++)
            {
                float a = -length * 0.5f + i * seg, b = Mathf.Min(a + seg, length * 0.5f);
                float mid = (a + b) * 0.5f, len = b - a;
                Quad(parent, "WallVisual", center + (vertical ? new Vector2(0, mid) : new Vector2(mid, 0)),
                     vertical ? new Vector2(size.x, len) : new Vector2(len, size.y), f.Bands[i % f.Bands.Length], 5);
            }
        }
        if (f.Accent.a > 0f)
            Quad(parent, "Accent", center, vertical ? new Vector2(size.x * 0.22f, length) : new Vector2(length, size.y * 0.22f), f.Accent, 6);
    }

    static SpriteRenderer Quad(Transform parent, string name, Vector2 pos, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = size;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Square;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    static void Disc(Transform parent, Vector2 pos, float diameter, Color color, int order)
    {
        var go = new GameObject("Cap");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * diameter;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Disc;
        sr.color = color;
        sr.sortingOrder = order;
    }

    void Wall(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(size.x, size.y, 4f);          // deep enough that nothing can slip past in Z
        box.sharedMaterial = _mat;
    }

    // ------------------------------------------------------------------ flow

    public void NewGame()
    {
        ResetBoard();
        NextLevel = CatDatabase.RandomDropLevel();
        PlayerData.ClearRun();
        _ui.Refresh();
        _ui.ShowGameOver(false);
    }

    void ResetBoard()
    {
        foreach (var f in _cats) if (f != null) Destroy(f.gameObject);
        _cats.Clear();
        if (_held != null) Destroy(_held.gameObject);
        _held = null;

        Score = 0;
        RunCoins = 0;
        IsGameOver = false;
        _dangerTimer = 0f;
        _combo = 0;
        _lastMergeTime = -100f;
        _cooldown = 0.2f;
        _aiming = false;
        _aimX = 0f;
    }

    // ------------------------------------------------------------------ saving the game in progress

    /// <summary>Put the board back the way it was saved (the Continue button).</summary>
    void ResumeRun(RunSnapshot run)
    {
        ResetBoard();
        Score = run.score;
        RunCoins = run.coins;
        NextLevel = run.next;
        foreach (var c in run.cats)
        {
            var cat = Cat.Create(c.l, new Vector2(c.x, c.y), _mat, preview: false);
            cat.Restore(c.r, new Vector2(c.vx, c.vy), c.w);
            _cats.Add(cat);
        }
        _ui.Refresh();
        _ui.ShowGameOver(false);
    }

    RunSnapshot BuildSnapshot()
    {
        var run = new RunSnapshot { score = Score, next = _held != null ? _held.Level : NextLevel, coins = RunCoins };
        foreach (var c in _cats)
        {
            if (c == null || !c.Dropped || c.Merging) continue;       // the cat in hand and cats about to merge are not part of the board
            var t = c.transform;
            var v = c.Velocity;
            run.cats.Add(new CatState { l = c.Level, x = t.position.x, y = t.position.y, r = t.eulerAngles.z, vx = v.x, vy = v.y, w = c.AngularVelocity });
        }
        return run;
    }

    /// <summary>Save the game in progress and everything else (called regularly, on pause, on page hide and before leaving).</summary>
    public void SaveProgress()
    {
        if (!IsGameOver) PlayerData.SaveRun(BuildSnapshot());
        PlayerData.Flush();
    }

    void SpawnHeld()
    {
        _held = Cat.Create(NextLevel, new Vector2(_aimX, SpawnY), _mat, preview: true);
        _cats.Add(_held);
        NotifyAppear(_held.Level);
        NextLevel = CatDatabase.RandomDropLevel();
        _ui.Refresh();
    }

    // ------------------------------------------------------------------ coins and the Cat Codex

    void AddCoins(int amount)
    {
        PlayerData.AddCoins(amount);
        RunCoins += amount;
        _ui.RefreshCoins();
    }

    /// <summary>A cat just appeared (dropped or created by a merge): count it, and reward the first time ever.</summary>
    void NotifyAppear(int level)
    {
        if (!PlayerData.RecordSeen(level)) return;
        int reward = CatDatabase.DiscoveryReward(level);
        if (reward <= 0) return;                      // the starter cat is known from the start
        AddCoins(reward);
        _ui.ShowDiscovery(level, reward);
        AudioManager.Instance.Discover();
        Haptics.Medium();
    }

    float _flushT;

    void OnDestroy() { PlayerData.Flush(); }

    void Update()
    {
        _flushT += Time.unscaledDeltaTime;
        if (_flushT > 3f) { _flushT = 0f; SaveProgress(); }          // keep the save fresh in case the tab is just closed

        if (IsGameOver || Paused) return;

        if (_held == null)
        {
            _cooldown -= Time.deltaTime;
            if (_cooldown <= 0f) SpawnHeld();
        }

        HandleInput();
        CheckDanger();
        ScanMerges();
    }

    // Safety net: two same-level cats that touch are merged by this scan (10 times a second) in case the collision event
    // (Cat.OnCollisionEnter) did not fire for them, e.g. a new cat that appears already touching its twin.
    float _scanT;
    readonly List<Cat> _mergeA = new List<Cat>(), _mergeB = new List<Cat>();

    void ScanMerges()
    {
        _scanT -= Time.deltaTime;
        if (_scanT > 0f) return;
        _scanT = 0.1f;

        _mergeA.Clear();
        _mergeB.Clear();
        for (int i = 0; i < _cats.Count; i++)
        {
            var a = _cats[i];
            if (a == null || a.Merging || !a.Dropped) continue;
            for (int j = i + 1; j < _cats.Count; j++)
            {
                var b = _cats[j];
                if (b == null || b.Merging || !b.Dropped || b.Level != a.Level) continue;
                float reach = a.Radius + b.Radius + 0.02f;
                if (((Vector2)(a.transform.position - b.transform.position)).sqrMagnitude > reach * reach) continue;
                _mergeA.Add(a);
                _mergeB.Add(b);
                break;
            }
        }
        for (int k = 0; k < _mergeA.Count; k++)
            if (!_mergeA[k].Merging && !_mergeB[k].Merging) Merge(_mergeA[k], _mergeB[k]);   // a cat can appear in two pairs: the second one is skipped
    }

    void HandleInput()
    {
        // Track the press even while no cat is held (cooldown). Otherwise a finger that is already
        // down when the next cat appears is ignored until the player lifts and taps again.
        bool up = Input.GetMouseButtonUp(0);
        if (Input.GetMouseButtonDown(0))
        {
            _aiming = !PointerOverUI();
            if (_aiming) _aimX = PointerWorldX();
        }
        if (_aiming && Input.GetMouseButton(0)) _aimX = PointerWorldX();

        if (_held == null)
        {
            _aimLine.enabled = false;
            if (up || !Input.GetMouseButton(0)) _aiming = false;   // released during cooldown: nothing to drop
            return;
        }
        _aimLine.enabled = true;

        float limit = HalfWidth - _held.Radius;
        _aimX = Mathf.Clamp(_aimX, -limit, limit);
        _held.transform.position = new Vector3(_aimX, SpawnY, 0);
        _aimLine.transform.position = new Vector3(_aimX, (SpawnY + FloorY) * 0.5f, 0);

        if (_aiming && up)
        {
            _aiming = false;
            _held.Release();
            AudioManager.Instance.Drop();
            _held = null;
            _cooldown = DropCooldown;
        }
        else if (_aiming && !Input.GetMouseButton(0)) _aiming = false;   // touch was cancelled (no release event)
    }

    static bool PointerOverUI()
    {
        if (EventSystem.current == null) return false;
        int id = Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1;
        return EventSystem.current.IsPointerOverGameObject(id);
    }

    static float PointerWorldX() => Camera.main.ScreenToWorldPoint(Input.mousePosition).x;

    // ------------------------------------------------------------------ merging

    public void Merge(Cat a, Cat b)
    {
        if (IsGameOver) return;
        a.Merging = b.Merging = true;
        int level = a.Level;
        Vector2 pos = (a.transform.position + b.transform.position) * 0.5f;

        // combo: merges that follow each other within ComboWindow seconds
        _combo = Time.time - _lastMergeTime <= ComboWindow ? _combo + 1 : 1;
        _lastMergeTime = Time.time;
        if (_combo >= 2) _ui.ShowCombo(pos, _combo);
        Color col = CatDatabase.BurstColor(level);

        _cats.Remove(a);
        _cats.Remove(b);
        Destroy(a.gameObject);
        Destroy(b.gameObject);
        StartCoroutine(Ring(pos, CatDatabase.Radius[level], col));

        // juice: sound, sparks, haptic, shake (scaled by cat level / combo)
        AudioManager.Instance.Merge(level, _combo);
        Fx.Instance.Burst(pos, col, 6 + level * 2, 2.5f + level * 0.3f);
        Haptics.Pulse(level);
        if (level >= 7) Fx.Instance.Shake(0.04f + 0.012f * (level - 7), 0.22f);
        else if (_combo >= 4) Fx.Instance.Shake(0.03f, 0.15f);

        if (level >= CatDatabase.MaxLevel)
        {
            AddScore(Balance.Current.maxMergeScore);   // two biggest cats: vanish for a bonus
            AddCoins(CatDatabase.MergeCoins(CatDatabase.MaxLevel + 1));
            AudioManager.Instance.Fanfare();
            foreach (var c in CatDatabase.ConfettiColors) Fx.Instance.Burst(pos, c, 8, 6f, 0.2f);
            Fx.Instance.Shake(0.25f, 0.4f);
            return;
        }

        var n = Cat.Create(level + 1, pos, _mat, preview: false);
        _cats.Add(n);
        AddScore(CatDatabase.Score[level + 1]);
        AddCoins(CatDatabase.MergeCoins(level + 1));
        NotifyAppear(level + 1);
    }

    void AddScore(int amount)
    {
        Score += amount;
        if (Score > Best)
        {
            Best = Score;
            PlayerData.SetBest(Best);
        }
        _ui.Refresh();
    }

    IEnumerator Ring(Vector2 pos, float radius, Color color)
    {
        var go = new GameObject("Ring");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle;
        sr.sortingOrder = 3;
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.25f)
        {
            go.transform.localScale = Vector3.one * Mathf.Lerp(radius * 2f, radius * 3.2f, t);
            sr.color = new Color(color.r, color.g, color.b, 0.6f * (1f - t));
            yield return null;
        }
        Destroy(go);
    }

    // ------------------------------------------------------------------ game over

    void CheckDanger()
    {
        bool danger = false;
        foreach (var f in _cats)
        {
            if (f == null || !f.Dropped || f.Merging || f.Age < DangerGrace) continue;
            if (f.transform.position.y + f.Radius > DangerY && f.Speed < 0.6f) { danger = true; break; }
        }

        _dangerTimer = danger ? _dangerTimer + Time.deltaTime : 0f;

        // scared faces + warning tick while in danger
        foreach (var f in _cats) if (f != null) f.Worried = danger;
        _warnT -= Time.deltaTime;
        if (danger && _warnT <= 0f)
        {
            _warnT = 0.5f;
            AudioManager.Instance.Warn();
            Haptics.Light();
        }

        // blink the line while in danger
        float pulse = danger ? 0.45f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 8f)) : 0.45f;
        _dangerLine.color = new Color(0.9f, 0.2f, 0.2f, pulse);

        if (_dangerTimer >= GameOverDelay) GameOver();
    }

    void GameOver()
    {
        IsGameOver = true;
        _aimLine.enabled = false;
        if (_held != null) { Destroy(_held.gameObject); _held = null; }
        AudioManager.Instance.GameOver();
        Haptics.Heavy();
        Fx.Instance.Shake(0.15f, 0.5f);
        PlayerData.ClearRun();             // a finished game cannot be continued
        PlayerData.Flush();
        _ui.ShowGameOver(true);
    }
}
