/// <summary>Where a theme's skill stands for the player. Shown by the Skin tab and the Cat Codex.</summary>
public enum SkillState
{
    NoSkill,    // the theme has no skill (Classic)
    Locked,     // the set is not complete
    Unlocked,   // the set was completed, but the outfit does not give the skill yet
    Active      // unlocked and all 11 cat numbers wear this theme: the next game has the skill
}

/// <summary>
/// One read-only snapshot of a theme for the UI: collection x/11, equipped x/11 and the skill state. Everything comes from
/// the shared sources (SetCompletion for the collection and the unlock, SkillResolver for what the outfit gives); the
/// views only show it and never work out ownership or unlocks themselves.
/// </summary>
public struct ThemeSkillStatus
{
    public CatStyle Theme;
    public ThemeSkillDef Skill;     // null for Classic
    public int Owned, Total;        // collection: cats of this theme the player owns / 11
    public int Worn;                // equipment: cat numbers that wear this theme (and own that cat) / 11
    public int MissingMask;         // bit n set = cat number n+1 does not wear this theme
    public SkillState State;

    public bool HasSkill => Skill != null;
    public bool Complete => Owned >= Total;
}

public static class ThemeSkillInfo
{
    public static ThemeSkillStatus Of(CatStyle theme)
    {
        var s = new ThemeSkillStatus { Theme = theme, Skill = ThemeSkills.Of(theme) };
        var progress = SetCompletion.Progress(theme);
        s.Owned = progress.Owned;
        s.Total = progress.Total;
        for (int level = 0; level < s.Total; level++)
        {
            if (SkillResolver.WornTheme(level) == theme) s.Worn++;
            else s.MissingMask |= 1 << level;
        }

        if (s.Skill == null) s.State = SkillState.NoSkill;
        else if (!SetCompletion.IsSkillUnlocked(s.Skill)) s.State = SkillState.Locked;
        else
        {
            var outfit = SkillResolver.Resolve();
            s.State = outfit.IsReady && outfit.Skill == s.Skill ? SkillState.Active : SkillState.Unlocked;
        }
        return s;
    }
}
