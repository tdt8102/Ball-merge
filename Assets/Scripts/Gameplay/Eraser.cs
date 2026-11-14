using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Board helpers for targeted skills (no scene needed, so they can be tested).</summary>
public static class Eraser
{
    /// <summary>
    /// The cat under a point: the one whose centre is nearest among the cats that contain the point (touching cats overlap
    /// a little), or null for empty space. Destroyed cats are skipped.
    /// </summary>
    public static Cat CatAt(List<Cat> cats, Vector2 point)
    {
        Cat best = null;
        float bestSq = float.MaxValue;
        foreach (var c in cats)
        {
            if (c == null) continue;
            float sq = ((Vector2)c.transform.position - point).sqrMagnitude;
            if (sq <= c.Radius * c.Radius && sq < bestSq) { best = c; bestSq = sq; }
        }
        return best;
    }

    /// <summary>Is this cat on the board and free to be removed (not the cat in hand, not already merging, not destroyed)?</summary>
    public static bool CanErase(Cat cat) => cat != null && cat.Dropped && !cat.Merging;

    /// <summary>
    /// Remove one cat from play: no score, coins, merge, discovery or energy, because none of those paths is called.
    /// Same lifecycle as a merge: it leaves the list, is flagged Merging (so a collision callback still queued for this
    /// frame, the safety scan, the danger check and the save all skip it), is switched off at once (no more contacts) and
    /// is destroyed by the caller's destroy. Returns false, changing nothing, if the cat is not a valid target.
    /// </summary>
    public static bool TryErase(List<Cat> cats, Cat target, Action<GameObject> destroy)
    {
        if (!CanErase(target) || !cats.Remove(target)) return false;     // not on the board (or already erased)
        target.Merging = true;
        target.gameObject.SetActive(false);
        destroy(target.gameObject);
        return true;
    }
}

/// <summary>Eraser: tap any cat on the board to remove it. Every level is valid.</summary>
public sealed class EraserHandler : ISkillHandler
{
    readonly Func<Cat, bool> _erase;

    public EraserHandler(Func<Cat, bool> erase) { _erase = erase; }

    public float Duration => 0f;                                    // instant effect, nothing to time
    public bool CanTarget(Cat target) => Eraser.CanErase(target);
    public bool Activate(Cat target) => target != null && _erase(target);
    public void End() { }                                           // nothing to stop
}
