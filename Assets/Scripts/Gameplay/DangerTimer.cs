using System.Collections.Generic;

/// <summary>
/// The red-line timer of the run (the one GameManager.CheckDanger always had: it grows while a cat rests above the line
/// and drops back to 0 when the board is safe). It can now also be held by named pause reasons, e.g. "moonlight".
/// While ANY reason is active the value is frozen (no growth, no reset); it runs again only when all are removed.
/// Pause/Settings and Game Over need no reason: GameManager does not step the timer then.
/// </summary>
public sealed class DangerTimer
{
    readonly HashSet<string> _pauses = new HashSet<string>();

    public float Value { get; private set; }
    public bool Paused => _pauses.Count > 0;

    /// <summary>One frame: grow while in danger, reset when safe, untouched while paused.</summary>
    public void Step(bool danger, float dt)
    {
        if (Paused) return;
        Value = danger ? Value + dt : 0f;
    }

    public bool Reached(float limit) => Value >= limit;

    public void Pause(string reason) { if (!string.IsNullOrEmpty(reason)) _pauses.Add(reason); }
    public void Resume(string reason) => _pauses.Remove(reason);

    /// <summary>A new run: value 0 and no pause reasons left over.</summary>
    public void Reset()
    {
        Value = 0f;
        _pauses.Clear();
    }
}

/// <summary>Moonlight: holds the danger timer for its duration (Balance.moonlightDuration) and then lets it run on from the held value.</summary>
public sealed class MoonlightHandler : ISkillHandler
{
    public const string PauseReason = "moonlight";
    readonly DangerTimer _timer;

    public MoonlightHandler(DangerTimer timer) { _timer = timer; }

    public float Duration => ThemeSkills.MoonlightSeconds;
    public bool CanTarget(Cat target) => false;

    public bool Activate(Cat target)
    {
        _timer.Pause(PauseReason);
        return true;
    }

    public void End() => _timer.Resume(PauseReason);
}
