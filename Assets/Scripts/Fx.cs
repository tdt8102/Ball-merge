using System.Collections.Generic;
using UnityEngine;

/// <summary>Cheap particle bursts (pooled sprites) and camera shake. No ParticleSystem / shaders needed.</summary>
public class Fx : MonoBehaviour
{
    public static Fx Instance { get; private set; }

    class Particle
    {
        public Transform T;
        public SpriteRenderer R;
        public Vector2 Vel;
        public float Life, Max, Size;
        public Color Col;
    }

    const int MaxParticles = 160;
    readonly List<Particle> _live = new List<Particle>();
    readonly Stack<Particle> _free = new Stack<Particle>();

    Camera _cam;
    Vector3 _camBase;
    float _shakeAmp, _shakeT, _shakeDur;

    void Awake()
    {
        Instance = this;
        _cam = Camera.main;
        _camBase = _cam.transform.position;
    }

    // ------------------------------------------------------------------ particles

    public void Burst(Vector2 pos, Color color, int count, float speed, float size = 0.14f)
    {
        for (int i = 0; i < count; i++)
        {
            if (_live.Count >= MaxParticles) return;
            var p = _free.Count > 0 ? _free.Pop() : Create();
            p.T.gameObject.SetActive(true);
            p.T.position = pos;
            float ang = Random.value * Mathf.PI * 2f;
            p.Vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed * Random.Range(0.4f, 1f);
            p.Max = p.Life = Random.Range(0.35f, 0.7f);
            p.Size = size * Random.Range(0.6f, 1.2f);
            // lighten the colour a bit so bursts pop against the fruit
            p.Col = Color.Lerp(color, Color.white, Random.Range(0.1f, 0.45f));
            p.R.color = p.Col;
            _live.Add(p);
        }
    }

    Particle Create()
    {
        var go = new GameObject("P");
        go.transform.SetParent(transform, false);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = SpriteFactory.Circle;
        r.sortingOrder = 4;
        return new Particle { T = go.transform, R = r };
    }

    // ------------------------------------------------------------------ shake

    public void Shake(float amplitude, float duration)
    {
        if (amplitude < _shakeAmp * (_shakeT / Mathf.Max(0.01f, _shakeDur))) return;   // don't cut a bigger shake
        _shakeAmp = amplitude;
        _shakeDur = _shakeT = duration;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = _live.Count - 1; i >= 0; i--)
        {
            var p = _live[i];
            p.Life -= dt;
            if (p.Life <= 0f)
            {
                p.T.gameObject.SetActive(false);
                _live.RemoveAt(i);
                _free.Push(p);
                continue;
            }
            p.Vel.y -= 9f * dt;
            p.Vel *= 1f - 2f * dt;
            p.T.position += (Vector3)(p.Vel * dt);
            float k = p.Life / p.Max;
            p.T.localScale = Vector3.one * p.Size * k;
            var c = p.Col; c.a = k;
            p.R.color = c;
        }
    }

    void LateUpdate()
    {
        if (_cam == null) return;
        if (_shakeT > 0f)
        {
            _shakeT -= Time.deltaTime;
            float k = Mathf.Clamp01(_shakeT / _shakeDur);
            Vector2 o = Random.insideUnitCircle * _shakeAmp * k;
            _cam.transform.position = _camBase + new Vector3(o.x, o.y, 0);
        }
        else _cam.transform.position = _camBase;
    }
}
