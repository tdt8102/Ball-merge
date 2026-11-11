using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tester's cheat menu, opened by tapping the "Settings" title five times. A number pad asks for the password,
/// then three buttons add coins (to try out the Gacha and the skins). Works in the menu and in the game.
/// </summary>
public class CheatPanel : MonoBehaviour
{
    const string Password = "2609";
    static readonly Color Brown = new Color(0.55f, 0.36f, 0.24f);
    static readonly Color Cream = new Color(1f, 0.97f, 0.9f);
    static readonly Color Ink = new Color(0.36f, 0.22f, 0.15f);
    static readonly Color Green = new Color(0.2f, 0.65f, 0.4f);
    static readonly Color Red = new Color(0.85f, 0.25f, 0.2f);
    static readonly Vector2 C = new Vector2(0.5f, 0.5f);

    /// <summary>Raised after coins were added, so a coin counter on screen can update at once.</summary>
    public static event Action CoinsChanged;

    GameObject _passGroup, _menuGroup;
    Button _resetBtn;
    Text _resetLabel;
    float _confirmT = -1f;
    Text _display, _prompt, _coins, _info;
    string _input = "";
    float _wrongT = -1f;

    public static CheatPanel Create(RectTransform parent)
    {
        var go = new GameObject("CheatPanel", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);              // blocks taps behind it
        go.AddComponent<Button>().transition = Selectable.Transition.None;      // ... and keeps them from reaching the settings popup's dim background
        var p = go.AddComponent<CheatPanel>();
        p.Build(rt);
        go.SetActive(false);
        return p;
    }

    public void Open()
    {
        _input = "";
        _wrongT = -1f;
        ShowPassword();
        gameObject.SetActive(true);
    }

    public void Close() => gameObject.SetActive(false);

    void Build(RectTransform rt)
    {
        const float w = 640f, h = 960f;
        GameUI.RoundImg(rt, "CardBorder", Brown, C, Vector2.zero, new Vector2(w + 10, h + 10), 44f).raycastTarget = true;
        var card = GameUI.RoundImg(rt, "Card", Cream, C, Vector2.zero, new Vector2(w, h), 40f);
        card.raycastTarget = true;
        card.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

        var title = GameUI.Label(rt, "Title", "", 56, TextAnchor.MiddleCenter, C, new Vector2(0, h * 0.5f - 70), new Vector2(500, 80));
        title.color = Red;
        Loc.Bind(title, () => Loc.T("cheat"));
        var close = GameUI.Button(rt, "Close", "X", 40, C, new Vector2(w * 0.5f - 62, h * 0.5f - 62), new Vector2(64, 64), Brown);
        close.onClick.AddListener(() => { AudioManager.Instance.Click(); Close(); });

        // ---- password: number pad
        _passGroup = Group(rt, "Password");
        var pg = (RectTransform)_passGroup.transform;
        _prompt = GameUI.Label(pg, "Prompt", "", 38, TextAnchor.MiddleCenter, C, new Vector2(0, 285), new Vector2(580, 56));
        _prompt.color = Ink;
        _display = GameUI.Label(pg, "Display", "", 90, TextAnchor.MiddleCenter, C, new Vector2(0, 190), new Vector2(580, 120));
        _display.color = Ink;
        string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "<", "0", "C" };
        for (int i = 0; i < keys.Length; i++)
        {
            string k = keys[i];
            var pos = new Vector2((i % 3 - 1) * 190f, 60f - (i / 3) * 125f);
            var b = GameUI.Button(pg, "Key" + k, k, 56, C, pos, new Vector2(170, 105), k == "<" || k == "C" ? Brown : new Color(0.7f, 0.5f, 0.36f));
            b.onClick.AddListener(() => OnKey(k));
        }

        // ---- cheat menu
        _menuGroup = Group(rt, "Menu");
        var mg = (RectTransform)_menuGroup.transform;
        _coins = GameUI.Label(mg, "Coins", "", 56, TextAnchor.MiddleCenter, C, new Vector2(0, 310), new Vector2(580, 80));
        _coins.color = Ink;
        _info = GameUI.Label(mg, "Info", "", 34, TextAnchor.MiddleCenter, C, new Vector2(0, 240), new Vector2(580, 50));
        _info.color = Green;
        int[] amounts = { 1000, 10000, 100000 };
        for (int i = 0; i < amounts.Length; i++)
        {
            int amount = amounts[i];
            var b = GameUI.Button(mg, "Add" + amount, "+" + amount.ToString("N0"), 52, C, new Vector2(0, 120 - i * 110f), new Vector2(480, 96), Green);
            b.onClick.AddListener(() => AddCoins(amount));
        }
        _resetBtn = GameUI.Button(mg, "Reset", "", 34, C, new Vector2(0, -235), new Vector2(540, 96), Red);
        _resetLabel = _resetBtn.GetComponentInChildren<Text>();
        _resetBtn.onClick.AddListener(OnReset);
        var done = GameUI.Button(mg, "Done", "OK", 50, C, new Vector2(0, -355), new Vector2(380, 100), Brown);
        done.onClick.AddListener(() => { AudioManager.Instance.Click(); Close(); });
    }

    static GameObject Group(RectTransform parent, string name)
    {
        var g = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)g.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        return g;
    }

    void ShowPassword()
    {
        _passGroup.SetActive(true);
        _menuGroup.SetActive(false);
        _prompt.text = Loc.T("enter_pass");
        _prompt.color = Ink;
        RefreshDisplay();
    }

    void ShowMenu()
    {
        _confirmT = -1f;
        _resetLabel.text = Loc.T("cheat_reset");
        _passGroup.SetActive(false);
        _menuGroup.SetActive(true);
        _info.text = "";
        _coins.text = Loc.T("coins") + ":  " + PlayerData.Coins;
    }

    void RefreshDisplay()
    {
        var s = "";
        for (int i = 0; i < Password.Length; i++) s += (i < _input.Length ? "*" : "-") + (i < Password.Length - 1 ? " " : "");
        _display.text = s;
    }

    void OnKey(string k)
    {
        AudioManager.Instance.Click();
        if (k == "<") { if (_input.Length > 0) _input = _input.Substring(0, _input.Length - 1); }
        else if (k == "C") _input = "";
        else if (_input.Length < Password.Length) _input += k;
        RefreshDisplay();
        if (_input.Length == Password.Length) Check();
    }

    void Check()
    {
        if (_input == Password) { Haptics.Medium(); ShowMenu(); return; }
        Haptics.Heavy();
        _input = "";
        RefreshDisplay();
        _prompt.text = Loc.T("wrong_pass");
        _prompt.color = Red;
        _wrongT = 0f;
    }

    /// <summary>First tap asks to confirm; a second tap within a few seconds erases all player data and returns to the menu.</summary>
    void OnReset()
    {
        AudioManager.Instance.Click();
        if (_confirmT < 0f)
        {
            _confirmT = 0f;
            _resetLabel.text = Loc.T("cheat_sure");
            Haptics.Medium();
            return;
        }
        PlayerData.ResetAll();
        Haptics.Heavy();
        Time.timeScale = 1f;                                        // the in-game settings popup pauses the game
        CoinsChanged?.Invoke();
        UnityEngine.SceneManagement.SceneManager.LoadScene(MenuController.MenuScene);   // reload the menu so every screen starts from the fresh profile
    }

    void AddCoins(int amount)
    {
        PlayerData.AddCoins(amount);
        PlayerData.Flush();
        AudioManager.Instance.Discover();
        Haptics.Light();
        _coins.text = Loc.T("coins") + ":  " + PlayerData.Coins;
        _info.text = Loc.T("coins_added") + "  +" + amount.ToString("N0");
        CoinsChanged?.Invoke();
    }

    void Update()
    {
        // a computer keyboard works too (handy in the Editor)
        if (_passGroup.activeSelf)
            foreach (char ch in Input.inputString)
                if (ch >= '0' && ch <= '9') OnKey(ch.ToString());
                else if (ch == '\b') OnKey("<");

        if (_confirmT >= 0f)
        {
            _confirmT += Time.unscaledDeltaTime;
            if (_confirmT > 3f) { _confirmT = -1f; _resetLabel.text = Loc.T("cheat_reset"); }
        }

        if (_wrongT >= 0f)
        {
            _wrongT += Time.unscaledDeltaTime;
            if (_wrongT > 1.5f) { _wrongT = -1f; if (_passGroup.activeSelf) { _prompt.text = Loc.T("enter_pass"); _prompt.color = Ink; } }
        }
    }
}
