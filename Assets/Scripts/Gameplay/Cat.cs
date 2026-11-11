using UnityEngine;

/// <summary>One merge-able ball. Physics lives on the root, visuals on a scaled child.</summary>
public class Cat : MonoBehaviour
{
    public int Level { get; private set; }
    public float Radius { get; private set; }
    public bool Merging { get; set; }
    public bool Dropped { get; private set; }
    public float Age { get; private set; }          // seconds since released
    public Rigidbody2D Body { get; private set; }

    public bool Worried { get; set; }               // set by GameManager when the stack is too high

    const float BaseEyeSize = 0.19f;
    float EyeSize;                                  // eye diameter; some cat styles have bigger eyes
    Transform _visual;
    readonly Transform[] _eyes = new Transform[2];
    float _popT = 1f, _blipIn, _lastImpact;

    public Vector2 Velocity
    {
#if UNITY_6000_0_OR_NEWER
        get => Body.linearVelocity;
        set => Body.linearVelocity = value;
#else
        get => Body.velocity;
        set => Body.velocity = value;
#endif
    }

    /// <summary>Put a saved cat back exactly as it was (no pop-in, already past the danger-line grace time).</summary>
    public void Restore(float angle, Vector2 velocity, float angularVelocity)
    {
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        Velocity = velocity;
        Body.angularVelocity = angularVelocity;
        Age = 5f;
        _popT = 1f;
    }

    public float Speed
    {
        get
        {
#if UNITY_6000_0_OR_NEWER
            return Body.linearVelocity.magnitude;
#else
            return Body.velocity.magnitude;
#endif
        }
    }

    public static Cat Create(int level, Vector2 pos, PhysicsMaterial2D mat, bool preview)
    {
        var go = new GameObject("Cat_" + level);
        go.transform.position = pos;
        var f = go.AddComponent<Cat>();
        f.Init(level, mat, preview);
        return f;
    }

    void Init(int level, PhysicsMaterial2D mat, bool preview)
    {
        Level = level;
        Radius = CatDatabase.Radius[level];

        // visual (circle sprite scaled to diameter)
        _visual = new GameObject("Visual").transform;
        _visual.SetParent(transform, false);
        _visual.localScale = Vector3.one * Radius * 2f;
        var sr = _visual.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = CatSprites.Get(level);
        sr.sortingOrder = 1;
        AddEyes(_visual, level);

        // physics
        Body = gameObject.AddComponent<Rigidbody2D>();
        Body.mass = Mathf.PI * Radius * Radius;
#if UNITY_6000_0_OR_NEWER
        Body.linearDamping = 0.05f;
        Body.angularDamping = 0.4f;
#else
        Body.drag = 0.05f;
        Body.angularDrag = 0.4f;
#endif
        Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        Body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = gameObject.AddComponent<CircleCollider2D>();
        col.radius = Radius;
        col.sharedMaterial = mat;

        if (preview) Body.simulated = false;   // hovering at the dropper, no collisions yet
        else { Dropped = true; _popT = 0f; }
    }

    void AddEyes(Transform parent, int level)
    {
        var style = Cosmetics.CatOf(level);
        EyeSize = BaseEyeSize * style.EyeScale;
        int i = 0;
        foreach (float sx in new[] { -style.EyeX, style.EyeX })
        {
            var eye = new GameObject("Eye").transform;
            eye.SetParent(parent, false);
            eye.localPosition = new Vector3(sx, style.EyeY, 0);
            eye.localScale = Vector3.one * EyeSize;
            var r = eye.gameObject.AddComponent<SpriteRenderer>();
            r.sprite = CatSprites.Eye(level);
            r.sortingOrder = 2;
            _eyes[i++] = eye;
        }
        _blipIn = Random.Range(1f, 4f);
    }

    void AnimateEyes()
    {
        // blink every few seconds; eyes widen when the stack is near the danger line
        _blipIn -= Time.deltaTime;
        if (_blipIn <= -0.12f) _blipIn = Random.Range(2f, 5f);
        float open = _blipIn < 0f ? 0.1f : 1f;
        float size = EyeSize * (Worried ? 1.5f : 1f);
        var s = new Vector3(size, size * open, 1f);
        _eyes[0].localScale = s;
        _eyes[1].localScale = s;
    }

    /// <summary>Release from the dropper.</summary>
    public void Release()
    {
        Dropped = true;
        Body.simulated = true;
    }

    void Update()
    {
        if (Dropped) Age += Time.deltaTime;
        AnimateEyes();

        // little squash-pop when freshly created by a merge
        if (_popT < 1f)
        {
            _popT = Mathf.Min(1f, _popT + Time.deltaTime / 0.18f);
            float s = 1f + 0.25f * Mathf.Sin(_popT * Mathf.PI);
            _visual.localScale = Vector3.one * Radius * 2f * s;
        }
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        TryMerge(c);
        if (Dropped && !Merging) Impact(c);
    }

    void OnCollisionStay2D(Collision2D c) => TryMerge(c);

    /// <summary>Knock sound + a little dust when landing hard.</summary>
    void Impact(Collision2D c)
    {
        float v = c.relativeVelocity.magnitude;
        if (v < 2.2f || Time.time - _lastImpact < 0.07f) return;
        _lastImpact = Time.time;
        if (AudioManager.Instance != null) AudioManager.Instance.Tock(v, Radius);
        if (v > 5f && Fx.Instance != null && c.contactCount > 0)
            Fx.Instance.Burst(c.GetContact(0).point, new Color(0.85f, 0.78f, 0.68f), 3, 1.2f, 0.08f);
    }

    void TryMerge(Collision2D c)
    {
        if (Merging || !Dropped) return;
        var other = c.collider.GetComponent<Cat>();
        if (other == null || other.Merging || !other.Dropped || other.Level != Level) return;
        // both cats see the collision; only the one with the lower id performs the merge
        if (GetInstanceID() > other.GetInstanceID()) return;
        GameManager.Instance.Merge(this, other);
    }
}
