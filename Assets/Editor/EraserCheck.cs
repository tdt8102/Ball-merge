using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks Eraser on real Cat objects: small and big cats, touching cats, a cat in a collision, empty space, a destroyed
/// cat, double tap, Pause and Game Over. Energy is spent exactly once and only on success; no reward, merge or charge.
/// Menu: Tools/Suika/Check Eraser. Batch: -executeMethod EraserCheck.Run (exit code 1 on failure).
/// Touches no save data.
/// </summary>
public static class EraserCheck
{
    [MenuItem("Tools/Suika/Check Eraser")]
    public static void RunMenu()
    {
        if (Execute()) Debug.Log("[Eraser] all checks passed");
    }

    public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

    static readonly List<Cat> Made = new List<Cat>();

    static bool Execute()
    {
        try
        {
            Test();
            SourceGuard();
            Debug.Log("ERASER_CHECK_OK");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Eraser check failed: " + e.Message);
            return false;
        }
        finally
        {
            foreach (var c in Made) if (c != null) UnityEngine.Object.DestroyImmediate(c.gameObject);
            Made.Clear();
        }
    }

    /// <summary>The board, the energy and the skill runtime the way GameManager wires them (rewards would show up in Score/Coins).</summary>
    sealed class Sim
    {
        public readonly List<Cat> Cats = new List<Cat>();
        public readonly SkillEnergy Energy = new SkillEnergy();
        public readonly SkillRuntime Skills;
        public int Score, Coins;                      // nothing in the erase path may touch these

        public Sim()
        {
            Energy.Reset();
            while (Energy.Value < Energy.Max) Energy.OnMerge(MergeSource.Normal);
            Skills = new SkillRuntime(Energy);
            Skills.Begin(ThemeSkills.Eraser, new EraserHandler(c => Eraser.TryErase(Cats, c, go => UnityEngine.Object.DestroyImmediate(go))));
        }

        public Cat Add(int level, float x, float y)
        {
            var c = Cat.Create(level, new Vector2(x, y), null, preview: false);
            Made.Add(c);
            Cats.Add(c);
            return c;
        }

        /// <summary>Press the button, tap a point (like GameManager.HandleTargetInput) and report what the tap did.</summary>
        public SkillTap TapAt(float x, float y)
        {
            var cat = Eraser.CatAt(Cats, new Vector2(x, y));
            if (cat != null && (!cat.Dropped || cat.Merging)) return SkillTap.Ignored;
            return Skills.Tap(cat);
        }
    }

    static void Test()
    {
        Check(ThemeSkills.Eraser.Kind == ThemeSkillKind.Target, "Eraser is a targeted skill");

        // ---- every level, small to big: erased, nothing else changes, 100 energy spent once
        for (int level = 0; level <= CatDatabase.MaxLevel; level++)
        {
            var s = new Sim();
            var target = s.Add(level, 0f, 0f);
            var bystander = s.Add(0, 5f, 0f);
            Check(s.Skills.Press() == SkillPress.EnteredTargeting && s.Energy.Value == 100, "target mode, level " + level);
            Check(s.TapAt(0f, 0f) == SkillTap.Activated, "erase level " + level);
            Check(s.Energy.Value == 0, "exactly 100 spent, level " + level + " (no merge charge)");
            Check(!s.Cats.Contains(target) && s.Cats.Contains(bystander) && s.Cats.Count == 1, "only the target left the board, level " + level);
            Check(target == null, "the object is gone, level " + level);
            Check(s.Score == 0 && s.Coins == 0, "no reward, level " + level);
            Check(!s.Skills.IsTargeting, "mode ends after the erase, level " + level);
        }

        // ---- cats close to each other: the one under the finger goes, its neighbour stays and nothing merges
        var n = new Sim();
        var left = n.Add(3, 0f, 0f);
        var right = n.Add(3, 0.9f, 0f);                                     // same level and touching (radius 0.48 each)
        n.Skills.Press();
        Check(Eraser.CatAt(n.Cats, new Vector2(0.1f, 0f)) == left, "pick: nearest centre, left");
        Check(Eraser.CatAt(n.Cats, new Vector2(0.8f, 0f)) == right, "pick: nearest centre, right");
        Check(Eraser.CatAt(n.Cats, new Vector2(0.45f, 0f)) != null, "pick: the overlap point still hits a cat");
        Check(n.TapAt(0.8f, 0f) == SkillTap.Activated, "erase the right cat");
        Check(right == null && left != null && n.Cats.Count == 1 && !left.Merging && left.Level == 3, "the neighbour is untouched and not merged");
        Check(n.Energy.Value == 0 && n.Score == 0, "no charge, no reward");

        // ---- a cat that is moving / in a collision is erased like any other (and is flagged so no merge can follow)
        var m = new Sim();
        var moving = m.Add(2, 0f, 0f);
        var twin = m.Add(2, 0.8f, 0f);
        moving.Velocity = new Vector2(3f, -5f);
        var other = m.Add(1, 2f, 0f);
        m.Skills.Press();
        Cat captured = moving;
        bool flaggedBeforeDestroy = false;
        var m2 = new Sim();                                                // same, but watch the flags at destroy time
        var colliding = m2.Add(2, 0f, 0f);
        m2.Add(2, 0.8f, 0f);
        Check(Eraser.TryErase(m2.Cats, colliding, go => { flaggedBeforeDestroy = colliding.Merging && !go.activeSelf; UnityEngine.Object.DestroyImmediate(go); }),
              "erase a colliding cat");
        Check(flaggedBeforeDestroy, "flagged Merging and switched off before it is destroyed (blocks a queued collision callback)");
        Check(m.TapAt(0f, 0f) == SkillTap.Activated && captured == null && m.Cats.Count == 2 && twin != null && other != null, "moving cat erased");

        // ---- a cat that is already merging is not a valid target: nothing spent, still choosing
        var g = new Sim();
        var merging = g.Add(4, 0f, 0f);
        merging.Merging = true;
        g.Skills.Press();
        Check(g.TapAt(0f, 0f) == SkillTap.Ignored && g.Energy.Value == 100 && g.Cats.Contains(merging) && g.Skills.IsTargeting, "merging cat: ignored, no cost");
        Check(!Eraser.TryErase(g.Cats, merging, go => { }), "TryErase refuses a merging cat");

        // ---- the cat in hand (not dropped yet) is not on the board
        var h = new Sim();
        var held = Cat.Create(1, new Vector2(0f, 4.5f), null, preview: true);
        Made.Add(held);
        h.Cats.Add(held);
        h.Skills.Press();
        Check(h.TapAt(0f, 4.5f) == SkillTap.Ignored && h.Cats.Contains(held) && h.Energy.Value == 100, "the cat in hand cannot be erased");

        // ---- empty space: cancels for free
        var e = new Sim();
        e.Add(2, 0f, 0f);
        e.Skills.Press();
        Check(e.TapAt(3f, 3f) == SkillTap.Cancelled && e.Energy.Value == 100 && e.Cats.Count == 1 && !e.Skills.IsTargeting, "tap on empty space: cancel, no cost");

        // ---- cancel by pressing the button again
        e.Skills.Press();
        Check(e.Skills.Press() == SkillPress.CancelledTargeting && e.Energy.Value == 100 && e.Cats.Count == 1, "button again: cancel, no cost");

        // ---- a cat that was destroyed meanwhile: no cost
        var d = new Sim();
        var gone = d.Add(5, 0f, 0f);
        d.Skills.Press();
        UnityEngine.Object.DestroyImmediate(gone.gameObject);
        Check(Eraser.CatAt(d.Cats, Vector2.zero) == null, "a destroyed cat is not picked");
        Check(d.Skills.Tap(gone) != SkillTap.Activated && d.Energy.Value == 100, "tap on a destroyed cat: nothing spent");
        Check(!Eraser.TryErase(d.Cats, gone, go => { }), "TryErase refuses a destroyed cat");

        // ---- double tap on the same cat: one erase, one payment
        var t = new Sim();
        var once = t.Add(6, 0f, 0f);
        t.Skills.Press();
        Check(t.TapAt(0f, 0f) == SkillTap.Activated && t.Energy.Value == 0, "first tap erases");
        Check(t.TapAt(0f, 0f) == SkillTap.Ignored && t.Energy.Value == 0, "second tap: nothing, nothing spent");
        Check(t.Skills.Press() == SkillPress.NotEnoughEnergy && t.Energy.Value == 0, "button right after: not enough energy");
        Check(!Eraser.TryErase(t.Cats, once, go => { }), "erasing the same cat twice fails");
        var twice = new Sim();
        var c1 = twice.Add(1, 0f, 0f);
        Check(Eraser.TryErase(twice.Cats, c1, go => { }) , "first TryErase works");
        Check(!Eraser.TryErase(twice.Cats, c1, go => { }), "second TryErase on the same cat fails (already off the board)");

        // ---- Pause: GameManager does not process input (and ignores the button) while paused; the choice survives
        var p = new Sim();
        p.Add(2, 0f, 0f);
        p.Skills.Press();
        // (paused: no TapAt calls happen)
        Check(p.Skills.IsTargeting && p.Cats.Count == 1 && p.Energy.Value == 100, "paused: still choosing, nothing changed");
        Check(p.TapAt(0f, 0f) == SkillTap.Activated && p.Cats.Count == 0, "after resume the erase works");

        // ---- Game Over while choosing: locked, nothing erased, nothing spent
        var o = new Sim();
        o.Add(2, 0f, 0f);
        o.Skills.Press();
        o.Skills.Lock();
        o.Energy.Freeze();
        Check(o.TapAt(0f, 0f) == SkillTap.Ignored && o.Cats.Count == 1 && o.Energy.Value == 100, "Game Over: no erase, no cost");
        Check(o.Skills.Press() == SkillPress.Ignored, "Game Over: button locked");
    }

    /// <summary>EraseCat must stay a pure removal: it may not touch score, coins, discovery, merge or energy.</summary>
    static void SourceGuard()
    {
        string gm = File.ReadAllText(Application.dataPath + "/Scripts/Gameplay/GameManager.cs");
        var m = Regex.Match(gm, @"bool EraseCat\(Cat cat\)\s*\{(?<body>.*?)\n    \}", RegexOptions.Singleline);
        Check(m.Success, "found EraseCat");
        string body = m.Groups["body"].Value;
        foreach (string forbidden in new[] { "AddScore", "AddCoins", "AddMergeReward", "NotifyAppear", "Merge(", "Energy", "RecordSeen" })
            Check(!body.Contains(forbidden), "EraseCat does not use " + forbidden);
        Check(Regex.IsMatch(gm, @"public void PressSkill\(\)\s*\{\s*if \(IsGameOver \|\| Paused\) return;"), "the skill button is ignored while paused or over");
        Check(Regex.IsMatch(gm, @"if \(IsGameOver \|\| Paused\) return;[\s\S]*?HandleInput\(\);"), "target taps are only read while the game runs");
    }

    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }
}
