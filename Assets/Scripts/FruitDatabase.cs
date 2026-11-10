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

    public static readonly Color[] Colors =
    {
        Hex("E8505B"), // 0 cherry
        Hex("FF8FA3"), // 1 strawberry
        Hex("B388EB"), // 2 grape
        Hex("FFA94D"), // 3 orange
        Hex("F76707"), // 4 persimmon
        Hex("E03131"), // 5 apple
        Hex("FFE066"), // 6 pear
        Hex("FFC9A8"), // 7 peach
        Hex("FFD43B"), // 8 pineapple
        Hex("74C69D"), // 9 melon
        Hex("2B8A3E"), // 10 watermelon
    };

    // Weighted random drop level (small ones more likely).
    static readonly int[] DropBag = { 0, 0, 0, 1, 1, 1, 2, 2, 3, 4 };

    public static int RandomDropLevel() => DropBag[Random.Range(0, DropBag.Length)];

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
