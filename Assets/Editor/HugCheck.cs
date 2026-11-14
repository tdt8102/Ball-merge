using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks Hug with real PhysX (Physics.Simulate on a walled board like GameManager's): same-level cats pulled together,
/// other levels untouched, no pull through an obstacle, wall limits, speed cap, a packed board, a cat at the red line,
/// pause/resume, the 3 s end, no stacking, and who caused a merge (energy) including a chain.
/// Menu: Tools/Suika/Check Hug. Batch: -executeMethod HugCheck.Run (exit code 1 on failure).
/// Touches no save data; physics settings are restored afterwards.
/// </summary>
public static class HugCheck
{
    const float Dt = 1f / 64f;                      // exact in binary: 3 s = 192 steps
    const float FloorY = -5.2f, HalfWidth = 3f;
    static float DangerY;

    [MenuItem("Tools/Suika/Check Hug")]
    public static void RunMenu()
    {
        if (Execute()) Debug.Log("[Hug] all checks passed");
    }

    public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

    static bool Execute()
    {
        var gravity = Physics.gravity;
        var mode = Physics.simulationMode;
        int iters = Physics.defaultSolverIterations, vIters = Physics.defaultSolverVelocityIterations;
        DangerY = (float)typeof(GameManager).GetField("DangerY", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            Physics.defaultSolverIterations = 12;
            Physics.defaultSolverVelocityIterations = 4;
            Test();
            SourceGuard();
            Debug.Log("HUG_CHECK_OK");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Hug check failed: " + e.Message);
            return false;
        }
        finally
        {
            Physics.gravity = gravity;
            Physics.simulationMode = mode;
            Physics.defaultSolverIterations = iters;
            Physics.defaultSolverVelocityIterations = vIters;
            foreach (var w in Worlds) w.Dispose();
            Worlds.Clear();
        }
    }

    static readonly List<World> Worlds = new List<World>();

    /// <summary>A walled board like GameManager.BuildPlayfield, with the same physics material and gravity.</summary>
    sealed class World
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly PhysicMaterial _mat = new PhysicMaterial("Cat") { dynamicFriction = 0.35f, staticFriction = 0.35f, bounciness = 0.08f };
        public readonly List<Cat> Cats = new List<Cat>();
        public readonly HugEffect Hug = new HugEffect();
        public float Time;

        public World(bool gravity)
        {
            foreach (var old in Worlds.ToArray()) old.Dispose();            // one board in the scene at a time
            Worlds.Clear();
            Physics.gravity = gravity ? new Vector3(0f, -20f, 0f) : Vector3.zero;
            Wall("WallL", new Vector2(-HalfWidth - 0.25f, FloorY + 10f - 0.5f), new Vector2(0.5f, 20f));
            Wall("WallR", new Vector2(HalfWidth + 0.25f, FloorY + 10f - 0.5f), new Vector2(0.5f, 20f));
            Wall("Floor", new Vector2(0f, FloorY - 0.25f), new Vector2(HalfWidth * 2f + 1f, 0.5f));
            Worlds.Add(this);
        }

        void Wall(string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(size.x, size.y, 4f);
            box.sharedMaterial = _mat;
            _objects.Add(go);
        }

        public Cat Add(int level, float x, float y, bool resting = true)
        {
            var c = Cat.Create(level, new Vector2(x, y), _mat, false);
            c.Restore(0f, Vector2.zero, 0f);
            Cats.Add(c);
            Physics.SyncTransforms();
            return c;
        }

        /// <summary>A cat resting on the floor at x.</summary>
        public Cat OnFloor(int level, float x) => Add(level, x, FloorY + CatDatabase.Radius[level]);

        /// <summary>FixedUpdate: Hug's pull, then the physics step.</summary>
        public void Step(bool hug = true)
        {
            if (hug) Hug.Step(Cats, Dt, Time, DangerY);
            Physics.Simulate(Dt);
            Physics.SyncTransforms();
            Time += Dt;
        }

        public void Steps(int n, bool hug = true) { for (int i = 0; i < n; i++) Step(hug); }

        public void Dispose()
        {
            foreach (var c in Cats) if (c != null) UnityEngine.Object.DestroyImmediate(c.gameObject);
            foreach (var o in _objects) if (o != null) UnityEngine.Object.DestroyImmediate(o);
            Cats.Clear();
            _objects.Clear();
        }
    }

    static Vector2 Pos(Cat c) => c.transform.position;
    static float Span(IEnumerable<Cat> cats)
    {
        float lo = float.MaxValue, hi = float.MinValue;
        foreach (var c in cats) { lo = Mathf.Min(lo, Pos(c).x); hi = Mathf.Max(hi, Pos(c).x); }
        return hi - lo;
    }

    static void InBounds(World w, string where)
    {
        foreach (var c in w.Cats)
        {
            var p = Pos(c);
            Check(!float.IsNaN(p.x) && !float.IsNaN(p.y), "no NaN " + where);
            Check(Mathf.Abs(p.x) + c.Radius <= HalfWidth + 0.06f, "inside the walls " + where + " (x=" + p.x + ", r=" + c.Radius + ")");
            Check(p.y - c.Radius >= FloorY - 0.06f, "above the floor " + where);
        }
    }

    static void Test()
    {
        Check(ThemeSkills.Hug.Kind == ThemeSkillKind.Instant, "Hug is an instant-use skill");
        Check(Mathf.Approximately(ThemeSkills.HugSeconds, 3f) && Mathf.Approximately(ThemeSkills.HugRadius, 3f), "3 s, radius 3");
        Check(ThemeSkills.HugAcceleration > 0f && ThemeSkills.HugAcceleration < Balance.Current.gravity, "the pull is lighter than gravity");

        // ---- several cats of one level on the floor: they come together; without Hug they do not move
        {
            var hugged = new World(true);
            foreach (float x in new[] { -2.4f, -1.2f, 0f, 1.2f, 2.4f }) hugged.OnFloor(3, x);
            hugged.Steps(40, hug: false);                                   // settle
            float before = Span(hugged.Cats);
            hugged.Hug.Active = true;
            hugged.Steps(192);
            float after = Span(hugged.Cats);
            InBounds(hugged, "same-level row");
            hugged.Dispose(); Worlds.Remove(hugged);

            var control = new World(true);
            foreach (float x in new[] { -2.4f, -1.2f, 0f, 1.2f, 2.4f }) control.OnFloor(3, x);
            control.Steps(40, hug: false);
            control.Steps(192, hug: false);
            float controlAfter = Span(control.Cats);
            control.Dispose(); Worlds.Remove(control);
            Debug.Log("Hug row span: before " + before + ", with Hug " + after + ", without " + controlAfter);
            Check(Mathf.Abs(controlAfter - before) < 0.05f, "without Hug nothing moves");
            Check(after < before - 0.4f, "with Hug the row pulls together (" + before + " -> " + after + ")");
        }

        // ---- different level groups: each group closes on its own partner, nobody is pulled by another level
        {
            var w = new World(true);
            var a1 = w.OnFloor(2, -2.5f); var a2 = w.OnFloor(2, -0.9f);      // level 2 pair
            var b1 = w.OnFloor(4, 0.6f); var b2 = w.OnFloor(4, 2.3f);        // level 4 pair
            w.Steps(40, hug: false);
            float gapA = Mathf.Abs(Pos(a1).x - Pos(a2).x), gapB = Mathf.Abs(Pos(b1).x - Pos(b2).x);
            float a2ToB = Pos(b1).x - Pos(a2).x;
            w.Hug.Active = true;
            w.Steps(192);
            Check(Mathf.Abs(Pos(a1).x - Pos(a2).x) < gapA - 0.3f, "level 2 pair closes");
            Check(Mathf.Abs(Pos(b1).x - Pos(b2).x) < gapB - 0.3f, "level 4 pair closes");
            Check(a1.Level == 2 && b1.Level == 4, "levels untouched");
            InBounds(w, "two groups");
        }

        // ---- a cat with no same-level partner is not touched at all (Step applies nothing)
        {
            var w = new World(false);
            var lone = w.Add(5, 0f, 0f);
            var other = w.Add(6, 1.6f, 0f);
            w.Hug.Active = true;
            w.Steps(64);
            Check(lone.Velocity.magnitude < 1e-4f && other.Velocity.magnitude < 1e-4f && lone.HugPartner == null && other.HugPartner == null, "different levels: no pull, no partner");
            Check(Pos(lone) == new Vector2(0f, 0f), "and no movement");
        }

        // ---- a different-level cat between a pair blocks the pull (nothing is dragged through an obstacle)
        {
            var w = new World(true);
            var left = w.OnFloor(3, -1.4f);
            var right = w.OnFloor(3, 1.4f);
            var blocker = w.OnFloor(5, 0f);                                  // a bigger cat between them (0.69 + 0.48 fits in 1.4)
            w.Steps(40, hug: false);
            Vector2 pl = Pos(left), pr = Pos(right);
            w.Hug.Active = true;
            for (int i = 0; i < 192; i++)
            {
                w.Step();
                if (left.HugPartner != null || right.HugPartner != null) { Debug.Log("blocker test: pulled at step " + i + ": left " + Pos(left) + " right " + Pos(right) + " blocker " + Pos(blocker) + " (start " + pl + ", " + pr + ")"); break; }
            }
            Check(left.HugPartner == null && right.HugPartner == null, "the pair is not pulled through the blocker");
            Check(Vector2.Distance(Pos(left), pl) < 0.05f && Vector2.Distance(Pos(right), pr) < 0.05f, "they stay where they were");
        }

        // ---- against a wall: pulled away from it towards the partner; never through it; never faster than allowed
        {
            var w = new World(true);
            float r = CatDatabase.Radius[2];
            var atWall = w.OnFloor(2, HalfWidth - r);
            var partner = w.OnFloor(2, 0.7f);
            var leftWall = w.OnFloor(5, -HalfWidth + CatDatabase.Radius[5]);
            var leftPartner = w.OnFloor(5, -0.3f);
            w.Steps(40, hug: false);
            w.Hug.Active = true;
            float worst = 0f;
            for (int i = 0; i < 192; i++)
            {
                w.Step();
                InBounds(w, "wall test step " + i);
                foreach (var c in w.Cats) worst = Mathf.Max(worst, Mathf.Abs(c.Velocity.x));
            }
            Check(Pos(atWall).x < HalfWidth - r - 0.2f, "the cat at the wall was pulled away from it");
            Check(worst <= ThemeSkills.HugMaxSpeed + 0.5f, "horizontal speed stays near the cap (" + worst + ")");
        }

        // ---- the speed cap, alone in space: a pair converges and never runs away
        {
            var w = new World(false);
            var a = w.Add(3, -1.1f, 0f);
            var b = w.Add(3, 1.1f, 0f);
            w.Hug.Active = true;
            float fastest = 0f;
            for (int i = 0; i < 192; i++)
            {
                w.Step();
                fastest = Mathf.Max(fastest, a.Velocity.magnitude, b.Velocity.magnitude);
            }
            Debug.Log("Hug pair in space: fastest " + fastest + ", gap " + Vector2.Distance(Pos(a), Pos(b)));
            Check(fastest <= ThemeSkills.HugMaxSpeed + 0.25f, "speed never passes the cap by more than a bounce (" + fastest + ")");
            Check(Vector2.Distance(Pos(a), Pos(b)) < 2.2f - 0.1f, "the pair came closer");
        }

        // ---- a packed board: mixed levels, many cats, everything stays inside, nothing explodes
        {
            var w = new World(true);
            int n = 0;
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 8; col++)
                {
                    int level = (row + col) % 3;                             // levels 0, 1, 2 mixed
                    float r = CatDatabase.Radius[level];
                    w.Add(level, -2.55f + col * 0.73f, FloorY + 0.4f + row * 0.78f);
                    n++;
                }
            w.Steps(120, hug: false);                                        // settle into a pile
            InBounds(w, "packed board before");
            w.Hug.Active = true;
            float worst = 0f;
            for (int i = 0; i < 192; i++)
            {
                w.Step();
                foreach (var c in w.Cats) worst = Mathf.Max(worst, c.Velocity.magnitude);
            }
            InBounds(w, "packed board after");
            Check(w.Cats.Count == n, "no cat lost");
            Check(worst < 8f, "no explosive speeds on a packed board (" + worst + ")");
        }

        // ---- a cat at the red line is never lifted over it by the pull (checked for one step, no gravity)
        {
            var w = new World(false);
            float r = CatDatabase.Radius[4];
            var edge = w.Add(4, 0f, DangerY - r - 0.02f);                    // top just under the line
            var above = w.Add(4, 0.3f, DangerY + 1.4f);                      // partner higher up
            w.Hug.Active = true;
            w.Step();
            Check(edge.Velocity.y <= 1e-5f, "no upward pull on a cat at the red line (vy=" + edge.Velocity.y + ")");
            Check(edge.Velocity.x > 0f, "it is still pulled sideways");
            var low = w.Add(2, -2f, 0f);
            var lowPartner = w.Add(2, -1.7f, 1.5f);
            w.Step();
            Check(low.Velocity.y > 0f, "a cat well below the line can be pulled up");
        }

        // ---- the pull stops when the two touch (no pushing them together)
        {
            var w = new World(false);
            float r = CatDatabase.Radius[3];
            var a = w.Add(3, 0f, 0f);
            var b = w.Add(3, 2f * r * 1.0f, 0f);                             // exactly touching
            w.Hug.Active = true;
            w.Steps(30);
            Check(a.HugPartner == null && b.HugPartner == null && a.Velocity.magnitude < 0.05f, "touching cats are not pulled any more");
        }

        // ---- the 3 s end, pause/resume, no stacking (skill framework + Hug, stepped like GameManager)
        {
            var energy = new SkillEnergy();
            energy.Reset();
            while (energy.Value < energy.Max) energy.OnMerge(MergeSource.Normal);
            var w = new World(true);
            w.OnFloor(3, -1.2f); w.OnFloor(3, 1.2f);
            w.Steps(40, hug: false);
            var skills = new SkillRuntime(energy);
            skills.Begin(ThemeSkills.Hug, new HugHandler(w.Hug));

            Check(skills.Press() == SkillPress.Activated && w.Hug.Active && energy.Value == 0, "activates, spends 100");
            for (int i = 0; i < 20; i++) Check(skills.Press() == SkillPress.Ignored, "pressing again while active is blocked");
            Check(!new HugHandler(w.Hug).Activate(null), "the handler refuses a second start");

            Action frame = () => { w.Step(); skills.Tick(Dt); };            // one running frame: physics step + skill clock
            for (int i = 0; i < 60; i++) frame();
            var snapshot = new[] { Pos(w.Cats[0]), Pos(w.Cats[1]) };
            float remaining = skills.TimedRemaining;
            // Pause/Settings: time scale 0, so neither FixedUpdate nor Update run: nothing is called
            Check(Pos(w.Cats[0]) == snapshot[0] && skills.TimedRemaining == remaining, "paused: nothing moves, clock stops");
            for (int i = 0; i < 191 - 60; i++) frame();                     // 60 + 131 = 191 running frames
            Check(skills.IsTimedActive && w.Hug.Active, "still active one frame before 3 s");
            frame();
            Check(!skills.IsTimedActive && !w.Hug.Active, "over after exactly 3 s of running time");

            // after the end, Step does nothing
            var cat = w.Cats[0];
            var stampBefore = cat.HugStamp;
            w.Hug.Step(w.Cats, Dt, w.Time + 10f, DangerY);
            Check(cat.HugStamp == stampBefore, "no pull after the end");

            // Game Over mid-effect stops it
            var energy2 = new SkillEnergy(); energy2.Reset(); while (energy2.Value < energy2.Max) energy2.OnMerge(MergeSource.Normal);
            var w2 = new World(true);
            w2.OnFloor(3, -1.2f); w2.OnFloor(3, 1.2f);
            var skills2 = new SkillRuntime(energy2);
            skills2.Begin(ThemeSkills.Hug, new HugHandler(w2.Hug));
            skills2.Press();
            w2.Steps(30);
            skills2.Lock();
            Check(!w2.Hug.Active && skills2.Press() == SkillPress.Ignored, "Game Over ends the pull and locks the button");

            // restart: Begin ends it
            var energy3 = new SkillEnergy(); energy3.Reset(); while (energy3.Value < energy3.Max) energy3.OnMerge(MergeSource.Normal);
            var w3 = new World(true);
            var skills3 = new SkillRuntime(energy3);
            skills3.Begin(ThemeSkills.Hug, new HugHandler(w3.Hug));
            skills3.Press();
            skills3.Begin(ThemeSkills.Hug, new HugHandler(w3.Hug));
            Check(!w3.Hug.Active, "restart ends the pull");
        }

        Causality();
    }

    /// <summary>Merges caused by the pull do not charge energy; a chain that follows from them does not either; others do.</summary>
    static void Causality()
    {
        // ---- the rule, pair by pair
        var w = new World(false);
        var a = w.Add(1, -1f, 0f);
        var b = w.Add(1, 1f, 0f);
        var other = w.Add(1, 0f, 3f);
        w.Hug.Active = true;
        w.Step();
        Check(a.HugPartner == b && b.HugPartner == a, "the pair was pulled towards each other");
        float now = w.Time;
        MergeSource s = HugEffect.Classify(MergeSource.Normal, a, b, now, out bool caused);
        Check(s == MergeSource.Skill && caused, "a pulled pair merges as skill-triggered");
        MergeSource late = HugEffect.Classify(MergeSource.Normal, a, b, now + HugEffect.BlameWindow + 0.2f, out bool lateCaused);
        Check(late == MergeSource.Normal && !lateCaused, "long after the pull the same pair is a normal merge");
        s = HugEffect.Classify(MergeSource.Normal, a, other, now, out caused);
        Check(s == MergeSource.Normal && !caused, "a pair that was not pulled towards each other is normal");
        s = HugEffect.Classify(MergeSource.Skill, a, other, now, out caused);
        Check(s == MergeSource.Skill && !caused, "a caller that says Skill stays Skill");

        // chain: the cat made by a hug merge keeps the next merge in the same chain, for a short window only
        var made = w.Add(2, 0f, -3f);
        var waiting = w.Add(2, 0.9f, -3f);
        s = HugEffect.Classify(MergeSource.Normal, made, waiting, now, out caused);
        Check(s == MergeSource.Normal, "before it is marked, that merge would count");
        HugEffect.MarkChain(made, now);
        s = HugEffect.Classify(MergeSource.Normal, made, waiting, now + 0.2f, out caused);
        Check(s == MergeSource.Skill && caused, "chain: the next merge of the hug-made cat is skill-triggered");
        s = HugEffect.Classify(MergeSource.Normal, made, waiting, now + HugEffect.ChainWindow + 0.3f, out caused);
        Check(s == MergeSource.Normal, "the chain window ends");
        w.Dispose(); Worlds.Remove(w);

        // ---- on the board, with the merge detection of ScanMerges: three level-1 cats and a waiting level-2 cat
        var energy = new SkillEnergy();
        energy.Reset();
        int normal = 0, skill = 0;
        var board = new World(true);
        board.OnFloor(1, -1.4f); board.OnFloor(1, 0.4f);                       // the pair Hug will bring together
        board.OnFloor(2, 2.2f);                                                // a level-2 cat waiting for the result
        board.Steps(40, hug: false);
        board.Hug.Active = true;
        int merges = 0;
        for (int i = 0; i < 192; i++)
        {
            board.Step();
            Cat ca = null, cb = null;
            foreach (var x in board.Cats)
                foreach (var y in board.Cats)
                    if (ca == null && x != y && x.Level == y.Level && x.Level < CatDatabase.MaxLevel && !x.Merging && !y.Merging &&
                        Vector2.Distance(Pos(x), Pos(y)) <= x.Radius + y.Radius + 0.02f) { ca = x; cb = y; }
            if (ca == null) continue;
            var src = HugEffect.Classify(MergeSource.Normal, ca, cb, board.Time, out bool hugCaused);
            if (energy.OnMerge(src) > 0) normal++; else skill++;
            merges++;
            var mid = (Pos(ca) + Pos(cb)) * 0.5f;
            int level = ca.Level + 1;
            board.Cats.Remove(ca); board.Cats.Remove(cb);
            UnityEngine.Object.DestroyImmediate(ca.gameObject); UnityEngine.Object.DestroyImmediate(cb.gameObject);
            var result = board.Add(level, mid.x, mid.y);
            if (hugCaused) HugEffect.MarkChain(result, board.Time);
        }
        Debug.Log("Hug board merges: " + merges + " (skill " + skill + ", normal " + normal + "), energy " + energy.Value);
        Check(merges >= 1, "Hug brought a pair together and it merged");
        Check(normal == 0 && energy.Value == 30, "every merge the pull caused (and its chain) gave no energy");
        board.Dispose(); Worlds.Remove(board);

        // ---- the same collision without Hug is a normal merge
        var plain = new World(false);
        var p1 = plain.Add(1, -0.5f, 0f);
        var p2 = plain.Add(1, 0.5f, 0f);
        p1.Velocity = new Vector2(2f, 0f); p2.Velocity = new Vector2(-2f, 0f);
        plain.Steps(30, hug: false);
        Check(HugEffect.Classify(MergeSource.Normal, p1, p2, plain.Time, out bool plainCaused) == MergeSource.Normal && !plainCaused, "two cats that collide on their own: normal merge, energy counts");
    }

    /// <summary>The pull goes through the physics engine only, and runs only while the game runs.</summary>
    static void SourceGuard()
    {
        string root = Application.dataPath + "/Scripts/Gameplay/";
        string hug = File.ReadAllText(root + "Hug.cs");
        Check(hug.Contains("AddForce(") && hug.Contains("ForceMode.Acceleration"), "the pull is a Rigidbody acceleration");
        Check(!Regex.IsMatch(hug, @"transform\.position\s*=|\.MovePosition|Body\.position\s*=|\.velocity\s*=|Velocity\s*="), "Hug never writes a position or a velocity directly");
        string gm = File.ReadAllText(root + "GameManager.cs");
        Check(Regex.IsMatch(gm, @"void FixedUpdate\(\)\s*\{\s*if \(_hug\.Active && !IsGameOver && !Paused\) _hug\.Step\("), "FixedUpdate steps Hug only while the game runs");
        Check(Regex.IsMatch(gm, @"HugEffect\.Classify\(source, a, b, Time\.time, out bool hugCaused\)"), "Merge classifies its cause");
    }

    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }
}
