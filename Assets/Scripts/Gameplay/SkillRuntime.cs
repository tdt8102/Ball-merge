using System;
using System.Collections.Generic;

/// <summary>What one theme skill does. The 7 real skills implement this in later phases; this phase only has a test handler.</summary>
public interface ISkillHandler
{
    /// <summary>Seconds a timed effect lasts after Activate succeeded; 0 = the effect is over immediately.</summary>
    float Duration { get; }

    /// <summary>Targeted skills: may this cat be chosen? No side effects.</summary>
    bool CanTarget(Cat target);

    /// <summary>Do it (target is null for instant skills). Return true only if it really happened: only then is energy spent.</summary>
    bool Activate(Cat target);

    /// <summary>A timed effect ended, or the run ended while it was running.</summary>
    void End();
}

/// <summary>Where handlers are plugged in: SkillHandlers.Register(skillId, () => new MyHandler()). No skill has one yet.</summary>
public static class SkillHandlers
{
    static readonly Dictionary<string, Func<ISkillHandler>> Factories = new Dictionary<string, Func<ISkillHandler>>();

    public static void Register(string skillId, Func<ISkillHandler> factory) => Factories[skillId] = factory;
    public static void Clear() => Factories.Clear();

    public static ISkillHandler Create(ThemeSkillDef skill) =>
        skill != null && Factories.TryGetValue(skill.Id, out var f) ? f() : null;
}

public enum SkillMode { Idle, Targeting }
public enum SkillPress { Ignored, NotImplemented, NotEnoughEnergy, Activated, Failed, EnteredTargeting, CancelledTargeting }
public enum SkillTap { Ignored, Cancelled, InvalidTarget, Activated, Failed }

/// <summary>
/// Activation framework for the skill of one run, on top of SkillResolver (which skill) and SkillEnergy (how much).
/// Plain class: GameManager calls Press (the HUD button), Tap (a board tap while choosing a target), Tick (game time,
/// only while the game is running, so it stops by itself during Pause/Settings) and Lock (Game Over).
/// Energy is spent only when the handler reports success: not for a cancel, an invalid target or a failed activation.
/// </summary>
public sealed class SkillRuntime
{
    readonly SkillEnergy _energy;
    ISkillHandler _handler;

    public SkillRuntime(SkillEnergy energy) { _energy = energy; }

    public ThemeSkillDef Skill { get; private set; }
    public SkillMode Mode { get; private set; }
    public bool Locked { get; private set; }
    public float TimedRemaining { get; private set; }
    public float TimedDuration { get; private set; }

    public float TimedElapsed => TimedDuration - TimedRemaining;      // game-time seconds since the timed effect started

    public bool Available => Skill != null;                       // the HUD button is shown only then
    public bool IsTargeting => Mode == SkillMode.Targeting;       // the board drop is blocked while true
    public bool IsTimedActive => TimedRemaining > 0f;
    public bool Ready => Skill != null && !Locked && _handler != null && !IsTimedActive && _energy.CanAfford;

    /// <summary>A run starts or resumes (also Restart): forget everything from the previous run.</summary>
    public void Begin(ThemeSkillDef skill, ISkillHandler handler)
    {
        Stop();
        Skill = skill;
        _handler = skill == null ? null : handler;
        Locked = false;
    }

    /// <summary>The run ended (Game Over): leave target mode, stop a running effect, refuse everything.</summary>
    public void Lock()
    {
        Stop();
        Locked = true;
    }

    void Stop()
    {
        Mode = SkillMode.Idle;
        if (IsTimedActive) { TimedRemaining = 0f; _handler?.End(); }
        TimedRemaining = TimedDuration = 0f;
    }

    /// <summary>The skill button was pressed.</summary>
    public SkillPress Press()
    {
        if (Skill == null || Locked) return SkillPress.Ignored;
        if (Mode == SkillMode.Targeting) { Mode = SkillMode.Idle; return SkillPress.CancelledTargeting; }   // free
        if (_handler == null) return SkillPress.NotImplemented;
        if (IsTimedActive) return SkillPress.Ignored;
        if (!_energy.CanAfford) return SkillPress.NotEnoughEnergy;
        if (Skill.Kind == ThemeSkillKind.Target) { Mode = SkillMode.Targeting; return SkillPress.EnteredTargeting; }
        return Perform(null) ? SkillPress.Activated : SkillPress.Failed;
    }

    /// <summary>
    /// A tap on the board while choosing a target. null = the tap was not on a cat: cancel for free. A cat the handler
    /// rejects is ignored and the mode stays on.
    /// </summary>
    public SkillTap Tap(Cat target)
    {
        if (Mode != SkillMode.Targeting || Locked) return SkillTap.Ignored;
        if (target == null) { Mode = SkillMode.Idle; return SkillTap.Cancelled; }
        if (!_handler.CanTarget(target)) return SkillTap.InvalidTarget;
        if (!Perform(target)) return SkillTap.Failed;
        Mode = SkillMode.Idle;
        return SkillTap.Activated;
    }

    bool Perform(Cat target)
    {
        if (!_energy.CanAfford || !_handler.Activate(target)) return false;
        _energy.Consume();
        TimedDuration = TimedRemaining = Math.Max(0f, _handler.Duration);   // 0 = no timed part, nothing to tick or end
        return true;
    }

    /// <summary>Advance a timed effect by game time. The caller does not call it while paused or over.</summary>
    public void Tick(float dt)
    {
        if (Locked || !IsTimedActive || dt <= 0f) return;
        TimedRemaining -= dt;
        if (TimedRemaining <= 0f) { TimedRemaining = 0f; _handler.End(); }
    }
}
