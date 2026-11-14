using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks the skill activation framework with a mock handler (no real skill): instant and targeted activation, energy
/// only spent on success, free cancel, invalid targets, timed effect (game-time ticks), Game Over lock, restart.
/// Menu: Tools/Suika/Check Skill Runtime. Batch: -executeMethod SkillRuntimeCheck.Run (exit code 1 on failure).
/// Touches no save data.
/// </summary>
public static class SkillRuntimeCheck
{
    sealed class Mock : ISkillHandler
    {
        public float DurationSeconds;
        public bool ActivateResult = true;
        public Cat Valid;
        public int Activated, Ended;
        public Cat LastTarget;
        public float Duration => DurationSeconds;
        public bool CanTarget(Cat target) => target == Valid;
        public bool Activate(Cat target) { if (!ActivateResult) return false; Activated++; LastTarget = target; return true; }
        public void End() => Ended++;
    }

    [MenuItem("Tools/Suika/Check Skill Runtime")]
    public static void RunMenu()
    {
        if (Execute()) Debug.Log("[SkillRuntime] all checks passed");
    }

    public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

    static bool Execute()
    {
        var made = new System.Collections.Generic.List<GameObject>();
        try
        {
            Test(() => { var g = new GameObject("cat"); made.Add(g); return g.AddComponent<Cat>(); });
            Debug.Log("SKILLRUNTIME_CHECK_OK");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("SkillRuntime check failed: " + e.Message);
            return false;
        }
        finally
        {
            foreach (var g in made) if (g != null) UnityEngine.Object.DestroyImmediate(g);
        }
    }

    static SkillEnergy Full()
    {
        var e = new SkillEnergy();
        e.Reset();
        while (e.Value < e.Max) e.OnMerge(MergeSource.Normal);
        return e;
    }

    static void Test(Func<Cat> newCat)
    {
        var instant = ThemeSkills.SugarRush;      // Kind = Instant
        var target = ThemeSkills.Eraser;          // Kind = Target

        // ---- no skill / no handler
        var energy = Full();
        var rt = new SkillRuntime(energy);
        rt.Begin(null, null);
        Check(!rt.Available && rt.Press() == SkillPress.Ignored && !rt.Ready, "no skill: nothing works");
        rt.Begin(instant, null);
        Check(rt.Available && rt.Press() == SkillPress.NotImplemented && energy.Value == 100, "no handler: not implemented, no cost");

        // ---- instant: not enough energy, then success, then failure
        var m = new Mock();
        energy = new SkillEnergy();
        energy.Reset();                                                   // 30
        rt = new SkillRuntime(energy);
        rt.Begin(instant, m);
        Check(!rt.Ready && rt.Press() == SkillPress.NotEnoughEnergy && energy.Value == 30 && m.Activated == 0, "not enough energy");
        energy = Full();
        rt = new SkillRuntime(energy);
        rt.Begin(instant, m);
        m.ActivateResult = false;
        Check(rt.Ready && rt.Press() == SkillPress.Failed && energy.Value == 100, "failed activation costs nothing");
        m.ActivateResult = true;
        Check(rt.Press() == SkillPress.Activated && energy.Value == 0 && m.Activated == 1 && m.LastTarget == null, "instant success spends 100");
        Check(!rt.IsTargeting, "instant never enters target mode");

        // ---- timed effect: runs on game-time ticks, stops when the caller stops ticking (Pause), ends once
        m = new Mock { DurationSeconds = 10f };
        energy = Full();
        rt = new SkillRuntime(energy);
        rt.Begin(instant, m);
        Check(rt.Press() == SkillPress.Activated && rt.IsTimedActive && rt.TimedRemaining == 10f, "timed effect starts");
        rt.Tick(4f);
        Check(Math.Abs(rt.TimedRemaining - 6f) < 1e-4f, "tick advances");
        for (int i = 0; i < 100; i++) { /* Pause/Settings: GameManager does not call Tick */ }
        Check(Math.Abs(rt.TimedRemaining - 6f) < 1e-4f && m.Ended == 0, "no ticks, no progress (pause)");
        rt.Tick(0f);
        Check(Math.Abs(rt.TimedRemaining - 6f) < 1e-4f, "a zero-length frame changes nothing");
        Check(energy.OnMerge(MergeSource.Normal) == 6, "normal merge still charges while the effect runs");
        Check(rt.Press() == SkillPress.Ignored || rt.Press() == SkillPress.NotEnoughEnergy, "cannot start again while it runs");
        rt.Tick(6f);
        Check(!rt.IsTimedActive && m.Ended == 1, "effect ends once");
        rt.Tick(5f);
        Check(m.Ended == 1, "no second End");

        // ---- Game Over during a timed effect: effect stops, activation locked
        m = new Mock { DurationSeconds = 10f };
        energy = Full();
        rt = new SkillRuntime(energy);
        rt.Begin(instant, m);
        rt.Press();
        energy = Full();                                                  // (even with energy again)
        rt = new SkillRuntime(energy);
        rt.Begin(instant, m);
        rt.Press();
        rt.Lock();
        Check(!rt.IsTimedActive && m.Ended >= 1, "Game Over stops the effect");
        int ended = m.Ended;
        rt.Tick(1f);
        Check(m.Ended == ended, "no more ticks after Game Over");
        energy.Freeze();
        Check(rt.Press() == SkillPress.Ignored && !rt.Ready, "Game Over locks activation");

        // ---- restart: a new Begin clears lock, target mode and a running effect
        rt.Begin(instant, new Mock());
        Check(!rt.Locked && !rt.IsTimedActive && !rt.IsTargeting, "restart clears the state");

        // ---- targeted skill
        var valid = newCat();
        var other = newCat();
        m = new Mock { Valid = valid };
        energy = Full();
        rt = new SkillRuntime(energy);
        rt.Begin(target, m);
        Check(rt.Press() == SkillPress.EnteredTargeting && rt.IsTargeting && energy.Value == 100, "enters target mode, no cost yet");
        Check(rt.Press() == SkillPress.CancelledTargeting && !rt.IsTargeting && energy.Value == 100, "pressing the button again cancels for free");
        rt.Press();
        Check(rt.Tap(null) == SkillTap.Cancelled && !rt.IsTargeting && energy.Value == 100, "tap outside cancels for free");
        rt.Press();
        Check(rt.Tap(other) == SkillTap.InvalidTarget && rt.IsTargeting && energy.Value == 100 && m.Activated == 0, "invalid target: ignored, still choosing, no cost");
        m.ActivateResult = false;
        Check(rt.Tap(valid) == SkillTap.Failed && rt.IsTargeting && energy.Value == 100, "failed activation on a valid target: no cost");
        m.ActivateResult = true;
        Check(rt.Tap(valid) == SkillTap.Activated && !rt.IsTargeting && energy.Value == 0 && m.LastTarget == valid, "valid target: used, spends 100");
        Check(rt.Tap(valid) == SkillTap.Ignored, "taps outside target mode are ignored");

        // ---- Game Over while choosing a target
        energy = Full();
        rt = new SkillRuntime(energy);
        rt.Begin(target, new Mock { Valid = valid });
        rt.Press();
        rt.Lock();
        Check(!rt.IsTargeting && rt.Tap(valid) == SkillTap.Ignored && energy.Value == 100, "Game Over leaves target mode, nothing spent");
    }

    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }
}
