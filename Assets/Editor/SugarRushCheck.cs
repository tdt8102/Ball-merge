using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks Sugar Rush: x2 merge score and x1.5 merge coins for 10 s of game time, applied once; discovery rewards, gacha
/// refunds and other sources untouched; chain merges; pause; no stacking or refresh; energy keeps charging to 100.
/// Menu: Tools/Suika/Check Sugar Rush. Batch: -executeMethod SugarRushCheck.Run (exit code 1 on failure).
/// Touches no save data.
/// </summary>
public static class SugarRushCheck
{
    const float Dt = 0.125f;                        // exact in binary: 10 s = 80 frames

    [MenuItem("Tools/Suika/Check Sugar Rush")]
    public static void RunMenu()
    {
        if (Execute()) Debug.Log("[SugarRush] all checks passed");
    }

    public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

    static bool Execute()
    {
        try
        {
            Test();
            SourceGuard();
            Debug.Log("SUGARRUSH_CHECK_OK");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("SugarRush check failed: " + e.Message);
            return false;
        }
    }

    /// <summary>What GameManager does for a merge (AddMergeReward + energy) and for the skill timer, without a scene.</summary>
    sealed class Sim
    {
        public readonly RewardMultiplier Rewards = new RewardMultiplier();
        public readonly SkillEnergy Energy = new SkillEnergy();
        public readonly SkillRuntime Skills;
        public int Score, Coins;

        public Sim(bool full = true)
        {
            Energy.Reset();
            if (full) while (Energy.Value < Energy.Max) Energy.OnMerge(MergeSource.Normal);
            Skills = new SkillRuntime(Energy);
            Skills.Begin(ThemeSkills.SugarRush, new SugarRushHandler(Rewards));
        }

        public void Merge(int level)
        {
            Energy.OnMerge(MergeSource.Normal);
            if (level >= CatDatabase.MaxLevel)
            {
                Score += Rewards.MergeScore(Balance.Current.maxMergeScore);
                Coins += Rewards.MergeCoins(CatDatabase.MergeCoins(CatDatabase.MaxLevel + 1));
                return;
            }
            Score += Rewards.MergeScore(CatDatabase.Score[level + 1]);
            Coins += Rewards.MergeCoins(CatDatabase.MergeCoins(level + 1));
        }

        /// <summary>Discovery reward as GameManager.NotifyAppear pays it: straight to coins.</summary>
        public void Discover(int level) => Coins += CatDatabase.DiscoveryReward(level);

        public void Frames(int n, bool paused = false) { for (int i = 0; i < n; i++) if (!paused) Skills.Tick(Dt); }
    }

    static int Half(int v, float f) => Mathf.FloorToInt(v * f + 0.5f);

    static void Test()
    {
        Check(Math.Abs(ThemeSkills.SugarRushSeconds - 10f) < 1e-5f, "10 s");
        Check(Math.Abs(ThemeSkills.SugarRushScoreMultiplier - 2f) < 1e-5f && Math.Abs(ThemeSkills.SugarRushCoinMultiplier - 1.5f) < 1e-5f, "x2 / x1.5");

        // ---- merge score and coin, every level, active vs not
        foreach (bool active in new[] { false, true })
            for (int level = 0; level <= CatDatabase.MaxLevel; level++)
            {
                var s = new Sim();
                if (active) s.Skills.Press();
                s.Merge(level);
                int baseScore = level >= CatDatabase.MaxLevel ? Balance.Current.maxMergeScore : CatDatabase.Score[level + 1];
                int baseCoins = CatDatabase.MergeCoins(level >= CatDatabase.MaxLevel ? CatDatabase.MaxLevel + 1 : level + 1);
                Check(s.Score == (active ? baseScore * 2 : baseScore), "score, level " + level + " active=" + active);
                Check(s.Coins == (active ? Half(baseCoins, 1.5f) : baseCoins), "coins, level " + level + " active=" + active);
            }
        Check(new RewardMultiplier { SugarRushActive = true }.MergeCoins(1) == 2, "1 coin x1.5 rounds half up to 2");
        Check(new RewardMultiplier { SugarRushActive = true }.MergeCoins(3) == 5, "3 coins x1.5 rounds half up to 5");

        // ---- discovery reward: not multiplied while active
        var d = new Sim();
        d.Skills.Press();
        d.Discover(5);
        Check(d.Coins == CatDatabase.DiscoveryReward(5) && d.Score == 0, "discovery reward is not multiplied");

        // ---- gacha duplicate refund: a pure coin value, never routed through the multiplier
        var rewards = new RewardMultiplier { SugarRushActive = true };
        foreach (Rarity r in new[] { Rarity.Common, Rarity.Rare, Rarity.Legendary })
            Check(Cosmetics.DuplicateCoins(r) == Mathf.Max(0, Mathf.RoundToInt(Balance.Current.gachaCost * (r == Rarity.Legendary ? Balance.Current.dupRefundLegendary : r == Rarity.Rare ? Balance.Current.dupRefundRare : Balance.Current.dupRefundCommon))),
                  "gacha refund unchanged while Sugar Rush is active (" + r + ")");
        Check(rewards.SugarRushActive, "(the multiplier object is not consulted by the refund)");

        // ---- chain merge: every merge of the chain is multiplied once, and energy charges for each
        var c = new Sim();
        c.Skills.Press();
        c.Energy.Reset();                                                  // 30, to count the charge
        int expectScore = 0, expectCoins = 0;
        for (int level = 0; level < 5; level++)
        {
            c.Merge(level);
            expectScore += CatDatabase.Score[level + 1] * 2;
            expectCoins += Half(CatDatabase.MergeCoins(level + 1), 1.5f);
        }
        Check(c.Score == expectScore && c.Coins == expectCoins, "chain: each merge multiplied once");
        Check(c.Energy.Value == 60, "chain: 5 merges charged 5 x 6");

        // ---- pause: no ticks, no progress; resume continues
        var p = new Sim();
        p.Skills.Press();
        p.Frames(40);
        float left = p.Skills.TimedRemaining;
        p.Frames(500, paused: true);
        Check(p.Skills.TimedRemaining == left && p.Rewards.SugarRushActive, "pause stops the clock");
        p.Frames(39);
        p.Merge(2);
        Check(p.Rewards.SugarRushActive && p.Score == CatDatabase.Score[3] * 2, "still x2 one frame before the end");
        p.Frames(1);
        Check(!p.Skills.IsTimedActive && !p.Rewards.SugarRushActive, "ends after exactly 10 s of game time");
        int before = p.Score;
        p.Merge(2);
        Check(p.Score == before + CatDatabase.Score[3], "after expiry merges pay normally");

        // ---- no stacking, no refresh
        var n = new Sim();
        Check(n.Skills.Press() == SkillPress.Activated, "first activation");
        n.Frames(40);
        for (int i = 0; i < 20; i++) Check(n.Skills.Press() == SkillPress.Ignored, "second press is blocked while active");
        Check(Math.Abs(n.Skills.TimedRemaining - 5f) < 1e-4f, "not refreshed");
        n.Merge(3);
        Check(n.Score == CatDatabase.Score[4] * 2, "not x4: applied once");
        var handler = new SugarRushHandler(n.Rewards);
        Check(!handler.Activate(null), "handler itself refuses a second start");

        // ---- energy reaches 100 while Sugar Rush runs; stays 100; cannot restart until it ends; then can
        var e = new Sim();                                                 // 100
        Check(e.Skills.Press() == SkillPress.Activated && e.Energy.Value == 0, "used at 100, energy 0");
        for (int i = 0; i < 17; i++) e.Merge(0);
        Check(e.Energy.Value == 100, "17 merges during the effect refill to 100 (102 clamped)");
        e.Merge(0);
        Check(e.Energy.Value == 100 && e.Rewards.SugarRushActive, "stays 100, effect continues");
        Check(e.Skills.Press() == SkillPress.Ignored && e.Energy.Value == 100, "100 energy does not allow a new start while active");
        e.Frames(80);
        Check(!e.Rewards.SugarRushActive, "effect over");
        Check(e.Skills.Press() == SkillPress.Activated && e.Energy.Value == 0, "after it ended, 100 energy starts it again");

        // ---- Game Over and restart end it
        var g = new Sim();
        g.Skills.Press();
        g.Skills.Lock();
        Check(!g.Rewards.SugarRushActive, "Game Over ends Sugar Rush");
        var r2 = new Sim();
        r2.Skills.Press();
        r2.Skills.Begin(ThemeSkills.SugarRush, new SugarRushHandler(r2.Rewards));
        Check(!r2.Rewards.SugarRushActive, "restart ends Sugar Rush");
    }

    /// <summary>
    /// Structure guard: the multiplier is used in exactly one place (AddMergeReward), AddScore is only reached from it, and the
    /// discovery reward / gacha refund / cheat code never mention the multiplier.
    /// </summary>
    static void SourceGuard()
    {
        string root = Application.dataPath + "/Scripts/";
        string gm = File.ReadAllText(root + "Gameplay/GameManager.cs");
        Check(Count(gm, @"_rewards\.MergeScore\(") == 1, "GameManager applies the score multiplier once");
        Check(Count(gm, @"_rewards\.MergeCoins\(") == 1, "GameManager applies the coin multiplier once");
        Check(Count(gm, @"\bAddScore\(") == 2, "AddScore: one definition + one call (AddMergeReward)");
        Check(Count(gm, @"\bAddMergeReward\(") == 3, "AddMergeReward: one definition + two merge branches");
        Check(gm.Contains("AddCoins(reward);"), "discovery still pays AddCoins(reward) directly");
        foreach (string file in new[] { "Data/Cosmetics.cs", "Data/PlayerData.cs", "UI/CheatPanel.cs", "Menu/GachaPage.cs", "Data/CatDatabase.cs" })
            Check(!File.ReadAllText(root + file).Contains("RewardMultiplier"), file + " does not use the multiplier");
    }

    static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }
}
