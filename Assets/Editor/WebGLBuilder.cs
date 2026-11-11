using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>WebGL build. Menu: Tools/Suika/Build WebGL. Batch: -executeMethod WebGLBuilder.BuildBatch -buildTarget WebGL. Output: WebBuild/</summary>
public static class WebGLBuilder
{
    // Committed to git so Vercel / any static host can serve it directly (no Unity on the server).
    const string OutDir = "WebBuild";

    [MenuItem("Tools/Suika/Build WebGL")]
    public static void BuildMenu()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        var ok = Build();
        if (ok) EditorUtility.RevealInFinder(OutDir);
    }

    public static void BuildBatch() => EditorApplication.Exit(Build() ? 0 : 1);

    static bool Build()
    {
        PlayerSettings.productName = "CatCatCat";
        PlayerSettings.WebGL.template = "PROJECT:Portrait";
        // No compression so it can be hosted on GitHub Pages / any static host as-is.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        // NOT None: with None any C# exception (even a harmless one) calls abort() and the whole game dies.
        // This way an unexpected exception is logged and the game keeps running.
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.nameFilesAsHashes = true;

        // hashed file names: wipe old output so stale builds do not pile up in git
        if (System.IO.Directory.Exists(OutDir)) System.IO.Directory.Delete(OutDir, true);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Menu.unity", "Assets/Scenes/Main.unity" },
            locationPathName = OutDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        Debug.Log("[WebGL] " + report.summary.result + " size=" + report.summary.totalSize / 1024 + "KB -> " + OutDir);
        return report.summary.result == BuildResult.Succeeded;
    }
}
