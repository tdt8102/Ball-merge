using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks Level Up on real Cat objects: levels 0, 8 (valid) and 9, 10 (invalid), sprite / collider / mass of the new level,
/// the run save, the Codex claim (first time only), a destroyed target, cancel and Game Over; energy only on success.
/// Menu: Tools/Suika/Check Level Up. Batch: -executeMethod LevelUpCheck.Run (exit code 1 on failure).
/// The real save is backed up first and restored afterwards.
/// </summary>
public static class LevelUpCheck
{
    const string SaveKey = "catcatcat.save", RunKey = "catcatcat.run";
    static readonly List<Cat> Made = new List<Cat>();

    [MenuItem("Tools/Suika/Check Level Up")]
    public static void RunMenu()
    {
        if (Execute()) Debug.Log("[LevelUp] all checks passed");
    }

    public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

    static bool Execute()
    {
        string saveBackup = PlayerPrefs.GetString(SaveKey, null), runBackup = PlayerPrefs.GetString(RunKey, null);
        bool ok = true;
        try
        {
            Test();
            Codex();
            SourceGuard();
            Debug.Log("LEVELUP_CHECK_OK");
        }
        catch (Exception e)
        {
            ok = false;
            Debug.LogError("LevelUp check failed: " + e.Message);
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
            ReloadPlayerData();
        }
        return ok;
    }

    sealed class Sim
    {
        public readonly List<Cat> Cats = new List<Cat>();
        public readonly SkillEnergy Energy = new SkillEnergy();
        public readonly SkillRuntime Skills;
        public int Score, Coins;                       // nothing in the upgrade path may touch these
        public int Upgrades;

        public Sim()
        {
            Energy.Reset();
            while (Energy.Value < Energy.Max) Energy.OnMerge(MergeSource.Normal);
            Skills = new SkillRuntime(Energy);
            Skills.Begin(ThemeSkills.LevelUp, new LevelUpHandler(c =>
            {
                var up = LevelUp.TryUpgrade(Cats, c, (level, pos) => Make(level, pos.x, pos.y, preview: false), go => UnityEngine.Object.DestroyImmediate(go));
                if (up == null) return false;
                Upgrades++;
                return true;
            }));
        }

        public Cat Add(int level, float x, float y)
        {
            var c = Make(level, x, y, preview: false);
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

    static Cat Make(int level, float x, float y, bool preview)
    {
        var c = Cat.Create(level, new Vector2(x, y), null, preview);
        Made.Add(c);
        return c;
    }

    static void Test()
    {
        Check(ThemeSkills.LevelUp.Kind == ThemeSkillKind.Target, "Level Up is a targeted skill");
        for (int l = 0; l <= 8; l++) Check(ThemeSkills.CanLevelUp(l), "level " + l + " is valid");
        Check(!ThemeSkills.CanLevelUp(9) && !ThemeSkills.CanLevelUp(10) && !ThemeSkills.CanLevelUp(-1), "levels 9, 10 and below 0 are not valid");

        // ---- level 0 and 8 upgrade by exactly one, with everything of the new level
        foreach (int level in new[] { 0, 3, 8 })
        {
            var s = new Sim();
            var old = s.Add(level, 1f, -2f);
            var bystander = s.Add(0, 5f, 0f);
            old.Velocity = new Vector2(1.5f, -2f);
            Quaternion oldRot = Quaternion.Euler(0f, 0f, 33f);
            old.transform.rotation = oldRot;
            int index = s.Cats.IndexOf(old);

            Check(s.Skills.Press() == SkillPress.EnteredTargeting && s.Energy.Value == 100, "target mode, level " + level);
            Check(s.TapAt(1f, -2f) == SkillTap.Activated, "upgrade level " + level);
            var up = s.Cats[index];
            Check(up != old && old == null, "the old cat is gone, level " + level);
            Check(up.Level == level + 1, "exactly one level up: " + level + " -> " + up.Level);
            Check(s.Cats.Count == 2 && s.Cats[1 - index] == bystander, "the board still has two cats, in the same order");
            Check(Vector2.Distance(up.transform.position, new Vector2(1f, -2f)) < 1e-4f, "same position");
            Check(Vector2.Distance(up.Velocity, new Vector2(1.5f, -2f)) < 1e-3f, "keeps its speed");
            Check(up.Dropped && !up.Merging, "the new cat is a normal board cat");
            Check(s.Energy.Value == 0, "exactly 100 spent, level " + level + " (no merge charge)");
            Check(s.Score == 0 && s.Coins == 0, "no merge score or coins, level " + level);
            Check(s.Upgrades == 1 && !s.Skills.IsTargeting, "one upgrade, mode ends");

            // sprite, collider, mass: all of the new level, like a merge-made cat
            float r = CatDatabase.Radius[level + 1];
            Check(Mathf.Approximately(up.Radius, r), "Radius of the new level");
            Check(Mathf.Approximately(up.GetComponent<SphereCollider>().radius, r), "collider radius of the new level");
            Check(Mathf.Abs(up.Body.mass - Mathf.PI * r * r) < 1e-4f, "mass of the new level");
            var sr = up.transform.Find("Visual").GetComponent<SpriteRenderer>();
            Check(sr.sprite == CatSprites.Get(level + 1), "sprite of the new level");
            Check(Mathf.Approximately(up.transform.Find("Visual").localScale.x, r * 2f), "visual scale of the new level");
            var plain = Make(level + 1, 9f, 9f, preview: false);
            Check(Mathf.Approximately(plain.Radius, up.Radius) && Mathf.Approximately(plain.Body.mass, up.Body.mass), "identical to a cat created for that level");
        }

        // ---- level 9 and 10 cannot be targeted: ignored, still choosing, nothing spent, cat unchanged
        foreach (int level in new[] { 9, 10 })
        {
            var s = new Sim();
            var cat = s.Add(level, 0f, 0f);
            s.Skills.Press();
            Check(s.TapAt(0f, 0f) == SkillTap.InvalidTarget, "level " + level + " is an invalid target");
            Check(s.Energy.Value == 100 && s.Skills.IsTargeting && cat.Level == level && s.Cats[0] == cat && s.Upgrades == 0, "level " + level + ": nothing changed, nothing spent");
            Check(LevelUp.TryUpgrade(s.Cats, cat, (l, p) => null, go => { }) == null, "TryUpgrade refuses level " + level);
        }

        // ---- other invalid targets
        var o = new Sim();
        var merging = o.Add(2, 0f, 0f);
        merging.Merging = true;
        var held = Make(1, 3f, 4.5f, preview: true);
        o.Cats.Add(held);
        o.Skills.Press();
        Check(o.TapAt(0f, 0f) == SkillTap.Ignored && o.TapAt(3f, 4.5f) == SkillTap.Ignored && o.Energy.Value == 100, "merging cat and cat in hand: ignored, no cost");
        Check(LevelUp.TryUpgrade(new List<Cat>(), Make(2, 20f, 20f, false), (l, p) => null, go => { }) == null, "a cat that is not on the board");

        // ---- cancel: button again and empty space are free
        var c = new Sim();
        c.Add(2, 0f, 0f);
        c.Skills.Press();
        Check(c.Skills.Press() == SkillPress.CancelledTargeting && c.Energy.Value == 100, "button again cancels for free");
        c.Skills.Press();
        Check(c.TapAt(4f, 4f) == SkillTap.Cancelled && c.Energy.Value == 100 && c.Cats[0].Level == 2, "empty space cancels for free");

        // ---- target destroyed meanwhile
        var d = new Sim();
        var gone = d.Add(4, 0f, 0f);
        d.Skills.Press();
        UnityEngine.Object.DestroyImmediate(gone.gameObject);
        Check(d.Skills.Tap(gone) != SkillTap.Activated && d.Energy.Value == 100 && d.Upgrades == 0, "destroyed target: nothing spent");
        Check(LevelUp.TryUpgrade(d.Cats, gone, (l, p) => null, go => { }) == null, "TryUpgrade refuses a destroyed cat");

        // ---- double tap: one upgrade, one payment
        var t = new Sim();
        t.Add(5, 0f, 0f);
        t.Skills.Press();
        Check(t.TapAt(0f, 0f) == SkillTap.Activated, "first tap");
        Check(t.TapAt(0f, 0f) == SkillTap.Ignored && t.Cats[0].Level == 6 && t.Energy.Value == 0, "second tap: nothing (still level 6)");

        // ---- Game Over while choosing
        var g = new Sim();
        g.Add(2, 0f, 0f);
        g.Skills.Press();
        g.Skills.Lock();
        g.Energy.Freeze();
        Check(g.TapAt(0f, 0f) == SkillTap.Ignored && g.Cats[0].Level == 2 && g.Energy.Value == 100 && g.Skills.Press() == SkillPress.Ignored, "Game Over: no upgrade, no cost");

        // ---- save/load: the run snapshot holds the NEW cat only (as GameManager.BuildSnapshot filters), and loads back
        var sv = new Sim();
        sv.Add(2, 0f, 0f);
        sv.Add(7, 3f, 0f);
        sv.Skills.Press();
        sv.TapAt(0f, 0f);
        var snap = new RunSnapshot();
        foreach (var cat in sv.Cats) if (cat != null && cat.Dropped && !cat.Merging) snap.cats.Add(new CatState { l = cat.Level, x = cat.transform.position.x, y = cat.transform.position.y });
        var back = JsonUtility.FromJson<RunSnapshot>(JsonUtility.ToJson(snap));
        Check(back.cats.Count == 2 && back.cats.Exists(k => k.l == 3) && back.cats.Exists(k => k.l == 7) && !back.cats.Exists(k => k.l == 2), "snapshot has level 3 and 7, not the old level 2");
        foreach (var k in back.cats) Check(k.l >= 0 && k.l <= CatDatabase.MaxLevel, "loaded level is valid");
        var resumed = Make(back.cats[0].l, 0f, 0f, preview: false);                       // what ResumeRun does
        Check(Mathf.Approximately(resumed.Radius, CatDatabase.Radius[resumed.Level]), "a resumed upgraded cat has the right radius");
    }

    /// <summary>The claim path of the Codex: what GameManager.NotifyAppear uses for a merge-made cat, the upgraded cat now uses too.</summary>
    static void Codex()
    {
        PlayerData.ResetAll();
        PlayerData.Blocked = false;
        ReloadPlayerData();
        int before = PlayerData.DiscoveredCount;
        Check(!PlayerData.IsDiscovered(6), "level 6 not discovered yet");
        Check(PlayerData.RecordSeen(6), "first appearance of level 6 is a discovery (reward claimable once)");
        Check(PlayerData.IsDiscovered(6) && PlayerData.Seen(6) == 1 && PlayerData.DiscoveredCount == before + 1, "Codex shows level 6");
        Check(!PlayerData.RecordSeen(6) && PlayerData.Seen(6) == 2, "second appearance: counted, but no new discovery (no second reward)");
        Check(CatDatabase.DiscoveryReward(6) > 0, "level 6 has a discovery reward");
    }

    /// <summary>UpgradeCat must reuse the shared claim path and must not pay or charge anything itself.</summary>
    static void SourceGuard()
    {
        string gm = File.ReadAllText(Application.dataPath + "/Scripts/Gameplay/GameManager.cs");
        var m = Regex.Match(gm, @"bool UpgradeCat\(Cat cat\)\s*\{(?<body>.*?)\n    \}", RegexOptions.Singleline);
        Check(m.Success, "found UpgradeCat");
        string body = m.Groups["body"].Value;
        Check(Regex.Matches(body, @"NotifyAppear\(").Count == 1, "UpgradeCat claims through NotifyAppear exactly once");
        foreach (string forbidden in new[] { "AddScore", "AddCoins", "AddMergeReward", "RecordSeen", "Merge(", "Energy", "DiscoveryReward" })
            Check(!body.Contains(forbidden), "UpgradeCat does not use " + forbidden);
        Check(Regex.Matches(gm, @"NotifyAppear\(").Count >= 4, "merge, spawn, upgrade and the definition all use NotifyAppear");
        string lu = File.ReadAllText(Application.dataPath + "/Scripts/Gameplay/LevelUp.cs");
        Check(lu.Contains("create(target.Level + 1"), "the new cat comes from the creation helper, one level up");
    }

    static void ReloadPlayerData()
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
