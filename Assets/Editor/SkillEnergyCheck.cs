using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks SkillEnergy: 30 + 11 merges = 96, the 12th reaches 100, no overflow, consume, normal merge after a skill was
/// used, skill-caused merge, restart, resume (and old saves) and Game Over.
/// Menu: Tools/Suika/Check Skill Energy. Batch: -executeMethod SkillEnergyCheck.Run (exit code 1 on failure).
/// Touches no save data.
/// </summary>
public static class SkillEnergyCheck
{
    [MenuItem("Tools/Suika/Check Skill Energy")]
    public static void RunMenu()
    {
        if (Execute()) Debug.Log("[SkillEnergy] all checks passed");
    }

    public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

    static bool Execute()
    {
        try
        {
            Test();
            Debug.Log("SKILLENERGY_CHECK_OK");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("SkillEnergy check failed: " + e.Message);
            return false;
        }
    }

    static void Test()
    {
        var e = new SkillEnergy();

        // ---- new run: 30; 30 + 11 normal merges = 96; the 12th reaches 100; more merges stay at 100
        e.Reset();
        Check(e.Value == 30 && e.Max == 100 && e.Cost == 100, "new run starts at 30");
        for (int i = 0; i < 11; i++) Check(e.OnMerge(MergeSource.Normal) == 6, "merge " + (i + 1) + " gives 6");
        Check(e.Value == 96, "30 + 11 merges = 96");
        Check(e.OnMerge(MergeSource.Normal) == 4 && e.Value == 100, "12th merge reaches 100 (clamped)");
        Check(e.OnMerge(MergeSource.Normal) == 0 && e.Value == 100, "no overflow at 100");
        Check(e.CanAfford, "can afford at 100");

        // ---- consume: 100 -> 0, only when affordable
        Check(e.Consume() && e.Value == 0, "consume to 0");
        Check(!e.Consume() && e.Value == 0, "cannot consume at 0");
        var poor = new SkillEnergy();
        poor.Reset();
        Check(!poor.Consume() && poor.Value == 30, "not enough energy: nothing is spent");   // e.g. invalid target: caller never consumes

        // ---- a normal merge while a timed skill runs still counts (the skill state is not an input here)
        Check(e.OnMerge(MergeSource.Normal) == 6 && e.Value == 6, "normal merge right after using a skill (skill still running)");

        // ---- a merge the skill causes directly does not count
        int before = e.Value;
        Check(e.OnMerge(MergeSource.Skill) == 0 && e.Value == before, "skill-triggered merge gives nothing");
        Check(e.OnMerge(MergeSource.Normal) == 6 && e.Value == before + 6, "next normal merge counts again");

        // ---- restart: back to 30 from any state
        e.Reset();
        Check(e.Value == 30, "restart resets to 30");

        // ---- resume: saved energy is kept, through json
        e.OnMerge(MergeSource.Normal);                                   // 36
        var run = new RunSnapshot();
        e.Save(run);
        var loaded = JsonUtility.FromJson<RunSnapshot>(JsonUtility.ToJson(run));
        var resumed = new SkillEnergy();
        resumed.Restore(loaded);
        Check(resumed.Value == 36, "resume keeps 36");
        var zero = new SkillEnergy();
        zero.Reset();
        zero.Consume();
        zero.OnMerge(MergeSource.Normal);
        run = new RunSnapshot();
        zero.Save(run);
        resumed.Restore(JsonUtility.FromJson<RunSnapshot>(JsonUtility.ToJson(run)));
        Check(resumed.Value == zero.Value, "resume keeps a low value");
        resumed.Restore(new RunSnapshot { energyRecorded = true, energy = 0 });
        Check(resumed.Value == 0, "a saved 0 stays 0 (not replaced by 30)");
        resumed.Restore(new RunSnapshot { energyRecorded = true, energy = 999 });
        Check(resumed.Value == 100, "bad saved value is clamped");

        // ---- old save: no energy field -> starts like a new run
        var old = JsonUtility.FromJson<RunSnapshot>("{\"score\":10,\"next\":1,\"coins\":2,\"cats\":[]}");
        Check(!old.energyRecorded, "old snapshot has no energy record");
        resumed.Restore(old);
        Check(resumed.Value == 30, "old save starts at 30");
        resumed.Restore(null);
        Check(resumed.Value == 30, "no snapshot starts at 30");

        // ---- Game Over: nothing changes any more; a new run unfreezes
        var g = new SkillEnergy();
        g.Reset();
        for (int i = 0; i < 12; i++) g.OnMerge(MergeSource.Normal);      // 100
        g.Freeze();
        Check(g.OnMerge(MergeSource.Normal) == 0 && g.Value == 100, "no charging after Game Over");
        Check(!g.Consume() && g.Value == 100, "no spending after Game Over");
        var g2 = new SkillEnergy();
        g2.Reset();
        g2.Freeze();
        Check(g2.OnMerge(MergeSource.Normal) == 0 && g2.Value == 30, "frozen at 30 stays 30");
        g.Reset();
        Check(!g.Frozen && g.OnMerge(MergeSource.Normal) == 6 && g.Value == 36, "restart after Game Over works");
        g.Freeze();
        g.Restore(loaded);
        Check(!g.Frozen && g.Value == 36, "resume also unfreezes");
    }

    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }
}
