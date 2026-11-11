using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Skin tab. CATS: flip between the themes (arrows or swipe) and see each of the 11 cats of a theme on its own; tap one you own
/// to wear it in its place (every cat can come from a different theme). BACKGROUND and FRAME: pick one.
/// A small picture of the playfield at the top shows the result. Things you do not own yet are silhouettes: win them in the Gacha.
/// </summary>
public class SkinPage : MonoBehaviour
{
    static readonly Color Brown = new Color(0.55f, 0.36f, 0.24f);
    static readonly Color Ink = new Color(0.36f, 0.22f, 0.15f);
    static readonly Color Soft = new Color(0.5f, 0.36f, 0.28f);
    static readonly Color Orange = new Color(0.95f, 0.6f, 0.2f);
    static readonly Color Green = new Color(0.2f, 0.65f, 0.4f);
    static readonly Color Cream = new Color(1f, 0.97f, 0.9f);
    static readonly Color Gray = new Color(0.72f, 0.66f, 0.6f);
    static readonly Vector2 Top = new Vector2(0.5f, 1f);
    static readonly string[] TabKeys = { "skin_cats", "skin_bg", "skin_frame" };

    const int Levels = CatDatabase.MaxLevel + 1;
    const int MaxTiles = Levels;
    const float ColStep = 249f;

    enum Tab { Cats, Backgrounds, Frames }

    sealed class Tile
    {
        public Image Border, Card, Thumb;
        public Text Num, Name, State;
        public Item Item;
        public GameObject Root;
    }

    Text _coinText;
    int _lastCoins = -1;
    readonly Image[] _chips = new Image[3];
    Image _tray;
    readonly Image[] _previewCats = new Image[3];
    GameObject _catGroup;
    Text _themeName, _themeSub;
    Button _equipSet;
    Image _equipSetBg;
    Text _equipSetLabel;
    readonly Image[] _slotBorders = new Image[Levels];
    readonly Image[] _slotIcons = new Image[Levels];
    bool _msgInSub;
    int _slot = -1;                                              // -1: browse by theme; 0..10: choose the theme worn by that cat number
    readonly Tile[] _tiles = new Tile[MaxTiles];
    Text _msg;
    float _msgT = 99f;
    Tab _tab = Tab.Cats;
    int _theme;                                                  // theme being shown on the CATS tab

    // Drawing a cat takes a few milliseconds; do one per frame instead of all at once when the page opens.
    readonly Queue<Action> _work = new Queue<Action>();
    readonly Dictionary<Image, int> _wanted = new Dictionary<Image, int>();

    public void Build(RectTransform page)
    {
        var title = GameUI.Label(page, "Title", "", 100, TextAnchor.MiddleCenter, Top, new Vector2(0, -190), new Vector2(1000, 140));
        title.color = Ink;
        var o = title.gameObject.AddComponent<Outline>();
        o.effectColor = Color.white;
        o.effectDistance = new Vector2(4, -4);
        Loc.Bind(title, () => Loc.T("tab_skin"));
        _coinText = GameUI.CoinChip(page, new Vector2(0, 1), new Vector2(169, -95), out _);

        for (int i = 0; i < 3; i++)
        {
            string key = TabKeys[i];
            var b = GameUI.Button(page, "Chip" + i, "", 36, Top, new Vector2((i - 1) * 330f, -320f), new Vector2(310, 92), Brown);
            Loc.Bind(b, () => Loc.T(key));
            _chips[i] = (Image)b.targetGraphic;
            var tab = (Tab)i;
            b.onClick.AddListener(() => { AudioManager.Instance.Click(); _tab = tab; Refresh(); });
        }

        BuildPreview(page);
        BuildThemePager(page);
        for (int i = 0; i < MaxTiles; i++) _tiles[i] = BuildTile(page, i);

        _msg = GameUI.Label(page, "Message", "", 34, TextAnchor.MiddleCenter, Top, new Vector2(0, -1712), new Vector2(1000, 44));
        _msg.color = Orange;
        _msg.gameObject.SetActive(false);

        Loc.Changed += OnLanguage;
    }

    void OnDestroy() => Loc.Changed -= OnLanguage;

    void OnLanguage()
    {
        if (_tiles[0] != null && gameObject.activeInHierarchy) Refresh();
    }

    // ------------------------------------------------------------------ layout

    static readonly Vector2 PreviewCenter = new Vector2(0, -560);
    static readonly int[] PreviewLevels = { 5, 3, 1 };
    static readonly Vector2[] PreviewPos = { new Vector2(-42, -67), new Vector2(46, -77), new Vector2(0, -10) };
    static readonly float[] PreviewSize = { 90, 72, 48 };

    void BuildPreview(RectTransform page)
    {
        GameUI.RoundImg(page, "PreviewBorder", Brown, Top, PreviewCenter, new Vector2(312, 312), 46f);
        _tray = GameUI.Img(page, "Tray", CatSprites.Tray(Cosmetics.Frame, Cosmetics.Bg), Color.white, Top, PreviewCenter, new Vector2(300, 300));
        // three cats resting in the tray: sizes are head diameter * 1.2 (the sprite is a bit larger than the head)
        for (int i = 0; i < 3; i++)
            _previewCats[i] = GameUI.Img(page, "PreviewCat" + i, null, Color.white, Top, PreviewCenter, new Vector2(100, 100));
    }

    void BuildThemePager(RectTransform page)
    {
        _catGroup = new GameObject("CatGroup", typeof(RectTransform));
        var g = (RectTransform)_catGroup.transform;
        g.SetParent(page, false);
        g.anchorMin = Vector2.zero; g.anchorMax = Vector2.one; g.offsetMin = g.offsetMax = Vector2.zero;   // same rect as the page, so the top anchors below work

        // one cell per cat number showing what it wears now; tap one to choose its theme
        for (int i = 0; i < Levels; i++)
        {
            var pos = new Vector2((i - 5) * 96f, -785f);
            _slotBorders[i] = GameUI.RoundImg(g, "SlotBorder" + i, Brown, Top, pos, new Vector2(92, 100), 22f);
            var cell = GameUI.RoundImg(g, "Slot" + i, Cream, Top, pos, new Vector2(86, 94), 18f);
            cell.raycastTarget = true;
            int slot = i;
            cell.gameObject.AddComponent<Button>().onClick.AddListener(() => OnSlot(slot));
            _slotIcons[i] = GameUI.Img((RectTransform)cell.transform, "Icon", null, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, -2), new Vector2(76, 76));
        }

        var left = GameUI.Button(g, "Prev", "<", 56, Top, new Vector2(-390, -895), new Vector2(110, 96), Brown);
        left.onClick.AddListener(() => ChangeTheme(-1));
        var right = GameUI.Button(g, "Next", ">", 56, Top, new Vector2(390, -895), new Vector2(110, 96), Brown);
        right.onClick.AddListener(() => ChangeTheme(1));
        _themeName = GameUI.Label(g, "Theme", "", 50, TextAnchor.MiddleCenter, Top, new Vector2(0, -880), new Vector2(560, 66));
        _themeName.color = Ink;
        _themeSub = GameUI.Label(g, "ThemeSub", "", 28, TextAnchor.MiddleCenter, Top, new Vector2(0, -925), new Vector2(660, 40));
        _themeSub.color = Soft;

        _equipSet = GameUI.Button(g, "EquipSet", "", 36, Top, new Vector2(0, -1685), new Vector2(600, 80), Green);
        _equipSetBg = (Image)_equipSet.targetGraphic;
        _equipSetLabel = _equipSet.GetComponentInChildren<Text>();
        _equipSet.onClick.AddListener(OnEquipSet);
    }

    Tile BuildTile(RectTransform page, int index)
    {
        var t = new Tile();
        t.Border = GameUI.RoundImg(page, "TileBorder" + index, Brown, Top, Vector2.zero, new Vector2(243, 223), 36f);
        t.Card = GameUI.RoundImg(page, "Tile" + index, Cream, Top, Vector2.zero, new Vector2(235, 215), 32f);
        t.Card.raycastTarget = true;
        t.Root = t.Border.gameObject;
        var cr = (RectTransform)t.Card.transform;
        var c = new Vector2(0.5f, 0.5f);
        t.Thumb = GameUI.Img(cr, "Thumb", null, Color.white, c, Vector2.zero, new Vector2(150, 150));
        t.Thumb.preserveAspect = true;
        t.Num = GameUI.Label(cr, "Num", "", 24, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(12, -6), new Vector2(60, 30));
        t.Num.color = Soft;
        t.Name = GameUI.Label(cr, "Name", "", 26, TextAnchor.MiddleCenter, c, Vector2.zero, new Vector2(229, 38));
        t.Name.color = Ink;
        t.State = GameUI.Label(cr, "State", "", 24, TextAnchor.MiddleCenter, c, Vector2.zero, new Vector2(229, 34));
        int i = index;
        t.Card.gameObject.AddComponent<Button>().onClick.AddListener(() => OnTile(i));
        return t;
    }

    // ------------------------------------------------------------------ content

    /// <summary>Called whenever the tab is shown, and after every change.</summary>
    public void Refresh()
    {
        if (_tiles[0] == null) return;
        for (int i = 0; i < 3; i++) _chips[i].color = i == (int)_tab ? Orange : Brown;
        _catGroup.SetActive(_tab == Tab.Cats);
        RefreshPreview();
        if (_tab == Tab.Cats) RefreshCats(); else RefreshOthers();
    }

    void RefreshPreview()
    {
        _tray.sprite = CatSprites.Tray(Cosmetics.Frame, Cosmetics.Bg);
        for (int i = 0; i < 3; i++)
        {
            var img = _previewCats[i];
            var rt = img.rectTransform;
            rt.anchoredPosition = PreviewCenter + PreviewPos[i];
            rt.sizeDelta = new Vector2(PreviewSize[i], PreviewSize[i]);
            ShowCat(img, PreviewLevels[i], Cosmetics.CatIndex(PreviewLevels[i]), Color.white);
        }
    }

    void RefreshCats()
    {
        RefreshSlotRow();
        if (_slot >= 0) RefreshSlotView(); else RefreshThemeView();
    }

    /// <summary>The 11 cells at the top: what each cat number wears now; the one being edited is outlined.</summary>
    void RefreshSlotRow()
    {
        for (int i = 0; i < Levels; i++)
        {
            _slotBorders[i].color = i == _slot ? Orange : Brown;
            ShowCat(_slotIcons[i], i, Cosmetics.CatIndex(i), Color.white);
        }
    }

    /// <summary>Browse one theme: its 11 cats, tap one to wear it in its own place.</summary>
    void RefreshThemeView()
    {
        var theme = Cosmetics.Cats[_theme];
        int owned = Cosmetics.OwnedInTheme(theme);
        _themeName.text = theme.Name;
        _themeSub.text = owned + " / " + Levels + "     " + (_theme + 1) + " / " + Cosmetics.Cats.Count;
        _themeSub.color = Soft;
        _equipSetLabel.text = Loc.T("equip_set");
        _equipSetBg.color = owned > 0 ? Green : Gray;

        for (int i = 0; i < MaxTiles; i++)
        {
            var t = _tiles[i];
            var item = Cosmetics.CatItemOf(theme, i);
            t.Root.SetActive(true);
            t.Card.gameObject.SetActive(true);
            t.Item = item;

            int inRow = Mathf.Min(4, Levels - (i / 4) * 4);
            Place(t, new Vector2((i % 4 - (inRow - 1) * 0.5f) * ColStep, -1065f - (i / 4) * 232f), 235f, 215f, 118f, 34f, -62f, -92f);
            bool owns = Cosmetics.IsOwned(item);
            bool worn = owns && Cosmetics.IsEquipped(item);
            Style(t, item, owns, worn);
            t.Num.text = "#" + (i + 1);
            ShowCat(t.Thumb, i, theme.Index, owns ? Color.white : CosmeticUI.Locked);
        }
    }

    /// <summary>Edit one cat number: the same cat in every theme, tap one to wear it. Mix themes freely, e.g. cat 1 Pixel, cat 2 Candy.</summary>
    void RefreshSlotView()
    {
        _themeName.text = "#" + (_slot + 1) + "  " + Loc.CatName(_slot);
        _themeSub.text = Loc.T("cat_slot").Replace("{0}", (_slot + 1).ToString());
        _themeSub.color = Soft;
        _equipSetLabel.text = Loc.T("by_theme");
        _equipSetBg.color = Brown;

        for (int i = 0; i < MaxTiles; i++)
        {
            var t = _tiles[i];
            bool used = i < Cosmetics.Cats.Count;
            t.Root.SetActive(used);
            t.Card.gameObject.SetActive(used);
            if (!used) { t.Item = null; continue; }

            var theme = Cosmetics.Cats[i];
            var item = Cosmetics.CatItemOf(theme, _slot);
            t.Item = item;
            Place(t, new Vector2((i % 4 - 1.5f) * ColStep, -1065f - (i / 4) * 232f), 235f, 215f, 118f, 34f, -62f, -92f);
            bool owns = Cosmetics.IsOwned(item);
            bool worn = owns && Cosmetics.IsEquipped(item);
            Style(t, item, owns, worn);
            t.Name.text = theme.Name;                                       // the cat is the same, so name the theme instead
            t.Num.text = "";
            ShowCat(t.Thumb, _slot, theme.Index, owns ? Color.white : CosmeticUI.Locked);
        }
    }

    void RefreshOthers()
    {
        var items = new List<Item>();
        if (_tab == Tab.Backgrounds) foreach (var b in Cosmetics.Backgrounds) items.Add(b);
        else foreach (var f in Cosmetics.Frames) items.Add(f);

        for (int i = 0; i < MaxTiles; i++)
        {
            var t = _tiles[i];
            bool used = i < items.Count;
            t.Root.SetActive(used);
            t.Card.gameObject.SetActive(used);
            if (!used) { t.Item = null; continue; }

            var item = items[i];
            t.Item = item;
            Place(t, new Vector2((i % 4 - 1.5f) * ColStep, -1010f - (i / 4) * 255f), 235f, 235f, 150f, 36f, -64f, -98f);
            bool owns = Cosmetics.IsOwned(item);
            bool worn = owns && Cosmetics.IsEquipped(item);
            Style(t, item, owns, worn);
            t.Num.text = "";
            t.Thumb.sprite = CosmeticUI.Thumb(item);
            t.Thumb.color = owns ? Color.white : CosmeticUI.Locked;
        }
    }

    static void Place(Tile t, Vector2 pos, float w, float h, float thumb, float thumbY, float nameY, float stateY)
    {
        t.Border.rectTransform.anchoredPosition = t.Card.rectTransform.anchoredPosition = pos;
        t.Border.rectTransform.sizeDelta = new Vector2(w + 8, h + 8);
        t.Card.rectTransform.sizeDelta = new Vector2(w, h);
        t.Thumb.rectTransform.sizeDelta = new Vector2(thumb, thumb);
        t.Thumb.rectTransform.anchoredPosition = new Vector2(0, thumbY);
        t.Name.rectTransform.anchoredPosition = new Vector2(0, nameY);
        t.State.rectTransform.anchoredPosition = new Vector2(0, stateY);
    }

    static void Style(Tile t, Item item, bool owned, bool worn)
    {
        var rc = Cosmetics.RarityColor(item.Rarity);
        t.Border.color = worn ? Green : (owned ? rc : Color.Lerp(rc, Color.white, 0.55f));
        t.Card.color = owned ? Cream : new Color(0.93f, 0.88f, 0.8f);
        t.Name.text = owned ? item.Name : "???";
        t.State.text = worn ? Loc.T("equipped") : CosmeticUI.RarityName(item.Rarity);
        t.State.color = worn ? Green : rc;
    }

    /// <summary>Show a cat icon; if it still has to be drawn, hide the image now and fill it in on a later frame.</summary>
    void ShowCat(Image img, int level, int style, Color shown)
    {
        int key = level * 100 + style;
        _wanted[img] = key;
        if (CatSprites.IsReady(level, style)) { img.sprite = CatSprites.Icon(level, style); img.color = shown; return; }
        img.color = Color.clear;
        _work.Enqueue(() =>
        {
            if (img == null || !_wanted.TryGetValue(img, out int now) || now != key) return;     // something newer was asked for
            img.sprite = CatSprites.Icon(level, style);
            img.color = shown;
        });
    }

    // ------------------------------------------------------------------ actions

    /// <summary>Arrows and swipes: next/previous theme, or next/previous cat number when one is being edited.</summary>
    void ChangeTheme(int d)
    {
        if (_slot >= 0) _slot = (_slot + d + Levels) % Levels;
        else _theme = (_theme + d + Cosmetics.Cats.Count) % Cosmetics.Cats.Count;
        AudioManager.Instance.Click();
        Refresh();
    }

    void OnSlot(int slot)
    {
        _slot = _slot == slot ? -1 : slot;                                  // tapping the open one goes back to the theme view
        AudioManager.Instance.Click();
        Refresh();
    }

    void OnTile(int index)
    {
        var item = _tiles[index].Item;
        if (item == null) return;
        if (!Cosmetics.IsOwned(item))
        {
            AudioManager.Instance.Click();
            Haptics.Light();
            ShowMessage(Loc.T("locked"));
            return;
        }
        if (Cosmetics.IsEquipped(item)) { AudioManager.Instance.Click(); return; }
        Cosmetics.Equip(item);
        AudioManager.Instance.Discover();
        Haptics.Light();
        Refresh();
    }

    void OnEquipSet()
    {
        if (_slot >= 0) { _slot = -1; AudioManager.Instance.Click(); Refresh(); return; }      // in cat view this button goes back to themes
        var theme = Cosmetics.Cats[_theme];
        if (Cosmetics.EquipTheme(theme) == 0)
        {
            AudioManager.Instance.Click();
            ShowMessage(Loc.T("locked"));
            return;
        }
        AudioManager.Instance.Discover();
        Haptics.Light();
        Refresh();
    }

    void ShowMessage(string text)
    {
        _msgT = 0f;
        if (_tab == Tab.Cats)                                      // no free line on this tab: borrow the one under the theme name
        {
            _themeSub.text = text;
            _themeSub.color = Orange;
            _msgInSub = true;
            return;
        }
        _msg.text = text;
        _msg.gameObject.SetActive(true);
    }

    void Update()
    {
        if (_work.Count > 0) _work.Dequeue()();

        int swipe = Swipe.Poll();
        if (swipe != 0 && _tab == Tab.Cats) ChangeTheme(swipe);

        if (_msgInSub)
        {
            _msgT += Time.unscaledDeltaTime;
            if (_msgT >= 2f) { _msgInSub = false; if (_tab == Tab.Cats) Refresh(); }
        }

        if (_msgT < 2f && !_msgInSub)
        {
            _msgT += Time.unscaledDeltaTime;
            var c = _msg.color;
            c.a = _msgT < 1.4f ? 1f : Mathf.Clamp01(1f - (_msgT - 1.4f) / 0.6f);
            _msg.color = c;
            if (_msgT >= 2f) _msg.gameObject.SetActive(false);
        }

        if (_lastCoins != PlayerData.Coins)
        {
            _lastCoins = PlayerData.Coins;
            _coinText.text = _lastCoins.ToString();
        }
    }
}
