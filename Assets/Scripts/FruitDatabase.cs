using UnityEngine;

/// <summary>Static tuning table for the 11 merge levels. Edit here to rebalance.</summary>
public static class FruitDatabase
{
    public const int MaxLevel = 10;      // index of the biggest fruit
    public const int MaxDropLevel = 4;   // player can only drop levels 0..4

    // Radius in world units (container is 6 units wide).
    public static readonly float[] Radius =
    {
        0.25f, 0.32f, 0.40f, 0.48f, 0.58f, 0.69f, 0.81f, 0.94f, 1.08f, 1.23f, 1.40f
    };

    // Score awarded when a merge CREATES this level.
    public static readonly int[] Score =
    {
        0, 3, 6, 10, 15, 21, 28, 36, 45, 55, 66
    };

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

    static readonly Color[] Confetti =
    {
        Hex("FF8FA3"), Hex("FFD43B"), Hex("74C69D"), Hex("B388EB"), Hex("FFA94D"), Hex("7FC8F8")
    };
    public static Color[] ConfettiColors => Confetti;

    /// <summary>Colour for merge sparks/rings: the coat mixed with pink so white and black cats stay visible.</summary>
    public static Color BurstColor(int level) => Color.Lerp(Colors[level], Hex("FF8FA3"), 0.5f);

    // Weighted random drop level (small ones more likely).
    static readonly int[] DropBag = { 0, 0, 0, 1, 1, 1, 2, 2, 3, 4 };

    public static int RandomDropLevel() => DropBag[Random.Range(0, DropBag.Length)];

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
