using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The size factor of Shrink over time, and how it is applied to the board. There is no clock of its own: the factor is a
/// pure function of the seconds elapsed in the skill's timed effect (SkillRuntime), so Pause/Settings, Game Over and
/// restart stop, end or reset it exactly like every other timed skill.
///
/// Timeline (Balance): at once scale (0.85) for shrinkDuration (6 s), then a linear grow back to 1 over
/// shrinkRestoreDuration (1.5 s). Cats are never multiplied: SetScale is absolute on the cat's BaseRadius, so repeated or
/// stacked calls cannot give 0.85 x 0.85, and the end is exactly the original size.
///
/// Policy for cats that appear while it is active (new drop, merge result, Level Up result): they take the CURRENT factor
/// (ApplyTo), so every cat on the board always has the same factor; when the effect ends they grow back with the rest.
/// Mass is not changed.
/// </summary>
public sealed class ShrinkEffect
{
    public const string PauseReason = "shrink_restore";

    public bool Active { get; set; }
    public float Elapsed { get; set; }

    public bool Restoring => Active && Elapsed >= ThemeSkills.ShrinkHoldSeconds;

    public float Factor => Active ? FactorAt(Elapsed, ThemeSkills.ShrinkScale, ThemeSkills.ShrinkHoldSeconds, ThemeSkills.ShrinkRestoreSeconds) : 1f;

    public static float FactorAt(float t, float scale, float hold, float restore)
    {
        if (t < hold) return scale;
        if (restore <= 0f || t >= hold + restore) return 1f;
        return Mathf.Lerp(scale, 1f, (t - hold) / restore);
    }

    /// <summary>A cat was just created: give it the current factor.</summary>
    public void ApplyTo(Cat cat)
    {
        float f = Factor;
        if (cat != null && cat.Scale != f) cat.SetScale(f);
    }

    /// <summary>
    /// Bring the whole board to the current factor, and (only if Balance.shrinkRestorePausesDanger is on) hold the red-line
    /// timer during the grow-back. Call it every running frame, and when the effect starts or ends.
    /// </summary>
    public void Sync(SkillRuntime skills, List<Cat> cats, DangerTimer danger)
    {
        if (Active && skills.IsTimedActive) Elapsed = skills.TimedElapsed;
        float f = Factor;
        foreach (var cat in cats)
            if (cat != null && cat.Scale != f) cat.SetScale(f);
        if (Restoring && ThemeSkills.ShrinkRestorePausesDanger) danger.Pause(PauseReason);
        else danger.Resume(PauseReason);
    }
}

/// <summary>Shrink: all cats shrink to 0.85 for 6 s, then grow back over 1.5 s. The whole 7.5 s is one timed effect (no re-use while it runs).</summary>
public sealed class ShrinkHandler : ISkillHandler
{
    readonly ShrinkEffect _effect;
    readonly Action _changed;

    public ShrinkHandler(ShrinkEffect effect, Action changed)
    {
        _effect = effect;
        _changed = changed;
    }

    public float Duration => ThemeSkills.ShrinkHoldSeconds + ThemeSkills.ShrinkRestoreSeconds;
    public bool CanTarget(Cat target) => false;

    public bool Activate(Cat target)
    {
        if (_effect.Active) return false;                 // no stacking
        _effect.Active = true;
        _effect.Elapsed = 0f;
        _changed();
        return true;
    }

    public void End()
    {
        _effect.Active = false;
        _changed();                                       // everything back to exactly 1
    }
}
