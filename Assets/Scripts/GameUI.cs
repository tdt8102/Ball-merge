using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Builds the whole HUD in code (1080x1920 reference, portrait).</summary>
public class GameUI : MonoBehaviour
{
    GameManager _gm;
    Text _score, _best, _finalScore;
    Image _nextIcon;
    GameObject _gameOver;
    Button _sndBtn, _hapBtn;
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
        _best.color = new Color(0.4f, 0.3f, 0.25f);

        // --- next fruit (top-right) ---
        var nextLbl = Label(root, "NextLabel", "NEXT", 36, TextAnchor.UpperCenter, new Vector2(1, 1), new Vector2(-190, -30), new Vector2(200, 44));
        nextLbl.color = new Color(0.4f, 0.3f, 0.25f);
        _nextIcon = Img(root, "NextIcon", SpriteFactory.Circle, Color.white, new Vector2(1, 1), new Vector2(-190, -140), new Vector2(100, 100));

        // --- restart (top-center-right) ---
        var restart = Button(root, "RestartSmall", "RESTART", 34, new Vector2(1, 1), new Vector2(-190, -250), new Vector2(200, 64), new Color(0.55f, 0.36f, 0.24f));
        restart.onClick.AddListener(() => { AudioManager.Instance.Click(); _gm.NewGame(); });

        // --- sound / haptic toggles (under BEST) ---
        _sndBtn = Button(root, "SoundToggle", "", 30, new Vector2(0, 1), new Vector2(135, -250), new Vector2(190, 64), Color.gray);
        _sndBtn.onClick.AddListener(() =>
        {
            var a = AudioManager.Instance;
            a.SetMuted(!a.Muted);
            a.Click();                       // audible only when turning sound ON
            RefreshToggles();
        });
        _hapBtn = Button(root, "HapticToggle", "", 30, new Vector2(0, 1), new Vector2(345, -250), new Vector2(190, 64), Color.gray);
        _hapBtn.onClick.AddListener(() =>
        {
            Haptics.Enabled = !Haptics.Enabled;
            AudioManager.Instance.Click();
            if (Haptics.Enabled) Haptics.Medium();   // let the player feel it
            RefreshToggles();
        });
        RefreshToggles();

        // --- evolution chain (bottom) ---
        const float slot = 90f;
        float startX = -slot * (FruitDatabase.MaxLevel) * 0.5f;
        for (int i = 0; i <= FruitDatabase.MaxLevel; i++)
        {
            float size = 28f + i * 5f;
            Img(root, "Chain" + i, SpriteFactory.Circle, FruitDatabase.Colors[i], new Vector2(0.5f, 0), new Vector2(startX + i * slot, 70f), new Vector2(size, size));
        }

        // --- game over panel ---
        _gameOver = new GameObject("GameOver", typeof(RectTransform), typeof(Image));
        var go = (RectTransform)_gameOver.transform;
        go.SetParent(root, false);
        go.anchorMin = Vector2.zero; go.anchorMax = Vector2.one; go.offsetMin = go.offsetMax = Vector2.zero;
        _gameOver.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        Label(go, "Title", "GAME OVER", 120, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(900, 160));
        _finalScore = Label(go, "Final", "0", 100, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(900, 140));
        var again = Button(go, "Again", "PLAY AGAIN", 60, new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(520, 130), new Color(0.2f, 0.65f, 0.4f));
        again.onClick.AddListener(() => { AudioManager.Instance.Click(); _gm.NewGame(); });
        _gameOver.SetActive(false);

        // Open the page with ?debug (or #debug) to show FPS / fruit count / last error on screen.
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

    void RefreshToggles()
    {
        SetToggle(_sndBtn, "SOUND", !AudioManager.Instance.Muted);
        SetToggle(_hapBtn, "HAPTIC", Haptics.Enabled);
    }

    static void SetToggle(Button b, string label, bool on)
    {
        b.GetComponentInChildren<Text>().text = label + (on ? " ON" : " OFF");
        ((Image)b.targetGraphic).color = on ? new Color(0.2f, 0.65f, 0.4f) : new Color(0.6f, 0.55f, 0.5f);
    }

    void Update()
    {
        if (_debug != null)
        {
            float dt = Time.unscaledDeltaTime;
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(dt, 0.0001f), 0.05f);
            _worst = Mathf.Max(_worst, dt);
            _dbgT += dt;
            if (_dbgT >= 0.5f)
            {
                _dbgT = 0f;
                _debug.text = $"fps {_fps:F0}  worst {_worst * 1000f:F0}ms  fruits {_gm.FruitCount}\n" +
                              $"mem {System.GC.GetTotalMemory(false) / 1048576f:F1}MB  err: {_lastError}";
                if (_fps > 30f) _worst = 0f;     // keep the worst frame visible while the game is slow
            }
        }

        // score "bump" when it increases
        if (_pop > 0f)
        {
            _pop = Mathf.Max(0f, _pop - Time.deltaTime * 4f);
            _score.rectTransform.localScale = Vector3.one * (1f + 0.3f * _pop);
        }
    }

    public void Refresh()
    {
        if (_gm == null || _score == null) return;
        if (_gm.Score > _lastScore) _pop = 1f;
        _lastScore = _gm.Score;
        _score.text = _gm.Score.ToString();
        _best.text = "BEST " + _gm.Best;
        _nextIcon.color = FruitDatabase.Colors[_gm.NextLevel];
        float d = 40f + FruitDatabase.Radius[_gm.NextLevel] * 110f;
        _nextIcon.rectTransform.sizeDelta = new Vector2(d, d);
    }

    public void ShowGameOver(bool show)
    {
        _gameOver.SetActive(show);
        if (show) _finalScore.text = "Score  " + _gm.Score;
    }

    // ---------------------------------------------------------------- combo popup

    static readonly string[] ComboWords = { "Good!", "Great!", "Awesome!", "Amazing!", "Incredible!" };
    static readonly Color[] ComboColors =
    {
        new Color(1f, 0.85f, 0.3f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.4f, 0.4f),
        new Color(0.9f, 0.4f, 0.9f), new Color(0.4f, 0.8f, 1f)
    };

    /// <summary>Small floating encouragement text at a world position. combo >= 2.</summary>
    public void ShowCombo(Vector2 worldPos, int combo)
    {
        int tier = Mathf.Clamp(combo - 2, 0, ComboWords.Length - 1);
        var root = (RectTransform)transform;
        Vector2 screen = Camera.main.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var local);

        var t = Label(root, "Combo", ComboWords[tier] + " x" + combo, 52 + tier * 6, TextAnchor.MiddleCenter,
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

    static Text Label(RectTransform parent, string name, string text, int size, TextAnchor align,
                      Vector2 anchor, Vector2 pos, Vector2 dim)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(align == TextAnchor.UpperLeft ? 0 : 0.5f, align == TextAnchor.MiddleCenter ? 0.5f : 1);
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
        t.raycastTarget = false;
        return t;
    }

    static Image Img(RectTransform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 dim)
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

    static Button Button(RectTransform parent, string name, string text, int fontSize, Vector2 anchor,
                         Vector2 pos, Vector2 dim, Color color)
    {
        var img = Img(parent, name, SpriteFactory.Square, color, anchor, pos, dim);
        img.raycastTarget = true;
        var btn = img.gameObject.AddComponent<Button>();
        var lbl = Label((RectTransform)img.transform, "Text", text, fontSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, dim);
        lbl.color = Color.white;
        return btn;
    }
}
