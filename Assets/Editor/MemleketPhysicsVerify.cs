using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// HARİTA 2 FİZİK ÖLÇÜMÜ. ParkPhysicsVerify'ın yöntemi (görünmez preview sahne,
// gerçek misket fiziği, açgözlü arama) + Memleket'in kuralları: buzlu misket
// (IceShell), karpuz (SplitMarble), kum/çamur/eğim/çukur (GroundRules.Step).
// Oyundaki bileşenlerin ve kuralın KENDİSİ kullanılır, taklit yok.
//
// İki ölçü:
//  1) TAVAN / TEMİZLENEBİLİRLİK — açgözlü arama: her atışta en çok değer çıkaran atış.
//     ParkPhysicsVerify'ın "temizlenebilirlik" ölçüsüyle aynı (Harita 1 merdiveni bu).
//  2) GEÇME ORANI — "sıradan oyuncu": her atışta rastgele 10 aday (±2° nişan hatası,
//     rastgele güç) dener, en iyisini seçer. 150 oyun, sabit tohum. 1/2/3 yıldız oranı,
//     atış başına misket, buz kırma ve bölünme sayıları.
// Ayrıca GÜÇLER (Baş Misket, Demir) ve ÖZEL MİSKETLER ile tavan (üst sınır).
//
// Harita 1'e dokunmaz. Yöntemin doğruluğu için Harita 1'den örnek bölümleri de
// ölçer ve AllLevelsPhysicsVerify.txt'deki tavanla karşılaştırır (Calibrate).
public static class MemleketPhysicsVerify
{
    private const float MarbleMass = .05f, MarbleDrag = .6f, MarbleAngularDrag = .5f, MarbleScale = .5f;
    private const float TargetY = .25f, MaxImpulse = .65f, RestSpeed = .15f, ExitMargin = .25f;
    private const float ObstacleY = .22f, ObstacleHeight = .44f, FallbackHalfWidth = 2.6f, Dt = .02f;
    private const int MaxFrames = 600;
    private const int LinePositions = 7;
    private static readonly float[] AimOffsets = { 0f, -.22f, .22f };
    private static readonly float[] Powers = { 1f, .8f, .6f, .45f };
    private const int CasualGames = 150, CasualSamples = 10;
    private const float CasualAimNoise = 2f;

    private class Variant { public string name; public float massMul = 1f, scaleMul = 1f, impulseMul = 1f, dragMul = 1f; public bool own; public float frictionMul = 1f, bounce = -1f; public PhysicsMaterialCombine fc, bc; }
    private static readonly Variant Normal = new Variant { name = "NORMAL" };
    private static readonly Variant[] Extra =
    {
        new Variant { name = "GUC-BUYUK", scaleMul = 1.65f, massMul = 2f, impulseMul = 2.3f },
        new Variant { name = "GUC-DEMIR", massMul = 2.6f, impulseMul = 2.5f },
        new Variant { name = "AGIR", massMul = 1.25f, impulseMul = 1.12f },
        new Variant { name = "INCE", scaleMul = .8f },
        new Variant { name = "SEKICI", own = true, bounce = .65f, fc = PhysicsMaterialCombine.Average, bc = PhysicsMaterialCombine.Maximum },
        new Variant { name = "KAYGAN", own = true, frictionMul = .65f, dragMul = .8f, fc = PhysicsMaterialCombine.Minimum, bc = PhysicsMaterialCombine.Maximum },
    };

    // Bir misketin durumu. Parçalar ayrı kayıt olur.
    private struct Rec
    {
        public Vector3 pos; public MarbleKind kind; public bool gone, lost, ice; public float scale, mass; public int worth;
    }
    private struct Candidate { public Vector3 start, dir; public float power; public string label; }
    private struct Outcome { public List<Rec> state; public int gained; public int iceBreaks, splits; public float value; }

    private static Scene scene;
    private static PhysicsScene physics;
    private static PhysicsMaterial marbleMat, groundMat, variantMat;
    private static readonly List<GameObject> live = new List<GameObject>();
    private static readonly List<int> liveRec = new List<int>();
    private static LevelData level;
    private static long sims;

    // ---------------------------------------------------------------- girişler
    public static void BatchAll() { Measure(Range(60, 60), "MemleketPhysics.txt", true); }
    public static void BatchQuick() { Measure(Range(60, 60), "MemleketPhysicsQuick.txt", false); }
    public static void Calibrate() { Measure(new[] { 0, 5, 18, 30, 44, 59 }, "MemleketCalibration.txt", false); }
    // Kuyruk/komut satırı: -memleketLevels 60,61,70
    public static void BatchSome()
    {
        var args = Environment.GetCommandLineArgs(); var list = new List<int>();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-memleketLevels") foreach (var p in args[i + 1].Split(',')) list.Add(int.Parse(p));
        Measure(list.ToArray(), "MemleketPhysicsSome.txt", false);
    }

    // ATIŞ EĞRİSİ: açgözlü arama her atışta bir öncekinden bağımsız en iyiyi seçtiği için
    // 7 atışlık tek koşu, 1..7 atışın hepsinin tavanını verir. Atış hakkı buradan seçilir.
    public static void BatchCurve()
    {
        marbleMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/MarbleMaterialPhysics.asset");
        groundMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/GroundMaterialPhysics.asset");
        var rows = new List<string>(); var t0 = DateTime.Now;
        var only = new List<int>(); var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-memleketLevels") foreach (var p in args[i + 1].Split(',')) only.Add(int.Parse(p));
        string outFile = only.Count > 0 ? "Logs/MemleketCurveSome.tsv" : "Logs/MemleketCurve.tsv";
        for (int g = 60; g < 120; g++)
        {
            if (only.Count > 0 && !only.Contains(g)) continue;
            level = Maps.Get(g);
            int keep = level.shotCount; level.shotCount = 7;
            Open();
            try
            {
                var state = Initial(); var cum = new List<int>(); int total = 0;
                for (int turn = 0; turn < 7; turn++)
                {
                    Outcome best = default; best.value = -1f;
                    foreach (var c in Candidates(state)) { var o = Play(state, c, Normal); if (o.value > best.value) best = o; }
                    if (best.state != null) { state = best.state; total += best.gained; }
                    cum.Add(total);
                }
                string row = g + "\t" + level.TotalMarbles() + "\t" + string.Join(",", cum);
                rows.Add(row); Debug.Log("MEMLEKET_CURVE " + row);
            }
            finally { Close(); level.shotCount = keep; }
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllLines(outFile, rows);
        Debug.Log("MEMLEKET_CURVE_DONE " + sims + " sim, " + (DateTime.Now - t0).TotalMinutes.ToString("F1") + " dk");
    }

    private static int[] Range(int from, int n) { var a = new int[n]; for (int i = 0; i < n; i++) a[i] = from + i; return a; }

    // ---------------------------------------------------------------- ölçüm
    public static List<string> Measure(int[] levels, string file, bool variants)
    {
        marbleMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/MarbleMaterialPhysics.asset");
        groundMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/GroundMaterialPhysics.asset");
        var lines = new List<string>();
        var rows = new List<string>();
        var t0 = DateTime.Now;
        foreach (int g in levels)
        {
            level = Maps.Get(g);
            long before = sims;
            Open();
            try
            {
                int total = level.TotalMarbles();
                var greedy = Greedy(Normal, out string route);
                var casual = Casual(g);
                string var = "";
                if (variants)
                    foreach (var v in Extra) { var r = Greedy(v, out _); var += " " + v.name + "=" + r.gained; }
                float clear = greedy.gained / (float)total;
                string label = Label(g);
                string row = label + " | " + total + " misket " + level.shotCount + " atış | TAVAN " + greedy.gained + " (%" + Mathf.RoundToInt(clear * 100) + ")" +
                             " | hedef " + level.oneStarTarget + "/" + level.twoStarTarget + "/" + level.threeStarTarget +
                             " | GEÇME %" + Pct(casual.pass) + " 2y %" + Pct(casual.two) + " 3y %" + Pct(casual.three) +
                             " | ort " + casual.avg.ToString("F1") + " (" + (casual.avg / level.shotCount).ToString("F2") + "/atış)" +
                             " | buz " + casual.ice.ToString("F1") + " bölünme " + casual.split.ToString("F1") + " çukur " + casual.pit.ToString("F1") +
                             " | sim " + (sims - before) + (variants ? " |" + var : "");
                lines.Add(row); lines.Add("    yol: " + route);
                rows.Add(g + "\t" + total + "\t" + level.shotCount + "\t" + greedy.gained + "\t" + casual.pass.ToString("F3") + "\t" + casual.two.ToString("F3") + "\t" + casual.three.ToString("F3") + "\t" + casual.avg.ToString("F2") + "\t" + casual.maxSeen +
                         "\t" + casual.ice.ToString("F2") + "\t" + casual.split.ToString("F2") + "\t" + casual.pit.ToString("F2") + "\t" + string.Join(",", casual.scores));
                Debug.Log("MEMLEKET_PHYSICS " + row);
            }
            finally { Close(); }
        }
        lines.Add("# " + sims + " simülasyon, " + (DateTime.Now - t0).TotalMinutes.ToString("F1") + " dk. Açgözlü tavan alt sınırdır.");
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/" + file, lines);
        File.WriteAllLines("Logs/" + Path.GetFileNameWithoutExtension(file) + ".tsv", rows);
        Debug.Log("MEMLEKET_PHYSICS_DONE: Logs/" + file);
        return lines;
    }

    private static string Pct(float f) => Mathf.RoundToInt(f * 100).ToString();

    private static string Label(int g)
    {
        return (g < 60 ? "H1 " : "") + L.Up(Maps.DistrictName(g / 12)) + " " + (g % 12 + 1).ToString("00") + " (#" + g + ")";
    }

    // Açgözlü: her atışta en değerli adayı seç.
    private static Outcome Greedy(Variant v, out string route)
    {
        var state = Initial();
        var moves = new List<string>();
        int total = 0, ice = 0, split = 0;
        for (int turn = 0; turn < level.shotCount; turn++)
        {
            Outcome best = default; best.value = -1f; string bestLabel = "";
            foreach (var c in Candidates(state))
            {
                var o = Play(state, c, v);
                if (o.value > best.value) { best = o; bestLabel = c.label + "@" + c.start.x.ToString("F2") + " %" + Mathf.RoundToInt(c.power * 100); }
            }
            if (best.state == null) break;
            state = best.state; total += best.gained; ice += best.iceBreaks; split += best.splits;
            moves.Add(bestLabel + " (+" + best.gained + ")");
        }
        route = string.Join(" ; ", moves);
        return new Outcome { state = state, gained = total, iceBreaks = ice, splits = split };
    }

    private struct CasualResult { public float pass, two, three, avg, ice, split, pit; public int maxSeen; public int[] scores; }

    // Sıradan oyuncu: her atışta rastgele 10 aday (nişan hatası ile), en iyisi.
    private static CasualResult Casual(int g)
    {
        var rng = new System.Random(1000 + g * 7919);   // sabit tohum: tekrarlanabilir
        var r = new CasualResult { scores = new int[CasualGames] };
        int pass = 0, two = 0, three = 0; float sum = 0, ice = 0, split = 0, pit = 0;
        for (int game = 0; game < CasualGames; game++)
        {
            var state = Initial(); int got = 0;
            for (int turn = 0; turn < level.shotCount; turn++)
            {
                var all = Candidates(state);
                if (all.Count == 0) break;
                Outcome best = default; best.value = -1f;
                for (int k = 0; k < CasualSamples; k++)
                {
                    var c = all[rng.Next(all.Count)];
                    float noise = (float)(rng.NextDouble() * 2 - 1) * CasualAimNoise;
                    c.dir = Quaternion.Euler(0, noise, 0) * c.dir;
                    c.power = Mathf.Clamp01(c.power * (.85f + (float)rng.NextDouble() * .3f));
                    var o = Play(state, c, Normal);
                    if (o.value > best.value) best = o;
                }
                state = best.state; got += best.gained; ice += best.iceBreaks; split += best.splits;
            }
            foreach (var rec in state) if (rec.lost) pit++;
            sum += got; if (got > r.maxSeen) r.maxSeen = got; r.scores[game] = got;
            if (got >= level.oneStarTarget) pass++;
            if (got >= level.twoStarTarget) two++;
            if (got >= level.threeStarTarget) three++;
        }
        r.pass = pass / (float)CasualGames; r.two = two / (float)CasualGames; r.three = three / (float)CasualGames;
        r.avg = sum / CasualGames; r.ice = ice / CasualGames; r.split = split / CasualGames; r.pit = pit / CasualGames;
        return r;
    }

    private static List<Rec> Initial()
    {
        var list = new List<Rec>();
        foreach (var s in level.marbles)
            list.Add(new Rec { pos = new Vector3(s.x, TargetY, s.z), kind = s.kind, ice = s.kind == MarbleKind.Ice, scale = MarbleScale, mass = MarbleMass, worth = s.kind == MarbleKind.Split ? 2 : 1 });
        return list;
    }

    private static List<Candidate> Candidates(List<Rec> state)
    {
        var list = new List<Candidate>();
        var seen = new HashSet<long>();
        float halfWidth = level.shooterHalfWidth > 0f ? level.shooterHalfWidth : FallbackHalfWidth;
        float lineZ = level.shooterStartPosition.z;
        for (int p = 0; p < LinePositions; p++)
        {
            float t = p / (float)(LinePositions - 1);
            var start = new Vector3(level.shooterOffsetX + Mathf.Lerp(-halfWidth, halfWidth, t), MarbleScale * .5f, lineZ);
            for (int i = 0; i < state.Count; i++)
            {
                if (state[i].gone || state[i].lost) continue;
                var to = state[i].pos - start; to.y = 0;
                if (to.sqrMagnitude < .01f) continue;
                var perp = Vector3.Cross(Vector3.up, to.normalized);
                foreach (float off in AimOffsets)
                    foreach (float pw in Powers)
                        Add(list, seen, p, start, state[i].pos + perp * off, pw, "m" + i);
                if (level.obstacles == null) continue;
                foreach (var w in level.obstacles)
                {
                    var normal = Quaternion.Euler(0, w.angle, 0) * (w.width >= w.depth ? Vector3.forward : Vector3.right);
                    float half = (w.width >= w.depth ? w.depth : w.width) * .5f + .25f;
                    foreach (float sign in new[] { -1f, 1f })
                    {
                        var plane = new Vector3(w.x, TargetY, w.z) + normal * (half * sign);
                        foreach (float pw in new[] { 1f, .7f })
                            Add(list, seen, p, start, state[i].pos - 2 * Vector3.Dot(state[i].pos - plane, normal) * normal, pw, "s" + i);
                    }
                }
            }
        }
        return list;
    }

    private static void Add(List<Candidate> list, HashSet<long> seen, int slot, Vector3 start, Vector3 aim, float power, string label)
    {
        var dir = aim - start; dir.y = 0;
        if (dir.sqrMagnitude < .01f) return;
        dir.Normalize();
        if (dir.z <= 0f) return;
        long key = (slot * 8L + Mathf.RoundToInt(power * 20f)) * 100000L + Mathf.RoundToInt(Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg * 2f);
        if (!seen.Add(key)) return;
        list.Add(new Candidate { start = start, dir = dir, power = power, label = label });
    }

    // Bir atış: sahayı durumdan kur, at, sonucu oku.
    private static Outcome Play(List<Rec> state, Candidate c, Variant v)
    {
        sims++;
        Clear();
        var recs = new List<Rec>(state);
        var bodies = new List<Rigidbody>();
        for (int i = 0; i < recs.Count; i++)
        {
            if (recs[i].gone || recs[i].lost) continue;
            var b = MakeMarble(recs[i].pos, recs[i].scale, recs[i].mass, MarbleDrag, marbleMat);
            if (recs[i].kind == MarbleKind.Ice && recs[i].ice) b.gameObject.AddComponent<IceShell>().Freeze(false);
            if (recs[i].kind == MarbleKind.Split && recs[i].worth == 2)
            {
                int parent = i;
                b.gameObject.AddComponent<SplitMarble>().Split += (w, a, bb) => { pendingSplits.Add((parent, a, bb)); };
            }
            live.Add(b.gameObject); liveRec.Add(i); bodies.Add(b);
        }
        pendingSplits.Clear();
        var mat = marbleMat;
        if (v.own)
        {
            if (variantMat == null) variantMat = new PhysicsMaterial("Ozel misket (test)");
            variantMat.dynamicFriction = marbleMat.dynamicFriction * v.frictionMul;
            variantMat.staticFriction = marbleMat.staticFriction * v.frictionMul;
            variantMat.bounciness = v.bounce < 0f ? marbleMat.bounciness : v.bounce;
            variantMat.frictionCombine = v.fc; variantMat.bounceCombine = v.bc;
            mat = variantMat;
        }
        var start = c.start; start.y = MarbleScale * v.scaleMul * .5f;
        var shot = MakeMarble(start, MarbleScale * v.scaleMul, MarbleMass * v.massMul, MarbleDrag * v.dragMul, mat);
        live.Add(shot.gameObject); liveRec.Add(-1);
        shot.AddForce(c.dir * (MaxImpulse * v.impulseMul * c.power), ForceMode.Impulse);

        float limit = level.arenaSize + ExitMargin;
        var gone = new Dictionary<Rigidbody, bool>();
        var lost = new HashSet<Rigidbody>();
        int splits = 0;
        var pieceRecs = new List<(Rigidbody body, int parent)>();
        for (int frame = 0; frame < MaxFrames; frame++)
        {
            GroundRules.Step(level, Vector3.zero, bodies, shot, Dt, b => lost.Add(b));
            physics.Simulate(Dt);
            if (pendingSplits.Count > 0)
            {
                foreach (var s in pendingSplits)
                {
                    splits++;
                    pieceRecs.Add((s.a, s.parent)); pieceRecs.Add((s.b, s.parent));
                    bodies.Add(s.a); bodies.Add(s.b); live.Add(s.a.gameObject); live.Add(s.b.gameObject); liveRec.Add(-2); liveRec.Add(-2);
                }
                pendingSplits.Clear();
            }
            bool rest = shot.linearVelocity.magnitude < RestSpeed;
            foreach (var b in bodies)
            {
                if (b == null || !b.gameObject.activeSelf || lost.Contains(b)) continue;
                var p = b.position;
                if (p.x * p.x + p.z * p.z > limit * limit) gone[b] = true;
                if (!b.isKinematic && b.linearVelocity.magnitude > RestSpeed) rest = false;
            }
            if (frame > 25 && rest) break;
        }

        // Sonucu kayda çevir.
        var result = new List<Rec>();
        int gained = 0, iceBreaks = 0;
        for (int k = 0; k < live.Count; k++)
        {
            int ri = liveRec[k];
            if (ri < 0) continue;
            var b = live[k].GetComponent<Rigidbody>();
            var r = recs[ri];
            var sm = live[k].GetComponent<SplitMarble>();
            if (sm != null && sm.Done && !sm.IsPiece) { recs[ri] = new Rec { gone = true, worth = 0 }; continue; }   // parçalara dönüştü
            if (lost.Contains(b)) { r.lost = true; }
            else if (gone.ContainsKey(b)) { r.gone = true; gained += r.worth; }
            r.pos = b.position; r.pos.y = r.scale * .5f;
            if (r.kind == MarbleKind.Ice && r.ice) { var ice = live[k].GetComponent<IceShell>(); if (ice != null && !ice.Intact) { r.ice = false; iceBreaks++; } }
            recs[ri] = r;
        }
        foreach (var r in recs) if (r.gone || r.lost || r.worth > 0) result.Add(r);
        foreach (var pr in pieceRecs)
        {
            var b = pr.body;
            var r = new Rec { kind = MarbleKind.Normal, scale = b.transform.localScale.x, mass = b.mass, worth = 1, pos = b.position };
            r.pos.y = r.scale * .5f;
            if (lost.Contains(b)) r.lost = true;
            else if (gone.ContainsKey(b)) { r.gone = true; gained += 1; }
            result.Add(r);
        }
        // Eşitlikte kalanları kenara yaklaştıranı seç (ParkPhysicsVerify ile aynı).
        float spread = 0f;
        foreach (var r in result) if (!r.gone && !r.lost) spread += new Vector2(r.pos.x, r.pos.z).magnitude;
        return new Outcome { state = result, gained = gained, iceBreaks = iceBreaks, splits = splits, value = gained + spread * .001f - Lost(result) * .01f };
    }

    private static int Lost(List<Rec> s) { int n = 0; foreach (var r in s) if (r.lost) n++; return n; }

    private static readonly List<(int parent, Rigidbody a, Rigidbody b)> pendingSplits = new List<(int, Rigidbody, Rigidbody)>();

    // ---------------------------------------------------------------- sahne
    private static void Open()
    {
        scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        physics = scene.GetPhysicsScene();
        var ground = Make(PrimitiveType.Cube, new Vector3(0, -.25f, 0), new Vector3(30, .5f, 30));
        ground.GetComponent<Collider>().sharedMaterial = groundMat;
        if (level.obstacles != null)
            foreach (var w in level.obstacles)
            {
                var wall = Make(PrimitiveType.Cube, new Vector3(w.x, ObstacleY, w.z), new Vector3(Mathf.Max(.2f, w.width), ObstacleHeight, Mathf.Max(.2f, w.depth)));
                wall.transform.rotation = Quaternion.Euler(0, w.angle, 0);
            }
    }

    private static void Clear()
    {
        foreach (var go in live) if (go != null) UnityEngine.Object.DestroyImmediate(go);
        live.Clear(); liveRec.Clear();
        // SplitMarble'ın parça kopyaları sahnede başıboş kalmasın.
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponent<Rigidbody>() != null) UnityEngine.Object.DestroyImmediate(root);
    }

    private static void Close()
    {
        Clear();
        if (scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        scene = default;
        if (variantMat != null) { UnityEngine.Object.DestroyImmediate(variantMat); variantMat = null; }
    }

    private static GameObject Make(PrimitiveType kind, Vector3 position, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(kind);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = position; go.transform.localScale = scale;
        return go;
    }

    private static Rigidbody MakeMarble(Vector3 position, float scale, float mass, float drag, PhysicsMaterial material)
    {
        var go = Make(PrimitiveType.Sphere, position, Vector3.one * scale);
        go.GetComponent<Collider>().sharedMaterial = material;
        var body = go.AddComponent<Rigidbody>();
        body.mass = mass; body.linearDamping = drag; body.angularDamping = MarbleAngularDrag;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        return body;
    }
}
