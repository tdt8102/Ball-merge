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
            try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { _font = null; }
            if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 32);
            return _font;
        }
    }
}
