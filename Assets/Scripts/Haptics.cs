using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Vibration feedback. Android: Vibrator via JNI (custom duration). iOS: Handheld.Vibrate (single strength,
/// so only medium/heavy pulses). WebGL: navigator.vibrate via Plugins/WebGL/Haptics.jslib (Android browsers).
/// </summary>
public static class Haptics
{
    static bool _loaded, _enabled = true;

    public static bool Enabled
    {
        get { Load(); return _enabled; }
        set { _enabled = value; _loaded = true; PlayerPrefs.SetInt("haptics", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    static void Load()
    {
        if (_loaded) return;
        _loaded = true;
        _enabled = PlayerPrefs.GetInt("haptics", 1) == 1;
    }

    public static void Light() => Vibrate(12, false);
    public static void Medium() => Vibrate(25, true);
    public static void Heavy() => Vibrate(60, true);

    /// <summary>Strength by fruit level: small = light tick, big = heavy thud.</summary>
    public static void Pulse(int level)
    {
        if (level < 4) Light();
        else if (level < 8) Medium();
        else Heavy();
    }

#if UNITY_EDITOR
    static void Vibrate(int ms, bool strong) { }   // nothing to feel in the editor
#elif UNITY_ANDROID
    static AndroidJavaObject _vibrator;

    static void Vibrate(int ms, bool strong)
    {
        if (!Enabled) return;
        try
        {
            if (_vibrator == null)
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            _vibrator.Call("vibrate", (long)ms);
        }
        catch
        {
            Handheld.Vibrate();   // fallback (also makes Unity add the VIBRATE permission)
        }
    }
#elif UNITY_IOS
    static void Vibrate(int ms, bool strong)
    {
        if (Enabled && strong) Handheld.Vibrate();
    }
#elif UNITY_WEBGL
    [DllImport("__Internal")] static extern void Haptic_Vibrate(int ms);

    static void Vibrate(int ms, bool strong)
    {
        if (Enabled) Haptic_Vibrate(ms);
    }
#else
    static void Vibrate(int ms, bool strong) { }
#endif
}
