using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Renders the Phase 14 screens to PNG files (Temp/uishots) so the layout can be looked at: Home, Gacha (tab, result card, grid),
/// Skin, Codex, Exchange, Set complete and the in-game HUD, at several aspect ratios and in English and Vietnamese.
/// Needs a graphics device, so it is run WITHOUT -nographics: Unity -batchmode -executeMethod UiShots.Run.
/// The real save is backed up first and restored afterwards.
/// </summary>
public static class UiShots
{
    const string SaveKey = "catcatcat.save", RunKey = "catcatcat.run";
    const float Unit = 0.01f;                       // world units per canvas unit
    static string _dir;
    static Camera _cam;
    static readonly List<GameObject> Made = new List<GameObject>();

    public static void Run()
    {
        string saveBackup = PlayerPrefs.GetString(SaveKey, null), runBackup = PlayerPrefs.GetString(RunKey, null);
        bool ok = true;
        try
        {
            _dir = Path.Combine(Path.GetTempPath(), "suika-uishots");          // (the project's own Temp folder is wiped when Unity exits)
            Directory.CreateDirectory(_dir);
            foreach (var f in Directory.GetFiles(_dir, "*.png")) File.Delete(f);
            var camGo = new GameObject("ShotCam") { tag = "MainCamera" };
            Made.Add(camGo);
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.transform.position = new Vector3(0, 0, -10);
            Audio();
            MenuShots();
            HudShots();
            Debug.Log("UISHOTS_OK " + _dir);
        }
        catch (Exception e)
        {
            ok = false;
            Debug.LogError("UiShots failed: " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            foreach (var go in Made) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            PlayerData.ResetAll();
            PlayerData.Blocked = false;
            if (saveBackup != null) PlayerPrefs.SetString(SaveKey, saveBackup);
            if (runBackup != null) PlayerPrefs.SetString(RunKey, runBackup);
            PlayerPrefs.Save();
            Reload();
            Loc.ResetLanguage();
        }
        EditorApplication.Exit(ok ? 0 : 1);
    }

    // ------------------------------------------------------------------ helpers

    static Vector2 CanvasFor(float w, float h)
    {
        float scale = Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(w / 1080f, 2f), Mathf.Log(h / 1920f, 2f), 0.5f));
        return new Vector2(w / scale, h / scale);
    }

    static void Audio()
    {
        var go = new GameObject("ShotAudio");
        var am = go.AddComponent<AudioManager>();
        typeof(AudioManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(am, null);
        Made.Add(go);
    }

    static void Fresh()
    {
        PlayerData.ResetAll();
        PlayerData.Blocked = false;
        Reload();
        Loc.SetLanguage("en");
    }

    static void Reload()
    {
        typeof(PlayerData).GetField("_d", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
        typeof(PlayerData).GetField("_runLoaded", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
        Cosmetics.Reload();
    }

    static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);
    static void SetField(object o, string name, object v) => o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(o, v);
    static object Call(object o, string name, params object[] a) => o.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(o, a);

    static void Own(string theme, int count)
    {
        for (int l = 0; l < count; l++) PlayerData.Unlock(Cosmetics.CatItemOf(Cosmetics.FindTheme(theme), l).Id);
    }

    /// <summary>Make a canvas a world-space one that exactly fills the camera for a screen of this size.</summary>
    static Vector2 Fit(Canvas canvas, int w, int h)
    {
        var cv = CanvasFor(w, h);
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null) scaler.enabled = false;
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        var rt = (RectTransform)canvas.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = cv;
        rt.localScale = Vector3.one * Unit;
        rt.position = Vector3.zero;
        _cam.aspect = w / (float)h;
        _cam.orthographicSize = cv.y * Unit * 0.5f;
        return cv;
    }

    static void Shoot(string name, int w, int h)
    {
        Canvas.ForceUpdateCanvases();
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        _cam.targetTexture = rt;
        _cam.aspect = w / (float)h;
        Canvas.ForceUpdateCanvases();
        _cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(_dir, name + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null;
        _cam.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(tex);
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);
    }

    // ------------------------------------------------------------------ menu screens

    static void MenuShots()
    {
        Fresh();
        _cam.backgroundColor = new Color(0.98f, 0.93f, 0.84f);
        var menuGo = new GameObject("Menu");
        Made.Add(menuGo);
        var menu = menuGo.AddComponent<MenuController>();
        typeof(MenuController).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(menu, null);
        var es = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (es != null) Made.Add(es.gameObject);
        var canvas = menuGo.GetComponentInChildren<Canvas>();
        foreach (var theme in Cosmetics.Cats)                  // draw every cat now: the pages draw them one per frame, and no frames run here
            for (int l = 0; l <= CatDatabase.MaxLevel; l++) CatSprites.Icon(l, theme.Index);

        PlayerData.AddCoins(2500);
        PlayerData.AddShards(25);
        for (int l = 0; l < 4; l++) PlayerData.RecordSeen(l);
        Own("cat_doodle", 7);
        Own("cat_kawaii", 3);
        Cosmetics.Reload();
        var skin = Field<SkinPage>(menu, "_skin");
        var gacha = Field<GachaPage>(menu, "_gacha");
        int doodle = Cosmetics.FindTheme("cat_doodle").Index;

        foreach (var size in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2340), new Vector2Int(1536, 2048) })
        {
            string tag = size.x + "x" + size.y;
            Fit(canvas, size.x, size.y);
            bool main = size.x == 1080 && size.y == 1920;

            Tab(menu, 1);
            Shoot("home_" + tag, size.x, size.y);
            if (main)
            {
                Tab(menu, 0);
                Shoot("gacha_" + tag, size.x, size.y);
            }

            SetField(skin, "_theme", doodle);
            Tab(menu, 2);
            Shoot("skin_locked_" + tag, size.x, size.y);

            if (main)
            {
                Own("cat_doodle", 11);
                Own("cat_kawaii", 11);
                for (int l = 0; l < 9; l++) Cosmetics.Equip(Cosmetics.CatItemOf(Cosmetics.FindTheme("cat_doodle"), l));
                Cosmetics.Equip(Cosmetics.CatItemOf(Cosmetics.FindTheme("cat_kawaii"), 3));
                skin.Refresh();
                Shoot("skin_unlocked_" + tag, size.x, size.y);
                Cosmetics.EquipTheme(Cosmetics.FindTheme("cat_doodle"));
                skin.Refresh();
                Shoot("skin_active_" + tag, size.x, size.y);
                SetField(skin, "_slot", 4);
                skin.Refresh();
                Shoot("skin_outfit_" + tag, size.x, size.y);
                SetField(skin, "_slot", -1);
                Loc.SetLanguage("vi");
                skin.Refresh();
                Shoot("skin_active_vi_" + tag, size.x, size.y);
                Loc.SetLanguage("en");
                Fresh2(menu);
            }

            var codex = (CodexScreen)menu.GetType().GetProperty("Codex", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(menu);
            SetField(codex, "_page", doodle);
            codex.Open();
            Shoot("codex_" + tag, size.x, size.y);
            if (main)
            {
                Loc.SetLanguage("vi");
                Call(codex, "Refresh");
                Shoot("codex_vi_" + tag, size.x, size.y);
                Loc.SetLanguage("en");
                SetField(codex, "_page", 0);
                Call(codex, "Refresh");
                Shoot("codex_classic_" + tag, size.x, size.y);
            }
            codex.Close();

            Tab(menu, 0);
            var exchange = Field<ExchangeScreen>(gacha, "_exchange");
            exchange.Open();
            Shoot("exchange_" + tag, size.x, size.y);
            if (main)
            {
                SetField(exchange, "_page", Cosmetics.FindTheme("cat_gold").Index);
                SetField(exchange, "_selected", 10);
                Call(exchange, "Refresh");
                Shoot("exchange_gold_" + tag, size.x, size.y);
                Loc.SetLanguage("vi");
                Call(exchange, "Refresh");
                Shoot("exchange_vi_" + tag, size.x, size.y);
                Loc.SetLanguage("en");
            }
            exchange.Close();

            if (main)
            {
                var complete = Field<SetCompleteScreen>(gacha, "_complete");
                complete.Show(ThemeSkills.Hug);
                Shoot("complete_" + tag, size.x, size.y);
                complete.Close();
                GachaCards(gacha, size);
            }
        }
        if (es != null) UnityEngine.Object.DestroyImmediate(es.gameObject);
        UnityEngine.Object.DestroyImmediate(menuGo);          // the HUD shots must not show the menu behind them
    }

    static void Fresh2(MenuController menu)
    {
        // back to a partial collection for the screens that follow
        PlayerData.ResetAll();
        PlayerData.Blocked = false;
        Reload();
        Loc.SetLanguage("en");
        PlayerData.AddCoins(2500);
        PlayerData.AddShards(25);
        for (int l = 0; l < 4; l++) PlayerData.RecordSeen(l);
        Own("cat_doodle", 7);
        Own("cat_kawaii", 3);
        Cosmetics.Reload();
    }

    static void Tab(MenuController menu, int tab)
    {
        var select = typeof(MenuController).GetMethod("SelectTab", BindingFlags.NonPublic | BindingFlags.Instance);
        select.Invoke(menu, new object[] { tab, true });
        var pill = Field<RectTransform>(menu, "_pill");
        float x = (tab + 0.5f) / 4f;
        pill.anchorMin = pill.anchorMax = new Vector2(x, 0f);
    }

    static PullResult Dup(Item item)
    {
        Cosmetics.Award(item, item.Rarity);
        return Cosmetics.Award(item, item.Rarity);
    }

    /// <summary>The prize card and the 10-pull grid, filled the way a real pull fills them (without the box animation).</summary>
    static void GachaCards(GachaPage gacha, Vector2Int size)
    {
        var pixel = Cosmetics.FindTheme("cat_pixel");
        var dupCat = Dup(Cosmetics.CatItemOf(pixel, 2));
        var newCat = Cosmetics.Award(Cosmetics.CatItemOf(pixel, 5), Rarity.Rare);
        var dupBg = Dup(Cosmetics.Backgrounds[2]);
        string tag = size.x + "x" + size.y;

        gacha.Overlay.SetActive(true);
        Call(gacha, "ResetOverlay");
        Field<RectTransform>(gacha, "_ovBox").gameObject.SetActive(false);
        var ok = Field<Button>(gacha, "_btnOk");
        ok.gameObject.SetActive(true);
        var card = Field<RectTransform>(gacha, "_cardRoot");

        foreach (var (name, result) in new[] { ("dupcat", dupCat), ("newcat", newCat), ("dupbg", dupBg) })
        {
            Call(gacha, "FillSingle", result);
            card.localScale = Vector3.one;
            Shoot("gacha_card_" + name + "_" + tag, size.x, size.y);
        }

        Call(gacha, "ResetOverlay");
        Field<RectTransform>(gacha, "_ovBox").gameObject.SetActive(false);
        Field<RectTransform>(gacha, "_gridRoot").gameObject.SetActive(true);
        ok.gameObject.SetActive(true);
        ((RectTransform)ok.transform).anchoredPosition = new Vector2(0, -520);
        var results = new List<PullResult>
        {
            dupCat,
            Cosmetics.Award(Cosmetics.CatItemOf(pixel, 7), Rarity.Rare),
            dupBg,
            Dup(Cosmetics.CatItemOf(Cosmetics.FindTheme("cat_gold"), 9)),
            Cosmetics.Award(Cosmetics.CatItemOf(Cosmetics.FindTheme("cat_candy"), 1), Rarity.Common),
            newCat,
            Dup(Cosmetics.Frames[1]),
            Dup(Cosmetics.CatItemOf(Cosmetics.FindTheme("cat_midnight"), 10)),
            Cosmetics.Award(Cosmetics.Frames[2], Rarity.Common),
            Cosmetics.Award(Cosmetics.CatItemOf(Cosmetics.FindTheme("cat_chibi"), 4), Rarity.Common)
        };
        for (int i = 0; i < results.Count; i++)
        {
            Call(gacha, "FillMini", i, results[i]);
            ((RectTransform)Field<GameObject[]>(gacha, "_mini")[i].transform).localScale = Vector3.one;
        }
        Shoot("gacha_grid_" + tag, size.x, size.y);
        gacha.Overlay.SetActive(false);
    }

    // ------------------------------------------------------------------ HUD

    sealed class Fake : ISkillHandler
    {
        public float Seconds;
        public float Duration => Seconds;
        public bool CanTarget(Cat target) => true;
        public bool Activate(Cat target) => true;
        public void End() { }
    }

    static void HudShots()
    {
        Fresh();
        // Doodle fully owned and worn: the next game has Eraser
        PlayerData.AddCoins(500);
        Own("cat_doodle", 11);
        Cosmetics.Reload();
        Cosmetics.EquipTheme(Cosmetics.FindTheme("cat_doodle"));

        var gmGo = new GameObject("ShotGM");
        Made.Add(gmGo);
        var gm = gmGo.AddComponent<GameManager>();
        var energy = gm.Energy;
        var skills = new SkillRuntime(energy);
        typeof(GameManager).GetProperty("Skills").GetSetMethod(true).Invoke(gm, new object[] { skills });
        typeof(GameManager).GetProperty("ActiveSkill").GetSetMethod(true).Invoke(gm, new object[] { ThemeSkills.Eraser });
        typeof(GameManager).GetMethod("BuildPlayfield", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(gm, null);
        var dim = Field<SpriteRenderer>(gm, "_dim");
        _cam.backgroundColor = Cosmetics.Bg.Outer;

        // a cat held at the far right of the drop band: the worst place for a button to be
        var held = new GameObject("HeldCat");
        Made.Add(held);
        var sr = held.AddComponent<SpriteRenderer>();
        sr.sprite = CatSprites.Icon(4);
        sr.sortingOrder = 10;
        float diameter = CatDatabase.Radius[4] * 2f * 1.2f;
        held.transform.localScale = Vector3.one * diameter / sr.sprite.bounds.size.x;
        held.transform.position = new Vector3(GameManager.HalfWidth - CatDatabase.Radius[4], GameManager.SpawnY, 0f);

        var uiGo = new GameObject("ShotUI");
        Made.Add(uiGo);
        var ui = uiGo.AddComponent<GameUI>();
        ui.Build(gm);
        var es = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (es != null) Made.Add(es.gameObject);
        var canvas = uiGo.GetComponent<Canvas>();
        var update = typeof(GameUI).GetMethod("UpdateSkill", BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var size in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2340), new Vector2Int(1080, 2220), new Vector2Int(1536, 2048), new Vector2Int(1920, 1080), new Vector2Int(2560, 1080) })
        {
            string tag = size.x + "x" + size.y;
            Fit(canvas, size.x, size.y);
            held.transform.position = new Vector3(GameManager.HalfWidth - CatDatabase.Radius[4], GameManager.SpawnY, 0f);

            skills.Begin(ThemeSkills.Eraser, new Fake());
            energy.Reset();
            dim.enabled = false;
            ui.Refresh();
            update.Invoke(ui, null);
            Shoot("hud_idle_" + tag, size.x, size.y);

            while (energy.Value < 100) energy.OnMerge(MergeSource.Normal);
            skills.Press();                                  // Eraser: choose a target
            dim.enabled = true;
            update.Invoke(ui, null);
            Shoot("hud_target_" + tag, size.x, size.y);
            dim.enabled = false;
            skills.Press();                                  // cancel

            if (size.x == 1080 && size.y == 1920)
            {
                update.Invoke(ui, null);
                Shoot("hud_ready_" + tag, size.x, size.y);
                skills.Begin(ThemeSkills.Moonlight, new Fake { Seconds = 8f });
                energy.Reset();
                while (energy.Value < 100) energy.OnMerge(MergeSource.Normal);
                skills.Press();
                skills.Tick(2.6f);
                update.Invoke(ui, null);
                Shoot("hud_timed_" + tag, size.x, size.y);
                skills.Begin(ThemeSkills.Eraser, new Fake());
                energy.Reset();
                ui.ShowSkillMessage(Loc.T("skill_need_energy").Replace("{0}", energy.Cost.ToString()));
                update.Invoke(ui, null);
                Shoot("hud_need_" + tag, size.x, size.y);
            }
        }
    }
}
