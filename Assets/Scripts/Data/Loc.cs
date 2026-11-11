using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// English / Vietnamese texts. Loc.T("key") gives the current language; Loc.Bind(text, () => ...) keeps a Text
/// up to date and re-evaluates it when the language changes. The choice is saved in PlayerData ("" = follow the device).
/// </summary>
public static class Loc
{
    public static event Action Changed;

    static string _lang;

    // key, English, Vietnamese
    static readonly string[][] Table =
    {
        new[] { "subtitle",   "Drop and merge the cats!",            "Thả và gộp mèo nào!" },
        new[] { "play",       "PLAY",                                "CHƠI" },
        new[] { "continue",   "CONTINUE",                            "TIẾP TỤC" },
        new[] { "new_game",   "NEW GAME",                            "VÁN MỚI" },
        new[] { "best",       "BEST",                                "KỶ LỤC" },
        new[] { "codex",      "CAT CODEX",                           "SỔ MÈO" },
        new[] { "tab_gacha",  "GACHA",                               "QUAY" },
        new[] { "tab_home",   "HOME",                                "TRANG CHỦ" },
        new[] { "tab_skin",   "SKIN",                                "TRANG PHỤC" },
        new[] { "tab_shop",   "SHOP",                                "CỬA HÀNG" },
        new[] { "soon",       "COMING SOON",                         "SẮP RA MẮT" },
        new[] { "soon_hint",  "We are working on it. Stay tuned!",   "Chúng mình đang làm. Hãy đón chờ nhé!" },
        new[] { "loading",    "Loading",                             "Đang tải" },
        new[] { "settings",   "SETTINGS",                            "CÀI ĐẶT" },
        new[] { "sound",      "Sound",                               "Âm thanh" },
        new[] { "haptic",     "Haptic",                              "Rung" },
        new[] { "language",   "Language",                            "Ngôn ngữ" },
        new[] { "restart",    "RESTART",                             "CHƠI LẠI" },
        new[] { "menu",       "MENU",                                "MENU" },
        new[] { "resume",     "RESUME",                              "TIẾP TỤC" },
        new[] { "next",       "NEXT",                                "TIẾP" },
        new[] { "game_over",  "GAME OVER",                           "KẾT THÚC" },
        new[] { "score",      "Score",                               "Điểm" },
        new[] { "coins",      "Coins",                               "Xu" },
        new[] { "play_again", "PLAY AGAIN",                          "CHƠI LẠI" },
        new[] { "new_cat",    "NEW CAT!",                            "MÈO MỚI!" },
        new[] { "discovered", "Discovered",                          "Đã tìm thấy" },
        new[] { "starter",    "Starter cat",                         "Mèo khởi đầu" },
        new[] { "seen",       "Seen",                                "Đã thấy" },
        new[] { "back",       "BACK",                                "QUAY LẠI" },
        new[] { "gacha_hint", "Win cat skins, backgrounds and frames!", "Quay trúng trang phục mèo, hình nền và khung!" },
        new[] { "pull",       "PULL",                                "QUAY" },
        new[] { "need_coins", "Not enough coins",                    "Không đủ xu" },
        new[] { "collection", "Collection",                          "Bộ sưu tập" },
        new[] { "rates",      "Rates",                               "Tỉ lệ" },
        new[] { "pity",       "Legendary guaranteed in {0} pulls",   "Chắc chắn Huyền thoại sau {0} lượt quay" },
        new[] { "ten_bonus",  "10 pulls: at least 1 Rare or better", "Quay 10: có ít nhất 1 món Hiếm trở lên" },
        new[] { "new_item",   "NEW!",                                "MỚI!" },
        new[] { "duplicate",  "Duplicate",                           "Trùng" },
        new[] { "equip",      "EQUIP",                               "TRANG BỊ" },
        new[] { "ok",         "OK",                                  "OK" },
        new[] { "drag_open",  "Tap to peek, drag the lid up to open!", "Chạm để hé, kéo nắp lên để mở!" },
        new[] { "skip",       "SKIP",                                "BỎ QUA" },
        new[] { "by_theme",   "SEE BY THEME",                        "XEM THEO BỘ" },
        new[] { "cat_slot",   "Cat #{0}: pick the theme it wears",   "Mèo số {0}: chọn bộ để mặc" },
        new[] { "rarity_common",    "Common",                        "Thường" },
        new[] { "rarity_rare",      "Rare",                          "Hiếm" },
        new[] { "rarity_legendary", "Legendary",                     "Huyền thoại" },
        new[] { "kind_cat",   "Cat skin",                            "Trang phục mèo" },
        new[] { "kind_bg",    "Background",                          "Hình nền" },
        new[] { "kind_frame", "Frame",                               "Khung" },
        new[] { "skin_cats",  "CATS",                                "MÈO" },
        new[] { "skin_bg",    "BACKGROUND",                          "NỀN" },
        new[] { "skin_frame", "FRAME",                               "KHUNG" },
        new[] { "all",        "ALL",                                 "TẤT CẢ" },
        new[] { "equipped",   "EQUIPPED",                            "ĐANG DÙNG" },
        new[] { "owned",      "OWNED",                               "ĐÃ CÓ" },
        new[] { "locked",     "Win it in the Gacha!",                "Quay trúng trong Gacha nhé!" },
        new[] { "equip_set",  "WEAR THIS WHOLE THEME",              "MẶC CẢ BỘ NÀY" },
        new[] { "collected",  "Collected",                           "Đã sưu tầm" },
        new[] { "cheat",      "CHEAT",                               "GIAN LẬN" },
        new[] { "enter_pass", "Enter the password",                  "Nhập mật khẩu" },
        new[] { "wrong_pass", "Wrong password",                      "Sai mật khẩu" },
        new[] { "coins_added","Coins added",                         "Đã thêm xu" },
        new[] { "combo0",     "Good!",                               "Tốt!" },
        new[] { "combo1",     "Great!",                              "Hay!" },
        new[] { "combo2",     "Awesome!",                            "Tuyệt!" },
        new[] { "combo3",     "Amazing!",                            "Siêu!" },
        new[] { "combo4",     "Incredible!",                         "Đỉnh!" },
    };

    static readonly string[] CatNamesVi =
    {
        "Tuyết", "Tro", "Bóng", "Gừng", "Tam Thể", "Xiêm", "Ca Cao", "Tuxedo", "Ngài Xanh", "Vàng Kim", "Bệ Hạ"
    };

    static Dictionary<string, string[]> _map;
    static readonly List<KeyValuePair<Text, Func<string>>> Bound = new List<KeyValuePair<Text, Func<string>>>();

    /// <summary>"en" or "vi".</summary>
    public static string Language
    {
        get
        {
            if (_lang == null)
            {
                string saved = PlayerData.Language;
                _lang = saved == "en" || saved == "vi" ? saved
                      : Application.systemLanguage == SystemLanguage.Vietnamese ? "vi" : "en";
            }
            return _lang;
        }
    }

    public static void SetLanguage(string lang)
    {
        if (lang != "en" && lang != "vi") return;
        if (lang == Language) return;
        _lang = lang;
        PlayerData.Language = lang;
        PlayerData.Flush();
        RefreshBound();
        Changed?.Invoke();
    }

    public static string T(string key)
    {
        if (_map == null)
        {
            _map = new Dictionary<string, string[]>();
            foreach (var row in Table) _map[row[0]] = new[] { row[1], row[2] };
        }
        return _map.TryGetValue(key, out var v) ? v[Language == "vi" ? 1 : 0] : key;
    }

    public static string CatName(int level)
    {
        level = Mathf.Clamp(level, 0, CatDatabase.MaxLevel);
        return Language == "vi" ? CatNamesVi[level] : CatDatabase.Names[level];
    }

    // ---- keep Text objects in sync ----

    public static void Bind(Text t, Func<string> text)
    {
        if (t == null) return;
        Bound.Add(new KeyValuePair<Text, Func<string>>(t, text));
        t.text = text();
    }

    public static void Bind(Button b, Func<string> text) => Bind(b.GetComponentInChildren<Text>(), text);

    static void RefreshBound()
    {
        for (int i = Bound.Count - 1; i >= 0; i--)
        {
            var kv = Bound[i];
            if (kv.Key == null) { Bound.RemoveAt(i); continue; }       // its scene was unloaded
            kv.Key.text = kv.Value();
        }
    }
}
