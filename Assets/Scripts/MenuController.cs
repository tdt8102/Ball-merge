using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Home screen. A bottom tab bar switches between Gacha / Home / Skin / Shop (only Home exists so far, the others
/// say "Coming soon"). PLAY shows a loading screen while the "Main" scene loads. A gear icon on Home opens the
/// settings popup. Put it on an empty GameObject in the first scene; it builds its own UI.
/// </summary>
public class MenuController : MonoBehaviour
{
    public const string GameScene = "Main";
    public const string MenuScene = "Menu";

    const float MinLoadTime = 1.6f;     // keep the loading screen visible long enough to read (the game loads instantly)
    const float BarWidth = 640f;
    const int HomeTab = 1;
    const float TabBarHeight = 190f;

    static readonly string[] TabNames = { "GACHA", "HOME", "SKIN", "SHOP" };
    static readonly Color Brown = new Color(0.55f, 0.36f, 0.24f);
    static readonly Color Ink = new Color(0.36f, 0.22f, 0.15f);
    static readonly Color Soft = new Color(0.5f, 0.36f, 0.28f);
    static readonly Color Orange = new Color(0.95f, 0.6f, 0.2f);
    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    RectTransform _root;
    GameObject _loadingPanel;
    readonly GameObject[] _pages = new GameObject[4];
    RectTransform _barFill;
    Text _percent, _loadingText;
    RectTransform[] _bobbers, _soonCats;
    RectTransform _loadingCat;
    SettingsPopup _popup;
    bool _loading;

    // tab bar
    RectTransform _pill;
    readonly Image[] _tabIcons = new Image[4];
    readonly Text[] _tabLabels = new Text[4];
    int _tab = HomeTab;
    float _pillX = (HomeTab + 0.5f) / 4f;

    void Awake()
    {
        SetupCamera();
        if (AudioManager.Instance == null) new GameObject("Audio").AddComponent<AudioManager>();
        BuildUI();
    }

    static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.98f, 0.93f, 0.84f);
        cam.orthographic = true;
    }

    void BuildUI()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var canvasGo = new GameObject("MenuCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        _root = (RectTransform)canvasGo.transform;

        // pages first, then the tab bar on top of them, then popup, then the loading screen over everything
        BuildHomePage();
        _soonCats = new RectTransform[4];
        for (int i = 0; i < 4; i++) if (i != HomeTab) BuildSoonPage(i);
        BuildTabBar();
        _popup = SettingsPopup.Create(_root, false);
        BuildLoadingPanel();
        _loadingPanel.SetActive(false);
        SelectTab(HomeTab, silent: true);
    }

    // ------------------------------------------------------------------ home page

    void BuildHomePage()
    {
        _pages[HomeTab] = Panel("HomePage", Color.clear);
        var p = (RectTransform)_pages[HomeTab].transform;

        var title = GameUI.Label(p, "Title", "CatCatCat", 150, TextAnchor.MiddleCenter, Center, new Vector2(0, 560), new Vector2(1000, 200));
        title.color = Ink;
        Outline(title, Color.white, 4);
        GameUI.Label(p, "Sub", "Drop and merge the cats!", 46, TextAnchor.MiddleCenter, Center, new Vector2(0, 430), new Vector2(1000, 70)).color = Soft;

        // a row of cats that bob up and down (levels sampled across the range)
        int[] levels = { 0, 3, 10, 4, 8 };
        float[] sizes = { 190, 230, 300, 230, 190 };
        float[] xs = { -400, -200, 0, 200, 400 };
        _bobbers = new RectTransform[levels.Length];
        for (int i = 0; i < levels.Length; i++)
        {
            var img = GameUI.Img(p, "Cat" + i, CatSprites.Icon(levels[i]), Color.white, Center, new Vector2(xs[i], 110), new Vector2(sizes[i], sizes[i]));
            _bobbers[i] = img.rectTransform;
        }

        var play = GameUI.Button(p, "Play", "PLAY", 90, Center, new Vector2(0, -270), new Vector2(560, 170), new Color(0.2f, 0.65f, 0.4f));
        play.onClick.AddListener(OnPlay);

        int best = PlayerPrefs.GetInt("best", 0);
        GameUI.Label(p, "Best", "BEST  " + best, 50, TextAnchor.MiddleCenter, Center, new Vector2(0, -460), new Vector2(800, 80)).color = Soft;

        // the only settings entry: a gear icon in the top-right corner
        var gear = GameUI.Button(p, "SettingsButton", "", 30, new Vector2(1, 1), new Vector2(-95, -95), new Vector2(110, 110), Brown);
        GameUI.Img((RectTransform)gear.transform, "Gear", CatSprites.Gear(), Color.white, Center, Vector2.zero, new Vector2(78, 78));
        gear.onClick.AddListener(() => { AudioManager.Instance.Click(); _popup.Open(); });
    }

    // ------------------------------------------------------------------ "coming soon" pages (gacha, skin, shop)

    void BuildSoonPage(int tab)
    {
        var go = Panel("Page" + TabNames[tab], Color.clear);
        _pages[tab] = go;
        var p = (RectTransform)go.transform;

        var title = GameUI.Label(p, "Title", TabNames[tab], 110, TextAnchor.MiddleCenter, Center, new Vector2(0, 560), new Vector2(1000, 150));
        title.color = Ink;
        Outline(title, Color.white, 4);
        GameUI.Img(p, "Icon", CatSprites.TabIcon(tab), new Color(0.85f, 0.75f, 0.6f), Center, new Vector2(0, 260), new Vector2(260, 260));
        var soon = GameUI.Label(p, "Soon", "COMING SOON", 96, TextAnchor.MiddleCenter, Center, new Vector2(0, -20), new Vector2(1000, 130));
        soon.color = Orange;
        Outline(soon, Color.white, 4);
        GameUI.Label(p, "Hint", "We are working on it. Stay tuned!", 42, TextAnchor.MiddleCenter, Center, new Vector2(0, -130), new Vector2(1000, 70)).color = Soft;
        _soonCats[tab] = GameUI.Img(p, "Cat", CatSprites.Icon(tab == 0 ? 3 : (tab == 2 ? 8 : 9)), Color.white, Center, new Vector2(0, -360), new Vector2(240, 240)).rectTransform;

        go.SetActive(false);
    }

    // ------------------------------------------------------------------ bottom tab bar

    void BuildTabBar()
    {
        var bottom = new Vector2(0.5f, 0);
        // thin brown border behind a cream bar; both are taller than the visible part so only the top corners show
        var border = GameUI.RoundImg(_root, "TabBarBorder", Brown, bottom, new Vector2(0, TabBarHeight * 0.5f - 40f + 3f), new Vector2(0, TabBarHeight + 80f + 6f), 48f);
        var bar = GameUI.RoundImg(_root, "TabBar", new Color(1f, 0.97f, 0.9f), bottom, new Vector2(0, TabBarHeight * 0.5f - 40f), new Vector2(0, TabBarHeight + 80f), 46f);
        StretchX(border.rectTransform, 0f, 1f);
        StretchX(bar.rectTransform, 0f, 1f);
        border.raycastTarget = bar.raycastTarget = true;     // taps on the bar must not reach anything behind

        // sliding highlight behind the selected tab
        var pill = GameUI.RoundImg(_root, "TabPill", Orange, new Vector2(_pillX, 0), new Vector2(0, 98), new Vector2(210, 146), 40f);
        _pill = pill.rectTransform;

        for (int i = 0; i < 4; i++)
        {
            float cx = (i + 0.5f) / 4f;
            _tabIcons[i] = GameUI.Img(_root, "TabIcon" + i, CatSprites.TabIcon(i), Brown, new Vector2(cx, 0), new Vector2(0, 122), new Vector2(76, 76));
            _tabLabels[i] = GameUI.Label(_root, "TabLabel" + i, TabNames[i], 30, TextAnchor.MiddleCenter, new Vector2(cx, 0), new Vector2(0, 56), new Vector2(240, 44));
            _tabLabels[i].fontStyle = FontStyle.Bold;

            // transparent hit area covering the whole cell
            var hit = GameUI.Img(_root, "TabHit" + i, SpriteFactory.Square, new Color(0, 0, 0, 0), new Vector2(0, 0), new Vector2(0, TabBarHeight * 0.5f), new Vector2(0, TabBarHeight));
            StretchX(hit.rectTransform, i / 4f, (i + 1) / 4f);
            hit.raycastTarget = true;
            var btn = hit.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            int index = i;
            btn.onClick.AddListener(() => SelectTab(index));
        }
    }

    /// <summary>Stretch a bottom-anchored rect horizontally between two normalized x positions (height/y stay as set).</summary>
    static void StretchX(RectTransform rt, float xMin, float xMax)
    {
        var pos = rt.anchoredPosition;
        var size = rt.sizeDelta;
        rt.anchorMin = new Vector2(xMin, 0f);
        rt.anchorMax = new Vector2(xMax, 0f);
        rt.anchoredPosition = new Vector2(0f, pos.y);
        rt.sizeDelta = new Vector2(0f, size.y);
    }

    void SelectTab(int tab, bool silent = false)
    {
        if (_loading) return;
        if (tab != _tab && !silent) AudioManager.Instance.Click();
        _tab = tab;
        for (int i = 0; i < 4; i++)
        {
            if (_pages[i] != null) _pages[i].SetActive(i == tab);
            bool on = i == tab;
            _tabIcons[i].color = on ? Color.white : Brown;
            _tabLabels[i].color = on ? Color.white : Brown;
        }
    }

    // ------------------------------------------------------------------ loading screen

    void BuildLoadingPanel()
    {
        _loadingPanel = Panel("LoadingPanel", new Color(0.98f, 0.93f, 0.84f, 1f));
        var p = (RectTransform)_loadingPanel.transform;

        _loadingCat = GameUI.Img(p, "LoadingCat", CatSprites.Icon(4), Color.white, Center, new Vector2(0, 160), new Vector2(300, 300)).rectTransform;
        _loadingText = GameUI.Label(p, "LoadingText", "Loading", 70, TextAnchor.MiddleCenter, Center, new Vector2(0, -60), new Vector2(800, 100));
        _loadingText.color = Ink;

        var track = GameUI.RoundImg(p, "BarTrack", new Color(0.85f, 0.75f, 0.6f), Center, new Vector2(0, -180), new Vector2(BarWidth, 36), 18f);
        var fill = GameUI.RoundImg((RectTransform)track.transform, "BarFill", Orange, new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 36), 18f);
        _barFill = fill.rectTransform;
        _barFill.pivot = new Vector2(0, 0.5f);

        _percent = GameUI.Label(p, "Percent", "0%", 50, TextAnchor.MiddleCenter, Center, new Vector2(0, -270), new Vector2(400, 70));
        _percent.color = Ink;
    }

    void OnPlay()
    {
        if (_loading) return;
        _loading = true;
        AudioManager.Instance.Merge(2, 1);          // meow
        Haptics.Light();
        _popup.Close();
        _loadingPanel.SetActive(true);
        StartCoroutine(LoadGame());
    }

    IEnumerator LoadGame()
    {
        var op = SceneManager.LoadSceneAsync(GameScene);
        op.allowSceneActivation = false;               // hold at 90% until the bar has finished

        float t = 0f, shown = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            float real = Mathf.Clamp01(op.progress / 0.9f);          // Unity reports 0..0.9 until activation
            float target = Mathf.Min(real, t / MinLoadTime);
            shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 1.5f);
            SetProgress(shown);
            if (shown >= 1f) break;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.2f);  // show 100% for a moment
        op.allowSceneActivation = true;
    }

    void SetProgress(float p)
    {
        _barFill.sizeDelta = new Vector2(BarWidth * p, 36f);
        _percent.text = Mathf.RoundToInt(p * 100f) + "%";
        int dots = (int)(Time.unscaledTime * 3f) % 4;
        _loadingText.text = "Loading" + new string('.', dots);
    }

    void Update()
    {
        float t = Time.unscaledTime;

        if (_bobbers != null && _pages[HomeTab].activeSelf)
            for (int i = 0; i < _bobbers.Length; i++)
            {
                var pos = _bobbers[i].anchoredPosition;
                pos.y = 110f + Mathf.Sin(t * 2.2f + i * 0.9f) * 18f;
                _bobbers[i].anchoredPosition = pos;
                _bobbers[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.6f + i) * 5f);
            }

        for (int i = 0; i < _soonCats.Length; i++)
            if (_soonCats[i] != null && _pages[i].activeSelf)
            {
                var pos = _soonCats[i].anchoredPosition;
                pos.y = -360f + Mathf.Sin(t * 2.2f) * 16f;
                _soonCats[i].anchoredPosition = pos;
                _soonCats[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.6f) * 5f);
            }

        // the highlight slides to the selected tab
        float target = (_tab + 0.5f) / 4f;
        _pillX = Mathf.Lerp(_pillX, target, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
        _pill.anchorMin = _pill.anchorMax = new Vector2(_pillX, 0f);
        _pill.anchoredPosition = new Vector2(0f, _pill.anchoredPosition.y);

        if (_loading && _loadingCat != null)            // the cat hops while loading
        {
            var pos = _loadingCat.anchoredPosition;
            pos.y = 160f + Mathf.Abs(Mathf.Sin(t * 5f)) * 60f;
            _loadingCat.anchoredPosition = pos;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && !_loading) _popup.Toggle();   // Android back button
    }

    // ------------------------------------------------------------------ helpers

    GameObject Panel(string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(_root, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = color.a > 0f;               // the loading panel blocks clicks; page panels do not need to
        return go;
    }

    static void Outline(Text t, Color c, float d)
    {
        var o = t.gameObject.AddComponent<UnityEngine.UI.Outline>();
        o.effectColor = c;
        o.effectDistance = new Vector2(d, -d);
    }
}
