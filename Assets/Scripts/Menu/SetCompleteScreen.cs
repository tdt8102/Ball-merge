using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "SET COMPLETE! Skill unlocked: ..." card. Gacha and the exchange screen show it when a result says the theme was just
/// completed (PullResult.Completed / the skill returned by Cosmetics.Exchange). That value is only set when the unlock was
/// recorded by this very result, so the card appears once, at that moment, and never again for the same skill.
/// </summary>
public class SetCompleteScreen : MonoBehaviour
{
    static readonly Color Brown = new Color(0.55f, 0.36f, 0.24f);
    static readonly Color Ink = new Color(0.36f, 0.22f, 0.15f);
    static readonly Color Soft = new Color(0.5f, 0.36f, 0.28f);
    static readonly Color Orange = new Color(0.95f, 0.6f, 0.2f);
    static readonly Color Gold = new Color(1f, 0.85f, 0.3f);
    static readonly Color Cream = new Color(1f, 0.97f, 0.9f);
    static readonly Vector2 C = new Vector2(0.5f, 0.5f);

    public bool IsOpen => gameObject.activeSelf;

    RectTransform _card;
    Image _glow, _cat;
    Text _theme, _skillName, _desc;
    float _t;

    public static SetCompleteScreen Create(RectTransform parent)
    {
        var go = new GameObject("SetCompleteScreen", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0.1f, 0.06f, 0.12f, 0.88f);       // also blocks taps behind it
        go.GetComponent<Button>().transition = Selectable.Transition.None;
        var s = go.AddComponent<SetCompleteScreen>();
        s.Build(rt);
        go.SetActive(false);
        return s;
    }

    void Build(RectTransform rt)
    {
        _glow = GameUI.Img(rt, "Glow", CatSprites.Glow(), new Color(1f, 0.85f, 0.4f, 0.8f), C, new Vector2(0, 120), new Vector2(1100, 1100));

        var cardGo = new GameObject("Card", typeof(RectTransform));
        _card = (RectTransform)cardGo.transform;
        _card.SetParent(rt, false);
        _card.anchorMin = _card.anchorMax = C;
        _card.anchoredPosition = new Vector2(0, 40);
        _card.sizeDelta = new Vector2(860, 940);
        GameUI.RoundImg(_card, "Border", Gold, C, Vector2.zero, new Vector2(860, 940), 60f);
        GameUI.RoundImg(_card, "Face", Cream, C, Vector2.zero, new Vector2(836, 916), 54f);

        var title = GameUI.Label(_card, "Title", "", 84, TextAnchor.MiddleCenter, C, new Vector2(0, 360), new Vector2(820, 110));
        title.color = Orange;
        var outline = title.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(4, -4);
        Loc.Bind(title, () => Loc.T("set_complete"));

        _cat = GameUI.Img(_card, "Cat", null, Color.white, C, new Vector2(0, 175), new Vector2(240, 240));
        _theme = GameUI.Label(_card, "Theme", "", 48, TextAnchor.MiddleCenter, C, new Vector2(0, 20), new Vector2(800, 64));
        _theme.color = Ink;
        var unlocked = GameUI.Label(_card, "Unlocked", "", 34, TextAnchor.MiddleCenter, C, new Vector2(0, -50), new Vector2(800, 46));
        unlocked.color = Soft;
        Loc.Bind(unlocked, () => Loc.T("skill_unlocked"));
        _skillName = GameUI.Label(_card, "Skill", "", 70, TextAnchor.MiddleCenter, C, new Vector2(0, -120), new Vector2(800, 90));
        _skillName.color = Orange;
        _desc = GameUI.Label(_card, "Desc", "", 32, TextAnchor.UpperCenter, C, new Vector2(0, -175), new Vector2(740, 130));
        _desc.horizontalOverflow = HorizontalWrapMode.Wrap;
        _desc.fontStyle = FontStyle.Normal;
        _desc.color = Ink;

        var ok = GameUI.Button(_card, "Ok", "", 54, C, new Vector2(0, -375), new Vector2(440, 110), new Color(0.2f, 0.65f, 0.4f));
        Loc.Bind(ok, () => Loc.T("ok"));
        ok.onClick.AddListener(Close);
    }

    public void Show(ThemeSkillDef skill)
    {
        if (skill == null || skill.Theme == null) return;
        _cat.sprite = CatSprites.Icon(CatDatabase.MaxLevel, skill.Theme.Index);
        _theme.text = skill.Theme.Name;
        _skillName.text = SkillText.Name(skill);
        _desc.text = SkillText.Description(skill);
        _t = 0f;
        gameObject.SetActive(true);
        AudioManager.Instance.Fanfare();
        Haptics.Heavy();
    }

    public void Close()
    {
        if (!IsOpen) return;
        AudioManager.Instance.Click();
        gameObject.SetActive(false);
    }

    void Update()
    {
        _t += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(_t / 0.4f);
        float pop = k < 0.7f ? Mathf.Lerp(0.2f, 1.1f, k / 0.7f) : Mathf.Lerp(1.1f, 1f, (k - 0.7f) / 0.3f);
        _card.localScale = Vector3.one * pop;
        _glow.rectTransform.localRotation = Quaternion.Euler(0, 0, _t * 20f);
        _cat.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(_t * 3f) * 6f);
    }
}
