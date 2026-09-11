using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// ONLINE DUELLO — FIZIK TARAMASI
//
// Soru: online macta misket ne kadar zor cikmali?
// Hedef olculebilir: ORTALAMA bir atis yaklasik 1 misket cikarsin.
// O zaman 6'sar tur, 14 misketlik cemberde mac sonuna kadar basa bas gider.
// Cok yuksekse tek atis maci bitirir, cok dusukse hicbir sey olmaz.
//
// Yontem: dort degiskeni TEK TEK tarar (kutle, atis gucu, surtunme, sekme).
// Ayni anda ikisini birden degistirmek hangisinin etki ettigini gizler --
// kampanyada bu hatayi bir kez yaptik, "sekme zinciri baslatir" diye
// ugrastik, olcum asil kaldiracin atis gucu oldugunu gosterdi.
public static class DuelPhysicsVerify
{
    // Kampanyayla birebir ayni temel degerler.
    private const float BaseMass = .05f, BaseDrag = .6f, BaseAngularDrag = .5f;
    private const float BaseScale = .5f, BaseImpulse = .65f;
    private const float TargetY = .25f, RestSpeed = .15f, ExitMargin = .25f;
    private const int MaxFrames = 700;

    // Duello arenasi: cember, engelsiz. Tek oyunculudan farkli olarak
    // icinde duvar yok -- sadece misketler ve acilar.
    private static float ArenaSize = 3.2f;
    private const float ShooterZ = -4.2f, ShooterHalfWidth = 2.4f;

    private static Scene scene;
    private static PhysicsScene physics;
    private static PhysicsMaterial groundMat, marbleMat, testMat;
    private static readonly List<string> report = new List<string>();
    private static int simCount;

    [MenuItem("MISKETR/Verify Duel Physics")]
    public static void Run()
    {
        marbleMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/MarbleMaterialPhysics.asset");
        groundMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/GroundMaterialPhysics.asset");
        if (marbleMat == null || groundMat == null) { Debug.LogError("DUEL_PHYSICS_ABORT: fizik materyalleri yok."); return; }

        report.Clear(); simCount = 0;
        report.Add("DUELLO FIZIK TARAMASI · cember " + ArenaSize + " · 14 misket · hedef: atis basina ~1.0");
        report.Add("");
        report.Add("Her satir: o ayarla yapilan butun ornek atislarin ortalamasi ve en iyisi.");
        report.Add("ORT = vasat oyuncunun bekleyecegi sonuc. EN IYI = ustanin tek atista cikarabildigi.");
        report.Add("");

        try
        {
            Sweep("TEMEL", new[] { 1f }, (v) => new Setting());

            Sweep("KUTLE (x kat)", new[] { 1f, 1.4f, 1.8f, 2.4f, 3.2f },
                  v => new Setting { massMul = v });

            Sweep("ATIS GUCU (impulse)", new[] { .40f, .50f, .65f, .80f, 1.00f },
                  v => new Setting { impulse = v });

            Sweep("SURTUNME (x kat)", new[] { .5f, 1f, 1.5f, 2f, 3f },
                  v => new Setting { frictionMul = v });

            Sweep("SEKME (bounciness)", new[] { .1f, .2f, .3f, .45f, .6f },
                  v => new Setting { bounciness = v });

            Sweep("SURUKLENME (drag x kat)", new[] { .6f, 1f, 1.5f, 2.2f },
                  v => new Setting { dragMul = v });

            Sweep("CEMBER BUYUKLUGU", new[] { 2.4f, 2.7f, 3.0f, 3.2f, 3.5f },
                  v => new Setting { arena = v });

            Sweep("ATIS GUCU ince ayar", new[] { .80f, .85f, .90f, .95f },
                  v => new Setting { impulse = v });

            // Tek degiskenle hedefe varilabiliyor ama his de onemli: sadece guc
            // artirmak "sert vurdum, dagildi" hissi verir. Sekme ve sürtünme
            // ekleyince misketler daha cok yayilir, taktik alani acilir.
            report.Add("BIRLESIK ADAYLAR (his icin: guc + sekme/surtunme)");
            Aday("A · guc .85 + sekme .45", new Setting { impulse = .85f, bounciness = .45f });
            Aday("B · guc .85 + surtunme .7x", new Setting { impulse = .85f, frictionMul = .7f });
            Aday("C · guc .80 + sekme .5 + surtunme .7x", new Setting { impulse = .80f, bounciness = .5f, frictionMul = .7f });
            Aday("D · guc .90 + cember 3.0", new Setting { impulse = .90f, arena = 3.0f });
            Aday("E · guc .85 + sekme .45 + cember 3.0", new Setting { impulse = .85f, bounciness = .45f, arena = 3.0f });
            report.Add("");
        }
        finally
        {
            Cleanup();
            if (testMat != null) { UnityEngine.Object.DestroyImmediate(testMat); testMat = null; }
        }

        report.Add("");
        report.Add("# " + simCount + " atis simule edildi.");
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/DuelPhysicsVerify.txt", report);
        Debug.Log("DUEL_PHYSICS_DONE: Logs/DuelPhysicsVerify.txt");
    }

    private static void Aday(string ad, Setting s)
    {
        var (avg, best, zero) = Measure(s);
        report.Add(string.Format("  {0,-38} ORT {1,5:0.00}  EN IYI {2}  bos atis %{3,3:0}{4}",
                                 ad, avg, best, zero * 100f,
                                 Math.Abs(avg - 1f) < .15f ? "   <-- hedefe yakin" : ""));
        Debug.Log(ad + " -> ort " + avg.ToString("0.00"));
    }

    private class Setting
    {
        public float massMul = 1f, dragMul = 1f, frictionMul = 1f, impulse = BaseImpulse, bounciness = -1f;
        public float arena = 3.2f;
    }

    private static void Sweep(string title, float[] values, Func<float, Setting> build)
    {
        report.Add(title);
        foreach (float v in values)
        {
            var s = build(v);
            var (avg, best, zero) = Measure(s);
            report.Add(string.Format("  {0,6:0.00} ->  ORT {1,5:0.00}  EN IYI {2}  bos atis %{3,3:0}{4}",
                                     v, avg, best, zero * 100f,
                                     Math.Abs(avg - 1f) < .12f ? "   <-- hedefe yakin" : ""));
            Debug.Log(title + " " + v.ToString("0.00") + " -> ort " + avg.ToString("0.00") + " en iyi " + best);
        }
        report.Add("");
    }

    // Bir ayarla ornek atislar yapar. Nisan noktalari ve gucler taranir;
    // sonuc TEK bir atisin ortalama verimi.
    private static (float avg, int best, float zeroRate) Measure(Setting s)
    {
        ArenaSize = s.arena;
        Build(s);
        float total = 0f; int best = 0, shots = 0, zero = 0;

        float[] powers = { 1f, .75f, .5f };
        for (int p = 0; p < 5; p++)
        {
            float sx = Mathf.Lerp(-ShooterHalfWidth, ShooterHalfWidth, p / 4f);
            for (int a = 0; a < 5; a++)
            {
                float ax = Mathf.Lerp(-1.4f, 1.4f, a / 4f);
                foreach (float pw in powers)
                {
                    int outCount = Shoot(s, new Vector3(sx, TargetY, ShooterZ), new Vector3(ax, TargetY, .8f), pw);
                    total += outCount; shots++; simCount++;
                    if (outCount > best) best = outCount;
                    if (outCount == 0) zero++;
                }
            }
        }
        Cleanup();
        return (shots == 0 ? 0f : total / shots, best, shots == 0 ? 0f : zero / (float)shots);
    }

    // Duello dizilisi: iki oyuncunun 7'ser misketi cembere yayilmis.
    // Gercek macta oyuncular kendi dizecek; burada temsili, makul sik bir dizilis.
    private static readonly float[,] Layout =
    {
        {  0f, 1.0f }, { -0.62f, 1.0f }, { 0.62f, 1.0f },
        { -0.31f, 1.54f }, { 0.31f, 1.54f }, { -0.93f, 1.54f }, { 0.93f, 1.54f },
        {  0f, 2.08f }, { -0.62f, 2.08f }, { 0.62f, 2.08f },
        { -1.24f, 1.0f }, { 1.24f, 1.0f }, { -0.31f, 0.46f }, { 0.31f, 0.46f }
    };

    private static readonly List<Rigidbody> bodies = new List<Rigidbody>();
    private static Vector3[] home;

    private static void Build(Setting s)
    {
        Cleanup();
        scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        physics = scene.GetPhysicsScene();
        bodies.Clear();

        var ground = Make(PrimitiveType.Cube, new Vector3(0, -.25f, 0), new Vector3(40, .5f, 40));
        ground.GetComponent<Collider>().sharedMaterial = groundMat;

        if (testMat == null) testMat = new PhysicsMaterial("Duello (test)");
        testMat.dynamicFriction = marbleMat.dynamicFriction * s.frictionMul;
        testMat.staticFriction = marbleMat.staticFriction * s.frictionMul;
        testMat.bounciness = s.bounciness < 0f ? marbleMat.bounciness : s.bounciness;
        testMat.frictionCombine = marbleMat.frictionCombine;
        testMat.bounceCombine = marbleMat.bounceCombine;

        for (int i = 0; i < Layout.GetLength(0); i++)
            bodies.Add(MakeMarble(new Vector3(Layout[i, 0], TargetY, Layout[i, 1]),
                                  BaseScale, BaseMass * s.massMul, BaseDrag * s.dragMul, testMat));

        home = new Vector3[bodies.Count];
        for (int i = 0; i < bodies.Count; i++) home[i] = bodies[i].position;
    }

    // Tek atis simule eder, cemberi terk eden misket sayisini dondurur.
    // Her atistan once dizilis basa alinir: atislar birbirini etkilemesin,
    // olculen sey tek bir atisin verimi olsun.
    private static int Shoot(Setting s, Vector3 from, Vector3 aim, float power)
    {
        for (int i = 0; i < bodies.Count; i++)
        {
            bodies[i].position = home[i];
            bodies[i].linearVelocity = Vector3.zero;
            bodies[i].angularVelocity = Vector3.zero;
        }

        var shot = MakeMarble(from, BaseScale, BaseMass * s.massMul, BaseDrag * s.dragMul, testMat);
        Vector3 dir = (aim - from); dir.y = 0f; dir.Normalize();
        shot.AddForce(dir * (s.impulse * power), ForceMode.Impulse);

        float exit = ArenaSize + ExitMargin;
        for (int f = 0; f < MaxFrames; f++)
        {
            physics.Simulate(.02f);
            bool moving = shot.linearVelocity.magnitude > RestSpeed;
            if (!moving)
                for (int i = 0; i < bodies.Count && !moving; i++)
                    if (bodies[i].linearVelocity.magnitude > RestSpeed) moving = true;
            if (!moving) break;
        }

        int outCount = 0;
        for (int i = 0; i < bodies.Count; i++)
        {
            var p = bodies[i].position;
            if (new Vector2(p.x, p.z).magnitude > exit) outCount++;
        }

        UnityEngine.Object.DestroyImmediate(shot.gameObject);
        return outCount;
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
        body.mass = mass; body.linearDamping = drag; body.angularDamping = BaseAngularDrag;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        return body;
    }

    private static void Cleanup()
    {
        if (scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        scene = default; bodies.Clear();
    }
}
