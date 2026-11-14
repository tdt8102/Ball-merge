using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>Section B: each of the 7 skills in the real game (real physics, real input entry points).</summary>
public partial class E2eRunner
{
    static bool Finite(Vector3 p) => !float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.x) && !float.IsInfinity(p.y);

    bool InsideTank(Cat c)
    {
        var p = c.transform.position;
        return Finite(p) && Mathf.Abs(p.x) + c.Radius <= GameManager.HalfWidth + 0.12f && p.y - c.Radius >= GameManager.FloorY - 0.12f;
    }

    IEnumerator Skills()
    {
        yield return Eraser();
        yield return Hug();
        yield return Shrink();
        yield return LevelUp();
        yield return Moonlight();
        yield return SugarRush();
        yield return Midas();
    }

    IEnumerator Eraser()
    {
        yield return Run("cat_doodle");
        R("B1.0", "Doodle outfit starts Eraser at 100 energy", "Eraser", GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id, GM.ActiveSkill == ThemeSkills.Eraser);
        ClearBoard();
        var a = MakeCat(5, -1.5f, -4.5f);
        var b = MakeCat(5, 1.5f, -4.5f);
        yield return new WaitForSeconds(0.6f);
        int score = GM.Score, coins = PlayerData.Coins, runCoins = GM.RunCoins, seen = PlayerData.Seen(5);
        var pos = a.transform.position;
        GM.PressSkill();
        var r = GM.Skills.Tap(a);
        yield return Frames(3);
        int cat = Physics.OverlapSphere(pos, 0.2f).Count(c => c.GetComponentInParent<Cat>() != null);
        R("B1.1", "Eraser removes the chosen cat only (no score, coins, discovery, merge, collider left)",
            "Activated; a gone, b stays; score/coins same; no collider; energy 0",
            r + "; a " + (a == null ? "gone" : "there") + ", b " + (b != null ? "stays" : "gone") + "; score " + (GM.Score - score) + ", coins " + (PlayerData.Coins - coins) + ", run " + (GM.RunCoins - runCoins) + "; colliders " + cat + "; energy " + GM.Energy.Value,
            r == SkillTap.Activated && a == null && b != null && GM.Score == score && PlayerData.Coins == coins && GM.RunCoins == runCoins && PlayerData.Seen(5) == seen && cat == 0 && GM.Energy.Value == 0);
        FillEnergy();
        GM.PressSkill();
        var r2 = GM.Skills.Tap(null);
        R("B1.2", "Tap on empty space while choosing = free cancel", "Cancelled; 100 energy; idle", r2 + "; " + GM.Energy.Value + "; " + (GM.Skills.IsTargeting ? "targeting" : "idle"),
            r2 == SkillTap.Cancelled && GM.Energy.Value == 100 && !GM.Skills.IsTargeting);
        GM.PressSkill();
        var held = Get<Cat>(GM, "_held");
        yield return Frames(2);
        R("B1.3", "The cat in hand and merging cats are never valid taps (HandleTargetInput guard)", "guard present", "see note",
            Source("Gameplay/GameManager.cs").Contains("if (cat != null && (!cat.Dropped || cat.Merging)) return;"), "pointer input cannot be injected in batch: checked in source");
        GM.PressSkill();
    }

    IEnumerator Hug()
    {
        yield return Run("cat_kawaii");
        R("B2.0", "Kawaii outfit starts Hug", "Hug", GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id, GM.ActiveSkill == ThemeSkills.Hug);
        ClearBoard();
        var a = MakeCat(3, -1.4f, -4.6f);
        var b = MakeCat(3, 1.4f, -4.6f);
        yield return new WaitForSeconds(0.8f);
        float d0 = Dist(a, b);
        GM.PressSkill();
        bool active = GM.Skills.IsTimedActive;
        yield return new WaitForSeconds(0.5f);
        float d1 = (a != null && b != null) ? Dist(a, b) : 0f;
        float t = 0f;
        while ((a != null || b != null) && t < 3f) { t += Time.deltaTime; yield return null; }
        yield return new WaitForSeconds(0.3f);
        var level4 = Board().Where(c => c.Level == 4).ToList();
        R("B2.1", "Hug pulls same-level cats together (they close in, then merge)", "timed; distance " + d0.ToString("0.0") + " -> smaller; merged to level 4",
            "timed " + active + "; " + d0.ToString("0.00") + " -> " + d1.ToString("0.00") + "; level-4 cats " + level4.Count,
            active && d1 < d0 - 0.05f && level4.Count == 1);
        R("B2.2", "A merge caused by Hug does not charge energy", "0", GM.Energy.Value, GM.Energy.Value == 0);
        yield return new WaitForSeconds(2.4f);
        R("B2.3", "Hug ends after its duration; board is sane", "not active; all cats finite and inside the tank", (GM.Skills.IsTimedActive ? "active" : "ended") + "; " + Board().All(InsideTank),
            !GM.Skills.IsTimedActive && Board().All(InsideTank));
    }

    IEnumerator Shrink()
    {
        yield return Run("cat_chibi");
        R("B3.0", "Chibi outfit starts Shrink", "Shrink", GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id, GM.ActiveSkill == ThemeSkills.Shrink);
        ClearBoard();
        var cats = new[] { MakeCat(1, -2.2f, -4.5f), MakeCat(2, -0.8f, -4.5f), MakeCat(3, 0.8f, -4.5f), MakeCat(4, 2.2f, -4.5f) };
        yield return new WaitForSeconds(0.8f);
        GM.PressSkill();
        yield return new WaitForSeconds(0.5f);
        var extra = MakeCat(2, 0f, -1f);                      // a cat that appears while it is active
        yield return Frames(2);
        float want = ThemeSkills.ShrinkScale;
        bool small = cats.All(c => Mathf.Abs(c.Scale - want) < 0.01f && Mathf.Abs(c.Radius - c.BaseRadius * want) < 0.01f) && Mathf.Abs(extra.Scale - want) < 0.01f;
        R("B3.1", "Shrink: every cat (and one created meanwhile) is at 0.85, collider radius follows", "scale 0.85, radius = base x 0.85", small ? "all 0.85" : "not all", small);
        float t = 0f;
        while (GM.Skills.TimedElapsed < ThemeSkills.ShrinkHoldSeconds + 0.75f && t < 9f) { t += Time.deltaTime; yield return null; }
        float mid = cats[1] != null ? cats[1].Scale : -1f;
        R("B3.2", "Growing back: scale is between 0.85 and 1 during the restore", "0.85 < s < 1", mid, mid > want + 0.02f && mid < 0.99f);
        t = 0f;
        while (GM.Skills.IsTimedActive && t < 4f) { t += Time.deltaTime; yield return null; }
        yield return Frames(3);
        bool exact = Board().All(c => c.Scale == 1f && Mathf.Approximately(c.Radius, c.BaseRadius));
        R("B3.3", "After the effect every cat is exactly its original size (no 0.85 x 0.85 drift)", "scale 1, radius = base", exact ? "exact" : string.Join(",", Board().Select(c => c.Scale.ToString("0.000"))), exact);
        yield return new WaitForSeconds(1.0f);
        R("B3.4", "Physics after the grow-back: cats finite, inside the tank, no Game Over", "all inside; not over", Board().All(InsideTank) + "; over=" + GM.IsGameOver, Board().All(InsideTank) && !GM.IsGameOver);
    }

    IEnumerator LevelUp()
    {
        yield return Run("cat_pixel");
        R("B4.0", "Pixel outfit starts Level Up", "Level Up", GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id, GM.ActiveSkill == ThemeSkills.LevelUp);
        ClearBoard();
        var big = MakeCat(9, 1.2f, -4.0f);
        var small = MakeCat(3, -1.8f, -4.6f);
        yield return new WaitForSeconds(0.7f);
        int score = GM.Score;
        GM.PressSkill();
        var bad = GM.Skills.Tap(big);
        R("B4.1", "Level 10 cat (index 9) is not a valid Level Up target; nothing is spent; still choosing", "InvalidTarget; 100; targeting", bad + "; " + GM.Energy.Value + "; " + (GM.Skills.IsTargeting ? "targeting" : "idle"),
            bad == SkillTap.InvalidTarget && GM.Energy.Value == 100 && GM.Skills.IsTargeting);
        var pos = small.transform.position;
        var ok = GM.Skills.Tap(small);
        yield return Frames(3);
        var up = Board().FirstOrDefault(c => c.Level == 4);
        R("B4.2", "Valid target goes up exactly one level, same place, no merge score", "Activated; level 4 near old spot; score same; energy 0",
            ok + "; " + (up == null ? "no level-4 cat" : "level 4, moved " + Vector2.Distance(pos, up.transform.position).ToString("0.00")) + "; score " + (GM.Score - score) + "; energy " + GM.Energy.Value,
            ok == SkillTap.Activated && up != null && Vector2.Distance(pos, up.transform.position) < 0.6f && GM.Score == score && GM.Energy.Value == 0);
        R("B4.3", "New cat has the size of its level and counts as discovered", "radius " + CatDatabase.Radius[4] + "; discovered", (up != null ? up.BaseRadius.ToString("0.00") : "-") + "; " + PlayerData.IsDiscovered(4),
            up != null && Mathf.Approximately(up.BaseRadius, CatDatabase.Radius[4]) && PlayerData.IsDiscovered(4));
        yield return new WaitForSeconds(0.8f);
        R("B4.4", "Board stays sane after the level-up (inside the tank)", "true", Board().All(InsideTank), Board().All(InsideTank));
    }

    IEnumerator Moonlight()
    {
        yield return Run("cat_midnight");
        R("B5.0", "Midnight outfit starts Moonlight", "Moonlight", GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id, GM.ActiveSkill == ThemeSkills.Moonlight);
        ClearBoard();
        var holder = MakeCat(1, 0f, 3.35f);                    // resting above the red line, held in place
        holder.Body.isKinematic = true;
        float t = 0f;
        while (Danger.Value < 0.5f && !GM.IsGameOver && t < 5f) { t += Time.deltaTime; yield return null; }
        float v0 = Danger.Value;
        float tAct = Time.time;
        GM.PressSkill();
        yield return Frames(2);
        yield return new WaitForSeconds(3f);
        float v1 = Danger.Value;
        R("B5.1", "Moonlight freezes the red-line timer (no growth, no reset) and no Game Over meanwhile", "value constant; paused; not over",
            v0.ToString("0.00") + " -> " + v1.ToString("0.00") + "; paused " + Danger.Paused + "; over " + GM.IsGameOver,
            Mathf.Abs(v1 - v0) < 0.08f && Danger.Paused && !GM.IsGameOver && v0 > 0.3f);
        t = 0f;
        while (GM.Skills.IsTimedActive && t < 8f) { t += Time.deltaTime; yield return null; }
        float lasted = Time.time - tAct;
        float tEnd = Time.time;
        R("B5.2", "Effect lasts its duration", ThemeSkills.MoonlightSeconds + " s", lasted.ToString("0.0") + " s", Mathf.Abs(lasted - ThemeSkills.MoonlightSeconds) < 0.5f);
        t = 0f;
        while (!GM.IsGameOver && t < 4f) { t += Time.deltaTime; yield return null; }
        float after = Time.time - tEnd;
        float expected = 2.0f - v0;
        R("B5.3", "The timer continues from the held value (not reset); Game Over comes after the effect, not during it",
            "game over ~" + expected.ToString("0.0") + " s after the end (not 2.0)", (GM.IsGameOver ? "over after " : "NOT over after ") + after.ToString("0.0") + " s",
            GM.IsGameOver && Mathf.Abs(after - expected) < 0.45f);
    }

    int Doubled(int v, float f) => Mathf.FloorToInt(v * f + 0.5f);

    IEnumerator SugarRush()
    {
        yield return Run("cat_candy");
        R("B6.0", "Candy outfit starts Sugar Rush", "Sugar Rush", GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id, GM.ActiveSkill == ThemeSkills.SugarRush);
        ClearBoard();
        yield return DoMerge(0, -2f);                           // first level-1 cat: discovery happens here
        ClearBoard();
        int s = GM.Score, c = PlayerData.Coins;
        yield return DoMerge(0, 0f);
        int S0 = GM.Score - s, C0 = PlayerData.Coins - c;
        ClearBoard();
        R("B6.1", "Baseline merge (no skill)", "score " + CatDatabase.Score[1] + "; coins " + CatDatabase.MergeCoins(1), "score " + S0 + "; coins " + C0, S0 == CatDatabase.Score[1] && C0 == CatDatabase.MergeCoins(1));
        FillEnergy();
        GM.PressSkill();
        float tAct = Time.time;
        s = GM.Score; c = PlayerData.Coins;
        yield return DoMerge(0, -1f);
        int S1 = GM.Score - s, C1 = PlayerData.Coins - c;
        R("B6.2", "During Sugar Rush: merge score x2, merge coins x1.5 (half up)", "score " + Doubled(S0, ThemeSkills.SugarRushScoreMultiplier) + "; coins " + Doubled(C0, ThemeSkills.SugarRushCoinMultiplier),
            "score " + S1 + "; coins " + C1, S1 == Doubled(S0, ThemeSkills.SugarRushScoreMultiplier) && C1 == Doubled(C0, ThemeSkills.SugarRushCoinMultiplier));
        ClearBoard();
        bool seenBefore = PlayerData.IsDiscovered(2);            // the random next cat may already have been a level-2 cat
        s = GM.Score; c = PlayerData.Coins;
        yield return DoMerge(1, 1.5f);
        int S2 = GM.Score - s, C2 = PlayerData.Coins - c;
        int expC = Doubled(CatDatabase.MergeCoins(2), ThemeSkills.SugarRushCoinMultiplier) + (seenBefore ? 0 : CatDatabase.DiscoveryReward(2));
        R("B6.3", "Discovery reward of a new cat is NOT multiplied", "score " + Doubled(CatDatabase.Score[2], 2f) + "; coins " + expC, "score " + S2 + "; coins " + C2,
            S2 == Doubled(CatDatabase.Score[2], ThemeSkills.SugarRushScoreMultiplier) && C2 == expC);
        ClearBoard();
        int cc = PlayerData.Coins;
        var dup = Cosmetics.Award(Cosmetics.Backgrounds[1], Rarity.Common);
        dup = Cosmetics.Award(Cosmetics.Backgrounds[1], Rarity.Common);
        R("B6.4", "Gacha refund during the effect is NOT multiplied", "refund " + dup.Refund + " x2 awards", PlayerData.Coins - cc, PlayerData.Coins - cc == dup.Refund, "first award of the pair is new (0), second is the refund");
        float t = 0f;
        while (GM.Skills.IsTimedActive && t < 12f) { t += Time.deltaTime; yield return null; }
        float lasted = Time.time - tAct;
        R("B6.5", "Effect lasts its duration", ThemeSkills.SugarRushSeconds + " s", lasted.ToString("0.0") + " s", Mathf.Abs(lasted - ThemeSkills.SugarRushSeconds) < 0.6f);
        s = GM.Score; c = PlayerData.Coins;
        yield return DoMerge(0, 0.5f);
        R("B6.6", "After the effect merges pay normally again", "score " + S0 + "; coins " + C0, "score " + (GM.Score - s) + "; coins " + (PlayerData.Coins - c), GM.Score - s == S0 && PlayerData.Coins - c == C0);
    }

    IEnumerator Midas()
    {
        yield return Run("cat_gold");
        R("B7.0", "Gold outfit starts Midas", "Midas", GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id, GM.ActiveSkill == ThemeSkills.Midas);
        ClearBoard();
        var big = MakeCat(8, 1.4f, -4.1f);
        var small = MakeCat(2, -1.8f, -4.6f);
        yield return new WaitForSeconds(0.7f);
        int score = GM.Score, coins = PlayerData.Coins, run = GM.RunCoins;
        GM.PressSkill();
        var bad = GM.Skills.Tap(big);
        R("B7.1", "A cat above the Midas limit is refused, nothing spent", "InvalidTarget; 100; targeting", bad + "; " + GM.Energy.Value + "; " + (GM.Skills.IsTargeting ? "targeting" : "idle"),
            bad == SkillTap.InvalidTarget && GM.Energy.Value == 100 && GM.Skills.IsTargeting);
        var ok = GM.Skills.Tap(small);
        yield return Frames(3);
        int paid = ThemeSkills.MidasCoin(2);
        R("B7.2", "Midas sells the cat: 3 + 2 x level coins through the normal coin path, no score", "Activated; +" + paid + " coins (balance and run); score same; gone; energy 0",
            ok + "; +" + (PlayerData.Coins - coins) + " / run +" + (GM.RunCoins - run) + "; score " + (GM.Score - score) + "; " + (small == null ? "gone" : "there") + "; " + GM.Energy.Value,
            ok == SkillTap.Activated && PlayerData.Coins - coins == paid && GM.RunCoins - run == paid && GM.Score == score && small == null && GM.Energy.Value == 0);
    }
}
