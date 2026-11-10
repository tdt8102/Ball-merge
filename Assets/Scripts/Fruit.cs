using UnityEngine;

/// <summary>One merge-able ball. Physics lives on the root, visuals on a scaled child.</summary>
public class Fruit : MonoBehaviour
{
    public int Level { get; private set; }
    public float Radius { get; private set; }
    public bool Merging { get; set; }
    public bool Dropped { get; private set; }
    public float Age { get; private set; }          // seconds since released
    public Rigidbody2D Body { get; private set; }

    Transform _visual;
    float _popT = 1f;

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

    public static Fruit Create(int level, Vector2 pos, PhysicsMaterial2D mat, bool preview)
    {
        var go = new GameObject("Fruit_" + level);
        go.transform.position = pos;
        var f = go.AddComponent<Fruit>();
        f.Init(level, mat, preview);
        return f;
    }

    void Init(int level, PhysicsMaterial2D mat, bool preview)
    {
        Level = level;
        Radius = FruitDatabase.Radius[level];

        // visual (circle sprite scaled to diameter)
        _visual = new GameObject("Visual").transform;
        _visual.SetParent(transform, false);
        _visual.localScale = Vector3.one * Radius * 2f;
        var sr = _visual.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle;
        sr.color = FruitDatabase.Colors[level];
        sr.sortingOrder = 1;
        AddEyes(_visual);

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

    static void AddEyes(Transform parent)
    {
        foreach (float sx in new[] { -0.2f, 0.2f })
        {
            var eye = new GameObject("Eye").transform;
            eye.SetParent(parent, false);
            eye.localPosition = new Vector3(sx, 0.06f, 0);
            eye.localScale = Vector3.one * 0.16f;
            var r = eye.gameObject.AddComponent<SpriteRenderer>();
            r.sprite = SpriteFactory.Circle;
            r.color = new Color(0.12f, 0.1f, 0.15f);
            r.sortingOrder = 2;
        }
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

        // little squash-pop when freshly created by a merge
        if (_popT < 1f)
        {
            _popT = Mathf.Min(1f, _popT + Time.deltaTime / 0.18f);
            float s = 1f + 0.25f * Mathf.Sin(_popT * Mathf.PI);
            _visual.localScale = Vector3.one * Radius * 2f * s;
        }
    }

    void OnCollisionEnter2D(Collision2D c) => TryMerge(c);
    void OnCollisionStay2D(Collision2D c) => TryMerge(c);

    void TryMerge(Collision2D c)
    {
        if (Merging || !Dropped) return;
        var other = c.collider.GetComponent<Fruit>();
        if (other == null || other.Merging || !other.Dropped || other.Level != Level) return;
        // both fruits see the collision; only the one with the lower id performs the merge
        if (GetInstanceID() > other.GetInstanceID()) return;
        GameManager.Instance.Merge(this, other);
    }
}
