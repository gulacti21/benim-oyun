using UnityEditor;
using UnityEngine;

// Statik olarak dogrulanabilen yapisal kurallar.
//
// NOT: "zorluk mahalleler boyunca tirmaniyor mu" kontrolu BURADA DEGIL.
// Once "1 yildiz / atis hakki" olcusuyle denendi ve olcum onu curuttu: bolumlerin
// fiziksel tavani birbirinden cok farkli, ayni hedef bir bolumde bol payli digerinde
// imkansiz oluyor. Gercek olcu PAY (tavan - hedef) ve tavani bilmek simulasyon
// gerektiriyor -- o kontrol MISKETR/Verify All Designed Levels ozetinde.
public static class DifficultyOrderVerify
{
    private static int passed, failed;
    private static readonly string[] Districts = { "Apartman Onu", "Okul Bahcesi", "Park", "Toprak Saha", "Mahalle Meydani" };

    [MenuItem("MISKETR/Verify Difficulty Order")]
    public static void Run()
    {
        passed = 0; failed = 0;
        var db = Campaign.Database;

        // Bes mahallenin altmis bolumu de artik elle tasarlandi.
        for (int district = 0; district < Districts.Length; district++)
            for (int local = 0; local < 12; local++)
            {
                var l = db.Get(district * 12 + local);
                string name = Districts[district] + " " + (local + 1);

                Check(l.oneStarTarget <= l.twoStarTarget && l.twoStarTarget <= l.threeStarTarget,
                      "Yildiz hedefleri artan olmali: " + name);
                Check(l.threeStarTarget <= l.TotalMarbles(),
                      "3 yildiz bolumdeki misket sayisini asamaz: " + name);
                Check(l.oneStarTarget >= 1 && l.shotCount >= 1,
                      "Gecerli hedef ve atis hakki: " + name);
                Check(l.marbles != null && l.marbles.Length > 0,
                      "Elle yerlestirilmis misketler olmali: " + name);
                // Kilit sarti 2 yildizsa, 2 yildiz 3 yildizdan dusuk olmali ki kilit
                // ile ustalik ayri seyler olsun.
                if (l.starsToPass == 2)
                    Check(l.twoStarTarget < l.threeStarTarget,
                          "Kilit ve ustalik ayni sayi olmamali: " + name);
                // Uc yildiz sarti olan bolumde uc esik de ayri olmali, yoksa
                // "gec" ile "ustalik" ayni sey olur ve yildizlar anlamini yitirir.
                if (l.starsToPass == 3)
                    Check(l.oneStarTarget < l.twoStarTarget && l.twoStarTarget < l.threeStarTarget,
                          "Uc yildiz sartinda uc esik de ayri olmali: " + name);
            }

        try { passed += MemleketChecks(); }
        catch (System.Exception e) { failed++; Debug.LogError("DIFFICULTY_ORDER: " + e.Message); }

        Debug.Log(failed == 0
            ? "DIFFICULTY_ORDER_OK: " + passed + " checks passed."
            : "DIFFICULTY_ORDER_FAIL: " + failed + " / " + (passed + failed));
    }

    // ---------------------------------------------------------------
    // HARİTA 2 (Memleket) ZORLUK SIRASI. Ölçü: temizlenebilirlik = açgözlü tavan / toplam
    // (Harita 1 merdiveninin ölçüsü), MemleketBook.Tuning'deki ölçülmüş tavandan.
    // Bir misket = 100/toplam puan; sıra kuralları bu ÖLÇÜM ADIMI kadar pay tanır, çünkü
    // 9 misketli bölümde bir misket 11 puan — daha ince ayrım ölçülemez.
    // Kural bozulursa CHECK FAILED (MahalleVerify de çağırır).
    public static readonly float[] RegionStart = { .62f, .58f, .55f, .52f, .49f };
    public static readonly float[] RegionEnd = { .55f, .51f, .48f, .45f, .40f };
    public const float Floor = .40f;
    public const float H1Average = (.97f + .85f + .66f + .62f + .57f) / 5f;

    public static float Clear(int local)
    {
        var l = MemleketCampaign.Database.Get(local);
        int c = MemleketBook.MeasuredCeiling(local);
        return c < 0 ? -1f : c / (float)l.TotalMarbles();
    }
    private static float Step(int local) => 1f / MemleketCampaign.Database.Get(local).TotalMarbles();

    public static int MemleketChecks()
    {
        int n = 0;
        void Must(bool ok, string m) { n++; if (!ok) throw new System.Exception("CHECK FAILED: " + m); }
        var report = new System.Collections.Generic.List<string>();
        float sum = 0f; int measured = 0;
        float prevRegionStart = 1f, prevRegionEnd = 1f;
        for (int r = 0; r < 5; r++)
        {
            float regionMin = 1f;
            for (int k = 0; k < 12; k++)
            {
                int i = r * 12 + k;
                float c = Clear(i);
                Must(c >= 0f, "Memleket " + i + " measured");
                sum += c; measured++;
                regionMin = Mathf.Min(regionMin, c);
                Must(c >= Floor - 1e-4f, "Memleket " + i + " clearability floor 40% (" + Mathf.RoundToInt(c * 100) + ")");
                if (k > 0)
                    // Pay: iki bölümden hangisi daha kaba ölçülüyorsa onun bir misketi.
                    Must(c <= Clear(i - 1) + Mathf.Max(Step(i), Step(i - 1)) + 1e-4f, "Memleket " + i + " not easier than previous by more than one marble (" +
                         Mathf.RoundToInt(Clear(i - 1) * 100) + " -> " + Mathf.RoundToInt(c * 100) + ")");
                float want = Mathf.Lerp(RegionStart[r], RegionEnd[r], k / 11f);
                if (Mathf.Abs(c - want) > .03f)
                    report.Add("  " + MemleketCampaign.Districts[r] + " " + (k + 1).ToString("00") + ": %" + Mathf.RoundToInt(c * 100) + " (hedef %" + Mathf.RoundToInt(want * 100) + ")");
            }
            float start = Clear(r * 12), end = Clear(r * 12 + 11);
            // Bölge başı bir önceki bölgenin sonundan en fazla ~7 puan (+ bir misket) kolay başlayabilir.
            if (r > 0) Must(start <= prevRegionEnd + .07f + Step(r * 12) + 1e-4f, "Memleket region " + r + " starts at most ~7 points easier than previous end");
            if (r > 0) Must(start <= prevRegionStart + Step(r * 12) + 1e-4f, "Memleket region starts fall, within one marble (" + r + ")");
            Must(end <= regionMin + Step(r * 12 + 11) + 1e-4f, "Memleket region " + r + " mastery is its hardest (within one marble)");
            prevRegionStart = start; prevRegionEnd = end;
        }
        float avg = sum / measured;
        Must(avg < H1Average - .05f, "Memleket average clearly harder than Mahalle (" + Mathf.RoundToInt(avg * 100) + " vs " + Mathf.RoundToInt(H1Average * 100) + ")");
        Debug.Log("MEMLEKET_ORDER: ortalama temizlenebilirlik %" + Mathf.RoundToInt(avg * 100) + " (Mahalle %" + Mathf.RoundToInt(H1Average * 100) + "), hedef ±3 dışında " + report.Count + " bölüm");
        foreach (var line in report) Debug.Log("MEMLEKET_ORDER hedef dışı:" + line);
        return n;
    }

    private static void Check(bool condition, string message)
    {
        if (condition) { passed++; return; }
        failed++; Debug.LogError("DIFFICULTY_ORDER: " + message);
    }
}
