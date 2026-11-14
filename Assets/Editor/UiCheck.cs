using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Phase 14 (Skin / Codex / Gacha UI and the HUD). The views only show what the shared data layer says, so this checks:
/// the shared status (locked / unlocked / active, x/11, equipped x/11, the reason), the texts (EN + VI, every Loc key used),
/// the Codex progress that the Home button shows, the one-time "set complete" signal of a pull and an exchange, the HUD
/// placement for many aspect ratios, and the built screens themselves (Skin, Codex, Gacha card, Exchange, Set complete, HUD).
/// Menu: Tools/Suika/Check UI. Batch: -executeMethod UiCheck.Run (exit code 1 on failure).
/// The real save is backed up first and restored afterwards.
/// </summary>
public static class UiCheck
{
    const string SaveKey = "catcatcat.save", RunKey = "catcatcat.run";
    const int Levels = CatDatabase.MaxLevel + 1;

    static readonly List<GameObject> Made = new List<GameObject>();

    [MenuItem("Tools/Suika/Check UI")]
    public static void RunMenu()
    {
        if (Execute()) Debug.Log("[UI] all checks passed");
    }

    public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

    static bool Execute()
    {
        string saveBackup = PlayerPrefs.GetString(SaveKey, null), runBackup = PlayerPrefs.GetString(RunKey, null);
        bool ok = true;
        try
        {
            Status();
            Texts();
            Progress();
            Results();
            Layouts();
            Screens();
            Hud();
            Guards();
            Debug.Log("UI_CHECK_OK");
        }
        catch (Exception e)
        {
            ok = false;
            Debug.LogError("UI check failed: " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            DisposeAll();
            PlayerData.ResetAll();
            PlayerData.Blocked = false;
            if (saveBackup != null) PlayerPrefs.SetString(SaveKey, saveBackup);
            if (runBackup != null) PlayerPrefs.SetString(RunKey, runBackup);
            PlayerPrefs.Save();
            Reload();
            Loc.ResetLanguage();
        }
        return ok;
    }

    static CatStyle Theme(string id) => Cosmetics.FindTheme(id);
    static CatItem Cat(string themeId, int level) => Cosmetics.CatItemOf(Theme(themeId), level);

    static void OwnAll(string themeId, int except = -1)
    {
        for (int l = 0; l < Levels; l++) if (l != except) PlayerData.Unlock(Cat(themeId, l).Id);
    }

    // ------------------------------------------------------------------ shared status: locked / unlocked / active

    static void Status()
    {
        Fresh();
        var classic = Cosmetics.Cats[0];
        var doodle = Theme("cat_doodle");
        var kawaii = Theme("cat_kawaii");

        var s = ThemeSkillInfo.Of(classic);
        Check(s.State == SkillState.NoSkill && !s.HasSkill, "Classic has no skill");
        Check(SkillText.Reason(s).Length > 0, "Classic says why");

        s = ThemeSkillInfo.Of(doodle);
        Check(s.State == SkillState.Locked && s.Owned == 0 && s.Total == 11 && s.Worn == 0 && s.MissingMask == (1 << 11) - 1, "a fresh theme: locked, 0/11 collected, 0/11 worn");
        Check(s.Skill == ThemeSkills.Eraser, "the skill of the theme is the shared definition");

        for (int l = 0; l < 7; l++) PlayerData.Unlock(Cat("cat_doodle", l).Id);
        s = ThemeSkillInfo.Of(doodle);
        Check(s.State == SkillState.Locked && s.Owned == 7, "7/11 collected: still locked");
        Check(SkillText.Reason(s).Contains("7/11"), "the lock reason names the progress (7/11): " + SkillText.Reason(s));
        Check(Cosmetics.OwnedInTheme(doodle) == s.Owned, "the count is the shared ownership count");

        OwnAll("cat_doodle");
        s = ThemeSkillInfo.Of(doodle);
        Check(s.State == SkillState.Unlocked && s.Owned == 11 && s.Complete, "11/11 collected: unlocked (but not worn yet)");
        Check(PlayerData.IsSkillUnlocked(ThemeSkills.EraserId), "and it is the stored unlock that says so");

        for (int l = 0; l < 9; l++) Cosmetics.Equip(Cat("cat_doodle", l));
        s = ThemeSkillInfo.Of(doodle);
        Check(s.State == SkillState.Unlocked && s.Worn == 9, "9/11 worn: unlocked, not active");
        Check(SkillText.Slots(s.MissingMask) == "#10  #11", "the missing cat numbers are named: " + SkillText.Slots(s.MissingMask));
        Check(SkillText.Reason(s).Contains("#10") && SkillText.Reason(s).Contains("#11"), "the reason lists them: " + SkillText.Reason(s));
        Check(SkillResolver.ForNewRun() == null, "the resolver agrees: no skill yet");

        Cosmetics.EquipTheme(doodle);
        s = ThemeSkillInfo.Of(doodle);
        Check(s.State == SkillState.Active && s.Worn == 11 && s.MissingMask == 0, "11/11 worn: active");
        Check(SkillResolver.ForNewRun() == ThemeSkills.Eraser, "the resolver agrees: the next game has Eraser");
        Check(SkillText.Reason(s) == Loc.T("skill_in_use_hint"), "in use says so");

        // another unlocked theme is "unlocked" while Doodle is active; wearing one of its cats breaks the Doodle set
        OwnAll("cat_kawaii");
        Check(ThemeSkillInfo.Of(kawaii).State == SkillState.Unlocked && ThemeSkillInfo.Of(doodle).State == SkillState.Active, "two unlocked themes: only the worn one is active");
        Cosmetics.Equip(Cat("cat_kawaii", 3));
        var d = ThemeSkillInfo.Of(doodle);
        var k = ThemeSkillInfo.Of(kawaii);
        Check(d.State == SkillState.Unlocked && d.Worn == 10 && d.MissingMask == 1 << 3, "one cat of another theme: Doodle is no longer active (10/11, missing #4)");
        Check(k.State == SkillState.Unlocked && k.Worn == 1, "Kawaii wears 1/11");
        Check(SkillResolver.ForNewRun() == null, "mixed outfit: no skill");
        Check(SkillText.Outfit(SkillResolver.Resolve()) == Loc.T("skill_outfit_mixed"), "the outfit text says mixed");

        // outfit texts for every status
        Fresh();
        Check(SkillText.Outfit(SkillResolver.Resolve()) == Loc.T("skill_outfit_classic"), "Classic outfit text");
        PlayerData.SetCatSkin(3, "cat_pixel");                     // a cat the player does not own
        var miss = SkillResolver.Resolve();
        Check(miss.Status == SkillStatus.MissingSlots, "an unowned skin on a slot is missing");
        Check(SkillText.Outfit(miss).Contains("10/11"), "missing slots text names x/11: " + SkillText.Outfit(miss));
        var locked = new SkillReadiness { Status = SkillStatus.NotUnlocked, Skill = ThemeSkills.Eraser, Theme = doodle, TotalSlots = 11 };
        Check(SkillText.Outfit(locked).Contains(SkillText.Name(ThemeSkills.Eraser)), "locked outfit text names the skill");
        var ready = new SkillReadiness { Status = SkillStatus.Ready, Skill = ThemeSkills.Hug, Theme = kawaii, TotalSlots = 11, ValidSlots = 11 };
        Check(SkillText.Outfit(ready).Contains(SkillText.Name(ThemeSkills.Hug)), "ready outfit text names the skill");
    }

    // ------------------------------------------------------------------ texts

    static void Texts()
    {
        Fresh();
        foreach (var lang in new[] { "en", "vi" })
        {
            Loc.SetLanguage(lang);
            foreach (var skill in ThemeSkills.All)
            {
                string name = SkillText.Name(skill), desc = SkillText.Description(skill);
                Check(name.Length > 0 && name != skill.NameKey, lang + " name of " + skill.Id);
                Check(desc.Length > 0 && desc != skill.DescriptionKey, lang + " description of " + skill.Id);
                Check(!desc.Contains("{"), lang + " description has an unfilled placeholder: " + desc);
            }
            Check(SkillText.Cost().Contains("100"), lang + " cost line shows the energy cost");
            foreach (var st in new[] { SkillState.Locked, SkillState.Unlocked, SkillState.Active })
                Check(SkillText.State(st).Length > 0, lang + " state label " + st);
        }
        Loc.SetLanguage("en");
        Check(SkillText.Description(ThemeSkills.LevelUp).Contains("#1-#9"), "Level Up: cat numbers 1-9 (levels 0-8)");
        Check(SkillText.Description(ThemeSkills.Midas).Contains("#1-#8") && SkillText.Description(ThemeSkills.Midas).Contains("3-17"), "Midas: cats #1-#8, 3-17 coins: " + SkillText.Description(ThemeSkills.Midas));
        Check(SkillText.Description(ThemeSkills.Shrink).Contains("85%") && SkillText.Description(ThemeSkills.Shrink).Contains("6s"), "Shrink numbers");
        Check(SkillText.Description(ThemeSkills.SugarRush).Contains("x2") && SkillText.Description(ThemeSkills.SugarRush).Contains("x1.5"), "Sugar Rush numbers: " + SkillText.Description(ThemeSkills.SugarRush));
        Check(SkillText.Description(ThemeSkills.Hug).Contains("3s") && SkillText.Description(ThemeSkills.Moonlight).Contains("8s"), "Hug / Moonlight seconds");

        // the numbers come from Balance: tuning balance.json changes the text
        float old = Balance.Current.hugDuration;
        Balance.Current.hugDuration = 4.5f;
        Check(SkillText.Description(ThemeSkills.Hug).Contains("4.5s"), "the description follows Balance");
        Balance.Current.hugDuration = old;

        // every Loc key used anywhere in the scripts exists in both languages
        var keys = new SortedSet<string>();
        foreach (var file in Directory.GetFiles(Application.dataPath + "/Scripts", "*.cs", SearchOption.AllDirectories))
            if (!file.EndsWith("Loc.cs"))                                  // (its own comment mentions Loc.T("key"))
            foreach (Match m in Regex.Matches(File.ReadAllText(file), "Loc\\.T\\(\"([a-z0-9_]+)\"\\)")) keys.Add(m.Groups[1].Value);
        foreach (var s in ThemeSkills.All) { keys.Add(s.NameKey); keys.Add(s.DescriptionKey); }
        foreach (var lang in new[] { "en", "vi" })
        {
            Loc.SetLanguage(lang);
            foreach (var key in keys) Check(Loc.T(key) != key, "missing " + lang + " text for \"" + key + "\"");
        }
        Loc.SetLanguage("en");
        Debug.Log("UI texts: " + keys.Count + " keys checked in 2 languages");
    }

    // ------------------------------------------------------------------ Codex progress and the Home button

    static void Progress()
    {
        Fresh();
        var classic = Cosmetics.Cats[0];
        var doodle = Theme("cat_doodle");
        int before = CodexProgress.Have(classic);
        for (int l = 0; l < 4; l++) PlayerData.RecordSeen(l);
        int discovered = 0;
        for (int l = 0; l < Levels; l++) if (PlayerData.IsDiscovered(l)) discovered++;
        Check(CodexProgress.Have(classic) == discovered && discovered >= before, "Classic counts cats that appeared in a game");

        for (int l = 0; l < 5; l++) PlayerData.Unlock(Cat("cat_doodle", l).Id);
        Check(CodexProgress.Have(doodle) == 5 && CodexProgress.Have(doodle) == Cosmetics.OwnedInTheme(doodle), "another theme counts cats the player owns");
        Check(CodexProgress.CompleteThemes() == (CodexProgress.Complete(classic) ? 1 : 0), "no complete skin yet");

        OwnAll("cat_doodle");
        Check(CodexProgress.CompleteThemes() == (CodexProgress.Complete(classic) ? 1 : 0) + 1, "a fully owned theme is a complete skin");
        for (int l = 0; l < Levels; l++) PlayerData.RecordSeen(l);
        Check(CodexProgress.CompleteThemes() == 2, "and a fully discovered Classic page is another");

        var label = typeof(MenuController).GetMethod("CodexLabel", BindingFlags.NonPublic | BindingFlags.Static);
        string text = (string)label.Invoke(null, null);
        Check(text.Contains("2 / " + Cosmetics.Cats.Count), "the Home button shows complete skins: " + text.Replace("\n", " | "));
        Check(text.StartsWith(Loc.T("codex")), "and still says CAT CODEX");
        Check(!text.Contains(PlayerData.DiscoveredCount + " / " + Levels), "it is no longer the plain cat count");

        Loc.SetLanguage("vi");
        text = (string)label.Invoke(null, null);
        Check(text.Contains("2 / " + Cosmetics.Cats.Count) && text.Contains(Loc.T("codex_sets").Replace("{0}", "2").Replace("{1}", Cosmetics.Cats.Count.ToString())), "Vietnamese Home button");
        Loc.SetLanguage("en");
    }

    // ------------------------------------------------------------------ what a pull / exchange reports

    static void Results()
    {
        Fresh();
        var cat = Cat("cat_doodle", 0);
        var r = Cosmetics.Award(cat, Rarity.Common);
        Check(r.IsNew && r.SetOwned == 1 && r.SetTotal == 11 && r.Completed == null && r.Shards == 0, "new cat: set progress 1/11, no shard, no completion");
        r = Cosmetics.Award(cat, Rarity.Common);
        Check(!r.IsNew && r.Shards == 1 && r.Refund == Cosmetics.DuplicateCoins(Rarity.Common) && r.SetOwned == 1 && r.Completed == null, "duplicate cat: shard + the usual coin refund, still 1/11");
        r = Cosmetics.Award(Cosmetics.Backgrounds[1], Rarity.Common);
        Check(r.SetOwned == 0 && r.SetTotal == 0 && r.Completed == null && r.Shards == 0, "a background has no set progress");

        // the result that completes a set announces it - once
        Fresh();
        OwnAll("cat_pixel", except: 6);
        Check(!PlayerData.IsSkillUnlocked(ThemeSkills.LevelUpId), "10/11: not unlocked");
        r = Cosmetics.Award(Cat("cat_pixel", 6), Rarity.Rare);
        Check(r.IsNew && r.SetOwned == 11 && r.Completed == ThemeSkills.LevelUp, "the 11th cat completes the set and carries the skill");
        Check(PlayerData.IsSkillUnlocked(ThemeSkills.LevelUpId), "and it is stored");
        r = Cosmetics.Award(Cat("cat_pixel", 6), Rarity.Rare);
        Check(!r.IsNew && r.Completed == null && r.SetOwned == 11, "the same cat again does not announce it again");
        r = Cosmetics.Award(Cat("cat_pixel", 2), Rarity.Common);
        Check(r.Completed == null, "a duplicate of another cat of the set does not either");
        PlayerData.Flush();
        Reload();
        r = Cosmetics.Award(Cat("cat_pixel", 1), Rarity.Common);
        Check(r.Completed == null, "after a restart nothing is announced again");

        // by exchange
        Fresh();
        OwnAll("cat_doodle", except: 10);
        PlayerData.AddShards(100);
        var last = Cat("cat_doodle", 10);
        var res = Cosmetics.Exchange(last, out var completed);
        Check(res == ExchangeResult.Success && completed == ThemeSkills.Eraser && PlayerData.Shards == 50, "exchange completing the set returns the skill, 50 shards spent");
        res = Cosmetics.Exchange(last, out completed);
        Check(res == ExchangeResult.AlreadyOwned && completed == null && PlayerData.Shards == 50, "a second tap: nothing announced, nothing spent");
        PlayerData.AddShards(500);
        res = Cosmetics.Exchange(Cat("cat_kawaii", 0), out completed);
        Check(res == ExchangeResult.Success && completed == null, "a cat that does not finish a set returns no skill");
        res = Cosmetics.Exchange(null, out completed);
        Check(res == ExchangeResult.Failed && completed == null, "null cat");

        // many real pulls: every unlock is announced by exactly one result
        Fresh();
        PlayerData.AddCoins(1200000);
        var announced = new List<string>();
        for (int i = 0; i < 700; i++)
        {
            var results = Cosmetics.Pull(10);
            if (results == null) break;
            foreach (var x in results) if (x.Completed != null) announced.Add(x.Completed.Id);
        }
        int unlockedNow = 0;
        foreach (var skill in ThemeSkills.All) if (PlayerData.IsSkillUnlocked(skill.Id)) unlockedNow++;
        var distinct = new HashSet<string>(announced);
        Check(distinct.Count == announced.Count, "no skill was announced twice");
        Check(announced.Count == unlockedNow && unlockedNow > 0, "every unlock was announced once (" + unlockedNow + " sets completed in the pulls)");
        Debug.Log("UI results: " + announced.Count + " set completions announced once each in the real pulls");
    }

    // ------------------------------------------------------------------ HUD placement on many screens

    /// <summary>The canvas size CanvasScaler gives for a screen: 1080x1920 reference, match 0.5.</summary>
    static Vector2 CanvasFor(float w, float h)
    {
        float scale = Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(w / 1080f, 2f), Mathf.Log(h / 1920f, 2f), 0.5f));
        return new Vector2(w / scale, h / scale);
    }

    static readonly Vector2[] Screens_ =
    {
        new Vector2(1080, 2400), new Vector2(1080, 2340), new Vector2(1080, 2220), new Vector2(1080, 2160), new Vector2(1080, 2000), new Vector2(1080, 1920),
        new Vector2(1080, 1800), new Vector2(1200, 1920), new Vector2(1080, 1680), new Vector2(1080, 1620), new Vector2(1536, 2048), new Vector2(1080, 1350),
        new Vector2(1080, 1080), new Vector2(1440, 1080), new Vector2(1920, 1080), new Vector2(1280, 800), new Vector2(2560, 1080), new Vector2(1366, 1024),
        new Vector2(720, 1600), new Vector2(750, 1334), new Vector2(828, 1792), new Vector2(1284, 2778)
    };

    static void Layouts()
    {
        float maxDrop = 0f;
        foreach (int level in Balance.Current.dropBag) maxDrop = Mathf.Max(maxDrop, CatDatabase.Radius[level]);
        var report = new System.Text.StringBuilder();
        foreach (var screen in Screens_)
        {
            var cv = CanvasFor(screen.x, screen.y);
            var l = HudLayout.Compute(cv.x, cv.y, GameManager.CameraHalfHeight(cv.x / cv.y), maxDrop);
            string where = screen.x + "x" + screen.y + " (canvas " + cv.x.ToString("0") + "x" + cv.y.ToString("0") + ")";
            Check(l.Slot != SkillSlot.NoRoom, "the skill button has a place on " + where);
            Check(l.Group.L >= 0f && l.Group.T >= 0f && l.Group.R <= cv.x && l.Group.B <= cv.y, "button inside the screen on " + where + " " + l.Group);
            Check(!l.Group.Overlaps(l.Board), "button does not cover the drop area on " + where + " " + l.Group + " vs " + l.Board);
            Check(!l.Group.Overlaps(l.Next), "button does not cover NEXT on " + where);
            Check(!l.Group.Overlaps(l.Gear) && !l.Group.Overlaps(l.Score), "button does not cover the gear or the score on " + where);

            var play = new HudBox(l.Board.L, (GameManager.CameraHalfHeight(cv.x / cv.y) - GameManager.WallTopY) * l.PixelsPerUnit, l.Board.R, cv.y);   // the tray itself, below its top edge
            Check(!l.Banner.Overlaps(play), "the target line stays above the playfield on " + where + " " + l.Banner + " vs " + play);
            Check(!l.Banner.Overlaps(l.Gear) && !l.Banner.Overlaps(l.Group), "the target line clears the gear and the button on " + where);
            Check(!l.Banner.Overlaps(l.Score) && !l.Banner.Overlaps(l.Next), "the target line clears the score/coins and NEXT on " + where + " " + l.Banner);
            Check(l.Banner.Width >= 540f, "the target line is wide enough for the hint and CANCEL on " + where + " (" + l.Banner.Width.ToString("0") + ")");
            Check(l.Banner.L >= 0f && l.Banner.R <= cv.x && l.Banner.T >= 0f, "the target line is on the screen on " + where);
            report.Append(where).Append(" -> ").Append(l.Slot).Append('\n');
        }
        Debug.Log("UI layout slots:\n" + report);
    }

    // ------------------------------------------------------------------ the built screens

    static RectTransform NewCanvas(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
        go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(1080, 1920);
        Made.Add(go);
        return rt;
    }

    static T Add<T>(RectTransform canvas, string name) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(canvas, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go.AddComponent<T>();
    }

    static Text TextAt(Transform root, string path)
    {
        var t = root.Find(path);
        if (t == null) throw new Exception("no object at " + path);
        var text = t.GetComponent<Text>();
        if (text == null) throw new Exception("no Text at " + path);
        return text;
    }

    static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);
    static void SetField(object o, string name, object value) => o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(o, value);
    static object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(o, args);

    static void Audio()
    {
        if (AudioManager.Instance != null) return;
        var go = new GameObject("TestAudio");
        var am = go.AddComponent<AudioManager>();
        typeof(AudioManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(am, null);   // Awake does not run outside play mode
        Made.Add(go);
    }

    static void Screens()
    {
        Audio();
        Loc.SetLanguage("en");
        SkinScreen();
        CodexScreenCheck();
        CompleteScreenCheck();
        ExchangeCheck();
        GachaCheck();
    }

    static void SkinScreen()
    {
        Fresh();
        var canvas = NewCanvas("SkinCanvas");
        var skin = Add<SkinPage>(canvas, "PageSkin");
        skin.Build((RectTransform)skin.transform);
        int doodleIdx = Theme("cat_doodle").Index;
        SetField(skin, "_theme", doodleIdx);
        skin.Refresh();
        var root = skin.transform;

        Check(TextAt(root, "SkillCardBorder/SkillCard/Name").text == "Eraser", "skin: the theme's skill name");
        Check(TextAt(root, "SkillCardBorder/SkillCard/State").text == "LOCKED", "skin: locked state");
        Check(TextAt(root, "SkillCardBorder/SkillCard/Desc").text.Contains("erase"), "skin: description");
        Check(TextAt(root, "ProgressCardBorder/ProgressCard/ValA").text == "0 / 11", "skin: collection 0/11");
        Check(TextAt(root, "ProgressCardBorder/ProgressCard/ValB").text == "0 / 11", "skin: equipped 0/11");
        Check(TextAt(root, "ProgressCardBorder/ProgressCard/Reason").text.Contains("0/11"), "skin: the reason names the missing collection");

        for (int l = 0; l < 7; l++) PlayerData.Unlock(Cat("cat_doodle", l).Id);
        skin.Refresh();
        Check(TextAt(root, "ProgressCardBorder/ProgressCard/ValA").text == "7 / 11" && TextAt(root, "SkillCardBorder/SkillCard/State").text == "LOCKED", "skin: 7/11 collected, still locked");

        OwnAll("cat_doodle");
        skin.Refresh();
        Check(TextAt(root, "SkillCardBorder/SkillCard/State").text == "UNLOCKED", "skin: unlocked once 11/11");
        Check(TextAt(root, "ProgressCardBorder/ProgressCard/ValB").text == "0 / 11", "skin: wearing 0/11");
        Check(TextAt(root, "ProgressCardBorder/ProgressCard/Reason").text.Contains("#1"), "skin: the reason names the missing cat numbers");

        // the WEAR THIS WHOLE THEME button runs the existing equip flow and the card follows
        var equip = root.Find("CatGroup/EquipSet").GetComponent<Button>();
        equip.onClick.Invoke();
        Check(Cosmetics.IsEquipped(Cat("cat_doodle", 4)) && SkillResolver.ForNewRun() == ThemeSkills.Eraser, "skin: the set button equips the whole theme (existing flow)");
        Check(TextAt(root, "SkillCardBorder/SkillCard/State").text == "IN USE", "skin: now in use");
        Check(TextAt(root, "ProgressCardBorder/ProgressCard/ValB").text == "11 / 11", "skin: wearing 11/11");
        Check(TextAt(root, "ProgressCardBorder/ProgressCard/Reason").text == Loc.T("skill_in_use_hint"), "skin: in-use text");

        // a different theme in the cat-number view: what the whole outfit gives
        SetField(skin, "_slot", 3);
        skin.Refresh();
        Check(TextAt(root, "ProgressCardBorder/ProgressCard/Reason").text == SkillText.Outfit(SkillResolver.Resolve()), "skin: cat-number view shows the outfit text");
        Check(TextAt(root, "SkillCardBorder/SkillCard/Name").text == "Eraser" && TextAt(root, "SkillCardBorder/SkillCard/State").text == "IN USE", "skin: the outfit's skill is shown");
        SetField(skin, "_slot", -1);

        // classic page: no skill
        SetField(skin, "_theme", 0);
        skin.Refresh();
        Check(TextAt(root, "SkillCardBorder/SkillCard/State").text == "" && TextAt(root, "ProgressCardBorder/ProgressCard/Reason").text == Loc.T("skill_none"), "skin: Classic has no skill");

        // Vietnamese follows the language change
        SetField(skin, "_theme", doodleIdx);
        skin.Refresh();
        Loc.SetLanguage("vi");
        Check(TextAt(root, "SkillCardBorder/SkillCard/Name").text == "Tẩy" && TextAt(root, "SkillCardBorder/SkillCard/State").text == "ĐANG DÙNG", "skin: Vietnamese text after a language change");
        Loc.SetLanguage("en");

        // the other tabs hide the skill cards
        SetField(skin, "_tab", Enum.ToObject(typeof(SkinPage).GetNestedType("Tab", BindingFlags.NonPublic), 1));
        skin.Refresh();
        Check(!root.Find("SkillCardBorder").gameObject.activeSelf && !root.Find("ProgressCardBorder").gameObject.activeSelf, "skin: the skill cards are only on the cats tab");
        Dispose(canvas.gameObject);
    }

    static void CodexScreenCheck()
    {
        Fresh();
        var canvas = NewCanvas("CodexCanvas");
        var codex = CodexScreen.Create(canvas);
        var root = codex.Root.transform;
        int doodleIdx = Theme("cat_doodle").Index;
        SetField(codex, "_page", doodleIdx);
        codex.Open();
        Check(codex.IsOpen, "codex opens");

        // an unfinished theme still previews its whole skill
        Check(TextAt(root, "SkillBorder/SkillCard/Name").text == "Eraser", "codex: skill name is previewed with 0/11");
        Check(TextAt(root, "SkillBorder/SkillCard/State").text == "LOCKED", "codex: locked state");
        Check(TextAt(root, "SkillBorder/SkillCard/Desc").text.Contains("erase") && TextAt(root, "SkillBorder/SkillCard/Desc").text.Contains("100%"), "codex: full description and energy cost");
        Check(TextAt(root, "SkillBorder/SkillCard/Progress").text.Contains("0 / 11"), "codex: collection progress");
        Check(TextAt(root, "SkillBorder/SkillCard/Reason").text.Contains("0/11"), "codex: why it is locked");
        Check(root.Find("SkillBorder/SkillCard/Icon").gameObject.activeSelf, "codex: the icon is shown");

        for (int l = 0; l < 6; l++) PlayerData.Unlock(Cat("cat_doodle", l).Id);
        Call(codex, "Refresh");
        Check(TextAt(root, "SkillBorder/SkillCard/Progress").text.Contains("6 / 11") && TextAt(root, "Progress").text.Contains("6 / 11"), "codex: 6/11 on the card and on the page");

        OwnAll("cat_doodle");
        Call(codex, "Refresh");
        Check(TextAt(root, "SkillBorder/SkillCard/State").text == "UNLOCKED", "codex: unlocked after 11/11");
        Check(TextAt(root, "SkillBorder/SkillCard/Reason").text.Contains("#1"), "codex: unlocked but not worn: what is missing");
        Cosmetics.EquipTheme(Theme("cat_doodle"));
        Call(codex, "Refresh");
        Check(TextAt(root, "SkillBorder/SkillCard/State").text == "IN USE", "codex: in use");

        // a theme with nothing: still previewed; Classic: no skill
        SetField(codex, "_page", Theme("cat_midnight").Index);
        Call(codex, "Refresh");
        Check(TextAt(root, "SkillBorder/SkillCard/Name").text == "Moonlight" && TextAt(root, "SkillBorder/SkillCard/State").text == "LOCKED", "codex: another theme previews its skill too");
        SetField(codex, "_page", 0);
        Call(codex, "Refresh");
        Check(!root.Find("SkillBorder/SkillCard/Icon").gameObject.activeSelf && TextAt(root, "SkillBorder/SkillCard/Reason").text == Loc.T("skill_none"), "codex: Classic card says there is no skill");

        // the preview is read-only: no run, no energy, no handlers
        Check(GameManager.Instance == null, "codex: there is no game to affect");
        Dispose(canvas.gameObject);
    }

    static void CompleteScreenCheck()
    {
        Fresh();
        var canvas = NewCanvas("CompleteCanvas");
        var screen = SetCompleteScreen.Create(canvas);
        Check(!screen.IsOpen, "the card starts hidden");
        screen.Show(ThemeSkills.Hug);
        Check(screen.IsOpen, "shown");
        var root = screen.transform;
        Check(TextAt(root, "Card/Title").text == "SET COMPLETE!", "title");
        Check(TextAt(root, "Card/Skill").text == "Hug" && TextAt(root, "Card/Theme").text == Theme("cat_kawaii").Name, "skill and theme names");
        Check(TextAt(root, "Card/Desc").text == SkillText.Description(ThemeSkills.Hug), "description");
        screen.Close();
        Check(!screen.IsOpen, "closed by OK");
        screen.Show(null);
        Check(!screen.IsOpen, "nothing to show for a null skill");
        Dispose(canvas.gameObject);
    }

    static void ExchangeCheck()
    {
        Fresh();
        var canvas = NewCanvas("ExchangeCanvas");
        var complete = SetCompleteScreen.Create(canvas);
        int changed = 0;
        var ex = ExchangeScreen.Create(canvas, complete, () => changed++);
        var root = ex.transform;
        var doodle = Theme("cat_doodle");

        ex.Open();
        Check(ex.IsOpen && TextAt(root, "Theme").text == doodle.Name, "exchange opens on the first theme with a missing cat (open=" + ex.IsOpen + ", shows \"" + TextAt(root, "Theme").text + "\", expected \"" + doodle.Name + "\")");
        Check(TextAt(root, "ShardChip/Amount").text == "0", "shard balance shown");
        Check(TextAt(root, "Detail/Name").text.Contains(Cat("cat_doodle", 0).Name), "the selected cat's name");
        Check(TextAt(root, "Detail/Theme").text.Contains(doodle.Name), "its theme");
        Check(TextAt(root, "Detail/Rarity").text.Contains(CosmeticUI.RarityName(Cat("cat_doodle", 0).Rarity)), "its rarity");
        Check(TextAt(root, "Detail/Cost").text == Cosmetics.ShardPrice(Cat("cat_doodle", 0)).ToString(), "its cost");
        var button = root.Find("Detail/Exchange").GetComponent<Button>();
        Check(!button.interactable, "no shards: the button is disabled");
        Check(TextAt(root, "Detail/Status").text.Contains("Need 20 more"), "and says how many shards are missing: " + TextAt(root, "Detail/Status").text);

        // prices on the cells, every rarity (Common 20, Rare 30, Legendary 40, Majesty 50)
        for (int l = 0; l < Levels; l++)
            Check(TextAt(root, "Cell" + l + "/Price").text == Cosmetics.ShardPrice(Cat("cat_doodle", l)).ToString(), "cell " + l + " price");
        var seen = new HashSet<string>();
        for (int l = 0; l < Levels; l++) seen.Add(TextAt(root, "Cell" + l + "/Price").text);
        Check(seen.Contains("20") && seen.Contains("30") && seen.Contains("40") && seen.Contains("50"), "all four price tiers appear on a page");

        // one shard short, then exactly enough
        PlayerData.AddShards(19);
        Call(ex, "Refresh");
        Check(!button.interactable && TextAt(root, "Detail/Status").text.Contains("Need 1 more"), "19/20: still disabled, 1 missing");
        PlayerData.AddShards(1);
        Call(ex, "Refresh");
        Check(button.interactable, "20/20: enabled");

        // owned cats are disabled and cost nothing
        PlayerData.Unlock(Cat("cat_doodle", 1).Id);
        root.Find("Cell1").GetComponent<Button>().onClick.Invoke();
        Check(!button.interactable && TextAt(root, "Detail/Status").text == Loc.T("exchange_owned") && TextAt(root, "Cell1/Price").text == "OWNED", "an owned cat: disabled, says owned");
        button.onClick.Invoke();                                      // even a forced tap changes nothing
        Check(PlayerData.Shards == 20, "a forced tap on an owned cat spends nothing");

        // exchange, then a double tap
        root.Find("Cell0").GetComponent<Button>().onClick.Invoke();
        Check(button.interactable, "cat #1 selected: enabled");
        for (int i = 0; i < 5; i++) button.onClick.Invoke();
        Check(Cosmetics.IsOwned(Cat("cat_doodle", 0)) && PlayerData.Shards == 0, "5 taps: owned once, 20 shards spent once");
        Check(changed == 1, "the Gacha tab was told once");
        Check(!button.interactable && TextAt(root, "Detail/Status").text.Length > 0, "after the exchange the button is disabled");
        Check(TextAt(root, "ShardChip/Amount").text == "0", "the balance updates");
        Check(!complete.IsOpen, "no set was completed: no card");

        // completing 11/11 by exchange shows the card once
        Fresh();
        OwnAll("cat_doodle", except: 10);
        PlayerData.AddShards(100);
        ex.Open();
        root.Find("Cell10").GetComponent<Button>().onClick.Invoke();
        Check(TextAt(root, "Detail/Rarity").text.Contains("Majesty") && TextAt(root, "Detail/Cost").text == "50", "the level-10 cat is the Majesty tier at 50");
        button.onClick.Invoke();
        Check(complete.IsOpen && TextAt(complete.transform, "Card/Skill").text == "Eraser", "completing the set by exchange shows SET COMPLETE");
        Check(SetCompletion.IsComplete(doodle) && PlayerData.IsSkillUnlocked(ThemeSkills.EraserId) && PlayerData.Shards == 50, "11/11, unlocked, 50 shards spent");
        complete.Close();
        button.onClick.Invoke();
        Check(!complete.IsOpen && PlayerData.Shards == 50, "tapping again: no second card, no second payment");

        // another theme: any theme, even Legendary / Majesty
        Fresh();
        PlayerData.AddShards(90);
        ex.Open();
        SetField(ex, "_page", Theme("cat_gold").Index);
        Call(ex, "Refresh");
        root.Find("Cell9").GetComponent<Button>().onClick.Invoke();
        button.onClick.Invoke();
        Check(Cosmetics.IsOwned(Cat("cat_gold", 9)) && PlayerData.Shards == 90 - Cosmetics.ShardPrice(Cat("cat_gold", 9)), "a Legendary cat of another theme can be exchanged");

        ex.Close();
        Check(!ex.IsOpen, "back closes the screen");
        Dispose(canvas.gameObject);
    }

    static void GachaCheck()
    {
        Fresh();
        var canvas = NewCanvas("GachaCanvas");
        var gacha = Add<GachaPage>(canvas, "PageGacha");
        gacha.Build((RectTransform)gacha.transform, canvas);
        gacha.PlaceOverlays(0);
        var page = gacha.transform;
        var overlay = gacha.Overlay.transform;

        PlayerData.AddShards(7);
        gacha.Refresh();
        Check(TextAt(page, "ShardChip/Amount").text == "7", "gacha: the shard balance is shown");
        var exchange = page.Find("Exchange").GetComponent<Button>();
        exchange.onClick.Invoke();
        var exScreen = Field<ExchangeScreen>(gacha, "_exchange");
        Check(exScreen.IsOpen, "gacha: the EXCHANGE button opens the exchange screen");
        Check(gacha.OnBack() && !exScreen.IsOpen, "gacha: back closes it");

        // result cards
        var dupCat = Cosmetics.Award(Cat("cat_pixel", 2), Rarity.Common);
        dupCat = Cosmetics.Award(Cat("cat_pixel", 2), Rarity.Common);
        Call(gacha, "FillSingle", dupCat);
        Check(TextAt(overlay, "Card/Status").text.Contains("+" + dupCat.Refund), "gacha card: the coin refund is shown");
        Check(TextAt(overlay, "Card/Extra").text.Contains("+1") && overlay.Find("Card/Star").gameObject.activeSelf, "gacha card: the shard from a duplicate cat is shown with the star");

        var newCat = Cosmetics.Award(Cat("cat_pixel", 5), Rarity.Rare);
        Call(gacha, "FillSingle", newCat);
        Check(TextAt(overlay, "Card/Status").text == "NEW!", "gacha card: new cat");
        Check(TextAt(overlay, "Card/Extra").text == Theme("cat_pixel").Name + "  " + newCat.SetOwned + " / 11" && !overlay.Find("Card/Star").gameObject.activeSelf, "gacha card: set progress of a new cat: " + TextAt(overlay, "Card/Extra").text);

        var bg = Cosmetics.Award(Cosmetics.Backgrounds[2], Rarity.Common);
        bg = Cosmetics.Award(Cosmetics.Backgrounds[2], Rarity.Common);
        Call(gacha, "FillSingle", bg);
        Check(TextAt(overlay, "Card/Extra").text == "" && !overlay.Find("Card/Star").gameObject.activeSelf, "gacha card: a duplicate background shows no shard");
        Check(TextAt(overlay, "Card/Status").text.Contains("+" + bg.Refund), "gacha card: but its coin refund");

        // the 10-pull grid
        Call(gacha, "FillMini", 0, dupCat);
        Call(gacha, "FillMini", 1, newCat);
        Call(gacha, "FillMini", 2, bg);
        Check(TextAt(overlay, "Grid/MiniBorder0/Extra").text == "+1" && overlay.Find("Grid/MiniBorder0/Star").gameObject.activeSelf, "grid: duplicate cat shows +1 shard");
        Check(TextAt(overlay, "Grid/MiniBorder0/Tag").text == "+" + dupCat.Refund, "grid: and its coins");
        Check(TextAt(overlay, "Grid/MiniBorder1/Extra").text.Contains("/ 11") && !overlay.Find("Grid/MiniBorder1/Star").gameObject.activeSelf, "grid: new cat shows its set progress");
        Check(TextAt(overlay, "Grid/MiniBorder2/Extra").text == "" && !overlay.Find("Grid/MiniBorder2/Star").gameObject.activeSelf, "grid: duplicate background shows no shard");
        Dispose(canvas.gameObject);
    }

    // ------------------------------------------------------------------ the HUD

    sealed class FakeHandler : ISkillHandler
    {
        public float Seconds;
        public float Duration => Seconds;
        public bool CanTarget(Cat target) => true;
        public bool Activate(Cat target) => true;
        public void End() { }
    }

    static void Hud()
    {
        Fresh();
        Loc.SetLanguage("en");
        var gmGo = new GameObject("TestGM");
        Made.Add(gmGo);
        var gm = gmGo.AddComponent<GameManager>();                           // Awake does not run outside play mode: only the members the HUD reads are set up
        var energy = gm.Energy;
        energy.Reset();
        var skills = new SkillRuntime(energy);
        typeof(GameManager).GetProperty("Skills").GetSetMethod(true).Invoke(gm, new object[] { skills });

        var uiGo = new GameObject("TestUI");
        Made.Add(uiGo);
        var ui = uiGo.AddComponent<GameUI>();
        ui.Build(gm);
        var es = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (es != null) Made.Add(es.gameObject);

        // the pieces the layout assumes are where it assumes them
        var nextIcon = (RectTransform)uiGo.transform.Find("NextIcon");
        var nextLabel = (RectTransform)uiGo.transform.Find("NextLabel");
        var gear = (RectTransform)uiGo.transform.Find("SettingsButton");
        Check(nextIcon.anchoredPosition == new Vector2(-HudLayout.NextFromRight, -140f) && nextLabel.anchoredPosition.x == -HudLayout.NextFromRight, "NEXT is where HudLayout says");
        Check(gear.anchoredPosition == new Vector2(0f, -85f) && gear.sizeDelta == new Vector2(120f, 120f), "the gear is where HudLayout says");

        float maxNextIcon = 0f;
        foreach (int level in Balance.Current.dropBag) maxNextIcon = Mathf.Max(maxNextIcon, (40f + CatDatabase.Radius[level] * 110f) * 1.2f);
        Check(140f + maxNextIcon * 0.5f <= 204f, "the biggest NEXT icon (" + maxNextIcon.ToString("0") + ") ends above the layout's NEXT box (204)");

        // the real button and target line follow the layout on every screen
        foreach (var screen in Screens_)
        {
            var cv = CanvasFor(screen.x, screen.y);
            var l = ui.ApplyLayout(cv.x, cv.y);
            var rt = ui.SkillRect;
            Check(rt.anchoredPosition == new Vector2(l.Group.L, -l.Group.T), "button position follows the layout on " + screen);
            Check(rt.sizeDelta == new Vector2(l.Group.Width, l.Group.Height), "button size follows the layout");
            var br = ui.BannerRect;
            Check(Mathf.Abs(br.anchoredPosition.x - (l.Banner.L + l.Banner.R) * 0.5f) < 0.01f && Mathf.Abs(br.anchoredPosition.y + (l.Banner.T + l.Banner.B) * 0.5f) < 0.01f, "target line position follows the layout");
            var iconBox = new HudBox(cv.x - HudLayout.NextFromRight - maxNextIcon * 0.5f, 140f - maxNextIcon * 0.5f, cv.x - HudLayout.NextFromRight + maxNextIcon * 0.5f, 140f + maxNextIcon * 0.5f);
            var group = new HudBox(l.Group.L, l.Group.T, l.Group.R, l.Group.B);
            Check(!group.Overlaps(iconBox), "button does not touch the real NEXT icon on " + screen);
        }
        ui.ApplyLayout(1080, 1920);

        var update = typeof(GameUI).GetMethod("UpdateSkill", BindingFlags.NonPublic | BindingFlags.Instance);
        var root = uiGo.transform;
        Func<Text> badge = () => TextAt(root, "SkillButton/Body/Badge/Text");
        Func<bool> bannerOn = () => root.Find("TargetBanner").gameObject.activeSelf;
        Func<Image> ring = () => root.Find("SkillButton/Body/Ring").GetComponent<Image>();
        Action tick = () => update.Invoke(ui, null);
        Action<ThemeSkillDef> setSkill = s => typeof(GameManager).GetProperty("ActiveSkill").GetSetMethod(true).Invoke(gm, new object[] { s });
        Action<int> fill = target => { while (energy.Value < target) energy.OnMerge(MergeSource.Normal); };

        // no skill in this run: nothing on the HUD
        tick();
        Check(!ui.SkillRect.gameObject.activeSelf && !bannerOn(), "HUD: no skill, no button");

        // energy not full: ring and the % in the pill
        setSkill(ThemeSkills.Eraser);
        skills.Begin(ThemeSkills.Eraser, new FakeHandler());
        energy.Reset();
        tick();
        Check(ui.SkillRect.gameObject.activeSelf, "HUD: the button appears with a skill");
        Check(badge().text == energy.Value + "%" && Mathf.Abs(ring().fillAmount - energy.Value / 100f) < 0.001f, "HUD: energy ring and % (" + badge().text + ")");
        Check(!bannerOn(), "HUD: no target line while idle");
        Check(root.Find("SkillButton/Body/Icon").GetComponent<Image>().sprite != null, "HUD: the skill icon is drawn");

        // insufficient energy: the pill says what is needed (GameManager sends this text)
        Check(skills.Press() == SkillPress.NotEnoughEnergy, "pressing without energy");
        ui.ShowSkillMessage(Loc.T("skill_need_energy").Replace("{0}", energy.Cost.ToString()));
        tick();
        Check(badge().text == "NEED 100%", "HUD: insufficient energy message: " + badge().text);

        // ready
        energy.Reset();
        fill(100);
        typeof(GameUI).GetField("_badgeMsgT", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ui, -1f);
        tick();
        Check(badge().text == "READY" && ring().fillAmount > 0.999f, "HUD: ready");

        // choosing a target: the line with the hint and CANCEL, CANCEL on the button
        Check(skills.Press() == SkillPress.EnteredTargeting, "Eraser enters target mode");
        tick();
        Check(bannerOn() && TextAt(root, "TargetBanner/Hint").text == "Tap a cat to use the skill", "HUD: target hint");
        Check(badge().text == "CANCEL", "HUD: the button says CANCEL");
        Check(TextAt(root, "TargetBanner/Cancel/Text").text == "CANCEL", "HUD: a CANCEL button on the target line");
        ui.ShowTargetRejected();
        tick();
        Check(TextAt(root, "TargetBanner/Hint").text == Loc.T("skill_bad_target"), "HUD: a cat that cannot be chosen is explained");
        Check(skills.Press() == SkillPress.CancelledTargeting && energy.Value == 100, "cancel is free");
        tick();
        Check(!bannerOn(), "HUD: the target line is gone after cancel");

        // timed effect: countdown on the ring and in the pill
        skills.Begin(ThemeSkills.Moonlight, new FakeHandler { Seconds = 8f });
        energy.Reset();
        fill(100);
        Check(skills.Press() == SkillPress.Activated, "Moonlight activates");
        tick();
        Check(badge().text == "8s" && ring().fillAmount > 0.999f, "HUD: 8s countdown starts full");
        skills.Tick(3f);
        tick();
        Check(badge().text == "5s" && Mathf.Abs(ring().fillAmount - 5f / 8f) < 0.001f, "HUD: 5s left, ring 5/8 (" + badge().text + ")");
        skills.Tick(5f);
        tick();
        Check(!skills.IsTimedActive && badge().text == energy.Value + "%", "HUD: back to the energy after the effect");

        // game over: target mode off, the button locked
        skills.Begin(ThemeSkills.Eraser, new FakeHandler());
        energy.Reset();
        fill(100);
        skills.Press();
        typeof(GameManager).GetProperty("IsGameOver").GetSetMethod(true).Invoke(gm, new object[] { true });
        skills.Lock();
        tick();
        Check(!bannerOn() && !root.Find("SkillButton/Body/Bg").GetComponent<Button>().interactable, "HUD: game over hides the target line and locks the button");

        // Vietnamese
        typeof(GameManager).GetProperty("IsGameOver").GetSetMethod(true).Invoke(gm, new object[] { false });
        skills.Begin(ThemeSkills.Eraser, new FakeHandler());
        energy.Reset();
        fill(100);
        skills.Press();
        Loc.SetLanguage("vi");
        tick();
        Check(badge().text == "HUỶ" && TextAt(root, "TargetBanner/Hint").text == Loc.T("skill_tap_cat") && TextAt(root, "TargetBanner/Cancel/Text").text == "HUỶ", "HUD: Vietnamese texts");
        Loc.SetLanguage("en");
    }

    // ------------------------------------------------------------------ the views only show data

    static string Source(string relative) => File.ReadAllText(Application.dataPath + "/Scripts/" + relative);

    static void Guards()
    {
        string[] views = { "Menu/SkinPage.cs", "Menu/SkinSkillPanel.cs", "Menu/CodexScreen.cs", "Menu/ExchangeScreen.cs", "Menu/SetCompleteScreen.cs", "Menu/GachaPage.cs", "Menu/MenuController.cs" };
        foreach (var file in views)
        {
            string src = Source(file);
            foreach (var banned in new[] { "PlayerData.Unlock", "AddShards", "TrySpendShards", "MarkSkillUnlocked", "AddCoins", "SetCompletion.OnOwned", "SetCompletion.Backfill", "skillsUnlocked" })
                Check(!src.Contains(banned), file + " must not change data itself (" + banned + ")");
            foreach (var banned in new[] { "SkillRuntime", "SkillHandlers", "ISkillHandler", "GameManager", "SkillEnergy" })
                Check(!src.Contains(banned), file + " must not touch gameplay (" + banned + ")");
        }
        foreach (var file in new[] { "Menu/SkinPage.cs", "Menu/SkinSkillPanel.cs", "Menu/CodexScreen.cs", "Menu/ExchangeScreen.cs", "Menu/SetCompleteScreen.cs" })
            Check(!Source(file).Contains("IsSkillUnlocked"), file + " reads skill state only through ThemeSkillInfo");
        Check(!Source("Menu/ExchangeScreen.cs").Contains("PlayerData.IsOwned"), "the exchange screen asks Cosmetics, not PlayerData, who owns what");

        // the only data calls the screens make
        Check(Source("Menu/ExchangeScreen.cs").Contains("Cosmetics.Exchange(cat, out var completed)"), "exchange goes through Cosmetics.Exchange");
        Check(Source("Menu/SkinPage.cs").Contains("Cosmetics.EquipTheme(theme)"), "the wear-whole-theme button still uses the existing equip flow");
        Check(Source("Menu/SkinPage.cs").Contains("ThemeSkillInfo.Of(") && Source("Menu/SkinPage.cs").Contains("SkillResolver.Resolve()"), "the skin tab reads the shared status");
        Check(Source("Menu/CodexScreen.cs").Contains("ThemeSkillInfo.Of(") && Source("Menu/CodexScreen.cs").Contains("CodexProgress"), "the codex reads the shared status and progress");
        Check(Source("Menu/MenuController.cs").Contains("CodexProgress.CompleteThemes()"), "Home reads the Codex progress");

        // gacha: the completion card comes from the result, after the prizes, once
        string gacha = Source("Menu/GachaPage.cs");
        Check(Regex.IsMatch(gacha, @"yield return ShowGrid\(results\);\s*(//[^\n]*\s*)+foreach \(var r in results\)\s*\{\s*if \(r\.Completed == null\) continue;"), "the SET COMPLETE card follows the prize reveal and is driven by PullResult.Completed");
        Check(Regex.Matches(gacha, @"_complete\.Show\(").Count == 1, "one place shows the card in the gacha");
        Check(Source("Menu/ExchangeScreen.cs").Contains("if (completed != null) _complete.Show(completed)"), "the exchange shows the card only for the exchange that completed the set");
        Check(!gacha.Contains("SkillUnlocked"), "no event subscription: the card cannot appear twice");

        // HUD: the button only presses; the layout is the shared one
        string ui = Source("UI/GameUI.cs");
        Check(ui.Contains("_gm.PressSkill()") && !ui.Contains("Skills.Press") && !ui.Contains("SkillResolver") && !ui.Contains("SetCompletion"), "the HUD button only calls PressSkill and decides nothing itself");
        Check(ui.Contains("HudLayout.Compute("), "the HUD uses the shared layout");
        string gm = Source("Gameplay/GameManager.cs");
        Check(gm.Contains("SkillTap.InvalidTarget") && gm.Contains("_ui.ShowTargetRejected()"), "an invalid target is explained");
        Check(gm.Contains("CameraHalfHeight(cam.aspect)"), "the camera and the layout share one rule");
    }

    // ------------------------------------------------------------------ helpers

    static void Fresh()
    {
        PlayerData.ResetAll();
        PlayerData.Blocked = false;
        Reload();
        Loc.SetLanguage("en");                  // a reset goes back to the device language; the checks read English
    }

    static void Reload()
    {
        typeof(PlayerData).GetField("_d", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
        typeof(PlayerData).GetField("_runLoaded", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
        Cosmetics.Reload();
    }

    /// <summary>OnDestroy does not run outside play mode: call it so the static event subscriptions are removed.</summary>
    static void Dispose(GameObject go)
    {
        if (go == null) return;
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb.GetType().Assembly != typeof(GameUI).Assembly) continue;           // the game's own scripts only, not Unity's UI components
            var m = mb.GetType().GetMethod("OnDestroy", BindingFlags.NonPublic | BindingFlags.Instance);
            if (m != null) m.Invoke(mb, null);
        }
        UnityEngine.Object.DestroyImmediate(go);
    }

    static void DisposeAll()
    {
        foreach (var go in Made) Dispose(go);
        Made.Clear();
    }

    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }
}
