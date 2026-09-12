using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Online mac kurallarinin testleri. Kural motoru saf C# oldugu icin
// sahne, fizik ve ag olmadan burada dogrulanabiliyor.
public static class DuelVerify
{
    private static int passed, failed;

    [MenuItem("MISKETR/Verify Duel Rules")]
    public static void Run()
    {
        passed = failed = 0;
        int n = RunChecks();
        Debug.Log(failed == 0 ? "DUEL_OK: " + n + " checks passed." : "DUEL_FAIL: " + failed + " / " + n);
    }

    private static void Check(bool ok, string message)
    {
        if (ok) { passed++; return; }
        failed++;
        Debug.LogError("DUEL: " + message);
        throw new Exception("CHECK FAILED: " + message);
    }

    private static List<(float, float)> Spots(float x)
    {
        var list = new List<(float, float)>();
        for (int i = 0; i < DuelMatch.MarblesPerPlayer; i++) list.Add((x, i * .6f));
        return list;
    }

    private static DuelMatch Ready(int firstPlacer = 0, bool big = true)
    {
        var m = new DuelMatch(firstPlacer);
        m.Place(0, Spots(-1f));
        m.Place(1, Spots(1f));
        if (big) m.PlaceBigMarble(0f, 0f);
        return m;
    }

    // Belirli bir sahibin cemberde duran misketlerinden n tanesinin indeksi.
    private static List<int> Of(DuelMatch m, int owner, int count)
    {
        var hit = new List<int>();
        for (int i = 0; i < m.Marbles.Count && hit.Count < count; i++)
            if (m.Marbles[i].owner == owner && !m.Marbles[i].out_) hit.Add(i);
        return hit;
    }

    private static List<int> Big(DuelMatch m)
    {
        for (int i = 0; i < m.Marbles.Count; i++) if (m.Marbles[i].big) return new List<int> { i };
        return new List<int>();
    }

    public static int RunChecks()
    {
        // --- Hazirlik ---
        var m = new DuelMatch();
        Check(m.State == DuelMatch.Phase.Placing, "Mac dizme asamasinda baslar");
        Check(!m.Place(0, new List<(float, float)> { (0f, 0f) }), "Eksik dizilis reddedilir");
        Check(m.Place(0, Spots(-1f)), "Gecerli dizilis kabul edilir");
        Check(!m.Place(0, Spots(-1f)), "Ayni oyuncu iki kez dizemez");
        Check(m.State == DuelMatch.Phase.Placing, "Tek oyuncu dizince cember acilmaz");
        Check(m.Place(1, Spots(1f)), "Ikinci oyuncu dizer");
        Check(m.State == DuelMatch.Phase.Shooting, "Ikisi de dizince atisa gecilir");
        Check(m.InRing(0) == 7 && m.InRing(1) == 7, "Herkes 7 misketle baslar");

        // --- Ilk dizen ikinci atar ---
        m = Ready(0);
        Check(m.FirstPlacer == 0 && m.Turn == 1, "Ilk dizen ikinci atar");
        m = Ready(1);
        Check(m.FirstPlacer == 1 && m.Turn == 0, "Ilk dizen 2. oyuncuysa 1. atar");

        // --- Puan: rakibin misketi sana, kendi misketin rakibe ---
        m = Ready(1);                       // sira 0'da
        Check(m.ResolveShot(Of(m, 1, 1)), "Rakibin misketini cikaran tekrar atar");
        Check(m.Score(0) == 1 && m.Score(1) == 0, "Rakibin misketi cikarana yazilir");
        Check(!m.ResolveShot(Of(m, 0, 1)), "Kendi misketini cikarmak turu uzatmaz");
        Check(m.Score(1) == 1, "Kendi misketin rakibe yazilir");
        Check(m.Turn == 1, "Kotu atistan sonra sira gecer");

        // --- Buyuk misket iki puan ---
        m = Ready(1);
        Check(m.ResolveShot(Big(m)), "Buyuk misketi cikaran tekrar atar");
        Check(m.Score(0) == DuelMatch.BigMarbleValue, "Buyuk misket iki puan");
        Check(m.Score(1) == 0, "Buyuk misket rakibe puan yazmaz");

        // --- Zincir en fazla uc atis ---
        m = Ready(1);
        Check(m.ResolveShot(Of(m, 1, 1)), "Birinci zincir atisi");
        Check(m.ResolveShot(Of(m, 1, 1)), "Ikinci zincir atisi");
        Check(!m.ResolveShot(Of(m, 1, 1)), "Turda en fazla uc atis");
        Check(m.Turn == 1, "Uc atistan sonra sira gecer");
        Check(m.Score(0) == 3, "Zincirdeki her misket puan yazar");

        // --- Iska ---
        m = Ready(1);
        Check(!m.ResolveShot(new List<int>()), "Iskalayinca sira gecer");
        Check(m.Score(0) == 0 && m.Score(1) == 0, "Iska puan yazmaz");

        // --- Cember bosalinca mac biter ---
        m = Ready(1);
        var hepsi = new List<int>();
        for (int i = 0; i < m.Marbles.Count; i++) hepsi.Add(i);
        m.ResolveShot(hepsi);
        Check(m.State == DuelMatch.Phase.Finished, "Cember bosalinca mac biter");
        Check(m.RemainingInRing == 0, "Cemberde misket kalmadi");
        Check(m.Score(0) + m.Score(1) == DuelMatch.MarblesPerPlayer * 2 + DuelMatch.BigMarbleValue,
              "Toplam puan 16 olmali");

        // --- Kazanan skora gore ---
        m = Ready(1);
        m.ResolveShot(Of(m, 1, 3));
        while (m.State == DuelMatch.Phase.Shooting) m.ForfeitTurn();
        Check(m.Result == DuelMatch.Outcome.PlayerOne, "Cok puan toplayan kazanir");

        // --- Herkes dort tur oynar ---
        m = Ready(1);
        Check(m.TurnsLeft(0) == DuelMatch.TurnsPerPlayer, "Herkes dort turla baslar");
        for (int i = 0; i < DuelMatch.TurnsPerPlayer * 2; i++) m.ForfeitTurn();
        Check(m.Overtime, "Dort tur dolunca skorlar esitse uzatma");
        Check(m.State == DuelMatch.Phase.Shooting, "Uzatmada mac devam eder");
        m.ForfeitTurn(); m.ForfeitTurn();
        Check(m.State == DuelMatch.Phase.Finished, "Uzatma bir tur surer");
        Check(m.Result == DuelMatch.Outcome.Draw, "Uzatmada da esitlik bozulmazsa berabere");

        // --- Onde olan varsa uzatma yok ---
        m = Ready(1);
        m.ResolveShot(Of(m, 1, 1));                 // 0 one gecti
        while (m.State == DuelMatch.Phase.Shooting) m.ForfeitTurn();
        Check(!m.Overtime, "Onde olan varken uzatma oynanmaz");
        Check(m.Result == DuelMatch.Outcome.PlayerOne, "Dort tur sonunda onde olan kazanir");

        // --- Tur sayaci: zincir tek tur sayilir ---
        m = Ready(1);
        m.ResolveShot(Of(m, 1, 1));                 // zincir, sira ayni oyuncuda
        Check(m.TurnsLeft(0) == DuelMatch.TurnsPerPlayer, "Zincir devam ederken tur harcanmaz");
        m.ResolveShot(new List<int>());             // iska, tur biter
        Check(m.TurnsLeft(0) == DuelMatch.TurnsPerPlayer - 1, "Tur bitince sayac duser");

        // --- Bitmis macta atis islenmez ---
        m = Ready(1);
        m.ResolveShot(hepsi);
        int oncekiSkor = m.Score(0);
        Check(!m.ResolveShot(Of(m, 1, 1)), "Bitmis macta atis islenmez");
        Check(m.Score(0) == oncekiSkor, "Bitmis macta puan degismez");

        // --- Ayni misket iki kez sayilmaz ---
        m = Ready(1);
        var tek = Of(m, 1, 1);
        m.ResolveShot(tek);
        int skor = m.Score(0);
        m.ResolveShot(tek);
        Check(m.Score(0) == skor, "Cikmis misket tekrar puan yazmaz");

        // --- Rovans ---
        m = Ready(0);
        var r = m.Rematch();
        Check(r.FirstPlacer == 1, "Rovansta ilk dizen degisir");
        Check(r.Turn == 0, "Ilk dizen degisince ilk atan da degisir");
        Check(r.State == DuelMatch.Phase.Placing, "Rovans dizmeden baslar");
        Check(r.MatchNumber == 1, "Rovans sayaci artar");
        Check(r.Rematch().FirstPlacer == 0, "Ikinci rovansta el geri doner");

        // --- Dizme kurallari ---
        const float arena = 3.2f;
        Check(DuelPlacement.Inside(0f, 0f, arena), "Merkez gecerli");
        Check(!DuelPlacement.Inside(3.1f, 0f, arena), "Cizgiye yapisik nokta reddedilir");
        var c = DuelPlacement.Clamp(9f, 0f, arena);
        Check(DuelPlacement.Inside(c.x, c.y, arena), "Disari tasan nokta ice cekilir");

        var cakisik = new List<Vector2> { new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(.1f, 1f) };
        var cozum = DuelPlacement.Resolve(cakisik, arena);
        for (int i = 0; i < cozum.Count; i++)
            for (int j = i + 1; j < cozum.Count; j++)
                Check(Vector2.Distance(cozum[i], cozum[j]) > DuelPlacement.MinGap - .02f, "Cakisan misketler ayrilir");
        foreach (var q in cozum) Check(DuelPlacement.Inside(q.x, q.y, arena), "Ayirma sonrasi hepsi cemberde kalir");

        var tekrar = DuelPlacement.Resolve(cakisik, arena);
        for (int i = 0; i < cozum.Count; i++)
            Check((cozum[i] - tekrar[i]).sqrMagnitude < .0000001f, "Ayirma sonucu her calistirmada ayni");

        for (int pl = 0; pl < 2; pl++)
        {
            var d = DuelPlacement.DefaultLayout(pl, DuelMatch.MarblesPerPlayer, arena);
            Check(d.Count == DuelMatch.MarblesPerPlayer, "Hazir dizilis tam sayida");
            foreach (var q in d) Check(DuelPlacement.Inside(q.x, q.y, arena), "Hazir dizilis cemberin icinde");
            for (int i = 0; i < d.Count; i++)
                for (int j = i + 1; j < d.Count; j++)
                    Check(Vector2.Distance(d[i], d[j]) > DuelPlacement.MinGap - .02f, "Hazir diziliste cakisma yok");
            // Hazir dizilis ortadaki buyuk misketin yerini isgal etmemeli.
            foreach (var q in d)
                Check(Vector2.Distance(q, DuelSession.BigMarbleSpot) > DuelPlacement.MinGap * DuelSession.BigScale - .02f,
                      "Hazir dizilis buyuk misketin yerine girmez");
        }

        return passed;
    }
}
