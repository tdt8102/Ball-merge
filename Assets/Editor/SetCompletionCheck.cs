using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks collection counting and the one-time skill unlock (0/11, 10/11, 11/11, duplicates, reload, old save).
/// Menu: Tools/Suika/Check Set Completion. Batch: -executeMethod SetCompletionCheck.Run (exit code 1 on failure).
/// The real save is backed up first and restored afterwards.
/// </summary>
public static class SetCompletionCheck
{
    const string SaveKey = "catcatcat.save", RunKey = "catcatcat.run";

    [MenuItem("Tools/Suika/Check Set Completion")]
    public static void RunMenu()
    {
        if (Execute()) Debug.Log("[SetCompletion] all checks passed");
    }

    public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

    static bool Execute()
    {
        string saveBackup = PlayerPrefs.GetString(SaveKey, null), runBackup = PlayerPrefs.GetString(RunKey, null);
        bool ok = true;
        var unlocked = new List<string>();
        Action<ThemeSkillDef> onUnlock = s => unlocked.Add(s.Id);
        SetCompletion.SkillUnlocked += onUnlock;
        try
        {
            Test(unlocked);
            Debug.Log("SETCOMPLETION_CHECK_OK");
        }
        catch (Exception e)
        {
            ok = false;
            Debug.LogError("SetCompletion check failed: " + e.Message);
        }
        finally
        {
            SetCompletion.SkillUnlocked -= onUnlock;
            PlayerData.ResetAll();                        // clear statics, then put the user's own save back
            PlayerData.Blocked = false;
            if (saveBackup != null) PlayerPrefs.SetString(SaveKey, saveBackup);
            if (runBackup != null) PlayerPrefs.SetString(RunKey, runBackup);
            PlayerPrefs.Save();
            Reload();
        }
        return ok;
    }

    static void Test(List<string> unlocked)
    {
        var doodle = Cosmetics.FindTheme("cat_doodle");
        var candy = Cosmetics.FindTheme("cat_candy");
        var classic = Cosmetics.FindTheme("cat_classic");
        var eraser = ThemeSkills.Eraser;

        // ---- 0/11 (fresh profile)
        PlayerData.ResetAll();
        PlayerData.Blocked = false;
        Reload();
        unlocked.Clear();
        Check(SetCompletion.Progress(doodle).Owned == 0 && SetCompletion.Progress(doodle).Total == 11, "fresh: 0/11");
        Check(!SetCompletion.IsSkillUnlocked(eraser), "fresh: locked");

        // ---- Classic has no skill, even though all 11 are owned from the start
        Check(SetCompletion.Progress(classic).Complete, "classic is 11/11");
        Check(!SetCompletion.IsSkillUnlocked(classic) && ThemeSkills.Of(classic) == null, "classic has no skill");

        // ---- 10/11 does not unlock
        for (int l = 0; l < 10; l++) Check(PlayerData.Unlock(Cosmetics.CatItemOf(doodle, l).Id), "new cat " + l);
        Check(SetCompletion.Progress(doodle).Owned == 10, "10/11");
        Check(!SetCompletion.IsSkillUnlocked(eraser) && unlocked.Count == 0, "10/11 stays locked");

        // ---- a duplicate changes nothing
        Check(!PlayerData.Unlock(Cosmetics.CatItemOf(doodle, 3).Id), "duplicate is not new");
        Check(SetCompletion.Progress(doodle).Owned == 10 && unlocked.Count == 0, "duplicate keeps 10/11");

        // ---- another theme / non-cat items do not count
        PlayerData.Unlock(Cosmetics.CatItemOf(candy, 0).Id);
        PlayerData.Unlock("bg_sky");
        Check(SetCompletion.Progress(doodle).Owned == 10 && unlocked.Count == 0, "other items do not count");

        // ---- 11/11 unlocks, once
        Check(PlayerData.Unlock(Cosmetics.CatItemOf(doodle, 10).Id), "11th cat");
        Check(SetCompletion.Progress(doodle).Complete, "11/11");
        Check(SetCompletion.IsSkillUnlocked(eraser), "11/11 unlocks");
        Check(unlocked.Count == 1 && unlocked[0] == ThemeSkills.EraserId, "event raised once");
        PlayerData.Unlock(Cosmetics.CatItemOf(doodle, 10).Id);
        SetCompletion.Backfill();
        Check(unlocked.Count == 1, "no repeat after duplicate / repeated check");

        // ---- save and load: still unlocked, nothing announced again
        PlayerData.Flush();
        Reload();
        Check(SetCompletion.IsSkillUnlocked(eraser) && SetCompletion.Progress(doodle).Owned == 11, "reload keeps unlock and ownership");
        Check(PlayerData.IsOwned(Cosmetics.CatItemOf(candy, 0).Id), "reload keeps other ownership");
        Check(unlocked.Count == 1, "reload does not raise the event");

        // ---- old save (no unlock list): ownership kept, complete set unlocks silently, incomplete one does not
        var ids = new List<string>();
        for (int l = 0; l < 11; l++) ids.Add(Cosmetics.CatItemOf(doodle, l).Id);
        for (int l = 0; l < 10; l++) ids.Add(Cosmetics.CatItemOf(candy, l).Id);
        string old = "{\"version\":1,\"coins\":123,\"best\":456,\"skins\":[\"" + string.Join("\",\"", ids) + "\"]}";
        PlayerData.ResetAll();
        PlayerData.Blocked = false;
        PlayerPrefs.SetString(SaveKey, old);
        unlocked.Clear();
        Reload();
        Check(PlayerData.Coins == 123 && PlayerData.Best == 456, "old save: coins and best kept");
        Check(SetCompletion.Progress(doodle).Owned == 11 && SetCompletion.Progress(candy).Owned == 10, "old save: ownership kept");
        Check(SetCompletion.IsSkillUnlocked(eraser), "old save: complete set unlocked");
        Check(!SetCompletion.IsSkillUnlocked(ThemeSkills.SugarRush), "old save: 10/11 not unlocked");
        Check(unlocked.Count == 0, "old save: no event on load");
        PlayerData.Unlock(Cosmetics.CatItemOf(candy, 10).Id);
        Check(SetCompletion.IsSkillUnlocked(ThemeSkills.SugarRush) && unlocked.Count == 1, "old save: completing the last cat unlocks once");
    }

    /// <summary>Forget the cached save so the next access reads PlayerPrefs again (what a game restart does).</summary>
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
