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

        BuildSkill(root);       // before the game-over panel, so that panel covers (and blocks) the button

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

    // ---------------------------------------------------------------- theme skill HUD

    // The button (ring = energy, or the time left of a timed effect; a pill under it says what is going on), the line shown while a
    // target is being chosen (hint + CANCEL) and the placement that keeps all of it off NEXT and off the drop area (HudLayout).
    RectTransform _skillRoot, _skillBody, _banner;
    Image _skillBg, _skillRing, _skillIcon, _badgeBg;
    Button _skillBtn;
    Text _badge, _bannerText;
    float _badgeMsgT = -1f, _rejectT = -1f, _shakeT = -1f;
    string _badgeMsg = "";
    int _skillIconTheme = -1;
    Vector2 _layoutKey;
    HudLayout.Layout _layout;

    public RectTransform SkillRect => _skillRoot;
    public RectTransform BannerRect => _banner;
    public HudLayout.Layout CurrentLayout => _layout;

    static readonly Color BadgeColor = new Color(0.3f, 0.19f, 0.12f, 0.92f);

    void BuildSkill(RectTransform root)
    {
        var c = new Vector2(0.5f, 0.5f);
        var topLeft = new Vector2(0, 1);
        var go = new GameObject("SkillButton", typeof(RectTransform));
        _skillRoot = (RectTransform)go.transform;
        _skillRoot.SetParent(root, false);
        _skillRoot.anchorMin = _skillRoot.anchorMax = topLeft;
        _skillRoot.pivot = topLeft;
        _skillRoot.sizeDelta = new Vector2(HudLayout.BadgeWidth, HudLayout.GroupHeight);

        var body = new GameObject("Body", typeof(RectTransform));        // the part that shakes
        _skillBody = (RectTransform)body.transform;
        _skillBody.SetParent(_skillRoot, false);
        _skillBody.anchorMin = _skillBody.anchorMax = topLeft;
        _skillBody.pivot = topLeft;
        _skillBody.sizeDelta = _skillRoot.sizeDelta;
        _skillBody.anchoredPosition = Vector2.zero;

        var ringCenter = new Vector2(HudLayout.BadgeWidth * 0.5f, -HudLayout.RingSize * 0.5f);
        _skillBg = Img(_skillBody, "Bg", SpriteFactory.Circle, new Color(0.45f, 0.3f, 0.2f), topLeft, ringCenter, new Vector2(HudLayout.RingSize * 0.77f, HudLayout.RingSize * 0.77f));
        _skillBg.raycastTarget = true;
        _skillBtn = _skillBg.gameObject.AddComponent<Button>();
        _skillBtn.transition = Selectable.Transition.None;
        _skillBtn.onClick.AddListener(() => _gm.PressSkill());
        _skillIcon = Img(_skillBody, "Icon", null, Color.white, topLeft, ringCenter, new Vector2(HudLayout.RingSize * 0.54f, HudLayout.RingSize * 0.54f));
        _skillRing = Img(_skillBody, "Ring", SpriteFactory.Ring, Color.white, topLeft, ringCenter, new Vector2(HudLayout.RingSize, HudLayout.RingSize));
        _skillRing.type = Image.Type.Filled;
        _skillRing.fillMethod = Image.FillMethod.Radial360;
        _skillRing.fillOrigin = (int)Image.Origin360.Top;
        _skillRing.fillClockwise = true;

        float badgeY = -(HudLayout.RingSize + HudLayout.BadgeGap + HudLayout.BadgeHeight * 0.5f);
        _badgeBg = RoundImg(_skillBody, "Badge", BadgeColor, topLeft, new Vector2(HudLayout.BadgeWidth * 0.5f, badgeY), new Vector2(HudLayout.BadgeWidth, HudLayout.BadgeHeight), HudLayout.BadgeHeight * 0.5f);
        _badge = Label((RectTransform)_badgeBg.transform, "Text", "", 22, TextAnchor.MiddleCenter, c, Vector2.zero, new Vector2(HudLayout.BadgeWidth, HudLayout.BadgeHeight));
        _badge.color = Color.white;

        // target mode: one line above the playfield with the hint and a CANCEL button (the dark board overlay is GameManager's)
        var bannerImg = RoundImg(root, "TargetBanner", new Color(0.25f, 0.15f, 0.1f, 0.9f), topLeft, Vector2.zero, new Vector2(800, HudLayout.BannerHeight), 30f);
        _banner = bannerImg.rectTransform;
        _banner.pivot = c;
        _bannerText = Label(_banner, "Hint", "", 30, TextAnchor.MiddleCenter, c, Vector2.zero, new Vector2(560, HudLayout.BannerHeight));
        _bannerText.color = Color.white;
        var cancel = Button(_banner, "Cancel", "", 28, c, Vector2.zero, new Vector2(170, 46), new Color(0.85f, 0.35f, 0.3f));
        Loc.Bind(cancel, () => Loc.T("skill_cancel"));
        cancel.onClick.AddListener(() => _gm.PressSkill());             // pressing the skill while choosing a target is the free cancel
        _banner.gameObject.SetActive(false);

        _skillRoot.gameObject.SetActive(false);
    }

    /// <summary>Put the skill button and the target line where HudLayout says, for a canvas of this size.</summary>
    public HudLayout.Layout ApplyLayout(float canvasW, float canvasH)
    {
        float maxDrop = 0f;
        foreach (int level in Balance.Current.dropBag) maxDrop = Mathf.Max(maxDrop, CatDatabase.Radius[level]);
        _layout = HudLayout.Compute(canvasW, canvasH, GameManager.CameraHalfHeight(canvasW / canvasH), maxDrop);

        _skillRoot.anchoredPosition = new Vector2(_layout.Group.L, -_layout.Group.T);
        var b = _layout.Banner;
        _banner.anchoredPosition = new Vector2((b.L + b.R) * 0.5f, -(b.T + b.B) * 0.5f);
        _banner.sizeDelta = new Vector2(b.Width, b.Height);
        float cancelW = 170f, pad = 18f;
        var hint = _bannerText.rectTransform;
        hint.sizeDelta = new Vector2(b.Width - cancelW - pad * 3f, b.Height);
        hint.anchoredPosition = new Vector2(-(cancelW + pad) * 0.5f, 0f);
        _banner.Find("Cancel").GetComponent<RectTransform>().anchoredPosition = new Vector2(b.Width * 0.5f - pad - cancelW * 0.5f, 0f);
        return _layout;
    }

    /// <summary>A short note in the pill under the button (e.g. NEED 100%), with a small shake.</summary>
    public void ShowSkillMessage(string text)
    {
        _badgeMsg = text;
        _badgeMsgT = 0f;
        _shakeT = 0f;
    }

    /// <summary>The tapped cat cannot be chosen: say so on the target line for a moment (the target mode stays on).</summary>
    public void ShowTargetRejected() => _rejectT = 0f;

    /// <summary>Every frame: show the button only when the run has a skill; ring = energy (gold and pulsing when ready) or time left.</summary>
    void UpdateSkill()
    {
        var skill = _gm.ActiveSkill;
        var rt = _gm.Skills;
        bool show = skill != null;
        if (_skillRoot.gameObject.activeSelf != show) _skillRoot.gameObject.SetActive(show);
        if (!show) { _banner.gameObject.SetActive(false); return; }

        var canvasSize = ((RectTransform)transform).rect.size;
        if (canvasSize.x > 1f && canvasSize.y > 1f && canvasSize != _layoutKey) { _layoutKey = canvasSize; ApplyLayout(canvasSize.x, canvasSize.y); }

        var theme = skill.Theme;
        if (theme != null && theme.Index != _skillIconTheme)
        {
            _skillIconTheme = theme.Index;
            _skillIcon.sprite = CosmeticUI.SkillIcon(skill);
        }

        bool dead = _gm.IsGameOver;
        float dt = Time.unscaledDeltaTime;
        float fill;
        Color ring;
        string badge;
        Color badgeColor = Color.white;
        if (rt.IsTimedActive)
        {
            fill = rt.TimedRemaining / Mathf.Max(0.01f, rt.TimedDuration);
            ring = new Color(0.45f, 0.75f, 1f);
            badge = Mathf.CeilToInt(rt.TimedRemaining) + "s";
            badgeColor = new Color(0.65f, 0.85f, 1f);
        }
        else
        {
            fill = _gm.Energy.Value / (float)Mathf.Max(1, _gm.Energy.Max);
            ring = rt.Ready ? new Color(1f, 0.85f, 0.3f, 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)))
                            : new Color(1f, 1f, 1f, 0.45f);
            badge = rt.Ready ? Loc.T("skill_ready") : _gm.Energy.Value + "%";
            if (rt.Ready) badgeColor = new Color(1f, 0.85f, 0.3f);
        }
        if (rt.IsTargeting && !dead) { badge = Loc.T("skill_cancel"); badgeColor = new Color(1f, 0.6f, 0.4f); }
        if (_badgeMsgT >= 0f)
        {
            _badgeMsgT += dt;
            if (_badgeMsgT >= 1.4f) _badgeMsgT = -1f;
            else { badge = _badgeMsg; badgeColor = new Color(1f, 0.5f, 0.4f); }
        }
        _skillRing.fillAmount = fill;
        _skillRing.color = ring;
        _badge.text = badge;
        _badge.color = badgeColor;
        _skillBg.color = rt.IsTargeting ? new Color(0.95f, 0.6f, 0.2f) : new Color(0.45f, 0.3f, 0.2f);
        _skillIcon.color = new Color(1f, 1f, 1f, dead ? 0.35f : (rt.Ready || rt.IsTimedActive || rt.IsTargeting ? 1f : 0.55f));
        _skillBtn.interactable = !dead;

        if (_shakeT >= 0f)
        {
            _shakeT += dt;
            float k = Mathf.Clamp01(_shakeT / 0.35f);
            _skillBody.anchoredPosition = new Vector2(Mathf.Sin(_shakeT * 70f) * 9f * (1f - k), 0f);
            if (_shakeT >= 0.35f) { _shakeT = -1f; _skillBody.anchoredPosition = Vector2.zero; }
        }

        bool targeting = rt.IsTargeting && !dead;
        if (_banner.gameObject.activeSelf != targeting) _banner.gameObject.SetActive(targeting);
        if (targeting)
        {
            bool rejected = _rejectT >= 0f;
            if (rejected) { _rejectT += dt; if (_rejectT >= 1.2f) { _rejectT = -1f; rejected = false; } }
            _bannerText.text = rejected ? Loc.T("skill_bad_target") : Loc.T("skill_tap_cat");
            _bannerText.color = rejected ? new Color(1f, 0.7f, 0.45f) : Color.white;
        }
        else _rejectT = -1f;
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

        UpdateSkill();
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
    public static Text CoinChip(RectTransform parent, Vector2 anchor, Vector2 pos, out RectTransform chip) =>
        Chip(parent, "CoinChip", CatSprites.CoinIcon(), PlayerData.Coins.ToString(), anchor, pos, out chip);

    /// <summary>The same pill for star shards (blue star icon).</summary>
    public static Text ShardChip(RectTransform parent, Vector2 anchor, Vector2 pos, out RectTransform chip) =>
        Chip(parent, "ShardChip", CatSprites.ShardIcon(), PlayerData.Shards.ToString(), anchor, pos, out chip);

    static Text Chip(RectTransform parent, string name, Sprite icon, string amount, Vector2 anchor, Vector2 pos, out RectTransform chip)
    {
        var border = RoundImg(parent, name, new Color(0.55f, 0.36f, 0.24f), anchor, pos, new Vector2(250, 72), 36f);
        chip = border.rectTransform;
        var cr = (RectTransform)border.transform;
        RoundImg(cr, "Fill", new Color(1f, 0.97f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(242, 64), 32f);
        Img(cr, "Icon", icon, Color.white, new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(54, 54));
        var t = Label(cr, "Amount", amount, 40, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(76, 0), new Vector2(165, 60));
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
