using System;
using System.Collections.Generic;
using UnityEngine;

public enum Rarity { Common, Rare, Legendary }
public enum ItemKind { Cat, Background, Frame }
public enum Motif { None, Dots, Stripes, Stars, Paws }
public enum EarShape { Pointy, Round }
public enum EyeShape { Normal, Dot, Happy, Sparkle }

/// <summary>Something the player can win in the Gacha and equip in the Skin tab.</summary>
public abstract class Item
{
    public string Id, En, Vi;
    public Rarity Rarity;
    public ItemKind Kind;
    public int Index;               // position in its own list
    public bool Default;            // owned from the start and not in the gacha
    public virtual string Name => Loc.Language == "vi" ? Vi : En;
    public virtual string Subtitle => "";      // e.g. the theme of a cat
}

/// <summary>
/// A cat theme: the same 11 cats drawn in a different style (shape of the ears and eyes, face, colours, outline, extras).
/// A theme is not collected as a whole: each of its 11 cats is a separate item (CatItem).
/// </summary>
public sealed class CatStyle
{
    public string Id, En, Vi;
    public int Index;
    public bool Default;
    public string Name => Loc.Language == "vi" ? Vi : En;
    public string[] NamesEn, NamesVi;       // the 11 cats of this theme have their own names (null = the classic names)

    public Func<Color, int, Color> Coat;    // (normal colour, level) -> this theme's colour; used for the coat and its patches (null = unchanged)
    public Func<int, Color> Eye;            // level -> eye colour (null = the normal eyes)
    public Color? LineColor;                // outline colour (null = a dark version of the coat)
    public float LineMix = 0.6f;            // ... mixed with dark ink by this much
    public float Outline = 0.016f;          // outline width (head diameter = 1)
    public EarShape Ears;
    public EyeShape EyeKind;
    public float EyeScale = 1f, EyeX = 0.2f, EyeY = 0.04f;
    public float Cheek = 0.4f;              // blush opacity
    public bool TinyFace;                   // small dot nose and a "w" mouth
    public bool Whiskers = true, Gloss, Sparkle, Fangs, Gem;
    public CatSprites.Pattern? Pat;         // null = each cat keeps its own coat pattern
    public int Pixels;                      // 0 = smooth; otherwise the head is drawn on a grid of this many pixels
}

/// <summary>One cat of one theme, e.g. "Ginger in the Pixel theme". Its rarity comes from the cat's level.</summary>
public sealed class CatItem : Item
{
    public CatStyle Theme;
    public int Level;
    public override string Name =>
        Theme.NamesEn == null ? Loc.CatName(Level) : (Loc.Language == "vi" ? Theme.NamesVi : Theme.NamesEn)[Level];
    public override string Subtitle => Theme.Name;
}

/// <summary>Playfield background: colour outside the tray, colour inside it, an optional repeating motif.</summary>
public sealed class BgStyle : Item
{
    public Color Outer, Panel, Ink;         // Ink = text colour for the HUD drawn on top
    public Motif Motif;
    public Color MotifColor;
}

/// <summary>Tray frame: one colour, or bands of several colours, plus an optional thin accent line along the middle.</summary>
public sealed class FrameStyle : Item
{
    public Color Color;
    public Color[] Bands;                   // alternating segments (null = solid)
    public Color Accent;                    // drawn only when Accent.a > 0
    public Color First => Bands != null && Bands.Length > 0 ? Bands[0] : Color;
}

public struct PullResult
{
    public Item Item;
    public bool IsNew;
    public int Refund;                      // coins given back for a duplicate
    public int Shards;                      // star shards given for a duplicate CAT (0 for a new item and for backgrounds/frames)
    public int SetOwned, SetTotal;          // a CAT: how many cats of its theme the player owns right after this result (0/0 for others)
    public ThemeSkillDef Completed;         // set only when this result completed the theme and newly unlocked its skill (announced once)
}

public enum ExchangeResult { Success, AlreadyOwned, NotEnoughShards, Failed }

/// <summary>The catalogue of collectibles, what is equipped, and the gacha roll.</summary>
public static class Cosmetics
{
    public const string DefaultBg = "bg_cream", DefaultFrame = "fr_wood";
    const int Levels = CatDatabase.MaxLevel + 1;

    public static event Action Changed;

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }

    static Color Hex(string hex, float alpha)
    {
        var c = Hex(hex);
        c.a = alpha;
        return c;
    }

    // ------------------------------------------------------------------ cat themes

    static readonly string[] CandyCoats = { "FF7BAC", "5EE6B8", "B58CFF", "FFC93C", "5CB8FF", "FF8FD8", "B8E65C", "FF9A5C", "3DD6D0", "E86BFF", "FFD84D" };
    static readonly string[] NeonEyes = { "5CF2FF", "FF6AE6", "B6FF5C" };

    public static readonly List<CatStyle> Cats = new List<CatStyle>
    {
        // the original look
        new CatStyle { Id = "cat_classic", En = "Classic", Vi = "Cổ điển", Default = true },

        // flat, vivid colours, thick black outline, plain black dot eyes
        new CatStyle { Id = "cat_doodle", NamesEn = Names("Scribble,Sketchy,Inky,Crayon,Doodlebug,Squiggle,Pencil,Blot,Marker,Comic,Masterpiece"), NamesVi = Names("Nguệch,Phác Thảo,Mực Tàu,Bút Sáp,Vẽ Bậy,Nét Cong,Bút Chì,Vệt Mực,Bút Lông,Truyện Tranh,Kiệt Tác"), En = "Doodle", Vi = "Nét vẽ", Coat = Vivid, LineColor = new Color(0.1f, 0.08f, 0.1f), Outline = 0.028f,
                       EyeKind = EyeShape.Dot, EyeScale = 1.1f, TinyFace = true, Whiskers = false, Cheek = 0f },

        // soft pastel colours, round bear ears, happy closed eyes
        new CatStyle { Id = "cat_kawaii", NamesEn = Names("Mochi,Boba,Pudding,Peach,Daifuku,Sakura,Taiyaki,Marshmallow,Cupcake,Princess,Angel"), NamesVi = Names("Mochi,Trân Châu,Bánh Flan,Đào,Daifuku,Anh Đào,Bánh Cá,Kẹo Xốp,Bánh Bông,Công Chúa,Thiên Thần"), En = "Kawaii", Vi = "Dễ thương", Coat = (c, l) => Color.Lerp(c, Hex("FFE9F0"), 0.55f), LineMix = 0.3f,
                       Ears = EarShape.Round, EyeKind = EyeShape.Happy, EyeScale = 1.15f, EyeY = 0.02f, TinyFace = true, Whiskers = false, Cheek = 0.75f },

        // huge sparkling eyes set low on the face
        new CatStyle { Id = "cat_chibi", NamesEn = Names("Pip,Bean,Nugget,Peanut,Button,Sprout,Pebble,Muffin,Biscuit,Mini King,Mini Queen"), NamesVi = Names("Bé Tí,Hạt Đậu,Viên Gà,Đậu Phộng,Cúc Áo,Mầm Nhỏ,Sỏi Nhỏ,Bánh Nướng,Bánh Quy,Vua Nhí,Nữ Hoàng Nhí"), En = "Chibi", Vi = "Chibi", EyeKind = EyeShape.Sparkle, EyeScale = 1.55f, EyeX = 0.21f, EyeY = -0.03f,
                       TinyFace = true, Whiskers = false, Cheek = 0.85f },

        // retro: a 40x40 grid of big square pixels and few colours
        new CatStyle { Id = "cat_pixel", NamesEn = Names("Bit,Byte,Sprite,Glitch,Joypad,Arcade,Level Up,Boss,Hi-Score,8-Bit King,Final Boss"), NamesVi = Names("Bit,Byte,Sprite,Lỗi Hình,Tay Cầm,Máy Game,Lên Cấp,Trùm,Điểm Cao,Vua 8-Bit,Trùm Cuối"), En = "Pixel", Vi = "Pixel", Coat = Retro, Pixels = 40, LineMix = 0.75f },

        // night cat: dark blue coat, neon eyes, little fangs and stars
        new CatStyle { Id = "cat_midnight", NamesEn = Names("Moonbeam,Dusk,Nebula,Comet,Eclipse,Twilight,Raven,Nocturne,Vesper,Lunar Duke,Night Queen"), NamesVi = Names("Ánh Trăng,Hoàng Hôn,Tinh Vân,Sao Chổi,Nhật Thực,Chạng Vạng,Quạ Đêm,Dạ Khúc,Sao Hôm,Nguyệt Công,Dạ Hoàng"), En = "Midnight", Vi = "Nửa đêm", Coat = Midnight, Eye = l => Hex(NeonEyes[l % 3]), LineMix = 0.75f,
                       Fangs = true, Sparkle = true, Cheek = 0.2f },

        // bold candy colours with white polka dots
        new CatStyle { Id = "cat_candy", NamesEn = Names("Gumdrop,Lollipop,Sprinkle,Caramel,Bonbon,Toffee,Jelly,Cotton Candy,Licorice,Sugar Prince,Candy Queen"), NamesVi = Names("Kẹo Dẻo,Kẹo Mút,Cốm Rắc,Caramel,Bonbon,Kẹo Bơ,Thạch,Kẹo Bông,Cam Thảo,Hoàng Tử Kẹo,Nữ Hoàng Kẹo"), En = "Candy", Vi = "Kẹo ngọt", Coat = (c, l) => Hex(CandyCoats[l]), Eye = l => Hex("5B3FD0"), Pat = CatSprites.Pattern.Dots,
                       Sparkle = true, TinyFace = true, Whiskers = false, Cheek = 0.6f, LineMix = 0.55f },

        // metallic gold with a shine, ruby eyes and a gem on the forehead
        new CatStyle { Id = "cat_gold", NamesEn = Names("Glimmer,Bullion,Ducat,Amber,Sunbeam,Topaz,Auric,Midas,Treasure,Golden Duke,Pharaoh"), NamesVi = Names("Lấp Lánh,Vàng Thỏi,Đồng Ducat,Hổ Phách,Tia Nắng,Hoàng Ngọc,Kim Quang,Midas,Kho Báu,Công Tước Vàng,Pharaoh"), En = "Golden", Vi = "Hoàng kim", Coat = (c, l) => Color.Lerp(Hex("FFF0A8"), Hex("E0A020"), l / 10f), Eye = l => Hex("E8334A"),
                       Gloss = true, Sparkle = true, Gem = true, LineMix = 0.7f },
    };

    static string[] Names(string csv) => csv.Split(',');

    static Color Vivid(Color c, int level)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        var m = Color.HSVToRGB(h, Mathf.Clamp01(s * 1.4f + (s > 0.05f ? 0.05f : 0f)), Mathf.Min(1f, v * 1.05f));
        m.a = c.a;
        return m;
    }

    static Color Retro(Color c, int level) => new Color(Mathf.Round(c.r * 4f) / 4f, Mathf.Round(c.g * 4f) / 4f, Mathf.Round(c.b * 4f) / 4f, c.a);

    static Color Midnight(Color c, int level)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        var m = Color.HSVToRGB(Mathf.Lerp(h, 0.68f, 0.7f), Mathf.Clamp01(s * 0.7f + 0.3f), v * 0.5f + 0.16f);
        m.a = c.a;
        return m;
    }

    // ------------------------------------------------------------------ backgrounds and frames

    public static readonly List<BgStyle> Backgrounds = new List<BgStyle>
    {
        new BgStyle { Id = DefaultBg, En = "Cream", Vi = "Kem", Rarity = Rarity.Common, Default = true,
                      Outer = new Color(0.98f, 0.93f, 0.84f), Panel = new Color(1f, 0.97f, 0.9f), Ink = new Color(0.3f, 0.2f, 0.15f) },
        new BgStyle { Id = "bg_sky", En = "Sky", Vi = "Bầu trời", Rarity = Rarity.Common,
                      Outer = Hex("D4EAF7"), Panel = Hex("F2F9FF"), Ink = Hex("2F4A66") },
        new BgStyle { Id = "bg_mint", En = "Mint", Vi = "Bạc hà", Rarity = Rarity.Common,
                      Outer = Hex("D5F0DF"), Panel = Hex("F2FFF7"), Ink = Hex("2D5A45") },
        new BgStyle { Id = "bg_blush", En = "Blush", Vi = "Hồng phấn", Rarity = Rarity.Common,
                      Outer = Hex("FADADF"), Panel = Hex("FFF2F5"), Ink = Hex("7A3B4B") },
        new BgStyle { Id = "bg_lavender", En = "Lavender Dots", Vi = "Tím chấm bi", Rarity = Rarity.Rare,
                      Outer = Hex("E4D9F6"), Panel = Hex("F6F1FF"), Ink = Hex("4B3A73"), Motif = Motif.Dots, MotifColor = Hex("9A72D9", 0.25f) },
        new BgStyle { Id = "bg_sunset", En = "Sunset Stripes", Vi = "Hoàng hôn", Rarity = Rarity.Rare,
                      Outer = Hex("F8C79F"), Panel = Hex("FFEBD9"), Ink = Hex("7A3F1E"), Motif = Motif.Stripes, MotifColor = Hex("F28C4D", 0.18f) },
        new BgStyle { Id = "bg_starry", En = "Starry Night", Vi = "Đêm sao", Rarity = Rarity.Legendary,
                      Outer = Hex("1F2347"), Panel = Hex("2D3260"), Ink = Hex("F5EFD8"), Motif = Motif.Stars, MotifColor = Hex("FFEB8C", 0.4f) },
        new BgStyle { Id = "bg_goldpaws", En = "Golden Paws", Vi = "Dấu chân vàng", Rarity = Rarity.Legendary,
                      Outer = Hex("F6D47A"), Panel = Hex("FFF1C9"), Ink = Hex("6B4510"), Motif = Motif.Paws, MotifColor = Hex("D99A1A", 0.3f) },
    };

    public static readonly List<FrameStyle> Frames = new List<FrameStyle>
    {
        new FrameStyle { Id = DefaultFrame, En = "Wood", Vi = "Gỗ", Rarity = Rarity.Common, Default = true, Color = new Color(0.55f, 0.36f, 0.24f) },
        new FrameStyle { Id = "fr_cloud", En = "Cloud", Vi = "Mây", Rarity = Rarity.Common, Color = Hex("9DB4D0") },
        new FrameStyle { Id = "fr_rose", En = "Rose", Vi = "Hồng", Rarity = Rarity.Common, Color = Hex("E58BA6") },
        new FrameStyle { Id = "fr_ocean", En = "Ocean", Vi = "Đại dương", Rarity = Rarity.Common, Color = Hex("4C8FC4") },
        new FrameStyle { Id = "fr_ink", En = "Ink and Gold", Vi = "Mực và vàng", Rarity = Rarity.Rare, Color = Hex("2E2B3A"), Accent = Hex("FFC93C") },
        new FrameStyle { Id = "fr_candy", En = "Candy Cane", Vi = "Kẹo gậy", Rarity = Rarity.Rare, Color = Hex("E8455A"), Bands = new[] { Hex("E8455A"), Hex("FFFFFF") } },
        new FrameStyle { Id = "fr_rainbow", En = "Rainbow", Vi = "Cầu vồng", Rarity = Rarity.Legendary, Color = Hex("E8455A"),
                         Bands = new[] { Hex("E8455A"), Hex("F7A541"), Hex("F4D35E"), Hex("6BC48A"), Hex("5AA9E6"), Hex("9B7BD4") } },
        new FrameStyle { Id = "fr_gold", En = "Royal Gold", Vi = "Vàng hoàng gia", Rarity = Rarity.Legendary, Color = Hex("E5A823"), Accent = Hex("FFF4B8") },
    };

    /// <summary>Every cat of every theme (theme 0 level 0..10, theme 1 level 0..10, ...).</summary>
    public static readonly List<CatItem> CatItems = new List<CatItem>();
    public static readonly List<Item> All = new List<Item>();
    static readonly Dictionary<string, Item> ById = new Dictionary<string, Item>();
    static readonly Dictionary<string, CatStyle> ThemeById = new Dictionary<string, CatStyle>();

    static Cosmetics()
    {
        for (int t = 0; t < Cats.Count; t++)
        {
            var theme = Cats[t];
            theme.Index = t;
            ThemeById[theme.Id] = theme;
            for (int l = 0; l < Levels; l++)
            {
                var item = new CatItem
                {
                    Theme = theme, Level = l, Kind = ItemKind.Cat, Default = theme.Default, Rarity = LevelRarity(l),
                    Id = theme.Id + "_" + l, En = CatDatabase.Names[l], Vi = CatDatabase.Names[l], Index = CatItems.Count
                };
                CatItems.Add(item);
                All.Add(item);
                ById[item.Id] = item;
            }
        }
        Register(Backgrounds, ItemKind.Background);
        Register(Frames, ItemKind.Frame);
    }

    /// <summary>The small cats are common, the big ones are rare, the two biggest are legendary.</summary>
    static Rarity LevelRarity(int level) => level >= 9 ? Rarity.Legendary : level >= 6 ? Rarity.Rare : Rarity.Common;

    static void Register<T>(List<T> list, ItemKind kind) where T : Item
    {
        for (int i = 0; i < list.Count; i++)
        {
            list[i].Kind = kind;
            list[i].Index = i;
            All.Add(list[i]);
            ById[list[i].Id] = list[i];
        }
    }

    public static Item Find(string id) => id != null && ById.TryGetValue(id, out var it) ? it : null;
    public static CatStyle FindTheme(string id) => id != null && ThemeById.TryGetValue(id, out var t) ? t : null;
    public static CatItem CatItemOf(CatStyle theme, int level) => CatItems[theme.Index * Levels + Mathf.Clamp(level, 0, Levels - 1)];
    public static string DefaultTheme => Cats[0].Id;

    /// <summary>Name of the cat of this level as it looks right now (the name belongs to the theme it wears).</summary>
    public static string WornName(int level) => CatItemOf(CatOf(level), level).Name;

    // ------------------------------------------------------------------ rarity and labels

    public static Color RarityColor(Rarity r) =>
        r == Rarity.Legendary ? new Color(1f, 0.7f, 0.1f) : r == Rarity.Rare ? new Color(0.3f, 0.55f, 0.95f) : new Color(0.58f, 0.64f, 0.7f);

    /// <summary>Colour of the light that leaks out of the gift box: white-blue, blue or gold.</summary>
    public static Color LightColor(Rarity r) =>
        r == Rarity.Legendary ? new Color(1f, 0.78f, 0.2f) : r == Rarity.Rare ? new Color(0.35f, 0.62f, 1f) : new Color(0.82f, 0.9f, 1f);

    public static string RarityKey(Rarity r) => r == Rarity.Legendary ? "rarity_legendary" : r == Rarity.Rare ? "rarity_rare" : "rarity_common";

    public static string KindKey(ItemKind k) => k == ItemKind.Cat ? "kind_cat" : k == ItemKind.Background ? "kind_bg" : "kind_frame";

    // ------------------------------------------------------------------ what is equipped (cached)

    static readonly int[] CatIdx = new int[Levels];          // theme index worn by each cat level
    static BgStyle _bg;
    static FrameStyle _frame;
    static bool _loaded;

    static void Load()
    {
        _loaded = true;
        for (int l = 0; l < Levels; l++)
        {
            var theme = FindTheme(PlayerData.CatSkin(l));
            CatIdx[l] = theme != null && PlayerData.IsOwned(CatItemOf(theme, l).Id) ? theme.Index : 0;
        }
        var bg = Find(PlayerData.Background) as BgStyle;
        _bg = bg != null && PlayerData.IsOwned(bg.Id) ? bg : Backgrounds[0];
        var fr = Find(PlayerData.Frame) as FrameStyle;
        _frame = fr != null && PlayerData.IsOwned(fr.Id) ? fr : Frames[0];
    }

    /// <summary>Read what is equipped again (after a data reset).</summary>
    public static void Reload() => _loaded = false;

    /// <summary>Index into Cats of the theme worn by this cat level.</summary>
    public static int CatIndex(int level)
    {
        if (!_loaded) Load();
        return CatIdx[Mathf.Clamp(level, 0, Levels - 1)];
    }

    public static CatStyle CatOf(int level) => Cats[CatIndex(level)];
    public static BgStyle Bg { get { if (!_loaded) Load(); return _bg; } }
    public static FrameStyle Frame { get { if (!_loaded) Load(); return _frame; } }

    public static bool IsOwned(Item item) => PlayerData.IsOwned(item.Id);

    public static bool IsEquipped(Item item)
    {
        if (!_loaded) Load();
        switch (item)
        {
            case CatItem c: return CatIdx[c.Level] == c.Theme.Index;
            case BgStyle b: return _bg == b;
            case FrameStyle f: return _frame == f;
        }
        return false;
    }

    /// <summary>A cat is worn in its own place (level); a background or frame replaces the current one.</summary>
    public static void Equip(Item item)
    {
        if (!IsOwned(item)) return;
        switch (item)
        {
            case CatItem c: PlayerData.SetCatSkin(c.Level, c.Theme.Id); break;
            case BgStyle b: PlayerData.Background = b.Id; break;
            case FrameStyle f: PlayerData.Frame = f.Id; break;
        }
        Commit();
    }

    /// <summary>Wear every cat of this theme that the player owns. Returns how many were changed on.</summary>
    public static int EquipTheme(CatStyle theme)
    {
        int n = 0;
        for (int l = 0; l < Levels; l++)
            if (PlayerData.IsOwned(CatItemOf(theme, l).Id)) { PlayerData.SetCatSkin(l, theme.Id); n++; }
        if (n > 0) Commit();
        return n;
    }

    static void Commit()
    {
        Load();
        PlayerData.Flush();
        Changed?.Invoke();
    }

    public static int OwnedInTheme(CatStyle theme)
    {
        int n = 0;
        for (int l = 0; l < Levels; l++) if (PlayerData.IsOwned(CatItemOf(theme, l).Id)) n++;
        return n;
    }

    /// <summary>How many of the collectible items (not the starter ones) the player owns, and how many exist.</summary>
    public static void Collection(out int owned, out int total)
    {
        owned = total = 0;
        foreach (var it in All)
        {
            if (it.Default) continue;
            total++;
            if (PlayerData.IsOwned(it.Id)) owned++;
        }
    }

    // ------------------------------------------------------------------ gacha

    public static int PullCost(int count) => Balance.Current.gachaCost * count;

    /// <summary>Coins given back for a duplicate: a part of what one pull costs, more for rarer items (balance.json: dupRefund...).</summary>
    public static int DuplicateCoins(Rarity r)
    {
        var b = Balance.Current;
        float part = r == Rarity.Legendary ? b.dupRefundLegendary : r == Rarity.Rare ? b.dupRefundRare : b.dupRefundCommon;
        return Mathf.Max(0, Mathf.RoundToInt(b.gachaCost * part));
    }

    /// <summary>Pulls left until a Legendary is guaranteed (0 = the pity rule is off).</summary>
    public static int PullsToPity => Balance.Current.gachaPity > 0 ? Mathf.Max(1, Balance.Current.gachaPity - PlayerData.Pity) : 0;

    /// <summary>
    /// Spends the coins and rolls <paramref name="count"/> items (1 or 10); each item is one cat of one theme, a background or a frame.
    /// Returns null when the player cannot afford it. New items are unlocked, duplicates give coins back. A 10-pull always
    /// contains at least one Rare or better, and a Legendary is guaranteed every gachaPity pulls.
    /// </summary>
    public static List<PullResult> Pull(int count)
    {
        var b = Balance.Current;
        if (count < 1 || !PlayerData.TrySpend(PullCost(count))) return null;
        var results = new List<PullResult>(count);
        bool gotRare = false;
        for (int i = 0; i < count; i++)
        {
            PlayerData.Pity++;
            var r = RollRarity(b);
            if (b.gachaPity > 0 && PlayerData.Pity >= b.gachaPity) r = Rarity.Legendary;
            if (count >= 10 && i == count - 1 && !gotRare && r == Rarity.Common) r = Rarity.Rare;
            if (r == Rarity.Legendary) PlayerData.Pity = 0;
            if (r != Rarity.Common) gotRare = true;

            results.Add(Award(RandomOf(r), r));
        }
        PlayerData.Flush();
        return results;
    }

    /// <summary>
    /// What one rolled item gives: a new item is unlocked; a duplicate gives its coin refund as before and, for a CAT only,
    /// star shards (Balance.shardRewardPerDuplicateCat). New items and duplicate backgrounds/frames give no shard.
    /// Public so the rule can be tested on exact items; Pull is the only caller in the game.
    /// </summary>
    public static PullResult Award(Item item, Rarity rolled)
    {
        var cat = item as CatItem;
        var skill = cat == null ? null : ThemeSkills.Of(cat.Theme);
        bool hadSkill = skill != null && SetCompletion.IsSkillUnlocked(skill);

        bool isNew = PlayerData.Unlock(item.Id);
        int refund = 0, shards = 0;
        if (!isNew)
        {
            refund = DuplicateCoins(rolled);
            PlayerData.AddCoins(refund);
            if (item is CatItem)
            {
                shards = ThemeSkills.ShardRewardPerDuplicateCat;
                PlayerData.AddShards(shards);
            }
        }

        var result = new PullResult { Item = item, IsNew = isNew, Refund = refund, Shards = shards };
        if (cat != null)
        {
            var progress = SetCompletion.Progress(cat.Theme);
            result.SetOwned = progress.Owned;
            result.SetTotal = progress.Total;
            // the unlock flag flips in PlayerData.Unlock (Phase 2); "newly" = it was not set before this result, so it is announced once
            if (skill != null && !hadSkill && SetCompletion.IsSkillUnlocked(skill)) result.Completed = skill;
        }
        return result;
    }

    // ------------------------------------------------------------------ star shard exchange

    static bool _exchanging;

    /// <summary>Shards needed for this cat: by rarity (Common 20, Rare 30, Legendary 40), the level-10 "Majesty" cat 50.</summary>
    public static int ShardPrice(CatItem cat) => ThemeSkills.ShardPrice(cat);

    /// <summary>
    /// Buy one cat the player does not own, of any theme and rarity, with star shards. Safe against a double tap or a retry:
    /// an owned cat is never charged, the shards are spent once, and if the cat could not be granted the shards are given
    /// back (a cat is never granted without payment, nor paid for without being granted). Ownership goes through
    /// PlayerData.Unlock, the same door as the gacha, so the set completion of Phase 2 is updated by the same call.
    /// There is no backend: everything is local and synchronous, so there is no half-finished request to retry.
    /// </summary>
    /// <param name="unlock">Test seam for the ownership step (default PlayerData.Unlock).</param>
    public static ExchangeResult Exchange(CatItem cat, Func<string, bool> unlock = null)
    {
        if (cat == null || _exchanging) return ExchangeResult.Failed;                // _exchanging: no re-entrant second call
        if (PlayerData.IsOwned(cat.Id)) return ExchangeResult.AlreadyOwned;
        int price = ShardPrice(cat);
        if (!PlayerData.TrySpendShards(price)) return ExchangeResult.NotEnoughShards;

        _exchanging = true;
        bool owned;
        try
        {
            try { (unlock ?? PlayerData.Unlock)(cat.Id); }
            catch (Exception e) { Debug.LogException(e); }                           // a failing listener must not leave a half state
            owned = PlayerData.IsOwned(cat.Id);
            if (!owned) PlayerData.AddShards(price);                                 // nothing was granted: refund
        }
        finally { _exchanging = false; }

        if (!owned) return ExchangeResult.Failed;
        Commit();                                                                    // saves, refreshes the equipped cache, raises Changed
        return ExchangeResult.Success;
    }

    /// <summary>Exchange, also telling the caller whether this exchange completed the theme and newly unlocked its skill (announced once).</summary>
    public static ExchangeResult Exchange(CatItem cat, out ThemeSkillDef completed)
    {
        var skill = cat == null ? null : ThemeSkills.Of(cat.Theme);
        bool hadSkill = skill != null && SetCompletion.IsSkillUnlocked(skill);
        var result = Exchange(cat);
        completed = result == ExchangeResult.Success && skill != null && !hadSkill && SetCompletion.IsSkillUnlocked(skill) ? skill : null;
        return result;
    }

    static Rarity RollRarity(Balance b)
    {
        float roll = UnityEngine.Random.value * 100f;
        if (roll < b.gachaRateLegendary) return Rarity.Legendary;
        if (roll < b.gachaRateLegendary + b.gachaRateRare) return Rarity.Rare;
        return Rarity.Common;
    }

    static Item RandomOf(Rarity r)
    {
        var pool = new List<Item>();
        foreach (var it in All) if (!it.Default && it.Rarity == r) pool.Add(it);
        return pool[UnityEngine.Random.Range(0, pool.Count)];
    }
}
