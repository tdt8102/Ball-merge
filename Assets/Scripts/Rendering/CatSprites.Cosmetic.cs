using System.Collections.Generic;
using UnityEngine;

/// <summary>Art for the Skin and Gacha screens, drawn with the same raster toolkit as the cats: tray previews, repeating background motifs, the gift box.</summary>
public static partial class CatSprites
{
    // ------------------------------------------------------------------ extra shapes

    static Shape Star(float cx, float cy, float s) => Union(Ellipse(cx, cy, s * 0.28f, s), Ellipse(cx, cy, s, s * 0.28f));

    static Shape RoundRect(float cx, float cy, float hx, float hy, float r) => Grow(Rect(cx, cy, hx - r, hy - r), r);

    /// <summary>Everything except the given shape (use it as a clip to keep the outside).</summary>
    static Shape Outside(Shape s)
    {
        var d = s.D;
        return new Shape { D = (u, v) => -d(u, v), X0 = -10f, Y0 = -10f, X1 = 10f, Y1 = 10f };
    }

    /// <summary>Diagonal bands: the centre of band k is at offset + k * period, each half * 2 wide.</summary>
    static Shape Bands(float period, float offset, float half) => new Shape
    {
        D = (u, v) => (Mathf.Abs(Mathf.Repeat(u + v - offset + period * 0.5f, period) - period * 0.5f) - half) * 0.7071f,
        X0 = -10f, Y0 = -10f, X1 = 10f, Y1 = 10f
    };

    static Shape Paw(float x, float y, float s)
    {
        var toes = new[] { new Vector2(-0.9f, 0.3f), new Vector2(-0.35f, 0.85f), new Vector2(0.35f, 0.85f), new Vector2(0.9f, 0.3f) };
        var parts = new Shape[5];
        parts[0] = Ellipse(x, y - 0.3f * s, 0.75f * s, 0.6f * s);
        for (int i = 0; i < 4; i++) parts[i + 1] = Circle(x + toes[i].x * s, y + toes[i].y * s, 0.32f * s);
        return Union(parts);
    }

    static Shape Mark(Motif m, float x, float y, float s)
    {
        switch (m)
        {
            case Motif.Stars: return Star(x, y, s * 1.6f);
            case Motif.Paws: return Paw(x, y, s * 0.9f);
            default: return Circle(x, y, s);
        }
    }

    /// <summary>Repeat a motif over the canvas (staggered rows). Stripes ignore the spacing grid.</summary>
    static void PaintMotif(Canvas cv, Motif m, Color c, float spacing, float size, Shape clip)
    {
        if (m == Motif.None) return;
        if (m == Motif.Stripes) { cv.Paint(Bands(spacing * 2f, 0f, size), c, clip); return; }
        int n = Mathf.CeilToInt(0.6f / spacing);
        for (int row = -n; row <= n; row++)
        for (int col = -n; col <= n; col++)
            cv.Paint(Mark(m, col * spacing + ((row & 1) != 0 ? spacing * 0.5f : 0f), row * spacing * 0.9f, size), c, clip);
    }

    // ------------------------------------------------------------------ tray preview (frame + background)

    static readonly Dictionary<int, Sprite> Trays = new Dictionary<int, Sprite>();

    /// <summary>
    /// A small picture of the playfield: the background colour, the tray with this frame, and its inside with the background's motif.
    /// Used by the tiles of the Skin tab and the gacha cards.
    /// </summary>
    public static Sprite Tray(FrameStyle frame, BgStyle bg)
    {
        int key = frame.Index * 100 + bg.Index;
        if (Trays.TryGetValue(key, out var cached)) return cached;

        var cv = new Canvas(160, 1f, bg.Outer);
        cv.Paint(RoundRect(0, 0, 0.5f, 0.5f, 0.14f), bg.Outer);

        var outer = RoundRect(0, 0, 0.34f, 0.42f, 0.07f);
        var inner = RoundRect(0, 0, 0.285f, 0.365f, 0.03f);
        cv.Paint(outer, frame.First);
        if (frame.Bands != null)
        {
            float w = 0.07f;
            for (int i = 1; i < frame.Bands.Length; i++)
                cv.Paint(Bands(w * frame.Bands.Length, i * w, w * 0.5f), frame.Bands[i], outer);
        }
        if (frame.Accent.a > 0f)
            cv.Paint(RoundRect(0, 0, 0.3205f, 0.4005f, 0.05f), frame.Accent, Outside(RoundRect(0, 0, 0.3045f, 0.3845f, 0.05f)));

        cv.Paint(inner, bg.Panel);
        PaintMotif(cv, bg.Motif, bg.MotifColor, 0.12f, 0.018f, inner);

        var sprite = MakeSprite(cv, 160, 1f);
        Trays[key] = sprite;
        return sprite;
    }

    // ------------------------------------------------------------------ repeating motif for the real background

    static readonly Dictionary<string, Sprite> Tiles = new Dictionary<string, Sprite>();

    /// <summary>
    /// A square tile of the motif that repeats seamlessly (SpriteRenderer draw mode Tiled). One tile is <paramref name="worldSize"/> world units wide.
    /// Returns null for backgrounds without a motif.
    /// </summary>
    public static Sprite MotifTile(BgStyle bg, float worldSize)
    {
        if (bg.Motif == Motif.None) return null;
        if (Tiles.TryGetValue(bg.Id, out var cached)) return cached;

        const int n = 128;
        var cv = new Canvas(n, 1f, bg.MotifColor);
        if (bg.Motif == Motif.Stripes)
            cv.Paint(Bands(0.5f, 0f, 0.07f), bg.MotifColor);          // period 0.5 divides the tile, so the bands continue across tiles
        else
        {
            cv.Paint(Mark(bg.Motif, -0.25f, 0.25f, 0.07f), bg.MotifColor);
            cv.Paint(Mark(bg.Motif, 0.25f, -0.25f, 0.07f), bg.MotifColor);
        }
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
        tex.SetPixels(cv.Px);
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n / worldSize, 0, SpriteMeshType.FullRect);
        Tiles[bg.Id] = sprite;
        return sprite;
    }

    // ------------------------------------------------------------------ gacha art

    static Sprite _box, _lid, _burst, _glow;

    static readonly Color BoxPink = new Color(0.95f, 0.5f, 0.65f), BoxEdge = new Color(0.72f, 0.27f, 0.42f),
                          Ribbon = new Color(1f, 0.82f, 0.29f), RibbonEdge = new Color(0.79f, 0.58f, 0.07f);

    /// <summary>Gift box without its lid (pair it with GiftLid at the same size and position).</summary>
    public static Sprite GiftBox()
    {
        if (_box != null) return _box;
        var cv = new Canvas(256, 1f, BoxEdge);
        var body = Rect(0, -0.15f, 0.33f, 0.27f);
        Outlined(cv, body, BoxPink, BoxEdge);
        cv.Paint(Rect(0, -0.38f, 0.33f, 0.06f), new Color(0.82f, 0.36f, 0.52f), body);                 // shade at the bottom
        cv.Paint(Cap(-0.25f, -0.06f, -0.25f, -0.28f, 0.016f), new Color(1f, 1f, 1f, 0.35f), body);     // shine
        Outlined(cv, Rect(0, -0.15f, 0.065f, 0.27f), Ribbon, RibbonEdge);
        _box = MakeSprite(cv, 256, 1f);
        return _box;
    }

    /// <summary>Lid and bow of the gift box.</summary>
    public static Sprite GiftLid()
    {
        if (_lid != null) return _lid;
        var cv = new Canvas(256, 1f, BoxEdge);
        Outlined(cv, Rect(0, 0.17f, 0.38f, 0.085f), new Color(0.98f, 0.62f, 0.76f), BoxEdge);
        Outlined(cv, Rect(0, 0.17f, 0.065f, 0.085f), Ribbon, RibbonEdge);
        Outlined(cv, Ellipse(-0.12f, 0.31f, 0.11f, 0.07f), Ribbon, RibbonEdge);
        Outlined(cv, Ellipse(0.12f, 0.31f, 0.11f, 0.07f), Ribbon, RibbonEdge);
        Outlined(cv, Circle(0, 0.29f, 0.05f), new Color(1f, 0.9f, 0.5f), RibbonEdge);
        _lid = MakeSprite(cv, 256, 1f);
        return _lid;
    }

    /// <summary>White sunburst that fades out towards its tips (tint it with the rarity colour and spin it behind the prize).</summary>
    public static Sprite Burst()
    {
        if (_burst != null) return _burst;
        const int n = 256;
        var cv = new Canvas(n, 1f, Color.white);
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6f, w = 0.11f;
            cv.Paint(Tri(Vector2.zero, new Vector2(Mathf.Cos(a - w), Mathf.Sin(a - w)) * 0.55f, new Vector2(Mathf.Cos(a + w), Mathf.Sin(a + w)) * 0.55f), Color.white);
        }
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float r = Mathf.Sqrt(((x + 0.5f) / n - 0.5f) * ((x + 0.5f) / n - 0.5f) + ((y + 0.5f) / n - 0.5f) * ((y + 0.5f) / n - 0.5f));
            cv.Px[y * n + x].a *= Mathf.Clamp01(1f - r / 0.5f);
        }
        _burst = MakeSprite(cv, n, 1f);
        return _burst;
    }

    /// <summary>Soft round glow (white, fading to nothing at the edge).</summary>
    public static Sprite Glow()
    {
        if (_glow != null) return _glow;
        const int n = 128;
        var cv = new Canvas(n, 1f, Color.white);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float r = Mathf.Sqrt(((x + 0.5f) / n - 0.5f) * ((x + 0.5f) / n - 0.5f) + ((y + 0.5f) / n - 0.5f) * ((y + 0.5f) / n - 0.5f));
            float a = Mathf.Clamp01(1f - r / 0.5f);
            cv.Px[y * n + x] = new Color(1f, 1f, 1f, a * a);
        }
        _glow = MakeSprite(cv, n, 1f);
        return _glow;
    }
}
