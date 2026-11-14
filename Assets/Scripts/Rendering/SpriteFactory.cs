using UnityEngine;

/// <summary>Generates all art at runtime so the prototype needs no imported assets.</summary>
public static class SpriteFactory
{
    static Sprite _circle, _square;
    static Font _font;

    /// <summary>Soft-edged circle, exactly 1 world unit in diameter.</summary>
    public static Sprite Circle
    {
        get
        {
            if (_circle != null) return _circle;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = size * 0.5f;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.Clamp01(r - d);                       // 1px anti-aliased edge
                float rim = Mathf.InverseLerp(r - 6f, r - 1f, d);     // darker rim
                float shade = 1f - 0.18f * Mathf.Clamp01(rim);
                byte c = (byte)(255 * shade);
                px[y * size + x] = new Color32(c, c, c, (byte)(255 * a));
            }
            tex.SetPixels32(px);
            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _circle;
        }
    }

    static Sprite _disc;

    /// <summary>Flat (no rim shading) anti-aliased disc, 1 world unit in diameter. Matches flat quads next to it.</summary>
    public static Sprite Disc
    {
        get
        {
            if (_disc != null) return _disc;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(r - d)));
            }
            tex.SetPixels32(px);
            tex.Apply();
            _disc = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _disc;
        }
    }

    static Sprite _ring;

    /// <summary>Thin white ring, 1 world unit across; used with Image.Type.Filled (radial) as a progress ring.</summary>
    public static Sprite Ring
    {
        get
        {
            if (_ring != null) return _ring;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float r = size * 0.5f, inner = r * 0.84f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.Min(Mathf.Clamp01(r - d), Mathf.Clamp01(d - inner));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * a));
            }
            tex.SetPixels32(px);
            tex.Apply();
            _ring = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _ring;
        }
    }

    static Sprite _rounded;

    /// <summary>
    /// White rounded rectangle for 9-slice use (Image.Type.Sliced / SpriteRenderer Sliced). The corner radius is
    /// 48 px = 0.48 world units at the default 100 ppu; on a uGUI Image set pixelsPerUnitMultiplier = 48 / wantedRadius.
    /// </summary>
    public static Sprite RoundedRect
    {
        get
        {
            if (_rounded != null) return _rounded;
            const int size = 128, r = 48;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - half) - (half - r);
                float qy = Mathf.Abs(y + 0.5f - half) - (half - r);
                float d = Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0)) + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(0.5f - d)));   // 1px anti-aliased edge
            }
            tex.SetPixels32(px);
            tex.Apply();
            _rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return _rounded;
        }
    }

    /// <summary>1x1 world-unit white square.</summary>
    public static Sprite Square
    {
        get
        {
            if (_square != null) return _square;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
            return _square;
        }
    }

    public static Font Font
    {
        get
        {
            if (_font != null) return _font;
            // Be Vietnam Pro (OFL, Assets/Resources/Fonts): has the Vietnamese letters the built-in font lacks
            _font = Resources.Load<Font>("Fonts/BeVietnamPro-Medium");
            if (_font != null) return _font;
            try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { _font = null; }
            if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 32);
            return _font;
        }
    }
}
