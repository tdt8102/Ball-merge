using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cat Codex: a full-screen grid of all cats. Cats you have not seen yet are black silhouettes with the coin reward
/// for discovering them; discovered ones show their name and how many times they have appeared.
/// </summary>
public class CodexScreen
{
    static readonly Color Brown = new Color(0.55f, 0.36f, 0.24f);
    static readonly Color Ink = new Color(0.36f, 0.22f, 0.15f);
    static readonly Color Soft = new Color(0.5f, 0.36f, 0.28f);
    static readonly Color Gold = new Color(0.9f, 0.6f, 0.1f);

    const int Levels = CatDatabase.MaxLevel + 1;

    public GameObject Root { get; private set; }
    public bool IsOpen => Root.activeSelf;

    Text _progress;
    RectTransform _barFill;
    float _barWidth;
    readonly Image[] _icons = new Image[Levels];
    readonly Text[] _names = new Text[Levels];
    readonly Text[] _subs = new Text[Levels];
    readonly Image[] _cards = new Image[Levels];
    readonly Image[] _coins = new Image[Levels];

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

    void Build(RectTransform parent)
    {
        var top = new Vector2(0.5f, 1f);

        Root = new GameObject("CodexScreen", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)Root.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        Root.GetComponent<Image>().color = new Color(0.98f, 0.93f, 0.84f, 1f);       // opaque: covers the tab bar too

        var title = GameUI.Label(rt, "Title", "CAT CODEX", 100, TextAnchor.MiddleCenter, top, new Vector2(0, -120), new Vector2(1000, 130));
        title.color = Ink;
        Loc.Bind(title, () => Loc.T("codex"));
        var outline = title.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(4, -4);

        _progress = GameUI.Label(rt, "Progress", "", 44, TextAnchor.MiddleCenter, top, new Vector2(0, -220), new Vector2(900, 60));
        _progress.color = Soft;

        _barWidth = 640f;
        var track = GameUI.RoundImg(rt, "BarTrack", new Color(0.85f, 0.75f, 0.6f), top, new Vector2(0, -285), new Vector2(_barWidth, 26), 13f);
        var fill = GameUI.RoundImg((RectTransform)track.transform, "BarFill", new Color(0.95f, 0.6f, 0.2f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 26), 13f);
        _barFill = fill.rectTransform;
        _barFill.pivot = new Vector2(0, 0.5f);

        // 3 columns; the last row (2 cats) is centred
        const float w = 310f, h = 330f, gapY = 22f, firstRowY = -350f - h * 0.5f;
        float[] colX = { -340f, 0f, 340f };
        for (int i = 0; i < Levels; i++)
        {
            int row = i / 3, col = i % 3;
            float x = row == 3 ? (col == 0 ? -170f : 170f) : colX[col];
            float y = firstRowY - row * (h + gapY);
            BuildCard(rt, i, top, new Vector2(x, y), w, h);
        }

        var back = GameUI.Button(rt, "Back", "", 50, new Vector2(0.5f, 0), new Vector2(0, 95), new Vector2(460, 100), Brown);
        back.onClick.AddListener(() => { AudioManager.Instance.Click(); Close(); });
        Loc.Bind(back, () => Loc.T("back"));

        Root.SetActive(false);
    }

    void BuildCard(RectTransform parent, int level, Vector2 anchor, Vector2 pos, float w, float h)
    {
        GameUI.RoundImg(parent, "CardBorder" + level, Brown, anchor, pos, new Vector2(w + 8, h + 8), 40f);
        var card = GameUI.RoundImg(parent, "Card" + level, new Color(1f, 0.97f, 0.9f), anchor, pos, new Vector2(w, h), 36f);
        _cards[level] = card;
        var cr = (RectTransform)card.transform;
        var c = new Vector2(0.5f, 0.5f);

        _icons[level] = GameUI.Img(cr, "Cat", CatSprites.Icon(level), Color.white, c, new Vector2(0, 50), new Vector2(210, 210));
        _names[level] = GameUI.Label(cr, "Name", "", 36, TextAnchor.MiddleCenter, c, new Vector2(0, -85), new Vector2(w - 20, 46));
        _names[level].color = Ink;
        _subs[level] = GameUI.Label(cr, "Sub", "", 28, TextAnchor.MiddleCenter, c, new Vector2(-14, -130), new Vector2(w - 20, 40));
        _subs[level].color = Soft;
        _coins[level] = GameUI.Img(cr, "Coin", CatSprites.CoinIcon(), Color.white, c, new Vector2(-46, -130), new Vector2(34, 34));
    }

    void Refresh()
    {
        int found = PlayerData.DiscoveredCount;
        _progress.text = Loc.T("discovered") + "  " + found + " / " + Levels;
        _barFill.sizeDelta = new Vector2(_barWidth * found / Levels, 26f);

        for (int i = 0; i < Levels; i++)
        {
            bool seen = PlayerData.IsDiscovered(i);
            _icons[i].color = seen ? Color.white : new Color(0.07f, 0.05f, 0.07f, 1f);     // black silhouette
            _cards[i].color = seen ? new Color(1f, 0.97f, 0.9f) : new Color(0.93f, 0.88f, 0.8f);
            _names[i].text = seen ? Loc.CatName(i) : "???";
            if (seen)
            {
                _subs[i].text = Loc.T("seen") + "  x" + PlayerData.Seen(i);
                _subs[i].rectTransform.anchoredPosition = new Vector2(0, -130);
                _coins[i].gameObject.SetActive(false);
            }
            else
            {
                int reward = CatDatabase.DiscoveryReward(i);
                bool starter = reward <= 0;
                _subs[i].text = starter ? Loc.T("starter") : "+" + reward;
                _subs[i].color = starter ? Soft : Gold;
                _subs[i].rectTransform.anchoredPosition = new Vector2(starter ? 0 : 14, -130);
                _coins[i].gameObject.SetActive(!starter);
            }
            if (seen) _subs[i].color = Soft;
        }
    }
}
