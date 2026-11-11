using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Compact settings popup shared by the Home screen and the game: sound and haptic switches, a language selector and,
/// in game, Restart / Menu / Resume. Tapping the dimmed background or X closes it.
/// </summary>
public class SettingsPopup
{
    static readonly Color Brown = new Color(0.55f, 0.36f, 0.24f);
    static readonly Color Cream = new Color(1f, 0.97f, 0.9f);
    static readonly Color Ink = new Color(0.36f, 0.22f, 0.15f);
    static readonly Color On = new Color(0.2f, 0.65f, 0.4f);
    static readonly Color Off = new Color(0.78f, 0.72f, 0.66f);

    public GameObject Root { get; private set; }
    public bool IsOpen => Root.activeSelf;

    readonly Action<bool> _onOpenChanged;
    Image _sndTrack, _hapTrack, _enBtn, _viBtn;
    RectTransform _sndKnob, _hapKnob;
    CheatPanel _cheat;
    int _titleTaps;
    float _lastTitleTap;

    SettingsPopup(Action<bool> onOpenChanged) { _onOpenChanged = onOpenChanged; }

    /// <param name="inGame">adds Restart / Menu / Resume</param>
    /// <param name="onOpenChanged">called with true when opened, false when closed (the game uses it to pause)</param>
    public static SettingsPopup Create(RectTransform parent, bool inGame, Action<bool> onOpenChanged = null,
                                       Action onRestart = null, Action onMenu = null)
    {
        var p = new SettingsPopup(onOpenChanged);
        p.Build(parent, inGame, onRestart, onMenu);
        return p;
    }

    public void Open() => Set(true);
    public void Close() => Set(false);
    public void Toggle() => Set(!IsOpen);

    void Set(bool open)
    {
        if (Root.activeSelf == open) return;
        Root.SetActive(open);
        if (open) Refresh();
        _onOpenChanged?.Invoke(open);
    }

    void Build(RectTransform parent, bool inGame, Action onRestart, Action onMenu)
    {
        var center = new Vector2(0.5f, 0.5f);

        // full-screen dim: blocks the screen behind and closes the popup when tapped
        Root = new GameObject("SettingsPopup", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)Root.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        Root.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);
        var dimBtn = Root.GetComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(() => { AudioManager.Instance.Click(); Close(); });

        float w = 620f, h = inGame ? 790f : 440f, top = h * 0.5f;
        GameUI.RoundImg(rt, "CardBorder", Brown, center, Vector2.zero, new Vector2(w + 10, h + 10), 42f).raycastTarget = true;
        var card = GameUI.RoundImg(rt, "Card", Cream, center, Vector2.zero, new Vector2(w, h), 38f);
        card.raycastTarget = true;
        card.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;   // absorbs taps (they would otherwise reach the dim background and close the popup)

        var title = GameUI.Label(rt, "Title", "", 54, TextAnchor.MiddleCenter, center, new Vector2(0, top - 70), new Vector2(500, 80));
        title.color = Ink;
        Loc.Bind(title, () => Loc.T("settings"));

        // tapping the title five times opens the tester's cheat menu (password protected)
        var titleHit = GameUI.Img(rt, "TitleHit", SpriteFactory.Square, new Color(0, 0, 0, 0), center, new Vector2(0, top - 70), new Vector2(400, 90));
        titleHit.raycastTarget = true;
        var titleBtn = titleHit.gameObject.AddComponent<Button>();
        titleBtn.transition = Selectable.Transition.None;
        titleBtn.onClick.AddListener(OnTitleTap);

        var close = GameUI.Button(rt, "Close", "X", 40, center, new Vector2(w * 0.5f - 62, top - 62), new Vector2(64, 64), Brown);
        close.onClick.AddListener(() => { AudioManager.Instance.Click(); Close(); });

        Row(rt, "sound", w, top - 170, out _sndTrack, out _sndKnob, () =>
        {
            var a = AudioManager.Instance;
            a.SetMuted(!a.Muted);
            a.Click();                                   // audible only when turning sound ON
            Refresh();
        });
        Row(rt, "haptic", w, top - 270, out _hapTrack, out _hapKnob, () =>
        {
            Haptics.Enabled = !Haptics.Enabled;
            AudioManager.Instance.Click();
            if (Haptics.Enabled) Haptics.Medium();       // let the player feel it
            Refresh();
        });
        LanguageRow(rt, w, top - 370);

        if (inGame)
        {
            var restart = GameUI.Button(rt, "Restart", "", 40, center, new Vector2(0, top - 490), new Vector2(460, 84), Brown);
            restart.onClick.AddListener(() => { AudioManager.Instance.Click(); Close(); onRestart?.Invoke(); });
            Loc.Bind(restart, () => Loc.T("restart"));

            var menu = GameUI.Button(rt, "Menu", "", 40, center, new Vector2(0, top - 590), new Vector2(460, 84), Brown);
            menu.onClick.AddListener(() => onMenu?.Invoke());
            Loc.Bind(menu, () => Loc.T("menu"));

            var resume = GameUI.Button(rt, "Resume", "", 44, center, new Vector2(0, top - 710), new Vector2(460, 92), On);
            resume.onClick.AddListener(() => { AudioManager.Instance.Click(); Close(); });
            Loc.Bind(resume, () => Loc.T("resume"));
        }

        _cheat = CheatPanel.Create(rt);       // last, so it covers everything in the popup

        Root.SetActive(false);
    }

    void OnTitleTap()
    {
        if (Time.unscaledTime - _lastTitleTap > 1.5f) _titleTaps = 0;        // taps must follow each other quickly
        _lastTitleTap = Time.unscaledTime;
        if (++_titleTaps < 5) return;
        _titleTaps = 0;
        _cheat.Open();
    }

    /// <summary>Label on the left, pill switch on the right.</summary>
    static void Row(RectTransform p, string key, float cardWidth, float y, out Image track, out RectTransform knob, UnityEngine.Events.UnityAction onClick)
    {
        var center = new Vector2(0.5f, 0.5f);
        var label = GameUI.Label(p, key + "Label", "", 44, TextAnchor.MiddleLeft, center, new Vector2(-cardWidth * 0.5f + 55, y), new Vector2(300, 70));
        label.color = Ink;
        Loc.Bind(label, () => Loc.T(key));
        track = GameUI.RoundImg(p, key + "Switch", Off, center, new Vector2(cardWidth * 0.5f - 55 - 65, y), new Vector2(130, 62), 31f);
        track.raycastTarget = true;
        var b = track.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(onClick);
        knob = GameUI.Img((RectTransform)track.transform, "Knob", SpriteFactory.Disc, Color.white, center, Vector2.zero, new Vector2(50, 50)).rectTransform;
    }

    /// <summary>Label on the left, EN | VI buttons on the right.</summary>
    void LanguageRow(RectTransform p, float cardWidth, float y)
    {
        var center = new Vector2(0.5f, 0.5f);
        var label = GameUI.Label(p, "languageLabel", "", 44, TextAnchor.MiddleLeft, center, new Vector2(-cardWidth * 0.5f + 55, y), new Vector2(300, 70));
        label.color = Ink;
        Loc.Bind(label, () => Loc.T("language"));

        float right = cardWidth * 0.5f - 55;
        var vi = GameUI.Button(p, "LangVI", "VI", 32, center, new Vector2(right - 40, y), new Vector2(80, 58), Brown);
        var en = GameUI.Button(p, "LangEN", "EN", 32, center, new Vector2(right - 40 - 88, y), new Vector2(80, 58), Brown);
        _viBtn = (Image)vi.targetGraphic;
        _enBtn = (Image)en.targetGraphic;
        en.onClick.AddListener(() => { AudioManager.Instance.Click(); Loc.SetLanguage("en"); Refresh(); });
        vi.onClick.AddListener(() => { AudioManager.Instance.Click(); Loc.SetLanguage("vi"); Refresh(); });
    }

    void Refresh()
    {
        SetSwitch(_sndTrack, _sndKnob, !AudioManager.Instance.Muted);
        SetSwitch(_hapTrack, _hapKnob, Haptics.Enabled);
        _enBtn.color = Loc.Language == "en" ? On : Brown;
        _viBtn.color = Loc.Language == "vi" ? On : Brown;
    }

    static void SetSwitch(Image track, RectTransform knob, bool on)
    {
        track.color = on ? On : Off;
        knob.anchoredPosition = new Vector2(on ? 34f : -34f, 0f);
    }
}
