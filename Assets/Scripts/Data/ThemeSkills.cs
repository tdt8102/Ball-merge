using UnityEngine;

/// <summary>How a theme skill is used: tap once, or tap a cat on the board.</summary>
public enum ThemeSkillKind { Instant, Target }

/// <summary>
/// Stable metadata for one Theme Set Skill. Numeric values live in Balance (Assets/Resources/balance.json)
/// so they can be tuned without touching this file.
/// </summary>
public sealed class ThemeSkillDef
{
    public readonly string Id;
    public readonly string NameKey;
    public readonly string DescriptionKey;
    public readonly ThemeSkillKind Kind;
    public readonly string ThemeId;

    public ThemeSkillDef(string id, string nameKey, string descriptionKey, ThemeSkillKind kind, string themeId)
    {
        Id = id;
        NameKey = nameKey;
        DescriptionKey = descriptionKey;
        Kind = kind;
        ThemeId = themeId;
    }

    /// <summary>The cat theme this skill belongs to (null if the theme id is unknown).</summary>
    public CatStyle Theme => Cosmetics.FindTheme(ThemeId);

    public override string ToString() => Id;
}

/// <summary>
/// The seven Theme Set Skills, keyed by stable string ids and CatStyle ids. This is metadata only:
/// gameplay and UI are added in later phases. Use Balance.Current for every tunable number.
/// </summary>
public static class ThemeSkills
{
    public const string EraserId = "skill_eraser";
    public const string HugId = "skill_hug";
    public const string ShrinkId = "skill_shrink";
    public const string LevelUpId = "skill_level_up";
    public const string MoonlightId = "skill_moonlight";
    public const string SugarRushId = "skill_sugar_rush";
    public const string MidasId = "skill_midas";

    // Hard design limits; Balance can tune inside these ranges but never past them.
    public const int LevelUpMaxTargetLevel = 8;
    public const int LevelUpMaxResultLevel = 9;
    public const int MidasMaxTargetLevel = 7;

    public static readonly ThemeSkillDef Eraser = new ThemeSkillDef(
        EraserId, "skill_eraser_name", "skill_eraser_desc", ThemeSkillKind.Target, "cat_doodle");

    public static readonly ThemeSkillDef Hug = new ThemeSkillDef(
        HugId, "skill_hug_name", "skill_hug_desc", ThemeSkillKind.Instant, "cat_kawaii");

    public static readonly ThemeSkillDef Shrink = new ThemeSkillDef(
        ShrinkId, "skill_shrink_name", "skill_shrink_desc", ThemeSkillKind.Instant, "cat_chibi");

    public static readonly ThemeSkillDef LevelUp = new ThemeSkillDef(
        LevelUpId, "skill_level_up_name", "skill_level_up_desc", ThemeSkillKind.Target, "cat_pixel");

    public static readonly ThemeSkillDef Moonlight = new ThemeSkillDef(
        MoonlightId, "skill_moonlight_name", "skill_moonlight_desc", ThemeSkillKind.Instant, "cat_midnight");

    public static readonly ThemeSkillDef SugarRush = new ThemeSkillDef(
        SugarRushId, "skill_sugar_rush_name", "skill_sugar_rush_desc", ThemeSkillKind.Instant, "cat_candy");

    public static readonly ThemeSkillDef Midas = new ThemeSkillDef(
        MidasId, "skill_midas_name", "skill_midas_desc", ThemeSkillKind.Target, "cat_gold");

    public static readonly ThemeSkillDef[] All =
    {
        Eraser, Hug, Shrink, LevelUp, Moonlight, SugarRush, Midas
    };

    public static ThemeSkillDef Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < All.Length; i++)
            if (All[i].Id == id) return All[i];
        return null;
    }

    /// <summary>The skill of a theme, or null for Classic / unknown themes.</summary>
    public static ThemeSkillDef OfTheme(string themeId)
    {
        if (string.IsNullOrEmpty(themeId)) return null;
        for (int i = 0; i < All.Length; i++)
            if (All[i].ThemeId == themeId) return All[i];
        return null;
    }

    public static ThemeSkillDef Of(CatStyle theme) => theme == null ? null : OfTheme(theme.Id);

    // ------------------------------------------------------------------ shared config

    public static int MaxEnergy => Balance.Current.themeSkillMaxEnergy;
    public static int StartingEnergy => Balance.Current.themeSkillStartingEnergy;
    public static int EnergyPerMerge => Balance.Current.themeSkillEnergyPerMerge;
    public static int SkillCost => Balance.Current.themeSkillCost;

    /// <summary>Level Up may target a cat of this level: inside the configured range, and the result may not pass the configured maximum.</summary>
    public static bool CanLevelUp(int level)
    {
        var b = Balance.Current;
        return level >= b.levelUpMinLevel && level <= b.levelUpMaxLevel && level + 1 <= b.levelUpMaxResult;
    }

    /// <summary>Midas may sell a cat of this level (Balance range, never above level 7).</summary>
    public static bool CanMidas(int level)
    {
        var b = Balance.Current;
        return level >= b.midasMinLevel && level <= b.midasMaxLevel;
    }

    public static float HugSeconds => Balance.Current.hugDuration;
    public static float HugRadius => Balance.Current.hugRadius;
    public static float HugAcceleration => Balance.Current.hugAcceleration;
    public static float HugMaxSpeed => Balance.Current.hugMaxSpeed;

    public static float ShrinkScale => Balance.Current.shrinkScale;
    public static float ShrinkHoldSeconds => Balance.Current.shrinkDuration;
    public static float ShrinkRestoreSeconds => Balance.Current.shrinkRestoreDuration;
    public static bool ShrinkRestorePausesDanger => Balance.Current.shrinkRestorePausesDanger;

    public static float MoonlightSeconds => Balance.Current.moonlightDuration;
    public static float SugarRushSeconds => Balance.Current.sugarRushDuration;
    public static float SugarRushScoreMultiplier => Balance.Current.sugarRushScoreMultiplier;
    public static float SugarRushCoinMultiplier => Balance.Current.sugarRushCoinMultiplier;

    public static int ShardRewardPerDuplicateCat => Balance.Current.shardRewardPerDuplicateCat;

    /// <summary>Exchange cost for the three rarities that exist in the current catalogue.</summary>
    public static int ShardCost(Rarity rarity)
    {
        var b = Balance.Current;
        switch (rarity)
        {
            case Rarity.Rare: return b.shardCostRare;
            case Rarity.Legendary: return b.shardCostLegendary;
            default: return b.shardCostCommon;
        }
    }

    /// <summary>Exchange price of a cat: the level-10 "Majesty" cat costs the Majesty price, every other cat its rarity's price.</summary>
    public static int ShardPrice(CatItem cat) => cat.Level >= CatDatabase.MaxLevel ? MajestyShardCost : ShardCost(cat.Rarity);

    /// <summary>Level 10 ("Majesty") is still Legendary in the current item model; this is its reserved price.</summary>
    public static int MajestyShardCost => Balance.Current.shardCostMajesty;

    /// <summary>Coins paid by Midas for a cat level. Target eligibility is checked by the gameplay layer.</summary>
    public static int MidasCoin(int level)
    {
        var b = Balance.Current;
        return b.midasBaseCoin + b.midasCoinPerLevel * Mathf.Max(0, level);
    }
}
