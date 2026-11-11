using System;
using UnityEngine;

/// <summary>
/// All tunable numbers in one place. Defaults live here; Assets/Resources/balance.json overrides any field it contains
/// (missing fields keep the defaults), so the game can be rebalanced without touching code.
/// </summary>
[Serializable]
public class Balance
{
    // ---- cats: one entry per level (11) ----
    public float[] radius = { 0.25f, 0.32f, 0.40f, 0.48f, 0.58f, 0.69f, 0.81f, 0.94f, 1.08f, 1.23f, 1.40f };   // world units, container is 6 wide
    public int[] score = { 0, 3, 6, 10, 15, 21, 28, 36, 45, 55, 66 };                                          // for a merge that CREATES this level
    public int[] dropBag = { 0, 0, 0, 1, 1, 1, 2, 2, 3, 4 };                                                   // next cat is picked from this list

    // ---- scoring and coins ----
    public int maxMergeScore = 100;          // two biggest cats vanishing
    public int mergeCoinBase = 1;            // coins per merge = base + createdLevel / mergeCoinDivisor
    public int mergeCoinDivisor = 3;
    public int maxMergeCoins = 30;           // two biggest cats vanishing
    public int discoveryBase = 10;           // first time a cat appears = base + level * step (starter cat gives 0)
    public int discoveryStep = 10;

    // ---- gacha ----
    public int gachaCost = 150;              // coins for one pull (10 pulls = 10x this)
    public int gachaRateRare = 25;           // percent chance of a Rare item
    public int gachaRateLegendary = 5;       // percent chance of a Legendary item (the rest is Common)
    public int gachaPity = 30;               // a Legendary is guaranteed every this many pulls (0 = off)
    public float dupRefundCommon = 0.25f;    // a duplicate gives back this part of the price of one pull; rarer items give more
    public float dupRefundRare = 0.4f;
    public float dupRefundLegendary = 0.6f;

    // ---- gameplay ----
    public float gravity = 20f;
    public float friction = 0.35f;
    public float bounciness = 0.08f;
    public float dropCooldown = 0.45f;       // seconds between drops
    public float dangerGrace = 1.2f;         // a freshly dropped cat may be above the red line this long
    public float gameOverDelay = 2.0f;       // seconds above the red line before it is over
    public float comboWindow = 1.2f;         // max gap between merges to count as a combo

    static Balance _current;

    public static Balance Current => _current ?? (_current = Load());

    static Balance Load()
    {
        var b = new Balance();
        var asset = Resources.Load<TextAsset>("balance");
        if (asset != null)
        {
            try { JsonUtility.FromJsonOverwrite(asset.text, b); }
            catch (Exception e) { Debug.LogWarning("balance.json ignored: " + e.Message); b = new Balance(); }
        }
        b.Validate();
        return b;
    }

    /// <summary>Arrays must have exactly one entry per cat level; otherwise fall back to the built-in values.</summary>
    void Validate()
    {
        var d = new Balance();
        int n = CatDatabase.MaxLevel + 1;
        if (radius == null || radius.Length != n) { Debug.LogWarning("balance.json: 'radius' needs " + n + " values, using defaults"); radius = d.radius; }
        if (score == null || score.Length != n) { Debug.LogWarning("balance.json: 'score' needs " + n + " values, using defaults"); score = d.score; }
        if (dropBag == null || dropBag.Length == 0) { dropBag = d.dropBag; }
        for (int i = 0; i < dropBag.Length; i++) dropBag[i] = Mathf.Clamp(dropBag[i], 0, CatDatabase.MaxLevel);
        mergeCoinDivisor = Mathf.Max(1, mergeCoinDivisor);
        gachaCost = Mathf.Max(1, gachaCost);
        gachaRateRare = Mathf.Clamp(gachaRateRare, 0, 100);
        gachaRateLegendary = Mathf.Clamp(gachaRateLegendary, 0, 100 - gachaRateRare);
    }
}
