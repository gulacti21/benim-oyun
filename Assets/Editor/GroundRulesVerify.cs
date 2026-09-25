using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// HARİTA 2 ZEMİN KURALLARI: sabit ölçümü (Measure) ve kural testleri (RunChecks).
// GroundRules.Step'in kendisi, görünmez preview sahnede gerçek misket fiziğiyle,
// oyundaki gibi her fizik adımından önce çağrılır.
public static class GroundRulesVerify
{
    private const float Mass = .05f, Drag = .6f, AngularDrag = .5f, Scale = .5f, Y = .25f, MaxImpulse = .65f, Dt = .02f;
    private static Scene scene;
    private static PhysicsScene physics;
    private static PhysicsMaterial marbleMat, groundMat;
    private static int checks;
    private static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception("CHECK FAILED: " + message); }

    [MenuItem("MISKETR/Measure Ground Rules")]
    public static void Measure()
    {
        var lines = new List<string>();
        void Say(string s) { lines.Add(s); Debug.Log(s); }
        float sand0 = GroundRules.SandDrag, mud0 = GroundRules.MudDrag, pit0 = GroundRules.PitCaptureSpeed;
        try
        {
            Say("=== KUM: atıcı düz gider, yolunda 4.5 önde r=1.0 kum (atıcı z=-4.5'ten) ===");
            Say("ek drag   güç   kumsuz yol   kumlu yol   kumdan çıkış hızı / giriş hızı");
            foreach (float d in new[] { 1.5f, 3f, 5f })
                foreach (float p in new[] { .6f, 1f })
                {
                    GroundRules.SandDrag = d;
                    var free = Roll(p, null, Vector2.zero, out _, out _);
                    var sand = Roll(p, new[] { new ZoneSpot(ZoneKind.Sand, 0, 0, 1f) }, Vector2.zero, out float vin, out float vout);
                    Say(d.ToString("F1").PadLeft(7) + ("%" + Mathf.RoundToInt(p * 100)).PadLeft(7) + free.z.ToString("F2").PadLeft(12) + sand.z.ToString("F2").PadLeft(12) +
                        "      " + (vin > 0 ? (vout / vin).ToString("F2") : "-"));
                }
            GroundRules.SandDrag = sand0;
            Say("");
            Say("=== ÇAMUR: r=0.6 çamur 4.5 önde; misket çamura girdikten sonra kaç birim ilerliyor ===");
            foreach (float d in new[] { 8f, 14f, 20f })
            {
                GroundRules.MudDrag = d;
                string row = "  ek drag " + d.ToString("F0").PadLeft(3) + " :";
                foreach (float p in new[] { .45f, .6f, .8f, 1f })
                {
                    var end = Roll(p, new[] { new ZoneSpot(ZoneKind.Mud, 0, 0, .6f) }, Vector2.zero, out _, out _);
                    row += "  %" + Mathf.RoundToInt(p * 100) + " -> " + (end.z + 4.5f - 3.9f).ToString("F2");
                }
                Say(row + "   (çamurun çapı 1.2; <1.2 = içinde saplandı)");
            }
            GroundRules.MudDrag = mud0;
            Say("");
            Say("=== EĞİM: yana ivme; 4.5 ilerideki hedef hizasında yana kayma (birim) ===");
            foreach (float a in new[] { .4f, .8f, 1.2f })
            {
                string row = "  ivme " + a.ToString("F1") + " :";
                foreach (float p in new[] { .45f, .6f, .8f, 1f })
                {
                    float side = SideAt(p, new Vector2(a, 0), 4.5f);
                    row += "  %" + Mathf.RoundToInt(p * 100) + " -> " + side.ToString("F2");
                }
                Say(row);
            }
            Say("");
            Say("=== ÇUKUR: r=0.35 çukur; misket çukurun üstünden şu hızla geçerse ===");
            foreach (float v in new[] { 1f, 1.5f, 2f, 2.5f, 3f, 4f })
                Say("  " + v.ToString("F1") + " m/s  -> çukura varış hızı " + ArrivalSpeed(v, 1.2f).ToString("F2") + "  (" + (ArrivalSpeed(v, 1.2f) < pit0 ? "düşer" : "geçer") + ", eşik " + pit0 + ")");
            Say("  (Atış sonrası saha içinde yuvarlanan misketlerin tipik hızı 0.5-2.5 m/s — Faz 2 ölçümü)");
        }
        finally { GroundRules.SandDrag = sand0; GroundRules.MudDrag = mud0; GroundRules.PitCaptureSpeed = pit0; }
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/GroundRules.txt", lines);
        Debug.Log("GROUND_MEASURE_DONE: Logs/GroundRules.txt");
    }

    // Atıcı z=-4.5'ten +z yönüne; bölüm verisi zones/slope ile. Son konumu döner.
    private static Vector3 Roll(float power, ZoneSpot[] zones, Vector2 slope, out float vin, out float vout)
    {
        vin = 0f; vout = 0f;
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.zones = zones; level.slope = slope;
        Open();
        try
        {
            var shot = Marble(new Vector3(0, Y, -4.5f));
            shot.AddForce(Vector3.forward * MaxImpulse * power, ForceMode.Impulse);
            var empty = new List<Rigidbody>();
            bool inside = false;
            for (int f = 0; f < 600; f++)
            {
                bool now = zones != null && zones.Length > 0 && GroundRules.Inside(zones[0], Vector3.zero, shot.position);
                if (now && !inside) vin = shot.linearVelocity.magnitude;
                if (!now && inside) vout = shot.linearVelocity.magnitude;
                inside = now;
                GroundRules.Step(level, Vector3.zero, empty, shot, Dt, null);
                physics.Simulate(Dt);
                if (f > 25 && shot.linearVelocity.magnitude < .05f) break;
            }
            return shot.position;
        }
        finally { Close(); UnityEngine.Object.DestroyImmediate(level); }
    }

    private static float SideAt(float power, Vector2 slope, float ahead)
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.slope = slope;
        Open();
        try
        {
            var shot = Marble(new Vector3(0, Y, -4.5f));
            shot.AddForce(Vector3.forward * MaxImpulse * power, ForceMode.Impulse);
            var empty = new List<Rigidbody>();
            for (int f = 0; f < 600; f++)
            {
                GroundRules.Step(level, Vector3.zero, empty, shot, Dt, null);
                physics.Simulate(Dt);
                if (shot.position.z >= ahead - 4.5f) return shot.position.x;
                if (f > 25 && shot.linearVelocity.magnitude < .05f) return float.NaN;
            }
            return float.NaN;
        }
        finally { Close(); UnityEngine.Object.DestroyImmediate(level); }
    }

    private static float ArrivalSpeed(float speed, float distance)
    {
        Open();
        try
        {
            var m = Marble(new Vector3(0, Y, -distance));
            m.linearVelocity = Vector3.forward * speed;
            for (int f = 0; f < 300; f++) { physics.Simulate(Dt); if (m.position.z >= 0f) return m.linearVelocity.magnitude; }
            return 0f;
        }
        finally { Close(); }
    }

    public static int RunChecks()
    {
        checks = 0;
        Check(Resources.Load<Shader>("Mahalle/Zone") != null, "Ground: zone shader included");
        var level = ScriptableObject.CreateInstance<LevelData>();
        try
        {
            Check(!GroundRules.HasRules(level), "Ground: plain level has no rules");
            for (int i = 0; i < Campaign.Count; i++) Check(!GroundRules.HasRules(Campaign.Database.Get(i)), "Ground: Mahalle level " + i + " untouched");

            // Kum: aynı atış kumda daha kısa gider, ama geçebilir.
            level.zones = new[] { new ZoneSpot(ZoneKind.Sand, 0, 0, 1f) };
            float free = Travel(level, 1f, false), sand = Travel(level, 1f, true);
            Check(sand < free - .5f, "Ground: sand shortens a full shot (" + free.ToString("F2") + " -> " + sand.ToString("F2") + ")");
            Check(sand > 1f, "Ground: full shot crosses the sand patch");

            // Çamur: giren misket çamurda saplanır (her güçte).
            level.zones = new[] { new ZoneSpot(ZoneKind.Mud, 0, 0, .6f) };
            foreach (float p in new[] { .6f, 1f })
            {
                float end = Travel(level, p, true);
                Check(end < .6f, "Ground: mud stops the marble inside it at %" + Mathf.RoundToInt(p * 100) + " (end " + end.ToString("F2") + ")");
            }

            // Eğim: hareket eden misket sapar, duran misket kaymaz.
            level.zones = null; level.slope = new Vector2(.8f, 0);
            float side = SideAt(1f, level.slope, 4.5f);
            Check(side > .05f, "Ground: slope bends a moving marble (" + side.ToString("F2") + ")");
            Open();
            try
            {
                var rest = Marble(new Vector3(1, Y, 1));
                var list = new List<Rigidbody> { rest };
                for (int f = 0; f < 200; f++) { GroundRules.Step(level, Vector3.zero, list, null, Dt, null); physics.Simulate(Dt); }
                Check(Vector3.Distance(rest.position, new Vector3(1, Y, 1)) < .01f, "Ground: slope never moves a resting marble");
            }
            finally { Close(); }
            // Hafifçe dürtülen misket eğimde durur (eğim sürtünmeyi yenmez).
            Open();
            try
            {
                level.slope = new Vector2(1.2f, 0);
                var nudged = Marble(new Vector3(0, Y, 0));
                nudged.linearVelocity = new Vector3(.8f, 0, 0);
                var list = new List<Rigidbody> { nudged };
                for (int f = 0; f < 400; f++) { GroundRules.Step(level, Vector3.zero, list, null, Dt, null); physics.Simulate(Dt); }
                Check(nudged.linearVelocity.magnitude < .05f, "Ground: a nudged marble stops on the slope");
                Check(nudged.position.x < 2.5f, "Ground: a nudged marble does not roll off (" + nudged.position.x.ToString("F2") + ")");
            }
            finally { Close(); }

            // Çukur: yavaş hedef düşer (kinematic, collider kapalı, çukur merkezinde), hızlı geçer.
            level.slope = Vector2.zero;
            level.zones = new[] { new ZoneSpot(ZoneKind.Pit, 0, 0, .35f) };
            foreach (var speedCase in new[] { (1.5f, true), (5f, false) })
            {
                Open();
                try
                {
                    var m = Marble(new Vector3(0, Y, -1.2f));
                    m.linearVelocity = Vector3.forward * speedCase.Item1;
                    var list = new List<Rigidbody> { m };
                    Rigidbody got = null;
                    for (int f = 0; f < 200 && got == null; f++) { GroundRules.Step(level, Vector3.zero, list, null, Dt, b => got = b); physics.Simulate(Dt); }
                    Check((got != null) == speedCase.Item2, "Ground: pit " + (speedCase.Item2 ? "captures slow" : "lets fast pass") + " marble at " + speedCase.Item1);
                    if (got != null)
                    {
                        Check(got.isKinematic && !got.GetComponent<Collider>().enabled, "Ground: captured marble is out of play");
                        Check(new Vector2(got.position.x, got.position.z).magnitude < .01f, "Ground: captured marble sits in the hole");
                        GroundRules.Release(got);
                        Check(!got.isKinematic && got.GetComponent<Collider>().enabled, "Ground: release restores marble (measurement)");
                    }
                }
                finally { Close(); }
            }
            // Atıcı çukura düşmez, sadece durur.
            Open();
            try
            {
                var shooter = Marble(new Vector3(0, Y, -1.2f));
                shooter.linearVelocity = Vector3.forward * 1.5f;
                var none = new List<Rigidbody>(); bool any = false;
                for (int f = 0; f < 200; f++) { GroundRules.Step(level, Vector3.zero, none, shooter, Dt, b => any = true); physics.Simulate(Dt); }
                Check(!any && !shooter.isKinematic, "Ground: shooter is never captured");
                Check(new Vector2(shooter.position.x, shooter.position.z).magnitude < .4f, "Ground: shooter stops at the pit");
            }
            finally { Close(); }

            // Merkez kaydırması: bölgeler çemberin merkezine göre.
            level.zones = new[] { new ZoneSpot(ZoneKind.Sand, 1f, 0, .5f) };
            Check(GroundRules.Inside(level.zones[0], new Vector3(0, 0, 2), new Vector3(1, 0, 2)), "Ground: zones follow arena centre");
        }
        finally { UnityEngine.Object.DestroyImmediate(level); }
        Debug.Log("GROUND_VERIFY_OK: " + checks + " checks.");
        return checks;
    }

    // Tam güç atıcının z=-4.5'ten sonra geldiği z (0 = bölgenin merkezi).
    private static float Travel(LevelData level, float power, bool withRules)
    {
        Open();
        try
        {
            var shot = Marble(new Vector3(0, Y, -4.5f));
            shot.AddForce(Vector3.forward * MaxImpulse * power, ForceMode.Impulse);
            var empty = new List<Rigidbody>();
            for (int f = 0; f < 600; f++)
            {
                if (withRules) GroundRules.Step(level, Vector3.zero, empty, shot, Dt, null);
                physics.Simulate(Dt);
                if (f > 25 && shot.linearVelocity.magnitude < .05f) break;
            }
            return shot.position.z;
        }
        finally { Close(); }
    }

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
}
