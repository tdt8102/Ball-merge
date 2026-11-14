using UnityEngine;

/// <summary>An axis-aligned box in canvas units, origin top-left, y pointing down.</summary>
public struct HudBox
{
    public float L, T, R, B;
    public HudBox(float l, float t, float r, float b) { L = l; T = t; R = r; B = b; }
    public float Width => R - L;
    public float Height => B - T;

    /// <summary>True when the boxes share more than 0.5 units (touching edges are fine).</summary>
    public bool Overlaps(HudBox o) => L < o.R - 0.5f && o.L < R - 0.5f && T < o.B - 0.5f && o.T < B - 0.5f;

    public float OverlapArea(HudBox o) =>
        Mathf.Max(0f, Mathf.Min(R, o.R) - Mathf.Max(L, o.L)) * Mathf.Max(0f, Mathf.Min(B, o.B) - Mathf.Max(T, o.T));

    public override string ToString() => "[" + L.ToString("0") + "," + T.ToString("0") + " - " + R.ToString("0") + "," + B.ToString("0") + "]";
}

public enum SkillSlot
{
    UnderNext,      // in NEXT's column, just under it (the preferred place)
    RightMargin,    // under NEXT's row, in the free strip right of the board (wide screens)
    LeftOfNext,     // in the top bar, between the settings gear and NEXT (narrow screens, where the strip is too thin)
    BelowGear,      // left of NEXT but under the gear's row (tall screens whose canvas is too narrow for the top bar)
    NoRoom         // nothing fits: the best of the three is used (a layout check treats this as a failure)
}

/// <summary>
/// Where the HUD puts the skill button so it never covers NEXT, the settings gear, the score block or the area where the
/// cat is aimed and dropped. Pure arithmetic on the canvas size and the camera, so it can be checked for any aspect ratio.
/// The canvas is 1080x1920 scaled by CanvasScaler (match 0.5); the camera shows the playfield with GameManager.CameraHalfHeight.
/// </summary>
public static class HudLayout
{
    // sizes of the pieces (canvas units); GameUI builds them with these numbers
    public const float RingSize = 124f;          // the skill button with its ring
    public const float BadgeHeight = 30f;        // the pill under the ring (energy %, time left, CANCEL)
    public const float BadgeWidth = 150f;
    public const float BadgeGap = 3f;
    public const float GroupHeight = RingSize + BadgeGap + BadgeHeight;
    public const float NextFromRight = 190f;     // NEXT label and icon are centred this far from the right edge
    const float NextIconBottom = 204f;           // the biggest cat the dropper can offer (see Balance.dropBag), plus a little
    const float Gap = 8f;
    const float EdgeMargin = 12f;
    public const float BannerHeight = 64f;

    public struct Layout
    {
        public SkillSlot Slot;
        public HudBox Group;         // ring + badge
        public HudBox Next, Gear, Score, Board;   // what the group must stay clear of (Board = the playfield and the drop band above it)
        public HudBox Banner;        // the target hint line (with CANCEL), above the playfield
        public float PixelsPerUnit;
    }

    /// <param name="maxDropRadius">Radius of the biggest cat that can be held (the drop band is this tall around the drop height).</param>
    public static Layout Compute(float canvasW, float canvasH, float cameraHalfHeight, float maxDropRadius)
    {
        var l = new Layout();
        float ppu = canvasH / (2f * cameraHalfHeight);
        l.PixelsPerUnit = ppu;
        float W = canvasW;

        float frame = 0.22f;
        float boardL = W * 0.5f - (GameManager.HalfWidth + frame) * ppu, boardR = W * 0.5f + (GameManager.HalfWidth + frame) * ppu;
        float bandTop = (cameraHalfHeight - (GameManager.SpawnY + maxDropRadius)) * ppu;
        l.Board = new HudBox(boardL, bandTop, boardR, canvasH);
        l.Next = new HudBox(W - NextFromRight - 100f, 30f, W - NextFromRight + 100f, NextIconBottom);
        l.Gear = new HudBox(W * 0.5f - 60f, 25f, W * 0.5f + 60f, 145f);
        l.Score = new HudBox(0f, 0f, 340f, 285f);

        float underTop = NextIconBottom + Gap;
        var a = Box(W - NextFromRight - BadgeWidth * 0.5f, underTop);
        var b = Box(W - EdgeMargin - BadgeWidth, underTop);
        var c = Box(l.Next.L - 4f - BadgeWidth, 22f);
        var d = Box(l.Next.L - 4f - BadgeWidth, l.Gear.B + 4f);

        if (Fits(a, l)) { l.Slot = SkillSlot.UnderNext; l.Group = a; }
        else if (Fits(b, l)) { l.Slot = SkillSlot.RightMargin; l.Group = b; }
        else if (Fits(c, l)) { l.Slot = SkillSlot.LeftOfNext; l.Group = c; }
        else if (Fits(d, l)) { l.Slot = SkillSlot.BelowGear; l.Group = d; }
        else
        {
            l.Slot = SkillSlot.NoRoom;
            l.Group = Least(l, a, b, c, d);
        }

        // the hint line sits just above the playfield (so it never hides a cat), but under the gear
        float boardTop = (cameraHalfHeight - (GameManager.WallTopY + frame * 0.5f)) * ppu;
        float wanted = boardTop - Gap - 6f;
        float bannerW = Mathf.Min(820f, W - 40f);
        if (wanted >= 150f + BannerHeight)
        {
            var b0 = new HudBox(W * 0.5f - bannerW * 0.5f, wanted - BannerHeight, W * 0.5f + bannerW * 0.5f, wanted);
            // on squarer screens the line is high enough to meet the coin chip (left) or the skill button (right): keep clear of both
            if (b0.Overlaps(l.Score)) b0.L = Mathf.Max(b0.L, l.Score.R + 8f);
            if (b0.Overlaps(l.Group)) b0.R = Mathf.Min(b0.R, l.Group.L - 8f);
            l.Banner = b0;
        }
        else
        {
            // a short, wide screen (e.g. ultra-wide desktop): no room between the gear and the tray, so the line goes in the top row, left of the gear
            float left = l.Score.R + 10f, right = l.Gear.L - 10f;
            l.Banner = new HudBox(left, 16f, Mathf.Min(right, left + 820f), 16f + BannerHeight);
        }
        return l;
    }

    static HudBox Box(float left, float top) => new HudBox(left, top, left + BadgeWidth, top + GroupHeight);

    static bool Fits(HudBox g, Layout l) =>
        !g.Overlaps(l.Board) && !g.Overlaps(l.Next) && !g.Overlaps(l.Gear) && !g.Overlaps(l.Score);

    static HudBox Least(Layout l, params HudBox[] options)
    {
        var best = options[0];
        float bestArea = float.MaxValue;
        foreach (var o in options)
        {
            float area = o.OverlapArea(l.Board) + o.OverlapArea(l.Next) + o.OverlapArea(l.Gear) + o.OverlapArea(l.Score);
            if (area < bestArea) { bestArea = area; best = o; }
        }
        return best;
    }
}
