using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Section G (double tap on the real Gacha button) and A (the mandatory end-to-end flow).</summary>
public partial class E2eRunner
{
    // ------------------------------------------------------------------ G: real menu, double tap on PULL

    IEnumerator GachaTap()
    {
        SceneManager.LoadScene("Menu");                       // batch Play Mode does not honour the play-from-menu setting
        yield return Frames(4);
        var gacha = FindObjectOfType<GachaPage>(true);          // its tab is inactive until selected
        R("G0", "Menu scene is the start scene and has the Gacha page", "GachaPage present", gacha != null ? "present" : "missing", gacha != null);
        if (gacha == null) yield break;
        Fresh();
        PlayerData.AddCoins(5000);
        Call(FindObjectOfType<MenuController>(), "SelectTab", 0, true);        // the player taps PULL on the visible Gacha tab
        yield return Frames(2);
        int c0 = PlayerData.Coins;
        Call(gacha, "OnPull", 1);
        Call(gacha, "OnPull", 1);                       // second tap while the opening is playing
        Call(gacha, "OnPull", 10);
        R("G1", "Triple tap on PULL while the box is opening", "charged once (150)", "charged " + (c0 - PlayerData.Coins),
            c0 - PlayerData.Coins == 150, "the Play coroutine sets _busy synchronously");
        // close the overlay so later sections start clean
        gacha.Overlay.SetActive(false);
        Call(gacha, "StopAllCoroutines");
    }

    // ------------------------------------------------------------------ A: the flow

    IEnumerator Flow()
    {
        // Gacha -> ownership
        Fresh();
        PlayerData.AddCoins(5000);
        int coins = PlayerData.Coins;
        var pull = Cosmetics.Pull(1);
        var item = pull[0].Item;
        R("A1", "Pull x1: coins spent, item owned", "coins -150; owned; IsNew",
            "coins " + (PlayerData.Coins - coins) + "; owned=" + PlayerData.IsOwned(item.Id) + "; new=" + pull[0].IsNew,
            PlayerData.Coins == coins - 150 && PlayerData.IsOwned(item.Id) && pull[0].IsNew);

        Fresh();
        Award("cat_doodle", 10);
        var s10 = ThemeSkillInfo.Of(Theme("cat_doodle"));
        R("A2", "10/11 cats of Doodle: collection 10/11, skill still locked", "Locked, 10/11, flag false",
            s10.State + ", " + s10.Owned + "/11, flag " + PlayerData.IsSkillUnlocked(ThemeSkills.EraserId),
            s10.State == SkillState.Locked && s10.Owned == 10 && !PlayerData.IsSkillUnlocked(ThemeSkills.EraserId));

        // outfit 10/11 -> a run without skill
        for (int l = 0; l < 10; l++) Cosmetics.Equip(CatOf("cat_doodle", l));
        yield return LoadMain(false);
        yield return Frames(3);
        R("A3", "Start a run with 10/11 owned and worn: no skill, no button", "ActiveSkill null; button hidden",
            (GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id) + "; button " + UI.SkillRect.gameObject.activeSelf,
            GM.ActiveSkill == null && !UI.SkillRect.gameObject.activeSelf);

        // 11th cat -> permanent unlock, announced once
        int events = 0;
        System.Action<ThemeSkillDef> h = s => events++;
        SetCompletion.SkillUnlocked += h;
        var last = Cosmetics.Award(CatOf("cat_doodle", 10), Rarity.Legendary);
        SetCompletion.SkillUnlocked -= h;
        R("A4", "11th cat from the Gacha path: unlock raised once and stored", "event 1; Completed=Eraser; flag true",
            "event " + events + "; " + (last.Completed == null ? "null" : last.Completed.Id) + "; flag " + PlayerData.IsSkillUnlocked(ThemeSkills.EraserId),
            events == 1 && last.Completed == ThemeSkills.Eraser && PlayerData.IsSkillUnlocked(ThemeSkills.EraserId));

        int loadEvents = 0;
        System.Action<ThemeSkillDef> h2 = s => loadEvents++;
        SetCompletion.SkillUnlocked += h2;
        PlayerData.Flush();
        Reread();
        bool still = PlayerData.IsSkillUnlocked(ThemeSkills.EraserId);
        SetCompletion.SkillUnlocked -= h2;
        R("A5", "App restart (save reloaded): still unlocked, nothing announced again", "flag true; 0 events", "flag " + still + "; " + loadEvents + " events",
            still && loadEvents == 0);

        // unlock survives a change of outfit
        Outfit("cat_classic");
        var afterSwap = ThemeSkillInfo.Of(Theme("cat_doodle"));
        R("A6", "Wearing Classic again does not take the unlock away", "Unlocked (not Active)", afterSwap.State.ToString(),
            afterSwap.State == SkillState.Unlocked && PlayerData.IsSkillUnlocked(ThemeSkills.EraserId));

        // equip all 11 -> run: energy 30
        Outfit("cat_doodle");
        yield return LoadMain(false);
        yield return Frames(3);
        R("A7", "Equip 11/11 Doodle and start a run: Eraser, energy 30, HUD button", "Eraser; 30; visible; badge 30%",
            (GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id) + "; " + GM.Energy.Value + "; " + UI.SkillRect.gameObject.activeSelf + "; " + BadgeText().text,
            GM.ActiveSkill == ThemeSkills.Eraser && GM.Energy.Value == 30 && UI.SkillRect.gameObject.activeSelf && BadgeText().text == "30%");

        // merge +6 (real collision merge)
        ClearBoard();
        int score0 = GM.Score;
        yield return DoMerge(0, -1.5f);
        R("A8", "One real merge: energy +6 and score paid", "36; score +" + CatDatabase.Score[1], GM.Energy.Value + "; score +" + (GM.Score - score0),
            GM.Energy.Value == 36 && GM.Score - score0 == CatDatabase.Score[1]);

        // up to 100: 12 merges in total, one by one
        int ok = 1, bad = 0;
        for (int i = 2; i <= 12; i++)
        {
            ClearBoard();
            int before = GM.Energy.Value;
            yield return DoMerge(0, (i % 5 - 2) * 1.0f);
            int want = Mathf.Min(100, before + 6);
            if (GM.Energy.Value == want) ok++; else bad++;
        }
        R("A9", "12 real merges in total: +6 each, capped", "100 after the 12th; every step +6", GM.Energy.Value + "; ok " + ok + " bad " + bad,
            GM.Energy.Value == 100 && bad == 0 && GM.Skills.Ready);
        ClearBoard();
        yield return DoMerge(0, 0f);
        Is("A10", "A merge at 100 energy does not overflow", 100, GM.Energy.Value);
        ClearBoard();

        // activation: Eraser target mode, cancel is free, then a target
        GM.PressSkill();
        yield return Frames(3);
        bool dimOn = Get<SpriteRenderer>(GM, "_dim").enabled;
        R("A11", "Press at 100: target mode (board dimmed, banner, badge CANCEL), no energy spent",
            "targeting; dim on; banner on; CANCEL; 100",
            GM.Skills.IsTargeting + "; " + dimOn + "; " + UI.BannerRect.gameObject.activeSelf + "; " + BadgeText().text + "; " + GM.Energy.Value,
            GM.Skills.IsTargeting && dimOn && UI.BannerRect.gameObject.activeSelf && BadgeText().text == "CANCEL" && GM.Energy.Value == 100);
        GM.PressSkill();
        yield return Frames(3);
        R("A12", "Press again = cancel: free, dim and banner off", "idle; 100; dim off; banner off",
            (GM.Skills.IsTargeting ? "targeting" : "idle") + "; " + GM.Energy.Value + "; " + Get<SpriteRenderer>(GM, "_dim").enabled + "; " + UI.BannerRect.gameObject.activeSelf,
            !GM.Skills.IsTargeting && GM.Energy.Value == 100 && !Get<SpriteRenderer>(GM, "_dim").enabled && !UI.BannerRect.gameObject.activeSelf);

        var victim = MakeCat(3, 1.5f, -4.8f);
        yield return Frames(5);
        GM.PressSkill();
        var tap = GM.Skills.Tap(victim);
        yield return Frames(3);
        R("A13", "Tap a cat in target mode: effect, energy spent, mode over", "Activated; cat gone; 0; idle",
            tap + "; " + (victim == null ? "gone" : "still there") + "; " + GM.Energy.Value + "; " + (GM.Skills.IsTargeting ? "targeting" : "idle"),
            tap == SkillTap.Activated && victim == null && GM.Energy.Value == 0 && !GM.Skills.IsTargeting);
        ClearBoard();
        yield return Frames(2);
        int e0 = GM.Energy.Value;
        yield return DoMerge(0, 1f);
        R("A14", "After using the skill the energy charges again from 0", "6", GM.Energy.Value, GM.Energy.Value == e0 + 6 && e0 == 0);
    }
}
