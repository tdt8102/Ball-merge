using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Sections C (pause / resume), D (save and resume of a run) and E (Game Over / restart).</summary>
public partial class E2eRunner
{
    static string Source(string relative) => System.IO.File.ReadAllText(Application.dataPath + "/Scripts/" + relative);

    Cat Holder()
    {
        var h = MakeCat(1, 0f, 3.35f);                          // resting above the red line, held in place
        h.Body.isKinematic = true;
        return h;
    }

    // ------------------------------------------------------------------ C: pause

    IEnumerator PauseTests()
    {
        yield return Run("cat_candy");
        ClearBoard();
        GM.PressSkill();
        yield return new WaitForSeconds(1f);
        float r1 = GM.Skills.TimedRemaining;
        GM.Paused = true;                                       // what the settings popup does
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(1.5f);
        float r2 = GM.Skills.TimedRemaining;
        R("C1", "Pause (settings open) for 1.5 s: the timed effect does not tick", "remaining unchanged", r1.ToString("0.000") + " -> " + r2.ToString("0.000"), Mathf.Abs(r1 - r2) < 0.001f);
        GM.Paused = false;
        Time.timeScale = 1f;
        yield return new WaitForSeconds(1f);
        float r3 = GM.Skills.TimedRemaining;
        R("C2", "Resume: the effect continues from where it stopped", "about 1 s less", (r1 - r3).ToString("0.00") + " s elapsed", Mathf.Abs((r1 - r3) - 1f) < 0.2f);

        // danger timer + target mode across a pause
        yield return Run("cat_doodle");
        ClearBoard();
        var holder = Holder();
        var victim = MakeCat(3, 1.6f, -4.6f);
        float t = 0f;
        while (Danger.Value < 0.3f && t < 4f) { t += Time.deltaTime; yield return null; }
        GM.PressSkill();                                         // target mode
        GM.Paused = true;
        Time.timeScale = 0f;
        float d0 = Danger.Value;
        yield return new WaitForSecondsRealtime(1.5f);
        float d1 = Danger.Value;
        R("C3", "Pause: the red-line timer is frozen (no unfair Game Over while Settings is open)", "unchanged; not over", d0.ToString("0.00") + " -> " + d1.ToString("0.00") + "; over " + GM.IsGameOver, Mathf.Abs(d0 - d1) < 0.001f && !GM.IsGameOver);
        GM.PressSkill();                                         // the HUD button while paused
        R("C4", "Skill button pressed while paused is ignored (still choosing, nothing spent)", "targeting; 100", (GM.Skills.IsTargeting ? "targeting" : "idle") + "; " + GM.Energy.Value, GM.Skills.IsTargeting && GM.Energy.Value == 100);
        var tap = GM.Skills.Tap(victim);
        R("C5", "A target that gets through while paused does nothing and costs nothing", "Failed; cat still there; 100", tap + "; " + (victim != null ? "there" : "gone") + "; " + GM.Energy.Value,
            tap == SkillTap.Failed && victim != null && GM.Energy.Value == 100);
        GM.Paused = false;
        Time.timeScale = 1f;
        yield return Frames(3);
        R("C6", "Resume: still in target mode, and the target now works", "targeting", GM.Skills.IsTargeting ? "targeting" : "idle", GM.Skills.IsTargeting);
        GM.PressSkill();                                         // cancel
        Cats.Remove(holder);
        Destroy(holder.gameObject);
        yield return new WaitForSeconds(0.3f);
        R("C7", "Removing the cat above the line clears the danger timer", "0", Danger.Value, Danger.Value == 0f);
    }

    // ------------------------------------------------------------------ D: save / resume

    IEnumerator SaveResume()
    {
        yield return Run("cat_doodle", full: false);
        ClearBoard();
        yield return DoMerge(0, -2f);
        ClearBoard();
        for (int i = 0; i < 5; i++) GM.Energy.OnMerge(MergeSource.Normal);
        var seeds = new[] { MakeCat(1, -2f, -4.6f), MakeCat(2, -0.6f, -4.5f), MakeCat(4, 1.4f, -4.3f) };
        yield return new WaitForSeconds(1.2f);
        int score = GM.Score, energy = GM.Energy.Value;
        GM.SaveProgress();
        var run = PlayerData.Run;
        R("D1", "Save a run: energy, skill id, score and the 3 board cats are in the snapshot", "66; skill_eraser; recorded; 3 cats",
            (run == null ? "no run" : run.energy + "; " + run.activeSkillId + "; recorded=" + run.skillRecorded + "; " + run.cats.Count + " cats"),
            run != null && run.energy == 66 && run.activeSkillId == ThemeSkills.EraserId && run.skillRecorded && run.energyRecorded && run.cats.Count == 3 && run.score == score);

        // the player changes the outfit before continuing: the run keeps its skill
        Outfit("cat_classic");
        yield return LoadMain(true);
        yield return Frames(3);
        var saved = run;
        bool near = saved.cats.All(sc => Board().Any(c => c.Level == sc.l && Vector2.Distance(c.transform.position, new Vector2(sc.x, sc.y)) < 0.4f));
        R("D2", "Continue after changing the outfit to Classic: skill, energy, score and cats restored (the skill is not recomputed)",
            "Eraser; 66; score; 3 cats near saved spots",
            (GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id) + "; " + GM.Energy.Value + "; " + GM.Score + "; " + Board().Count + " cats; near=" + near,
            GM.ActiveSkill == ThemeSkills.Eraser && GM.Energy.Value == 66 && GM.Score == score && Board().Count == 3 && near);
        R("D3", "HUD after Continue shows the skill and the saved energy", "button; 66%", UI.SkillRect.gameObject.activeSelf + "; " + BadgeText().text, UI.SkillRect.gameObject.activeSelf && BadgeText().text == "66%");

        GM.NewGame();                                            // a NEW game uses the current outfit (Classic)
        yield return Frames(3);
        R("D4", "New game after that: current outfit (Classic) gives no skill, energy 30", "null; 30; button hidden", (GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id) + "; " + GM.Energy.Value + "; " + UI.SkillRect.gameObject.activeSelf,
            GM.ActiveSkill == null && GM.Energy.Value == 30 && !UI.SkillRect.gameObject.activeSelf);

        // a running timed effect is not saved
        yield return Run("cat_candy");
        ClearBoard();
        MakeCat(2, -1f, -4.6f);                                  // an empty board is not saved as a run
        yield return new WaitForSeconds(0.5f);
        GM.PressSkill();
        GM.SaveProgress();
        yield return LoadMain(true);
        R("D5", "Save during Sugar Rush: after Continue the effect is gone, the energy spent stays spent", "not timed; multiplier off; 0",
            (GM.Skills.IsTimedActive ? "timed" : "idle") + "; " + Get<RewardMultiplier>(GM, "_rewards").SugarRushActive + "; " + GM.Energy.Value,
            !GM.Skills.IsTimedActive && !Get<RewardMultiplier>(GM, "_rewards").SugarRushActive && GM.Energy.Value == 0);

        // saved while choosing a target
        yield return Run("cat_doodle");
        ClearBoard();
        MakeCat(2, 1f, -4.6f);
        GM.PressSkill();
        GM.SaveProgress();
        yield return LoadMain(true);
        R("D6", "Save while choosing a target: Continue is back to idle with the energy kept", "idle; 100", (GM.Skills.IsTargeting ? "targeting" : "idle") + "; " + GM.Energy.Value, !GM.Skills.IsTargeting && GM.Energy.Value == 100);

        // saved in the middle of Shrink: cats were saved small, come back at full size
        yield return Run("cat_chibi");
        ClearBoard();
        foreach (var x in new[] { -2.2f, -1.1f, 0f, 1.1f, 2.2f }) MakeCat(3, x, -4.5f);
        yield return new WaitForSeconds(0.8f);
        GM.PressSkill();
        yield return new WaitForSeconds(1.2f);
        GM.SaveProgress();
        yield return LoadMain(true);
        yield return new WaitForSeconds(3f);
        float fastest = Board().Count == 0 ? 0f : Board().Max(c => c.Speed);
        R("D7", "Save during Shrink, Continue: cats come back full size without being thrown out or ending the game",
            "5 cats, scale 1, inside tank, calm, not over", Board().Count + " cats; scale " + (Board().Count == 0 ? "-" : Board()[0].Scale.ToString("0.00")) + "; inside " + Board().All(InsideTank) + "; max speed " + fastest.ToString("0.0") + "; over " + GM.IsGameOver,
            Board().Count >= 4 && Board().All(c => c.Scale == 1f) && Board().All(InsideTank) && fastest < 3f && !GM.IsGameOver);
    }

    // ------------------------------------------------------------------ E: game over and restart

    IEnumerator GameOverTests()
    {
        yield return Run("cat_doodle");
        ClearBoard();
        Holder();
        GM.PressSkill();                                         // choosing a target when the game ends
        float t = 0f;
        while (!GM.IsGameOver && t < 6f) { t += Time.deltaTime; yield return null; }
        yield return Frames(3);
        int energy = GM.Energy.Value;
        int gained = GM.Energy.OnMerge(MergeSource.Normal);
        GM.PressSkill();
        var bg = UI.transform.Find("SkillButton/Body/Bg").GetComponent<Button>();
        R("E1", "Natural Game Over while choosing a target: skill locked, mode and dim off, energy frozen, button disabled",
            "over; locked; idle; dim off; +0; disabled",
            GM.IsGameOver + "; " + GM.Skills.Locked + "; " + (GM.Skills.IsTargeting ? "targeting" : "idle") + "; " + Get<SpriteRenderer>(GM, "_dim").enabled + "; +" + gained + "; interactable " + bg.interactable,
            GM.IsGameOver && GM.Skills.Locked && !GM.Skills.IsTargeting && !Get<SpriteRenderer>(GM, "_dim").enabled && gained == 0 && !bg.interactable && GM.Energy.Value == energy);
        R("E2", "Finished game cannot be continued; Game Over panel shown", "no saved run; panel on", (PlayerData.Run == null ? "no run" : "run exists") + "; " + UI.transform.Find("GameOver").gameObject.activeSelf,
            PlayerData.Run == null && UI.transform.Find("GameOver").gameObject.activeSelf);
        int merged = GM.Score;
        GM.Merge(MakeCat(0, 0, -4.5f), MakeCat(0, 0.1f, -4.5f));
        R("E3", "A merge after Game Over does nothing (score, energy)", "same", (GM.Score - merged) + "; +" + (GM.Energy.Value - energy), GM.Score == merged && GM.Energy.Value == energy);

        // timed effect running at Game Over
        yield return Run("cat_candy");
        ClearBoard();
        GM.PressSkill();
        Holder();
        t = 0f;
        while (!GM.IsGameOver && t < 6f) { t += Time.deltaTime; yield return null; }
        R("E4", "Game Over during Sugar Rush ends the effect", "multiplier off; not timed", Get<RewardMultiplier>(GM, "_rewards").SugarRushActive + "; " + GM.Skills.IsTimedActive,
            GM.IsGameOver && !Get<RewardMultiplier>(GM, "_rewards").SugarRushActive && !GM.Skills.IsTimedActive);

        // play again with another outfit
        Award("cat_pixel", 11);
        Outfit("cat_pixel");
        GM.NewGame();
        yield return Frames(3);
        R("E5", "Play again: skill re-resolved from the current outfit, energy 30, everything reset", "Level Up; 30; unlocked; board empty; danger 0; panel off",
            (GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id) + "; " + GM.Energy.Value + "; locked=" + GM.Skills.Locked + "; " + Board().Count + " cats; " + Danger.Value + "; " + UI.transform.Find("GameOver").gameObject.activeSelf,
            GM.ActiveSkill == ThemeSkills.LevelUp && GM.Energy.Value == 30 && !GM.Skills.Locked && Board().Count == 0 && Danger.Value == 0f && !UI.transform.Find("GameOver").gameObject.activeSelf && !GM.IsGameOver);

        // restart while choosing a target
        yield return Run("cat_doodle");
        GM.PressSkill();
        GM.NewGame();
        yield return Frames(3);
        R("E6", "Settings > Restart while choosing a target: idle, energy 30", "idle; 30; banner off", (GM.Skills.IsTargeting ? "targeting" : "idle") + "; " + GM.Energy.Value + "; " + UI.BannerRect.gameObject.activeSelf,
            !GM.Skills.IsTargeting && GM.Energy.Value == 30 && !UI.BannerRect.gameObject.activeSelf);

        // restart during Shrink
        yield return Run("cat_chibi");
        ClearBoard();
        MakeCat(2, 0f, -4.5f);
        GM.PressSkill();
        yield return new WaitForSeconds(0.4f);
        GM.NewGame();
        yield return new WaitForSeconds(0.5f);
        var fresh = MakeCat(2, 0f, -4.5f);
        R("E7", "Restart during Shrink: old cats gone, effect off, new cats are normal size", "0 old cats; inactive; scale 1",
            (Board().Count - 1) + " old cats; " + Get<ShrinkEffect>(GM, "_shrink").Active + "; " + fresh.Scale, Board().Count == 1 && !Get<ShrinkEffect>(GM, "_shrink").Active && fresh.Scale == 1f);
    }
}
