using System.Globalization;
using System.Text;

/// <summary>
/// The words for skills: name, description (with the numbers from Balance, so tuning balance.json changes the text too),
/// the state and the reason a skill is not usable yet. Text only: every fact comes from ThemeSkillStatus / SkillResolver.
/// Cat numbers are shown 1-based, as the Skin tab does (level 0 = cat #1).
/// </summary>
public static class SkillText
{
    public static string Name(ThemeSkillDef skill) => skill == null ? "" : Loc.T(skill.NameKey);

    static string N(float v) => v.ToString(IsWhole(v) ? "0" : "0.#", CultureInfo.InvariantCulture);
    static bool IsWhole(float v) => System.Math.Abs(v - System.Math.Round(v)) < 0.001;

    static string F(string key, params object[] args)
    {
        string t = Loc.T(key);
        for (int i = 0; i < args.Length; i++) t = t.Replace("{" + i + "}", args[i] as string ?? System.Convert.ToString(args[i], CultureInfo.InvariantCulture));
        return t;
    }

    public static string Description(ThemeSkillDef skill)
    {
        if (skill == null) return "";
        var b = Balance.Current;
        switch (skill.Id)
        {
            case ThemeSkills.EraserId: return Loc.T(skill.DescriptionKey);
            case ThemeSkills.HugId: return F(skill.DescriptionKey, N(ThemeSkills.HugSeconds));
            case ThemeSkills.ShrinkId:
                return F(skill.DescriptionKey, N(ThemeSkills.ShrinkScale * 100f), N(ThemeSkills.ShrinkHoldSeconds));
            case ThemeSkills.LevelUpId:
                return F(skill.DescriptionKey, b.levelUpMinLevel + 1, System.Math.Min(b.levelUpMaxLevel, b.levelUpMaxResult - 1) + 1);
            case ThemeSkills.MoonlightId: return F(skill.DescriptionKey, N(ThemeSkills.MoonlightSeconds));
            case ThemeSkills.SugarRushId:
                return F(skill.DescriptionKey, N(ThemeSkills.SugarRushSeconds), N(ThemeSkills.SugarRushScoreMultiplier), N(ThemeSkills.SugarRushCoinMultiplier));
            case ThemeSkills.MidasId:
                return F(skill.DescriptionKey, b.midasMinLevel + 1, b.midasMaxLevel + 1, ThemeSkills.MidasCoin(b.midasMinLevel), ThemeSkills.MidasCoin(b.midasMaxLevel));
        }
        return Loc.T(skill.DescriptionKey);
    }

    /// <summary>"Uses 100% energy".</summary>
    public static string Cost() => F("skill_cost", ThemeSkills.SkillCost);

    public static string State(SkillState state)
    {
        switch (state)
        {
            case SkillState.Locked: return Loc.T("skill_state_locked");
            case SkillState.Unlocked: return Loc.T("skill_state_unlocked");
            case SkillState.Active: return Loc.T("skill_state_active");
        }
        return "";
    }

    /// <summary>"#3  #7  #9" for the cat numbers whose bit is set (at most 6 are named, then "+n").</summary>
    public static string Slots(int mask)
    {
        var sb = new StringBuilder();
        int shown = 0, hidden = 0;
        for (int i = 0; i < 31; i++)
        {
            if ((mask & (1 << i)) == 0) continue;
            if (shown >= 6) { hidden++; continue; }
            if (shown > 0) sb.Append("  ");
            sb.Append('#').Append(i + 1);
            shown++;
        }
        if (hidden > 0) sb.Append("  +").Append(hidden);
        return sb.ToString();
    }

    /// <summary>Why the skill cannot be used yet, or that it is in use. "" when the theme has no skill.</summary>
    public static string Reason(ThemeSkillStatus s)
    {
        switch (s.State)
        {
            case SkillState.NoSkill: return Loc.T("skill_none");
            case SkillState.Locked: return F("skill_need_collect", s.Owned, s.Total);
            case SkillState.Unlocked: return F("skill_need_wear", Slots(s.MissingMask), s.Total);
            case SkillState.Active: return Loc.T("skill_in_use_hint");
        }
        return "";
    }

    /// <summary>What the whole current outfit gives (the Skin tab's cat-number view).</summary>
    public static string Outfit(SkillReadiness r)
    {
        switch (r.Status)
        {
            case SkillStatus.Ready: return F("skill_outfit_ready", Name(r.Skill));
            case SkillStatus.MissingSlots: return F("skill_outfit_missing", r.ValidSlots, r.TotalSlots);
            case SkillStatus.MixedTheme: return Loc.T("skill_outfit_mixed");
            case SkillStatus.NotUnlocked:
                return F("skill_outfit_locked", Name(r.Skill), SetCompletion.Progress(r.Theme).Owned, r.TotalSlots);
        }
        return Loc.T("skill_outfit_classic");
    }

    /// <summary>"Pixel  8 / 11" for a cat's set.</summary>
    public static string SetProgress(CatStyle theme, int owned, int total) => F("set_progress", theme.Name, owned, total);
}
