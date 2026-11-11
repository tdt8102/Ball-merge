using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gacha tab: spend coins to open a gift box and win a collectible (a cat style, a background or a tray frame; Common, Rare or Legendary).
/// One pull gives one item; ten pulls give ten (at least one Rare or better). Items you already own give some coins back.
/// The opening is a full-screen overlay: the box shakes, bursts open and shows the prize.
/// </summary>
public class GachaPage : MonoBehaviour
{
    static readonly Color Brown = new Color(0.55f, 0.36f, 0.24f);
    static readonly Color Ink = new Color(0.36f, 0.22f, 0.15f);
    static readonly Color Soft = new Color(0.5f, 0.36f, 0.28f);
    static readonly Color Orange = new Color(0.95f, 0.6f, 0.2f);
    static readonly Color Green = new Color(0.2f, 0.65f, 0.4f);
    static readonly Color Gold = new Color(0.9f, 0.6f, 0.1f);
    static readonly Color Cream = new Color(1f, 0.97f, 0.9f);
    static readonly Color Gray = new Color(0.72f, 0.66f, 0.6f);
    static readonly Vector2 Top = new Vector2(0.5f, 1f);
    static readonly Vector2 C = new Vector2(0.5f, 0.5f);

    const float BarWidth = 640f;
    const int MaxParticles = 80;
    const int GridSize = 10;

    // ---- the tab itself
    Text _coinText, _collection, _pity, _rates, _msg;
    int _lastCoins = -1;
    RectTransform _boxRoot, _barFill;
    Image _glow;
    readonly Image[] _pullBg = new Image[2];
    readonly Text[] _cost = new Text[2];
    readonly int[] _counts = { 1, 10 };
    float _msgT = 99f;

    // ---- the opening overlay
    public GameObject Overlay { get; private set; }
    public bool IsOpen => Overlay != null && Overlay.activeSelf;
    RectTransform _ovBox, _ovLid, _cardRoot, _gridRoot;
    Image _ovBoxImg, _ovLidImg, _ovBurst, _ovGlow, _flash, _cardBorder, _cardThumb;
    Text _cardKind, _cardName, _cardRarity, _cardStatus, _hint;
    Button _btnEquip, _btnOk;
    Text _equipLabel;
    readonly Image[] _miniBorder = new Image[GridSize], _miniThumb = new Image[GridSize], _miniCoin = new Image[GridSize];
    readonly Text[] _miniName = new Text[GridSize], _miniTag = new Text[GridSize];
    readonly GameObject[] _mini = new GameObject[GridSize];
    bool _busy, _tapped, _skip;
    Image _beam, _gapLight, _cardCoin;
    Text _cardAmount;
    Button _btnSkip;
    bool _blinkSingle;                              // the single prize is a duplicate: it blinks between the item and the coins
    readonly Image[] _miniBig = new Image[GridSize];
    readonly bool[] _miniDup = new bool[GridSize];
    Item _prize;                                    // the single prize currently shown (for the Equip button)

    // confetti
    readonly RectTransform[] _pr = new RectTransform[MaxParticles];
    readonly Image[] _pi = new Image[MaxParticles];
    readonly Vector2[] _pv = new Vector2[MaxParticles];
    readonly float[] _plife = new float[MaxParticles], _pspin = new float[MaxParticles];

    public void Build(RectTransform page, RectTransform overlayParent)
    {
        var title = GameUI.Label(page, "Title", "", 100, TextAnchor.MiddleCenter, Top, new Vector2(0, -190), new Vector2(1000, 140));
        title.color = Ink;
        var o = title.gameObject.AddComponent<Outline>();
        o.effectColor = Color.white;
        o.effectDistance = new Vector2(4, -4);
        Loc.Bind(title, () => Loc.T("tab_gacha"));
        _coinText = GameUI.CoinChip(page, new Vector2(0, 1), new Vector2(169, -95), out _);

        // the gift box (bobs gently) on a warm glow
        _glow = GameUI.Img(page, "Glow", CatSprites.Glow(), new Color(1f, 0.85f, 0.5f, 0.8f), Top, new Vector2(0, -650), new Vector2(700, 700));
        var boxGo = new GameObject("Box", typeof(RectTransform));
        _boxRoot = (RectTransform)boxGo.transform;
        _boxRoot.SetParent(page, false);
        _boxRoot.anchorMin = _boxRoot.anchorMax = Top;
        _boxRoot.anchoredPosition = new Vector2(0, -650);
        _boxRoot.sizeDelta = new Vector2(460, 460);
        GameUI.Img(_boxRoot, "Body", CatSprites.GiftBox(), Color.white, C, Vector2.zero, new Vector2(460, 460));
        GameUI.Img(_boxRoot, "Lid", CatSprites.GiftLid(), Color.white, C, Vector2.zero, new Vector2(460, 460));

        var hint = GameUI.Label(page, "Hint", "", 40, TextAnchor.MiddleCenter, Top, new Vector2(0, -945), new Vector2(1040, 60));
        hint.color = Soft;
        Loc.Bind(hint, () => Loc.T("gacha_hint"));
        _rates = GameUI.Label(page, "Rates", "", 32, TextAnchor.MiddleCenter, Top, new Vector2(0, -1005), new Vector2(1040, 46));
        _rates.color = Soft;

        BuildPullButton(page, 0, new Vector2(-255, -1160), Orange);
        BuildPullButton(page, 1, new Vector2(255, -1160), Green);

        var ten = GameUI.Label(page, "TenBonus", "", 30, TextAnchor.MiddleCenter, Top, new Vector2(0, -1285), new Vector2(1040, 44));
        ten.color = Soft;
        Loc.Bind(ten, () => Loc.T("ten_bonus"));

        _collection = GameUI.Label(page, "Collection", "", 42, TextAnchor.MiddleCenter, Top, new Vector2(0, -1370), new Vector2(1000, 60));
        _collection.color = Ink;
        var track = GameUI.RoundImg(page, "BarTrack", new Color(0.85f, 0.75f, 0.6f), Top, new Vector2(0, -1425), new Vector2(BarWidth, 26), 13f);
        var fill = GameUI.RoundImg((RectTransform)track.transform, "BarFill", Orange, new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 26), 13f);
        _barFill = fill.rectTransform;
        _barFill.pivot = new Vector2(0, 0.5f);
        _pity = GameUI.Label(page, "Pity", "", 32, TextAnchor.MiddleCenter, Top, new Vector2(0, -1490), new Vector2(1040, 46));
        _pity.color = Gold;

        _msg = GameUI.Label(page, "Message", "", 44, TextAnchor.MiddleCenter, Top, new Vector2(0, -1610), new Vector2(1000, 60));
        _msg.color = new Color(0.85f, 0.25f, 0.2f);
        _msg.gameObject.SetActive(false);

        BuildOverlay(overlayParent);
        Loc.Changed += OnLanguage;
    }

    void OnDestroy() => Loc.Changed -= OnLanguage;

    void OnLanguage()
    {
        if (_collection != null) Refresh();
    }

    void BuildPullButton(RectTransform page, int idx, Vector2 pos, Color color)
    {
        int count = _counts[idx];
        var b = GameUI.Button(page, "Pull" + count, "", 40, Top, pos, new Vector2(470, 170), color);
        _pullBg[idx] = (Image)b.targetGraphic;
        var rt = (RectTransform)b.transform;
        var label = GameUI.Label(rt, "Label", "", 52, TextAnchor.MiddleCenter, C, new Vector2(0, 34), new Vector2(450, 70));
        label.color = Color.white;
        Loc.Bind(label, () => Loc.T("pull") + " x" + count);
        GameUI.Img(rt, "Coin", CatSprites.CoinIcon(), Color.white, C, new Vector2(-78, -36), new Vector2(56, 56));
        _cost[idx] = GameUI.Label(rt, "Cost", "", 46, TextAnchor.MiddleLeft, C, new Vector2(-44, -36), new Vector2(240, 60));
        _cost[idx].color = Color.white;
        b.onClick.AddListener(() => OnPull(count));
    }

    // ------------------------------------------------------------------ the tab

    /// <summary>Called whenever the tab is shown and after the overlay closes.</summary>
    public void Refresh()
    {
        if (_collection == null) return;
        var b = Balance.Current;
        _rates.text = Loc.T("rates") + ":  " + Loc.T("rarity_common") + " " + (100 - b.gachaRateRare - b.gachaRateLegendary) + "%   " +
                      Loc.T("rarity_rare") + " " + b.gachaRateRare + "%   " + Loc.T("rarity_legendary") + " " + b.gachaRateLegendary + "%";
        Cosmetics.Collection(out int owned, out int total);
        _collection.text = Loc.T("collection") + "  " + owned + " / " + total;
        _barFill.sizeDelta = new Vector2(BarWidth * owned / Mathf.Max(1, total), 26f);
        int pity = Cosmetics.PullsToPity;
        _pity.text = pity > 0 ? Loc.T("pity").Replace("{0}", pity.ToString()) : "";
        for (int i = 0; i < 2; i++)
        {
            int cost = Cosmetics.PullCost(_counts[i]);
            _cost[i].text = cost.ToString();
            _pullBg[i].color = PlayerData.Coins >= cost ? (i == 0 ? Orange : Green) : Gray;
        }
        _coinText.text = PlayerData.Coins.ToString();
        _lastCoins = PlayerData.Coins;
    }

    void OnPull(int count)
    {
        if (_busy) return;
        AudioManager.Instance.Click();
        if (PlayerData.Coins < Cosmetics.PullCost(count))
        {
            Haptics.Light();
            _msg.text = Loc.T("need_coins");
            _msg.gameObject.SetActive(true);
            _msgT = 0f;
            return;
        }
        var results = Cosmetics.Pull(count);
        if (results == null) return;
        StartCoroutine(Play(results));
    }

    // ------------------------------------------------------------------ the overlay

    void BuildOverlay(RectTransform parent)
    {
        Overlay = new GameObject("GachaOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)Overlay.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        Overlay.GetComponent<Image>().color = new Color(0.1f, 0.06f, 0.12f, 0.9f);
        var tap = Overlay.GetComponent<Button>();
        tap.transition = Selectable.Transition.None;
        tap.onClick.AddListener(() => _tapped = true);

        _ovGlow = GameUI.Img(rt, "Glow", CatSprites.Glow(), Color.clear, C, new Vector2(0, 40), new Vector2(1000, 1000));
        _ovBurst = GameUI.Img(rt, "Burst", CatSprites.Burst(), Color.clear, C, new Vector2(0, 40), new Vector2(1500, 1500));

        // the box
        var boxGo = new GameObject("Box", typeof(RectTransform));
        _ovBox = (RectTransform)boxGo.transform;
        _ovBox.SetParent(rt, false);
        _ovBox.anchorMin = _ovBox.anchorMax = C;
        _ovBox.anchoredPosition = new Vector2(0, 20);
        _ovBox.sizeDelta = new Vector2(600, 600);
        _beam = GameUI.Img(_ovBox, "Beam", CatSprites.Glow(), Color.clear, C, new Vector2(0, 72), new Vector2(400, 400));
        _ovBoxImg = GameUI.Img(_ovBox, "Body", CatSprites.GiftBox(), Color.white, C, Vector2.zero, new Vector2(600, 600));
        _gapLight = GameUI.Img(_ovBox, "GapLight", CatSprites.Glow(), Color.clear, C, new Vector2(0, 72), new Vector2(420, 10));
        _ovLidImg = GameUI.Img(_ovBox, "Lid", CatSprites.GiftLid(), Color.white, C, Vector2.zero, new Vector2(600, 600));
        _ovLid = _ovLidImg.rectTransform;

        BuildCard(rt);
        BuildGrid(rt);

        _hint = GameUI.Label(rt, "Hint", "", 36, TextAnchor.MiddleCenter, C, new Vector2(0, -500), new Vector2(1000, 50));
        _hint.color = new Color(1f, 1f, 1f, 0.7f);
        Loc.Bind(_hint, () => Loc.T("drag_open"));

        _btnSkip = GameUI.Button(rt, "Skip", "", 40, C, new Vector2(0, -610), new Vector2(300, 88), Brown);
        Loc.Bind(_btnSkip, () => Loc.T("skip"));
        _btnSkip.onClick.AddListener(() => { _skip = true; _tapped = true; });

        _btnEquip = GameUI.Button(rt, "Equip", "", 46, C, new Vector2(-250, -520), new Vector2(440, 110), Green);
        _equipLabel = _btnEquip.GetComponentInChildren<Text>();
        _btnEquip.onClick.AddListener(OnEquip);
        _btnOk = GameUI.Button(rt, "Ok", "", 50, C, new Vector2(0, -520), new Vector2(440, 110), Brown);
        Loc.Bind(_btnOk, () => Loc.T("ok"));
        _btnOk.onClick.AddListener(Close);

        for (int i = 0; i < MaxParticles; i++)
        {
            _pi[i] = GameUI.Img(rt, "P" + i, SpriteFactory.Square, Color.white, C, Vector2.zero, new Vector2(20, 20));
            _pr[i] = _pi[i].rectTransform;
            _pi[i].gameObject.SetActive(false);
        }
        _flash = GameUI.Img(rt, "Flash", SpriteFactory.Square, Color.clear, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 4000));
        Overlay.SetActive(false);
    }

    void BuildCard(RectTransform parent)
    {
        var go = new GameObject("Card", typeof(RectTransform));
        _cardRoot = (RectTransform)go.transform;
        _cardRoot.SetParent(parent, false);
        _cardRoot.anchorMin = _cardRoot.anchorMax = C;
        _cardRoot.anchoredPosition = new Vector2(0, 80);
        _cardRoot.sizeDelta = new Vector2(640, 760);
        _cardBorder = GameUI.RoundImg(_cardRoot, "Border", Brown, C, Vector2.zero, new Vector2(640, 760), 52f);
        GameUI.RoundImg(_cardRoot, "Face", Cream, C, Vector2.zero, new Vector2(616, 736), 46f);
        _cardThumb = GameUI.Img(_cardRoot, "Thumb", null, Color.white, C, new Vector2(0, 160), new Vector2(380, 380));
        _cardThumb.preserveAspect = true;
        _cardCoin = GameUI.Img(_cardRoot, "BigCoin", CatSprites.CoinIcon(), Color.white, C, new Vector2(0, 160), new Vector2(300, 300));
        _cardAmount = GameUI.Label(_cardRoot, "Amount", "", 96, TextAnchor.MiddleCenter, C, new Vector2(0, 160), new Vector2(380, 130));
        _cardAmount.color = Color.white;
        var amountOutline = _cardAmount.gameObject.AddComponent<Outline>();
        amountOutline.effectColor = new Color(0.55f, 0.32f, 0.04f);
        amountOutline.effectDistance = new Vector2(5, -5);
        _cardCoin.gameObject.SetActive(false);
        _cardAmount.gameObject.SetActive(false);
        _cardKind = GameUI.Label(_cardRoot, "Kind", "", 32, TextAnchor.MiddleCenter, C, new Vector2(0, -75), new Vector2(600, 44));
        _cardKind.color = Soft;
        _cardName = GameUI.Label(_cardRoot, "Name", "", 62, TextAnchor.MiddleCenter, C, new Vector2(0, -140), new Vector2(600, 80));
        _cardName.color = Ink;
        _cardRarity = GameUI.Label(_cardRoot, "Rarity", "", 42, TextAnchor.MiddleCenter, C, new Vector2(0, -205), new Vector2(600, 56));
        _cardStatus = GameUI.Label(_cardRoot, "Status", "", 50, TextAnchor.MiddleCenter, C, new Vector2(0, -285), new Vector2(600, 70));
        _cardRoot.gameObject.SetActive(false);
    }

    void BuildGrid(RectTransform parent)
    {
        var go = new GameObject("Grid", typeof(RectTransform));
        _gridRoot = (RectTransform)go.transform;
        _gridRoot.SetParent(parent, false);
        _gridRoot.anchorMin = _gridRoot.anchorMax = C;
        _gridRoot.anchoredPosition = new Vector2(0, 40);
        _gridRoot.sizeDelta = new Vector2(1000, 600);
        for (int i = 0; i < GridSize; i++)
        {
            var pos = new Vector2((i % 5 - 2) * 202f, i < 5 ? 140f : -140f);
            _miniBorder[i] = GameUI.RoundImg(_gridRoot, "MiniBorder" + i, Brown, C, pos, new Vector2(194, 254), 34f);
            _mini[i] = _miniBorder[i].gameObject;
            var rt = _miniBorder[i].rectTransform;
            GameUI.RoundImg(rt, "Face", Cream, C, Vector2.zero, new Vector2(182, 242), 30f);
            _miniThumb[i] = GameUI.Img(rt, "Thumb", null, Color.white, C, new Vector2(0, 46), new Vector2(130, 130));
            _miniThumb[i].preserveAspect = true;
            _miniName[i] = GameUI.Label(rt, "Name", "", 22, TextAnchor.MiddleCenter, C, new Vector2(0, -50), new Vector2(186, 34));
            _miniName[i].color = Ink;
            _miniTag[i] = GameUI.Label(rt, "Tag", "", 28, TextAnchor.MiddleCenter, C, new Vector2(0, -90), new Vector2(180, 40));
            _miniCoin[i] = GameUI.Img(rt, "Coin", CatSprites.CoinIcon(), Color.white, C, new Vector2(-34, -90), new Vector2(32, 32));
            _miniBig[i] = GameUI.Img(rt, "BigCoin", CatSprites.CoinIcon(), Color.white, C, new Vector2(0, 46), new Vector2(110, 110));
            _miniBig[i].gameObject.SetActive(false);
            _mini[i].SetActive(false);
        }
        _gridRoot.gameObject.SetActive(false);
    }

    void ResetOverlay()
    {
        _cardRoot.gameObject.SetActive(false);
        _gridRoot.gameObject.SetActive(false);
        _btnEquip.gameObject.SetActive(false);
        _btnOk.gameObject.SetActive(false);
        _hint.gameObject.SetActive(false);
        _btnSkip.gameObject.SetActive(false);
        _blinkSingle = false;
        _cardCoin.gameObject.SetActive(false);
        _cardAmount.gameObject.SetActive(false);
        _beam.color = _gapLight.color = Color.clear;
        for (int i = 0; i < GridSize; i++) { _miniDup[i] = false; _miniBig[i].gameObject.SetActive(false); }
        _ovBox.gameObject.SetActive(true);
        _ovBox.localRotation = Quaternion.identity;
        _ovBox.localScale = Vector3.one;
        _ovBox.anchoredPosition = new Vector2(0, 20);
        _ovLid.anchoredPosition = Vector2.zero;
        _ovLid.localRotation = Quaternion.identity;
        _ovBoxImg.color = _ovLidImg.color = Color.white;
        _ovBurst.color = Color.clear;
        _ovGlow.color = Color.clear;
        _flash.color = Color.clear;
        for (int i = 0; i < MaxParticles; i++) { _plife[i] = 0f; _pi[i].gameObject.SetActive(false); }
    }

    /// <summary>
    /// The box drops in closed. The player can tap it to peek, or drag the lid up: a crack of light in the colour of the best prize
    /// (white-blue common, blue rare, gold legendary) shines out and grows with the gap. Pulling the lid all the way opens it.
    /// SKIP opens it at once.
    /// </summary>
    IEnumerator Play(List<PullResult> results)
    {
        _busy = true;
        _tapped = false;
        _skip = false;
        Overlay.SetActive(true);
        ResetOverlay();

        var best = Rarity.Common;
        foreach (var r in results) if (r.Item.Rarity > best) best = r.Item.Rarity;
        var light = Cosmetics.LightColor(best);
        var warm = new Queue<Item>();                  // the prizes' pictures are drawn one per frame while the player plays with the box
        foreach (var r in results) if (!CosmeticUI.ThumbReady(r.Item) && !warm.Contains(r.Item)) warm.Enqueue(r.Item);

        // 1. the closed box drops in
        _btnSkip.gameObject.SetActive(true);
        for (float a = 0f; a < 0.5f && !_skip; a += Time.unscaledDeltaTime)
        {
            float k = a / 0.5f, e = 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f);      // ease out with a little overshoot
            _ovBox.anchoredPosition = new Vector2(0, Mathf.LerpUnclamped(900f, 20f, e));
            if (warm.Count > 0) CosmeticUI.Thumb(warm.Dequeue());
            yield return null;
        }
        _ovBox.anchoredPosition = new Vector2(0, 20);
        _hint.gameObject.SetActive(true);

        // 2. peek and pull
        float lift = 0f, startY = 0f, dragMax = 0f, peekT = -1f, t = 0f;
        bool dragging = false, buzzed = false, opened = false;
        while (!opened && !_skip)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            if (warm.Count > 0) CosmeticUI.Thumb(warm.Dequeue());

            if (Input.GetMouseButtonDown(0) && Input.mousePosition.y > Screen.height * 0.25f)    // (not on the SKIP button)
            {
                dragging = true; startY = Input.mousePosition.y; dragMax = 0f; peekT = -1f; buzzed = false;
            }
            if (dragging)
            {
                float dy = Input.mousePosition.y - startY;
                dragMax = Mathf.Max(dragMax, Mathf.Abs(dy));
                lift = Mathf.MoveTowards(lift, Mathf.Clamp01(dy / (Screen.height * 0.16f)), 6f * dt);
                if (!buzzed && lift > 0.25f) { buzzed = true; if (best == Rarity.Legendary) Haptics.Medium(); else Haptics.Light(); }
                if (Input.GetMouseButtonUp(0))
                {
                    dragging = false;
                    if (lift >= 0.88f) opened = true;
                    else if (dragMax < Screen.height * 0.02f) peekT = 0f;                            // just a tap: show a little light
                }
            }
            else if (peekT >= 0f)
            {
                peekT += dt;
                float k = peekT / 0.9f;
                lift = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) * 0.5f;
                if (k >= 1f) { peekT = -1f; lift = 0f; }
                if (k > 0.2f && !buzzed) { buzzed = true; Haptics.Light(); }
            }
            else lift = Mathf.MoveTowards(lift, 0f, 2.5f * dt);                                       // the lid settles back

            ApplyLift(lift, light, t);
            yield return null;
        }
        while (warm.Count > 0) { CosmeticUI.Thumb(warm.Dequeue()); yield return null; }
        _tapped = false;

        // 3. the lid flies off in a flash of light
        if (best == Rarity.Legendary) { AudioManager.Instance.Fanfare(); Haptics.Heavy(); }
        else if (best == Rarity.Rare) { AudioManager.Instance.Discover(); Haptics.Medium(); }
        else { AudioManager.Instance.Discover(); Haptics.Light(); }
        _hint.gameObject.SetActive(false);
        _btnSkip.gameObject.SetActive(false);
        _ovBurst.color = new Color(light.r, light.g, light.b, 0.55f);
        SpawnConfetti(best == Rarity.Legendary ? 80 : (best == Rarity.Rare ? 55 : 30), new Vector2(0, 40), light);
        var lidFrom = _ovLid.anchoredPosition;
        for (float a = 0f; a < 0.45f; a += Time.unscaledDeltaTime)
        {
            float k = a / 0.45f;
            _flash.color = new Color(1f, 1f, 1f, 1f - k);
            _ovLid.anchoredPosition = Vector2.Lerp(lidFrom, new Vector2(120, 520), k);
            _ovLid.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, -40, k));
            _ovLidImg.color = new Color(1f, 1f, 1f, 1f - k);
            _ovBoxImg.color = new Color(1f, 1f, 1f, 1f - k);
            _beam.color = new Color(light.r, light.g, light.b, 1f - k);
            _gapLight.color = Color.clear;
            _ovBox.localScale = Vector3.one * (1.1f + 0.35f * k);
            yield return null;
        }
        _flash.color = Color.clear;
        _ovBox.gameObject.SetActive(false);

        // 4. show the prize(s)
        if (results.Count == 1) yield return ShowSingle(results[0]);
        else yield return ShowGrid(results);

        _btnOk.gameObject.SetActive(true);
        _busy = false;
    }

    /// <summary>Lid height 0..1: lifts the lid, lets a beam of the prize's colour shine out of the gap and wobbles the box more when it is nearly open.</summary>
    void ApplyLift(float lift, Color light, float t)
    {
        float lidUp = lift * 150f;
        _ovLid.anchoredPosition = new Vector2(lift * 16f, lidUp);
        _ovLid.localRotation = Quaternion.Euler(0, 0, -lift * 9f);
        float wobble = lift < 0.01f ? 1.5f : 1.5f + lift * 4f;
        _ovBox.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * (lift < 0.01f ? 5f : 38f)) * wobble);
        _ovBox.localScale = Vector3.one * (1f + 0.012f * Mathf.Sin(t * 4f) + lift * 0.06f);

        float gap = Mathf.Max(0f, lidUp - 20f);                       // the box top is at 72, the lid bottom at 51 + lidUp
        _gapLight.rectTransform.sizeDelta = new Vector2(440f, gap * 1.8f + 50f);
        _gapLight.rectTransform.anchoredPosition = new Vector2(0, 72f + gap * 0.5f);
        _gapLight.color = new Color(light.r, light.g, light.b, Mathf.Clamp01(lift * 4f));
        _beam.rectTransform.sizeDelta = new Vector2(380f + lift * 420f, 200f + lift * 1300f);
        _beam.rectTransform.anchoredPosition = new Vector2(0, 72f + lift * 330f);
        _beam.color = new Color(light.r, light.g, light.b, Mathf.Clamp01(lift * 1.6f));
        _ovGlow.color = new Color(light.r, light.g, light.b, lift * 0.55f);
        _ovBurst.color = new Color(light.r, light.g, light.b, lift * lift * 0.7f);
    }

    IEnumerator ShowSingle(PullResult r)
    {
        var item = r.Item;
        var rc = Cosmetics.RarityColor(item.Rarity);
        _prize = item;
        _cardBorder.color = rc;
        _cardThumb.sprite = CosmeticUI.Thumb(item);
        _cardKind.text = CosmeticUI.KindLabel(item);
        _cardName.text = item.Name;
        _cardRarity.text = CosmeticUI.RarityName(item.Rarity);
        _cardRarity.color = rc;
        _cardStatus.text = r.IsNew ? Loc.T("new_item") : Loc.T("duplicate") + "  +" + r.Refund + " " + Loc.T("coins");
        _cardStatus.color = r.IsNew ? Green : Gold;
        _blinkSingle = !r.IsNew;
        _cardCoin.gameObject.SetActive(false);
        _cardAmount.gameObject.SetActive(false);
        _cardAmount.text = "+" + r.Refund;
        _cardThumb.color = Color.white;
        _cardRoot.gameObject.SetActive(true);
        _btnEquip.gameObject.SetActive(r.IsNew);
        _btnOk.GetComponent<RectTransform>().anchoredPosition = new Vector2(r.IsNew ? 250 : 0, -520);
        _equipLabel.text = Loc.T("equip");
        _btnEquip.interactable = true;
        _btnEquip.targetGraphic.color = Green;

        for (float a = 0f; a < 0.4f; a += Time.unscaledDeltaTime)
        {
            float k = a / 0.4f;
            _cardRoot.localScale = Vector3.one * Pop(k);
            yield return null;
        }
        _cardRoot.localScale = Vector3.one;
    }

    IEnumerator ShowGrid(List<PullResult> results)
    {
        _gridRoot.gameObject.SetActive(true);
        _btnOk.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -520);
        for (int i = 0; i < GridSize; i++) _mini[i].SetActive(false);
        _prize = null;

        for (int i = 0; i < results.Count && i < GridSize; i++)
        {
            var r = results[i];
            var rc = Cosmetics.RarityColor(r.Item.Rarity);
            _miniBorder[i].color = rc;
            _miniThumb[i].sprite = CosmeticUI.Thumb(r.Item);
            _miniName[i].text = r.Item.Name;
            _miniTag[i].text = r.IsNew ? Loc.T("new_item") : "+" + r.Refund;
            _miniTag[i].color = r.IsNew ? Green : Gold;
            _miniCoin[i].gameObject.SetActive(!r.IsNew);
            _miniDup[i] = !r.IsNew;
            _miniTag[i].rectTransform.anchoredPosition = new Vector2(r.IsNew ? 0 : 14, -90);
            _mini[i].SetActive(true);
            _mini[i].transform.localScale = Vector3.zero;
            if (r.Item.Rarity == Rarity.Legendary) { AudioManager.Instance.Fanfare(); Haptics.Heavy(); SpawnConfetti(50, _gridRoot.anchoredPosition + (Vector2)_mini[i].GetComponent<RectTransform>().anchoredPosition, rc); }
            else if (r.Item.Rarity == Rarity.Rare) { AudioManager.Instance.Discover(); Haptics.Medium(); }
            else AudioManager.Instance.Click();

            // pop this card in; a tap shows the rest at once
            for (float a = 0f; a < 0.14f; a += Time.unscaledDeltaTime)
            {
                _mini[i].transform.localScale = Vector3.one * Pop(Mathf.Min(a / 0.14f, 1f));
                yield return null;
            }
            _mini[i].transform.localScale = Vector3.one;
            if (_tapped) { for (int j = i + 1; j < results.Count && j < GridSize; j++) ShowMiniInstant(j, results[j]); break; }
        }
        _tapped = false;
    }

    void ShowMiniInstant(int i, PullResult r)
    {
        var rc = Cosmetics.RarityColor(r.Item.Rarity);
        _miniBorder[i].color = rc;
        _miniThumb[i].sprite = CosmeticUI.Thumb(r.Item);
        _miniName[i].text = r.Item.Name;
        _miniTag[i].text = r.IsNew ? Loc.T("new_item") : "+" + r.Refund;
        _miniTag[i].color = r.IsNew ? Green : Gold;
        _miniCoin[i].gameObject.SetActive(!r.IsNew);
        _miniDup[i] = !r.IsNew;
        _miniTag[i].rectTransform.anchoredPosition = new Vector2(r.IsNew ? 0 : 14, -90);
        _mini[i].SetActive(true);
        _mini[i].transform.localScale = Vector3.one;
    }

    /// <summary>0..1 -> scale that overshoots a little and settles on 1.</summary>
    static float Pop(float k) => k < 0.7f ? Mathf.Lerp(0.2f, 1.12f, k / 0.7f) : Mathf.Lerp(1.12f, 1f, (k - 0.7f) / 0.3f);

    void OnEquip()
    {
        if (_prize == null) return;
        AudioManager.Instance.Click();
        Cosmetics.Equip(_prize);
        _equipLabel.text = Loc.T("equipped");
        _btnEquip.interactable = false;
        _btnEquip.targetGraphic.color = Gray;
    }

    public void Close()
    {
        if (_busy) return;
        AudioManager.Instance.Click();
        _blinkSingle = false;
        Overlay.SetActive(false);
        Refresh();
    }

    /// <summary>Back button: skip the animation, or close the result. Returns false when nothing was open.</summary>
    public bool OnBack()
    {
        if (!IsOpen) return false;
        if (_busy) { _tapped = true; _skip = true; } else Close();
        return true;
    }

    // ------------------------------------------------------------------ confetti

    void SpawnConfetti(int count, Vector2 origin, Color accent)
    {
        var palette = CatDatabase.ConfettiColors;
        for (int i = 0; i < MaxParticles && count > 0; i++)
        {
            if (_plife[i] > 0f) continue;
            count--;
            float ang = Random.Range(0f, Mathf.PI * 2f), speed = Random.Range(500f, 1500f);
            _pv[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed + new Vector2(0, 300f);
            _plife[i] = Random.Range(1.4f, 2.2f);
            _pspin[i] = Random.Range(-540f, 540f);
            float s = Random.Range(14f, 30f);
            _pr[i].sizeDelta = new Vector2(s, s * Random.Range(0.5f, 1f));
            _pr[i].anchoredPosition = origin;
            _pr[i].localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            _pi[i].color = Random.value < 0.35f ? accent : palette[Random.Range(0, palette.Length)];
            _pi[i].gameObject.SetActive(true);
        }
    }

    // ------------------------------------------------------------------ per-frame

    void Update()
    {
        float dt = Time.unscaledDeltaTime, t = Time.unscaledTime;

        // the box on the tab bobs and wiggles
        if (_boxRoot != null && _boxRoot.gameObject.activeInHierarchy)
        {
            _boxRoot.anchoredPosition = new Vector2(0, -650f + Mathf.Sin(t * 2.2f) * 14f);
            _boxRoot.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.7f) * 3f);
            _glow.color = new Color(1f, 0.85f, 0.5f, 0.55f + 0.25f * Mathf.Sin(t * 2f));
        }

        if (IsOpen)
        {
            // a duplicate blinks between the item and the coins it was turned into
            bool itemPhase = ((int)(t * 2.6f) & 1) == 0;
            if (_blinkSingle && _cardRoot.gameObject.activeSelf)
            {
                _cardThumb.color = itemPhase ? Color.white : new Color(1f, 1f, 1f, 0.12f);
                _cardCoin.gameObject.SetActive(!itemPhase);
                _cardAmount.gameObject.SetActive(!itemPhase);
            }
            for (int i = 0; i < GridSize; i++)
                if (_miniDup[i] && _mini[i].activeSelf)
                {
                    _miniThumb[i].color = itemPhase ? Color.white : new Color(1f, 1f, 1f, 0.12f);
                    _miniBig[i].gameObject.SetActive(!itemPhase);
                }

            _ovBurst.rectTransform.localRotation = Quaternion.Euler(0, 0, t * 25f);
            for (int i = 0; i < MaxParticles; i++)
            {
                if (_plife[i] <= 0f) continue;
                _plife[i] -= dt;
                if (_plife[i] <= 0f) { _pi[i].gameObject.SetActive(false); continue; }
                _pv[i].y -= 2200f * dt;
                _pv[i] *= 1f - 0.9f * dt;
                _pr[i].anchoredPosition += _pv[i] * dt;
                _pr[i].Rotate(0, 0, _pspin[i] * dt);
                var c = _pi[i].color;
                c.a = Mathf.Clamp01(_plife[i] / 0.4f);
                _pi[i].color = c;
            }
        }

        if (_msgT < 2f)
        {
            _msgT += dt;
            var c = _msg.color;
            c.a = _msgT < 1.4f ? 1f : Mathf.Clamp01(1f - (_msgT - 1.4f) / 0.6f);
            _msg.color = c;
            if (_msgT >= 2f) _msg.gameObject.SetActive(false);
        }

        if (_lastCoins != PlayerData.Coins && _collection != null && !IsOpen) Refresh();
    }
}
