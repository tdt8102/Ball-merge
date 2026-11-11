using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Title screen: PLAY shows a loading screen (progress bar) while the "Main" scene loads, then starts the game.
/// Put it on an empty GameObject in the first scene; it builds its own UI.
/// </summary>
public class MenuController : MonoBehaviour
{
    public const string GameScene = "Main";
    public const string MenuScene = "Menu";

    const float MinLoadTime = 1.6f;     // keep the loading screen visible long enough to read (the game loads instantly)

    RectTransform _root;
    GameObject _menuPanel, _loadingPanel;
    RectTransform _barFill;
    Text _percent, _loadingText;
    RectTransform[] _bobbers;
    RectTransform _loadingCat;
    bool _loading;
    const float BarWidth = 640f;

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

        BuildMenuPanel();
        BuildLoadingPanel();
        _loadingPanel.SetActive(false);
    }

    // ------------------------------------------------------------------ title screen

    void BuildMenuPanel()
    {
        _menuPanel = Panel("MenuPanel", Color.clear);
        var p = (RectTransform)_menuPanel.transform;
        var brown = new Color(0.36f, 0.22f, 0.15f);

        var title = GameUI.Label(p, "Title", "CatCatCat", 150, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 600), new Vector2(1000, 200));
        title.color = brown;
        Outline(title, Color.white, 4);
        var sub = GameUI.Label(p, "Sub", "Drop and merge the cats!", 46, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 470), new Vector2(1000, 70));
        sub.color = new Color(0.5f, 0.36f, 0.28f);

        // a row of cats that bob up and down (levels 0..10 sampled)
        int[] levels = { 0, 3, 10, 4, 8 };
        float[] sizes = { 190, 230, 300, 230, 190 };
        float[] xs = { -400, -200, 0, 200, 400 };
        _bobbers = new RectTransform[levels.Length];
        for (int i = 0; i < levels.Length; i++)
        {
            var img = GameUI.Img(p, "Cat" + i, CatSprites.Icon(levels[i]), Color.white, new Vector2(0.5f, 0.5f), new Vector2(xs[i], 150), new Vector2(sizes[i], sizes[i]));
            _bobbers[i] = img.rectTransform;
        }

        var play = GameUI.Button(p, "Play", "PLAY", 90, new Vector2(0.5f, 0.5f), new Vector2(0, -230), new Vector2(560, 170), new Color(0.2f, 0.65f, 0.4f));
        play.onClick.AddListener(OnPlay);

        int best = PlayerPrefs.GetInt("best", 0);
        var bestLbl = GameUI.Label(p, "Best", "BEST  " + best, 50, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -420), new Vector2(800, 80));
        bestLbl.color = new Color(0.5f, 0.36f, 0.28f);
    }

    // ------------------------------------------------------------------ loading screen

    void BuildLoadingPanel()
    {
        _loadingPanel = Panel("LoadingPanel", new Color(0.98f, 0.93f, 0.84f, 1f));
        var p = (RectTransform)_loadingPanel.transform;
        var brown = new Color(0.36f, 0.22f, 0.15f);

        _loadingCat = GameUI.Img(p, "LoadingCat", CatSprites.Icon(4), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(300, 300)).rectTransform;
        _loadingText = GameUI.Label(p, "LoadingText", "Loading", 70, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(800, 100));
        _loadingText.color = brown;

        var track = GameUI.RoundImg(p, "BarTrack", new Color(0.85f, 0.75f, 0.6f), new Vector2(0.5f, 0.5f), new Vector2(0, -180), new Vector2(BarWidth, 36), 18f);
        var fill = GameUI.RoundImg((RectTransform)track.transform, "BarFill", new Color(0.95f, 0.6f, 0.2f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 36), 18f);
        _barFill = fill.rectTransform;
        _barFill.pivot = new Vector2(0, 0.5f);

        _percent = GameUI.Label(p, "Percent", "0%", 50, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -270), new Vector2(400, 70));
        _percent.color = brown;
    }

    void OnPlay()
    {
        if (_loading) return;
        _loading = true;
        AudioManager.Instance.Merge(2, 1);          // meow
        Haptics.Light();
        _menuPanel.SetActive(false);
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
        if (_bobbers != null && _menuPanel != null && _menuPanel.activeSelf)
            for (int i = 0; i < _bobbers.Length; i++)
            {
                var pos = _bobbers[i].anchoredPosition;
                pos.y = 150f + Mathf.Sin(t * 2.2f + i * 0.9f) * 18f;
                _bobbers[i].anchoredPosition = pos;
                _bobbers[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.6f + i) * 5f);
            }
        if (_loading && _loadingCat != null)            // the cat hops while loading
        {
            var pos = _loadingCat.anchoredPosition;
            pos.y = 160f + Mathf.Abs(Mathf.Sin(t * 5f)) * 60f;
            _loadingCat.anchoredPosition = pos;
        }
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
        img.raycastTarget = color.a > 0f;               // the loading panel blocks clicks; the title panel does not need to
        return go;
    }

    static void Outline(Text t, Color c, float d)
    {
        var o = t.gameObject.AddComponent<UnityEngine.UI.Outline>();
        o.effectColor = c;
        o.effectDistance = new Vector2(d, -d);
    }
}
