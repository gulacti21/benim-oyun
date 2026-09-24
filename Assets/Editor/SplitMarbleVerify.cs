using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// BÖLÜNEN MİSKET: eşik ölçümü (Measure) ve kural testleri (RunChecks).
// Oyundaki SplitMarble bileşeni görünmez preview sahnede gerçek misket fiziğiyle denenir.
public static class SplitMarbleVerify
{
    private const float Mass = .05f, Drag = .6f, AngularDrag = .5f, Scale = .5f, Y = .25f, MaxImpulse = .65f;
    private static Scene scene;
    private static PhysicsScene physics;
    private static PhysicsMaterial marbleMat, groundMat;
    private static int checks;
    private static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception("CHECK FAILED: " + message); }

    [MenuItem("MISKETR/Measure Split Threshold")]
    public static void Measure()
    {
        var lines = new List<string>();
        void Say(string s) { lines.Add(s); Debug.Log(s); }
        Say("=== BÖLÜNME ÖLÇÜMÜ: atıcı karpuza doğrudan (karpuz atıcıdan 4.5 önde = saha ortası) ===");
        Say("güç   temas hızı   bölünürse parçaların gittiği yol (A / B)   bütün kalırsa karpuzun yolu");
        foreach (float p in new[] { .3f, .45f, .6f, .8f, 1f })
        {
            float hit; float wholeTravel = Whole(p, out hit);
            Vector2 pieces = Pieces(p);
            Say(("%" + Mathf.RoundToInt(p * 100)).PadRight(6) + hit.ToString("F2").PadLeft(8) + "        " +
                pieces.x.ToString("F2") + " / " + pieces.y.ToString("F2") + "                         " + wholeTravel.ToString("F2"));
        }
        Say("");
        Say("Saha yarıçapı 3.2-3.5; merkezdeki misketin çıkması için ~3.5 yol gerekir.");
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/SplitThreshold.txt", lines);
        Debug.Log("SPLIT_MEASURE_DONE: Logs/SplitThreshold.txt");
    }

    private static float Whole(float power, out float hit)
    {
        Open();
        try
        {
            var start = new Vector3(0, Y, 0);
            var s = Split(start); s.MeasureOnly = true;
            var shot = Marble(new Vector3(0, Y, -4.5f), Scale, Mass);
            shot.AddForce(Vector3.forward * MaxImpulse * power, ForceMode.Impulse);
            Run(500);
            hit = s.LastHitSpeed;
            return Vector3.Distance(Flat(s.transform.position), Flat(start));
        }
        finally { Close(); }
    }

    private static Vector2 Pieces(float power)
    {
        Open();
        try
        {
            var start = new Vector3(0, Y, 0);
            var s = Split(start);
            Rigidbody a = null, b = null; s.Split += (w, x, y) => { a = x; b = y; };
            var shot = Marble(new Vector3(0, Y, -4.5f), Scale, Mass);
            shot.AddForce(Vector3.forward * MaxImpulse * power, ForceMode.Impulse);
            Run(500);
            if (a == null) return new Vector2(-1, -1);
            return new Vector2(Vector3.Distance(Flat(a.position), Flat(start)), Vector3.Distance(Flat(b.position), Flat(start)));
        }
        finally { Close(); }
    }

    public static int RunChecks()
    {
        checks = 0;
        Load();

        // Veri: karpuz 2 değerinde, TotalMarbles buna göre.
        var lvl = ScriptableObject.CreateInstance<LevelData>();
        lvl.marbles = new[] { new MarbleSpot(0, 0), new MarbleSpot(1, 0, MarbleKind.Split), new MarbleSpot(-1, 0, MarbleKind.Ice) };
        Check(lvl.TotalMarbles() == 3, "Split: counts as one in TotalMarbles");
        UnityEngine.Object.DestroyImmediate(lvl);
        Check(SplitMarble.Worth == 1, "Split: whole watermelon worth one");

        // Zayıf darbe: bölünmez, normal misket gibi itilir.
        Open();
        try
        {
            var start = new Vector3(0, Y, 1.2f);
            var s = Split(start); int n = 0; s.Split += (w, a, b) => n++;
            var shot = Marble(new Vector3(0, Y, .6f), Scale, Mass);
            shot.linearVelocity = Vector3.forward * SplitMarble.SplitSpeed * .7f;
            Run(200);
            Check(n == 0 && !s.Done && s.gameObject.activeSelf, "Split: weak hit keeps it whole");
            Check(Vector3.Distance(Flat(s.transform.position), Flat(start)) > .2f, "Split: weak hit still pushes it");
        }
        finally { Close(); }

        // Sert darbe: iki parça, yarım kütle, x0.7 ölçek, hız devralır, iki yana açılır.
        Open();
        try
        {
            var start = new Vector3(0, Y, 0f);
            var s = Split(start);
            Rigidbody a = null, b = null; int n = 0;
            Vector3 whole = Vector3.zero;
            s.Split += (w, x, y) => { n++; a = x; b = y; };
            var shot = Marble(new Vector3(0, Y, -3f), Scale, Mass);
            shot.AddForce(Vector3.forward * MaxImpulse, ForceMode.Impulse);
            for (int f = 0; f < 200 && n == 0; f++) physics.Simulate(.02f);
            Check(n == 1 && s.Done && !s.gameObject.activeSelf, "Split: hard hit splits once and hides the whole");
            Check(a != null && b != null && a.gameObject.activeSelf && b.gameObject.activeSelf, "Split: two live pieces");
            Check(Mathf.Approximately(a.mass, Mass * SplitMarble.PieceMass) && Mathf.Approximately(b.mass, Mass * SplitMarble.PieceMass), "Split: half mass pieces");
            Check(Mathf.Abs(a.transform.localScale.x - Scale * SplitMarble.PieceScale) < 1e-4f, "Split: pieces scaled x0.7");
            foreach (var piece in new[] { a, b })
            {
                var sm = piece.GetComponent<SplitMarble>();
                Check(sm == null || sm.IsPiece, "Split: pieces cannot split again");
            }
            Check(a.gameObject.scene == scene && b.gameObject.scene == scene, "Split: pieces stay in the same physics scene");
            var va = a.linearVelocity; va.y = 0; var vb = b.linearVelocity; vb.y = 0;
            Check(va.magnitude > .5f && vb.magnitude > .5f, "Split: pieces inherit speed");
            float angle = Vector3.Angle(va, vb);
            Check(angle > SplitMarble.PieceSpread * 1.5f && angle < SplitMarble.PieceSpread * 2.5f, "Split: pieces open to both sides (" + angle.ToString("F1") + " deg)");
            Check(Vector3.Dot(va + vb, s.HitDirection) > 0f, "Split: pieces keep going along the hit");
            Check(Vector3.Dot(Vector3.Cross(s.HitDirection, va).normalized, Vector3.Cross(s.HitDirection, vb).normalized) < 0f, "Split: one piece each side");

            // Parça sert vurulsa da bölünmez (tek kademe).
            Run(300);
            int before = CountBodies();
            var hammer = Marble(a.position - Vector3.forward * .8f, Scale, Mass);
            hammer.linearVelocity = Vector3.forward * SplitMarble.SplitSpeed * 2.5f;
            Run(200);
            Check(CountBodies() == before + 1, "Split: second hard hit makes no new pieces");
        }
        finally { Close(); }

        // Zemin darbesi bölmez (yüksekten düşse bile).
        Open();
        try
        {
            var s = Split(new Vector3(0, 1.2f, 0));
            s.GetComponent<Rigidbody>().linearVelocity = Vector3.down * SplitMarble.SplitSpeed * 2f;
            Run(100);
            Check(!s.Done, "Split: ground impact never splits");
        }
        finally { Close(); }

        Debug.Log("SPLIT_VERIFY_OK: " + checks + " checks.");
        return checks;
    }

    private static int CountBodies()
    {
        int n = 0;
        foreach (var root in scene.GetRootGameObjects()) if (root.activeSelf && root.GetComponent<Rigidbody>() != null) n++;
        return n;
    }

    private static Vector3 Flat(Vector3 p) => new Vector3(p.x, 0, p.z);
    private static void Load()
    {
        if (marbleMat == null) marbleMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/MarbleMaterialPhysics.asset");
        if (groundMat == null) groundMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/GroundMaterialPhysics.asset");
    }
    private static void Open()
    {
        Load();
        scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        physics = scene.GetPhysicsScene();
        var ground = Prim(PrimitiveType.Cube, new Vector3(0, -.25f, 0), new Vector3(40, .5f, 40));
        ground.GetComponent<Collider>().sharedMaterial = groundMat;
    }
    private static void Close()
    {
        if (scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        scene = default;
    }
    private static void Run(int frames) { for (int i = 0; i < frames; i++) physics.Simulate(.02f); }
    private static GameObject Prim(PrimitiveType kind, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(kind);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = pos; go.transform.localScale = scale;
        return go;
    }
    private static Rigidbody Marble(Vector3 pos, float scale, float mass)
    {
        var go = Prim(PrimitiveType.Sphere, pos, Vector3.one * scale);
        go.GetComponent<Collider>().sharedMaterial = marbleMat;
        var b = go.AddComponent<Rigidbody>();
        b.mass = mass; b.linearDamping = Drag; b.angularDamping = AngularDrag;
        b.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        return b;
    }
    private static SplitMarble Split(Vector3 pos) => Marble(pos, Scale, Mass).gameObject.AddComponent<SplitMarble>();
}
