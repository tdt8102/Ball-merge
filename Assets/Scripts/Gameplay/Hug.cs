using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hug: for its duration, cats of the SAME level within the hug radius are pulled towards each other.
///
/// How: a small acceleration through Rigidbody.AddForce (ForceMode.Acceleration, so every cat accelerates the same
/// whatever its mass), applied once per physics step from GameManager.FixedUpdate. Transforms are never moved, so walls and
/// other cats stop the cats as always. Each cat is pulled towards its nearest same-level partner only:
///  - strength is hugAcceleration at most, fading linearly to 0 at the hug radius (a light pull, below gravity);
///  - hugMaxSpeed caps the speed the pull itself can build up towards the partner (no runaway velocity);
///  - no pull once the two are touching (merge range), so nothing is pushed together and vibrates;
///  - a pair is skipped when another cat sits on the line between them (nothing is pulled through an obstacle);
///  - a cat whose top is at the red line is never pulled upwards.
/// Different-level cats are not affected at all.
///
/// Causality: every cat remembers who it was last pulled towards and when. A merge of that pair shortly after is a
/// skill-triggered merge (no energy); the cat it creates is marked for a short chain window, so a merge that the new cat
/// then makes is skill-triggered too. Merges that need no pull stay normal.
/// </summary>
public sealed class HugEffect
{
    public const float DeadZone = 1.05f;          // no pull when the centre distance is below (r1 + r2) * this
    public const float TopMargin = 0.15f;         // no upward pull for a cat whose top is within this of the red line
    public const float BlameWindow = 0.3f;        // seconds a pull counts as "the cause" of a merge of that pair
    public const float ChainWindow = 0.5f;        // seconds a cat made by a hug merge counts as hug-driven

    public bool Active { get; set; }

    static bool OnBoard(Cat c) => c != null && c.Dropped && !c.Merging && c.Body != null && !c.Body.isKinematic;

    /// <summary>One physics step of pulling (no-op when Hug is not active).</summary>
    /// <param name="redLineY">The y of the red line (GameManager.DangerY).</param>
    public void Step(List<Cat> cats, float dt, float now, float redLineY)
    {
        if (!Active || dt <= 0f) return;
        float radius = ThemeSkills.HugRadius, maxAccel = ThemeSkills.HugAcceleration, maxSpeed = ThemeSkills.HugMaxSpeed;

        foreach (var a in cats)
        {
            if (!OnBoard(a)) continue;
            Vector2 pa = a.transform.position;

            Cat partner = null;
            float best = float.MaxValue;
            foreach (var b in cats)
            {
                if (b == a || !OnBoard(b) || b.Level != a.Level) continue;
                float d = Vector2.Distance(pa, b.transform.position);
                if (d > radius || d >= best) continue;
                if (Blocked(cats, a, b)) continue;
                partner = b;
                best = d;
            }
            if (partner == null) continue;
            if (best <= (a.Radius + partner.Radius) * DeadZone) continue;       // already together: the merge takes it from here

            Vector2 dir = ((Vector2)partner.transform.position - pa) / best;
            float accel = maxAccel * Mathf.Clamp01(1f - best / radius);         // fades to 0 at the radius
            float towards = Vector2.Dot(a.Velocity, dir);
            accel = Mathf.Min(accel, Mathf.Max(0f, (maxSpeed - towards) / dt)); // never build more than maxSpeed towards the partner
            Vector2 push = dir * accel;
            if (push.y > 0f && pa.y + a.Radius >= redLineY - TopMargin) push.y = 0f;   // do not lift a cat over the red line
            if (push.sqrMagnitude <= 0f) continue;

            a.Body.AddForce(new Vector3(push.x, push.y, 0f), ForceMode.Acceleration);
            a.HugPartner = partner;
            a.HugStamp = now;
        }
    }

    /// <summary>Is another cat on the line between a and b (so a pull would drive a into it)?</summary>
    static bool Blocked(List<Cat> cats, Cat a, Cat b)
    {
        Vector2 p = a.transform.position, q = b.transform.position, seg = q - p;
        float len2 = seg.sqrMagnitude;
        foreach (var o in cats)
        {
            if (o == null || o == a || o == b) continue;
            Vector2 c = o.transform.position;
            float t = len2 > 0f ? Mathf.Clamp01(Vector2.Dot(c - p, seg) / len2) : 0f;
            if (Vector2.Distance(c, p + seg * t) < o.Radius * 0.9f) return true;
        }
        return false;
    }

    // ------------------------------------------------------------------ who caused a merge

    /// <summary>
    /// The source of a merge of a and b. A caller that already says Skill keeps it. Otherwise it is Skill when the pull was
    /// driving this very pair a moment ago, or when one of the two came out of a hug merge a moment ago (chain).
    /// </summary>
    public static MergeSource Classify(MergeSource source, Cat a, Cat b, float now, out bool hugCaused)
    {
        hugCaused = (a.HugPartner == b && now - a.HugStamp <= BlameWindow)
                 || (b.HugPartner == a && now - b.HugStamp <= BlameWindow)
                 || a.HugChainUntil >= now || b.HugChainUntil >= now;
        return hugCaused ? MergeSource.Skill : source;
    }

    /// <summary>The cat a hug merge created: merges it makes right away belong to the same chain.</summary>
    public static void MarkChain(Cat created, float now) => created.HugChainUntil = now + ChainWindow;
}

/// <summary>Hug: pulls same-level cats together for hugDuration. One use at a time.</summary>
public sealed class HugHandler : ISkillHandler
{
    readonly HugEffect _effect;

    public HugHandler(HugEffect effect) { _effect = effect; }

    public float Duration => ThemeSkills.HugSeconds;
    public bool CanTarget(Cat target) => false;

    public bool Activate(Cat target)
    {
        if (_effect.Active) return false;                 // no stacking
        _effect.Active = true;
        return true;
    }

    public void End() => _effect.Active = false;
}
