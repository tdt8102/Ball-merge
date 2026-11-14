using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Board logic of the Midas skill (no scene needed, so it can be tested).</summary>
public static class Midas
{
    /// <summary>On the board and in the sellable level range (Balance: 0..7).</summary>
    public static bool CanSell(Cat cat) => Eraser.CanErase(cat) && ThemeSkills.CanMidas(cat.Level);

    /// <summary>
    /// Sell a cat: remove it from play (same lifecycle as Eraser) and pay 3 + 2 * level coins through <paramref name="pay"/>,
    /// once, only if the removal worked. No score, discovery or energy; the payment is not a merge reward, so no Sugar
    /// Rush multiplier. Returns false (nothing changed, nothing paid) for an invalid target.
    /// </summary>
    public static bool TrySell(List<Cat> cats, Cat target, Action<GameObject> destroy, Action<int> pay)
    {
        if (!CanSell(target)) return false;
        int coins = ThemeSkills.MidasCoin(target.Level);
        if (!Eraser.TryErase(cats, target, destroy)) return false;
        pay(coins);
        return true;
    }
}

/// <summary>Midas: tap a cat of level 0..7 to sell it for coins.</summary>
public sealed class MidasHandler : ISkillHandler
{
    readonly Func<Cat, bool> _sell;

    public MidasHandler(Func<Cat, bool> sell) { _sell = sell; }

    public float Duration => 0f;
    public bool CanTarget(Cat target) => Midas.CanSell(target);
    public bool Activate(Cat target) => target != null && _sell(target);
    public void End() { }
}
