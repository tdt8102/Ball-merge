using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks Shrink on real Cat objects: sizes (visual, collider, radius; mass untouched), baseline restore, 6 s hold then
/// 1.5 s grow back, no stacking, repeated use, a cat on the red line, cats spawned during the effect, pause in the hold and
/// in the grow-back, restart, Game Over, and the optional danger hold during the grow-back (off by default).
/// Menu: Tools/Suika/Check Shrink. Batch: -executeMethod ShrinkCheck.Run (exit code 1 on failure).
/// Touches no save data.
/// </summary>
public static class ShrinkCheck
{
    const float Dt = 0.125f;                        // exact in binary: 6 s = 48 frames, 1.5 s = 12 frames
    static readonly List<Cat> Made = new List<Cat>();

    [MenuItem("Tools/Suika/Check Shrink")]
    public static void RunMenu()
    {
        if (Execute()) Debug.Log("[Shrink] all checks passed");
    }

    public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

    static bool Execute()
    {
        bool savedConfig = Balance.Current.shrinkRestorePausesDanger;
        try
        {
            Test();
            DangerHold();
            SourceGuard();
            Debug.Log("SHRINK_CHECK_OK");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Shrink check failed: " + e.Message);
            return false;
        }
        finally
        {
            Balance.Current.shrinkRestorePausesDanger = savedConfig;
            foreach (var c in Made) if (c != null) UnityEngine.Object.DestroyImmediate(c.gameObject);
            Made.Clear();
        }
    }

    sealed class Sim
    {
        public readonly List<Cat> Cats = new List<Cat>();
        public readonly SkillEnergy Energy = new SkillEnergy();
        public readonly SkillRuntime Skills;
        public readonly ShrinkEffect Fx = new ShrinkEffect();
        public readonly DangerTimer Danger = new DangerTimer();

        public Sim()
        {
            Charge();
            Skills = new SkillRuntime(Energy);
            Begin();
        }

        public void Begin() => Skills.Begin(ThemeSkills.Shrink, new ShrinkHandler(Fx, () => Fx.Sync(Skills, Cats, Danger)));
        public void Charge() { Energy.Reset(); while (Energy.Value < Energy.Max) Energy.OnMerge(MergeSource.Normal); }

        /// <summary>The creation policy of GameManager.NewCat.</summary>
        public Cat Spawn(int level, float x, float y, bool preview = false)
        {
            var c = Cat.Create(level, new Vector2(x, y), null, preview);
            Made.Add(c);
            if (!preview) c.Restore(0f, Vector2.zero, 0f);     // pop-in already finished, as it is 0.18 s after a real spawn
            Fx.ApplyTo(c);
            Cats.Add(c);
            return c;
        }

        /// <summary>One running frame in GameManager.Update order: danger step, skill clock, shrink sync.</summary>
        public void Frame(bool danger = true, bool paused = false)
        {
            if (paused) return;                                // Pause/Settings: GameManager.Update returns first
            Danger.Step(danger, Dt);
            Skills.Tick(Dt);
            Fx.Sync(Skills, Cats, Danger);
        }

        public void Frames(int n, bool danger = true, bool paused = false) { for (int i = 0; i < n; i++) Frame(danger, paused); }
    }

    static bool Same(float a, float b) => Mathf.Abs(a - b) < 1e-6f;

    /// <summary>The cat has the size of factor f: radius, collider, visual; the baseline and the mass never move.</summary>
    static void Sized(Cat c, float f, string where)
    {
        float baseR = CatDatabase.Radius[c.Level];
        Check(c.BaseRadius == baseR, "baseline untouched " + where);
        Check(Same(c.Radius, baseR * f), "radius " + c.Radius + " vs " + baseR * f + " " + where);
        Check(Same(c.GetComponent<SphereCollider>().radius, baseR * f), "collider radius " + where);
        Check(Same(c.transform.Find("Visual").localScale.x, 2f * baseR * f), "visual scale " + where);
        Check(Mathf.Abs(c.Body.mass - Mathf.PI * baseR * baseR) < 1e-5f, "mass is the full-size mass " + where);
    }

    static void AllSized(Sim s, float f, string where) { foreach (var c in s.Cats) Sized(c, f, where); }

    static void Exact(Sim s, string where)
    {
        foreach (var c in s.Cats)
        {
            Check(c.Radius == c.BaseRadius && c.Scale == 1f, "exactly the original radius " + where);
            Check(c.GetComponent<SphereCollider>().radius == c.BaseRadius, "exactly the original collider " + where);
            Check(Mathf.Approximately(c.transform.Find("Visual").localScale.x, 2f * c.BaseRadius), "original visual scale " + where);
        }
    }

    static void Test()
    {
        Check(Same(ThemeSkills.ShrinkScale, 0.85f) && Same(ThemeSkills.ShrinkHoldSeconds, 6f) && Same(ThemeSkills.ShrinkRestoreSeconds, 1.5f), "0.85 / 6 s / 1.5 s");
        Check(!ThemeSkills.ShrinkRestorePausesDanger, "the danger hold during the grow-back is OFF by default");
        Check(ThemeSkills.Shrink.Kind == ThemeSkillKind.Instant, "Shrink is an instant-use skill");

        // ---- few cats: small, medium, big
        var few = new Sim();
        few.Spawn(0, -2f, 0f); few.Spawn(5, 0f, 0f); few.Spawn(10, 3f, 0f);
        AllSized(few, 1f, "before");
        Check(few.Skills.Press() == SkillPress.Activated && few.Energy.Value == 0, "activates, spends 100");
        AllSized(few, 0.85f, "right after activation");

        // ---- hold for the full 6 s
        few.Frames(47);
        AllSized(few, 0.85f, "after 5.875 s");
        few.Frame();
        AllSized(few, 0.85f, "at 6 s (grow-back starts)");

        // ---- grow back linearly over 1.5 s
        float last = 0.85f;
        for (int i = 1; i <= 11; i++)
        {
            few.Frame();
            float expect = 0.85f + 0.15f * (i * Dt / 1.5f);
            AllSized(few, expect, "grow-back frame " + i);
            Check(few.Cats[0].Scale >= last, "never shrinks again while growing");
            last = few.Cats[0].Scale;
        }
        Check(few.Skills.IsTimedActive, "still running one frame before the end");
        few.Frame();
        Check(!few.Skills.IsTimedActive && !few.Fx.Active, "effect over after exactly 7.5 s");
        Exact(few, "after the effect");

        // ---- no stacking
        var st = new Sim();
        st.Spawn(3, 0f, 0f);
        st.Skills.Press();
        for (int i = 0; i < 30; i++) Check(st.Skills.Press() == SkillPress.Ignored, "pressing again is blocked");
        Check(!new ShrinkHandler(st.Fx, () => { }).Activate(null), "the handler refuses a second start");
        st.Fx.Sync(st.Skills, st.Cats, st.Danger); st.Fx.Sync(st.Skills, st.Cats, st.Danger); st.Fx.Sync(st.Skills, st.Cats, st.Danger);
        AllSized(st, 0.85f, "sync called many times: still 0.85, not 0.85^n");
        st.Cats[0].SetScale(0.85f); st.Cats[0].SetScale(0.85f);
        AllSized(st, 0.85f, "SetScale(0.85) twice");

        // ---- full board
        var full = new Sim();
        for (int i = 0; i < 60; i++) full.Spawn(i % 11, (i % 10 - 5) * 0.6f, (i / 10 - 3) * 0.7f);
        full.Skills.Press();
        AllSized(full, 0.85f, "full board shrunk");
        full.Frames(60);
        Check(!full.Skills.IsTimedActive, "full board: effect over");
        Exact(full, "full board restored");

        // ---- repeated use: baseline never drifts
        var rep = new Sim();
        rep.Spawn(2, 0f, 0f); rep.Spawn(7, 2f, 0f);
        for (int round = 0; round < 6; round++)
        {
            rep.Charge();
            Check(rep.Skills.Press() == SkillPress.Activated, "round " + round + " starts");
            AllSized(rep, 0.85f, "round " + round + " shrunk");
            rep.Frames(60);
            Exact(rep, "round " + round + " restored");
        }

        // ---- pause in the middle of the hold and of the grow-back
        var p = new Sim();
        p.Spawn(4, 0f, 0f);
        p.Skills.Press();
        p.Frames(20);
        float elapsed = p.Skills.TimedElapsed;
        p.Frames(300, paused: true);
        Check(p.Skills.TimedElapsed == elapsed && p.Cats[0].Scale == 0.85f, "pause in the hold: clock and size frozen");
        p.Frames(28);                                                   // 20 + 28 = 48 frames: grow-back about to start
        p.Frames(6);                                                    // mid grow-back (elapsed 6.75 s)
        float mid = p.Cats[0].Scale;
        Check(Same(mid, 0.925f), "mid grow-back is half way: " + mid);
        p.Frames(300, paused: true);
        Check(p.Cats[0].Scale == mid && p.Skills.IsTimedActive, "pause in the grow-back: frozen");
        p.Frames(5);
        Check(p.Skills.IsTimedActive, "still growing 0.125 s before the end");
        p.Frame();
        Check(!p.Skills.IsTimedActive, "total running time is still exactly 7.5 s");
        Exact(p, "after pauses");

        // ---- a cat on the red line (the rule of CheckDanger: y + radius > DangerY)
        float dangerY = (float)typeof(GameManager).GetField("DangerY", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
        var rl = new Sim();
        float baseR = CatDatabase.Radius[6];
        var edge = rl.Spawn(6, 0f, dangerY - baseR * 0.9f);              // top 10% of the radius above the line
        Func<bool> above = () => edge.transform.position.y + edge.Radius > dangerY;
        Check(above(), "the cat pokes over the red line at full size");
        rl.Skills.Press();
        Check(!above(), "shrunk, it is under the line");
        rl.Frames(48);
        Check(!above(), "still under at the end of the hold");
        bool crossed = false;
        for (int i = 0; i < 12; i++) { rl.Frame(); if (above()) crossed = true; }
        Check(crossed && above() && !rl.Skills.IsTimedActive, "it comes back over the line as it grows back, and ends over it");

        // ---- cats created during the effect take the current factor, in the hold and in the grow-back
        var sp = new Sim();
        sp.Spawn(1, 0f, 0f);
        sp.Skills.Press();
        sp.Frames(10);
        var inHold = sp.Spawn(4, 1f, 0f);
        var held = sp.Spawn(2, 2f, 4.5f, preview: true);
        Check(inHold.Scale == 0.85f && held.Scale == 0.85f, "a cat created in the hold is shrunk");
        Sized(inHold, 0.85f, "spawned in the hold");
        sp.Frames(38 + 6);                                              // to the middle of the grow-back
        var inRestore = sp.Spawn(3, -1f, 0f);
        Check(Same(inRestore.Scale, sp.Fx.Factor) && Same(inRestore.Scale, 0.925f), "a cat created in the grow-back takes the current factor");
        AllSized(sp, sp.Fx.Factor, "all cats share one factor");
        sp.Frames(6);
        Exact(sp, "everything, spawned cats included, ends at the original size");
        var after = sp.Spawn(5, 0f, 2f);
        Check(after.Scale == 1f && after.Radius == after.BaseRadius, "a cat created after the effect is normal");

        // ---- a cat that is still popping in when the effect changes its size: Update scales the visual from Radius
        var popping = Cat.Create(4, Vector2.zero, null, false);
        Made.Add(popping);
        popping.SetScale(0.85f);
        typeof(Cat).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(popping, null);
        Sized(popping, 0.85f, "popping cat after one Update");
        popping.SetScale(1f);
        typeof(Cat).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(popping, null);
        Sized(popping, 1f, "popping cat back to normal");

        // ---- restart during the effect
        var rs = new Sim();
        rs.Spawn(3, 0f, 0f);
        rs.Skills.Press();
        rs.Frames(10);
        rs.Charge();
        rs.Begin();                                                     // GameManager.NewGame
        Check(!rs.Fx.Active && !rs.Skills.IsTimedActive, "restart ends the effect");
        Exact(rs, "restart");
        var fresh = rs.Spawn(4, 0f, 0f);
        Check(fresh.Scale == 1f, "after a restart new cats are normal");

        // ---- Game Over during the effect (also during the grow-back)
        foreach (int framesBefore in new[] { 10, 54 })
        {
            var go = new Sim();
            go.Spawn(3, 0f, 0f); go.Spawn(8, 2f, 0f);
            go.Skills.Press();
            go.Frames(framesBefore);
            go.Skills.Lock();
            Check(!go.Fx.Active && !go.Skills.IsTimedActive, "Game Over ends the effect (" + framesBefore + ")");
            Exact(go, "Game Over (" + framesBefore + ")");
            go.Energy.Freeze();
            Check(go.Skills.Press() == SkillPress.Ignored, "locked after Game Over");
        }
    }

    /// <summary>Config shrinkRestorePausesDanger: off = the red-line timer runs during the grow-back; on = held, only then.</summary>
    static void DangerHold()
    {
        foreach (bool on in new[] { false, true })
        {
            Balance.Current.shrinkRestorePausesDanger = on;
            var s = new Sim();
            s.Spawn(3, 0f, 0f);
            s.Skills.Press();
            s.Frames(40);
            Check(!s.Danger.Paused && Same(s.Danger.Value, 5f), "no danger hold in the shrunk part, whatever the config (on=" + on + ")");
            s.Frames(8);                                                 // 6 s: grow-back starts
            float atStart = s.Danger.Value;
            s.Frames(6);
            Check(s.Danger.Paused == on, "during the grow-back the danger timer is held only if configured (on=" + on + ")");
            Check(on ? s.Danger.Value == atStart : s.Danger.Value > atStart, "danger value " + (on ? "frozen" : "keeps running") + " (on=" + on + ")");
            s.Frames(6);
            Check(!s.Skills.IsTimedActive && !s.Danger.Paused, "released when the grow-back ends (on=" + on + ")");
            if (on)
            {
                var o = new Sim();
                o.Skills.Press();
                o.Frames(54);
                Check(o.Danger.Paused, "held mid grow-back");
                o.Skills.Lock();
                Check(!o.Danger.Paused, "Game Over releases the hold");
            }
        }
        Balance.Current.shrinkRestorePausesDanger = false;
    }

    /// <summary>Every cat is created in one place; sizes are set absolutely; nothing in Shrink touches mass.</summary>
    static void SourceGuard()
    {
        string root = Application.dataPath + "/Scripts/Gameplay/";
        string gm = File.ReadAllText(root + "GameManager.cs");
        Check(Regex.Matches(gm, @"Cat\.Create\(").Count == 1, "GameManager creates cats in exactly one place (NewCat)");
        Check(Regex.Matches(gm, @"NewCat\(").Count == 5, "NewCat: one definition + spawn, resume, merge, Level Up");
        Check(Regex.IsMatch(gm, @"Skills\.Tick\(Time\.deltaTime\);\s*ApplyShrink\(\);"), "the shrink follows the skill clock inside the running part of Update");

        string cat = File.ReadAllText(root + "Cat.cs");
        var m = Regex.Match(cat, @"public void SetScale\(float scale\)\s*\{(?<body>.*?)\n    \}", RegexOptions.Singleline);
        Check(m.Success, "found SetScale");
        string body = m.Groups["body"].Value;
        Check(body.Contains("BaseRadius * scale") && !body.Contains("Radius *= ") && !body.Contains("mass"), "SetScale is absolute on BaseRadius and leaves the mass alone");
        Check(!File.ReadAllText(root + "Shrink.cs").Contains("mass"), "Shrink.cs never touches mass");
    }

    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }
}
