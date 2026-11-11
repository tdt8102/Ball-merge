using UnityEngine;

/// <summary>White tab-bar icons drawn with the same raster toolkit as the cats (tint them with Image.color).</summary>
public static partial class CatSprites
{
    static readonly Sprite[] TabIcons = new Sprite[4];

    /// <summary>0 = gacha (gift), 1 = home, 2 = skin (t-shirt), 3 = shop (gem).</summary>
    public static Sprite TabIcon(int kind)
    {
        kind = Mathf.Clamp(kind, 0, 3);
        if (TabIcons[kind] != null) return TabIcons[kind];

        var cv = new Canvas(128, 1f, Color.white);
        var w = Color.white;
        switch (kind)
        {
            case 0: // gift box with a bow
                cv.Paint(Rect(0, -0.15f, 0.32f, 0.22f), w);
                cv.Paint(Rect(0, 0.13f, 0.37f, 0.08f), w);
                cv.Paint(Ellipse(-0.1f, 0.3f, 0.1f, 0.075f), w);
                cv.Paint(Ellipse(0.1f, 0.3f, 0.1f, 0.075f), w);
                cv.Erase(Rect(0, 0.05f, 0.4f, 0.012f));        // gap between lid and box
                cv.Erase(Rect(0, -0.08f, 0.03f, 0.3f));        // ribbon gap
                break;
            case 1: // house
                cv.Paint(Tri(new Vector2(-0.44f, 0.06f), new Vector2(0.44f, 0.06f), new Vector2(0, 0.44f)), w);
                cv.Paint(Rect(0, -0.17f, 0.31f, 0.23f), w);
                cv.Paint(Rect(0.23f, 0.3f, 0.05f, 0.09f), w);   // chimney
                cv.Erase(Rect(0, -0.27f, 0.085f, 0.13f));        // door
                break;
            case 2: // t-shirt
                cv.Paint(Rect(0, -0.1f, 0.21f, 0.3f), w);
                cv.Paint(Cap(-0.2f, 0.18f, -0.38f, 0.0f, 0.1f), w);
                cv.Paint(Cap(0.2f, 0.18f, 0.38f, 0.0f, 0.1f), w);
                cv.Paint(Rect(0, 0.16f, 0.22f, 0.04f), w);
                cv.Erase(Circle(0, 0.2f, 0.085f));               // neckline
                break;
            default: // gem
                cv.Paint(Tri(new Vector2(-0.42f, 0.1f), new Vector2(-0.22f, 0.36f), new Vector2(0.22f, 0.36f)), w);
                cv.Paint(Tri(new Vector2(-0.42f, 0.1f), new Vector2(0.22f, 0.36f), new Vector2(0.42f, 0.1f)), w);
                cv.Paint(Tri(new Vector2(-0.42f, 0.1f), new Vector2(0.42f, 0.1f), new Vector2(0, -0.4f)), w);
                cv.Erase(Cap(-0.42f, 0.1f, 0.42f, 0.1f, 0.012f));
                cv.Erase(Cap(-0.22f, 0.36f, -0.1f, 0.1f, 0.012f));
                cv.Erase(Cap(0.22f, 0.36f, 0.1f, 0.1f, 0.012f));
                cv.Erase(Cap(-0.1f, 0.1f, 0f, -0.4f, 0.012f));
                cv.Erase(Cap(0.1f, 0.1f, 0f, -0.4f, 0.012f));
                break;
        }
        TabIcons[kind] = MakeSprite(cv, 128, 1f);
        return TabIcons[kind];
    }
}
