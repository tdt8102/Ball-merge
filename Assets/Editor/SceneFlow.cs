using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Keeps Build Settings = [Menu, Main] and makes the Play button always start from the Menu scene,
/// so the editor behaves like the real game (title -> loading -> gameplay) whatever scene is open.
/// Turn it off with Tools > Suika > Play From Menu Scene to test Main directly.
/// </summary>
[InitializeOnLoad]
public static class SceneFlow
{
    const string MenuPath = "Assets/Scenes/Menu.unity";
    const string MainPath = "Assets/Scenes/Main.unity";
    const string Pref = "Suika.PlayFromMenu";
    const string MenuItemPath = "Tools/Suika/Play From Menu Scene";

    static bool _retried;

    static SceneFlow() => EditorApplication.delayCall += Apply;

    static void Apply()
    {
        if (File.Exists(MenuPath) && File.Exists(MainPath))
        {
            var want = new[] { new EditorBuildSettingsScene(MenuPath, true), new EditorBuildSettingsScene(MainPath, true) };
            var have = EditorBuildSettings.scenes;
            if (have.Length != 2 || have[0].path != MenuPath || have[1].path != MainPath || !have.All(s => s.enabled))
                EditorBuildSettings.scenes = want;
        }

        bool on = EditorPrefs.GetBool(Pref, true);
        var menu = on && File.Exists(MenuPath) ? AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath) : null;
        EditorSceneManager.playModeStartScene = menu;
        if (on && menu == null && File.Exists(MenuPath) && !_retried)   // asset not imported yet: try once more
        {
            _retried = true;
            EditorApplication.delayCall += Apply;
        }
    }

    [MenuItem(MenuItemPath)]
    static void Toggle()
    {
        EditorPrefs.SetBool(Pref, !EditorPrefs.GetBool(Pref, true));
        Apply();
    }

    [MenuItem(MenuItemPath, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuItemPath, EditorPrefs.GetBool(Pref, true));
        return true;
    }
}
