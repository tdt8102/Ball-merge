using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Compact settings popup shared by the Home screen and the game: two switches (sound, haptic) and, in game,
/// Restart / Menu / Resume. Tapping the dimmed background or X closes it.
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
    Image _sndTrack, _hapTrack;
    RectTransform _sndKnob, _hapKnob;

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

        float w = 620f, h = inGame ? 690f : 340f, top = h * 0.5f;
        GameUI.RoundImg(rt, "CardBorder", Brown, center, Vector2.zero, new Vector2(w + 10, h + 10), 42f).raycastTarget = true;
        GameUI.RoundImg(rt, "Card", Cream, center, Vector2.zero, new Vector2(w, h), 38f).raycastTarget = true;   // absorbs taps

        GameUI.Label(rt, "Title", "SETTINGS", 54, TextAnchor.MiddleCenter, center, new Vector2(0, top - 70), new Vector2(500, 80)).color = Ink;

        var close = GameUI.Button(rt, "Close", "X", 40, center, new Vector2(w * 0.5f - 62, top - 62), new Vector2(64, 64), Brown);
        close.onClick.AddListener(() => { AudioManager.Instance.Click(); Close(); });

        Row(rt, "Sound", w, top - 170, out _sndTrack, out _sndKnob, () =>
        {
            var a = AudioManager.Instance;
            a.SetMuted(!a.Muted);
            a.Click();                                   // audible only when turning sound ON
            Refresh();
        });
        Row(rt, "Haptic", w, top - 270, out _hapTrack, out _hapKnob, () =>
        {
            Haptics.Enabled = !Haptics.Enabled;
            AudioManager.Instance.Click();
            if (Haptics.Enabled) Haptics.Medium();       // let the player feel it
            Refresh();
        });

        if (inGame)
        {
            GameUI.Button(rt, "Restart", "RESTART", 40, center, new Vector2(0, top - 390), new Vector2(460, 84), Brown)
                .onClick.AddListener(() => { AudioManager.Instance.Click(); Close(); onRestart?.Invoke(); });
            GameUI.Button(rt, "Menu", "MENU", 40, center, new Vector2(0, top - 490), new Vector2(460, 84), Brown)
                .onClick.AddListener(() => onMenu?.Invoke());
            GameUI.Button(rt, "Resume", "RESUME", 44, center, new Vector2(0, top - 610), new Vector2(460, 92), On)
                .onClick.AddListener(() => { AudioManager.Instance.Click(); Close(); });
        }

        Root.SetActive(false);
    }

    /// <summary>Label on the left, pill switch on the right.</summary>
    static void Row(RectTransform p, string label, float cardWidth, float y, out Image track, out RectTransform knob, UnityEngine.Events.UnityAction onClick)
    {
        var center = new Vector2(0.5f, 0.5f);
        GameUI.Label(p, label + "Label", label, 44, TextAnchor.MiddleLeft, center, new Vector2(-cardWidth * 0.5f + 55, y), new Vector2(300, 70)).color = Ink;
        track = GameUI.RoundImg(p, label + "Switch", Off, center, new Vector2(cardWidth * 0.5f - 55 - 65, y), new Vector2(130, 62), 31f);
        track.raycastTarget = true;
        var b = track.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(onClick);
        knob = GameUI.Img((RectTransform)track.transform, "Knob", SpriteFactory.Disc, Color.white, center, Vector2.zero, new Vector2(50, 50)).rectTransform;
    }

    void Refresh()
    {
        SetSwitch(_sndTrack, _sndKnob, !AudioManager.Instance.Muted);
        SetSwitch(_hapTrack, _hapKnob, Haptics.Enabled);
    }

    static void SetSwitch(Image track, RectTransform knob, bool on)
    {
        track.color = on ? On : Off;
        knob.anchoredPosition = new Vector2(on ? 34f : -34f, 0f);
    }
}
