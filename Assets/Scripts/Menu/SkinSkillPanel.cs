using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The two cards on each side of the tray in the Skin tab: the theme's skill (name, state, description) and the progress
/// (collected x/11, wearing x/11, and why the skill is not usable yet). It only shows what ThemeSkillInfo / SkillResolver
/// report; it never decides ownership or unlocks.
/// </summary>
public class SkinSkillPanel
{
    static readonly Color Brown = new Color(0.55f, 0.36f, 0.24f);
    static readonly Color Ink = new Color(0.36f, 0.22f, 0.15f);
    static readonly Color Soft = new Color(0.5f, 0.36f, 0.28f);
    static readonly Color Orange = new Color(0.95f, 0.6f, 0.2f);
    static readonly Color Green = new Color(0.2f, 0.65f, 0.4f);
    static readonly Color Cream = new Color(1f, 0.97f, 0.9f);
    static readonly Color Track = new Color(0.85f, 0.75f, 0.6f);
    static readonly Vector2 Top = new Vector2(0.5f, 1f);
    static readonly Vector2 C = new Vector2(0.5f, 0.5f);

    const float CardW = 340f, CardH = 312f, BarW = 300f;

    GameObject _left, _right;
    Image _icon;
    Text _name, _state, _desc, _cost;
    Text _rowA, _valA, _rowB, _valB, _reason;
    RectTransform _fillA, _fillB;

    public SkinSkillPanel(RectTransform page, Vector2 center)
    {
        // each side of the 312 px tray, with a small gap
        float x = 156f + 14f + CardW * 0.5f;
        BuildLeft(page, new Vector2(-x, center.y));
        BuildRight(page, new Vector2(x, center.y));
    }

    static RectTransform Card(RectTransform page, string name, Vector2 pos, out GameObject root)
    {
        var border = GameUI.RoundImg(page, name + "Border", Brown, Top, pos, new Vector2(CardW + 8, CardH + 8), 30f);
        var face = GameUI.RoundImg((RectTransform)border.transform, name, Cream, C, Vector2.zero, new Vector2(CardW, CardH), 26f);
        root = border.gameObject;
        return (RectTransform)face.transform;
    }

    static Text Wrapped(RectTransform parent, string name, int size, Vector2 topLeft, float width, float height)
    {
        var t = GameUI.Label(parent, name, "", size, TextAnchor.UpperLeft, C, topLeft, new Vector2(width, height));
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.fontStyle = FontStyle.Normal;
        t.lineSpacing = 0.95f;
        return t;
    }

    void BuildLeft(RectTransform page, Vector2 pos)
    {
        var card = Card(page, "SkillCard", pos, out _left);
        var title = GameUI.Label(card, "Title", "", 22, TextAnchor.MiddleLeft, C, new Vector2(-150, 134), new Vector2(300, 28));
        title.color = Soft;
        Loc.Bind(title, () => Loc.T("skill_title"));
        _icon = GameUI.Img(card, "Icon", null, Color.white, C, new Vector2(-104, 74), new Vector2(86, 86));
        _name = GameUI.Label(card, "Name", "", 28, TextAnchor.MiddleLeft, C, new Vector2(-52, 90), new Vector2(210, 40));
        _name.color = Ink;
        _state = GameUI.Label(card, "State", "", 24, TextAnchor.MiddleLeft, C, new Vector2(-52, 56), new Vector2(210, 34));
        _desc = Wrapped(card, "Desc", 22, new Vector2(-150, 0), 300, 130);
        _desc.color = Ink;
        _cost = GameUI.Label(card, "Cost", "", 22, TextAnchor.MiddleLeft, C, new Vector2(-150, -134), new Vector2(300, 28));
        _cost.color = Soft;
    }

    void BuildRight(RectTransform page, Vector2 pos)
    {
        var card = Card(page, "ProgressCard", pos, out _right);
        _rowA = GameUI.Label(card, "RowA", "", 22, TextAnchor.MiddleLeft, C, new Vector2(-150, 128), new Vector2(190, 30));
        _rowA.color = Soft;
        _valA = GameUI.Label(card, "ValA", "", 30, TextAnchor.MiddleRight, C, new Vector2(90, 128), new Vector2(120, 38));
        _valA.color = Ink;
        _fillA = Bar(card, 98f);
        _rowB = GameUI.Label(card, "RowB", "", 22, TextAnchor.MiddleLeft, C, new Vector2(-150, 60), new Vector2(190, 30));
        _rowB.color = Soft;
        _valB = GameUI.Label(card, "ValB", "", 30, TextAnchor.MiddleRight, C, new Vector2(90, 60), new Vector2(120, 38));
        _valB.color = Ink;
        _fillB = Bar(card, 30f);
        _reason = Wrapped(card, "Reason", 22, new Vector2(-150, -4), 300, 150);
        _reason.color = Ink;
    }

    static RectTransform Bar(RectTransform card, float y)
    {
        var track = GameUI.RoundImg(card, "Track", Track, C, new Vector2(0, y), new Vector2(BarW, 14), 7f);
        var fill = GameUI.RoundImg((RectTransform)track.transform, "Fill", Orange, new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 14), 7f);
        fill.rectTransform.pivot = new Vector2(0, 0.5f);
        return fill.rectTransform;
    }

    static void SetBar(RectTransform fill, int have, int total, bool full)
    {
        fill.sizeDelta = new Vector2(BarW * Mathf.Clamp01(have / (float)Mathf.Max(1, total)), 14f);
        fill.GetComponent<Image>().color = full ? Green : Orange;
    }

    public void SetActive(bool on)
    {
        _left.SetActive(on);
        _right.SetActive(on);
    }

    /// <summary>The theme being browsed: its skill, collection and equipment.</summary>
    public void ShowTheme(ThemeSkillStatus s)
    {
        SetSkill(s.Skill, s.State);
        _rowA.text = Loc.T("skill_collected");
        _valA.text = s.Owned + " / " + s.Total;
        SetBar(_fillA, s.Owned, s.Total, s.Complete);
        _rowB.text = Loc.T("skill_wearing");
        _valB.text = s.Worn + " / " + s.Total;
        SetBar(_fillB, s.Worn, s.Total, s.Worn >= s.Total);
        _reason.text = SkillText.Reason(s);
        _reason.color = s.State == SkillState.Active ? Green : Ink;
    }

    /// <summary>The cat-number view: what the whole current outfit gives.</summary>
    public void ShowOutfit(SkillReadiness r)
    {
        if (r.Skill != null && r.Theme != null) SetSkill(r.Skill, ThemeSkillInfo.Of(r.Theme).State);
        else SetSkill(null, SkillState.NoSkill);
        _rowA.text = Loc.T("skill_outfit");
        _valA.text = r.ValidSlots + " / " + r.TotalSlots;
        SetBar(_fillA, r.ValidSlots, r.TotalSlots, r.ValidSlots >= r.TotalSlots);
        _rowB.text = "";
        _valB.text = "";
        _fillB.parent.gameObject.SetActive(false);              // no second row in this view
        _reason.text = SkillText.Outfit(r);
        _reason.color = r.IsReady ? Green : Ink;
    }

    void SetSkill(ThemeSkillDef skill, SkillState state)
    {
        _fillB.parent.gameObject.SetActive(true);
        bool has = skill != null;
        _icon.gameObject.SetActive(has);
        if (has)
        {
            _icon.sprite = CosmeticUI.SkillIcon(skill);
            _icon.color = state == SkillState.Locked ? CosmeticUI.Locked : Color.white;
        }
        _name.text = has ? SkillText.Name(skill) : "-";
        _state.text = SkillText.State(state);
        _state.color = CosmeticUI.StateColor(state);
        _desc.text = has ? SkillText.Description(skill) : "";
        _cost.text = has ? SkillText.Cost() : "";
    }
}
