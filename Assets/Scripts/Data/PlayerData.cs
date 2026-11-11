using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Everything the game remembers: coins, best score and how many times each cat has appeared (the Cat Codex).
/// Stored as one JSON string in PlayerPrefs (IndexedDB on WebGL). Changes are written by Flush(), which the game
/// calls at game over, when leaving to the menu, when the app is paused and every few seconds while playing.
/// </summary>
/// <summary>One cat on the board, as saved.</summary>
[Serializable]
public class CatState
{
    public int l;                    // level
    public float x, y, r;            // position and rotation (degrees)
    public float vx, vy, w;          // velocity and angular velocity
}

/// <summary>The game in progress, so a closed or reloaded page can continue where the player left off.</summary>
[Serializable]
public class RunSnapshot
{
    public int score;
    public int next;                 // level of the next cat to offer
    public int coins;                // coins earned in this run (for the game-over summary)
    public List<CatState> cats = new List<CatState>();
}

public static class PlayerData
{
    const string Key = "catcatcat.save";
    const string RunKey = "catcatcat.run";
    const int Levels = CatDatabase.MaxLevel + 1;

    [Serializable]
    class Data
    {
        public int version = 1;
        public int coins;
        public int best;
        public int[] seen = new int[Levels];     // how many times each cat appeared (drop or merge)
        public List<string> skins = new List<string>();               // owned item ids (cat styles, backgrounds, frames)
        public string[] catSkin = new string[Levels];                 // equipped cat style per cat level
        public string bg = Cosmetics.DefaultBg;                       // equipped background
        public string frame = Cosmetics.DefaultFrame;                 // equipped tray frame
        public int pity;                                              // gacha pulls since the last Legendary
        public string lang = "";                 // "en", "vi" or "" = follow the device
    }

    static Data _d;
    static bool _dirty;
    static RunSnapshot _run;
    static bool _runLoaded, _runDirty;
    static string _lastRunJson;

    /// <summary>Set by the Continue button; GameManager reads it once when the game scene starts.</summary>
    public static bool ResumeRequested;

    /// <summary>The saved game in progress, or null.</summary>
    public static RunSnapshot Run
    {
        get
        {
            if (!_runLoaded) LoadRun();
            return _run;
        }
    }

    public static bool HasRun => Run != null;

    /// <summary>True between a data reset and the next menu load: nothing may be written, so the reset cannot be undone by an autosave.</summary>
    public static bool Blocked;

    /// <summary>Forget everything: coins, best score, collection, equipped skins, language and the game in progress.</summary>
    public static void ResetAll()
    {
        Blocked = true;
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.DeleteKey(RunKey);
        PlayerPrefs.DeleteKey("best");                    // score key of the first versions
        PlayerPrefs.Save();
        _d = null;
        _dirty = false;
        _run = null;
        _runLoaded = true;
        _runDirty = false;
        _lastRunJson = "";
        ResumeRequested = false;
        Loc.ResetLanguage();
        Cosmetics.Reload();
    }

    /// <summary>Remember the game in progress (null or an empty board clears it).</summary>
    public static void SaveRun(RunSnapshot snapshot)
    {
        if (Blocked) return;
        _run = snapshot != null && snapshot.cats.Count > 0 ? snapshot : null;
        _runLoaded = true;
        _runDirty = true;
    }

    public static void ClearRun() => SaveRun(null);

    static Data D
    {
        get
        {
            if (_d == null) Load();
            return _d;
        }
    }

    public static int Coins => D.coins;
    public static int Best => D.best;
    public static int Seen(int level) => D.seen[Mathf.Clamp(level, 0, Levels - 1)];
    public static bool IsDiscovered(int level) => Seen(level) > 0;

    public static int DiscoveredCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < Levels; i++) if (D.seen[i] > 0) n++;
            return n;
        }
    }

    public static void AddCoins(int amount)
    {
        if (amount == 0) return;
        D.coins = Mathf.Max(0, D.coins + amount);
        _dirty = true;
    }

    public static bool TrySpend(int amount)
    {
        if (amount < 0 || D.coins < amount) return false;
        D.coins -= amount;
        _dirty = true;
        return true;
    }

    public static string Language
    {
        get => D.lang ?? "";
        set { D.lang = value; _dirty = true; }
    }

    // ---- collection: owned items and what is equipped (see Cosmetics) ----
    public static bool IsOwned(string id) => D.skins.Contains(id);

    /// <summary>Returns true if the item was newly unlocked.</summary>
    public static bool Unlock(string id)
    {
        if (string.IsNullOrEmpty(id) || D.skins.Contains(id)) return false;
        D.skins.Add(id);
        _dirty = true;
        return true;
    }

    public static string CatSkin(int level) => D.catSkin[Mathf.Clamp(level, 0, Levels - 1)];

    public static void SetCatSkin(int level, string id)
    {
        D.catSkin[Mathf.Clamp(level, 0, Levels - 1)] = id;
        _dirty = true;
    }

    public static string Background
    {
        get => D.bg;
        set { D.bg = value; _dirty = true; }
    }

    public static string Frame
    {
        get => D.frame;
        set { D.frame = value; _dirty = true; }
    }

    public static int Pity
    {
        get => D.pity;
        set { D.pity = Mathf.Max(0, value); _dirty = true; }
    }

    public static void SetBest(int score)
    {
        if (score <= D.best) return;
        D.best = score;
        _dirty = true;
    }

    /// <summary>Count one appearance of this cat. Returns true the first time ever (a new discovery).</summary>
    public static bool RecordSeen(int level)
    {
        level = Mathf.Clamp(level, 0, Levels - 1);
        bool first = D.seen[level] == 0;
        D.seen[level]++;
        _dirty = true;
        return first;
    }

    /// <summary>Write pending changes to disk (no-op when nothing changed).</summary>
    public static void Flush()
    {
        if (Blocked) return;
        bool write = false;
        if (_dirty && _d != null)
        {
            _dirty = false;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(_d));
            write = true;
        }
        if (_runDirty)
        {
            _runDirty = false;
            string json = _run == null ? "" : JsonUtility.ToJson(_run);
            if (json != _lastRunJson)                         // the board often has not changed since the last save
            {
                _lastRunJson = json;
                if (json.Length == 0) PlayerPrefs.DeleteKey(RunKey); else PlayerPrefs.SetString(RunKey, json);
                write = true;
            }
        }
        if (write) PlayerPrefs.Save();
    }

    static void LoadRun()
    {
        _runLoaded = true;
        _run = null;
        string json = PlayerPrefs.GetString(RunKey, "");
        _lastRunJson = json;
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            var r = JsonUtility.FromJson<RunSnapshot>(json);
            if (r == null || r.cats == null || r.cats.Count == 0 || r.cats.Count > 300) return;
            foreach (var c in r.cats)
            {
                bool ok = c.l >= 0 && c.l <= CatDatabase.MaxLevel
                          && !float.IsNaN(c.x) && !float.IsNaN(c.y) && Mathf.Abs(c.x) < 20f && Mathf.Abs(c.y) < 20f;
                if (!ok) return;                              // damaged save: ignore it
            }
            r.next = Mathf.Clamp(r.next, 0, CatDatabase.MaxLevel);
            _run = r;
        }
        catch (Exception) { _run = null; }
    }

    static void Load()
    {
        _d = null;
        string json = PlayerPrefs.GetString(Key, "");
        if (!string.IsNullOrEmpty(json))
        {
            try { _d = JsonUtility.FromJson<Data>(json); }
            catch (Exception) { _d = null; }          // corrupted save: start fresh instead of crashing
        }

        if (_d == null)
        {
            _d = new Data { best = PlayerPrefs.GetInt("best", 0) };     // carry over the score from older versions
            _dirty = true;
        }
        // collection: the starter items are always owned; anything unknown or not owned falls back to them
        if (_d.skins == null) _d.skins = new List<string>();
        _d.skins.Remove("default");                                  // id used before the Skin tab existed
        foreach (var it in Cosmetics.All)
            if (it.Default && !_d.skins.Contains(it.Id)) _d.skins.Add(it.Id);
        if (_d.catSkin == null || _d.catSkin.Length != Levels) _d.catSkin = new string[Levels];
        for (int i = 0; i < Levels; i++)
        {
            var theme = Cosmetics.FindTheme(_d.catSkin[i]);
            if (theme == null || !_d.skins.Contains(Cosmetics.CatItemOf(theme, i).Id)) _d.catSkin[i] = Cosmetics.DefaultTheme;
        }
        if (!(Cosmetics.Find(_d.bg) is BgStyle) || !_d.skins.Contains(_d.bg)) _d.bg = Cosmetics.DefaultBg;
        if (!(Cosmetics.Find(_d.frame) is FrameStyle) || !_d.skins.Contains(_d.frame)) _d.frame = Cosmetics.DefaultFrame;
        if (_d.seen == null || _d.seen.Length != Levels)
        {
            var fixedSeen = new int[Levels];
            if (_d.seen != null) Array.Copy(_d.seen, fixedSeen, Mathf.Min(_d.seen.Length, Levels));
            _d.seen = fixedSeen;
        }
    }
}
