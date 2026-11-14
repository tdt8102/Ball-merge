using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Phase 15: end-to-end QA of the Theme Set Skills in REAL Play Mode (the real Menu/Main scenes, GameManager, physics, save/resume).
/// Batch: Unity -batchmode -nographics -executeMethod E2eCheck.Run  (no -quit; the runner exits Unity when done).
/// Writes a Markdown table (Test | Expected | Actual | Pass/Fail | Note) to %TEMP%/suika-e2e-report.md and logs every row as "E2E|...".
/// The real save is backed up first and restored afterwards. Legacy Input cannot be injected, so pointer taps are covered by
/// calling the same SkillRuntime/GameManager entry points the input code calls (noted in the rows).
/// </summary>
[InitializeOnLoad]
public static class E2eCheck
{
    const string Flag = "suika.e2e", SaveBak = "suika.e2e.save", RunBak = "suika.e2e.run";

    static E2eCheck() => EditorApplication.playModeStateChanged += OnState;

    public static void Run()
    {
        SessionState.SetString(SaveBak, PlayerPrefs.GetString("catcatcat.save", "\u0001none"));
        SessionState.SetString(RunBak, PlayerPrefs.GetString("catcatcat.run", "\u0001none"));
        SessionState.SetBool(Flag, true);
        EditorApplication.EnterPlaymode();
    }

    static void OnState(PlayModeStateChange s)
    {
        if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Flag, false))
        {
            SessionState.SetBool(Flag, false);
            new GameObject("E2E").AddComponent<E2eRunner>();
        }
    }

    public static void RestorePrefs()
    {
        Restore("catcatcat.save", SessionState.GetString(SaveBak, "\u0001none"));
        Restore("catcatcat.run", SessionState.GetString(RunBak, "\u0001none"));
        PlayerPrefs.Save();
    }

    static void Restore(string key, string value)
    {
        if (value == "\u0001none") PlayerPrefs.DeleteKey(key); else PlayerPrefs.SetString(key, value);
    }
}

public partial class E2eRunner : MonoBehaviour
{
    sealed class Row { public string Id, Test, Expected, Actual, Note; public bool Pass, Known; }

    readonly List<Row> _rows = new List<Row>();
    readonly List<string> _errors = new List<string>();
    const BindingFlags NP = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

    GameManager GM => GameManager.Instance;
    List<Cat> Cats => Get<List<Cat>>(GM, "_cats");
    DangerTimer Danger => Get<DangerTimer>(GM, "_danger");
    GameUI UI => FindObjectOfType<GameUI>();

    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceived += OnLog;
        var started = DateTime.Now;
        yield return Safe("G (menu gacha tap)", GachaTap());
        yield return Safe("A (flow)", Flow());
        yield return Safe("B (skills)", Skills());
        yield return Safe("C (pause)", PauseTests());
        yield return Safe("D (save/resume)", SaveResume());
        yield return Safe("E (game over)", GameOverTests());
        yield return Safe("F (old saves)", OldSaves());
        yield return Safe("H (economy/debug)", EconomyAndDebug());
        Application.logMessageReceived -= OnLog;
        R("Z1", "No errors/exceptions logged during the whole session", "0", _errors.Count, _errors.Count == 0, string.Join(" || ", _errors.Take(3)));
        Finish((DateTime.Now - started).TotalSeconds);
    }

    // ------------------------------------------------------------------ plumbing

    void OnLog(string condition, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) _errors.Add(condition);
    }

    /// <summary>Runs a section; an exception becomes a failed row instead of killing the run. Nested coroutines are driven here.</summary>
    IEnumerator Safe(string name, IEnumerator body)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(body);
        while (stack.Count > 0)
        {
            object cur = null;
            bool more;
            try
            {
                more = stack.Peek().MoveNext();
                if (more) cur = stack.Peek().Current;
            }
            catch (Exception e)
            {
                R("X-" + name, "section " + name + " runs to the end", "no exception", e.GetType().Name + ": " + e.Message, false, e.StackTrace.Split('\n')[0]);
                Time.timeScale = 1f;
                yield break;
            }
            if (!more) { stack.Pop(); continue; }
            if (cur is IEnumerator inner) { stack.Push(inner); continue; }
            yield return cur;
        }
    }

    void R(string id, string test, object expected, object actual, bool pass, string note = "", bool known = false)
    {
        _rows.Add(new Row { Id = id, Test = test, Expected = Str(expected), Actual = Str(actual), Pass = pass, Note = note, Known = known });
        Debug.Log("E2E|" + id + "|" + (pass ? "PASS" : "FAIL") + "|" + test + "|" + Str(expected) + "|" + Str(actual) + "|" + note);
    }

    /// <summary>Row whose pass condition is "actual text equals expected text".</summary>
    void Is(string id, string test, object expected, object actual, string note = "") => R(id, test, expected, actual, Str(expected) == Str(actual), note);

    static string Str(object o) => o is float f ? f.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : (o ?? "").ToString();

    void Finish(double seconds)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| ID | Test | Expected | Actual | Pass/Fail | Bug / note |");
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (var r in _rows)
            sb.AppendLine("| " + r.Id + " | " + Cell(r.Test) + " | " + Cell(r.Expected) + " | " + Cell(r.Actual) + " | " + (r.Pass ? "Pass" : "**Fail**") + " | " + Cell(r.Note) + " |");
        int fail = _rows.Count(r => !r.Pass && !r.Known), known = _rows.Count(r => !r.Pass && r.Known);
        sb.AppendLine().AppendLine(_rows.Count + " rows, " + _rows.Count(r => r.Pass) + " pass, " + fail + " fail, " + known + " known/pending decision, " + seconds.ToString("0") + " s");
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "suika-e2e-report.md"), sb.ToString());
        Debug.Log("E2E_DONE rows=" + _rows.Count + " fail=" + fail + " known=" + known);
        Time.timeScale = 1f;
        E2eCheck.RestorePrefs();
        EditorApplication.Exit(fail == 0 ? 0 : 1);
    }

    static string Cell(string s) => (s ?? "").Replace("|", "/").Replace("\n", " ");

    // ------------------------------------------------------------------ helpers shared by the sections

    static T Get<T>(object o, string name) => (T)o.GetType().GetField(name, NP).GetValue(o);
    static object Call(object o, string name, params object[] a) => o.GetType().GetMethod(name, NP).Invoke(o, a);

    static void Fresh()
    {
        PlayerData.ResetAll();
        PlayerData.Blocked = false;
        Loc.SetLanguage("en");
    }

    /// <summary>Forget the in-memory profile and read PlayerPrefs again, like a restart of the app.</summary>
    static void Reread()
    {
        typeof(PlayerData).GetField("_d", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
        typeof(PlayerData).GetField("_runLoaded", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
        Cosmetics.Reload();
    }

    static CatStyle Theme(string id) => Cosmetics.FindTheme(id);
    static CatItem CatOf(string theme, int level) => Cosmetics.CatItemOf(Theme(theme), level);

    /// <summary>Own cats the way the Gacha grants them (Cosmetics.Award).</summary>
    static void Award(string theme, int count, int except = -1)
    {
        for (int l = 0; l < count; l++) if (l != except) Cosmetics.Award(CatOf(theme, l), CatOf(theme, l).Rarity);
    }

    static void Outfit(string theme) => Cosmetics.EquipTheme(Theme(theme));

    IEnumerator LoadMain(bool resume)
    {
        PlayerData.ResumeRequested = resume;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main");
        yield return null;
        yield return null;
        float t = 0f;
        while ((GM == null || GM.Skills == null) && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    /// <summary>A fresh run with this theme worn on all 11 cats (themeId null = Classic). Energy 100 when full is true.</summary>
    IEnumerator Run(string themeId, bool full = true)
    {
        Fresh();
        if (themeId != null) { Award(themeId, 11); Outfit(themeId); }
        GM.NewGame();
        yield return Frames(2);
        if (full) FillEnergy();
    }

    void FillEnergy() { while (GM.Energy.Value < 100) GM.Energy.OnMerge(MergeSource.Normal); }

    // board helpers
    Cat MakeCat(int level, float x, float y)
    {
        var c = (Cat)Call(GM, "NewCat", level, new Vector2(x, y), false);
        Cats.Add(c);
        return c;
    }

    void ClearBoard()
    {
        foreach (var c in Cats.ToArray())
            if (c != null && c.Dropped) { Cats.Remove(c); Destroy(c.gameObject); }
    }

    List<Cat> Board() => Cats.Where(c => c != null && c.Dropped).ToList();

    /// <summary>Drop two touching cats of one level: a real collision merge. Returns when the pair is gone (or after 3 s).</summary>
    IEnumerator DoMerge(int level, float x)
    {
        var a = MakeCat(level, x - 0.12f, -4.4f);
        var b = MakeCat(level, x + 0.12f, -4.4f);
        float t = 0f;
        while ((a != null || b != null) && t < 3f) { t += Time.deltaTime; yield return null; }
        yield return Frames(2);
    }

    float Dist(Cat a, Cat b) => Vector2.Distance(a.transform.position, b.transform.position);

    UnityEngine.UI.Text BadgeText() => UI.transform.Find("SkillButton/Body/Badge/Text").GetComponent<UnityEngine.UI.Text>();
}
