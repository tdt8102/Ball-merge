using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Android APK build (debug-signed, installable by sideloading). Menu: Tools/Suika/Build Android APK.
/// Batch: -executeMethod AndroidBuilder.BuildBatch -buildTarget Android. Output: Builds/Android/CatCatCat.apk (git-ignored).
/// IL2CPP with ARMv7 + ARM64 so it runs on old and new phones; portrait only.
/// </summary>
public static class AndroidBuilder
{
    const string OutFile = "Builds/Android/CatCatCat.apk";

    [MenuItem("Tools/Suika/Build Android APK")]
    public static void BuildMenu()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        if (Build()) EditorUtility.RevealInFinder(OutFile);
    }

    public static void BuildBatch() => EditorApplication.Exit(Build() ? 0 : 1);

    static bool Build()
    {
        PlayerSettings.productName = "CatCatCat";
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Medium);
        PlayerSettings.Android.useCustomKeystore = false;           // the standard debug keystore: fine for installing on your own phone
        EditorUserBuildSettings.buildAppBundle = false;             // APK, not AAB
        EditorUserBuildSettings.androidBuildType = AndroidBuildType.Release;

        Directory.CreateDirectory(Path.GetDirectoryName(OutFile));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Menu.unity", "Assets/Scenes/Main.unity" },
            locationPathName = OutFile,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        Debug.Log("[Android] " + report.summary.result + " size=" + report.summary.totalSize / 1024 + "KB -> " + OutFile);
        return report.summary.result == BuildResult.Succeeded;
    }
}
