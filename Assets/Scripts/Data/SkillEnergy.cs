using UnityEngine;

/// <summary>Where a merge came from. Only normal gameplay merges charge the skill energy.</summary>
public enum MergeSource { Normal, Skill }

/// <summary>
/// Energy shared by all theme skills, for one run. Numbers come from Balance (themeSkillMaxEnergy, StartingEnergy,
/// EnergyPerMerge, Cost). Plain class (no MonoBehaviour) so it can be tested without a scene; GameManager owns one.
/// No combo or level bonus, no overflow: the value is always 0..max.
/// </summary>
public sealed class SkillEnergy
{
    public int Value { get; private set; }
    public bool Frozen { get; private set; }     // Game Over: nothing changes any more

    public int Max => ThemeSkills.MaxEnergy;
    public int Cost => ThemeSkills.SkillCost;
    public bool CanAfford => Value >= Cost;

    /// <summary>A new run (also Restart / Play again).</summary>
    public void Reset()
    {
        Frozen = false;
        Value = Mathf.Clamp(ThemeSkills.StartingEnergy, 0, Max);
    }

    /// <summary>Continue a saved run: keep the saved energy. Saves from before energy existed start like a new run.</summary>
    public void Restore(RunSnapshot run)
    {
        Reset();
        if (run != null && run.energyRecorded) Value = Mathf.Clamp(run.energy, 0, Max);
    }

    public void Save(RunSnapshot run)
    {
        run.energyRecorded = true;
        run.energy = Value;
    }

    /// <summary>
    /// A merge happened. Only normal gameplay merges count, including chains started by a normal drop and merges that
    /// happen while a timed skill is running; a merge a skill causes directly does not. Returns the energy gained.
    /// </summary>
    public int OnMerge(MergeSource source)
    {
        if (Frozen || source != MergeSource.Normal) return 0;
        int before = Value;
        Value = Mathf.Min(Max, Value + ThemeSkills.EnergyPerMerge);
        return Value - before;
    }

    /// <summary>
    /// Spend the skill cost. Call it only after the skill really took effect: cancelling target selection or picking an
    /// invalid target must not call it. Returns false (and spends nothing) when there is not enough energy.
    /// </summary>
    public bool Consume()
    {
        if (Frozen || !CanAfford) return false;
        Value -= Cost;
        return true;
    }

    /// <summary>Game Over: stop charging and spending.</summary>
    public void Freeze() => Frozen = true;
}
