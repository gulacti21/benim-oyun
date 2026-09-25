using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// BUZLU MİSKET: eşik ölçümü (Measure) ve kural testleri (RunChecks).
// Oyundaki IceShell bileşeninin kendisi, görünmez preview sahnede gerçek misket
// fiziğiyle (kütle .05, drag .6, ölçek .5, tam güç .65) denenir.
public static class IceMarbleVerify
{
    private const float Mass = .05f, Drag = .6f, AngularDrag = .5f, Scale = .5f, Y = .25f, MaxImpulse = .65f;
    private static Scene scene;
    private static PhysicsScene physics;
    private static PhysicsMaterial marbleMat, groundMat;
    private static int checks;
    private static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception("CHECK FAILED: " + message); }

    // --- ölçüm: atış gücü x mesafe -> buza çarpma hızı ---
    [MenuItem("MISKETR/Measure Ice Threshold")]
    public static void Measure()
    {
        var lines = new List<string>();
        void Say(string s) { lines.Add(s); Debug.Log(s); }
        float[] powers = { .1f, .15f, .2f, .3f, .45f, .6f, .8f, 1f };
        float[] distances = { 1.5f, 3f, 4.5f, 6f, 7.5f };
        Say("=== BUZ EŞİĞİ ÖLÇÜMÜ: atıcı doğrudan buza, temas doğrultusundaki hız (m/s) ===");
        Say("güç \\ mesafe  " + string.Join("  ", Array.ConvertAll(distances, d => d.ToString("F1").PadLeft(5))));
        foreach (float p in powers)
        {
            string row = ("%" + Mathf.RoundToInt(p * 100)).PadRight(13);
            foreach (float d in distances) row += "  " + Direct(p, d).ToString("F2").PadLeft(5);
            Say(row);
        }
        Say("");
        Say("=== ZİNCİR: atıcı önce normal misketi (1.2 önde) vurur, o buza çarpar ===");
        foreach (float p in powers)
            Say(("%" + Mathf.RoundToInt(p * 100)).PadRight(13) + "  " + Chain(p, 3f).ToString("F2").PadLeft(5) + "  (buz atıcıdan 3.0+1.2)");
        Say("");
        Say("=== YAVAŞ YUVARLANAN MİSKET (atıştan sonra kenardan dönen) ===");
        foreach (float v in new[] { .5f, 1f, 1.5f, 2f, 3f })
            Say("  " + v.ToString("F1") + " m/s ile gelen misket -> buza " + Rolling(v).ToString("F2") + " m/s");
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/IceThreshold.txt", lines);
        Debug.Log("ICE_MEASURE_DONE: Logs/IceThreshold.txt");
    }

    private static float Direct(float power, float distance)
    {
        Open();
        try
        {
            var ice = Ice(new Vector3(0, Y, distance), 999f);
            var shot = Marble(new Vector3(0, Y, 0));
            shot.AddForce(Vector3.forward * MaxImpulse * power, ForceMode.Impulse);
            Run(400);
            return ice.LastHitSpeed;
        }
        finally { Close(); }
    }

    private static float Chain(float power, float distance)
    {
        Open();
        try
        {
            Marble(new Vector3(0, Y, distance));
            var ice = Ice(new Vector3(0, Y, distance + 1.2f), 999f);
            var shot = Marble(new Vector3(0, Y, 0));
            shot.AddForce(Vector3.forward * MaxImpulse * power, ForceMode.Impulse);
            Run(400);
            return ice.LastHitSpeed;
        }
        finally { Close(); }
    }

    private static float Rolling(float speed)
    {
        Open();
        try
        {
            var ice = Ice(new Vector3(0, Y, .7f), 999f);
            var m = Marble(new Vector3(0, Y, 0));
            m.linearVelocity = Vector3.forward * speed;
            Run(200);
            return ice.LastHitSpeed;
        }
        finally { Close(); }
    }

    // --- kurallar (MahalleVerify.Run içinden) ---
    public static int RunChecks()
    {
        checks = 0;
        Load();
        Check(IceShell.BreakSpeed > 0f, "Ice: threshold set");
        Check(Resources.Load<Shader>("Mahalle/Ice") != null, "Ice: shader included in Resources");

        // 1) Kabuk kinematic, hareketsiz başlar.
        Open();
        try
        {
            var ice = Ice(new Vector3(0, Y, 2f), IceShell.BreakSpeed);
            Check(ice.Intact && ice.GetComponent<Rigidbody>().isKinematic, "Ice: starts frozen");
            Run(50);
            Check(Vector3.Distance(ice.transform.position, new Vector3(0, Y, 2f)) < .001f, "Ice: never drifts while idle");
        }
        finally { Close(); }

        // 2) Zayıf darbe: kırmaz, kıpırdatmaz; atıcı seker.
        Open();
        try
        {
            var start = new Vector3(0, Y, 2f);
            var ice = Ice(start, IceShell.BreakSpeed);
            var shot = Marble(new Vector3(0, Y, 1f));
            shot.linearVelocity = Vector3.forward * IceShell.BreakSpeed * .6f;
            Run(150);
            Check(ice.Intact, "Ice: weak hit leaves shell intact");
            Check(ice.LastHitSpeed > 0f && ice.LastHitSpeed < IceShell.BreakSpeed, "Ice: weak hit registered below threshold");
            Check(Vector3.Distance(ice.transform.position, start) < .001f, "Ice: weak hit does not move marble");
            Check(shot.position.z < 1.6f, "Ice: shooter bounces off the shell");
        }
        finally { Close(); }

        // 3) Sert darbe: kırar ama misket yerinde kalır; ikinci vuruş hareket ettirir.
        Open();
        try
        {
            var start = new Vector3(0, Y, 3f);
            var ice = Ice(start, IceShell.BreakSpeed);
            int broken = 0; ice.Broken += _ => broken++;
            var shot = Marble(new Vector3(0, Y, 0f));
            shot.AddForce(Vector3.forward * MaxImpulse, ForceMode.Impulse);
            Run(400);
            Check(!ice.Intact && broken == 1, "Ice: full-power hit breaks shell once");
            Check(!ice.GetComponent<Rigidbody>().isKinematic, "Ice: broken marble is dynamic");
            float drift = Vector3.Distance(Flat(ice.transform.position), Flat(start));
            Check(drift < .05f, "Ice: breaking hit leaves marble in place (drift " + drift.ToString("F3") + ")");
            Check(ice.GetComponent<Rigidbody>().collisionDetectionMode == CollisionDetectionMode.ContinuousDynamic, "Ice: detection restored after break");

            var second = Marble(new Vector3(0, Y, 0f));
            shot.gameObject.SetActive(false);
            second.AddForce(Vector3.forward * MaxImpulse * .6f, ForceMode.Impulse);
            Run(400);
            Check(Vector3.Distance(Flat(ice.transform.position), Flat(start)) > .4f, "Ice: second hit moves the thawed marble");
        }
        finally { Close(); }

        // 4) Eşik gerçekten eşik: hemen altı kırmaz, hemen üstü kırar (temas hızıyla).
        foreach (float mul in new[] { .85f, 1.25f })
        {
            Open();
            try
            {
                var ice = Ice(new Vector3(0, Y, 1.2f), IceShell.BreakSpeed);
                var shot = Marble(new Vector3(0, Y, .6f));
                shot.linearVelocity = Vector3.forward * IceShell.BreakSpeed * mul;
                Run(100);
                Check(ice.Intact == (ice.LastHitSpeed < IceShell.BreakSpeed), "Ice: threshold decides x" + mul);
                Check(ice.Intact == (mul < 1f), "Ice: x" + mul + " " + (mul < 1f ? "keeps" : "breaks") + " shell (hit " + ice.LastHitSpeed.ToString("F2") + ")");
            }
            finally { Close(); }
        }

        // 5) Freeze/Thaw ölçüm aracı için geri sarılabilir.
        Open();
        try
        {
            var ice = Ice(new Vector3(0, Y, 1f), IceShell.BreakSpeed);
            ice.Thaw(); Check(!ice.Intact && !ice.GetComponent<Rigidbody>().isKinematic, "Ice: thaw");
            ice.Freeze(false); Check(ice.Intact && ice.GetComponent<Rigidbody>().isKinematic, "Ice: refreeze");
            ice.Thaw(); Check(ice.GetComponent<Rigidbody>().collisionDetectionMode == CollisionDetectionMode.ContinuousDynamic, "Ice: refreeze keeps original detection");
        }
        finally { Close(); }

        Debug.Log("ICE_VERIFY_OK: " + checks + " checks.");
        return checks;
    }

    // --- yardımcılar ---
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

    private static Rigidbody Marble(Vector3 pos)
    {
        var go = Prim(PrimitiveType.Sphere, pos, Vector3.one * Scale);
        go.GetComponent<Collider>().sharedMaterial = marbleMat;
        var b = go.AddComponent<Rigidbody>();
        b.mass = Mass; b.linearDamping = Drag; b.angularDamping = AngularDrag;
        b.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        return b;
    }

    // threshold 999: ölçümde kırılmasın, sadece hız kaydedilsin.
    private static IceShell Ice(Vector3 pos, float threshold)
    {
        var b = Marble(pos);
        var ice = b.gameObject.AddComponent<IceShell>();
        ice.Freeze(false);
        if (threshold > 100f) ice.MeasureOnly = true;
        return ice;
    }
}
