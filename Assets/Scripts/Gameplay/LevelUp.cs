using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Board logic of the Level Up skill (no scene needed, so it can be tested).</summary>
public static class LevelUp
{
    /// <summary>On the board (not the cat in hand, not merging, not destroyed) and in the allowed level range (Balance: 0..8, result at most 9).</summary>
    public static bool CanUpgrade(Cat cat) =>
        cat != null && cat.Dropped && !cat.Merging && ThemeSkills.CanLevelUp(cat.Level);

    /// <summary>
    /// Replace a cat by the same cat one level up. The new cat comes from the same helper a merge uses (create), so sprite,
    /// eyes, collider radius and mass are all those of the new level; it takes over position order in the list, rotation
    /// and speed. The old cat leaves the way an erased or merged cat does (flagged Merging, switched off, destroyed).
    /// No score, coins or energy are involved. Returns the new cat, or null (nothing changed) for an invalid target.
    /// </summary>
    public static Cat TryUpgrade(List<Cat> cats, Cat target, Func<int, Vector2, Cat> create, Action<GameObject> destroy)
    {
        if (!CanUpgrade(target)) return null;
        int index = cats.IndexOf(target);
        if (index < 0) return null;                                            // not on the board

        var upgraded = create(target.Level + 1, target.transform.position);
        upgraded.CopyMotionFrom(target);
        cats[index] = upgraded;

        target.Merging = true;                                                 // a queued collision callback must not merge it
        target.gameObject.SetActive(false);
        destroy(target.gameObject);
        return upgraded;
    }
}

/// <summary>Level Up: tap a cat of level 0..8 to raise it exactly one level.</summary>
public sealed class LevelUpHandler : ISkillHandler
{
    readonly Func<Cat, bool> _upgrade;

    public LevelUpHandler(Func<Cat, bool> upgrade) { _upgrade = upgrade; }

    public float Duration => 0f;
    public bool CanTarget(Cat target) => LevelUp.CanUpgrade(target);
    public bool Activate(Cat target) => target != null && _upgrade(target);
    public void End() { }
}
