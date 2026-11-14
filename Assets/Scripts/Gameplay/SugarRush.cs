using UnityEngine;

/// <summary>
/// The multiplier applied to what a MERGE pays (score and coins). GameManager applies it in one place, AddMergeReward;
/// discovery rewards, gacha refunds and every other source never go through it. Rounding for coins: half up (1.5 -> 2).
/// </summary>
public sealed class RewardMultiplier
{
    /// <summary>True while Sugar Rush runs.</summary>
    public bool SugarRushActive { get; set; }

    public int MergeScore(int baseScore) => SugarRushActive ? Scale(baseScore, ThemeSkills.SugarRushScoreMultiplier) : baseScore;
    public int MergeCoins(int baseCoins) => SugarRushActive ? Scale(baseCoins, ThemeSkills.SugarRushCoinMultiplier) : baseCoins;

    static int Scale(int value, float factor) => Mathf.FloorToInt(value * factor + 0.5f);
}

/// <summary>
/// Sugar Rush: merge score and merge coins are multiplied for its duration (Balance: sugarRushDuration, ...Multiplier).
/// It does not stack or refresh: SkillRuntime ignores the button while it runs, and Activate refuses a second start.
/// </summary>
public sealed class SugarRushHandler : ISkillHandler
{
    readonly RewardMultiplier _rewards;

    public SugarRushHandler(RewardMultiplier rewards) { _rewards = rewards; }

    public float Duration => ThemeSkills.SugarRushSeconds;
    public bool CanTarget(Cat target) => false;

    public bool Activate(Cat target)
    {
        if (_rewards.SugarRushActive) return false;
        _rewards.SugarRushActive = true;
        return true;
    }

    public void End() => _rewards.SugarRushActive = false;
}
