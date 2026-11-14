using System;

/// <summary>How many of a theme's cats the player owns.</summary>
public struct SetProgress
{
    public CatStyle Theme;
    public int Owned, Total;
    public bool Complete => Owned >= Total;
}

/// <summary>
/// Collection progress per theme and the permanent unlock of its skill. Ownership is NOT stored here: it is read from
/// PlayerData (via Cosmetics.OwnedInTheme), so a cat from any source counts once, and duplicates cannot raise the count.
/// Only the "already unlocked" fact is stored (PlayerData.skillsUnlocked) so the unlock event fires exactly once.
/// </summary>
public static class SetCompletion
{
    /// <summary>Raised once per skill, when its theme reaches 11/11 during play. Not raised when a save is loaded.</summary>
    public static event Action<ThemeSkillDef> SkillUnlocked;

    public const int Total = CatDatabase.MaxLevel + 1;

    public static SetProgress Progress(CatStyle theme) =>
        new SetProgress { Theme = theme, Owned = theme == null ? 0 : Cosmetics.OwnedInTheme(theme), Total = Total };

    public static bool IsComplete(CatStyle theme) => Progress(theme).Complete;

    /// <summary>True once the skill's set was completed (permanent). Classic and unknown themes have no skill: always false.</summary>
    public static bool IsSkillUnlocked(ThemeSkillDef skill) => skill != null && PlayerData.IsSkillUnlocked(skill.Id);

    public static bool IsSkillUnlocked(CatStyle theme) => IsSkillUnlocked(ThemeSkills.Of(theme));

    /// <summary>Called by PlayerData.Unlock after a new item was added.</summary>
    public static void OnOwned(string itemId)
    {
        var cat = Cosmetics.Find(itemId) as CatItem;
        var skill = cat == null ? null : ThemeSkills.Of(cat.Theme);
        if (skill == null || !IsComplete(cat.Theme)) return;
        if (PlayerData.MarkSkillUnlocked(skill.Id)) SkillUnlocked?.Invoke(skill);
    }

    /// <summary>Called when a save is loaded: unlock complete sets that are not recorded yet, without raising the event.</summary>
    public static void Backfill()
    {
        foreach (var skill in ThemeSkills.All)
            if (IsComplete(skill.Theme)) PlayerData.MarkSkillUnlocked(skill.Id);
    }
}
