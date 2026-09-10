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
    private static readonly string[] Districts = { "Apartman Onu", "Okul Bahcesi", "Park" };

    [MenuItem("MISKETR/Verify Difficulty Order")]
    public static void Run()
    {
        passed = 0; failed = 0;
        var db = Campaign.Database;

        // Elle tasarlanmis ilk uc mahalle. Toprak Saha ve Meydan hala formulle uretiliyor.
        for (int district = 0; district < 3; district++)
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
            }

        Debug.Log(failed == 0
            ? "DIFFICULTY_ORDER_OK: " + passed + " checks passed."
            : "DIFFICULTY_ORDER_FAIL: " + failed + " / " + (passed + failed));
    }

    private static void Check(bool condition, string message)
    {
        if (condition) { passed++; return; }
        failed++; Debug.LogError("DIFFICULTY_ORDER: " + message);
    }
}
