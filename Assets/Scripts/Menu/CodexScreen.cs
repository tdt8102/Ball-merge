using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cat Codex: one page per cat theme (arrows or swipe to flip). Each page shows the 11 cats of that theme.
/// The first theme (Classic) is about discovering the cats while playing: unseen cats are black silhouettes with the coin reward
/// for finding them. The other themes are about collecting: cats won in the Gacha are shown, the rest are silhouettes with their rarity.
/// </summary>
public class CodexScreen
{
    static readonly Color Brown = new Color(0.55f, 0.36f, 0.24f);
    static readonly Color Ink = new Color(0.36f, 0.22f, 0.15f);
    static readonly Color Soft = new Color(0.5f, 0.36f, 0.28f);
    static readonly Color Gold = new Color(0.9f, 0.6f, 0.1f);
    static readonly Color Orange = new Color(0.95f, 0.6f, 0.2f);
    static readonly Color Cream = new Color(1f, 0.97f, 0.9f);

    const int Levels = CatDatabase.MaxLevel + 1;

    public GameObject Root { get; private set; }
    public bool IsOpen => Root.activeSelf;

    Text _themeName, _progress;
    RectTransform _barFill;
    float _barWidth;
    int _page;
    readonly Image[] _icons = new Image[Levels];
    readonly Text[] _names = new Text[Levels];
    readonly Text[] _subs = new Text[Levels];
    readonly Image[] _cards = new Image[Levels];
    readonly Image[] _borders = new Image[Levels];
    readonly Image[] _coins = new Image[Levels];
    Image[] _dots;

    // Drawing a cat takes a few milliseconds; do one per frame instead of all at once.
    readonly Queue<Action> _work = new Queue<Action>();
    readonly Dictionary<Image, int> _wanted = new Dictionary<Image, int>();

    public static CodexScreen Create(RectTransform parent)
    {
        var c = new CodexScreen();
        c.Build(parent);
        return c;
    }

    public void Open()
    {
        Refresh();
        Root.SetActive(true);
    }

    public void Close() => Root.SetActive(false);

    /// <summary>Call every frame while the game is on the menu: flips pages on a swipe and draws pending cats.</summary>
    public void Tick()
    {
        if (!IsOpen) return;
        if (_work.Count > 0) _work.Dequeue()();
        int swipe = Swipe.Poll();
        if (swipe != 0) Flip(swipe);
    }

    void Flip(int d)
    {
        int n = Cosmetics.Cats.Count;
        _page = (_page + d + n) % n;
        AudioManager.Instance.Click();
        Refresh();
    }

    void Build(RectTransform parent)
    {
        var top = new Vector2(0.5f, 1f);

        Root = new GameObject("CodexScreen", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)Root.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        Root.GetComponent<Image>().color = new Color(0.98f, 0.93f, 0.84f, 1f);       // opaque: covers the tab bar too

        var title = GameUI.Label(rt, "Title", "CAT CODEX", 90, TextAnchor.MiddleCenter, top, new Vector2(0, -110), new Vector2(1000, 120));
        title.color = Ink;
        Loc.Bind(title, () => Loc.T("codex"));
        var outline = title.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(4, -4);

        // theme name between two arrows
        var prev = GameUI.Button(rt, "Prev", "<", 56, top, new Vector2(-390, -225), new Vector2(110, 96), Brown);
        prev.onClick.AddListener(() => Flip(-1));
        var next = GameUI.Button(rt, "Next", ">", 56, top, new Vector2(390, -225), new Vector2(110, 96), Brown);
        next.onClick.AddListener(() => Flip(1));
        _themeName = GameUI.Label(rt, "Theme", "", 58, TextAnchor.MiddleCenter, top, new Vector2(0, -225), new Vector2(560, 80));
        _themeName.color = Orange;

        _progress = GameUI.Label(rt, "Progress", "", 38, TextAnchor.MiddleCenter, top, new Vector2(0, -300), new Vector2(900, 52));
        _progress.color = Soft;

        _barWidth = 640f;
        var track = GameUI.RoundImg(rt, "BarTrack", new Color(0.85f, 0.75f, 0.6f), top, new Vector2(0, -350), new Vector2(_barWidth, 24), 12f);
        var fill = GameUI.RoundImg((RectTransform)track.transform, "BarFill", Orange, new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 24), 12f);
        _barFill = fill.rectTransform;
        _barFill.pivot = new Vector2(0, 0.5f);

        // 3 columns; the last row (2 cats) is centred
        const float w = 310f, h = 270f, gapY = 18f, firstRowY = -395f - h * 0.5f;
        float[] colX = { -340f, 0f, 340f };
        for (int i = 0; i < Levels; i++)
        {
            int row = i / 3, col = i % 3;
            float x = row == 3 ? (col == 0 ? -170f : 170f) : colX[col];
            BuildCard(rt, i, top, new Vector2(x, firstRowY - row * (h + gapY)), w, h);
        }

        // page dots
        int pages = Cosmetics.Cats.Count;
        _dots = new Image[pages];
        for (int i = 0; i < pages; i++)
            _dots[i] = GameUI.Img(rt, "Dot" + i, SpriteFactory.Disc, Color.white, top, new Vector2((i - (pages - 1) * 0.5f) * 44f, -1625), new Vector2(24, 24));

        var back = GameUI.Button(rt, "Back", "", 50, new Vector2(0.5f, 0), new Vector2(0, 95), new Vector2(460, 100), Brown);
        back.onClick.AddListener(() => { AudioManager.Instance.Click(); Close(); });
        Loc.Bind(back, () => Loc.T("back"));

        Loc.Changed += OnLanguage;
        Root.SetActive(false);
    }

    void OnLanguage()
    {
        if (Root != null && Root.activeSelf) Refresh();
    }

    void BuildCard(RectTransform parent, int level, Vector2 anchor, Vector2 pos, float w, float h)
    {
        _borders[level] = GameUI.RoundImg(parent, "CardBorder" + level, Brown, anchor, pos, new Vector2(w + 8, h + 8), 40f);
        var card = GameUI.RoundImg(parent, "Card" + level, Cream, anchor, pos, new Vector2(w, h), 36f);
        _cards[level] = card;
        var cr = (RectTransform)card.transform;
        var c = new Vector2(0.5f, 0.5f);

        _icons[level] = GameUI.Img(cr, "Cat", null, Color.white, c, new Vector2(0, 38), new Vector2(180, 180));
        _names[level] = GameUI.Label(cr, "Name", "", 34, TextAnchor.MiddleCenter, c, new Vector2(0, -72), new Vector2(w - 20, 46));
        _names[level].color = Ink;
        _subs[level] = GameUI.Label(cr, "Sub", "", 28, TextAnchor.MiddleCenter, c, new Vector2(-14, -112), new Vector2(w - 20, 40));
        _subs[level].color = Soft;
        _coins[level] = GameUI.Img(cr, "Coin", CatSprites.CoinIcon(), Color.white, c, new Vector2(-46, -112), new Vector2(34, 34));
    }

    void Refresh()
    {
        var theme = Cosmetics.Cats[_page];
        bool classic = theme.Default;                      // the first theme is "discover while playing"
        _themeName.text = theme.Name;
        for (int i = 0; i < _dots.Length; i++) _dots[i].color = i == _page ? Orange : new Color(0.85f, 0.75f, 0.6f);

        int have = 0;
        for (int i = 0; i < Levels; i++)
        {
            var item = Cosmetics.CatItemOf(theme, i);
            bool seen = classic ? PlayerData.IsDiscovered(i) : PlayerData.IsOwned(item.Id);
            if (seen) have++;

            ShowCat(_icons[i], i, theme.Index, seen ? Color.white : new Color(0.07f, 0.05f, 0.07f, 1f));     // black silhouette when not found
            _cards[i].color = seen ? Cream : new Color(0.93f, 0.88f, 0.8f);
            _borders[i].color = seen && !classic ? Cosmetics.RarityColor(item.Rarity) : Brown;
            _names[i].text = seen ? Loc.CatName(i) : "???";

            if (classic)
            {
                if (seen)
                {
                    _subs[i].text = Loc.T("seen") + "  x" + PlayerData.Seen(i);
                    _subs[i].color = Soft;
                    _subs[i].rectTransform.anchoredPosition = new Vector2(0, -112);
                    _coins[i].gameObject.SetActive(false);
                }
                else
                {
                    int reward = CatDatabase.DiscoveryReward(i);
                    bool starter = reward <= 0;
                    _subs[i].text = starter ? Loc.T("starter") : "+" + reward;
                    _subs[i].color = starter ? Soft : Gold;
                    _subs[i].rectTransform.anchoredPosition = new Vector2(starter ? 0 : 14, -112);
                    _coins[i].gameObject.SetActive(!starter);
                }
            }
            else
            {
                _subs[i].text = CosmeticUI.RarityName(item.Rarity);
                _subs[i].color = Cosmetics.RarityColor(item.Rarity);
                _subs[i].rectTransform.anchoredPosition = new Vector2(0, -112);
                _coins[i].gameObject.SetActive(false);
            }
        }
        _progress.text = Loc.T(classic ? "discovered" : "collected") + "  " + have + " / " + Levels;
        _barFill.sizeDelta = new Vector2(_barWidth * have / Levels, 24f);
    }

    void ShowCat(Image img, int level, int style, Color shown)
    {
        int key = level * 100 + style;
        _wanted[img] = key;
        if (CatSprites.IsReady(level, style)) { img.sprite = CatSprites.Icon(level, style); img.color = shown; return; }
        img.color = Color.clear;
        _work.Enqueue(() =>
        {
            if (img == null || !_wanted.TryGetValue(img, out int now) || now != key) return;
            img.sprite = CatSprites.Icon(level, style);
            img.color = shown;
        });
    }
}
