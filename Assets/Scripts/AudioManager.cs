using UnityEngine;

/// <summary>
/// All sounds are synthesized at startup (no audio files). Merge pitch climbs a pentatonic scale
/// with the cat level, so chains of merges sound like a little melody.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    public bool Muted { get; private set; }

    const int Voices = 10;
    static readonly int[] Penta = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21, 24 };   // semitones per level

    AudioSource[] _src;
    int _next;
    float _lastTock;
    int _sr;
    AudioClip _drop, _meow, _ding, _tock, _thump, _over, _fanfare, _click, _warn;

    void Awake()
    {
        Instance = this;
        Muted = PlayerPrefs.GetInt("muted", 0) == 1;
        _sr = AudioSettings.outputSampleRate;
        BuildClips();

        _src = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++)
        {
            _src[i] = gameObject.AddComponent<AudioSource>();
            _src[i].playOnAwake = false;
            _src[i].spatialBlend = 0f;
        }

        if (FindObjectOfType<AudioListener>() == null && Camera.main != null)
            Camera.main.gameObject.AddComponent<AudioListener>();
    }

    public void SetMuted(bool muted)
    {
        Muted = muted;
        PlayerPrefs.SetInt("muted", muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------------ public API

    public void Drop() => Play(_drop, 0.7f, Random.Range(0.95f, 1.05f));
    public void Click() => Play(_click, 0.6f);
    public void Warn() => Play(_warn, 0.35f);
    public void GameOver() => Play(_over, 0.8f);
    public void Fanfare() => Play(_fanfare, 0.9f);
    public void Discover() { Play(_ding, 0.6f, 1f); Play(_ding, 0.5f, 1.5f); }

    public void Merge(int level, int combo)
    {
        // bigger cat = deeper meow
        float pitch = Mathf.Lerp(1.45f, 0.62f, Mathf.Clamp01(level / (float)CatDatabase.MaxLevel)) * Random.Range(0.96f, 1.04f);
        Play(_meow, 0.75f, pitch);
        if (level >= 7) Play(_thump, 0.8f);                       // big cats get a low thud
        if (combo >= 2)                                           // combo: rising "ding" up the scale
        {
            float cp = Mathf.Pow(2f, Penta[Mathf.Clamp(combo - 2, 0, Penta.Length - 1)] / 12f);
            Play(_ding, 0.55f, cp);
        }
    }

    public void Tock(float speed, float radius)
    {
        if (Time.unscaledTime - _lastTock < 0.04f) return;
        _lastTock = Time.unscaledTime;
        float vol = Mathf.Clamp01((speed - 2f) / 8f) * 0.5f + 0.1f;
        float pitch = Mathf.Clamp(1.4f / (0.6f + radius), 0.5f, 2f);   // bigger cat = lower knock
        Play(_tock, vol, pitch);
    }

    void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (Muted || clip == null) return;
        var s = _src[_next];
        _next = (_next + 1) % Voices;
        s.clip = clip;
        s.volume = volume;
        s.pitch = pitch;
        s.Play();
    }

    // ------------------------------------------------------------------ synthesis

    void BuildClips()
    {
        _drop = Clip("drop", Tone(600, 220, 0.10f, 0.003f, 2.5f, 0.2f));
        _meow = Clip("meow", Meow());
        _ding = Clip("ding", Tone(880, 880, 0.40f, 0.002f, 3f, 0.25f, 0.10f));
        _thump = Clip("thump", Tone(120, 45, 0.30f, 0.003f, 2f, 0.1f));
        _click = Clip("click", Tone(900, 700, 0.04f, 0.001f, 2f, 0.1f));
        _warn = Clip("warn", Tone(500, 500, 0.09f, 0.005f, 1.5f, 0.5f, 0.3f));

        // knock: short low tone + a puff of filtered noise
        var tock = Tone(220, 110, 0.08f, 0.002f, 3f, 0.2f);
        Mix(tock, Noise(0.05f, 3f, 0.25f), 0, 0.5f);
        _tock = Clip("tock", tock);

        // game over: four falling notes
        var over = new float[(int)(1.3f * _sr)];
        float[] notes = { 392f, 330f, 262f, 196f };
        for (int i = 0; i < notes.Length; i++)
            Mix(over, Tone(notes[i], notes[i] * 0.98f, i == 3 ? 0.6f : 0.25f, 0.005f, 1.6f, 0.3f, 0.1f), (int)(i * 0.22f * _sr), 0.8f);
        _over = Clip("over", over);

        // fanfare for two max cats: rising arpeggio
        var fan = new float[(int)(1.2f * _sr)];
        float[] arp = { 523f, 659f, 784f, 1047f, 1319f };
        for (int i = 0; i < arp.Length; i++)
            Mix(fan, Tone(arp[i], arp[i], 0.45f, 0.003f, 2.2f, 0.3f, 0.1f), (int)(i * 0.09f * _sr), 0.55f);
        _fanfare = Clip("fanfare", fan);
    }

    AudioClip Clip(string name, float[] data)
    {
        var c = AudioClip.Create(name, data.Length, 1, _sr, false);
        c.SetData(data, 0);
        return c;
    }

    /// <summary>Sine (+harmonics) with exponential pitch glide f0 -> f1 and attack/decay envelope.</summary>
    float[] Tone(float f0, float f1, float dur, float attack, float decayPow, float h2 = 0f, float h3 = 0f)
    {
        int n = Mathf.Max(1, (int)(dur * _sr));
        var d = new float[n];
        double phase = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float f = f0 * Mathf.Pow(f1 / f0, t);
            phase += 2.0 * System.Math.PI * f / _sr;
            float env = Mathf.Min(1f, i / Mathf.Max(1f, attack * _sr)) * Mathf.Pow(1f - t, decayPow);
            float s = (float)(System.Math.Sin(phase) + h2 * System.Math.Sin(2 * phase) + h3 * System.Math.Sin(3 * phase));
            d[i] = s * env * 0.45f;
        }
        return d;
    }

    /// <summary>"Mi-aow": pitch rises quickly then falls, harmonics shift from bright to hollow, light vibrato.</summary>
    float[] Meow()
    {
        const float dur = 0.4f;
        int n = (int)(dur * _sr);
        var d = new float[n];
        double ph = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n, sec = i / (float)_sr;
            float f = 380f + 470f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.6f));
            f *= 1f + 0.02f * Mathf.Sin(2f * Mathf.PI * 7f * sec);
            ph += 2.0 * System.Math.PI * f / _sr;
            float bright = 0.55f - 0.35f * t;                         // "mi" is bright, "ow" is rounder
            float s = (float)(System.Math.Sin(ph) + 0.6 * System.Math.Sin(2 * ph) + bright * System.Math.Sin(3 * ph) + 0.5 * bright * System.Math.Sin(4 * ph));
            float env = Mathf.Min(1f, sec / 0.03f) * Mathf.Pow(1f - t, 0.8f);
            d[i] = s * env * 0.3f;
        }
        return d;
    }

    float[] Noise(float dur, float decayPow, float smooth)
    {
        int n = Mathf.Max(1, (int)(dur * _sr));
        var d = new float[n];
        float y = 0;
        var rng = new System.Random(7);
        for (int i = 0; i < n; i++)
        {
            float x = (float)(rng.NextDouble() * 2 - 1);
            y += smooth * (x - y);                                  // one-pole low-pass
            d[i] = y * Mathf.Pow(1f - i / (float)n, decayPow);
        }
        return d;
    }

    static void Mix(float[] dst, float[] src, int offset, float gain)
    {
        for (int i = 0; i < src.Length && offset + i < dst.Length; i++) dst[offset + i] += src[i] * gain;
    }
}
