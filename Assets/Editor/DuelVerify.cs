using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Online maç kurallarının testleri. Kural motoru saf C# oldugu icin
// sahne, fizik ve ag olmadan burada dogrulanabiliyor.
public static class DuelVerify
{
    private static int passed, failed;

    [MenuItem("MISKETR/Verify Duel Rules")]
    public static void Run()
    {
        passed = failed = 0;
        int n = RunChecks();
        Debug.Log(failed == 0 ? "DUEL_OK: " + n + " checks passed."
                              : "DUEL_FAIL: " + failed + " / " + n);
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

    private static DuelMatch Ready(int starter = 0)
    {
        var m = new DuelMatch(starter);
        m.Place(0, Spots(-1f));
        m.Place(1, Spots(1f));
        return m;
    }

    // Belirli bir oyuncunun misketlerinden n tanesinin indeksi.
    private static List<int> Of(DuelMatch m, int owner, int count)
    {
        var hit = new List<int>();
        for (int i = 0; i < m.Marbles.Count && hit.Count < count; i++)
            if (m.Marbles[i].owner == owner && !m.Marbles[i].out_) hit.Add(i);
        return hit;
    }

    public static int RunChecks()
    {
        // --- Hazırlık ---
        var m = new DuelMatch();
        Check(m.State == DuelMatch.Phase.Placing, "Mac dizme asamasinda baslar");
        Check(!m.Place(0, new List<(float, float)> { (0f, 0f) }), "Eksik dizilis reddedilir");
        Check(m.Place(0, Spots(-1f)), "Gecerli dizilis kabul edilir");
        Check(!m.Place(0, Spots(-1f)), "Ayni oyuncu iki kez dizemez");
        Check(m.State == DuelMatch.Phase.Placing, "Tek oyuncu dizince cember acilmaz");
        Check(m.Place(1, Spots(1f)), "Ikinci oyuncu dizer");
        Check(m.State == DuelMatch.Phase.Shooting, "Ikisi de dizince atisa gecilir");
        Check(m.InRing(0) == 7 && m.InRing(1) == 7, "Herkes 7 misketle baslar");

        // --- Sira ve zincir ---
        m = Ready();
        Check(m.Turn == 0, "Baslayan oyuncu sirayi alir");
        Check(m.ResolveShot(Of(m, 1, 1)), "Rakibin misketini cikaran tekrar atar");
        Check(m.Turn == 0 && m.ShotsThisTurn == 1, "Zincirde sira ayni oyuncuda kalir");
        Check(m.ResolveShot(Of(m, 1, 1)), "Ikinci zincir atisi");
        Check(!m.ResolveShot(Of(m, 1, 1)), "Turda en fazla uc atis");
        Check(m.Turn == 1, "Uc atistan sonra sira gecer");
        Check(m.InRing(1) == 4, "Uc misket cikti");

        // --- Kendi misketini cikarmak turu uzatmaz ---
        m = Ready();
        Check(!m.ResolveShot(Of(m, 0, 1)), "Kendi misketini cikarmak turu uzatmaz");
        Check(m.InRing(0) == 6, "Kendi misketi de cemberden gider");
        Check(m.Turn == 1, "Kotu atistan sonra sira gecer");

        // --- Iskalamak ---
        m = Ready();
        Check(!m.ResolveShot(new List<int>()), "Iskalayinca sira gecer");
        Check(m.Turn == 1, "Iska sonrasi rakibin sirasi");
        Check(m.TurnsLeft(0) == DuelMatch.TurnsPerPlayer - 1, "Iska da bir tur harcar");

        // --- Turlar bitince mac biter ---
        m = Ready();
        for (int i = 0; i < DuelMatch.TurnsPerPlayer * 2; i++) m.ResolveShot(new List<int>());
        Check(m.State == DuelMatch.Phase.Finished, "Turlar bitince mac biter");
        Check(m.Result == DuelMatch.Outcome.Draw, "Kimse misket cikarmadiysa beraberlik");

        // --- Biri tukenirse mac erken biter ---
        m = Ready();
        Check(!m.ResolveShot(Of(m, 1, 7)), "Rakibin butun misketleri cikti");
        Check(m.State == DuelMatch.Phase.Finished, "Bir taraf tukenince mac biter");
        Check(m.Result == DuelMatch.Outcome.PlayerOne, "Cemberde misketi kalan kazanir");

        // --- Ikinci oyuncu kazanir ---
        m = Ready(1);
        Check(m.Turn == 1, "Baslayan oyuncu ikinci de olabilir");
        m.ResolveShot(Of(m, 0, 7));
        Check(m.Result == DuelMatch.Outcome.PlayerTwo, "Ikinci oyuncu da kazanabilir");

        // --- Turu biten oyuncuya sira gelmez ---
        m = Ready();
        for (int i = 0; i < DuelMatch.TurnsPerPlayer; i++) { m.ForfeitTurn(); if (m.Turn == 1) m.ForfeitTurn(); }
        Check(m.State == DuelMatch.Phase.Finished, "Iki tarafin da turu bitince mac biter");

        // --- Bitmis macta atis islenmez ---
        m = Ready();
        m.ResolveShot(Of(m, 1, 7));
        Check(!m.ResolveShot(Of(m, 0, 1)), "Bitmis macta atis islenmez");
        Check(m.InRing(0) == 7, "Bitmis macta misket kaybedilmez");

        // --- Ayni misket iki kez sayilmaz ---
        m = Ready();
        var tek = Of(m, 1, 1);
        m.ResolveShot(tek);
        int before = m.InRing(1);
        m.ResolveShot(tek);
        Check(m.InRing(1) == before, "Cikmis misket tekrar sayilmaz");

        // --- Rovans ---
        m = Ready();
        var r = m.Rematch();
        Check(r.StartingPlayer == 1, "Rovansta ilk baslayan degisir");
        Check(r.State == DuelMatch.Phase.Placing, "Rovans dizmeden baslar");
        Check(r.MatchNumber == 1, "Rovans sayaci artar");
        Check(r.Rematch().StartingPlayer == 0, "Ikinci rovansta sira geri doner");

        // --- Dizme kurallari ---
        const float arena = 3.2f;
        Check(DuelPlacement.Inside(0f, 0f, arena), "Merkez gecerli");
        Check(!DuelPlacement.Inside(3.1f, 0f, arena), "Cizgiye yapisik nokta reddedilir");
        var c = DuelPlacement.Clamp(9f, 0f, arena);
        Check(DuelPlacement.Inside(c.x, c.y, arena), "Disari tasan nokta ice cekilir");

        // Ust uste binen iki misket ayrilir.
        var cakisik = new List<Vector2> { new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(.1f, 1f) };
        var cozum = DuelPlacement.Resolve(cakisik, arena);
        for (int i = 0; i < cozum.Count; i++)
            for (int j = i + 1; j < cozum.Count; j++)
                Check(Vector2.Distance(cozum[i], cozum[j]) > DuelPlacement.MinGap - .02f, "Cakisan misketler ayrilir");
        foreach (var q in cozum) Check(DuelPlacement.Inside(q.x, q.y, arena), "Ayirma sonrasi hepsi cemberde kalir");

        // Ayni girdi her cihazda ayni sonucu vermeli (ag icin sart).
        var tekrar = DuelPlacement.Resolve(cakisik, arena);
        for (int i = 0; i < cozum.Count; i++)
            Check((cozum[i] - tekrar[i]).sqrMagnitude < .0000001f, "Ayirma sonucu her calistirmada ayni");

        // Hazir dizilisler gecerli olmali.
        for (int pl = 0; pl < 2; pl++)
        {
            var d = DuelPlacement.DefaultLayout(pl, DuelMatch.MarblesPerPlayer, arena);
            Check(d.Count == DuelMatch.MarblesPerPlayer, "Hazir dizilis tam sayida");
            foreach (var q in d) Check(DuelPlacement.Inside(q.x, q.y, arena), "Hazir dizilis cemberin icinde");
            for (int i = 0; i < d.Count; i++)
                for (int j = i + 1; j < d.Count; j++)
                    Check(Vector2.Distance(d[i], d[j]) > DuelPlacement.MinGap - .02f, "Hazir diziliste cakisma yok");
        }

        return passed;
    }
}
