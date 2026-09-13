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
        report.Add("DUELLO FIZIK TARAMASI · cember " + ArenaSize + " · hedef: atis basina ~1.0");
        report.Add("KALAN = aticinin cemberin ICINDE durdugu atislarin orani.");
        report.Add("Yeni kuralda atici cemberde kalirsa kaybediliyor, yani bu oran");
        report.Add("dusuk olmali; yuksekse oyuncu her atista misket kaybeder.");
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

            report.Add("UCGEN SAHA (koseler 270/30/150, sivri uc aticiya bakiyor)");
            foreach (float boy in new[] { 3.2f, 3.6f, 4.0f })
                Sweep("  ucgen boyu " + boy.ToString("0.0"), new[] { .80f, .90f, 1.00f },
                      v => new Setting { impulse = v, arena = boy, triangle = true,
                                         bounciness = .45f, frictionMul = .7f });
            report.Add("");

            // DIZI (KONDIK) MODU: kural farkli, olcum de farkli.
            // Kazanmak icin misketi CIKARMAK degil KIPIRDATMAK yeterli.
            // Iki soru: (1) hangi guc atis basina ~1 misket veriyor,
            // (2) "kipirdadi" esigi gurultuden ayirt edilebiliyor mu.
            DiziReport();

            // KUYU MODU: cukura girmek ne kadar zor?
            KuyuReport();

            // SIRA BELIRLEME ATISI olculebilir mi?
            // Soru: oyuncu cizgiye (sahanin uzak kenari) ULASABILIYOR mu ve
            // tam gucte GECIYOR mu. Ikisi de olmazsa atis beceri testi degil,
            // "sonuna kadar bas" oyunu olur.
            TossReport();

            // Atici nerede duruyor? Kural esigini buna gore secilecek.
            report.Add("ATICI NEREDE DURUYOR (cember yaricapinin yuzdesi icinde kalma orani)");
            foreach (var (ad, st) in new (string, Setting)[]
            {
                ("guc .80", new Setting { impulse = .80f }),
                ("guc .90", new Setting { impulse = .90f }),
                ("guc 1.00", new Setting { impulse = 1.00f }),
                ("guc .90 + surtunme .7x", new Setting { impulse = .90f, frictionMul = .7f }),
            })
            {
                Measure(st);
                report.Add("  " + ad.PadRight(24) + BandReport());
            }
            report.Add("");
        }
        finally
        {
            Cleanup();
            if (testMat != null) { UnityEngine.Object.DestroyImmediate(testMat); testMat = null; }
        }

        Denge();

        report.Add("");
        report.Add("# " + simCount + " atis simule edildi.");
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/DuelPhysicsVerify.txt", report);
        Debug.Log("DUEL_PHYSICS_DONE: Logs/DuelPhysicsVerify.txt");
    }

    // Bos sahada tek atis: hangi gucte nerede duruyor.
    // Sira belirleme atisinin oynanabilirligi buna bagli.
    // Sekiz misket tek sirada, aralik ~.63.
    private static float[,] RowLayout(float yariBoy, int adet)
    {
        var a = new float[adet, 2];
        float lim = yariBoy - .125f;
        for (int i = 0; i < adet; i++)
        {
            float t = adet == 1 ? .5f : i / (float)(adet - 1);
            a[i, 0] = Mathf.Lerp(-lim, lim, t);
            a[i, 1] = DuelSession.RowZ;
        }
        return a;
    }

    private static void DiziReport()
    {
        report.Add("DIZI (KONDIK) MODU · sira yarim boyu " + DuelSession.RowSize.ToString("0.0")
                   + " · 8 misket · kazanma: KIPIRDATMAK");
        report.Add("  ORT = atis basina kipirdayan misket · ISKA = hicbirine degmeyen atis");
        report.Add("  guc    ORT   EN IYI   ISKA    en kucuk gercek oynama   yorum");

        float eskiBoy = ArenaSize; bool eskiTri = triangleMode;
        var eskiTur = DuelSession.Type;
        DuelSession.Type = DuelSession.GameType.Dizi;

        // Sira GENISLIGI de taranir: dar sirada tek atis butun sirayi
        // devirir (zincir), genis sirada nisan almak gerekir.
        foreach (float boy in new[] { 2.2f, 2.8f, 3.4f, 4.0f })
        {
            report.Add("  -- yarim boy " + boy.ToString("0.0")
                       + " (aralik " + (boy * 2f / 7f).ToString("0.00") + ")");
            foreach (float guc in new[] { .35f, .45f, .55f, .70f, 1.00f })
            {
                var s = new Setting
                {
                    arena = boy, triangle = false, impulse = guc,
                    bounciness = DuelSession.Bounciness, frictionMul = DuelSession.FrictionMul,
                };
                ArenaSize = s.arena; triangleMode = false;
                BuildRow(s, 8);

                float toplam = 0f; int atis = 0, iska = 0, best = 0;
                float enKucuk = 999f;
                // Nisan noktalari siranin UCLARININ da disina tasar: gercek
                // oyuncu her zaman siraya isabet ettiremez, iska orani
                // ancak boyle olculebilir.
                for (int a = 0; a < 13; a++)
                {
                    float ax = Mathf.Lerp(-boy * 1.35f, boy * 1.35f, a / 12f);
                    var (oynayan, minOynama) = RowShot(s, ax);
                    toplam += oynayan; atis++; simCount++;
                    if (oynayan == 0) iska++;
                    if (oynayan > best) best = oynayan;
                    if (minOynama > 0f && minOynama < enKucuk) enKucuk = minOynama;
                }
                Cleanup();

                float ort = atis == 0 ? 0f : toplam / atis;
                string yorum = Mathf.Abs(ort - 1f) < .25f ? "   <-- hedefe yakin" : "";
                report.Add(string.Format("  {0,4:0.00}  {1,5:0.00}   {2,4}   %{3,3:0}   {4,20}{5}",
                                         guc, ort, best, iska * 100f / Mathf.Max(1, atis),
                                         enKucuk > 100f ? "yok" : enKucuk.ToString("0.000"), yorum));
            }
        }

        report.Add("  secili guc: " + DuelSession.RowImpulse.ToString("0.00")
                   + " · kipirdama esigi: " + DuelSession.RowNudge.ToString("0.00"));
        report.Add("  (esik, yukaridaki 'en kucuk gercek oynama' degerlerinin ALTINDA olmali;");
        report.Add("   ustunde olursa gercek temas sayilmaz, cok altinda olursa titresim sayilir)");
        report.Add("");

        DuelSession.Type = eskiTur;
        ArenaSize = eskiBoy; triangleMode = eskiTri;
    }

    private static Vector3[] rowHome;

    private static void BuildRow(Setting s, int adet)
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

        var yer = RowLayout(s.arena, adet);
        for (int i = 0; i < adet; i++)
            bodies.Add(MakeMarble(new Vector3(yer[i, 0], TargetY, yer[i, 1]),
                                  BaseScale, BaseMass * s.massMul, BaseDrag * s.dragMul, testMat));

        home = new Vector3[bodies.Count];
        rowHome = new Vector3[bodies.Count];
        for (int i = 0; i < bodies.Count; i++) home[i] = rowHome[i] = bodies[i].position;
    }

    // Tek atis; kac misketin KIPIRDADIGI ve en kucuk gercek yer degistirme.
    private static (int oynayan, float enKucukOynama) RowShot(Setting s, float aimX)
    {
        for (int i = 0; i < bodies.Count; i++)
        {
            bodies[i].position = rowHome[i];
            bodies[i].linearVelocity = Vector3.zero; bodies[i].angularVelocity = Vector3.zero;
        }

        var from = new Vector3(0f, TargetY, ShooterZ);
        var shot = MakeMarble(from, BaseScale, BaseMass * s.massMul, BaseDrag * s.dragMul, testMat);
        Vector3 dir = (new Vector3(aimX, TargetY, DuelSession.RowZ) - from); dir.y = 0f; dir.Normalize();
        shot.AddForce(dir * s.impulse, ForceMode.Impulse);

        for (int f = 0; f < MaxFrames; f++)
        {
            physics.Simulate(.02f);
            bool moving = shot.linearVelocity.magnitude > RestSpeed;
            if (!moving)
                for (int i = 0; i < bodies.Count && !moving; i++)
                    if (bodies[i].linearVelocity.magnitude > RestSpeed) moving = true;
            if (!moving) break;
        }

        int oynayan = 0; float enKucuk = 999f;
        for (int i = 0; i < bodies.Count; i++)
        {
            float d = Vector3.Distance(bodies[i].position, rowHome[i]);
            if (d > DuelSession.RowNudge) { oynayan++; if (d < enKucuk) enKucuk = d; }
        }
        UnityEngine.Object.DestroyImmediate(shot.gameObject);
        return (oynayan, enKucuk);
    }

    // KUYU: atici cizgiden cukura atar. Olculen sey cukura girme orani.
    // Cok yuksekse mac sikici bir tekrar, cok dusukse 12 sayi hic gelmez.
    private static void KuyuReport()
    {
        var eski = DuelSession.Type;
        DuelSession.Type = DuelSession.GameType.Kuyu;
        float saha = DuelSession.WellSize;

        report.Add("KUYU MODU · saha " + saha.ToString("0.0")
                   + " · cukur yaricapi " + DuelSession.HoleRadius.ToString("0.00")
                   + " · cizgiden cukura " + (0f - ShooterZ).ToString("0.0") + " birim");
        report.Add("  Hedef: cukura girme orani makul olsun. Yeni kuralda cukur turu");
        report.Add("  kazandirmiyor, sadece PISIRIYOR -- turu almak icin sonra");
        report.Add("  rakibi sahadan cikarmak gerekiyor.");
        report.Add("  cukur   guc    CUKUR%   SAHA DISI%   pismek icin ~atis");

        float secili = DuelSession.WellImpulse;
        foreach (float cukur in new[] { .35f, .45f, DuelSession.HoleRadius, .55f, .70f })
        {
            foreach (float guc in new[] { secili * .8f, secili, secili * 1.2f })
            {
                var st = new Setting
                {
                    arena = saha, triangle = false, impulse = guc,
                    bounciness = DuelSession.Bounciness, frictionMul = DuelSession.FrictionMul,
                };
                ArenaSize = saha; triangleMode = false;
                BuildEmpty(st);

                int atis = 0, giren = 0, disari = 0;
                // Oyuncu nisan alir ama tam tutturamaz: cukurun etrafina
                // yayilmis hedef noktalari ve guc sapmasi ile taranir.
                for (int a = -3; a <= 3; a++)
                {
                    float ax = a * .18f;
                    for (int g = -2; g <= 2; g++)
                    {
                        float pw = Mathf.Clamp01(1f + g * .07f);
                        var (mx, mz, dis) = KuyuShot(st, ax, pw, saha);
                        atis++; simCount++;
                        if (dis) { disari++; continue; }
                        float r = new Vector2(mx, mz).magnitude;
                        if (r <= cukur - .125f) giren++;
                    }
                }
                Cleanup();

                float oran = atis == 0 ? 0f : giren / (float)atis;
                // Yeni kuralda sayi yok: turu almak icin BIR kez pismek yeterli,
                // gerisi rakibi cikarmak. Yani onemli olan "kac atista pisersin".
                string tahmin = oran <= .001f ? "ulasilmaz"
                              : Mathf.RoundToInt(1f / oran) + " atis";
                report.Add(string.Format("  {0,5:0.00}  {1,4:0.00}   %{2,5:0}   %{3,9:0}   {4,17}{5}",
                                         cukur, guc, oran * 100f, disari * 100f / Mathf.Max(1, atis), tahmin,
                                         Mathf.Approximately(guc, secili) && Mathf.Approximately(cukur, DuelSession.HoleRadius)
                                             ? "   <-- SECILI" : ""));
            }
        }
        report.Add("  secili: cukur " + DuelSession.HoleRadius.ToString("0.00")
                   + " · guc " + DuelSession.WellImpulse.ToString("0.00"));
        report.Add("");
        DuelSession.Type = eski;
    }

    // Cizgiden cukura tek atis; misketin durdugu yer ve sahayi terk edip
    // etmedigi. Cukur fiziksel bir delik degil, duran misketin merkeze
    // uzakligina bakiliyor (oyundaki kuralin aynisi).
    private static (float x, float z, bool disarida) KuyuShot(Setting s, float aimX, float power, float saha)
    {
        var from = new Vector3(0f, TargetY, ShooterZ);
        var shot = MakeMarble(from, BaseScale, BaseMass * s.massMul, BaseDrag * s.dragMul, testMat);
        Vector3 dir = (new Vector3(aimX, TargetY, 0f) - from); dir.y = 0f; dir.Normalize();
        shot.AddForce(dir * (s.impulse * power), ForceMode.Impulse);
        for (int f = 0; f < MaxFrames; f++)
        {
            physics.Simulate(.02f);
            if (shot.linearVelocity.magnitude <= RestSpeed) break;
        }
        var p = shot.position;
        bool disarida = new Vector2(p.x, p.z).magnitude > saha + ExitMargin;
        UnityEngine.Object.DestroyImmediate(shot.gameObject);
        return (p.x, p.z, disarida);
    }

    private static void TossReport()
    {
        // Sira atisi oynanabilir mi? Iki sey olmali:
        //  1) cizgi sliderin ORTALARINDA olsun (cok erken = pencere dar,
        //     cok gec = "sonuna kadar bas" oyunu),
        //  2) cizgiyi gecmek mumkun olsun, yoksa yanma kurali laf olur.
        foreach (bool tri in new[] { false, true })
        {
            var eskiTur = DuelSession.Type;
            DuelSession.Type = tri ? DuelSession.GameType.Ucgen : DuelSession.GameType.Cember;
            float boy = DuelSession.ArenaSize, cizgi = DuelToss.Line;
            float secili = DuelSession.TossImpulse;

            report.Add("SIRA BELIRLEME ATISI · " + (tri ? "ucgen " : "cember ")
                       + boy.ToString("0.0") + " · cizgi z=" + cizgi.ToString("0.00")
                       + " · atici z=" + ShooterZ.ToString("0.0")
                       + " · yol=" + (cizgi - ShooterZ).ToString("0.0"));
            report.Add("  guc    cizgiye varan slider   tam gucte z   yorum");

            foreach (float guc in new[] { secili, secili * .7f, secili * 1.4f, 1f })
            {
                var s = new Setting
                {
                    arena = boy, triangle = tri, impulse = guc,
                    bounciness = DuelSession.Bounciness,
                    frictionMul = DuelSession.FrictionMul,
                };
                ArenaSize = boy; triangleMode = tri;
                BuildEmpty(s);

                float varan = -1f, tamZ = 0f;
                for (int i = 1; i <= 20; i++)
                {
                    float pw = i / 20f;
                    var (z, _) = TossShot(s, pw);
                    simCount++;
                    if (varan < 0f && z > cizgi) varan = pw;
                    if (i == 20) tamZ = z;
                }
                Cleanup();

                string yorum = varan < 0f ? "cizgi GECILEMIYOR -- yanma kurali islemez"
                             : varan <= .40f ? "cizgi cok erken, pencere dar"
                             : varan >= .95f ? "cizgi sliderin ta ucunda"
                             : "IYI: cizgi sliderin %" + Mathf.RoundToInt(varan * 100) + "'inde";
                report.Add(string.Format("  {0,4:0.00}   {1,19}   {2,11:0.0}   {3}{4}",
                                         guc, varan < 0f ? "yok" : varan.ToString("0.00"), tamZ, yorum,
                                         Mathf.Approximately(guc, secili) ? "   <-- SECILI" : ""));
            }
            report.Add("");
            DuelSession.Type = eskiTur;
        }
    }

    // Sahayi bos kurar: sira atisinda ortada misket yok.
    private static void BuildEmpty(Setting s)
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
        home = new Vector3[0];
    }

    // Duz ileri tek atis; durdugu z ve sahayi terk edip etmedigi.
    private static (float z, bool disarida) TossShot(Setting s, float power)
    {
        var from = new Vector3(0f, TargetY, ShooterZ);
        var shot = MakeMarble(from, BaseScale, BaseMass * s.massMul, BaseDrag * s.dragMul, testMat);
        shot.AddForce(Vector3.forward * (s.impulse * power), ForceMode.Impulse);
        for (int f = 0; f < MaxFrames; f++)
        {
            physics.Simulate(.02f);
            if (shot.linearVelocity.magnitude <= RestSpeed) break;
        }
        var p = shot.position;
        // Oyundaki kural DuelPlacement.InsideArena ile ayni: pay yok.
        bool disarida = triangleMode ? Disarida(new Vector2(p.x, p.z))
                                     : new Vector2(p.x, p.z).magnitude > s.arena;
        UnityEngine.Object.DestroyImmediate(shot.gameObject);
        return (p.z, disarida);
    }

    private static void Aday(string ad, Setting s)
    {
        var (avg, best, zero, strand) = Measure(s);
        report.Add(string.Format("  {0,-38} ORT {1,5:0.00}  EN IYI {2}  bos %{3,3:0}  KALAN %{4,3:0}{5}",
                                 ad, avg, best, zero * 100f, strand * 100f,
                                 Math.Abs(avg - 1f) < .15f ? "   <-- hedefe yakin" : ""));
        Debug.Log(ad + " -> ort " + avg.ToString("0.00"));
    }

    // Atici cemberin hangi bolumunde durdu? Kural esigini veriyle secmek icin.
    private static readonly float[] Thresholds = { .40f, .55f, .70f, .85f, 1f };
    private static readonly int[] bands = new int[5];
    private static int lastShots;

    private static string BandReport()
    {
        if (lastShots == 0) return "";
        var parts = new List<string>();
        for (int i = 0; i < Thresholds.Length; i++)
            parts.Add("%" + Mathf.RoundToInt(Thresholds[i] * 100) + ": %" + Mathf.RoundToInt(bands[i] * 100f / lastShots));
        return string.Join("  ", parts);
    }

    // ---------------- MAC DENGESI ----------------
    // Fizik olcumunden cikan atis modeliyle binlerce mac oynatir.
    // Cevaplanan soru: bes el yeterli mi, kese bosaliyor mu, berabere ne siklikta.
    private static void Denge()
    {
        // Olculen degerler: ortalama 0.93 misket/atis, atislarin %47'si bos,
        // en iyi atis 3 misket, atici %30 ihtimalle cemberin ortasinda kaliyor.
        float[] dagilim = { .45f, .24f, .19f, .12f };   // 0,1,2,3 misket (guc 1.0)
        const float strandSans = .20f;   // tehlike bolgesi %35 yaricap
        var rnd = new System.Random(12345);             // sabit tohum: sonuc tekrarlanabilir

        int mac = 4000, beraberlik = 0, erkenBitis = 0;
        long toplamEl = 0, toplamAtis = 0, toplamTikanma = 0;
        var farklar = new List<int>();

        for (int i = 0; i < mac; i++)
        {
            int[] kese = { DuelMatch.StartingPouch, DuelMatch.StartingPouch };
            int ilkDizen = 0, el = 1, kalanOrtada = 0;
            bool erken = false;

            while (el <= DuelMatch.RoundsPerMatch)
            {
                int ante0 = Mathf.Min(DuelMatch.AntePerRound, kese[0]);
                int ante1 = Mathf.Min(DuelMatch.AntePerRound, kese[1]);
                if (ante0 <= 0 || ante1 <= 0) { erken = true; break; }
                kese[0] -= ante0; kese[1] -= ante1;
                int ortada = ante0 + ante1 + kalanOrtada;

                int sira = 1 - ilkDizen, bosTur = 0, atisSayaci = 0;
                while (ortada > 0 && bosTur < DuelMatch.StaleTurnLimit)
                {
                    int turdaAtis = 0; bool turKazanc = false;
                    while (turdaAtis < DuelMatch.MaxShotsPerTurn && ortada > 0)
                    {
                        double r = rnd.NextDouble(); int cikan = 0, birikim = 0;
                        for (int k = 0; k < dagilim.Length; k++) { birikim = k; if ((r -= dagilim[k]) <= 0) break; }
                        cikan = Mathf.Min(birikim, ortada);
                        bool kaldi = rnd.NextDouble() < strandSans;

                        kese[sira] += cikan; ortada -= cikan;
                        if (kaldi) ortada += 1;          // cemberde kalan atici yeni hedef
                        turdaAtis++; atisSayaci++;
                        if (cikan > 0) turKazanc = true;
                        if (cikan == 0 || kaldi) break;  // zincir kesilir
                    }
                    bosTur = turKazanc ? 0 : bosTur + 1;
                    sira = 1 - sira;
                }
                if (ortada > 0) toplamTikanma++;
                kalanOrtada = ortada;
                toplamAtis += atisSayaci;
                toplamEl++;
                ilkDizen = 1 - ilkDizen;
                el++;
            }

            if (erken) erkenBitis++;
            if (kese[0] == kese[1]) beraberlik++;
            farklar.Add(Mathf.Abs(kese[0] - kese[1]));
        }

        farklar.Sort();
        report.Add("");
        report.Add("MAC DENGESI · " + mac + " mac simule edildi");
        report.Add("  ortalama el basina atis    : " + (toplamAtis / (float)toplamEl).ToString("0.0"));
        report.Add("  tikanan el orani           : %" + Mathf.RoundToInt(toplamTikanma * 100f / toplamEl));
        report.Add("  kesesi bosalip erken biten : %" + Mathf.RoundToInt(erkenBitis * 100f / mac));
        report.Add("  beraberlik                 : %" + Mathf.RoundToInt(beraberlik * 100f / mac));
        report.Add("  kese farki (ortanca)       : " + farklar[farklar.Count / 2] + " misket");
        report.Add("  kese farki (en yakin %25)  : " + farklar[farklar.Count / 4] + " misket");
        report.Add("  kese farki (en uzak %25)   : " + farklar[farklar.Count * 3 / 4] + " misket");
    }

    private class Setting
    {
        public float massMul = 1f, dragMul = 1f, frictionMul = 1f, impulse = BaseImpulse, bounciness = -1f;
        public float arena = 3.2f;
        public bool triangle;
    }

    private static void Sweep(string title, float[] values, Func<float, Setting> build)
    {
        report.Add(title);
        foreach (float v in values)
        {
            var s = build(v);
            var (avg, best, zero, strand) = Measure(s);
            report.Add(string.Format("  {0,6:0.00} ->  ORT {1,5:0.00}  EN IYI {2}  bos %{3,3:0}  KALAN %{4,3:0}{5}",
                                     v, avg, best, zero * 100f, strand * 100f,
                                     Math.Abs(avg - 1f) < .12f ? "   <-- hedefe yakin" : ""));
            Debug.Log(title + " " + v.ToString("0.00") + " -> ort " + avg.ToString("0.00") + " kalan %" + (strand*100f).ToString("0"));
        }
        report.Add("");
    }

    // Bir ayarla ornek atislar yapar. Nisan noktalari ve gucler taranir;
    // sonuc TEK bir atisin ortalama verimi.
    private static (float avg, int best, float zeroRate, float strandRate) Measure(Setting s)
    {
        ArenaSize = s.arena; triangleMode = s.triangle;
        Build(s);
        float total = 0f; int best = 0, shots = 0, zero = 0, strand = 0;
        for (int i = 0; i < bands.Length; i++) bands[i] = 0;
        lastShots = 0;

        float[] powers = { 1f, .75f, .5f };
        for (int p = 0; p < 5; p++)
        {
            float sx = Mathf.Lerp(-ShooterHalfWidth, ShooterHalfWidth, p / 4f);
            for (int a = 0; a < 5; a++)
            {
                float ax = Mathf.Lerp(-1.4f, 1.4f, a / 4f);
                foreach (float pw in powers)
                {
                    var (outCount, endR) = Shoot(s, new Vector3(sx, TargetY, ShooterZ), new Vector3(ax, TargetY, .8f), pw);
                    total += outCount; shots++; simCount++;
                    if (outCount > best) best = outCount;
                    if (outCount == 0) zero++;
                    // Esik taramasi: aticinin cemberin hangi bolgesinde durdugu.
                    for (int t = 0; t < Thresholds.Length; t++)
                        if (endR <= ArenaSize * Thresholds[t]) bands[t]++;
                    if (endR <= ArenaSize + ExitMargin) strand++;
                }
            }
        }
        Cleanup();
        lastShots = shots;
        return (shots == 0 ? 0f : total / shots, best, shots == 0 ? 0f : zero / (float)shots, shots == 0 ? 0f : strand / (float)shots);
    }

    // Duello dizilisi: iki oyuncunun 7'ser misketi cembere yayilmis.
    // Gercek macta oyuncular kendi dizecek; burada temsili, makul sik bir dizilis.
    // Duello dizilisi: iki oyuncunun 5'er misketi. Gercek macta oyuncular
    // kendi dizecek; burada temsili, makul sik bir dizilis.
    private static readonly float[,] Layout =
    {
        { -0.62f, 1.0f }, { 0f, 1.0f }, { 0.62f, 1.0f },
        { -0.31f, 1.54f }, { 0.31f, 1.54f },
        { -0.93f, 0.46f }, { -0.31f, 0.46f }, { 0.31f, 0.46f }, { 0.93f, 0.46f },
        {  0f, 2.08f }
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

        var yerlesim = triangleMode ? TriLayout : Layout;
        for (int i = 0; i < yerlesim.GetLength(0); i++)
            bodies.Add(MakeMarble(new Vector3(yerlesim[i, 0], TargetY, yerlesim[i, 1]),
                                  BaseScale, BaseMass * s.massMul, BaseDrag * s.dragMul, testMat));

        home = new Vector3[bodies.Count];
        for (int i = 0; i < bodies.Count; i++) home[i] = bodies[i].position;
    }

    // Tek atis simule eder, cemberi terk eden misket sayisini dondurur.
    // Her atistan once dizilis basa alinir: atislar birbirini etkilemesin,
    // olculen sey tek bir atisin verimi olsun.
    private static (int outCount, float endRadius) Shoot(Setting s, Vector3 from, Vector3 aim, float power)
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

        // (cikis testi Disarida() icinde)
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
            if (Disarida(new Vector2(p.x, p.z))) outCount++;
        }

        // Atici nerede durdu? Yeni kuralda sahanin ortasinda kalmak misket kaybi.
        var sp = shot.position;
        float endR = new Vector2(sp.x, sp.z).magnitude;

        UnityEngine.Object.DestroyImmediate(shot.gameObject);
        return (outCount, endR);
    }

    private static bool triangleMode;

    // Saha disinda mi? MarbleArena ile ayni geometri.
    private static bool Disarida(Vector2 p)
    {
        if (!triangleMode) return p.magnitude > ArenaSize + ExitMargin;
        for (int i = 0; i < 3; i++)
        {
            float a0 = (270f + i * 120f) * Mathf.Deg2Rad, a1 = (270f + (i + 1) * 120f) * Mathf.Deg2Rad;
            Vector2 c0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * ArenaSize;
            Vector2 c1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * ArenaSize;
            Vector2 kenar = c1 - c0;
            Vector2 n = new Vector2(kenar.y, -kenar.x).normalized;
            if (Vector2.Dot(p - c0, n) > ExitMargin) return true;
        }
        return false;
    }

    // Ucgen sahada temsili dizilis: agirlik merkezi cevresinde on misket.
    private static readonly float[,] TriLayout =
    {
        { -0.62f, 0.30f }, { 0f, 0.30f }, { 0.62f, 0.30f },
        { -0.31f, 0.86f }, { 0.31f, 0.86f },
        { -0.93f, -0.26f }, { -0.31f, -0.26f }, { 0.31f, -0.26f }, { 0.93f, -0.26f },
        { 0f, 1.42f }
    };

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
