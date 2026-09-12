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

    private static List<(float, float)> Spots(float x, int count)
    {
        var list = new List<(float, float)>();
        for (int i = 0; i < count; i++) list.Add((x, i * .6f));
        return list;
    }

    private static List<(float, float)> ToTuple(List<Vector2> v)
    {
        var list = new List<(float, float)>();
        foreach (var q in v) list.Add((q.x, q.y));
        return list;
    }

    private static DuelMatch Ready(int firstPlacer = 0)
    {
        var m = new DuelMatch(firstPlacer);
        m.Place(0, Spots(-1f, m.AnteFor(0)));
        m.Place(1, Spots(1f, m.AnteFor(1)));
        return m;
    }

    // Cemberde duran misketlerden n tanesinin indeksi.
    private static List<int> Ring(DuelMatch m, int count)
    {
        var hit = new List<int>();
        for (int i = 0; i < m.Marbles.Count && hit.Count < count; i++)
            if (!m.Marbles[i].out_) hit.Add(i);
        return hit;
    }

    public static int RunChecks()
    {
        // --- El hazirligi ve kese ---
        var m = new DuelMatch();
        Check(m.State == DuelMatch.Phase.Placing, "Mac dizme asamasinda baslar");
        Check(m.Pouch(0) == DuelMatch.StartingPouch && m.Pouch(1) == DuelMatch.StartingPouch, "Herkes yirmi misketle baslar");
        Check(m.AnteFor(0) == DuelMatch.AntePerRound, "Ilk el bes misket ortaya konur");
        Check(!m.Place(0, Spots(-1f, 2)), "Eksik dizilis reddedilir");
        Check(m.Place(0, Spots(-1f, DuelMatch.AntePerRound)), "Gecerli dizilis kabul edilir");
        Check(m.Pouch(0) == DuelMatch.StartingPouch - DuelMatch.AntePerRound, "Ortaya konan misket keseden duser");
        Check(!m.Place(0, Spots(-1f, DuelMatch.AntePerRound)), "Ayni oyuncu iki kez dizemez");
        Check(m.State == DuelMatch.Phase.Placing, "Tek oyuncu dizince cember acilmaz");
        Check(m.Place(1, Spots(1f, DuelMatch.AntePerRound)), "Ikinci oyuncu dizer");
        Check(m.State == DuelMatch.Phase.Shooting, "Ikisi de dizince atisa gecilir");
        Check(m.RemainingInRing == DuelMatch.AntePerRound * 2, "Cemberde on misket var");

        // --- Ilk dizen ikinci atar ---
        m = Ready(0); Check(m.Turn == 1, "Ilk dizen ikinci atar");
        m = Ready(1); Check(m.Turn == 0, "Ilk dizen 2. oyuncuysa 1. atar");

        // --- Cikardigin misket kesene girer ---
        m = Ready(1);
        int once = m.Pouch(0);
        Check(m.ResolveShot(Ring(m, 1), false), "Misket cikaran tekrar atar");
        Check(m.Pouch(0) == once + 1, "Cikardigin misket kesene girer");
        Check(m.Pouch(1) == DuelMatch.StartingPouch - DuelMatch.AntePerRound, "Rakibin kesesi degismez");

        // --- Zincir en fazla uc atis ---
        m = Ready(1);
        Check(m.ResolveShot(Ring(m, 1), false), "Birinci zincir atisi");
        Check(m.ResolveShot(Ring(m, 1), false), "Ikinci zincir atisi");
        Check(!m.ResolveShot(Ring(m, 1), false), "Turda en fazla uc atis");
        Check(m.Turn == 1, "Uc atistan sonra sira gecer");

        // --- Atici cemberde kalirsa kaybedilir ---
        m = Ready(1);
        once = m.Pouch(0);
        int cemberde = m.RemainingInRing;
        Check(!m.ResolveShot(null, true, .5f, .5f), "Atici cemberde kalinca tur biter");
        Check(m.Pouch(0) == once - 1, "Cemberde kalan atici keseden duser");
        Check(m.RemainingInRing == cemberde + 1, "Kalan atici ortada hedef olur");

        // --- Cemberde kalan atici rakip tarafindan alinabilir ---
        int hedef = -1;
        for (int i = 0; i < m.Marbles.Count; i++) if (m.Marbles[i].stranded) hedef = i;
        Check(hedef >= 0, "Kalan atici listede isaretli");
        int rakipOnce = m.Pouch(1);
        m.ResolveShot(new List<int> { hedef }, false);
        Check(m.Pouch(1) == rakipOnce + 1, "Rakip kalan aticiyi kesesine katar");

        // --- Misket cikarsan bile atici cemberde kaldiysa zincir yok ---
        m = Ready(1);
        Check(!m.ResolveShot(Ring(m, 1), true, .3f, .3f), "Atici cemberde kaldiysa zincir kesilir");

        // --- Cember bosalinca el biter ---
        m = Ready(1);
        m.ResolveShot(Ring(m, m.RemainingInRing), false);
        Check(m.State == DuelMatch.Phase.RoundOver, "Cember bosalinca el biter");
        Check(m.Round == 1, "El bitti ama sonraki ele henuz gecilmedi");
        m.NextRound();
        Check(m.Round == 2 && m.State == DuelMatch.Phase.Placing, "Sonraki el dizmeyle baslar");

        // --- Ikinci el otomatik dizilir, ekstra hak elle dizdirir ---
        Check(!m.PlacesByHand(0), "Ikinci el varsayilan olarak otomatik dizilir");
        Check(m.PlacementRights(0) == DuelMatch.ExtraPlacements, "Ekstra dizme hakki duruyor");
        Check(m.UsePlacementRight(0), "Ekstra hak kullanilabilir");
        Check(m.PlacesByHand(0), "Hak kullanilinca elle dizilir");
        Check(m.PlacementRights(0) == 0, "Hak bir kez kullanilir");
        Check(!m.UsePlacementRight(0), "Biten hak tekrar kullanilamaz");

        // --- Tikanan elde kalan misketler devreder ---
        m = Ready(1);
        m.ResolveShot(Ring(m, 2), false);
        while (m.State == DuelMatch.Phase.Shooting) m.ForfeitTurn();
        Check(m.State == DuelMatch.Phase.RoundOver, "Dort bos tur eli bitirir");
        int devreden = m.RemainingInRing;
        Check(devreden > 0, "Cemberde misket kalmis");
        m.NextRound();
        Check(m.RemainingInRing == devreden, "Kalan misketler sonraki ele devreder");

        // --- Her el ilk dizen degisir ---
        m = Ready(0);
        Check(m.FirstPlacer == 0, "Ilk elde birinci dizer");
        m.ResolveShot(Ring(m, m.RemainingInRing), false);
        m.NextRound();
        Check(m.FirstPlacer == 1, "Ikinci elde ikinci dizer");
        Check(m.Turn == 0, "Dizen degisince atan da degisir");

        // --- Bes el sonunda mac biter ---
        m = Ready(0);
        for (int el = 1; el <= DuelMatch.RoundsPerMatch; el++)
        {
            if (m.State == DuelMatch.Phase.Placing)
            {
                m.Place(0, Spots(-1f, m.AnteFor(0)));
                m.Place(1, Spots(1f, m.AnteFor(1)));
            }
            if (m.State == DuelMatch.Phase.Shooting) m.ResolveShot(Ring(m, m.RemainingInRing), false);
            if (m.State == DuelMatch.Phase.RoundOver) m.NextRound();
        }
        Check(m.State == DuelMatch.Phase.Finished, "Bes el sonunda mac biter");
        // Kazanani sabit varsayamayiz: her el ilk dizen degistigi icin ilk atan
        // da donusumlu oluyor. Kural su: kesesi kalabalik olan kazanir.
        var beklenen = m.Pouch(0) > m.Pouch(1) ? DuelMatch.Outcome.PlayerOne
                     : m.Pouch(1) > m.Pouch(0) ? DuelMatch.Outcome.PlayerTwo
                     : DuelMatch.Outcome.Draw;
        Check(m.Result == beklenen, "Kesesi kalabalik olan kazanir");
        Check(m.Pouch(0) + m.Pouch(1) == DuelMatch.StartingPouch * 2, "Misketler kaybolmaz, el degistirir");

        // --- Bitmis macta atis islenmez ---
        int kese = m.Pouch(0);
        Check(!m.ResolveShot(new List<int> { 0 }, false), "Bitmis macta atis islenmez");
        Check(m.Pouch(0) == kese, "Bitmis macta kese degismez");

        // --- Ayni misket iki kez sayilmaz ---
        m = Ready(1);
        var tek = Ring(m, 1);
        m.ResolveShot(tek, false);
        kese = m.Pouch(0);
        m.ResolveShot(tek, false);
        Check(m.Pouch(0) == kese, "Cikmis misket tekrar kesene girmez");

        // --- Rovans ---
        m = Ready(0);
        var r = m.Rematch();
        Check(r.FirstPlacer == 1, "Rovansta ilk dizen degisir");
        Check(r.Turn == 0, "Ilk dizen degisince ilk atan da degisir");
        Check(r.Pouch(0) == DuelMatch.StartingPouch, "Rovansta kese sifirlanir");
        Check(r.Round == 1 && r.State == DuelMatch.Phase.Placing, "Rovans ilk elden baslar");
        Check(r.PlacementRights(0) == DuelMatch.ExtraPlacements, "Rovansta ekstra hak geri gelir");

        // --- Sira belirleme atisi ---
        DuelToss.Reset(0);
        Check(!DuelToss.Done, "Sira atisi basta bitmemis");
        Check(DuelToss.Shooter == 0, "Sira atisini birinci oyuncu baslatir");
        Check(DuelToss.Winner == -1, "Atis bitmeden kazanan yok");
        Check(DuelToss.ScoreText(0) == "\u2014", "Atmayan oyuncunun skoru bos");

        // Cizgi: cemberde uzak kenar, ucgende uzak YATAY kenar (z = .5r).
        Check(Mathf.Approximately(DuelToss.Line, DuelSession.CircleSize), "Cemberde cizgi uzak kenar");

        Check(Mathf.Approximately(DuelToss.Measure(0f, DuelToss.Line, false), 0f),
              "Cizginin uzerinde duran sifir alir");
        Check(DuelToss.Measure(0f, DuelToss.Line - 1f, false) > DuelToss.Measure(0f, DuelToss.Line - .2f, false),
              "Cizgiye yakin olan daha iyi");
        Check(DuelToss.Measure(1.5f, DuelToss.Line - .2f, false) > DuelToss.Measure(0f, DuelToss.Line - .2f, false),
              "Yandan sapan ceza alir");

        // Yanma SADECE cizgiyi gecmekle olur. Atici sahanin gerisinden
        // basliyor, o yuzden "sahanin disinda" olmak yanma sayilmaz.
        Check(!DuelToss.Crossed(DuelToss.Line - .01f), "Cizginin gerisi yanmaz");
        Check(DuelToss.Crossed(DuelToss.Line + .01f), "Cizgiyi gecen yanar");
        Check(DuelToss.Measure(0f, -4f, false) < DuelToss.FoulPenalty,
              "Kisa kalan atis yanmaz, sadece kotu puan alir");
        Check(DuelToss.Measure(0f, DuelToss.Line + .1f, false) > DuelToss.FoulPenalty,
              "Cizgiyi gecen atis yanik puani alir");
        Check(DuelToss.Measure(0f, 0f, true) > DuelToss.FoulPenalty,
              "Dunyadan dusen atis da yanar");

        DuelToss.Record(0, DuelToss.Measure(0f, DuelToss.Line - .5f, false));
        Check(DuelToss.HasShot(0) && !DuelToss.HasShot(1), "Birinci atti, ikinci atmadi");
        Check(DuelToss.Shooter == 1, "Sira digerine gecer");
        float ilk = DuelToss.Score(0);
        DuelToss.Record(0, 0f);
        Check(Mathf.Approximately(DuelToss.Score(0), ilk), "Bir oyuncu iki kez atmaz");

        DuelToss.Record(1, DuelToss.Measure(0f, DuelToss.Line - 1.5f, false));
        Check(DuelToss.Done, "Iki atis sonra biter");
        Check(DuelToss.Winner == 0, "Cizgiye yakin olan kazanir");
        // Kazanan ONCE ATAR; kurallarda once atan ikinci dizendir.
        Check(DuelToss.FirstPlacer == 1, "Atisi kaybeden once dizer");
        var mt = new DuelMatch(DuelToss.FirstPlacer);
        Check(mt.Turn == DuelToss.Winner, "Atisi kazanan ilk atisi yapar");

        // Yanan oyuncu, cizginin gerisinde kalan rakibe kaybeder.
        DuelToss.Reset(1);
        Check(DuelToss.Shooter == 1, "Sira atisini ikinci oyuncu da baslatabilir");
        DuelToss.Record(1, DuelToss.Measure(0f, DuelToss.Line + .3f, false));
        DuelToss.Record(0, DuelToss.Measure(0f, 0f, false));
        Check(DuelToss.Fouled(1) && !DuelToss.Fouled(0), "Cizgiyi gecen yanar");
        Check(DuelToss.ScoreText(1) == "YANDI", "Yanan oyuncunun skoru yazili");
        Check(DuelToss.Winner == 0, "Yanan oyuncu kaybeder");

        // Ikisi de yandiysa az tasan kazanir.
        DuelToss.Reset(0);
        DuelToss.Record(0, DuelToss.Measure(0f, DuelToss.Line + 2f, false));
        DuelToss.Record(1, DuelToss.Measure(0f, DuelToss.Line + .2f, false));
        Check(DuelToss.Winner == 1, "Iki yanik atista az tasan kazanir");

        // Atis gucu: cizgi sliderin ortalarinda olmali. Olculdu (bkz.
        // DuelPhysicsVerify): guc 1.0 ile misket 27 birim gidiyor, cizgi
        // cemberde 7.4 birim otede. Sinirlar disina kayarsa fizik taramasi
        // "pencere dar" ya da "cizgi gecilemiyor" diyecek.
        for (int t = 0; t < 2; t++)
        {
            var eski = DuelSession.Type;
            DuelSession.Type = t == 0 ? DuelSession.GameType.Cember : DuelSession.GameType.Ucgen;
            float yol = DuelToss.Line - DuelSession.ShooterZ;
            float tahmin = DuelSession.TossImpulse * DuelSession.TossTravelPerImpulse;
            Check(yol > 0f, "Cizgi aticinin onunde");
            Check(tahmin > yol, "Tam gucte cizgi gecilebiliyor");
            Check(tahmin < yol * 2f, "Tam guc cizgiyi fahis asmiyor");
            Check(DuelSession.TossImpulse < DuelSession.Impulse,
                  "Sira atisi mac atisindan hafif");
            DuelSession.Type = eski;
        }

        // --- DIZI (KONDIK) MODU ---
        {
            var eski = DuelSession.Type;
            DuelSession.Type = DuelSession.GameType.Dizi;
            float sira = DuelSession.RowSize;
            Check(DuelSession.Row, "Dizi modu taniniyor");
            Check(DuelSession.Shape == ArenaShape.Line, "Dizi modunda saha sekli cizgi");
            Check(Mathf.Approximately(DuelToss.Line, DuelSession.RowZ), "Dizide cizgi siranin kendisi");
            Check(DuelSession.RowImpulse < DuelSession.Impulse,
                  "Dizide atis gucu cemberden dusuk");
            // Esik gercek temasin altinda, gurultunun ustunde olmali.
            Check(DuelSession.RowNudge > .01f && DuelSession.RowNudge < .09f,
                  "Kipirdama esigi olculen bantta");

            Check(DuelPlacement.Inside(0f, DuelSession.RowZ, sira), "Siranin ortasi gecerli");
            Check(!DuelPlacement.Inside(0f, DuelSession.RowZ + 1f, sira), "Siranin disi (onu) reddedilir");
            Check(!DuelPlacement.Inside(sira * 2f, DuelSession.RowZ, sira), "Siranin ucunu asan reddedilir");
            var kirp = DuelPlacement.Clamp(sira * 3f, 2f, sira);
            Check(DuelPlacement.Inside(kirp.x, kirp.y, sira), "Tasan nokta siraya cekilir");
            Check(Mathf.Approximately(kirp.y, DuelSession.RowZ), "Kirpilan nokta siranin uzerinde");

            // Otomatik dizilis: iki oyuncu GECMELI dizilir, cakisma yok.
            var d0 = DuelPlacement.DefaultLayout(0, DuelMatch.AntePerRound, sira);
            var d1 = DuelPlacement.DefaultLayout(1, DuelMatch.AntePerRound, sira);
            Check(d0.Count == DuelMatch.AntePerRound && d1.Count == DuelMatch.AntePerRound,
                  "Dizide hazir dizilis tam sayida");
            foreach (var q in d0) Check(Mathf.Approximately(q.y, DuelSession.RowZ), "Dizilis tek sirada (1. oyuncu)");
            foreach (var q in d1) Check(Mathf.Approximately(q.y, DuelSession.RowZ), "Dizilis tek sirada (2. oyuncu)");
            var hepsi = new List<Vector2>(d0); hepsi.AddRange(d1);
            for (int i = 0; i < hepsi.Count; i++)
                for (int j = i + 1; j < hepsi.Count; j++)
                    Check(Vector2.Distance(hepsi[i], hepsi[j]) > DuelPlacement.MinGap - .02f,
                          "Dizide iki misket cakismaz");
            // Gecmeli: her oyuncunun misketleri birbirinin arasina giriyor mu?
            hepsi.Sort((a, b) => a.x.CompareTo(b.x));
            bool gecmeli = false;
            for (int i = 0; i + 1 < hepsi.Count; i++)
            {
                bool ilkSol = d0.Contains(hepsi[i]);
                bool ikiSol = d0.Contains(hepsi[i + 1]);
                if (ilkSol != ikiSol) gecmeli = true;
            }
            Check(gecmeli, "Iki oyuncunun misketleri gecmeli diziliyor");

            // Kural motoru degismiyor: dizi de ayni kese/el duzenini kullanir.
            var dm = new DuelMatch(0);
            dm.Place(0, ToTuple(d0)); dm.Place(1, ToTuple(d1));
            Check(dm.State == DuelMatch.Phase.Shooting, "Dizide de iki dizilis sonrasi atisa gecilir");
            Check(dm.RemainingInRing == DuelMatch.AntePerRound * 2, "Sirada sekiz misket var");

            DuelSession.Type = eski;
        }
        Check(DuelSession.Type == DuelSession.GameType.Cember, "Dizi testinden sonra tur cembere doner");

        // --- KUYU (CUKUR) MODU ---
        // Bu modun kural motoru AYRI (WellMatch): kese yok, sayi var.
        {
            var eskiKuyuTur = DuelSession.Type;
            DuelSession.Type = DuelSession.GameType.Kuyu;
            Check(DuelSession.Well, "Kuyu modu taniniyor");
            Check(DuelSession.Shape == ArenaShape.Circle, "Kuyu sahasi cember");
            Check(DuelSession.HoleRadius < DuelSession.WellSize * .3f, "Cukur sahaya gore kucuk");

            // Dizme: misketler cukurun etrafina, cukurun AGZINA degil.
            float a = DuelSession.WellSize;
            Check(!DuelPlacement.Inside(0f, 0f, a), "Cukurun tam ortasina dizilemez");
            Check(!DuelPlacement.Inside(DuelSession.HoleRadius * .5f, 0f, a), "Cukurun agzina dizilemez");
            Check(DuelPlacement.Inside(0f, DuelSession.HoleRadius + .8f, a), "Cukurun disi gecerli");
            Check(!DuelPlacement.Inside(0f, a + 1f, a), "Sahanin disi reddedilir");
            var kirpik = DuelPlacement.Clamp(0f, 0f, a);
            Check(DuelPlacement.Inside(kirpik.x, kirpik.y, a), "Tam merkez disari itilir");

            var w = new WellMatch(0);
            Check(w.Turn == 0 && w.State == WellMatch.Phase.Shooting, "Kuyu maci atisla baslar");
            Check(w.Score(0) == 0 && w.Score(1) == 0, "Sayilar sifirdan baslar");
            Check(!w.Cooked(0) && !w.Cooked(1), "Kimse basta pismis degil");
            Check(w.OnLine(0) && w.OnLine(1), "Iki misket de cizgide baslar");
            Check(w.Winner == -1, "Mac bitmeden kazanan yok");

            // Pismemisken rakibe vurmak SAYI DEGIL.
            Check(!w.Resolve(WellMatch.ShotResult.Hit, 1f, 1f, 2f, 0f), "Pismemis vurus tekrar atis vermez");
            Check(w.Score(0) == 0, "Pismemisken vurmak sayi kazandirmaz");
            Check(w.Turn == 1, "Sayi alamayinca sira gecer");
            Check(!w.OnLine(0), "Atan misket sahada kalir");

            // Cukura girmek: sayi + pisme + tekrar atis.
            Check(w.Resolve(WellMatch.ShotResult.InHole, 0f, .8f, 1f, 1f), "Cukura giren tekrar atar");
            Check(w.Score(1) == 1, "Cukura girmek sayi kazandirir");
            Check(w.Cooked(1), "Cukura giren pisiyor");
            Check(w.Turn == 1, "Tekrar atista sira degismiyor");

            // Pismisken vurmak sayi.
            Check(w.Resolve(WellMatch.ShotResult.Hit, .5f, .5f, 1.4f, .2f), "Pismis vurus tekrar atis verir");
            Check(w.Score(1) == 2, "Pismisken vurmak sayi kazandirir");

            // Ust uste atis siniri.
            Check(!w.Resolve(WellMatch.ShotResult.InHole, 0f, .8f, 1.4f, .2f), "Ucuncu atistan sonra sira gecer");
            Check(w.Score(1) == 3, "Ucuncu atisin sayisi da sayilir");
            Check(w.Turn == 0, "Hak bitince sira rakibe gecer");

            // Iska: sira gecer, sayi yok.
            int onceki = w.Score(0);
            Check(!w.Resolve(WellMatch.ShotResult.Miss, 2f, 2f, 0f, .8f), "Iska tekrar atis vermez");
            Check(w.Score(0) == onceki, "Iska sayi kazandirmaz");

            // Saha disi: misket cizgiye doner, sayi yok.
            w.Resolve(WellMatch.ShotResult.Miss, 1f, 1f, 0f, .8f);     // sira 0'a gelsin
            while (w.Turn != 0) w.Resolve(WellMatch.ShotResult.Miss, 1f, 1f, 0f, .8f);
            onceki = w.Score(0);
            Check(!w.Resolve(WellMatch.ShotResult.OutOfField, 9f, 9f, 0f, .8f), "Saha disi tekrar atis vermez");
            Check(w.Score(0) == onceki, "Saha disi sayi kazandirmaz");
            Check(w.OnLine(0), "Sahadan cikan misket cizgiye doner");

            // 12 sayiya ulasan kazanir.
            var yaris = new WellMatch(0);
            int guvenlik = 0;
            while (yaris.State == WellMatch.Phase.Shooting && guvenlik++ < 200)
                yaris.Resolve(WellMatch.ShotResult.InHole, 0f, .8f, 1f, 1f);
            Check(yaris.State == WellMatch.Phase.Finished, "Mac bitiyor");
            Check(yaris.Score(yaris.Winner) >= WellMatch.TargetScore, "Kazanan hedefe ulasmis");
            Check(WellMatch.TargetScore == 12, "Hedef geleneksel kuraldaki gibi 12");
            Check(yaris.Winner == 0 || yaris.Winner == 1, "Kazanan belli");

            // Cukurdan cikis noktasi cukurun DISINDA olmali, yoksa misket
            // bir sonraki atista kendiliginden yine iceride sayilirdi.
            Check(WellMatch.HoleExitDistance(DuelSession.HoleRadius) > DuelSession.HoleRadius,
                  "Cukurdan cikan misket agzin disina konur");

            // Rovans: ilk atan degisir, sayilar sifirlanir.
            var rov = yaris.Rematch();
            Check(rov.Starter == 1 - yaris.Starter, "Rovansta ilk atan degisir");
            Check(rov.Score(0) == 0 && rov.Score(1) == 0, "Rovansta sayilar sifirlanir");
            Check(!rov.Cooked(0) && !rov.Cooked(1), "Rovansta pisme sifirlanir");

            DuelSession.Type = eskiKuyuTur;
        }
        Check(DuelSession.Type == DuelSession.GameType.Cember, "Kuyu testinden sonra tur cembere doner");

        // Saha sinirlari: dizme payi olmayan sade kontrol.
        Check(DuelPlacement.InsideArena(0f, 0f, 3.2f), "Sahanin ortasi sahada");
        Check(DuelPlacement.InsideArena(0f, 3.1f, 3.2f), "Cizgiye yapisik nokta hala sahada");
        Check(!DuelPlacement.InsideArena(0f, 3.3f, 3.2f), "Cizgiyi gecen nokta sahada degil");

        // --- Ucgen sahada dizme ---
        var eskiTur = DuelSession.Type;
        try
        {
            DuelSession.Type = DuelSession.GameType.Ucgen;
            float u = DuelSession.TriangleSize;
            // Olculdu: ayni guc ucgende atis basina 1.68 misket cikariyor,
            // cemberde 1.03. Ucgenin gucu DUSUK olmali, yoksa saha bedava.
            Check(DuelSession.Impulse < 1f, "Ucgende atis gucu cemberden dusuk");
            Check(DuelSession.ArenaSize > DuelSession.CircleSize, "Ucgen saha cemberden buyuk");
            // Ucgenin koseleri 270/30/150: uzak taraf z = .5r yatay kenari.
            Check(Mathf.Approximately(DuelToss.Line, u * .5f), "Ucgende cizgi uzak yatay kenar");
            Check(DuelToss.Line < DuelSession.CircleSize, "Ucgende cizgi cemberdekinden yakin");
            Check(DuelPlacement.Inside(0f, u * .15f, u), "Ucgenin ortasi gecerli");
            Check(!DuelPlacement.Inside(0f, u * 1.2f, u), "Ucgenin disi reddedilir");
            // Cemberin icinde ama ucgenin disinda kalan bir kose noktasi.
            Check(!DuelPlacement.Inside(u * .8f, -u * .5f, u), "Ucgenin kestigi kose reddedilir");
            var kirpilmis = DuelPlacement.Clamp(u * 2f, -u * 2f, u);
            Check(DuelPlacement.Inside(kirpilmis.x, kirpilmis.y, u), "Ucgende tasan nokta ice cekilir");

            for (int pl = 0; pl < 2; pl++)
            {
                var d = DuelPlacement.DefaultLayout(pl, DuelMatch.AntePerRound, u);
                Check(d.Count == DuelMatch.AntePerRound, "Ucgende hazir dizilis tam sayida");
                foreach (var q in d) Check(DuelPlacement.Inside(q.x, q.y, u), "Ucgende hazir dizilis sahanin icinde");
                for (int i = 0; i < d.Count; i++)
                    for (int j = i + 1; j < d.Count; j++)
                        Check(Vector2.Distance(d[i], d[j]) > DuelPlacement.MinGap - .02f, "Ucgende cakisma yok");
            }
        }
        finally { DuelSession.Type = eskiTur; }

        Check(DuelSession.Type == DuelSession.GameType.Cember, "Test sonrasi tur cembere doner");
        Check(Mathf.Approximately(DuelSession.Impulse, 1f), "Cemberde atis gucu tam");

        // --- Dizme kurallari (cember) ---
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
            var d = DuelPlacement.DefaultLayout(pl, DuelMatch.AntePerRound, arena);
            Check(d.Count == DuelMatch.AntePerRound, "Hazir dizilis tam sayida");
            foreach (var q in d) Check(DuelPlacement.Inside(q.x, q.y, arena), "Hazir dizilis cemberin icinde");
            for (int i = 0; i < d.Count; i++)
                for (int j = i + 1; j < d.Count; j++)
                    Check(Vector2.Distance(d[i], d[j]) > DuelPlacement.MinGap - .02f, "Hazir diziliste cakisma yok");
        }

        return passed;
    }
}
