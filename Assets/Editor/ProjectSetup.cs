using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>One-click setup: portrait player settings + Main scene. Also runnable from batch mode.</summary>
public static class ProjectSetup
{
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string MenuPath = "Assets/Scenes/Menu.unity";

    [MenuItem("Tools/Suika/Setup Project and Create Scene")]
    public static void Run()
    {
        // --- portrait player settings ---
        PlayerSettings.companyName = "Proto";
        PlayerSettings.productName = "CatCatCat";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.proto.suikamerge");
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.proto.suikamerge");
        // Standalone test builds: small portrait window
        PlayerSettings.defaultScreenWidth = 540;
        PlayerSettings.defaultScreenHeight = 960;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;

        // --- scenes: Menu (title + loading) -> Main (gameplay) ---
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        CreateScene(MenuPath, "Menu", typeof(MenuController));
        CreateScene(ScenePath, "GameManager", typeof(GameManager));
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MenuPath, true), new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[Suika] Setup done. Scenes: " + MenuPath + ", " + ScenePath);
    }

    static void CreateScene(string path, string rootName, System.Type component)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.98f, 0.93f, 0.84f);
        camGo.transform.position = new Vector3(0, 0, -10);

        new GameObject(rootName).AddComponent(component);
        EditorSceneManager.SaveScene(scene, path);
    }
}
