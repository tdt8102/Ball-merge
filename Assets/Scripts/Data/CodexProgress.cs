/// <summary>
/// How much of each theme the Cat Codex counts as found. The first theme (Classic) is about discovering the cats while
/// playing; every other theme is about collecting them. Shared by the Codex pages and the Home button, so both agree.
/// </summary>
public static class CodexProgress
{
    public const int Total = CatDatabase.MaxLevel + 1;

    /// <summary>Is this cat of the theme "found"? Classic: it has appeared in a game. Others: the player owns it.</summary>
    public static bool Found(CatStyle theme, int level) =>
        theme.Default ? PlayerData.IsDiscovered(level) : PlayerData.IsOwned(Cosmetics.CatItemOf(theme, level).Id);

    public static int Have(CatStyle theme)
    {
        int n = 0;
        for (int level = 0; level < Total; level++) if (Found(theme, level)) n++;
        return n;
    }

    public static bool Complete(CatStyle theme) => Have(theme) >= Total;

    /// <summary>Themes (skins) whose page is complete.</summary>
    public static int CompleteThemes()
    {
        int n = 0;
        foreach (var theme in Cosmetics.Cats) if (Complete(theme)) n++;
        return n;
    }
}
