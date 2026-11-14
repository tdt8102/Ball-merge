using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>Section F (old / damaged saves) and H (duplicates, retry, economy persistence, debug tools, analytics).</summary>
public partial class E2eRunner
{
    const string SaveKey = "catcatcat.save", RunKey = "catcatcat.run";

    static string Quoted(System.Collections.Generic.IEnumerable<string> ids) => string.Join(",", ids.Select(i => "\"" + i + "\""));

    /// <summary>A save as the first versions wrote it: no shards, no unlocked-skill list.</summary>
    static string OldSave(string theme, bool outfit, int coins = 321)
    {
        var skins = Enumerable.Range(0, 11).Select(l => theme + "_" + l);
        var worn = Enumerable.Range(0, 11).Select(l => outfit ? theme : "cat_classic");
        return "{\"version\":1,\"coins\":" + coins + ",\"best\":99,\"seen\":[1,1,1,0,0,0,0,0,0,0,0],\"skins\":[" + Quoted(skins) + "],\"catSkin\":[" + Quoted(worn) + "],\"bg\":\"bg_cream\",\"frame\":\"fr_wood\",\"pity\":7,\"lang\":\"en\"}";
    }

    const string OldCats = "[{\"l\":0,\"x\":-1.5,\"y\":-4.9,\"r\":0,\"vx\":0,\"vy\":0,\"w\":0},{\"l\":1,\"x\":0,\"y\":-4.85,\"r\":0,\"vx\":0,\"vy\":0,\"w\":0},{\"l\":2,\"x\":1.5,\"y\":-4.8,\"r\":0,\"vx\":0,\"vy\":0,\"w\":0},{\"l\":3,\"x\":-0.5,\"y\":-3.9,\"r\":0,\"vx\":0,\"vy\":0,\"w\":0}]";

    void SetPrefs(string save, string run)
    {
        PlayerData.Blocked = false;
        if (save == null) PlayerPrefs.DeleteKey(SaveKey); else PlayerPrefs.SetString(SaveKey, save);
        if (run == null) PlayerPrefs.DeleteKey(RunKey); else PlayerPrefs.SetString(RunKey, run);
        PlayerPrefs.Save();
        Reread();
        Loc.SetLanguage("en");
    }

    IEnumerator OldSaves()
    {
        // F1: an old profile with a finished set and the matching outfit
        int events = 0;
        System.Action<ThemeSkillDef> h = s => events++;
        SetCompletion.SkillUnlocked += h;
        SetPrefs(OldSave("cat_doodle", true), null);
        bool unlocked = PlayerData.IsSkillUnlocked(ThemeSkills.EraserId);
        SetCompletion.SkillUnlocked -= h;
        R("F1", "Old save (no shards / unlocked list), set already complete: loads, backfills the unlock silently, keeps coins/best/pity",
            "shards 0; unlocked; 0 events; coins 321; best 99; pity 7",
            "shards " + PlayerData.Shards + "; " + unlocked + "; " + events + " events; coins " + PlayerData.Coins + "; best " + PlayerData.Best + "; pity " + PlayerData.Pity,
            PlayerData.Shards == 0 && unlocked && events == 0 && PlayerData.Coins == 321 && PlayerData.Best == 99 && PlayerData.Pity == 7);
        yield return LoadMain(false);
        R("F2", "New game from that old profile: Eraser, energy 30", "Eraser; 30", (GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id) + "; " + GM.Energy.Value, GM.ActiveSkill == ThemeSkills.Eraser && GM.Energy.Value == 30);

        // F3: old run snapshot (no skill / energy fields) of that profile
        SetPrefs(OldSave("cat_doodle", true), "{\"score\":120,\"next\":2,\"coins\":5,\"cats\":" + OldCats + "}");
        yield return LoadMain(true);
        R("F3", "Old run snapshot, outfit complete: Continue resolves the skill once, energy 30, score and 4 cats back",
            "Eraser; 30; 120; 4 cats", (GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id) + "; " + GM.Energy.Value + "; " + GM.Score + "; " + Board().Count + " cats",
            GM.ActiveSkill == ThemeSkills.Eraser && GM.Energy.Value == 30 && GM.Score == 120 && Board().Count == 4);
        GM.SaveProgress();
        R("F4", "The next save records the skill and energy", "recorded; skill_eraser; energy 30", PlayerData.Run.skillRecorded + "; " + PlayerData.Run.activeSkillId + "; " + PlayerData.Run.energy,
            PlayerData.Run.skillRecorded && PlayerData.Run.activeSkillId == ThemeSkills.EraserId && PlayerData.Run.energy == 30);

        // F5: old run, but the outfit is Classic
        SetPrefs(OldSave("cat_doodle", false), "{\"score\":50,\"next\":1,\"coins\":0,\"cats\":" + OldCats + "}");
        yield return LoadMain(true);
        R("F5", "Old run snapshot, outfit not complete: no skill, no button", "null; hidden", (GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id) + "; " + UI.SkillRect.gameObject.activeSelf, GM.ActiveSkill == null && !UI.SkillRect.gameObject.activeSelf);

        // F6: tampered values
        SetPrefs(OldSave("cat_doodle", true), "{\"score\":1,\"next\":0,\"coins\":0,\"skillRecorded\":true,\"activeSkillId\":\"skill_hug\",\"energyRecorded\":true,\"energy\":9999,\"cats\":" + OldCats + "}");
        yield return LoadMain(true);
        R("F6", "Run naming a skill that is not unlocked, energy 9999: no skill, energy clamped to 100", "null; 100", (GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id) + "; " + GM.Energy.Value, GM.ActiveSkill == null && GM.Energy.Value == 100);
        SetPrefs(OldSave("cat_doodle", true), "{\"score\":1,\"next\":0,\"coins\":0,\"skillRecorded\":true,\"activeSkillId\":\"skill_gone\",\"energyRecorded\":true,\"energy\":-5,\"cats\":" + OldCats + "}");
        yield return LoadMain(true);
        R("F7", "Run naming an unknown skill, energy -5: no skill, energy 0", "null; 0", (GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id) + "; " + GM.Energy.Value, GM.ActiveSkill == null && GM.Energy.Value == 0);

        // F8: damaged data
        SetPrefs("{not json", "{also not json");
        R("F8", "Damaged save and damaged run: fresh profile, no run, no exception", "coins 0; shards 0; no run", PlayerData.Coins + "; " + PlayerData.Shards + "; " + (PlayerData.Run == null ? "no run" : "run"), PlayerData.Coins == 0 && PlayerData.Shards == 0 && PlayerData.Run == null);
        yield return LoadMain(false);
        R("F9", "The game starts normally from a damaged save", "new game, energy 30", GM.Energy.Value, GM.Energy.Value == 30 && !GM.IsGameOver);
        SetPrefs(OldSave("cat_doodle", true), "{\"score\":1,\"next\":0,\"coins\":0,\"cats\":[{\"l\":99,\"x\":0,\"y\":0,\"r\":0,\"vx\":0,\"vy\":0,\"w\":0}]}");
        R("F10", "A run with an impossible cat level is rejected as damaged", "no run", PlayerData.Run == null ? "no run" : "run kept", PlayerData.Run == null);

        // F11: stale ids in the unlocked list
        SetPrefs(OldSave("cat_doodle", true).Replace("\"lang\":\"en\"}", "\"lang\":\"en\",\"skillsUnlocked\":[\"skill_removed\"]}"), null);
        R("F11", "Unknown id in skillsUnlocked is harmless; real unlocks still come from the collection", "Eraser unlocked; no crash", PlayerData.IsSkillUnlocked(ThemeSkills.EraserId), PlayerData.IsSkillUnlocked(ThemeSkills.EraserId));
    }

    IEnumerator EconomyAndDebug()
    {
        // H1 duplicates
        Fresh();
        var cat = CatOf("cat_pixel", 2);
        Cosmetics.Award(cat, Rarity.Common);
        int coins = PlayerData.Coins;
        var dup = Cosmetics.Award(cat, Rarity.Common);
        PlayerData.Flush();
        Reread();
        R("H1", "Duplicate cat: +1 shard and the usual coin refund, kept after a restart", "shards 1; refund " + Cosmetics.DuplicateCoins(Rarity.Common),
            "shards " + PlayerData.Shards + "; refund " + (PlayerData.Coins - coins), PlayerData.Shards == 1 && PlayerData.Coins - coins == dup.Refund && dup.Refund == Cosmetics.DuplicateCoins(Rarity.Common));

        // H2 exchange persists, spends once
        Fresh();
        PlayerData.AddShards(40);
        var target = CatOf("cat_kawaii", 1);
        var res = Cosmetics.Exchange(target);
        PlayerData.Flush();
        Reread();
        var again = Cosmetics.Exchange(target);
        R("H2", "Exchange (20 shards), restart, exchange again: owned, spent once", "Success; owned; 20 left; AlreadyOwned; 20",
            res + "; " + PlayerData.IsOwned(target.Id) + "; " + again + "; " + PlayerData.Shards, res == ExchangeResult.Success && PlayerData.IsOwned(target.Id) && again == ExchangeResult.AlreadyOwned && PlayerData.Shards == 20);

        // H3 retry after a failed grant
        Fresh();
        PlayerData.AddShards(20);
        var t2 = CatOf("cat_doodle", 0);
        var failed = Cosmetics.Exchange(t2, id => throw new System.InvalidOperationException("injected"));
        int afterFail = PlayerData.Shards;
        var retry = Cosmetics.Exchange(t2);
        R("H3", "Exchange whose grant fails (injected), then retry", "Failed; 20 back; Success; 0", failed + "; " + afterFail + "; " + retry + "; " + PlayerData.Shards,
            failed == ExchangeResult.Failed && afterFail == 20 && retry == ExchangeResult.Success && PlayerData.Shards == 0, "the injected exception is logged by Exchange (expected)");
        _errors.RemoveAll(e => e.Contains("injected"));

        // H4 completing a set by exchange leads to a working skill in a real run
        Fresh();
        Award("cat_gold", 10);
        PlayerData.AddShards(50);
        var done = Cosmetics.Exchange(CatOf("cat_gold", 10), out var completed);
        Outfit("cat_gold");
        yield return LoadMain(false);
        R("H4", "Set completed by exchange (Majesty, 50) -> equip -> run has Midas", "Success; Midas unlocked; Midas active; 30", done + "; " + completed + "; " + (GM.ActiveSkill == null ? "null" : GM.ActiveSkill.Id) + "; " + GM.Energy.Value,
            done == ExchangeResult.Success && completed == ThemeSkills.Midas && GM.ActiveSkill == ThemeSkills.Midas && GM.Energy.Value == 30);

        // H5 data reset (the cheat's RESET) leaves nothing of the Theme Set data
        GM.SaveProgress();
        PlayerData.AddShards(9);
        PlayerData.Flush();
        PlayerData.ResetAll();
        bool keys = PlayerPrefs.HasKey(SaveKey) || PlayerPrefs.HasKey(RunKey);
        PlayerData.Blocked = false;
        R("H5", "Reset player data clears shards, unlocked skills, outfit and the saved run", "0 shards; locked; no run; Classic outfit; keys gone",
            PlayerData.Shards + "; " + PlayerData.IsSkillUnlocked(ThemeSkills.MidasId) + "; " + (PlayerData.Run == null ? "no run" : "run") + "; " + SkillResolver.Resolve().Status + "; keys " + keys,
            PlayerData.Shards == 0 && !PlayerData.IsSkillUnlocked(ThemeSkills.MidasId) && PlayerData.Run == null && SkillResolver.Resolve().Status == SkillStatus.Classic && !keys);

        // H6 debug tools
        string cheat = Source("UI/CheatPanel.cs") + Source("UI/SettingsPopup.cs");
        bool gated = cheat.Contains("isDebugBuild") || cheat.Contains("DEVELOPMENT_BUILD") || cheat.Contains("UNITY_EDITOR");
        R("H6", "Cheat menu (5 taps on SETTINGS + password; +coins, reset all data) is only reachable in development/debug builds", "gated by a build check", gated ? "gated" : "NOT gated: ships in every build, password 2609 is in the client code",
            gated, "existing tester tool, not a Theme Set Skills feature: needs a product decision (see report)", known: true);
        R("H7", "No cheat grants shards, skills or energy (nothing in the tools can corrupt Theme Set data)", "no shard/skill/energy cheat", "none",
            !cheat.Contains("AddShards") && !cheat.Contains("MarkSkillUnlocked") && !cheat.Contains("Energy"));
        string dbg = Source("UI/GameUI.cs");
        int at = dbg.IndexOf("Application.absoluteURL");
        string block = at < 0 ? "" : dbg.Substring(at, 700);
        R("H8", "The ?debug overlay (URL contains 'debug') only displays fps, cat count, memory and the last error", "no data access in the overlay code", at < 0 ? "overlay missing" : (block.Contains("PlayerData") || block.Contains("Cosmetics") ? "touches data" : "display only"),
            at >= 0 && !block.Contains("PlayerData") && !block.Contains("Cosmetics"), "it is still reachable in production builds by adding ?debug to the URL (harmless: read-only)");

        // H9 analytics
        string[] hits = System.IO.Directory.GetFiles(Application.dataPath, "*.*", System.IO.SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".html") || f.EndsWith(".js") || f.EndsWith(".jslib"))
            .Where(f => !f.Contains("/Editor/") && !f.Contains("\\Editor\\"))
            .Where(f => System.Text.RegularExpressions.Regex.IsMatch(System.IO.File.ReadAllText(f), "analytics|firebase|gtag|LogEvent|telemetry|mixpanel", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            .ToArray();
        R("H9", "Analytics convention in the project", "found or none", hits.Length == 0 ? "none found (no SDK, no event code)" : string.Join(",", hits.Select(System.IO.Path.GetFileName)), hits.Length == 0, "nothing was added or sent; proposed events are listed in the report");
    }
}
