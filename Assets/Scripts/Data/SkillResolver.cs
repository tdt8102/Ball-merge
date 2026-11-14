/// <summary>Why a theme skill can or cannot be used in the next run.</summary>
public enum SkillStatus
{
    Ready,          // all 11 slots wear the same non-Classic theme and its skill is unlocked
    MissingSlots,   // a slot has no valid owned cat equipped
    MixedTheme,     // the slots wear more than one theme
    Classic,        // the whole outfit is Classic (or a theme without a skill)
    NotUnlocked     // one non-Classic theme in all 11 slots, but its set was never completed
}

public struct SkillReadiness
{
    public SkillStatus Status;
    public int ValidSlots, TotalSlots;
    public CatStyle Theme;          // the common theme (null for MissingSlots / MixedTheme)
    public ThemeSkillDef Skill;     // set for Ready and NotUnlocked
    public bool IsReady => Status == SkillStatus.Ready;
}

/// <summary>
/// Decides which theme skill a run gets. Equipment is read from the existing slots (PlayerData.catSkin through
/// Cosmetics); nothing is stored here.
///
/// Start of a run: Resolve() on the current equipment. Resume of a saved run: the skill stored in the snapshot
/// (RunSnapshot.activeSkillId) is used as is, NOT recomputed. Skins can only be changed in the Menu scene, but a saved
/// run can be continued after changing them, so recomputing on resume would let the player swap skills mid-run.
/// Snapshots written before this existed (skillRecorded == false) are resolved once, then recorded.
/// </summary>
public static class SkillResolver
{
    /// <summary>The theme this cat number wears, or null when that slot is not valid (unknown theme, or the cat is not owned).</summary>
    public static CatStyle WornTheme(int level)
    {
        var t = Cosmetics.FindTheme(PlayerData.CatSkin(level));
        return t == null || !PlayerData.IsOwned(Cosmetics.CatItemOf(t, level).Id) ? null : t;
    }

    public static SkillReadiness Resolve()
    {
        int total = SetCompletion.Total, valid = 0;
        CatStyle theme = null;
        bool mixed = false;
        for (int level = 0; level < total; level++)
        {
            var t = WornTheme(level);
            if (t == null) continue;
            valid++;
            if (theme == null) theme = t;
            else if (theme != t) mixed = true;
        }

        var r = new SkillReadiness { ValidSlots = valid, TotalSlots = total };
        if (valid < total) { r.Status = SkillStatus.MissingSlots; return r; }
        if (mixed) { r.Status = SkillStatus.MixedTheme; return r; }

        r.Theme = theme;
        r.Skill = ThemeSkills.Of(theme);
        if (theme.Default || r.Skill == null) r.Status = SkillStatus.Classic;
        else r.Status = SetCompletion.IsSkillUnlocked(r.Skill) ? SkillStatus.Ready : SkillStatus.NotUnlocked;
        return r;
    }

    /// <summary>The skill for a run that starts now (null when not ready).</summary>
    public static ThemeSkillDef ForNewRun()
    {
        var r = Resolve();
        return r.IsReady ? r.Skill : null;
    }

    /// <summary>The skill for a run loaded from a save: the recorded one, or (old snapshot) the current equipment.</summary>
    public static ThemeSkillDef ForResume(RunSnapshot run)
    {
        if (run == null || !run.skillRecorded) return ForNewRun();
        var skill = ThemeSkills.Find(run.activeSkillId);
        return SetCompletion.IsSkillUnlocked(skill) ? skill : null;       // empty id = the run had no skill
    }
}
