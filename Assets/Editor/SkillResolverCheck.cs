using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks SkillResolver: 11/11, one slot missing, mixed themes, Classic, set not unlocked, and resume of a saved run.
/// Menu: Tools/Suika/Check Skill Resolver. Batch: -executeMethod SkillResolverCheck.Run (exit code 1 on failure).
/// The real save is backed up first and restored afterwards.
/// </summary>
public static class SkillResolverCheck
{
    const string SaveKey = "catcatcat.save", RunKey = "catcatcat.run";

    [MenuItem("Tools/Suika/Check Skill Resolver")]
    public static void RunMenu()
    {
        if (Execute()) Debug.Log("[SkillResolver] all checks passed");
    }

    public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

    static bool Execute()
    {
        string saveBackup = PlayerPrefs.GetString(SaveKey, null), runBackup = PlayerPrefs.GetString(RunKey, null);
        bool ok = true;
        try
        {
            Test();
            Debug.Log("SKILLRESOLVER_CHECK_OK");
        }
        catch (Exception e)
        {
            ok = false;
            Debug.LogError("SkillResolver check failed: " + e.Message);
        }
        finally
        {
            PlayerData.ResetAll();
            PlayerData.Blocked = false;
            if (saveBackup != null) PlayerPrefs.SetString(SaveKey, saveBackup);
            if (runBackup != null) PlayerPrefs.SetString(RunKey, runBackup);
            PlayerPrefs.Save();
            Reload();
        }
        return ok;
    }

    static void Test()
    {
        var doodle = Cosmetics.FindTheme("cat_doodle");
        var candy = Cosmetics.FindTheme("cat_candy");

        // ---- Classic: a fresh profile wears the Classic cats
        Fresh();
        var r = SkillResolver.Resolve();
        Check(r.Status == SkillStatus.Classic && !r.IsReady && r.ValidSlots == 11 && r.Skill == null, "classic is not ready");
        Check(SkillResolver.ForNewRun() == null, "classic: no skill for a new run");

        // ---- one slot missing: 10 doodle cats owned, 10 slots wear them, the 11th slot names a cat the player does not own
        for (int l = 0; l < 10; l++) PlayerData.Unlock(Cosmetics.CatItemOf(doodle, l).Id);
        Cosmetics.EquipTheme(doodle);                                   // equips the 10 owned cats; slot 10 stays Classic
        r = SkillResolver.Resolve();
        Check(r.Status == SkillStatus.MixedTheme && !r.IsReady, "10 doodle + 1 classic is mixed");
        PlayerData.SetCatSkin(10, "cat_doodle");                         // wears a cat it does not own
        Cosmetics.Reload();
        r = SkillResolver.Resolve();
        Check(r.Status == SkillStatus.MissingSlots && r.ValidSlots == 10 && !r.IsReady, "one slot missing");
        PlayerData.SetCatSkin(10, "no_such_theme");                      // unknown theme id
        r = SkillResolver.Resolve();
        Check(r.Status == SkillStatus.MissingSlots && r.ValidSlots == 10, "unknown theme counts as missing");
        Check(SkillResolver.ForNewRun() == null, "missing slot: no skill");

        // ---- mixed themes: all slots valid but two themes
        Fresh();
        for (int l = 0; l < 11; l++) { PlayerData.Unlock(Cosmetics.CatItemOf(doodle, l).Id); }
        for (int l = 0; l < 5; l++) PlayerData.Unlock(Cosmetics.CatItemOf(candy, l).Id);
        Cosmetics.EquipTheme(doodle);
        PlayerData.SetCatSkin(0, "cat_candy");
        Cosmetics.Reload();
        r = SkillResolver.Resolve();
        Check(r.Status == SkillStatus.MixedTheme && r.ValidSlots == 11 && !r.IsReady, "mixed themes");
        Check(SetCompletion.IsSkillUnlocked(ThemeSkills.Eraser), "doodle set is unlocked");

        // ---- 11/11 and unlocked: ready
        Cosmetics.EquipTheme(doodle);
        r = SkillResolver.Resolve();
        Check(r.IsReady && r.Status == SkillStatus.Ready && r.Theme == doodle && r.Skill == ThemeSkills.Eraser && r.ValidSlots == 11, "ready");
        Check(SkillResolver.ForNewRun() == ThemeSkills.Eraser, "ready: new run gets the skill");

        // ---- full set worn but the set is not recorded as unlocked (inconsistent data)
        ClearUnlockedList();
        r = SkillResolver.Resolve();
        Check(r.Status == SkillStatus.NotUnlocked && !r.IsReady && r.Skill == ThemeSkills.Eraser, "not unlocked");
        Check(SkillResolver.ForNewRun() == null, "not unlocked: no skill");
        SetCompletion.Backfill();                                        // what a save load does
        Check(SkillResolver.Resolve().IsReady, "ready again once unlock is recorded");

        // ---- resume: the recorded skill is kept even if the equipment changed in the menu
        var snapshot = new RunSnapshot { skillRecorded = true, activeSkillId = ThemeSkills.EraserId };
        var roundTrip = JsonUtility.FromJson<RunSnapshot>(JsonUtility.ToJson(snapshot));
        Check(roundTrip.skillRecorded && roundTrip.activeSkillId == ThemeSkills.EraserId, "snapshot keeps the skill in json");
        PlayerData.SetCatSkin(0, "cat_classic");                         // player changes the skin, then continues the run
        Cosmetics.Reload();
        Check(SkillResolver.Resolve().Status == SkillStatus.MixedTheme, "equipment is no longer a full set");
        Check(SkillResolver.ForResume(roundTrip) == ThemeSkills.Eraser, "resume keeps the recorded skill");
        Check(SkillResolver.ForNewRun() == null, "...while a new run would get none");

        // a run that started without a skill stays without one, even if the outfit became ready
        Cosmetics.EquipTheme(doodle);
        var none = new RunSnapshot { skillRecorded = true, activeSkillId = "" };
        Check(SkillResolver.ForResume(none) == null, "run without skill stays without");

        // unknown or locked recorded id is ignored
        Check(SkillResolver.ForResume(new RunSnapshot { skillRecorded = true, activeSkillId = "skill_nope" }) == null, "unknown id ignored");
        Check(SkillResolver.ForResume(new RunSnapshot { skillRecorded = true, activeSkillId = ThemeSkills.SugarRushId }) == null, "locked id ignored");

        // old snapshot (saved before skills existed): resolved once from the current equipment
        var oldJson = "{\"score\":10,\"next\":1,\"coins\":2,\"cats\":[]}";
        var old = JsonUtility.FromJson<RunSnapshot>(oldJson);
        Check(!old.skillRecorded, "old snapshot has no record");
        Check(SkillResolver.ForResume(old) == ThemeSkills.Eraser, "old snapshot: resolved from current equipment");
        Check(SkillResolver.ForResume(null) == ThemeSkills.Eraser, "no snapshot: resolved like a new run");
    }

    static void Fresh()
    {
        PlayerData.ResetAll();
        PlayerData.Blocked = false;
        Reload();
    }

    static void ClearUnlockedList()
    {
        object data = typeof(PlayerData).GetField("_d", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        ((List<string>)data.GetType().GetField("skillsUnlocked").GetValue(data)).Clear();
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
