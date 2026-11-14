using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks the star shard economy on the real PlayerData / Cosmetics / Pull: shards only from duplicate cats, prices per
/// rarity (and Majesty), exchange with enough / too few shards, owned cats, double tap and retry, a failing step, save/load
/// (also an old save) and completing a set (11/11) by exchange. The game has no backend, so there is no network retry.
/// Menu: Tools/Suika/Check Shards. Batch: -executeMethod ShardsCheck.Run (exit code 1 on failure).
/// The real save is backed up first and restored afterwards.
/// </summary>
public static class ShardsCheck
{
    const string SaveKey = "catcatcat.save", RunKey = "catcatcat.run";

    [MenuItem("Tools/Suika/Check Shards")]
    public static void RunMenu()
    {
        if (Execute()) Debug.Log("[Shards] all checks passed");
    }

    public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

    static bool Execute()
    {
        string saveBackup = PlayerPrefs.GetString(SaveKey, null), runBackup = PlayerPrefs.GetString(RunKey, null);
        bool ok = true;
        try
        {
            Rules();
            RealPull();
            Prices();
            Exchanges();
            Transactions();
            SaveLoad();
            CompleteSet();
            SourceGuard();
            Debug.Log("SHARDS_CHECK_OK");
        }
        catch (Exception e)
        {
            ok = false;
            Debug.LogError("Shards check failed: " + e.Message);
        }
        finally
        {
            PlayerData.ResetAll();
            PlayerData.Blocked = false;
            if (saveBackup != null) PlayerPrefs.SetString(SaveKey, saveBackup);
            if (runBackup != null) PlayerPrefs.SetString(RunKey, runBackup);
            PlayerPrefs.Save();
            Reload();
        }
        return ok;
    }

    static CatItem Cat(string themeId, int level) => Cosmetics.CatItemOf(Cosmetics.FindTheme(themeId), level);

    // ------------------------------------------------------------------ who gets a shard

    static void Rules()
    {
        Fresh();
        var cat = Cat("cat_doodle", 0);

        var r = Cosmetics.Award(cat, Rarity.Common);
        Check(r.IsNew && r.Shards == 0 && r.Refund == 0 && PlayerData.Shards == 0, "a NEW cat gives no shard and no refund");

        int coins = PlayerData.Coins;
        r = Cosmetics.Award(cat, Rarity.Common);
        Check(!r.IsNew && r.Shards == 1 && PlayerData.Shards == 1, "a duplicate cat gives 1 shard");
        Check(r.Refund == Cosmetics.DuplicateCoins(Rarity.Common) && PlayerData.Coins - coins == r.Refund, "the coin refund is unchanged and still paid");

        foreach (var rarity in new[] { Rarity.Rare, Rarity.Legendary })
        {
            var c = Cat("cat_kawaii", rarity == Rarity.Rare ? 6 : 9);
            Cosmetics.Award(c, rarity);
            int shardsBefore = PlayerData.Shards;
            coins = PlayerData.Coins;
            r = Cosmetics.Award(c, rarity);
            Check(r.Shards == 1 && PlayerData.Shards == shardsBefore + 1, "a duplicate " + rarity + " cat gives 1 shard (not rarity based)");
            Check(r.Refund == Cosmetics.DuplicateCoins(rarity) && PlayerData.Coins - coins == r.Refund, rarity + " refund unchanged");
        }

        foreach (Item item in new Item[] { Cosmetics.Backgrounds[1], Cosmetics.Backgrounds[4], Cosmetics.Frames[1], Cosmetics.Frames[6] })
        {
            int before = PlayerData.Shards;
            var first = Cosmetics.Award(item, item.Rarity);
            Check(first.IsNew && first.Shards == 0, "a new " + item.Kind + " gives no shard");
            coins = PlayerData.Coins;
            var dup = Cosmetics.Award(item, item.Rarity);
            Check(!dup.IsNew && dup.Shards == 0 && PlayerData.Shards == before, "a duplicate " + item.Kind + " gives NO shard");
            Check(dup.Refund == Cosmetics.DuplicateCoins(item.Rarity) && PlayerData.Coins - coins == dup.Refund, "its coin refund is unchanged");
        }
    }

    /// <summary>The real Pull: odds, pity, price and refunds are as before; shards follow the rule on every result.</summary>
    static void RealPull()
    {
        Fresh();
        var b = Balance.Current;
        Check(b.gachaCost == 150 && b.gachaRateRare == 25 && b.gachaRateLegendary == 5 && b.gachaPity == 30, "odds, pity and pull price are unchanged");
        Check(Mathf.Approximately(b.dupRefundCommon, 0.25f) && Mathf.Approximately(b.dupRefundRare, 0.4f) && Mathf.Approximately(b.dupRefundLegendary, 0.6f), "refund rates are unchanged");

        PlayerData.AddCoins(20000000);
        int startCoins = PlayerData.Coins;
        long refunds = 0, shards = 0;
        int dupCats = 0, dupOthers = 0, news = 0, pulls = 0;
        for (int i = 0; i < 100; i++)
        {
            var results = Cosmetics.Pull(10);
            Check(results != null && results.Count == 10, "a 10-pull returns 10 results");
            pulls += 10;
            foreach (var r in results)
            {
                if (r.IsNew) { news++; Check(r.Shards == 0 && r.Refund == 0, "new item: nothing extra"); continue; }
                bool isCat = r.Item is CatItem;
                Check(r.Shards == (isCat ? 1 : 0), "duplicate " + r.Item.Kind + " gives " + (isCat ? 1 : 0) + " shard(s)");
                Check(r.Refund == Cosmetics.DuplicateCoins(r.Item.Rarity), "refund is the usual amount");
                refunds += r.Refund;
                shards += r.Shards;
                if (isCat) dupCats++; else dupOthers++;
            }
        }
        Check(PlayerData.Shards == shards, "the shard balance is exactly the shards of the results (" + shards + ")");
        Check(PlayerData.Coins == startCoins - pulls * b.gachaCost + refunds, "coins: price paid per pull, refunds received, nothing else");
        Check(dupCats > 0 && dupOthers > 0 && news > 0, "the run saw new items, duplicate cats and duplicate backgrounds/frames");
        Debug.Log("Shards real pulls: " + pulls + " items, " + news + " new, " + dupCats + " duplicate cats, " + dupOthers + " other duplicates, " + shards + " shards");
    }

    // ------------------------------------------------------------------ prices

    static void Prices()
    {
        var b = Balance.Current;
        Check(b.shardCostCommon == 20 && b.shardCostRare == 30 && b.shardCostLegendary == 40 && b.shardCostMajesty == 50, "config: 20 / 30 / 40 / 50");
        foreach (var theme in Cosmetics.Cats)
            foreach (int level in new[] { 0, 5, 6, 8, 9, 10 })
            {
                var cat = Cosmetics.CatItemOf(theme, level);
                int expected = level == 10 ? 50 : level >= 9 ? 40 : level >= 6 ? 30 : 20;
                Check(Cosmetics.ShardPrice(cat) == expected, theme.Id + " level " + level + " costs " + expected);
            }
        Check(Cosmetics.ShardPrice(Cat("cat_doodle", 0)) == 20 && Cosmetics.ShardPrice(Cat("cat_pixel", 7)) == 30 &&
              Cosmetics.ShardPrice(Cat("cat_gold", 9)) == 40 && Cosmetics.ShardPrice(Cat("cat_candy", 10)) == 50, "Common / Rare / Legendary / Majesty");
    }

    // ------------------------------------------------------------------ exchange

    static void Exchanges()
    {
        // every rarity, enough shards exactly: any theme, including Legendary and Majesty
        foreach (var (theme, level, price) in new[] { ("cat_doodle", 0, 20), ("cat_pixel", 7, 30), ("cat_chibi", 9, 40), ("cat_candy", 10, 50), ("cat_gold", 10, 50) })
        {
            Fresh();
            var cat = Cat(theme, level);
            Check(!PlayerData.IsOwned(cat.Id), "starts unowned");
            PlayerData.AddShards(price);
            Check(Cosmetics.Exchange(cat) == ExchangeResult.Success, theme + " level " + level + " exchanges for " + price);
            Check(PlayerData.IsOwned(cat.Id) && PlayerData.Shards == 0, "owned, exactly " + price + " spent");
            Check(Cosmetics.Exchange(cat) == ExchangeResult.AlreadyOwned && PlayerData.Shards == 0, "again: already owned, nothing spent");
        }

        // one shard short: refused, nothing changes; after one more shard (a retry) it works once
        Fresh();
        var target = Cat("cat_kawaii", 6);
        PlayerData.AddShards(29);
        Check(Cosmetics.Exchange(target) == ExchangeResult.NotEnoughShards, "29 shards for a 30 cat: refused");
        Check(!PlayerData.IsOwned(target.Id) && PlayerData.Shards == 29, "refused: no cat, no charge");
        PlayerData.AddShards(1);
        Check(Cosmetics.Exchange(target) == ExchangeResult.Success && PlayerData.Shards == 0, "the retry with 30 shards works");

        // zero shards
        Fresh();
        Check(Cosmetics.Exchange(Cat("cat_doodle", 0)) == ExchangeResult.NotEnoughShards && PlayerData.Shards == 0, "no shards: refused");

        // already owned (from the gacha) with plenty of shards: never charged
        Fresh();
        var owned = Cat("cat_pixel", 3);
        PlayerData.Unlock(owned.Id);
        PlayerData.AddShards(100);
        Check(Cosmetics.Exchange(owned) == ExchangeResult.AlreadyOwned && PlayerData.Shards == 100, "an owned cat cannot be exchanged");
        Check(Cosmetics.Exchange(Cat("cat_classic", 4)) == ExchangeResult.AlreadyOwned && PlayerData.Shards == 100, "a starter (Classic) cat is always owned");

        // double tap: many calls in a row, one cat, one payment
        Fresh();
        var tapped = Cat("cat_midnight", 8);
        PlayerData.AddShards(100);
        int success = 0, already = 0;
        for (int i = 0; i < 10; i++)
        {
            var r = Cosmetics.Exchange(tapped);
            if (r == ExchangeResult.Success) success++; else if (r == ExchangeResult.AlreadyOwned) already++;
        }
        Check(success == 1 && already == 9 && PlayerData.Shards == 70, "10 taps: one exchange, shards 100 -> 70");
        var other = Cat("cat_midnight", 2);
        Check(Cosmetics.Exchange(other) == ExchangeResult.Success && PlayerData.Shards == 50, "a different cat is a separate exchange");
    }

    // ------------------------------------------------------------------ transaction safety

    static void Transactions()
    {
        // the ownership step fails without granting anything (throws): shards come back
        Fresh();
        var cat = Cat("cat_doodle", 2);
        PlayerData.AddShards(25);
        var r = Cosmetics.Exchange(cat, id => throw new InvalidOperationException("storage failed"));
        Check(r == ExchangeResult.Failed && !PlayerData.IsOwned(cat.Id) && PlayerData.Shards == 25, "failed grant: no cat, shards refunded");

        // ... or reports false without granting
        r = Cosmetics.Exchange(cat, id => false);
        Check(r == ExchangeResult.Failed && !PlayerData.IsOwned(cat.Id) && PlayerData.Shards == 25, "grant returned false: shards refunded");

        // the retry then works, once
        Check(Cosmetics.Exchange(cat) == ExchangeResult.Success && PlayerData.Shards == 5 && PlayerData.IsOwned(cat.Id), "retry after a failure: one payment, one cat");

        // the cat is granted but something after it throws: the cat and the payment both stand
        PlayerData.AddShards(15);
        var cat2 = Cat("cat_doodle", 3);
        r = Cosmetics.Exchange(cat2, id => { PlayerData.Unlock(id); throw new InvalidOperationException("listener failed"); });
        Check(r == ExchangeResult.Success && PlayerData.IsOwned(cat2.Id) && PlayerData.Shards == 0, "granted then threw: owned and paid, no refund, no double grant");

        // a re-entrant second exchange (from inside the grant) is refused: one payment
        Fresh();
        var cat3 = Cat("cat_pixel", 1);
        PlayerData.AddShards(100);
        ExchangeResult inner = ExchangeResult.Success;
        r = Cosmetics.Exchange(cat3, id => { inner = Cosmetics.Exchange(cat3); return PlayerData.Unlock(id); });
        Check(r == ExchangeResult.Success && inner == ExchangeResult.Failed && PlayerData.Shards == 80, "re-entrant call refused: charged once (100 -> 80)");

        // a listener that throws when the set completes does not break the exchange
        Fresh();
        for (int l = 0; l < 10; l++) PlayerData.Unlock(Cat("cat_doodle", l).Id);
        PlayerData.AddShards(50);
        Action<ThemeSkillDef> bad = s => throw new InvalidOperationException("a UI listener failed");
        SetCompletion.SkillUnlocked += bad;
        try
        {
            r = Cosmetics.Exchange(Cat("cat_doodle", 10));
        }
        finally { SetCompletion.SkillUnlocked -= bad; }
        Check(r == ExchangeResult.Success && PlayerData.Shards == 0 && PlayerData.IsOwned(Cat("cat_doodle", 10).Id), "a failing unlock listener: still exchanged, paid once");
        Check(SetCompletion.IsSkillUnlocked(ThemeSkills.Eraser), "and the skill unlock was recorded");
    }

    // ------------------------------------------------------------------ save / load

    static void SaveLoad()
    {
        Fresh();
        PlayerData.AddShards(7);
        var cat = Cat("cat_kawaii", 1);
        PlayerData.AddShards(13);
        Check(Cosmetics.Exchange(cat) == ExchangeResult.Success, "exchange before saving");     // 20 - 20 = 0
        PlayerData.AddShards(4);
        PlayerData.Flush();
        Reload();
        Check(PlayerData.Shards == 4 && PlayerData.IsOwned(cat.Id), "after a restart: the shard balance and the exchanged cat are still there");
        Check(Cosmetics.Exchange(cat) == ExchangeResult.AlreadyOwned && PlayerData.Shards == 4, "and it cannot be bought again");

        // an old save: no shard field at all; ownership and coins are kept
        Fresh();
        string ids = "\"" + Cat("cat_doodle", 0).Id + "\",\"" + Cat("cat_candy", 3).Id + "\"";
        PlayerPrefs.SetString(SaveKey, "{\"version\":1,\"coins\":123,\"best\":45,\"skins\":[" + ids + "]}");
        Reload();
        Check(PlayerData.Shards == 0 && PlayerData.Coins == 123 && PlayerData.Best == 45, "old save: 0 shards, coins and best kept");
        Check(PlayerData.IsOwned(Cat("cat_doodle", 0).Id) && PlayerData.IsOwned(Cat("cat_candy", 3).Id), "old save: ownership kept");
        PlayerData.AddShards(20);
        PlayerData.Flush();
        Reload();
        Check(PlayerData.Shards == 20 && PlayerData.Coins == 123, "the old save upgrades cleanly");

        // a damaged negative value is not a debt
        Fresh();
        PlayerPrefs.SetString(SaveKey, "{\"version\":1,\"shards\":-50}");
        Reload();
        Check(PlayerData.Shards == 0, "a negative saved balance loads as 0");
    }

    // ------------------------------------------------------------------ completing a set by exchange

    static void CompleteSet()
    {
        foreach (int lastLevel in new[] { 10, 0 })
        {
            Fresh();
            var unlocked = new List<string>();
            Action<ThemeSkillDef> onUnlock = s => unlocked.Add(s.Id);
            SetCompletion.SkillUnlocked += onUnlock;
            try
            {
                var theme = Cosmetics.FindTheme("cat_doodle");
                for (int l = 0; l <= 10; l++) if (l != lastLevel) PlayerData.Unlock(Cosmetics.CatItemOf(theme, l).Id);
                Check(SetCompletion.Progress(theme).Owned == 10 && !SetCompletion.IsSkillUnlocked(ThemeSkills.Eraser) && unlocked.Count == 0, "10/11: locked");
                var last = Cosmetics.CatItemOf(theme, lastLevel);
                int price = Cosmetics.ShardPrice(last);
                PlayerData.AddShards(price - 1);
                Check(Cosmetics.Exchange(last) == ExchangeResult.NotEnoughShards && SetCompletion.Progress(theme).Owned == 10 && unlocked.Count == 0, "one shard short: still 10/11");
                PlayerData.AddShards(1);
                Check(Cosmetics.Exchange(last) == ExchangeResult.Success, "the 11th cat by exchange (price " + price + ")");
                Check(SetCompletion.Progress(theme).Complete && SetCompletion.IsSkillUnlocked(ThemeSkills.Eraser), "11/11 unlocks the skill");
                Check(unlocked.Count == 1 && unlocked[0] == ThemeSkills.EraserId, "the unlock event fires once");
                Check(Cosmetics.Exchange(last) == ExchangeResult.AlreadyOwned && unlocked.Count == 1, "a second tap: no second unlock");
                PlayerData.Flush();
                Reload();
                Check(SetCompletion.IsSkillUnlocked(ThemeSkills.Eraser) && SetCompletion.Progress(theme).Owned == 11 && unlocked.Count == 1, "after a restart: still unlocked, nothing announced again");
            }
            finally { SetCompletion.SkillUnlocked -= onUnlock; }
        }
    }

    /// <summary>Pull still rolls as before; shards are paid in one place; the exchange goes through PlayerData.Unlock.</summary>
    static void SourceGuard()
    {
        string cos = File.ReadAllText(Application.dataPath + "/Scripts/Data/Cosmetics.cs");
        var pull = Regex.Match(cos, @"public static List<PullResult> Pull\(int count\)\s*\{(?<body>.*?)\n    \}", RegexOptions.Singleline).Groups["body"].Value;
        Check(pull.Contains("RollRarity(b)") && pull.Contains("PlayerData.Pity++") && pull.Contains("b.gachaPity") && pull.Contains("TrySpend(PullCost(count))"), "Pull still rolls, counts pity and charges as before");
        Check(!pull.Contains("AddShards"), "Pull itself does not pay shards (Award does)");
        Check(Regex.Matches(cos, @"AddShards\(").Count == 2, "AddShards: the duplicate-cat award and the exchange refund");
        Check(Regex.IsMatch(cos, @"if \(item is CatItem\)\s*\{\s*shards = ThemeSkills\.ShardRewardPerDuplicateCat;"), "shards only for a duplicate CatItem");
        Check(cos.Contains("(unlock ?? PlayerData.Unlock)(cat.Id)"), "the exchange grants through PlayerData.Unlock (the Phase 2 completion hook)");
    }

    static void Fresh()
    {
        PlayerData.ResetAll();
        PlayerData.Blocked = false;
        Reload();
    }

    static void Reload()
    {
        typeof(PlayerData).GetField("_d", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
        typeof(PlayerData).GetField("_runLoaded", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
        Cosmetics.Reload();
    }

    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }
}
