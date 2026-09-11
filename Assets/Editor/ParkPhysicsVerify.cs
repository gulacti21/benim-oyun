using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Bolumu Claude yerine makine oynar: gorunmez bir preview sahnede gercek misket
// fizigiyle binlerce atis dener ve "bu bolumden en fazla kac misket cikarilabiliyor"
// sorusunu cevaplar.
//
// Uc ayri soru sorulur, cunku tasarim kurali su:
//   1 yildiz (gecis)  -> normal misketle HER ZAMAN mumkun olmali. Yoksa oyuncu kilitlenir.
//   2 yildiz (kilit)  -> normal misketle mumkun olmali. Mahalle finalinde kilidi bu acar;
//                        ozel miskete baglanirsa ilerleme 400 boncugun arkasina kilitlenir.
//   3 yildiz (odul)   -> normal misketle imkansiz OLABILIR. Ozel misketlerin varlik sebebi.
//
// Arama acgozludur: her atista o anki en cok misketi cikaran atisi secer, bir atisi
// sonrakini kurmak icin feda etmez. Yani iyi bir insan oyuncudan ZAYIFTIR.
//   VAR = bu arama bir yol buldu. Alt sinir. "Dengeli" demek DEGILDIR.
//   YOK = bu arama bulamadi. "Imkansiz" demek DEGILDIR, "elle bak" demektir.
public static class ParkPhysicsVerify
{
    // --- oyunla birebir eslesen sabitler ---
    private const float MarbleMass = .05f;        // TargetMarble.prefab
    private const float MarbleDrag = .6f;         // TargetMarble.prefab
    private const float MarbleAngularDrag = .5f;  // TargetMarble.prefab
    private const float MarbleScale = .5f;        // TargetMarble.prefab
    private const float TargetY = .25f;
    private const float MaxImpulse = .65f;        // ShotController.maxShotImpulse (tam guc)
    private const float RestSpeed = .15f;         // MarbleArena.restSpeedThreshold
    private const float ExitMargin = .25f;        // MarbleArena.exitMargin
    private const float ObstacleY = .22f;         // MahalleWorld
    private const float ObstacleHeight = .44f;    // MahalleWorld

    // --- arama genisligi ---
    private const float FallbackHalfWidth = 2.6f; // shooterHalfWidth=0 ise FitToCamera ~2.6 (iPhone dikey)
    private const int LinePositions = 5;
    private const int MaxFrames = 600;            // 12 s @ 0.02
    private const int BatchPerTick = 25;
    private static readonly float[] AimOffsets = { 0f, -.22f, .22f }; // duz / kesme atis
    // Oyuncunun cekis gucu 0-1 arasi surekli. Bazi bolumlerin cozumu tam guc DEGIL:
    // engelli/koridorlu yerlerde kontrollu atis daha iyi, ve sert atis kalan misketleri
    // merkeze gomup sonraki atislari bozabiliyor. O yuzden guc de aranmali.
    private static readonly float[] Powers = { 1f, .7f, .45f };

    // --- misket turleri: SpecialMarblePhysics.Apply ile ayni ---
    private class Variant
    {
        public string name;
        public float massMul = 1f, scaleMul = 1f, impulseMul = 1f, dragMul = 1f;
        public bool ownMaterial;
        public float frictionMul = 1f, bounciness;
        public PhysicsMaterialCombine frictionCombine, bounceCombine;
    }

    private static readonly Variant[] Variants =
    {
        new Variant { name = "NORMAL" },
        new Variant { name = "AGIR",   massMul = 1.25f, impulseMul = 1.12f },
        new Variant { name = "INCE",   scaleMul = .8f },
        new Variant { name = "SEKICI", ownMaterial = true, bounciness = .65f,
                      frictionCombine = PhysicsMaterialCombine.Average, bounceCombine = PhysicsMaterialCombine.Maximum },
        new Variant { name = "KAYGAN", ownMaterial = true, frictionMul = .65f, dragMul = .8f, bounciness = -1f,
                      frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Maximum },
        // Ozel gucler: satin alinan misket degil, ucuz ve her zaman elde olan taktik secim.
        // "Normal misketle zor, dogru gucle acilir" bolumleri bunlarla olculur.
        new Variant { name = "GUC-BUYUK", scaleMul = 1.65f, massMul = 2f, impulseMul = 2.3f },
        new Variant { name = "GUC-DEMIR", massMul = 2.6f, impulseMul = 2.5f },
    };

    // --- calisma durumu ---
    private struct Candidate { public Vector3 start; public Vector3 dir; public float power; public string label; }
    private static readonly List<string> report = new List<string>();
    private static readonly List<string> moves = new List<string>();
    private static readonly List<string> variantNotes = new List<string>();
    // Zorluk ozeti icin: (bolum, tavan, 1y, 2y, 3y)
    private static readonly List<int[]> ledger = new List<int[]>();
    private static readonly List<Candidate> candidates = new List<Candidate>();
    private static readonly List<Rigidbody> bodies = new List<Rigidbody>();
    private static readonly List<int> variantsLeft = new List<int>();
    private static int[] levelQueue;
    private static int levelQ, candidateIndex, turn, simCount, currentLevel, currentVariant, normalScore;
    private static LevelData level;
    private static Scene scene;
    private static PhysicsScene physics;
    private static Rigidbody shot;
    private static float shotY, shotImpulse;
    private static Vector3[] state, bestState;
    private static bool[] scored, bestScored;
    private static float bestValue;
    private static string bestMove;
    private static PhysicsMaterial marbleMat, groundMat, variantMat;
    private static string outputName;
    private static bool testSpecials = true;
    // Kalibrasyon modu: ayni bolumu farkli atis guclerinde tarar.
    private static bool calibrating;
    // Batchmode pompasi icin: is devam ediyor mu?
    private static bool busy;
    private static readonly float[] CalibrationImpulses = { .55f, .65f, .75f, .85f };

    [MenuItem("MISKETR/Verify Park Physics Routes")]
    public static void RunPark()
    {
        var list = new int[12];
        for (int i = 0; i < 12; i++) list[i] = 24 + i;
        testSpecials = true; calibrating = false;
        Begin(list, "ParkPhysicsVerify.txt");
    }

    [MenuItem("MISKETR/Verify Mastery Finals")]
    public static void RunFinals() { testSpecials = true; calibrating = false; Begin(new[] { 11, 23, 35 }, "MasteryFinalsVerify.txt"); }

    // Dogru atis gucunu bulmak icin: 6 temsili bolum x 4 guc.
    // Hedef, acgozlu makinenin 2 yildiz civarinda takilmasi.
    [MenuItem("MISKETR/Calibrate Shot Power")]
    public static void RunCalibration()
    {
        testSpecials = false; calibrating = true;
        Begin(new[] { 11, 17, 23, 28, 32, 35 }, "ShotPowerCalibration.txt");
    }

    // Sadece son turda degisen bolumler. Degismeyenlerin tavani zaten olculdu,
    // tekrar taramak bosuna. Her yerlesim degisikliginden sonra bu liste guncellenir.
    private static readonly int[] ChangedLevels = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59 };  // TAM DENETIM: 60 bolum, guc ve ozel misketlerle

    // Mahalle Meydani (5. mahalle) tek basina.
    [MenuItem("MISKETR/Verify Mahalle Meydani")]
    public static void RunMeydan()
    {
        var list = new int[12];
        for (int i = 0; i < 12; i++) list[i] = 48 + i;
        testSpecials = true; calibrating = false;
        Begin(list, "MeydanPhysicsVerify.txt");
    }

    [MenuItem("MISKETR/Verify Changed Levels")]
    public static void RunChanged()
    {
        testSpecials = true; calibrating = false;   // gucleri de olc: "normal ile kac, gucle kac"
        Begin(ChangedLevels, "ChangedLevelsVerify.txt");
    }

    // ---------- Batchmode girisleri ----------
    // Unity'yi komut satirindan (-batchmode -executeMethod) calistirinca
    // EditorApplication.update hic tiklamaz, is hic baslamaz. Bu yuzden
    // batchmode'da Step()'i burada elle donduruyoruz. Menuden calistirmayi
    // hicbir sekilde degistirmez.
    public static void BatchChanged() { RunChanged(); Pump(); }
    public static void BatchMeydan()  { RunMeydan();  Pump(); }
    public static void BatchAll()     { RunAll();     Pump(); }
    public static void BatchPark()    { RunPark();    Pump(); }
    public static void BatchFinals()  { RunFinals();  Pump(); }

    private static void Pump()
    {
        EditorApplication.update -= Step;   // kuyrugu biz suruyoruz
        long guard = 0;
        while (busy)
        {
            Step();
            if (++guard > 200000000L) { Debug.LogError("PHYSICS_ABORT: pompa guvenlik siniri."); Finish(); break; }
        }
    }

    [MenuItem("MISKETR/Verify All Designed Levels")]
    public static void RunAll()
    {
        var list = new int[Campaign.Count];   // bes mahalle, 60 bolum
        for (int i = 0; i < list.Length; i++) list[i] = i;
        testSpecials = false; calibrating = false; // 36 bolum x 5 misket cok uzun surer
        Begin(list, "AllLevelsPhysicsVerify.txt");
    }

    private static void Begin(int[] levels, string fileName)
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Fizik kontrolu icin Play'i kapat."); return; }

        marbleMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/MarbleMaterialPhysics.asset");
        groundMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/GroundMaterialPhysics.asset");
        if (marbleMat == null || groundMat == null)
        {
            Debug.LogError("PHYSICS_ABORT: Assets/Materials/ altinda MarbleMaterialPhysics.asset ve GroundMaterialPhysics.asset olmali.");
            return;
        }

        // Oyundaki hedef misket gercekten bu materyali mi kullaniyor? Kullanmiyorsa test oyunu olcmez.
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TargetMarble.prefab");
        var prefabCollider = prefab != null ? prefab.GetComponent<Collider>() : null;
        if (prefabCollider == null || prefabCollider.sharedMaterial != marbleMat)
            Debug.LogWarning("PHYSICS_WARN: TargetMarble.prefab collider materyali MarbleMaterialPhysics degil (" +
                             (prefabCollider == null ? "collider yok" : prefabCollider.sharedMaterial == null ? "None" : prefabCollider.sharedMaterial.name) +
                             "). Sonuclar oyunla ayni fizigi anlatmaz.");

        Cleanup();
        busy = true;
        levelQueue = levels; levelQ = 0; outputName = fileName;
        report.Clear(); variantsLeft.Clear(); variantNotes.Clear(); ledger.Clear();
        level = null; simCount = 0; normalScore = -1;
        EditorApplication.update -= Step;
        EditorApplication.update += Step;
    }

    private static void Step()
    {
        try
        {
            if (level == null) { NextRun(); return; }

            int done = 0;
            while (candidateIndex < candidates.Count && done < BatchPerTick)
            { Evaluate(candidates[candidateIndex]); candidateIndex++; done++; simCount++; }

            if (EditorUtility.DisplayCancelableProgressBar(
                    "Fizik kontrolu · " + Variants[currentVariant].name,
                    "Bolum " + (levelQ + 1) + "/" + levelQueue.Length + " · atis " + (turn + 1) + "/" + level.shotCount +
                    " · aday " + candidateIndex + "/" + candidates.Count,
                    candidates.Count == 0 ? 0f : candidateIndex / (float)candidates.Count))
            { report.Add("CANCELLED"); Finish(); return; }

            if (candidateIndex < candidates.Count) return;
            CommitTurn();
        }
        catch (Exception e) { Debug.LogException(e); Finish(); }
    }

    private static void NextRun()
    {
        if (variantsLeft.Count == 0)
        {
            if (levelQ >= levelQueue.Length) { Finish(); return; }
            currentLevel = levelQueue[levelQ];
            variantNotes.Clear(); normalScore = -1;
            if (calibrating) for (int i = 0; i < CalibrationImpulses.Length; i++) variantsLeft.Add(i);
            else variantsLeft.Add(0); // once normal misket
        }
        currentVariant = variantsLeft[0]; variantsLeft.RemoveAt(0);
        BeginRun(currentLevel, currentVariant);
    }

    private static void CommitTurn()
    {
        if (bestState != null) { state = bestState; scored = bestScored; moves.Add(bestMove); }
        turn++;
        if (turn < level.shotCount && bestState != null) { BuildCandidates(); return; }

        int total = 0; foreach (bool s in scored) if (s) total++;
        int one = level.oneStarTarget, two = level.twoStarTarget, three = level.threeStarTarget;
        int gate = level.starsToPass == 2 ? two : one;
        int marbles = level.TotalMarbles(); int shots = level.shotCount;
        string route = string.Join(" ; ", moves);
        EndRun();

        if (calibrating)
        {
            variantNotes.Add("guc " + CalibrationImpulses[currentVariant].ToString("F2") +
                             " -> " + total + "/" + marbles + " (%" + Mathf.RoundToInt(total * 100f / marbles) +
                             ")" + (total >= three ? " 3y" : total >= two ? " 2y" : total >= one ? " 1y" : " GECEMEDI"));
            if (variantsLeft.Count > 0) return;
            string head = Label(currentLevel) + "  " + marbles + " misket / " + shots + " atis · hedefler " +
                          one + "/" + two + "/" + three;
            report.Add(head); Debug.Log(head);
            foreach (var n in variantNotes) { report.Add("    " + n); Debug.Log("    " + n); }
            report.Add("");
            levelQ++; return;
        }

        if (currentVariant == 0)
        {
            normalScore = total;
            ledger.Add(new[] { currentLevel, total, one, two, three });
            report.Add(Header(currentLevel, marbles, shots, one, two, three, gate, total, route));
            // 3 yildiza normal misketle ulasilamiyorsa ozel misketleri dene.
            if (testSpecials && total < three)
            {
                variantsLeft.Add(5); variantsLeft.Add(6);                       // gucler
                variantsLeft.Add(1); variantsLeft.Add(2); variantsLeft.Add(3); variantsLeft.Add(4); // ozel misketler
                return;
            }
            Verdict(three, gate);
            levelQ++; return;
        }

        variantNotes.Add(Variants[currentVariant].name + " " + total +
                         (total >= three ? " (3y)" : total >= two ? " (2y)" : total >= one ? " (1y)" : ""));
        if (variantsLeft.Count > 0) return;
        report.Add("    guc ve ozel misketle: " + string.Join(" | ", variantNotes));
        Verdict(three, gate);
        levelQ++;
    }

    private static string Label(int index)
    {
        string d = index < 12 ? "APARTMAN" : index < 24 ? "OKUL" : index < 36 ? "PARK" : index < 48 ? "TOPRAK" : "MEYDAN";
        return d + " " + (index % 12 + 1).ToString("00");
    }

    private static string Header(int index, int marbles, int shots, int one, int two, int three, int gate, int total, string route)
    {
        string district = index < 12 ? "APARTMAN" : index < 24 ? "OKUL" : index < 36 ? "PARK" : index < 48 ? "TOPRAK SAHA" : "MEYDAN";
        string line = district + " " + (index % 12 + 1).ToString("00") + " (idx=" + index + ") " +
                      marbles + " misket / " + shots + " atis · NORMAL misketle en iyi " + total +
                      "  ->  1y(" + one + ")=" + (total >= one ? "VAR" : "YOK") +
                      " 2y(" + two + ")=" + (total >= two ? "VAR" : "YOK") +
                      " 3y(" + three + ")=" + (total >= three ? "VAR" : "YOK") +
                      (gate != one ? "  [kilit " + gate + "=" + (total >= gate ? "VAR" : "YOK") + "]" : "");
        Debug.Log(line);
        report.Add("    yol: " + route);
        return line;
    }

    private static void Verdict(int three, int gate)
    {
        string verdict;
        if (normalScore < gate) verdict = "KIRMIZI · gecis/kilit normal misketle bulunamadi, bolum kilitleyebilir";
        else if (normalScore >= three) verdict = "SARI · 3 yildiz da normal misketle aliniyor, ozel misket bu bolumde bir sey katmiyor";
        else
        {
            bool any = false; foreach (var n in variantNotes) if (n.Contains("(3y)")) any = true;
            verdict = any ? "YESIL · gecis normal misketle, 3 yildiz ozel miskete birakilmis"
                          : "SARI · 3 yildizi hicbir misket bulamadi, hedef fazla yuksek olabilir";
        }
        report.Add("    SONUC: " + verdict);
        report.Add("");
        Debug.Log("    SONUC: " + verdict);
    }

    private static void BeginRun(int index, int variantIndex)
    {
        level = Campaign.Database.Get(index);
        var v = Variants[calibrating ? 0 : variantIndex];

        scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        if (!scene.IsValid()) { Debug.LogError("PHYSICS_ABORT: preview scene olusturulamadi."); level = null; Finish(); return; }
        physics = scene.GetPhysicsScene();
        bodies.Clear(); moves.Clear();

        var ground = Make(PrimitiveType.Cube, new Vector3(0, -.25f, 0), new Vector3(30, .5f, 30));
        ground.GetComponent<Collider>().sharedMaterial = groundMat;

        if (level.obstacles != null)
            foreach (var w in level.obstacles)
            {
                var wall = Make(PrimitiveType.Cube, new Vector3(w.x, ObstacleY, w.z),
                                new Vector3(Mathf.Max(.2f, w.width), ObstacleHeight, Mathf.Max(.2f, w.depth)));
                wall.transform.rotation = Quaternion.Euler(0, w.angle, 0);
            }

        if (level.marbles == null || level.marbles.Length == 0)
        {
            Debug.LogWarning("PHYSICS_WARN: idx=" + index + " elle yerlestirilmis misket yok, atlaniyor.");
            EndRun(); variantsLeft.Clear(); levelQ++; return;
        }
        foreach (var spot in level.marbles)
            bodies.Add(MakeMarble(new Vector3(spot.x, TargetY, spot.z), MarbleScale, MarbleMass, MarbleDrag, marbleMat));

        // Atici misket: secilen turun ozellikleriyle.
        shotY = MarbleScale * v.scaleMul * .5f;
        shotImpulse = (calibrating ? CalibrationImpulses[variantIndex] : MaxImpulse) * v.impulseMul;
        var mat = marbleMat;
        if (v.ownMaterial)
        {
            if (variantMat == null) variantMat = new PhysicsMaterial("Ozel misket (test)");
            variantMat.dynamicFriction = marbleMat.dynamicFriction * v.frictionMul;
            variantMat.staticFriction = marbleMat.staticFriction * v.frictionMul;
            variantMat.bounciness = v.bounciness < 0f ? marbleMat.bounciness : v.bounciness;
            variantMat.frictionCombine = v.frictionCombine;
            variantMat.bounceCombine = v.bounceCombine;
            mat = variantMat;
        }
        shot = MakeMarble(new Vector3(0, shotY, level.shooterStartPosition.z),
                          MarbleScale * v.scaleMul, MarbleMass * v.massMul, MarbleDrag * v.dragMul, mat);

        state = new Vector3[bodies.Count]; scored = new bool[bodies.Count];
        for (int i = 0; i < state.Length; i++) state[i] = bodies[i].position;
        turn = 0; BuildCandidates();
    }

    private static void EndRun()
    {
        if (scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        scene = default; bodies.Clear(); shot = null; level = null; bestState = null; bestScored = null;
    }

    private static void Finish()
    {
        busy = false;
        EditorApplication.update -= Step;
        EditorUtility.ClearProgressBar();
        EndRun();
        if (variantMat != null) { UnityEngine.Object.DestroyImmediate(variantMat); variantMat = null; }
        if (report.Count == 0) return;
        Summarise();
        report.Add("# " + simCount + " simulasyon. Acgozlu arama: VAR alt sinirdir, YOK kesin degildir.");
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/" + outputName, report);
        Debug.Log("PHYSICS_DONE: Logs/" + outputName);
    }

    // Iki olcu birlikte zorlugu anlatiyor:
    //   TEMIZLENEBILIRLIK = tavan / toplam misket. Bolumun kendisi ne kadar direniyor.
    //     Mahalleler ilerledikce DUSMELI -- Park'ta misketlerin ucte biri fiziksel olarak cikmiyor.
    //   PAY = tavan - 2 yildiz. Kac atisi ziyan edebilirsin.
    //     Ogretici mahallede bol, sinav bolumlerinde sifir.
    private static void Summarise()
    {
        if (ledger.Count == 0) return;
        var clear = new float[5]; var pay = new float[5]; var count = new int[5];
        var blocked = new List<string>(); var finals = new List<string>();
        foreach (var row in ledger)
        {
            int d = row[0] / 12; if (d > 4) continue;
            var lvl = Campaign.Database.Get(row[0]);
            clear[d] += row[1] / (float)lvl.TotalMarbles();
            pay[d] += row[1] - row[3];
            count[d]++;
            if (row[1] < row[2]) blocked.Add(Label(row[0]) + " (gecis " + row[2] + ", tavan " + row[1] + ")");
            if (lvl.starsToPass == 2)
            {
                if (row[1] < row[3]) blocked.Add(Label(row[0]) + " (kilit " + row[3] + ", tavan " + row[1] + ")");
                finals.Add(Label(row[0]) + " pay " + (row[1] - row[3]));
            }
        }
        report.Add(""); report.Add("=== ZORLUK OZETI ===");
        string[] names = { "Apartman", "Okul    ", "Park    ", "Toprak  ", "Meydan  " };
        for (int d = 0; d < 5; d++)
            if (count[d] > 0)
            {
                string line = "  " + names[d] + "  temizlenebilirlik %" + Mathf.RoundToInt(clear[d] / count[d] * 100) +
                              "   ortalama pay " + (pay[d] / count[d]).ToString("F1") + "   (" + count[d] + " bolum)";
                report.Add(line); Debug.Log(line);
            }
        // Zorluk sirasi: temizlenebilirlik her mahallede bir oncekinden DUSUK olmali.
        // Sadece taranan mahalleler karsilastirilir; tek mahalle tarandiysa kontrol atlanir.
        var seen = new List<int>();
        for (int d = 0; d < 5; d++) if (count[d] > 0) seen.Add(d);
        if (seen.Count >= 2)
        {
            bool ok = true; string chain = "";
            for (int k = 0; k < seen.Count; k++)
            {
                float c = clear[seen[k]] / count[seen[k]];
                chain += (k > 0 ? " > " : "") + names[seen[k]].Trim() + " %" + Mathf.RoundToInt(c * 100);
                if (k > 0 && c >= clear[seen[k - 1]] / count[seen[k - 1]]) ok = false;
            }
            string v = (ok ? "  SIRA DOGRU: " : "  SIRA BOZUK: ") + chain;
            report.Add(v); if (ok) Debug.Log(v); else Debug.LogError(v);
        }
        else report.Add("  (tek mahalle tarandi, sira kontrolu atlandi)");
        if (finals.Count > 0) report.Add("  Finaller: " + string.Join(" · ", finals));
        if (blocked.Count > 0)
        {
            string b = "  KAPALI BOLUMLER: " + string.Join(", ", blocked);
            report.Add(b); Debug.LogError(b);
        }
        else { report.Add("  Kapali bolum yok."); Debug.Log("  Kapali bolum yok."); }
    }

    private static void Cleanup()
    {
        EditorApplication.update -= Step;
        EditorUtility.ClearProgressBar();
        EndRun();
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

    private static void BuildCandidates()
    {
        candidates.Clear(); candidateIndex = 0; bestValue = -1f; bestState = null; bestScored = null; bestMove = "";

        float halfWidth = level.shooterHalfWidth > 0f ? level.shooterHalfWidth : FallbackHalfWidth;
        float centerX = level.shooterOffsetX;
        float lineZ = level.shooterStartPosition.z;
        var seen = new HashSet<long>();

        for (int p = 0; p < LinePositions; p++)
        {
            float t = LinePositions == 1 ? .5f : p / (float)(LinePositions - 1);
            var start = new Vector3(centerX + Mathf.Lerp(-halfWidth, halfWidth, t), shotY, lineZ);

            for (int i = 0; i < state.Length; i++)
            {
                if (scored[i]) continue;
                var toMarble = state[i] - start; toMarble.y = 0;
                if (toMarble.sqrMagnitude < .01f) continue;

                // Duz ve kesme atislar
                var perpendicular = Vector3.Cross(Vector3.up, toMarble.normalized);
                foreach (float offset in AimOffsets)
                    foreach (float power in Powers)
                        Add(seen, p, start, state[i] + perpendicular * offset, power, "m" + i);

                // Engelden sekerek
                if (level.obstacles == null) continue;
                foreach (var w in level.obstacles)
                {
                    var normal = Quaternion.Euler(0, w.angle, 0) * (w.width >= w.depth ? Vector3.forward : Vector3.right);
                    float half = (w.width >= w.depth ? w.depth : w.width) * .5f + .25f;
                    foreach (float sign in new[] { -1f, 1f })
                    {
                        var plane = new Vector3(w.x, TargetY, w.z) + normal * (half * sign);
                        foreach (float power in Powers)
                            Add(seen, p, start, state[i] - 2 * Vector3.Dot(state[i] - plane, normal) * normal, power, "s" + i);
                    }
                }
            }
        }
    }

    private static void Add(HashSet<long> seen, int slot, Vector3 start, Vector3 aim, float power, string label)
    {
        var dir = aim - start; dir.y = 0;
        if (dir.sqrMagnitude < .01f) return;
        dir.Normalize();
        if (dir.z <= 0f) return; // oyuncu cizginin gerisine atmaz
        long key = (slot * 8L + Mathf.RoundToInt(power * 20f)) * 100000L
                   + Mathf.RoundToInt(Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg * 2f);
        if (!seen.Add(key)) return;
        candidates.Add(new Candidate { start = start, dir = dir, power = power, label = label });
    }

    private static void Evaluate(Candidate candidate)
    {
        for (int i = 0; i < state.Length; i++)
        {
            var p = state[i]; p.y = TargetY;
            bodies[i].position = p; bodies[i].rotation = Quaternion.identity;
            bodies[i].linearVelocity = Vector3.zero; bodies[i].angularVelocity = Vector3.zero; bodies[i].WakeUp();
        }
        shot.position = candidate.start; shot.rotation = Quaternion.identity;
        shot.linearVelocity = Vector3.zero; shot.angularVelocity = Vector3.zero; shot.WakeUp();

        var attempt = (bool[])scored.Clone();
        shot.AddForce(candidate.dir * (shotImpulse * candidate.power), ForceMode.Impulse);

        float limit = level.arenaSize + ExitMargin;
        for (int frame = 0; frame < MaxFrames; frame++)
        {
            physics.Simulate(.02f);
            bool rest = shot.linearVelocity.magnitude < RestSpeed;
            for (int i = 0; i < state.Length; i++)
            {
                var pos = bodies[i].position;
                if (pos.x * pos.x + pos.z * pos.z > limit * limit) attempt[i] = true;
                if (bodies[i].linearVelocity.magnitude > RestSpeed) rest = false;
            }
            if (frame > 25 && rest) break;
        }

        int score = 0; float spread = 0f;
        for (int i = 0; i < state.Length; i++)
        {
            if (attempt[i]) score++;
            else spread += new Vector2(bodies[i].position.x, bodies[i].position.z).magnitude;
        }
        float value = score + spread * .001f; // esitlikte kalanlari kenara yaklastirani sec
        if (value <= bestValue) return;

        bestValue = value;
        bestState = new Vector3[state.Length];
        for (int i = 0; i < state.Length; i++) bestState[i] = bodies[i].position;
        bestScored = attempt;
        bestMove = candidate.label + "@" + candidate.start.x.ToString("F2") + " guc%" + Mathf.RoundToInt(candidate.power * 100) + " (+" + score + ")";
    }
}
