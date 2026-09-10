using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Teshis araci. Bolum tasarimini degil, FIZIGIN KENDISINI olcer:
// tam guclu bir atis gercekte ne kadar is yapiyor?
// Fizik test sonucu "her atis tam 1 misket cikariyor, zincir yok" dedi.
// Bunun sebebi guc mu, mesafe mu, sekme mi, sonucu burada gorunur.
public static class MarbleYieldVerify
{
    private const float Mass = .05f, Drag = .6f, AngularDrag = .5f, Scale = .5f, Y = .25f;
    private const float MaxImpulse = .5f, Rest = .15f, ExitMargin = .25f;
    private const float Arena = 3.45f, LineZ = -4.2f;

    private static Scene scene;
    private static PhysicsScene physics;
    private static PhysicsMaterial marbleMat, groundMat;

    [MenuItem("MISKETR/Diagnose Marble Yield")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Play'i kapat."); return; }
        marbleMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/MarbleMaterialPhysics.asset");
        groundMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/GroundMaterialPhysics.asset");
        if (marbleMat == null || groundMat == null) { Debug.LogError("Physics material bulunamadi."); return; }

        var lines = new List<string>();
        void Say(string s) { lines.Add(s); Debug.Log(s); }

        Say("=== MISKET VERIMI TESHISI (cember yaricapi " + Arena + ", cikis siniri " + (Arena + ExitMargin) + ") ===");
        Say("Atici hizi tam gucte: " + (MaxImpulse / Mass).ToString("F1") + " m/s");
        Say("");
        Say("--- 1) TEK MISKET: merkeze d uzaklikta duran misketi tam ortadan vur ---");
        Say("     (cikmasi icin merkezden " + (Arena + ExitMargin) + " birim oteye gitmeli)");
        foreach (float d in new[] { 0f, .5f, 1f, 1.5f, 2f, 2.5f, 3f })
        {
            var r = SingleHit(d);
            Say("     d=" + d.ToString("F1") + " -> misket merkezden " + r.x.ToString("F2") +
                " birime gitti, " + (r.x > Arena + ExitMargin ? "CIKTI" : "ICERIDE KALDI") +
                "  | atici carpma sonrasi " + r.y.ToString("F2") + " m/s, merkezden " + r.z.ToString("F2"));
        }
        Say("");
        Say("--- 2) SIKI KUME: 0.62 arayla dizili misketlere tam guclu tek atis ---");
        foreach (int n in new[] { 3, 5, 7 })
        {
            int outCount = Cluster(n);
            Say("     " + n + " misketlik siraya tek atis -> " + outCount + " misket cikti");
        }
        Say("");
        Say("--- 2b) YERLESIM DENEMELERI: ayni misket sayisi, farkli dizilim, tek tam guclu atis ---");
        Say("     DIK SIRA (aticiya dik, bugunku bolumlerdeki dizilim):");
        foreach (int n in new[] { 3, 5 }) Say("        " + n + " misket -> " + Layout(Row(n, .62f, .6f)) + " cikti");
        Say("     DERIN KOLON (atis yonu boyunca arka arkaya):");
        foreach (int n in new[] { 3, 5 }) Say("        " + n + " misket -> " + Layout(Column(n, .62f, .6f)) + " cikti");
        Say("     DERIN KOLON, sifir bosluk (0.50 arayla, misketler degiyor):");
        foreach (int n in new[] { 3, 5 }) Say("        " + n + " misket -> " + Layout(Column(n, .5f, .6f)) + " cikti");
        Say("     UCGEN ISTAKA (bilardo dizilimi, ucu aticiya bakiyor):");
        foreach (int rows in new[] { 3, 4 }) Say("        " + (rows * (rows + 1) / 2) + " misket -> " + Layout(Rack(rows, .62f, .6f)) + " cikti");
        Say("     KENARA YAKIN YAY (merkezden 2.8, sadece sıyırmak yetiyor mu):");
        foreach (int n in new[] { 3, 5 }) Say("        " + n + " misket -> " + Layout(Arc(n, 2.8f, 55f)) + " cikti");
        Say("     KENARA YAKIN DERIN KOLON (merkezden 1.9'dan 3.1'e):");
        foreach (int n in new[] { 3, 5 }) Say("        " + n + " misket -> " + Layout(Column(n, .62f, 1.9f)) + " cikti");
        Say("");
        Say("--- 2c) SEKME (bounciness) ZINCIRI ACIYOR MU? ayni dizilim, farkli sekme ---");
        Say("     (bugunku deger 0.4 · gercek cam misket ~0.9)");
        foreach (float b in new[] { .4f, .6f, .75f, .85f, .95f })
        {
            Say("     bounciness " + b.ToString("F2") + ":");
            Say("        dik sira 5     -> " + Layout(Row(5, .62f, .6f), b) + " cikti");
            Say("        derin kolon 5  -> " + Layout(Column(5, .62f, .6f), b) + " cikti");
            Say("        ucgen istaka 10-> " + Layout(Rack(4, .62f, .6f), b) + " cikti");
            Say("        kenar yayi 5   -> " + Layout(Arc(5, 2.8f, 55f), b) + " cikti");
        }
        Say("");
        Say("--- 2d) GUC x DIZILIM: fazla enerjinin gidecek yeri varsa ne oluyor ---");
        Say("     (bugunku guc x1.0 = 10 m/s)");
        foreach (float mul in new[] { 1f, 1.4f, 2f, 2.8f })
        {
            float imp = MaxImpulse * mul;
            Say("     guc x" + mul.ToString("F1") + " (" + (imp / Mass).ToString("F0") + " m/s):");
            Say("        sekme 0.40 | sira5=" + Layout(Row(5, .62f, .6f), .4f, imp) +
                " kolon5=" + Layout(Column(5, .62f, .6f), .4f, imp) +
                " istaka10=" + Layout(Rack(4, .62f, .6f), .4f, imp) +
                " yay5=" + Layout(Arc(5, 2.8f, 55f), .4f, imp));
            Say("        sekme 0.85 | sira5=" + Layout(Row(5, .62f, .6f), .85f, imp) +
                " kolon5=" + Layout(Column(5, .62f, .6f), .85f, imp) +
                " istaka10=" + Layout(Rack(4, .62f, .6f), .85f, imp) +
                " yay5=" + Layout(Arc(5, 2.8f, 55f), .85f, imp));
        }
        Say("");
        Say("--- 3) GUC ve SEKME denemeleri: d=1.5'teki tek misket ---");
        foreach (float mul in new[] { 1f, 1.5f, 2f, 3f })
            Say("     impulse x" + mul.ToString("F1") + " (" + (MaxImpulse * mul / Mass).ToString("F0") + " m/s) -> misket " +
                SingleHit(1.5f, MaxImpulse * mul).x.ToString("F2") + " birime gitti");
        foreach (float b in new[] { .4f, .7f, .9f })
            Say("     bounciness " + b.ToString("F1") + " -> misket " + SingleHit(1.5f, MaxImpulse, b).x.ToString("F2") + " birime gitti");
        foreach (float drag in new[] { .6f, .4f, .25f })
            Say("     linearDamping " + drag.ToString("F2") + " -> misket " + SingleHit(1.5f, MaxImpulse, -1f, drag).x.ToString("F2") + " birime gitti");

        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/MarbleYield.txt", lines);
        Debug.Log("YIELD_DONE: Logs/MarbleYield.txt");
    }

    // x = hedef misketin merkezden son uzakligi, y = aticinin carpma sonrasi hizi, z = aticinin son uzakligi
    private static Vector3 SingleHit(float distance, float impulse = MaxImpulse, float bounciness = -1f, float drag = -1f)
    {
        Open(bounciness, drag, out var mat, out float useDrag);
        try
        {
            var target = Marble(new Vector3(0, Y, distance), mat, useDrag);
            var shooter = Marble(new Vector3(0, Y, LineZ), mat, useDrag);
            shooter.AddForce(Vector3.forward * impulse, ForceMode.Impulse);
            float afterImpact = -1f; bool touched = false;
            for (int f = 0; f < 900; f++)
            {
                physics.Simulate(.02f);
                if (!touched && target.linearVelocity.magnitude > .2f) { touched = true; afterImpact = shooter.linearVelocity.magnitude; }
                if (f > 25 && shooter.linearVelocity.magnitude < Rest && target.linearVelocity.magnitude < Rest) break;
            }
            return new Vector3(Flat(target.position), afterImpact, Flat(shooter.position));
        }
        finally { Close(); }
    }

    private static int Cluster(int count)
    {
        Open(-1f, -1f, out var mat, out float drag);
        try
        {
            var list = new List<Rigidbody>();
            for (int i = 0; i < count; i++)
                list.Add(Marble(new Vector3((i - (count - 1) * .5f) * .62f, Y, .6f), mat, drag));
            var shooter = Marble(new Vector3(0, Y, LineZ), mat, drag);
            shooter.AddForce(Vector3.forward * MaxImpulse, ForceMode.Impulse);
            var outSide = new bool[count];
            for (int f = 0; f < 900; f++)
            {
                physics.Simulate(.02f);
                bool rest = shooter.linearVelocity.magnitude < Rest;
                for (int i = 0; i < count; i++)
                {
                    if (Flat(list[i].position) > Arena + ExitMargin) outSide[i] = true;
                    if (list[i].linearVelocity.magnitude > Rest) rest = false;
                }
                if (f > 25 && rest) break;
            }
            int n = 0; foreach (bool o in outSide) if (o) n++;
            return n;
        }
        finally { Close(); }
    }

    private static float Flat(Vector3 p) => new Vector2(p.x, p.z).magnitude;

    private static Vector3[] Row(int n, float gap, float z)
    { var a = new Vector3[n]; for (int i = 0; i < n; i++) a[i] = new Vector3((i - (n - 1) * .5f) * gap, Y, z); return a; }

    private static Vector3[] Column(int n, float gap, float z)
    { var a = new Vector3[n]; for (int i = 0; i < n; i++) a[i] = new Vector3(0, Y, z + i * gap); return a; }

    private static Vector3[] Rack(int rows, float gap, float z)
    {
        var list = new List<Vector3>();
        for (int r = 0; r < rows; r++)
            for (int i = 0; i <= r; i++)
                list.Add(new Vector3((i - r * .5f) * gap, Y, z + r * gap * .87f));
        return list.ToArray();
    }

    private static Vector3[] Arc(int n, float radius, float spanDegrees)
    {
        var a = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? .5f : i / (float)(n - 1);
            float deg = 90f + Mathf.Lerp(-spanDegrees * .5f, spanDegrees * .5f, t);
            a[i] = new Vector3(Mathf.Cos(deg * Mathf.Deg2Rad) * radius, Y, Mathf.Sin(deg * Mathf.Deg2Rad) * radius);
        }
        return a;
    }

    // Verilen yerlesime, en iyi tek tam guclu atisi arar ve kac misket ciktigini doner.
    private static int Layout(Vector3[] spots, float bounciness = -1f, float impulse = MaxImpulse)
    {
        int best = 0;
        foreach (float startX in new[] { -1.3f, 0f, 1.3f })
            foreach (var target in spots)
                foreach (float offset in new[] { -.22f, 0f, .22f })
                {
                    Open(bounciness, -1f, out var mat, out float drag);
                    try
                    {
                        var list = new List<Rigidbody>();
                        foreach (var s in spots) list.Add(Marble(s, mat, drag));
                        var shooter = Marble(new Vector3(startX, Y, LineZ), mat, drag);
                        var dir = target - shooter.position; dir.y = 0;
                        if (dir.sqrMagnitude < .01f) continue;
                        dir.Normalize();
                        var perp = Vector3.Cross(Vector3.up, dir);
                        var aim = target + perp * offset - shooter.position; aim.y = 0; aim.Normalize();
                        shooter.AddForce(aim * impulse, ForceMode.Impulse);
                        var outSide = new bool[list.Count];
                        for (int f = 0; f < 900; f++)
                        {
                            physics.Simulate(.02f);
                            bool rest = shooter.linearVelocity.magnitude < Rest;
                            for (int i = 0; i < list.Count; i++)
                            {
                                if (Flat(list[i].position) > Arena + ExitMargin) outSide[i] = true;
                                if (list[i].linearVelocity.magnitude > Rest) rest = false;
                            }
                            if (f > 25 && rest) break;
                        }
                        int n = 0; foreach (bool o in outSide) if (o) n++;
                        if (n > best) best = n;
                    }
                    finally { Close(); }
                }
        return best;
    }

    private static void Open(float bounciness, float drag, out PhysicsMaterial mat, out float useDrag)
    {
        scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        physics = scene.GetPhysicsScene();
        var ground = Prim(PrimitiveType.Cube, new Vector3(0, -.25f, 0), new Vector3(40, .5f, 40));
        ground.GetComponent<Collider>().sharedMaterial = groundMat;
        useDrag = drag < 0f ? Drag : drag;
        if (bounciness < 0f) { mat = marbleMat; return; }
        mat = new PhysicsMaterial("teshis");
        mat.dynamicFriction = marbleMat.dynamicFriction; mat.staticFriction = marbleMat.staticFriction;
        mat.frictionCombine = marbleMat.frictionCombine; mat.bounceCombine = PhysicsMaterialCombine.Maximum;
        mat.bounciness = bounciness;
    }

    private static void Close()
    {
        if (scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        scene = default;
    }

    private static GameObject Prim(PrimitiveType kind, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(kind);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = pos; go.transform.localScale = scale;
        return go;
    }

    private static Rigidbody Marble(Vector3 pos, PhysicsMaterial mat, float drag)
    {
        var go = Prim(PrimitiveType.Sphere, pos, Vector3.one * Scale);
        go.GetComponent<Collider>().sharedMaterial = mat;
        var b = go.AddComponent<Rigidbody>();
        b.mass = Mass; b.linearDamping = drag; b.angularDamping = AngularDrag;
        b.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        return b;
    }
}
