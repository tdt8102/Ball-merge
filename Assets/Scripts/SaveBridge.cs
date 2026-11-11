using UnityEngine;

/// <summary>
/// Survives scene changes and saves when the app is paused, loses focus or the page is hidden (the web template calls
/// PageHidden from visibilitychange / pagehide, which is the last reliable moment on a phone browser).
/// </summary>
public class SaveBridge : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        var go = new GameObject("SaveBridge");
        DontDestroyOnLoad(go);
        go.AddComponent<SaveBridge>();
    }

    /// <summary>Called from the page via unityInstance.SendMessage("SaveBridge", "PageHidden").</summary>
    public void PageHidden() => Save();

    void OnApplicationPause(bool paused) { if (paused) Save(); }
    void OnApplicationFocus(bool focused) { if (!focused) Save(); }

    static void Save()
    {
        if (GameManager.Instance != null) GameManager.Instance.SaveProgress();   // also flushes
        else PlayerData.Flush();
    }
}
