using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Everything the game remembers: coins, best score and how many times each cat has appeared (the Cat Codex).
/// Stored as one JSON string in PlayerPrefs (IndexedDB on WebGL). Changes are written by Flush(), which the game
/// calls at game over, when leaving to the menu, when the app is paused and every few seconds while playing.
/// </summary>
public static class PlayerData
{
    const string Key = "catcatcat.save";
    const int Levels = CatDatabase.MaxLevel + 1;

    [Serializable]
    class Data
    {
        public int version = 1;
        public int coins;
        public int best;
        public int[] seen = new int[Levels];     // how many times each cat appeared (drop or merge)
        public List<string> skins = new List<string> { "default" };   // unlocked skin ids
        public string skin = "default";          // equipped skin id
        public string lang = "";                 // "en", "vi" or "" = follow the device
    }

    static Data _d;
    static bool _dirty;

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

    // ---- skins (the Skin tab will use these) ----
    public static string EquippedSkin => D.skin;
    public static bool IsSkinUnlocked(string id) => D.skins.Contains(id);

    /// <summary>Returns true if the skin was newly unlocked.</summary>
    public static bool UnlockSkin(string id)
    {
        if (string.IsNullOrEmpty(id) || D.skins.Contains(id)) return false;
        D.skins.Add(id);
        _dirty = true;
        return true;
    }

    public static bool EquipSkin(string id)
    {
        if (!D.skins.Contains(id)) return false;
        D.skin = id;
        _dirty = true;
        return true;
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
        if (!_dirty || _d == null) return;
        _dirty = false;
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(_d));
        PlayerPrefs.Save();
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
        if (_d.skins == null || !_d.skins.Contains("default")) { _d.skins = _d.skins ?? new List<string>(); _d.skins.Insert(0, "default"); }
        if (string.IsNullOrEmpty(_d.skin) || !_d.skins.Contains(_d.skin)) _d.skin = "default";
        if (_d.seen == null || _d.seen.Length != Levels)
        {
            var fixedSeen = new int[Levels];
            if (_d.seen != null) Array.Copy(_d.seen, fixedSeen, Mathf.Min(_d.seen.Length, Levels));
            _d.seen = fixedSeen;
        }
    }
}
