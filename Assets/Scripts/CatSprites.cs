using System;
using UnityEngine;

/// <summary>
/// Draws the cats at runtime (no image files). Each level is a different cat breed/coat:
/// round head (the physics body is a circle of diameter 1), pointy ears, nose, whiskers,
/// coat pattern and an accessory for the high levels. Eyes are separate sprites so they can blink.
/// </summary>
public static partial class CatSprites
{
    const int Size = 256;
    const float Span = 1.2f;      // world units covered by the texture; the round head has diameter 1

    static readonly Sprite[] Bodies = new Sprite[FruitDatabase.MaxLevel + 1];
    static readonly Sprite[] Eyes = new Sprite[FruitDatabase.MaxLevel + 1];
    static readonly Sprite[] Icons = new Sprite[FruitDatabase.MaxLevel + 1];

    enum Pattern { None, Tabby, Calico, Siamese, Tuxedo }
    enum Accessory { None, Bow, Crown, RoyalCrown }

    struct Cat
    {
        public Pattern Pat;
        public Accessory Acc;
        public Color Eye;
        public Cat(Pattern p, Accessory a, Color eye) { Pat = p; Acc = a; Eye = eye; }
    }

    // Coat colours live in FruitDatabase.Colors; this table has everything else.
    static readonly Cat[] Cats =
    {
        new Cat(Pattern.None,    Accessory.None,       Hex("59A6F2")), // 0 white, blue eyes
        new Cat(Pattern.Tabby,   Accessory.None,       Hex("73CC66")), // 1 gray tabby
        new Cat(Pattern.None,    Accessory.None,       Hex("FAD940")), // 2 black
        new Cat(Pattern.Tabby,   Accessory.None,       Hex("80CC4D")), // 3 orange tabby
        new Cat(Pattern.Calico,  Accessory.None,       Hex("F2B333")), // 4 calico
        new Cat(Pattern.Siamese, Accessory.None,       Hex("4D99F2")), // 5 siamese
        new Cat(Pattern.Tabby,   Accessory.None,       Hex("F2B333")), // 6 brown tabby
        new Cat(Pattern.Tuxedo,  Accessory.None,       Hex("B3E64D")), // 7 tuxedo
        new Cat(Pattern.None,    Accessory.Bow,        Hex("73CC66")), // 8 blue-gray with bow
        new Cat(Pattern.None,    Accessory.Crown,      Hex("4D99F2")), // 9 golden with crown
        new Cat(Pattern.None,    Accessory.RoyalCrown, Hex("FF8AD0")), // 10 royal purple
    };

    public static Sprite Get(int level)
    {
        level = Mathf.Clamp(level, 0, FruitDatabase.MaxLevel);
        if (Bodies[level] == null) Bodies[level] = MakeSprite(DrawBody(level), Size, Span);
        return Bodies[level];
    }

    /// <summary>Head with the eyes baked in, for UI (menu, next preview, chain). In the game the eyes are separate so they can blink.</summary>
    public static Sprite Icon(int level)
    {
        level = Mathf.Clamp(level, 0, FruitDatabase.MaxLevel);
        if (Icons[level] == null)
        {
            var cv = DrawBody(level);
            var eye = DrawEye(level);
            foreach (float sx in new[] { -0.2f, 0.2f }) cv.Overlay(eye, sx, 0.04f, 0.19f);
            Icons[level] = MakeSprite(cv, Size, Span);
        }
        return Icons[level];
    }

    static Sprite _gear;

    /// <summary>White gear icon for the settings button (the hole matches the button colour).</summary>
    public static Sprite Gear()
    {
        if (_gear != null) return _gear;
        var cv = new Canvas(128, 1f, Color.white);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            cv.Paint(Cap(0, 0, Mathf.Cos(a) * 0.41f, Mathf.Sin(a) * 0.41f, 0.08f), Color.white);   // teeth
        }
        cv.Paint(Circle(0, 0, 0.34f), Color.white);
        cv.Paint(Circle(0, 0, 0.15f), new Color(0.55f, 0.36f, 0.24f));                             // hole = button colour
        _gear = MakeSprite(cv, 128, 1f);
        return _gear;
    }

    public static Sprite Eye(int level)
    {
        level = Mathf.Clamp(level, 0, FruitDatabase.MaxLevel);
        if (Eyes[level] == null) Eyes[level] = MakeSprite(DrawEye(level), 64, 1f);
        return Eyes[level];
    }

    // ------------------------------------------------------------------ drawing

    static Canvas DrawBody(int level)
    {
        var cat = Cats[level];
        Color coat = FruitDatabase.Colors[level];
        Color line = Color.Lerp(coat, new Color(0.15f, 0.1f, 0.12f), 0.6f);
        Color dark = Color.Lerp(coat, Color.black, 0.3f);
        Color pink = new Color(1f, 0.72f, 0.8f);
        var cv = new Canvas(Size, Span, line);

        var body = Circle(0, 0, 0.5f);
        var earL = new[] { new Vector2(-0.47f, 0.12f), new Vector2(-0.12f, 0.47f), new Vector2(-0.38f, 0.58f) };
        var earR = new[] { new Vector2(0.47f, 0.12f), new Vector2(0.12f, 0.47f), new Vector2(0.38f, 0.58f) };

        Color earColL = coat, earColR = coat;
        if (cat.Pat == Pattern.Calico) { earColL = Hex("F2A154"); earColR = Hex("3B3B46"); }
        if (cat.Pat == Pattern.Siamese) earColL = earColR = Hex("6B4A3A");

        // outline, ears, inner ears, head
        cv.Paint(Grow(Union(body, Tri(earL), Tri(earR)), 0.016f), line);
        cv.Paint(Tri(earL), earColL);
        cv.Paint(Tri(earR), earColR);
        cv.Paint(Tri(Shrink(earL, 0.5f)), pink);
        cv.Paint(Tri(Shrink(earR, 0.5f)), pink);
        cv.Paint(body, coat);

        // coat pattern (clipped to the head)
        switch (cat.Pat)
        {
            case Pattern.Tabby:
                foreach (var s in new[] { Cap(0, 0.5f, 0, 0.33f, 0.02f), Cap(-0.11f, 0.49f, -0.08f, 0.36f, 0.018f), Cap(0.11f, 0.49f, 0.08f, 0.36f, 0.018f),
                                          Cap(-0.5f, 0.02f, -0.36f, 0.04f, 0.017f), Cap(-0.5f, -0.06f, -0.37f, -0.04f, 0.017f),
                                          Cap(0.5f, 0.02f, 0.36f, 0.04f, 0.017f), Cap(0.5f, -0.06f, 0.37f, -0.04f, 0.017f) })
                    cv.Paint(s, dark, body);
                break;
            case Pattern.Calico:
                cv.Paint(Circle(-0.3f, 0.3f, 0.22f), Hex("F2A154"), body);
                cv.Paint(Circle(0.32f, 0.22f, 0.2f), Hex("3B3B46"), body);
                cv.Paint(Circle(0.22f, -0.38f, 0.14f), Hex("F2A154"), body);
                break;
            case Pattern.Siamese:
                cv.Paint(Ellipse(0, -0.08f, 0.27f, 0.22f), new Color(0.42f, 0.29f, 0.23f, 0.9f), body);
                break;
            case Pattern.Tuxedo:
                cv.Paint(Ellipse(0, -0.22f, 0.31f, 0.2f), Color.white, body);
                cv.Paint(Cap(0, 0.5f, 0, 0.2f, 0.05f), Color.white, body);
                break;
        }

        // face: cheeks, nose, mouth, whiskers
        cv.Paint(Ellipse(-0.3f, -0.07f, 0.07f, 0.045f), new Color(1f, 0.55f, 0.65f, 0.4f), body);
        cv.Paint(Ellipse(0.3f, -0.07f, 0.07f, 0.045f), new Color(1f, 0.55f, 0.65f, 0.4f), body);
        cv.Paint(Tri(new Vector2(-0.04f, -0.05f), new Vector2(0.04f, -0.05f), new Vector2(0, -0.095f)), new Color(1f, 0.5f, 0.6f));
        Color mouth = new Color(0.35f, 0.2f, 0.25f);
        cv.Paint(Cap(0, -0.095f, 0, -0.13f, 0.008f), mouth);
        cv.Paint(Cap(0, -0.13f, -0.055f, -0.16f, 0.008f), mouth);
        cv.Paint(Cap(0, -0.13f, 0.055f, -0.16f, 0.008f), mouth);

        float lum = coat.r * 0.3f + coat.g * 0.59f + coat.b * 0.11f;
        Color whisk = lum < 0.4f ? new Color(0.92f, 0.92f, 0.95f, 0.9f) : new Color(0.3f, 0.26f, 0.3f, 0.85f);
        foreach (float sx in new[] { -1f, 1f })
        {
            cv.Paint(Cap(sx * 0.24f, -0.08f, sx * 0.47f, -0.03f, 0.006f), whisk, body);
            cv.Paint(Cap(sx * 0.24f, -0.11f, sx * 0.48f, -0.11f, 0.006f), whisk, body);
            cv.Paint(Cap(sx * 0.24f, -0.14f, sx * 0.47f, -0.19f, 0.006f), whisk, body);
        }

        // accessories (drawn outside the head too, so no clip)
        if (cat.Acc == Accessory.Bow)
        {
            Color red = Hex("E8455A");
            Outlined(cv, Tri(new Vector2(0, -0.49f), new Vector2(-0.19f, -0.4f), new Vector2(-0.19f, -0.58f)), red, line);
            Outlined(cv, Tri(new Vector2(0, -0.49f), new Vector2(0.19f, -0.4f), new Vector2(0.19f, -0.58f)), red, line);
            Outlined(cv, Circle(0, -0.49f, 0.055f), Hex("C42E44"), line);
        }
        else if (cat.Acc == Accessory.Crown || cat.Acc == Accessory.RoyalCrown)
        {
            bool royal = cat.Acc == Accessory.RoyalCrown;
            float hw = royal ? 0.2f : 0.16f, top = royal ? 0.595f : 0.57f;
            Color gold = Hex("FFC93C"), edge = Hex("A8731A");
            var crown = Union(Rect(0, 0.455f, hw, 0.035f),
                              Tri(new Vector2(-hw, 0.485f), new Vector2(-hw + 0.1f, 0.485f), new Vector2(-hw + 0.02f, top - 0.02f)),
                              Tri(new Vector2(-0.06f, 0.485f), new Vector2(0.06f, 0.485f), new Vector2(0, top)),
                              Tri(new Vector2(hw, 0.485f), new Vector2(hw - 0.1f, 0.485f), new Vector2(hw - 0.02f, top - 0.02f)));
            Outlined(cv, crown, gold, edge);
            cv.Paint(Circle(0, 0.455f, 0.02f), royal ? Hex("E8455A") : Hex("4D99F2"));
            if (royal)
            {
                cv.Paint(Circle(-0.11f, 0.455f, 0.014f), Hex("4D99F2"));
                cv.Paint(Circle(0.11f, 0.455f, 0.014f), Hex("4D99F2"));
            }
        }
        return cv;
    }

    static Canvas DrawEye(int level)
    {
        Color iris = Cats[level].Eye;
        var cv = new Canvas(64, 1f, new Color(0.1f, 0.08f, 0.12f));
        cv.Paint(Circle(0, 0, 0.5f), new Color(0.1f, 0.08f, 0.12f));
        cv.Paint(Circle(0, 0, 0.43f), iris);
        cv.Paint(Ellipse(0, -0.02f, 0.13f, 0.3f), new Color(0.05f, 0.04f, 0.07f));
        cv.Paint(Circle(-0.14f, 0.17f, 0.12f), Color.white);
        cv.Paint(Circle(0.12f, -0.15f, 0.05f), new Color(1, 1, 1, 0.8f));
        return cv;
    }

    static void Outlined(Canvas cv, Shape s, Color fill, Color line)
    {
        cv.Paint(Grow(s, 0.012f), line);
        cv.Paint(s, fill);
    }

    static Vector2[] Shrink(Vector2[] t, float f)
    {
        var c = (t[0] + t[1] + t[2]) / 3f + new Vector2(Mathf.Sign(t[2].x) * 0.015f, 0.015f);   // nudge outward/up
        return new[] { c + (t[0] - c) * f, c + (t[1] - c) * f, c + (t[2] - c) * f };
    }

    // ------------------------------------------------------------------ tiny raster toolkit

    struct Shape
    {
        public Func<float, float, float> D;     // signed distance in world units (negative = inside)
        public float X0, Y0, X1, Y1;            // bounding box
    }

    static Shape Circle(float cx, float cy, float r) => new Shape
    {
        D = (u, v) => Mathf.Sqrt((u - cx) * (u - cx) + (v - cy) * (v - cy)) - r,
        X0 = cx - r, Y0 = cy - r, X1 = cx + r, Y1 = cy + r
    };

    static Shape Ellipse(float cx, float cy, float rx, float ry) => new Shape
    {
        D = (u, v) =>
        {
            float qx = (u - cx) / rx, qy = (v - cy) / ry;
            return (Mathf.Sqrt(qx * qx + qy * qy) - 1f) * Mathf.Min(rx, ry);
        },
        X0 = cx - rx, Y0 = cy - ry, X1 = cx + rx, Y1 = cy + ry
    };

    static Shape Rect(float cx, float cy, float hx, float hy) => new Shape
    {
        D = (u, v) =>
        {
            float dx = Mathf.Abs(u - cx) - hx, dy = Mathf.Abs(v - cy) - hy;
            return Mathf.Sqrt(Mathf.Max(dx, 0) * Mathf.Max(dx, 0) + Mathf.Max(dy, 0) * Mathf.Max(dy, 0)) + Mathf.Min(Mathf.Max(dx, dy), 0);
        },
        X0 = cx - hx, Y0 = cy - hy, X1 = cx + hx, Y1 = cy + hy
    };

    static Shape Cap(float ax, float ay, float bx, float by, float r) => new Shape
    {
        D = (u, v) =>
        {
            float pax = u - ax, pay = v - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        },
        X0 = Mathf.Min(ax, bx) - r, Y0 = Mathf.Min(ay, by) - r, X1 = Mathf.Max(ax, bx) + r, Y1 = Mathf.Max(ay, by) + r
    };

    static Shape Tri(Vector2[] p) => Tri(p[0], p[1], p[2]);

    static Shape Tri(Vector2 a, Vector2 b, Vector2 c) => new Shape
    {
        D = (u, v) =>
        {
            Vector2 pt = new Vector2(u, v);
            Vector2 e0 = b - a, e1 = c - b, e2 = a - c, v0 = pt - a, v1 = pt - b, v2 = pt - c;
            Vector2 q0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            Vector2 q1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            Vector2 q2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
            float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            float d0 = Vector2.Dot(q0, q0), d1 = Vector2.Dot(q1, q1), d2 = Vector2.Dot(q2, q2);
            float s0 = s * (v0.x * e0.y - v0.y * e0.x), s1 = s * (v1.x * e1.y - v1.y * e1.x), s2 = s * (v2.x * e2.y - v2.y * e2.x);
            return -Mathf.Sqrt(Mathf.Min(d0, Mathf.Min(d1, d2))) * Mathf.Sign(Mathf.Min(s0, Mathf.Min(s1, s2)));
        },
        X0 = Mathf.Min(a.x, Mathf.Min(b.x, c.x)), Y0 = Mathf.Min(a.y, Mathf.Min(b.y, c.y)),
        X1 = Mathf.Max(a.x, Mathf.Max(b.x, c.x)), Y1 = Mathf.Max(a.y, Mathf.Max(b.y, c.y))
    };

    static Shape Union(params Shape[] s)
    {
        var r = new Shape { D = (u, v) => { float m = float.MaxValue; foreach (var x in s) m = Mathf.Min(m, x.D(u, v)); return m; },
                            X0 = float.MaxValue, Y0 = float.MaxValue, X1 = float.MinValue, Y1 = float.MinValue };
        foreach (var x in s) { r.X0 = Mathf.Min(r.X0, x.X0); r.Y0 = Mathf.Min(r.Y0, x.Y0); r.X1 = Mathf.Max(r.X1, x.X1); r.Y1 = Mathf.Max(r.Y1, x.Y1); }
        return r;
    }

    static Shape Grow(Shape s, float g)
    {
        var d = s.D;
        return new Shape { D = (u, v) => d(u, v) - g, X0 = s.X0 - g, Y0 = s.Y0 - g, X1 = s.X1 + g, Y1 = s.Y1 + g };
    }

    /// <summary>Square pixel canvas covering [-span/2, span/2] in both axes, y up.</summary>
    class Canvas
    {
        public readonly int N;
        public readonly float Span;
        public readonly Color[] Px;

        public Canvas(int n, float span, Color transparentRgb)
        {
            N = n; Span = span; Px = new Color[n * n];
            transparentRgb.a = 0f;                      // keeps edge pixels free of dark/light halos when filtered
            for (int i = 0; i < Px.Length; i++) Px[i] = transparentRgb;
        }

        public void Paint(Shape s, Color c) => Paint(s, c, default, false);
        public void Paint(Shape s, Color c, Shape clip) => Paint(s, c, clip, true);

        void Paint(Shape s, Color c, Shape clip, bool hasClip)
        {
            float k = N / Span, half = Span * 0.5f;
            int x0 = Mathf.Max(0, (int)((s.X0 + half) * k) - 1), x1 = Mathf.Min(N - 1, (int)((s.X1 + half) * k) + 1);
            int y0 = Mathf.Max(0, (int)((s.Y0 + half) * k) - 1), y1 = Mathf.Min(N - 1, (int)((s.Y1 + half) * k) + 1);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float u = (x + 0.5f) / k - half, v = (y + 0.5f) / k - half;
                float a = Mathf.Clamp01(0.5f - s.D(u, v) * k);          // ~1px anti-aliased edge
                if (a <= 0f) continue;
                if (hasClip) { a *= Mathf.Clamp01(0.5f - clip.D(u, v) * k); if (a <= 0f) continue; }
                a *= c.a;
                Blend(ref Px[y * N + x], c, a);
            }
        }

        /// <summary>Cut the shape out of what is already painted (makes it transparent).</summary>
        public void Erase(Shape s)
        {
            float k = N / Span, half = Span * 0.5f;
            int x0 = Mathf.Max(0, (int)((s.X0 + half) * k) - 1), x1 = Mathf.Min(N - 1, (int)((s.X1 + half) * k) + 1);
            int y0 = Mathf.Max(0, (int)((s.Y0 + half) * k) - 1), y1 = Mathf.Min(N - 1, (int)((s.Y1 + half) * k) + 1);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float u = (x + 0.5f) / k - half, v = (y + 0.5f) / k - half;
                float a = Mathf.Clamp01(0.5f - s.D(u, v) * k);
                if (a > 0f) Px[y * N + x].a *= 1f - a;
            }
        }

        static void Blend(ref Color d, Color c, float a)
        {
            float oa = a + d.a * (1f - a);
            if (oa <= 0f) return;
            d.r = (c.r * a + d.r * d.a * (1f - a)) / oa;
            d.g = (c.g * a + d.g * d.a * (1f - a)) / oa;
            d.b = (c.b * a + d.b * d.a * (1f - a)) / oa;
            d.a = oa;
        }

        /// <summary>Draw another canvas (covering 1 unit) centred at (cx, cy) with the given world size, bilinear sampled.</summary>
        public void Overlay(Canvas src, float cx, float cy, float size)
        {
            float k = N / Span, half = Span * 0.5f;
            int x0 = Mathf.Max(0, (int)((cx - size * 0.5f + half) * k) - 1), x1 = Mathf.Min(N - 1, (int)((cx + size * 0.5f + half) * k) + 1);
            int y0 = Mathf.Max(0, (int)((cy - size * 0.5f + half) * k) - 1), y1 = Mathf.Min(N - 1, (int)((cy + size * 0.5f + half) * k) + 1);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float lu = ((x + 0.5f) / k - half - cx) / size + 0.5f, lv = ((y + 0.5f) / k - half - cy) / size + 0.5f;
                if (lu < 0f || lu >= 1f || lv < 0f || lv >= 1f) continue;
                Color s = src.Sample(lu, lv);
                if (s.a > 0f) Blend(ref Px[y * N + x], s, s.a);
            }
        }

        Color Sample(float u, float v)
        {
            float fx = u * N - 0.5f, fy = v * N - 0.5f;
            int ix = Mathf.Clamp(Mathf.FloorToInt(fx), 0, N - 2), iy = Mathf.Clamp(Mathf.FloorToInt(fy), 0, N - 2);
            float tx = Mathf.Clamp01(fx - ix), ty = Mathf.Clamp01(fy - iy);
            Color a = Color.Lerp(Px[iy * N + ix], Px[iy * N + ix + 1], tx);
            Color b = Color.Lerp(Px[(iy + 1) * N + ix], Px[(iy + 1) * N + ix + 1], tx);
            return Color.Lerp(a, b, ty);
        }
    }

    static Sprite MakeSprite(Canvas cv, int size, float span)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels(cv.Px);
        tex.Apply(true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / span);
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
