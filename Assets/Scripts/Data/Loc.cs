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
        new[] { "cheat_reset","RESET PLAYER DATA",                   "XOÁ DỮ LIỆU CHƠI" },
        new[] { "cheat_sure", "TAP AGAIN TO CONFIRM",                 "BẤM LẠI ĐỂ XÁC NHẬN" },
        new[] { "enter_pass", "Enter the password",                  "Nhập mật khẩu" },
        new[] { "wrong_pass", "Wrong password",                      "Sai mật khẩu" },
        new[] { "coins_added","Coins added",                         "Đã thêm xu" },
        new[] { "skill_tap_cat", "Tap a cat to use the skill",       "Chạm một chú mèo để dùng kỹ năng" },
        new[] { "skill_cancel", "CANCEL",                            "HUỶ" },
        new[] { "skill_ready", "READY",                              "SẴN SÀNG" },
        new[] { "skill_need_energy", "NEED {0}%",                    "CẦN {0}%" },
        new[] { "skill_bad_target", "This cat can't be chosen",      "Không chọn được chú mèo này" },
        new[] { "skill_eraser_name", "Eraser",                       "Tẩy" },
        new[] { "skill_eraser_desc", "Tap any cat to erase it from the tank. No score or coins.", "Chạm một chú mèo bất kỳ để xóa khỏi bể. Không có điểm hay xu." },
        new[] { "skill_hug_name", "Hug",                             "Cái ôm" },
        new[] { "skill_hug_desc", "For {0}s, cats of the same level nearby are pulled towards each other.", "Trong {0} giây, các mèo cùng cấp ở gần nhau hút về phía nhau." },
        new[] { "skill_shrink_name", "Shrink",                       "Thu nhỏ" },
        new[] { "skill_shrink_desc", "Every cat shrinks to {0}% for {1}s, then grows back. More room!", "Mọi chú mèo thu nhỏ còn {0}% trong {1} giây rồi lớn lại. Thêm chỗ trống!" },
        new[] { "skill_level_up_name", "Level Up",                    "Lên cấp" },
        new[] { "skill_level_up_desc", "Tap a cat #{0}-#{1} to raise it one level.", "Chạm mèo số {0}-{1} để nâng nó lên một cấp." },
        new[] { "skill_moonlight_name", "Moonlight",                 "Ánh trăng" },
        new[] { "skill_moonlight_desc", "Pauses the red-line danger timer for {0}s. It is not reset.", "Tạm dừng bộ đếm nguy hiểm ở vạch đỏ trong {0} giây. Không bị đặt lại." },
        new[] { "skill_sugar_rush_name", "Sugar Rush",               "Cơn sốt đường" },
        new[] { "skill_sugar_rush_desc", "For {0}s, merge score x{1} and merge coins x{2}.", "Trong {0} giây, điểm gộp x{1} và xu gộp x{2}." },
        new[] { "skill_midas_name", "Midas",                         "Midas" },
        new[] { "skill_midas_desc", "Tap a cat #{0}-#{1} to turn it to gold and sell it for {2}-{3} coins.", "Chạm mèo số {0}-{1} để hóa vàng và bán được {2}-{3} xu." },
        new[] { "skill_cost", "Uses {0}% energy",                    "Tốn {0}% năng lượng" },
        new[] { "skill_state_locked", "LOCKED",                      "CHƯA MỞ" },
        new[] { "skill_state_unlocked", "UNLOCKED",                  "ĐÃ MỞ KHÓA" },
        new[] { "skill_state_active", "IN USE",                      "ĐANG DÙNG" },
        new[] { "skill_none", "This theme has no set skill.",        "Bộ này không có kỹ năng." },
        new[] { "skill_need_collect", "Collect all {1} cats of this theme to unlock it ({0}/{1}).", "Sưu tầm đủ {1} mèo của bộ này để mở khóa ({0}/{1})." },
        new[] { "skill_need_wear", "Unlocked! Wear all {1} cats of this theme to use it. Missing: {0}", "Đã mở khóa! Mặc đủ {1} mèo của bộ này để dùng. Còn thiếu: {0}" },
        new[] { "skill_in_use_hint", "Your next game has this skill.", "Ván tới của bạn có kỹ năng này." },
        new[] { "skill_outfit_ready", "Your outfit gives the skill: {0}", "Trang phục hiện tại có kỹ năng: {0}" },
        new[] { "skill_outfit_missing", "Every cat number needs a skin you own ({0}/{1} ready).", "Mỗi số mèo cần một trang phục bạn có ({0}/{1})." },
        new[] { "skill_outfit_mixed", "Cats wear different themes. Wear one theme on all 11 to use its skill.", "Các mèo đang mặc nhiều bộ khác nhau. Mặc một bộ cho cả 11 mèo để dùng kỹ năng." },
        new[] { "skill_outfit_classic", "No skill. Wear one whole theme to use its skill.", "Chưa có kỹ năng. Mặc trọn một bộ để dùng kỹ năng của bộ đó." },
        new[] { "skill_outfit_locked", "{0} is locked. Collect the whole theme first ({1}/{2}).", "{0} chưa mở khóa. Sưu tầm đủ bộ trước ({1}/{2})." },
        new[] { "skill_title", "SET SKILL",                          "KỸ NĂNG BỘ" },
        new[] { "skill_preview", "Preview",                          "Xem trước" },
        new[] { "skill_collected", "Collected",                      "Đã có" },
        new[] { "skill_wearing", "Wearing",                          "Đang mặc" },
        new[] { "skill_outfit", "Your outfit",                       "Trang phục" },
        new[] { "codex_sets", "{0} / {1} skins complete",            "Hoàn thành {0} / {1} bộ" },
        new[] { "set_progress", "{0}  {1} / {2}",                    "{0}  {1} / {2}" },
        new[] { "shard_gain", "+{0} star shard",                     "+{0} mảnh sao" },
        new[] { "shards", "Star shards",                             "Mảnh sao" },
        new[] { "set_complete", "SET COMPLETE!",                     "HOÀN THÀNH BỘ!" },
        new[] { "skill_unlocked", "Skill unlocked",                  "Đã mở khóa kỹ năng" },
        new[] { "exchange", "EXCHANGE",                              "ĐỔI MÈO" },
        new[] { "exchange_title", "STAR EXCHANGE",                   "ĐỔI MẢNH SAO" },
        new[] { "exchange_hint", "Spend star shards on any cat you don't own.", "Dùng mảnh sao để đổi bất kỳ chú mèo nào bạn chưa có." },
        new[] { "exchange_cost", "Cost",                             "Giá" },
        new[] { "exchange_have", "You have",                         "Bạn có" },
        new[] { "exchange_theme", "Theme",                           "Bộ" },
        new[] { "exchange_need", "Need {0} more shards",             "Cần thêm {0} mảnh sao" },
        new[] { "exchange_done", "Exchanged!",                       "Đã đổi!" },
        new[] { "exchange_fail", "Exchange failed, please try again", "Đổi không được, hãy thử lại" },
        new[] { "exchange_owned", "You already own this cat",        "Bạn đã có chú mèo này" },
        new[] { "tier_majesty", "Majesty",                           "Bệ Hạ" },
        new[] { "combo0",    "Good!",                               "Tốt!" },
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

    /// <summary>Forget the chosen language (after a data reset): the device language is used again.</summary>
    public static void ResetLanguage() => _lang = null;

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
