using UnityEngine;

/// <summary>Pictures and labels of collectibles shared by the Skin tab, the Cat Codex and the gacha cards.</summary>
public static class CosmeticUI
{
    public static readonly Color Locked = new Color(0.1f, 0.08f, 0.1f);     // silhouette of something not owned yet

    /// <summary>A cat is shown as that cat in its theme; a background or frame as a small picture of the tray.</summary>
    public static Sprite Thumb(Item item)
    {
        switch (item)
        {
            case CatItem c: return CatSprites.Icon(c.Level, c.Theme.Index);
            case BgStyle b: return CatSprites.Tray(Cosmetics.Frames[0], b);
            case FrameStyle f: return CatSprites.Tray(f, Cosmetics.Backgrounds[0]);
        }
        return null;
    }

    /// <summary>False while the picture still has to be drawn (cats are the expensive ones).</summary>
    public static bool ThumbReady(Item item) => !(item is CatItem c) || CatSprites.IsReady(c.Level, c.Theme.Index);

    /// <summary>Picture of a theme skill (placeholder art: a cat of the skill's theme). Used by the HUD, the Skin tab and the Codex.</summary>
    public static Sprite SkillIcon(ThemeSkillDef skill) => skill == null || skill.Theme == null ? null : CatSprites.Icon(5, skill.Theme.Index);

    public static Color StateColor(SkillState state)
    {
        switch (state)
        {
            case SkillState.Active: return new Color(0.2f, 0.65f, 0.4f);
            case SkillState.Unlocked: return new Color(0.9f, 0.6f, 0.1f);
            default: return new Color(0.5f, 0.36f, 0.28f);
        }
    }

    public static string RarityName(Rarity r) => Loc.T(Cosmetics.RarityKey(r));

    /// <summary>"Pixel - Cat skin" for a cat, "Frame" / "Background" for the others.</summary>
    public static string KindLabel(Item item) =>
        item is CatItem c ? c.Theme.Name + "  -  " + Loc.T("kind_cat") : Loc.T(Cosmetics.KindKey(item.Kind));
}
