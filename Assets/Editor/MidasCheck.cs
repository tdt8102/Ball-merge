using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks Midas on real Cat objects and the real coin balance (PlayerData): levels 0, 7 (sell) and 8 (not allowed), coins
/// before/after (3 + 2 * level), Sugar Rush active, destroyed target, double tap, cancel, Pause and Game Over.
/// Menu: Tools/Suika/Check Midas. Batch: -executeMethod MidasCheck.Run (exit code 1 on failure).
/// The real save is backed up first and restored afterwards.
/// </summary>
public static class MidasCheck
{
    const string SaveKey = "catcatcat.save", RunKey = "catcatcat.run";
    static readonly List<Cat> Made = new List<Cat>();

    [MenuItem("Tools/Suika/Check Midas")]
    public static void RunMenu()
    {
        if (Execute()) Debug.Log("[Midas] all checks passed");
    }

    public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

    static bool Execute()
    {
        string saveBackup = PlayerPrefs.GetString(SaveKey, null), runBackup = PlayerPrefs.GetString(RunKey, null);
        bool ok = true;
        try
        {
            Test();
            SourceGuard();
            Debug.Log("MIDAS_CHECK_OK");
        }
        catch (Exception e)
        {
            ok = false;
            Debug.LogError("Midas check failed: " + e.Message);
        }
        finally
        {
            foreach (var c in Made) if (c != null) UnityEngine.Object.DestroyImmediate(c.gameObject);
            Made.Clear();
            PlayerData.ResetAll();
            PlayerData.Blocked = false;
            if (saveBackup != null) PlayerPrefs.SetString(SaveKey, saveBackup);
            if (runBackup != null) PlayerPrefs.SetString(RunKey, runBackup);
            PlayerPrefs.Save();
            Reload();
        }
        return ok;
    }

    /// <summary>The board, energy, skill and the real coin balance, wired as GameManager wires them.</summary>
    sealed class Sim
    {
        public readonly List<Cat> Cats = new List<Cat>();
        public readonly SkillEnergy Energy = new SkillEnergy();
        public readonly RewardMultiplier Rewards = new RewardMultiplier();
        public readonly SkillRuntime Skills;
        public int Score, Sold, Paid;                  // Score must stay 0; Paid is every coin amount handed to the currency

        public Sim()
        {
            Energy.Reset();
            while (Energy.Value < Energy.Max) Energy.OnMerge(MergeSource.Normal);
            Skills = new SkillRuntime(Energy);
            Skills.Begin(ThemeSkills.Midas, new MidasHandler(c =>
            {
                bool ok = Midas.TrySell(Cats, c, go => UnityEngine.Object.DestroyImmediate(go), coins => { Paid += coins; PlayerData.AddCoins(coins); });
                if (ok) Sold++;
                return ok;
            }));
        }

        public Cat Add(int level, float x, float y)
        {
            var c = Cat.Create(level, new Vector2(x, y), null, false);
            Made.Add(c);
            Cats.Add(c);
            return c;
        }

        public SkillTap TapAt(float x, float y)
        {
            var cat = Eraser.CatAt(Cats, new Vector2(x, y));
            if (cat != null && (!cat.Dropped || cat.Merging)) return SkillTap.Ignored;
            return Skills.Tap(cat);
        }
    }

    static void Test()
    {
        Check(ThemeSkills.Midas.Kind == ThemeSkillKind.Target, "Midas is a targeted skill");
        Check(ThemeSkills.MidasCoin(0) == 3 && ThemeSkills.MidasCoin(7) == 17, "3 coins at level 0, 17 at level 7");
        for (int l = 0; l <= 7; l++) Check(ThemeSkills.CanMidas(l), "level " + l + " is sellable");
        Check(!ThemeSkills.CanMidas(8) && !ThemeSkills.CanMidas(9) && !ThemeSkills.CanMidas(10) && !ThemeSkills.CanMidas(-1), "levels 8+ are not sellable");

        Fresh();
        PlayerData.AddCoins(1000);

        // ---- every sellable level: coin balance before/after, cat gone, 100 energy once, nothing else
        for (int level = 0; level <= 7; level++)
        {
            var s = new Sim();
            var target = s.Add(level, 0f, 0f);
            var bystander = s.Add(0, 5f, 0f);
            int before = PlayerData.Coins;
            Check(s.Skills.Press() == SkillPress.EnteredTargeting && s.Energy.Value == 100, "target mode, level " + level);
            Check(s.TapAt(0f, 0f) == SkillTap.Activated, "sell level " + level);
            int expected = 3 + 2 * level;
            Check(PlayerData.Coins - before == expected, "level " + level + " pays " + expected + " (balance " + before + " -> " + PlayerData.Coins + ")");
            Check(s.Paid == expected && s.Sold == 1, "one payment of " + expected);
            Check(target == null && s.Cats.Count == 1 && s.Cats[0] == bystander, "the cat left the board, the other stays");
            Check(s.Energy.Value == 0, "exactly 100 spent, no merge charge");
            Check(s.Score == 0, "no score");
            Check(!s.Skills.IsTargeting, "mode ends");
        }

        // ---- level 8, 9, 10 cannot be chosen
        foreach (int level in new[] { 8, 9, 10 })
        {
            var s = new Sim();
            var cat = s.Add(level, 0f, 0f);
            int before = PlayerData.Coins;
            s.Skills.Press();
            Check(s.TapAt(0f, 0f) == SkillTap.InvalidTarget, "level " + level + " is an invalid target");
            Check(PlayerData.Coins == before && s.Energy.Value == 100 && s.Skills.IsTargeting && s.Cats[0] == cat && cat != null && s.Sold == 0, "level " + level + ": no coins, no cost, cat stays");
            Check(!Midas.TrySell(s.Cats, cat, go => { }, c => throw new Exception("paid for an invalid target")), "TrySell refuses level " + level);
        }

        // ---- Sugar Rush active: the sale is NOT a merge reward, so no x1.5
        {
            var s = new Sim();
            var rush = new SkillRuntime(new SkillEnergy());
            s.Rewards.SugarRushActive = true;                              // as if Sugar Rush were running
            Check(s.Rewards.MergeCoins(10) == 15, "(the multiplier would turn 10 merge coins into 15)");
            var cat = s.Add(7, 0f, 0f);
            int before = PlayerData.Coins;
            s.Skills.Press();
            s.TapAt(0f, 0f);
            Check(PlayerData.Coins - before == 17 && s.Paid == 17, "level 7 pays exactly 17 with Sugar Rush active, not 26");
            Check(s.Rewards.SugarRushActive, "the Sugar Rush state is untouched");
            var s2 = new Sim();
            s2.Rewards.SugarRushActive = true;
            s2.Add(0, 0f, 0f);
            before = PlayerData.Coins;
            s2.Skills.Press();
            s2.TapAt(0f, 0f);
            Check(PlayerData.Coins - before == 3, "level 0 pays exactly 3 with Sugar Rush active, not 5");
        }

        // ---- other invalid targets: merging cat, cat in hand, cat not on the board
        var o = new Sim();
        var merging = o.Add(2, 0f, 0f);
        merging.Merging = true;
        var held = Cat.Create(1, new Vector2(3f, 4.5f), null, true);
        Made.Add(held);
        o.Cats.Add(held);
        int coinsO = PlayerData.Coins;
        o.Skills.Press();
        Check(o.TapAt(0f, 0f) == SkillTap.Ignored && o.TapAt(3f, 4.5f) == SkillTap.Ignored && o.Energy.Value == 100 && PlayerData.Coins == coinsO, "merging cat / cat in hand: ignored, no coins, no cost");

        // ---- cancel: button again, empty space
        var c = new Sim();
        c.Add(3, 0f, 0f);
        int coinsC = PlayerData.Coins;
        c.Skills.Press();
        Check(c.Skills.Press() == SkillPress.CancelledTargeting && c.Energy.Value == 100 && PlayerData.Coins == coinsC, "button again cancels for free");
        c.Skills.Press();
        Check(c.TapAt(4f, 4f) == SkillTap.Cancelled && c.Energy.Value == 100 && PlayerData.Coins == coinsC && c.Cats.Count == 1, "empty space cancels for free");

        // ---- target destroyed meanwhile
        var d = new Sim();
        var gone = d.Add(4, 0f, 0f);
        d.Skills.Press();
        UnityEngine.Object.DestroyImmediate(gone.gameObject);
        int coinsD = PlayerData.Coins;
        Check(d.Skills.Tap(gone) != SkillTap.Activated && d.Energy.Value == 100 && PlayerData.Coins == coinsD, "destroyed target: no coins, no cost");
        Check(!Midas.TrySell(d.Cats, gone, go => { }, x => throw new Exception("paid for a destroyed cat")), "TrySell refuses a destroyed cat");

        // ---- double tap: one sale, one payment
        var t = new Sim();
        var once = t.Add(5, 0f, 0f);
        int coinsT = PlayerData.Coins;
        t.Skills.Press();
        Check(t.TapAt(0f, 0f) == SkillTap.Activated, "first tap sells");
        Check(t.TapAt(0f, 0f) == SkillTap.Ignored, "second tap is ignored");
        Check(PlayerData.Coins - coinsT == 13 && t.Paid == 13 && t.Energy.Value == 0, "paid once (13), 100 energy once");
        Check(!Midas.TrySell(t.Cats, once, go => { }, x => throw new Exception("paid twice")), "selling the same cat again pays nothing");

        // ---- Game Over while choosing
        var g = new Sim();
        g.Add(2, 0f, 0f);
        int coinsG = PlayerData.Coins;
        g.Skills.Press();
        g.Skills.Lock();
        g.Energy.Freeze();
        Check(g.TapAt(0f, 0f) == SkillTap.Ignored && g.Cats.Count == 1 && g.Energy.Value == 100 && PlayerData.Coins == coinsG && g.Skills.Press() == SkillPress.Ignored, "Game Over: no sale, no coins, no cost");

        // ---- Pause: no input is processed while paused (structure checked in SourceGuard); the choice survives it
        var p = new Sim();
        p.Add(2, 0f, 0f);
        int coinsP = PlayerData.Coins;
        p.Skills.Press();
        Check(p.Skills.IsTargeting && p.Cats.Count == 1 && PlayerData.Coins == coinsP, "paused: still choosing, nothing changed");
        Check(p.TapAt(0f, 0f) == SkillTap.Activated && PlayerData.Coins - coinsP == 7, "after resume the sale works (level 2 = 7 coins)");
    }

    /// <summary>SellCat must pay through AddCoins only, once, and touch nothing else.</summary>
    static void SourceGuard()
    {
        string gm = File.ReadAllText(Application.dataPath + "/Scripts/Gameplay/GameManager.cs");
        var m = Regex.Match(gm, @"bool SellCat\(Cat cat\)\s*\{(?<body>.*?)\n    \}", RegexOptions.Singleline);
        Check(m.Success, "found SellCat");
        string body = m.Groups["body"].Value;
        Check(Regex.Matches(body, @"\bAddCoins\b").Count == 1, "SellCat pays through AddCoins, once");
        foreach (string forbidden in new[] { "AddScore", "AddMergeReward", "_rewards", "NotifyAppear", "RecordSeen", "Merge(", "Energy", "PlayerData" })
            Check(!body.Contains(forbidden), "SellCat does not use " + forbidden);
        Check(Regex.Matches(gm, @"AddMergeReward\(").Count == 3, "the merge reward path is unchanged (one definition + two merge branches)");
        string midas = File.ReadAllText(Application.dataPath + "/Scripts/Gameplay/Midas.cs");
        Check(!midas.Contains("PlayerData") && !midas.Contains("RewardMultiplier"), "Midas.cs never edits coins directly or uses the multiplier");
        Check(Regex.IsMatch(gm, @"if \(IsGameOver \|\| Paused \|\| cat == null\) return false;\s*Vector2 pos = cat\.transform\.position;\s*if \(!Midas\.TrySell"), "SellCat refuses while paused or over");
    }

    static void Fresh()
    {
        PlayerData.ResetAll();
        PlayerData.Blocked = false;
        Reload();
    }

    static void Reload()
    {
        typeof(PlayerData).GetField("_d", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
        typeof(PlayerData).GetField("_runLoaded", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
        Cosmetics.Reload();
    }

    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }
}
