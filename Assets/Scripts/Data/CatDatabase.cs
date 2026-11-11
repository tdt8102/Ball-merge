using UnityEngine;

/// <summary>Per-level cat data. The numbers come from Balance (Assets/Resources/balance.json); colours and names live here.</summary>
public static class CatDatabase
{
    public const int MaxLevel = 10;      // index of the biggest cat
    public const int MaxDropLevel = 4;   // player can only drop levels 0..4

    /// <summary>Radius in world units (container is 6 units wide).</summary>
    public static float[] Radius => Balance.Current.radius;

    /// <summary>Score awarded when a merge CREATES this level.</summary>
    public static int[] Score => Balance.Current.score;

    // Cat theme: the "levels" are cats, coat colour per level (art is drawn in CatSprites).
    public static readonly Color[] Colors =
    {
        Hex("F4F0E8"), // 0 white
        Hex("AEB4BE"), // 1 gray tabby
        Hex("3B3B46"), // 2 black
        Hex("F2A154"), // 3 orange tabby
        Hex("F6F1E8"), // 4 calico
        Hex("EFE0C8"), // 5 siamese
        Hex("A56B3F"), // 6 brown tabby
        Hex("3B3B46"), // 7 tuxedo
        Hex("7F93AD"), // 8 russian blue + bow
        Hex("EBC45B"), // 9 golden + crown
        Hex("9B7BD4"), // 10 royal purple + big crown
    };

    public static readonly string[] Names =
    {
        "Snowy", "Ash", "Shadow", "Ginger", "Patches", "Siam", "Cocoa", "Tux", "Sir Blue", "Goldie", "Majesty"
    };

    /// <summary>Coins for the first time this cat ever appears (the starter cat gives nothing).</summary>
    public static int DiscoveryReward(int level) => level <= 0 ? 0 : Balance.Current.discoveryBase + Balance.Current.discoveryStep * level;

    /// <summary>Coins for a merge that creates this level (two max cats vanishing is worth more).</summary>
    public static int MergeCoins(int createdLevel) =>
        createdLevel > MaxLevel ? Balance.Current.maxMergeCoins : Balance.Current.mergeCoinBase + createdLevel / Balance.Current.mergeCoinDivisor;

    static readonly Color[] Confetti =
    {
        Hex("FF8FA3"), Hex("FFD43B"), Hex("74C69D"), Hex("B388EB"), Hex("FFA94D"), Hex("7FC8F8")
    };
    public static Color[] ConfettiColors => Confetti;

    /// <summary>Colour for merge sparks/rings: the coat mixed with pink so white and black cats stay visible.</summary>
    public static Color BurstColor(int level) => Color.Lerp(Colors[level], Hex("FF8FA3"), 0.5f);

    /// <summary>Weighted random drop level (the bag lists levels; repeats make a level more likely).</summary>
    public static int RandomDropLevel()
    {
        var bag = Balance.Current.dropBag;
        return bag[Random.Range(0, bag.Length)];
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
