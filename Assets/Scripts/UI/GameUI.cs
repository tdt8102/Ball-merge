using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds the whole HUD in code (1080x1920 reference, portrait).</summary>
public class GameUI : MonoBehaviour
{
    GameManager _gm;
    Text _score, _best, _finalScore;
    Image _nextIcon;
    GameObject _gameOver;
    SettingsPopup _popup;
    Text _coinText, _finalCoins;
    RectTransform _coinChip;
    float _coinPop;
    int _lastCoins = -1;
    // "new cat" banner
    RectTransform _toast;
    CanvasGroup _toastGroup;
    Image _toastIcon;
    Text _toastName, _toastReward;
    readonly Queue<Vector2Int> _toastQueue = new Queue<Vector2Int>();
    float _toastT = -1f;
    float _pop;
    int _lastScore;
    Text _debug;
    float _fps = 60f, _worst, _dbgT;
    string _lastError = "-";

    public void Build(GameManager gm)
    {
        _gm = gm;

        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        var root = (RectTransform)transform;

        // --- score (top-left) ---
        _score = Label(root, "Score", "0", 90, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(40, -40), new Vector2(520, 110));
        _best = Label(root, "Best", "BEST 0", 40, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(44, -150), new Vector2(520, 50));
        var ink = Cosmetics.Bg.Ink;                       // text colour that reads on the chosen background
        _score.color = ink;
        ink.a = 0.8f;
        _best.color = ink;
        _coinText = CoinChip(root, new Vector2(0, 1), new Vector2(169, -245), out _coinChip);

        // --- next cat (top-right) ---
        var nextLbl = Label(root, "NextLabel", "NEXT", 36, TextAnchor.UpperCenter, new Vector2(1, 1), new Vector2(-190, -30), new Vector2(200, 44));
        Loc.Bind(nextLbl, () => Loc.T("next"));
        nextLbl.color = ink;
        _nextIcon = Img(root, "NextIcon", CatSprites.Icon(0), Color.white, new Vector2(1, 1), new Vector2(-190, -140), new Vector2(100, 100));

        // --- evolution chain (bottom) ---
        const float slot = 90f;
        float startX = -slot * (CatDatabase.MaxLevel) * 0.5f;
        for (int i = 0; i <= CatDatabase.MaxLevel; i++)
        {
            float size = (30f + i * 4f) * 1.2f;     // sprite is 1.2x the head diameter (ears/accessories)
            Img(root, "Chain" + i, CatSprites.Icon(i), Color.white, new Vector2(0.5f, 0), new Vector2(startX + i * slot, 70f), new Vector2(size, size));
        }

        // --- game over panel ---
        _gameOver = new GameObject("GameOver", typeof(RectTransform), typeof(Image));
        var go = (RectTransform)_gameOver.transform;
        go.SetParent(root, false);
        go.anchorMin = Vector2.zero; go.anchorMax = Vector2.one; go.offsetMin = go.offsetMax = Vector2.zero;
        _gameOver.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        var goTitle = Label(go, "Title", "GAME OVER", 120, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(900, 160));
        Loc.Bind(goTitle, () => Loc.T("game_over"));
        _finalCoins = Label(go, "Coins", "", 50, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -35), new Vector2(900, 70));
        _finalCoins.color = new Color(1f, 0.85f, 0.3f);
        _finalScore = Label(go, "Final", "0", 100, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(900, 140));
        var again = Button(go, "Again", "PLAY AGAIN", 60, new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(520, 130), new Color(0.2f, 0.65f, 0.4f));
        again.onClick.AddListener(() => { AudioManager.Instance.Click(); _gm.NewGame(); });
        Loc.Bind(again, () => Loc.T("play_again"));
        var home = Button(go, "Home", "MENU", 50, new Vector2(0.5f, 0.5f), new Vector2(0, -320), new Vector2(520, 110), new Color(0.55f, 0.36f, 0.24f));
        home.onClick.AddListener(GoToMenu);
        Loc.Bind(home, () => Loc.T("menu"));
        _gameOver.SetActive(false);

        Loc.Changed += OnLanguageChanged;
        CheatPanel.CoinsChanged += RefreshCoins;
        BuildToast(root);
        BuildSettings(root);   // created last so the gear button and its panel draw on top of the HUD

        // Open the page with ?debug (or #debug) to show FPS / cat count / last error on screen.
        if (Application.absoluteURL != null && Application.absoluteURL.Contains("debug"))
        {
            _debug = Label(root, "Debug", "", 28, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(40, -330), new Vector2(900, 200));
            _debug.color = new Color(0.8f, 0.1f, 0.1f);
            Application.logMessageReceived += (msg, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    _lastError = msg.Length > 120 ? msg.Substring(0, 120) : msg;
            };
        }
    }

    // ---------------------------------------------------------------- settings (gear button + popup)

    /// <summary>One small gear button (top centre, clear of the cats) opens the shared settings popup; it pauses the game.</summary>
    void BuildSettings(RectTransform root)
    {
        var gear = Button(root, "SettingsButton", "", 30, new Vector2(0.5f, 1), new Vector2(0, -85), new Vector2(120, 120), new Color(0.55f, 0.36f, 0.24f));
        Img((RectTransform)gear.transform, "Gear", CatSprites.Gear(), Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84, 84));
        gear.onClick.AddListener(() => { AudioManager.Instance.Click(); _popup.Open(); });

        _popup = SettingsPopup.Create(root, true,
            open => { _gm.Paused = open; Time.timeScale = open ? 0f : 1f; },
            onRestart: () => _gm.NewGame(),
            onMenu: GoToMenu);
    }

    void OnLanguageChanged() => Refresh();

    void OnDestroy()
    {
        Loc.Changed -= OnLanguageChanged;
        CheatPanel.CoinsChanged -= RefreshCoins;
    }

    static void GoToMenu()
    {
        Time.timeScale = 1f;               // the settings popup pauses the game
        if (GameManager.Instance != null) GameManager.Instance.SaveProgress();   // the game can be continued from Home
        AudioManager.Instance.Click();
        SceneManager.LoadScene(MenuController.MenuScene);
    }

    void Update()
    {
        // Esc (also the Android back button) toggles the settings panel
        if (Input.GetKeyDown(KeyCode.Escape) && _popup != null && !_gameOver.activeSelf) _popup.Toggle();

        if (_debug != null)
        {
            float dt = Time.unscaledDeltaTime;
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(dt, 0.0001f), 0.05f);
            _worst = Mathf.Max(_worst, dt);
            _dbgT += dt;
            if (_dbgT >= 0.5f)
            {
                _dbgT = 0f;
                _debug.text = $"fps {_fps:F0}  worst {_worst * 1000f:F0}ms  cats {_gm.CatCount}\n" +
                              $"mem {System.GC.GetTotalMemory(false) / 1048576f:F1}MB  err: {_lastError}";
                if (_fps > 30f) _worst = 0f;     // keep the worst frame visible while the game is slow
            }
        }

        UpdateToast();
        if (_coinPop > 0f)
        {
            _coinPop = Mathf.Max(0f, _coinPop - Time.unscaledDeltaTime * 4f);
            _coinChip.localScale = Vector3.one * (1f + 0.15f * _coinPop);
        }

        // score "bump" when it increases
        if (_pop > 0f)
        {
            _pop = Mathf.Max(0f, _pop - Time.deltaTime * 4f);
            _score.rectTransform.localScale = Vector3.one * (1f + 0.3f * _pop);
        }
    }

    public void RefreshCoins()
    {
        if (_coinText == null) return;
        int c = PlayerData.Coins;
        _coinText.text = c.ToString();
        if (_lastCoins >= 0 && c > _lastCoins) _coinPop = 1f;
        _lastCoins = c;
    }

    public void Refresh()
    {
        RefreshCoins();
        if (_gm == null || _score == null) return;
        if (_gm.Score > _lastScore) _pop = 1f;
        _lastScore = _gm.Score;
        _score.text = _gm.Score.ToString();
        _best.text = Loc.T("best") + " " + _gm.Best;
        _nextIcon.sprite = CatSprites.Icon(_gm.NextLevel);
        float d = (40f + CatDatabase.Radius[_gm.NextLevel] * 110f) * 1.2f;
        _nextIcon.rectTransform.sizeDelta = new Vector2(d, d);
    }

    public void ShowGameOver(bool show)
    {
        _gameOver.SetActive(show);
        if (show)
        {
            _finalScore.text = Loc.T("score") + "  " + _gm.Score;
            _finalCoins.text = Loc.T("coins") + "  +" + _gm.RunCoins;
        }
    }

    // ---------------------------------------------------------------- coin chip and "new cat" banner

    /// <summary>Small pill with a coin icon and the coin total. Returns the amount text; chip is the pill (for bump animation).</summary>
    public static Text CoinChip(RectTransform parent, Vector2 anchor, Vector2 pos, out RectTransform chip)
    {
        var border = RoundImg(parent, "CoinChip", new Color(0.55f, 0.36f, 0.24f), anchor, pos, new Vector2(250, 72), 36f);
        chip = border.rectTransform;
        var cr = (RectTransform)border.transform;
        RoundImg(cr, "Fill", new Color(1f, 0.97f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(242, 64), 32f);
        Img(cr, "Coin", CatSprites.CoinIcon(), Color.white, new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(54, 54));
        var t = Label(cr, "Amount", PlayerData.Coins.ToString(), 40, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(76, 0), new Vector2(165, 60));
        t.color = new Color(0.36f, 0.22f, 0.15f);
        return t;
    }

    void BuildToast(RectTransform root)
    {
        var gold = new Color(1f, 0.85f, 0.3f);
        var card = RoundImg(root, "DiscoveryToast", new Color(0.36f, 0.22f, 0.15f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0, -690), new Vector2(780, 150), 46f);
        _toast = card.rectTransform;
        _toastGroup = card.gameObject.AddComponent<CanvasGroup>();
        _toastGroup.blocksRaycasts = false;
        _toastGroup.interactable = false;
        var cr = (RectTransform)card.transform;
        _toastIcon = Img(cr, "Cat", CatSprites.Icon(1), Color.white, new Vector2(0, 0.5f), new Vector2(95, 0), new Vector2(130, 130));
        var toastTitle = Label(cr, "Title", "NEW CAT!", 34, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(185, 34), new Vector2(300, 44));
        toastTitle.color = gold;
        Loc.Bind(toastTitle, () => Loc.T("new_cat"));
        _toastName = Label(cr, "Name", "", 48, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(185, -16), new Vector2(360, 60));
        _toastName.color = Color.white;
        Img(cr, "Coin", CatSprites.CoinIcon(), Color.white, new Vector2(1, 0.5f), new Vector2(-165, 0), new Vector2(54, 54));
        _toastReward = Label(cr, "Reward", "+0", 46, TextAnchor.MiddleLeft, new Vector2(1, 0.5f), new Vector2(-132, 0), new Vector2(120, 60));
        _toastReward.color = gold;
        card.gameObject.SetActive(false);
    }

    /// <summary>Queue a "new cat discovered" banner (shown one after another).</summary>
    public void ShowDiscovery(int level, int reward) => _toastQueue.Enqueue(new Vector2Int(level, reward));

    void UpdateToast()
    {
        if (_toast == null) return;
        if (_toastT < 0f && _toastQueue.Count > 0)
        {
            var q = _toastQueue.Dequeue();
            _toastIcon.sprite = CatSprites.Icon(q.x);
            _toastName.text = Cosmetics.WornName(q.x);
            _toastReward.text = "+" + q.y;
            _toast.gameObject.SetActive(true);
            _toastT = 0f;
        }
        if (_toastT < 0f) return;

        const float dur = 2.4f;
        _toastT += Time.unscaledDeltaTime;
        _toastGroup.alpha = Mathf.Clamp01(_toastT / 0.2f) * Mathf.Clamp01((dur - _toastT) / 0.4f);
        _toast.localScale = Vector3.one * (0.85f + 0.15f * Mathf.Clamp01(_toastT / 0.25f));
        if (_toastT >= dur)
        {
            _toastT = -1f;
            _toast.gameObject.SetActive(false);
        }
    }

    // ---------------------------------------------------------------- combo popup

    static readonly Color[] ComboColors =
    {
        new Color(1f, 0.85f, 0.3f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.4f, 0.4f),
        new Color(0.9f, 0.4f, 0.9f), new Color(0.4f, 0.8f, 1f)
    };

    /// <summary>Small floating encouragement text at a world position. combo >= 2.</summary>
    public void ShowCombo(Vector2 worldPos, int combo)
    {
        int tier = Mathf.Clamp(combo - 2, 0, 4);
        var root = (RectTransform)transform;
        Vector2 screen = Camera.main.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var local);

        var t = Label(root, "Combo", Loc.T("combo" + tier) + " x" + combo, 52 + tier * 6, TextAnchor.MiddleCenter,
                      new Vector2(0.5f, 0.5f), local, new Vector2(600, 90));
        t.color = ComboColors[tier];
        var outline = t.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.15f, 0.1f, 0.9f);
        outline.effectDistance = new Vector2(3, -3);
        StartCoroutine(FloatAndFade(t, local));
    }

    System.Collections.IEnumerator FloatAndFade(Text t, Vector2 from)
    {
        var rt = t.rectTransform;
        const float dur = 0.9f;
        for (float k = 0; k < 1f; k += Time.deltaTime / dur)
        {
            if (t == null) yield break;
            rt.anchoredPosition = from + new Vector2(0, 110f * k);
            rt.localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(Mathf.Min(k * 4f, 1f) * Mathf.PI * 0.5f));
            var c = t.color; c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f; t.color = c;
            yield return null;
        }
        if (t != null) Destroy(t.gameObject);
    }

    // ---------------------------------------------------------------- helpers

    public static Text Label(RectTransform parent, string name, string text, int size, TextAnchor align,
                      Vector2 anchor, Vector2 pos, Vector2 dim)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        bool left = align == TextAnchor.UpperLeft || align == TextAnchor.MiddleLeft || align == TextAnchor.LowerLeft;
        bool upper = align == TextAnchor.UpperLeft || align == TextAnchor.UpperCenter || align == TextAnchor.UpperRight;
        bool lower = align == TextAnchor.LowerLeft || align == TextAnchor.LowerCenter || align == TextAnchor.LowerRight;
        rt.pivot = new Vector2(left ? 0f : 0.5f, upper ? 1f : (lower ? 0f : 0.5f));
        rt.anchoredPosition = pos;
        rt.sizeDelta = dim;
        var t = go.GetComponent<Text>();
        t.font = SpriteFactory.Font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = align;
        t.color = new Color(0.3f, 0.2f, 0.15f);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;     // never let a taller font clip the whole line
        t.raycastTarget = false;
        return t;
    }

    public static Image Img(RectTransform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 dim)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = dim;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>Rounded rectangle (9-slice) with the given corner radius in canvas units.</summary>
    public static Image RoundImg(RectTransform parent, string name, Color color, Vector2 anchor, Vector2 pos, Vector2 dim, float radius)
    {
        var img = Img(parent, name, SpriteFactory.RoundedRect, color, anchor, pos, dim);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 48f / Mathf.Max(1f, radius);    // the sprite's corner is 48 px
        return img;
    }

    public static Button Button(RectTransform parent, string name, string text, int fontSize, Vector2 anchor,
                         Vector2 pos, Vector2 dim, Color color)
    {
        var img = RoundImg(parent, name, color, anchor, pos, dim, Mathf.Min(dim.y * 0.35f, 44f));
        img.raycastTarget = true;
        var btn = img.gameObject.AddComponent<Button>();
        var lbl = Label((RectTransform)img.transform, "Text", text, fontSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, dim);
        lbl.color = Color.white;
        return btn;
    }
}
