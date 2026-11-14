using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks Moonlight on the real DangerTimer + SkillRuntime, stepped in the same order as GameManager.Update:
/// timer nearly full / half / 1 second left, pause in the middle, expiry after 8 s, other pause reasons, button spam,
/// Game Over and restart. Menu: Tools/Suika/Check Moonlight. Batch: -executeMethod MoonlightCheck.Run (exit code 1 on failure).
/// Touches no save data.
/// </summary>
public static class MoonlightCheck
{
    const float Dt = 0.125f;                        // exact in binary: 8 s = 64 frames, no rounding drift

    [MenuItem("Tools/Suika/Check Moonlight")]
    public static void RunMenu()
    {
        if (Execute()) Debug.Log("[Moonlight] all checks passed");
    }

    public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

    static bool Execute()
    {
        try
        {
            Test();
            Debug.Log("MOONLIGHT_CHECK_OK");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Moonlight check failed: " + e.Message);
            return false;
        }
    }

    /// <summary>The parts of one run that Moonlight touches, stepped like GameManager.Update.</summary>
    sealed class Sim
    {
        public readonly DangerTimer Timer = new DangerTimer();
        public readonly SkillEnergy Energy = new SkillEnergy();
        public readonly SkillRuntime Skills;
        public bool Over;
        public readonly float Limit = Balance.Current.gameOverDelay;

        public Sim()
        {
            Energy.Reset();
            Skills = new SkillRuntime(Energy);
            Skills.Begin(ThemeSkills.Moonlight, new MoonlightHandler(Timer));
        }

        public void Charge() { while (Energy.Value < Energy.Max) Energy.OnMerge(MergeSource.Normal); }

        /// <summary>paused = Pause/Settings open: GameManager.Update returns before doing anything.</summary>
        public void Frame(bool danger, bool paused = false)
        {
            if (Over || paused) return;
            Timer.Step(danger, Dt);
            if (Timer.Reached(Limit)) { Over = true; Skills.Lock(); Energy.Freeze(); }   // GameOver()
            Skills.Tick(Dt);
        }

        public void Frames(int n, bool danger, bool paused = false) { for (int i = 0; i < n; i++) Frame(danger, paused); }
    }

    static void Test()
    {
        Check(Math.Abs(ThemeSkills.MoonlightSeconds - 8f) < 1e-5f, "Moonlight lasts 8 s");
        Check(Math.Abs(Balance.Current.gameOverDelay - 2f) < 1e-5f, "test assumes a 2 s game-over delay");

        // ---- timer with 1 s left / 0.5 s left / one frame left; activate; hold for exactly 8 s; then run on, never reset
        foreach (float remaining in new[] { 1.0f, 0.5f, Dt })
        {
            string tag = " (" + remaining + " s left)";
            var s = new Sim();
            s.Charge();
            int toHold = Mathf.RoundToInt((s.Limit - remaining) / Dt);
            s.Frames(toHold, danger: true);
            float held = s.Timer.Value;
            Check(!s.Over && Math.Abs(held - (s.Limit - remaining)) < 1e-4f, "setup" + tag);

            Check(s.Skills.Press() == SkillPress.Activated && s.Timer.Paused && s.Energy.Value == 0, "activates, spends energy" + tag);

            s.Frames(20, danger: true);                                    // 2.5 s of the effect
            float left = s.Skills.TimedRemaining;
            s.Frames(50, danger: true, paused: true);                      // Pause / Settings in the middle: nothing moves
            Check(s.Timer.Value == held && s.Skills.TimedRemaining == left && !s.Over, "pause menu freezes both" + tag);
            s.Frames(43, danger: true);                                    // resume: 20 + 43 = 63 frames done
            Check(s.Timer.Paused && s.Timer.Value == held && !s.Over, "still held at 7.875 s" + tag);
            s.Frame(danger: true);                                         // frame 64 = the 8 s mark
            Check(!s.Timer.Paused && !s.Skills.IsTimedActive, "effect over after 8 s" + tag);
            Check(s.Timer.Value == held && !s.Over, "timer resumes from the held value, not reset" + tag);

            int framesLeft = Mathf.RoundToInt(remaining / Dt);
            s.Frames(framesLeft - 1, danger: true);
            Check(!s.Over, "not over one frame early" + tag);
            s.Frame(danger: true);
            Check(s.Over, "game over exactly when the held timer fills" + tag);
        }

        // ---- the same timer without Moonlight loses at the same moment (the rule itself is unchanged)
        var plain = new Sim();
        int frames = 0;
        while (!plain.Over) { plain.Frame(true); frames++; }
        Check(frames == Mathf.RoundToInt(plain.Limit / Dt), "no skill: game over after the normal 2 s");

        // ---- board becomes safe during Moonlight: still held; after it ends the normal rule applies (safe = 0)
        var safe = new Sim();
        safe.Charge();
        safe.Frames(8, danger: true);                                      // 1 s
        safe.Skills.Press();
        safe.Frames(10, danger: false);
        Check(safe.Timer.Value == 1f && safe.Timer.Paused, "safe during Moonlight: value held, not reset");
        safe.Frames(54, danger: false);
        Check(!safe.Timer.Paused && safe.Timer.Value == 1f, "held value at the end");
        safe.Frame(danger: false);
        Check(safe.Timer.Value == 0f, "afterwards a safe board resets as always");

        // ---- other pause reasons: only resume when ALL are gone
        var multi = new Sim();
        multi.Charge();
        multi.Timer.Pause("other");
        multi.Skills.Press();
        multi.Frames(64, danger: true);
        Check(!multi.Skills.IsTimedActive && multi.Timer.Paused && multi.Timer.Value == 0f, "Moonlight ended but another reason still holds the timer");
        multi.Timer.Resume("other");
        multi.Frame(danger: true);
        Check(!multi.Timer.Paused && multi.Timer.Value == Dt, "runs once every reason is removed");
        var multi2 = new Sim();
        multi2.Charge();
        multi2.Skills.Press();
        multi2.Timer.Pause("other");
        multi2.Frames(70, danger: true);                                   // Moonlight over, "other" still on
        Check(multi2.Timer.Paused, "removing Moonlight does not release someone else's pause");
        multi2.Timer.Pause("other");
        multi2.Timer.Resume("other");
        Check(!multi2.Timer.Paused, "a reason is a set entry: pausing twice then resuming once releases it");

        // ---- button spam
        var spam = new Sim();
        spam.Charge();
        spam.Frames(8, danger: true);
        int activated = 0;
        for (int i = 0; i < 30; i++) if (spam.Skills.Press() == SkillPress.Activated) activated++;
        Check(activated == 1 && spam.Energy.Value == 0 && spam.Skills.TimedRemaining == 8f, "spam: one activation, effect not extended");
        spam.Frames(16, danger: true);
        for (int i = 0; i < 30; i++) spam.Skills.Press();
        Check(Math.Abs(spam.Skills.TimedRemaining - 6f) < 1e-4f && spam.Energy.Value == 0, "spam during the effect changes nothing");
        spam.Frames(48, danger: true);
        Check(!spam.Timer.Paused, "effect ended");
        for (int i = 0; i < 30; i++) Check(spam.Skills.Press() == SkillPress.NotEnoughEnergy, "spam after the effect: not enough energy");
        Check(!spam.Timer.Paused && spam.Energy.Value == 0, "spam leaves no pause behind");

        // ---- Game Over: no activation, effect stopped
        var over = new Sim();
        over.Charge();
        over.Frames(15, danger: true);
        over.Frame(danger: true);                                          // 2.0 s: game over
        Check(over.Over, "game over reached");
        over.Energy.OnMerge(MergeSource.Normal);
        Check(over.Skills.Press() == SkillPress.Ignored && !over.Timer.Paused && over.Energy.Value == 100, "no activation after Game Over");
        var mid = new Sim();
        mid.Charge();
        mid.Skills.Press();
        Check(mid.Timer.Paused, "pause on");
        mid.Skills.Lock();                                                 // run ends while Moonlight runs
        Check(!mid.Timer.Paused && !mid.Skills.IsTimedActive, "ending the run releases the pause");
        Check(mid.Skills.Press() == SkillPress.Ignored, "locked");

        // ---- restart during Moonlight
        var re = new Sim();
        re.Charge();
        re.Frames(8, danger: true);
        re.Skills.Press();
        re.Frames(10, danger: true);
        re.Timer.Reset();                                                  // GameManager.ResetBoard
        re.Energy.Reset();
        re.Skills.Begin(ThemeSkills.Moonlight, new MoonlightHandler(re.Timer));   // GameManager.NewGame
        Check(!re.Timer.Paused && re.Timer.Value == 0f && !re.Skills.IsTimedActive, "restart: no timer, no pause, no effect left");
        re.Frame(danger: true);
        Check(re.Timer.Value == Dt, "restart: timer runs normally");
        Check(re.Skills.Press() == SkillPress.NotEnoughEnergy, "restart: back to 30 energy");
    }

    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }
}
