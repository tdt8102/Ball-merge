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

    // ---- Theme Set Skills: energy ----
    public int themeSkillMaxEnergy = 100;       // percent
    public int themeSkillStartingEnergy = 30;   // percent at the start of a new run
    public int themeSkillEnergyPerMerge = 6;    // percent per normal merge
    public int themeSkillCost = 100;            // percent spent by one skill use

    // ---- Theme Set Skills: shards ----
    public int shardRewardPerDuplicateCat = 1;  // only duplicate cats, never backgrounds/frames
    public int shardCostCommon = 20;
    public int shardCostRare = 30;
    public int shardCostLegendary = 40;
    public int shardCostMajesty = 50;           // reserved for the level-10 "Majesty" cat

    // ---- Theme Set Skills: individual skills ----
    public float hugDuration = 3f;              // seconds
    public float hugRadius = 3f;                // world units
    public float hugAcceleration = 8f;          // strongest pull in units/s^2 (gravity is 20), fades to 0 at the radius; same for every cat mass
    public float hugMaxSpeed = 2f;              // the pull never builds more speed than this towards the partner (units/s)
    public float shrinkScale = 0.85f;           // visual scale and collider radius multiplier
    public float shrinkDuration = 6f;           // seconds at the small size
    public float shrinkRestoreDuration = 1.5f;  // seconds to grow back
    public bool shrinkRestorePausesDanger = false;  // true = the red-line timer is also held while the cats grow back (not approved yet: off)
    public float moonlightDuration = 8f;        // seconds the danger timer is paused
    public float sugarRushDuration = 10f;       // seconds
    public float sugarRushScoreMultiplier = 2f; // merge score only
    public float sugarRushCoinMultiplier = 1.5f; // merge coin only
    public int levelUpMinLevel = 0;             // target levels allowed
    public int levelUpMaxLevel = 8;
    public int levelUpMaxResult = 9;            // the skill must never create level 10
    public int midasMinLevel = 0;               // target levels allowed
    public int midasMaxLevel = 7;
    public int midasBaseCoin = 3;               // coins = base + perLevel * cat level
    public int midasCoinPerLevel = 2;

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

        // Theme Set Skills. Missing fields in an older balance.json keep the defaults above.
        themeSkillMaxEnergy = Mathf.Max(1, themeSkillMaxEnergy);
        themeSkillStartingEnergy = Mathf.Clamp(themeSkillStartingEnergy, 0, themeSkillMaxEnergy);
        themeSkillEnergyPerMerge = Mathf.Max(0, themeSkillEnergyPerMerge);
        themeSkillCost = Mathf.Clamp(themeSkillCost, 1, themeSkillMaxEnergy);

        shardRewardPerDuplicateCat = Mathf.Max(0, shardRewardPerDuplicateCat);
        shardCostCommon = Mathf.Max(0, shardCostCommon);
        shardCostRare = Mathf.Max(0, shardCostRare);
        shardCostLegendary = Mathf.Max(0, shardCostLegendary);
        shardCostMajesty = Mathf.Max(0, shardCostMajesty);

        hugDuration = Mathf.Max(0f, hugDuration);
        hugRadius = Mathf.Max(0f, hugRadius);
        hugAcceleration = Mathf.Max(0f, hugAcceleration);
        hugMaxSpeed = Mathf.Max(0f, hugMaxSpeed);
        shrinkScale = Mathf.Clamp(shrinkScale, 0.1f, 1f);
        shrinkDuration = Mathf.Max(0f, shrinkDuration);
        shrinkRestoreDuration = Mathf.Max(0f, shrinkRestoreDuration);
        moonlightDuration = Mathf.Max(0f, moonlightDuration);
        sugarRushDuration = Mathf.Max(0f, sugarRushDuration);
        sugarRushScoreMultiplier = Mathf.Max(0f, sugarRushScoreMultiplier);
        sugarRushCoinMultiplier = Mathf.Max(0f, sugarRushCoinMultiplier);
        int levelUpTargetCap = Mathf.Min(ThemeSkills.LevelUpMaxTargetLevel, CatDatabase.MaxLevel);
        int levelUpResultCap = Mathf.Min(ThemeSkills.LevelUpMaxResultLevel, CatDatabase.MaxLevel);
        levelUpMinLevel = Mathf.Clamp(levelUpMinLevel, 0, levelUpTargetCap);
        levelUpMaxLevel = Mathf.Clamp(levelUpMaxLevel, levelUpMinLevel, levelUpTargetCap);
        int levelUpResultFloor = Mathf.Min(levelUpMaxLevel + 1, levelUpResultCap);
        levelUpMaxResult = Mathf.Clamp(levelUpMaxResult, levelUpResultFloor, levelUpResultCap);
        int midasTargetCap = Mathf.Min(ThemeSkills.MidasMaxTargetLevel, CatDatabase.MaxLevel);
        midasMinLevel = Mathf.Clamp(midasMinLevel, 0, midasTargetCap);
        midasMaxLevel = Mathf.Clamp(midasMaxLevel, midasMinLevel, midasTargetCap);
        midasBaseCoin = Mathf.Max(0, midasBaseCoin);
        midasCoinPerLevel = Mathf.Max(0, midasCoinPerLevel);
    }
}
