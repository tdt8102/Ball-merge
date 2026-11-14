using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Star exchange: pick any cat of any theme that you do not own and buy it with star shards. One page per theme (arrows or
/// swipe); the detail panel shows the cat, its theme, rarity, cost and your shard balance, and the button is disabled when
/// the cat is already owned or the shards are not enough. The screen only reads PlayerData / Cosmetics and calls
/// Cosmetics.Exchange; the transaction (charge once, grant once, set completion) is done there, not here.
/// </summary>
public class ExchangeScreen : MonoBehaviour
{
    static readonly Color Brown = new Color(0.55f, 0.36f, 0.24f);
    static readonly Color Ink = new Color(0.36f, 0.22f, 0.15f);
    static readonly Color Soft = new Color(0.5f, 0.36f, 0.28f);
    static readonly Color Orange = new Color(0.95f, 0.6f, 0.2f);
    static readonly Color Green = new Color(0.2f, 0.65f, 0.4f);
    static readonly Color Red = new Color(0.85f, 0.25f, 0.2f);
    static readonly Color Gray = new Color(0.72f, 0.66f, 0.6f);
    static readonly Color Cream = new Color(1f, 0.97f, 0.9f);
    static readonly Vector2 Top = new Vector2(0.5f, 1f);
    static readonly Vector2 C = new Vector2(0.5f, 0.5f);

    const int Levels = CatDatabase.MaxLevel + 1;
    const float CellW = 236f, CellH = 232f;

    public bool IsOpen => gameObject.activeSelf;

    SetCompleteScreen _complete;
    Action _onChanged;
    int _page, _selected;
    Text _themeName, _setProgress, _chipText;
    readonly Image[] _borders = new Image[Levels], _cards = new Image[Levels], _thumbs = new Image[Levels], _stars = new Image[Levels];
    readonly Text[] _names = new Text[Levels], _prices = new Text[Levels];

    // detail panel
    Image _bigCat, _costStar, _haveStar;
    Text _name, _themeInfo, _rarity, _cost, _have, _status;
    Button _btn;
    Image _btnBg;
    Text _btnLabel;
    float _flashT = 99f;

    // drawing a cat takes a few milliseconds: one per frame instead of all at once
    readonly Queue<Action> _work = new Queue<Action>();
    readonly Dictionary<Image, int> _wanted = new Dictionary<Image, int>();

    /// <param name="onChanged">Called after a successful exchange (the Gacha tab refreshes its counters).</param>
    public static ExchangeScreen Create(RectTransform parent, SetCompleteScreen complete, Action onChanged)
    {
        var go = new GameObject("ExchangeScreen", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0.98f, 0.93f, 0.84f, 1f);          // opaque: covers the tab bar too
        var s = go.AddComponent<ExchangeScreen>();
        s._complete = complete;
        s._onChanged = onChanged;
        s.Build(rt);
        go.SetActive(false);
        return s;
    }

    public void Open()
    {
        _page = FirstThemeWithMissingCat();
        _selected = FirstToShow(Cosmetics.Cats[_page]);
        _flashT = 99f;
        Refresh();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        if (!IsOpen) return;
        AudioManager.Instance.Click();
        gameObject.SetActive(false);
    }

    static int FirstThemeWithMissingCat()
    {
        for (int i = 0; i < Cosmetics.Cats.Count; i++)
            if (FirstMissing(Cosmetics.Cats[i]) >= 0) return i;
        return 0;
    }

    /// <summary>Index (cat number - 1) of the first cat of the theme the player does not own; -1 when the set is complete.</summary>
    static int FirstMissing(CatStyle theme)
    {
        for (int l = 0; l < Levels; l++) if (!Cosmetics.IsOwned(Cosmetics.CatItemOf(theme, l))) return l;
        return -1;
    }

    /// <summary>The cat to show first on a page: the first one still to get, or the first one when the set is complete.</summary>
    static int FirstToShow(CatStyle theme) => Mathf.Max(0, FirstMissing(theme));

    CatItem Selected => Cosmetics.CatItemOf(Cosmetics.Cats[_page], _selected);

    // ------------------------------------------------------------------ build

    void Build(RectTransform rt)
    {
        var title = GameUI.Label(rt, "Title", "", 84, TextAnchor.MiddleCenter, Top, new Vector2(0, -105), new Vector2(1000, 110));
        title.color = Ink;
        var outline = title.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(4, -4);
        Loc.Bind(title, () => Loc.T("exchange_title"));

        _chipText = GameUI.ShardChip(rt, Top, new Vector2(0, -205), out _);

        var prev = GameUI.Button(rt, "Prev", "<", 56, Top, new Vector2(-390, -305), new Vector2(110, 96), Brown);
        prev.onClick.AddListener(() => Flip(-1));
        var next = GameUI.Button(rt, "Next", ">", 56, Top, new Vector2(390, -305), new Vector2(110, 96), Brown);
        next.onClick.AddListener(() => Flip(1));
        _themeName = GameUI.Label(rt, "Theme", "", 54, TextAnchor.MiddleCenter, Top, new Vector2(0, -305), new Vector2(560, 76));
        _themeName.color = Orange;
        _setProgress = GameUI.Label(rt, "SetProgress", "", 32, TextAnchor.MiddleCenter, Top, new Vector2(0, -392), new Vector2(900, 44));
        _setProgress.color = Soft;

        for (int i = 0; i < Levels; i++)
        {
            int row = i / 4, col = i % 4;
            int inRow = Mathf.Min(4, Levels - row * 4);
            var pos = new Vector2((col - (inRow - 1) * 0.5f) * 250f, -565f - row * 240f);
            BuildCell(rt, i, pos);
        }

        BuildDetail(rt, new Vector2(0, -1440));

        var back = GameUI.Button(rt, "Back", "", 50, new Vector2(0.5f, 0), new Vector2(0, 95), new Vector2(460, 100), Brown);
        back.onClick.AddListener(Close);
        Loc.Bind(back, () => Loc.T("back"));

        Loc.Changed += OnLanguage;
    }

    void OnDestroy() => Loc.Changed -= OnLanguage;

    void OnLanguage()
    {
        if (IsOpen) Refresh();
    }

    void BuildCell(RectTransform rt, int i, Vector2 pos)
    {
        _borders[i] = GameUI.RoundImg(rt, "CellBorder" + i, Brown, Top, pos, new Vector2(CellW + 10, CellH + 10), 36f);
        _cards[i] = GameUI.RoundImg(rt, "Cell" + i, Cream, Top, pos, new Vector2(CellW, CellH), 32f);
        _cards[i].raycastTarget = true;
        var cr = (RectTransform)_cards[i].transform;
        _thumbs[i] = GameUI.Img(cr, "Thumb", null, Color.white, C, new Vector2(0, 46), new Vector2(124, 124));
        _thumbs[i].preserveAspect = true;
        _names[i] = GameUI.Label(cr, "Name", "", 25, TextAnchor.MiddleCenter, C, new Vector2(0, -42), new Vector2(CellW - 10, 34));
        _names[i].color = Ink;
        _stars[i] = GameUI.Img(cr, "Star", CatSprites.ShardIcon(), Color.white, C, new Vector2(-34, -82), new Vector2(36, 36));
        _prices[i] = GameUI.Label(cr, "Price", "", 30, TextAnchor.MiddleCenter, C, new Vector2(10, -82), new Vector2(150, 40));
        int index = i;
        _cards[i].gameObject.AddComponent<Button>().onClick.AddListener(() => Select(index));
    }

    void BuildDetail(RectTransform rt, Vector2 pos)
    {
        GameUI.RoundImg(rt, "DetailBorder", Brown, Top, pos, new Vector2(1008, 528), 44f);
        var face = GameUI.RoundImg(rt, "Detail", Cream, Top, pos, new Vector2(1000, 520), 40f);
        var d = (RectTransform)face.transform;

        _bigCat = GameUI.Img(d, "Cat", null, Color.white, C, new Vector2(-345, 60), new Vector2(270, 270));
        _bigCat.preserveAspect = true;
        _name = GameUI.Label(d, "Name", "", 56, TextAnchor.MiddleLeft, C, new Vector2(-190, 190), new Vector2(660, 76));
        _name.color = Ink;
        _themeInfo = GameUI.Label(d, "Theme", "", 32, TextAnchor.MiddleLeft, C, new Vector2(-190, 130), new Vector2(660, 44));
        _themeInfo.color = Soft;
        _rarity = GameUI.Label(d, "Rarity", "", 36, TextAnchor.MiddleLeft, C, new Vector2(-190, 82), new Vector2(660, 48));

        var costLabel = GameUI.Label(d, "CostLabel", "", 32, TextAnchor.MiddleLeft, C, new Vector2(-190, 22), new Vector2(180, 44));
        costLabel.color = Soft;
        Loc.Bind(costLabel, () => Loc.T("exchange_cost"));
        _costStar = GameUI.Img(d, "CostStar", CatSprites.ShardIcon(), Color.white, C, new Vector2(10, 22), new Vector2(44, 44));
        _cost = GameUI.Label(d, "Cost", "", 44, TextAnchor.MiddleLeft, C, new Vector2(42, 22), new Vector2(200, 56));
        _cost.color = Ink;

        var haveLabel = GameUI.Label(d, "HaveLabel", "", 32, TextAnchor.MiddleLeft, C, new Vector2(-190, -38), new Vector2(180, 44));
        haveLabel.color = Soft;
        Loc.Bind(haveLabel, () => Loc.T("exchange_have"));
        _haveStar = GameUI.Img(d, "HaveStar", CatSprites.ShardIcon(), Color.white, C, new Vector2(10, -38), new Vector2(44, 44));
        _have = GameUI.Label(d, "Have", "", 44, TextAnchor.MiddleLeft, C, new Vector2(42, -38), new Vector2(200, 56));
        _have.color = Ink;

        _status = GameUI.Label(d, "Status", "", 30, TextAnchor.MiddleCenter, C, new Vector2(0, -105), new Vector2(940, 44));

        _btn = GameUI.Button(d, "Exchange", "", 52, C, new Vector2(0, -190), new Vector2(560, 110), Green);
        _btnBg = (Image)_btn.targetGraphic;
        _btnLabel = _btn.GetComponentInChildren<Text>();
        Loc.Bind(_btn, () => Loc.T("exchange"));
        _btn.onClick.AddListener(OnExchange);
    }

    // ------------------------------------------------------------------ content

    void Flip(int d)
    {
        int n = Cosmetics.Cats.Count;
        _page = (_page + d + n) % n;
        _selected = FirstToShow(Cosmetics.Cats[_page]);
        _flashT = 99f;
        AudioManager.Instance.Click();
        Refresh();
    }

    void Select(int index)
    {
        _selected = index;
        _flashT = 99f;
        AudioManager.Instance.Click();
        Refresh();
    }

    void Refresh()
    {
        var theme = Cosmetics.Cats[_page];
        _themeName.text = theme.Name;
        var progress = SetCompletion.Progress(theme);
        _setProgress.text = SkillText.SetProgress(theme, progress.Owned, progress.Total) + "      " + (_page + 1) + " / " + Cosmetics.Cats.Count;
        _chipText.text = PlayerData.Shards.ToString();

        for (int i = 0; i < Levels; i++)
        {
            var item = Cosmetics.CatItemOf(theme, i);
            bool owned = Cosmetics.IsOwned(item);
            var rc = Cosmetics.RarityColor(item.Rarity);
            bool picked = i == _selected;                            // the picked cell: dark, thicker border (rarity colours include orange)
            _borders[i].color = picked ? Ink : (owned ? Brown : rc);
            _borders[i].rectTransform.sizeDelta = new Vector2(CellW + (picked ? 20f : 10f), CellH + (picked ? 20f : 10f));
            _cards[i].color = owned ? new Color(0.93f, 0.88f, 0.8f) : Cream;
            _names[i].text = item.Name;
            _stars[i].gameObject.SetActive(!owned);
            _prices[i].text = owned ? Loc.T("owned") : Cosmetics.ShardPrice(item).ToString();
            _prices[i].color = owned ? Green : (PlayerData.Shards >= Cosmetics.ShardPrice(item) ? Ink : Red);
            _prices[i].rectTransform.anchoredPosition = new Vector2(owned ? 0 : 10, -82);
            ShowCat(_thumbs[i], i, theme.Index, owned ? new Color(1f, 1f, 1f, 0.55f) : Color.white);
        }
        RefreshDetail();
    }

    void RefreshDetail()
    {
        var cat = Selected;
        var theme = cat.Theme;
        bool owned = Cosmetics.IsOwned(cat);
        int price = Cosmetics.ShardPrice(cat), have = PlayerData.Shards;
        bool majesty = cat.Level >= CatDatabase.MaxLevel;

        ShowCat(_bigCat, cat.Level, theme.Index, Color.white);
        _name.text = "#" + (cat.Level + 1) + "  " + cat.Name;
        _themeInfo.text = Loc.T("exchange_theme") + ": " + theme.Name;
        _rarity.text = CosmeticUI.RarityName(cat.Rarity) + (majesty ? "  -  " + Loc.T("tier_majesty") : "");
        _rarity.color = Cosmetics.RarityColor(cat.Rarity);
        _cost.text = price.ToString();
        _have.text = have.ToString();
        _have.color = !owned && have < price ? Red : Ink;

        bool can = !owned && have >= price;
        _btn.interactable = can;
        _btnBg.color = can ? Green : Gray;
        if (_flashT < 2.2f) return;                           // a fresh result message is showing: Update restores the status later
        SetStatus(owned, have, price);
    }

    void SetStatus(bool owned, int have, int price)
    {
        if (owned) { _status.text = Loc.T("exchange_owned"); _status.color = Green; }
        else if (have < price) { _status.text = Loc.T("exchange_need").Replace("{0}", (price - have).ToString()); _status.color = Red; }
        else { _status.text = Loc.T("exchange_hint"); _status.color = Soft; }
    }

    void Flash(string text, Color color)
    {
        _status.text = text;
        _status.color = color;
        _flashT = 0f;
    }

    void OnExchange()
    {
        var cat = Selected;
        var result = Cosmetics.Exchange(cat, out var completed);
        switch (result)
        {
            case ExchangeResult.Success:
                AudioManager.Instance.Discover();
                Haptics.Medium();
                _flashT = 99f;
                Refresh();
                Flash(Loc.T("exchange_done"), Green);
                _onChanged?.Invoke();
                if (completed != null) _complete.Show(completed);      // once: only the exchange that finished the set returns it
                return;
            case ExchangeResult.NotEnoughShards:
                Haptics.Light();
                _flashT = 99f;
                Refresh();
                return;
            case ExchangeResult.AlreadyOwned:
                Haptics.Light();
                _flashT = 99f;
                Refresh();
                return;
            default:
                Haptics.Light();
                Refresh();
                Flash(Loc.T("exchange_fail"), Red);
                return;
        }
    }

    // ------------------------------------------------------------------ per frame

    void Update()
    {
        if (_work.Count > 0) _work.Dequeue()();
        if (!_complete.IsOpen)
        {
            int swipe = Swipe.Poll();
            if (swipe != 0) Flip(swipe);
        }
        if (_flashT < 2.2f)
        {
            _flashT += Time.unscaledDeltaTime;
            if (_flashT >= 2.2f) RefreshDetail();
        }
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
