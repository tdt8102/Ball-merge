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
    const float DangerY = 3.0f;       // fruits resting above this line for too long = game over
    const float SpawnY = 4.5f;        // dropper height
    const float WallThickness = 0.5f;
    const float ViewWidth = 7.4f;     // world width always visible on screen
    const float MinViewHeightHalf = 6.6f;

    const float DropCooldown = 0.45f;
    const float GameOverDelay = 2.0f;
    const float DangerGrace = 1.2f;   // a freshly dropped fruit may be above the line this long

    public int Score { get; private set; }
    public int Best { get; private set; }
    public int NextLevel { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool Paused { get; set; }               // settings panel open
    public int FruitCount => _fruits.Count;

    GameUI _ui;
    PhysicsMaterial2D _mat;
    readonly List<Fruit> _fruits = new List<Fruit>();
    Fruit _held;
    SpriteRenderer _aimLine, _dangerLine;
    float _aimX, _dangerTimer, _cooldown;
    bool _aiming;
    int _combo;
    float _lastMergeTime = -100f, _warnT;
    const float ComboWindow = 1.2f;   // max gap between merges to count as a combo

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;                // in case the previous scene was left while paused
#if !UNITY_WEBGL
        Application.targetFrameRate = 60;   // WebGL: keep the default (requestAnimationFrame); a fixed target makes it use setTimeout and stutter on phones
#endif
        Time.maximumDeltaTime = 0.06f;      // slow frames slow the game down instead of piling up physics steps
        Physics2D.gravity = new Vector2(0f, -20f);
        Best = PlayerPrefs.GetInt("best", 0);

        _mat = new PhysicsMaterial2D("Fruit") { friction = 0.35f, bounciness = 0.08f };

        SetupCamera();
        new GameObject("Audio").AddComponent<AudioManager>();
        new GameObject("Fx").AddComponent<Fx>();
        BuildPlayfield();
        _ui = new GameObject("UI").AddComponent<GameUI>();
        _ui.Build(this);
    }

    void Start() => NewGame();

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
        cam.backgroundColor = new Color(0.98f, 0.93f, 0.84f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        // Fit width first; make sure the full height is visible too.
        cam.orthographicSize = Mathf.Max(ViewWidth / cam.aspect * 0.5f, MinViewHeightHalf);
    }

    void BuildPlayfield()
    {
        var root = new GameObject("Playfield").transform;

        // background panel
        float h = WallTopY - FloorY;
        Quad(root, "Panel", new Vector2(0, FloorY + h * 0.5f), new Vector2(HalfWidth * 2, h),
             new Color(1f, 0.97f, 0.9f), -10);

        Color wallCol = new Color(0.55f, 0.36f, 0.24f);
        float wallH = 20f;

        // visible frame: thin tray with round wall tops and a softly rounded outer bottom corner.
        // The colliders below are thicker invisible boxes along the same inner edge, so physics is unchanged.
        const float t = 0.22f;
        float xc = HalfWidth + t * 0.5f, topC = WallTopY - t * 0.5f, botC = FloorY - t * 0.5f;
        foreach (float s in new[] { -1f, 1f })
        {
            Quad(root, "WallVisual", new Vector2(s * xc, (topC + botC) * 0.5f), new Vector2(t, topC - botC), wallCol, 5);
            Disc(root, new Vector2(s * xc, topC), t, wallCol, 5);     // round top
            Disc(root, new Vector2(s * xc, botC), t, wallCol, 5);     // round outer bottom corner
        }
        Quad(root, "FloorVisual", new Vector2(0, botC), new Vector2(xc * 2f, t), wallCol, 5);

        // left / right / floor (colliders only)
        Wall(root, "WallL", new Vector2(-HalfWidth - WallThickness * 0.5f, FloorY + wallH * 0.5f - WallThickness), new Vector2(WallThickness, wallH));
        Wall(root, "WallR", new Vector2(HalfWidth + WallThickness * 0.5f, FloorY + wallH * 0.5f - WallThickness), new Vector2(WallThickness, wallH));
        Wall(root, "Floor", new Vector2(0, FloorY - WallThickness * 0.5f), new Vector2(HalfWidth * 2 + WallThickness * 2, WallThickness));

        // danger line
        _dangerLine = Quad(root, "DangerLine", new Vector2(0, DangerY), new Vector2(HalfWidth * 2, 0.06f),
                           new Color(0.9f, 0.2f, 0.2f, 0.45f), 0);
        // aim guide
        _aimLine = Quad(root, "Aim", new Vector2(0, (SpawnY + FloorY) * 0.5f), new Vector2(0.04f, SpawnY - FloorY),
                        new Color(0.3f, 0.3f, 0.3f, 0.18f), 0);
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
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        box.sharedMaterial = _mat;
    }

    // ------------------------------------------------------------------ flow

    public void NewGame()
    {
        foreach (var f in _fruits) if (f != null) Destroy(f.gameObject);
        _fruits.Clear();
        if (_held != null) Destroy(_held.gameObject);
        _held = null;

        Score = 0;
        IsGameOver = false;
        _dangerTimer = 0f;
        _combo = 0;
        _lastMergeTime = -100f;
        _cooldown = 0.2f;
        _aiming = false;
        _aimX = 0f;
        NextLevel = FruitDatabase.RandomDropLevel();
        _ui.Refresh();
        _ui.ShowGameOver(false);
    }

    void SpawnHeld()
    {
        _held = Fruit.Create(NextLevel, new Vector2(_aimX, SpawnY), _mat, preview: true);
        _fruits.Add(_held);
        NextLevel = FruitDatabase.RandomDropLevel();
        _ui.Refresh();
    }

    void Update()
    {
        if (IsGameOver || Paused) return;

        if (_held == null)
        {
            _cooldown -= Time.deltaTime;
            if (_cooldown <= 0f) SpawnHeld();
        }

        HandleInput();
        CheckDanger();
    }

    void HandleInput()
    {
        // Track the press even while no fruit is held (cooldown). Otherwise a finger that is already
        // down when the next fruit appears is ignored until the player lifts and taps again.
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

    public void Merge(Fruit a, Fruit b)
    {
        if (IsGameOver) return;
        a.Merging = b.Merging = true;
        int level = a.Level;
        Vector2 pos = (a.transform.position + b.transform.position) * 0.5f;

        // combo: merges that follow each other within ComboWindow seconds
        _combo = Time.time - _lastMergeTime <= ComboWindow ? _combo + 1 : 1;
        _lastMergeTime = Time.time;
        if (_combo >= 2) _ui.ShowCombo(pos, _combo);
        Color col = FruitDatabase.BurstColor(level);

        _fruits.Remove(a);
        _fruits.Remove(b);
        Destroy(a.gameObject);
        Destroy(b.gameObject);
        StartCoroutine(Ring(pos, FruitDatabase.Radius[level], col));

        // juice: sound, sparks, haptic, shake (scaled by fruit level / combo)
        AudioManager.Instance.Merge(level, _combo);
        Fx.Instance.Burst(pos, col, 6 + level * 2, 2.5f + level * 0.3f);
        Haptics.Pulse(level);
        if (level >= 7) Fx.Instance.Shake(0.04f + 0.012f * (level - 7), 0.22f);
        else if (_combo >= 4) Fx.Instance.Shake(0.03f, 0.15f);

        if (level >= FruitDatabase.MaxLevel)
        {
            AddScore(100);          // two biggest fruits: vanish for a bonus
            AudioManager.Instance.Fanfare();
            foreach (var c in FruitDatabase.ConfettiColors) Fx.Instance.Burst(pos, c, 8, 6f, 0.2f);
            Fx.Instance.Shake(0.25f, 0.4f);
            return;
        }

        var n = Fruit.Create(level + 1, pos, _mat, preview: false);
        _fruits.Add(n);
        AddScore(FruitDatabase.Score[level + 1]);
    }

    void AddScore(int amount)
    {
        Score += amount;
        if (Score > Best)
        {
            Best = Score;
            PlayerPrefs.SetInt("best", Best);
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
        foreach (var f in _fruits)
        {
            if (f == null || !f.Dropped || f.Merging || f.Age < DangerGrace) continue;
            if (f.transform.position.y + f.Radius > DangerY && f.Speed < 0.6f) { danger = true; break; }
        }

        _dangerTimer = danger ? _dangerTimer + Time.deltaTime : 0f;

        // scared faces + warning tick while in danger
        foreach (var f in _fruits) if (f != null) f.Worried = danger;
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
        _ui.ShowGameOver(true);
    }
}
