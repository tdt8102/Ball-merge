using UnityEngine;

/// <summary>White tab-bar icons drawn with the same raster toolkit as the cats (tint them with Image.color).</summary>
public static partial class CatSprites
{
    static readonly Sprite[] TabIcons = new Sprite[4];
    static Sprite _coin;

    /// <summary>Gold coin with a paw print.</summary>
    public static Sprite CoinIcon()
    {
        if (_coin != null) return _coin;
        var cv = new Canvas(128, 1f, new Color(0.6f, 0.38f, 0.05f));
        Color rim = new Color(0.62f, 0.4f, 0.06f), gold = new Color(1f, 0.8f, 0.22f), ring = new Color(0.93f, 0.66f, 0.1f), paw = new Color(0.8f, 0.5f, 0.05f);
        cv.Paint(Circle(0, 0, 0.5f), rim);
        cv.Paint(Circle(0, 0, 0.43f), gold);
        cv.Paint(Circle(0, 0, 0.34f), ring);
        cv.Paint(Circle(0, 0, 0.3f), gold);
        cv.Paint(Ellipse(0, -0.07f, 0.13f, 0.1f), paw);                       // pad
        foreach (var t in new[] { new Vector2(-0.16f, 0.08f), new Vector2(-0.055f, 0.17f), new Vector2(0.055f, 0.17f), new Vector2(0.16f, 0.08f) })
            cv.Paint(Circle(t.x, t.y, 0.052f), paw);                          // toes
        cv.Paint(Cap(-0.3f, 0.2f, -0.18f, 0.32f, 0.018f), new Color(1f, 1f, 0.85f, 0.8f));   // shine
        _coin = MakeSprite(cv, 128, 1f);
        return _coin;
    }

    static Sprite _shard;

    /// <summary>Star shard: a blue five-point star (so it is never mistaken for the gold coin).</summary>
    public static Sprite ShardIcon()
    {
        if (_shard != null) return _shard;
        var cv = new Canvas(128, 1f, new Color(0.25f, 0.5f, 0.9f));
        Color rim = new Color(0.22f, 0.42f, 0.86f), fill = new Color(0.55f, 0.82f, 1f), light = new Color(0.86f, 0.96f, 1f);

        var tris = new Shape[10];
        var outer = new Vector2[5];
        var inner = new Vector2[5];
        for (int i = 0; i < 5; i++)
        {
            float a = Mathf.PI * 0.5f + i * Mathf.PI * 0.4f;
            outer[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.47f;
            float b = a + Mathf.PI * 0.2f;
            inner[i] = new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 0.21f;
        }
        for (int i = 0; i < 5; i++)
        {
            tris[i * 2] = Tri(Vector2.zero, outer[i], inner[i]);
            tris[i * 2 + 1] = Tri(Vector2.zero, inner[i], outer[(i + 1) % 5]);
        }
        var star = Union(tris);
        cv.Paint(Grow(star, 0.03f), rim);
        cv.Paint(star, fill);
        cv.Paint(Union(Tri(Vector2.zero, outer[0], inner[0]), Tri(Vector2.zero, inner[4], outer[0])), light);   // lit top point
        cv.Paint(Ellipse(-0.1f, 0.12f, 0.05f, 0.035f), new Color(1f, 1f, 1f, 0.8f));                              // shine
        _shard = MakeSprite(cv, 128, 1f);
        return _shard;
    }

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
