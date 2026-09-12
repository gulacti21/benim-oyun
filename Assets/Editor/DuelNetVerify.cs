using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// ONLINE AG KATMANI DOGRULAMASI.
//
// Buradaki soru sunlar:
//   1) Paketler kayipsiz mi? (kodla-coz-karsilastir)
//   2) Mesaj kumesi YETERLI mi? Yani iki cihaz ayni mesaj dizisiyle AYNI
//      duruma variyor mu -- bu online oyunun tek gercek sartidir.
//   3) Bozuk/kotu niyetli paket oyunu bozabiliyor mu? (baskasi adina hamle,
//      sirasi gelmeden atis, cop bayt)
//
// Bulut hesabi gerekmez: LoopbackTransport iki ucu tek surecte baglar.
// Gercek tasiyici geldiginde bu testlerin hicbiri degismez.
public static class DuelNetVerify
{
    private static int passed, failed;

    private static void Check(bool ok, string message)
    {
        if (ok) { passed++; return; }
        failed++;
        Debug.LogError("DUEL_NET: " + message);
        throw new System.Exception("CHECK FAILED: " + message);
    }

    [MenuItem("MISKETR/Verify Duel Network")]
    public static void Run()
    {
        passed = failed = 0;
        int n = RunChecks();
        Debug.Log(failed == 0 ? "DUEL_NET_OK: " + n + " checks passed." : "DUEL_NET_FAIL: " + failed + " / " + n);
    }

    public static int RunChecks()
    {
        int basla = passed;
        Paketler();
        Bozuk();
        TamMac(false);
        TamMac(true);
        Kotucu();
        return passed - basla;
    }

    // ---------- 1) Paket kodlama ----------
    private static void Paketler()
    {
        var yer = new List<(float, float)> { (-1.25f, .5f), (0f, 1.75f), (.9f, -.4f), (1.1f, 2f) };
        var place = DuelNet.Place(1, yer);
        Check(DuelNet.Decode(DuelNet.Encode(place), out var c1), "Dizilis paketi cozulur");
        var geri = DuelNet.ReadSpots(c1);
        Check(geri.Count == yer.Count, "Dizilis misket sayisi korunur");
        bool ayni = true;
        for (int i = 0; i < yer.Count; i++)
            if (!Mathf.Approximately(geri[i].x, yer[i].Item1) || !Mathf.Approximately(geri[i].z, yer[i].Item2))
                ayni = false;
        Check(ayni, "Dizilis koordinatlari bozulmadan gider");
        Check(c1.player == 1, "Paketin sahibi korunur");

        var shot = DuelNet.Shot(0, new List<int> { 3, 7, 11 }, true, -1.4f, 2.2f);
        Check(DuelNet.Decode(DuelNet.Encode(shot), out var c2), "Atis paketi cozulur");
        Check(c2.knocked.Length == 3 && c2.knocked[2] == 11, "Cikan misket indeksleri korunur");
        Check(c2.flag, "Aticinin sahada kalma bilgisi korunur");
        Check(Mathf.Approximately(c2.x, -1.4f) && Mathf.Approximately(c2.z, 2.2f), "Atici konumu korunur");

        var toss = DuelNet.Toss(1, .3f, 2.9f, false);
        Check(DuelNet.Decode(DuelNet.Encode(toss), out var c3), "Sira atisi paketi cozulur");
        Check(c3.number == 0, "Dusme bayragi korunur");

        var hello = DuelNet.Hello(0, 1);
        Check(DuelNet.Decode(DuelNet.Encode(hello), out var c4), "El sikisma paketi cozulur");
        Check(c4.number == DuelNet.Protocol, "Protokol surumu gider");

        // Boyut: sira tabanli oyun icin fazlasiyla kucuk olmali.
        Check(DuelNet.Encode(shot).Length < 64, "Atis paketi 64 bayttan kucuk");
    }

    // ---------- 2) Bozuk veri ----------
    private static void Bozuk()
    {
        Check(!DuelNet.Decode(null, out _), "Bos veri reddedilir");
        Check(!DuelNet.Decode(new byte[0], out _), "Sifir uzunluk reddedilir");
        Check(!DuelNet.Decode(new byte[] { 1, 2, 3 }, out _), "Kisa paket reddedilir");

        var iyi = DuelNet.Encode(DuelNet.Shot(0, new List<int> { 1 }, false, 0f, 0f));
        var tur = (byte[])iyi.Clone(); tur[0] = 99;
        Check(!DuelNet.Decode(tur, out _), "Taninmayan mesaj turu reddedilir");

        var kesik = new byte[iyi.Length - 3];
        System.Buffer.BlockCopy(iyi, 0, kesik, 0, kesik.Length);
        Check(!DuelNet.Decode(kesik, out _), "Yarim paket reddedilir");
    }

    // ---------- 3) Tam mac: iki taraf ayni duruma variyor mu ----------
    // gecikmeli: paketler aninda degil kuyrukta teslim edilir.
    private static void TamMac(bool gecikmeli)
    {
        string etiket = gecikmeli ? " (gecikmeli ag)" : "";
        var (ht, gt) = LoopbackTransport.Pair();
        ht.Buffered = gecikmeli; gt.Buffered = gecikmeli;

        var host = new DuelNetSession(ht, 0);
        var guest = new DuelNetSession(gt, 0);
        void Ak() { if (gecikmeli) { ht.Flush(); gt.Flush(); } }
        host.Begin(); guest.Begin(); Ak(); Ak();

        Check(host.HandshakeDone && guest.HandshakeDone, "El sikisma iki tarafta tamam" + etiket);
        Check(host.LocalPlayer == 0 && guest.LocalPlayer == 1, "Oyuncu numaralari ayri" + etiket);

        // --- Sira belirleme atisi ---
        host.LocalToss(0f, DuelToss.Line - .4f, false); Ak();
        guest.LocalToss(0f, DuelToss.Line - 1.2f, false); Ak();
        Check(host.Toss.Finished && guest.Toss.Finished, "Sira atisi iki tarafta bitti" + etiket);
        Check(host.Toss.WinnerOf == guest.Toss.WinnerOf, "Sira atisinin kazanani iki tarafta ayni" + etiket);
        Check(host.Toss.WinnerOf == 0, "Cizgiye yakin olan kazanir" + etiket);
        Check(host.Match != null && guest.Match != null, "Atis bitince mac kuruldu" + etiket);
        Check(host.Match.FirstPlacer == guest.Match.FirstPlacer, "Ilk dizen iki tarafta ayni" + etiket);

        // --- Bes el oyna ---
        var rnd = new System.Random(4242);
        int guvenlik = 0;
        while (host.Match.State != DuelMatch.Phase.Finished && guvenlik++ < 400)
        {
            var m = host.Match;

            if (m.State == DuelMatch.Phase.Placing)
            {
                // Sirasi gelen taraf dizer; her iki oturum da kendi hamlesini uretir.
                if (!m.HasPlaced(host.LocalPlayer)) { host.LocalPlace(Dizilis(m.AnteFor(0), -1f)); Ak(); }
                else if (!m.HasPlaced(guest.LocalPlayer)) { guest.LocalPlace(Dizilis(m.AnteFor(1), 1f)); Ak(); }
                continue;
            }

            if (m.State == DuelMatch.Phase.Shooting)
            {
                // ATAN taraf fizigi kendinde cozer, sonucu yollar.
                var atan = m.Turn == host.LocalPlayer ? host : guest;
                int ortada = m.RemainingInRing;
                int cikan = ortada == 0 ? 0 : rnd.Next(0, Mathf.Min(3, ortada) + 1);
                var liste = Ortadakiler(m, cikan);
                bool kaldi = rnd.NextDouble() < .2;
                atan.LocalShot(liste, kaldi, .2f, .3f);
                Ak();
                continue;
            }

            if (m.State == DuelMatch.Phase.RoundOver)
            {
                // Iki taraf da basar; ikincisi tekrar sayilip yutulmali.
                host.LocalNextRound(); Ak();
                guest.LocalNextRound(); Ak();
                continue;
            }
        }

        Check(guvenlik < 400, "Mac sonsuz donmuyor" + etiket);
        Check(host.Match.State == DuelMatch.Phase.Finished, "Mac bitti" + etiket);
        Check(guest.Match.State == DuelMatch.Phase.Finished, "Mac karsi tarafta da bitti" + etiket);

        // ASIL SORU: iki taraf ayni durumda mi?
        Check(host.Match.Pouch(0) == guest.Match.Pouch(0)
           && host.Match.Pouch(1) == guest.Match.Pouch(1), "Iki tarafin keseleri ayni" + etiket);
        Check(host.Match.Result == guest.Match.Result, "Iki taraf ayni kazanani gosteriyor" + etiket);
        Check(host.Match.Round == guest.Match.Round, "El sayisi ayni" + etiket);
        Check(host.Match.Marbles.Count == guest.Match.Marbles.Count, "Misket listesi ayni uzunlukta" + etiket);
        bool misketAyni = true;
        for (int i = 0; i < host.Match.Marbles.Count; i++)
        {
            var a = host.Match.Marbles[i]; var b = guest.Match.Marbles[i];
            if (a.out_ != b.out_ || a.placedBy != b.placedBy || a.stranded != b.stranded) misketAyni = false;
        }
        Check(misketAyni, "Misketlerin durumu birebir ayni" + etiket);
        Check(host.Rejected == 0 && guest.Rejected == 0,
              "Normal oyunda hicbir paket reddedilmedi" + etiket
              + " (host " + host.Rejected + ", guest " + guest.Rejected + ")");
        Check(host.Match.Pouch(0) + host.Match.Pouch(1) == DuelMatch.StartingPouch * 2,
              "Misketler kaybolmadi" + etiket);

        // --- Rovans ---
        host.LocalRematch(); Ak();
        guest.LocalRematch(); Ak();
        Check(host.Match.Round == 1 && guest.Match.Round == 1, "Rovans ilk elden basladi" + etiket);
        Check(host.Match.FirstPlacer == guest.Match.FirstPlacer, "Rovansta ilk dizen ayni" + etiket);
        Check(host.Match.Pouch(0) == DuelMatch.StartingPouch, "Rovansta kese sifirlandi" + etiket);

        // --- Cikis ---
        guest.LocalLeave();
        Check(true, "Cikis paketi hata vermedi" + etiket);
        host.Dispose();
    }

    // ---------- 4) Kotu niyetli / hatali paketler ----------
    private static void Kotucu()
    {
        var (ht, gt) = LoopbackTransport.Pair();
        var host = new DuelNetSession(ht, 0);
        var guest = new DuelNetSession(gt, 0);
        host.Begin(); guest.Begin();

        // Karsi taraf BIZIM adimiza hamle uretemez.
        int once = host.Rejected;
        gt.Send(DuelNet.Encode(DuelNet.Toss(0, 0f, 1f, false)));   // guest, host adina
        Check(host.Rejected == once + 1, "Baskasi adina hamle reddedilir");
        Check(!host.Toss.Threw(0), "Reddedilen hamle duruma islemez");

        // Sira atisini bitir, maci kur.
        host.LocalToss(0f, DuelToss.Line - .3f, false);
        guest.LocalToss(0f, DuelToss.Line - 1f, false);
        Check(host.Match != null, "Mac kuruldu");

        // Dizme asamasindayken atis paketi gelemez.
        once = host.Rejected;
        gt.Send(DuelNet.Encode(DuelNet.Shot(1, new List<int> { 0 }, false, 0f, 0f)));
        Check(host.Rejected == once + 1, "Dizme asamasinda atis reddedilir");

        // Dizilisi tamamla.
        host.LocalPlace(Dizilis(host.Match.AnteFor(0), -1f));
        guest.LocalPlace(Dizilis(host.Match.AnteFor(1), 1f));
        Check(host.Match.State == DuelMatch.Phase.Shooting, "Iki dizilis sonrasi atisa gecildi");

        // Sirasi gelmeyen oyuncunun atisi reddedilir.
        int sirasiOlmayan = 1 - host.Match.Turn;
        if (sirasiOlmayan == guest.LocalPlayer)
        {
            once = host.Rejected;
            gt.Send(DuelNet.Encode(DuelNet.Shot(guest.LocalPlayer, new List<int> { 0 }, false, 0f, 0f)));
            // Sira guest'te degilse reddedilmeli.
            Check(host.Rejected == once + 1, "Sirasi gelmeyenin atisi reddedilir");
        }

        // Cop bayt oyunu bozmaz.
        once = host.Rejected;
        gt.Send(new byte[] { 250, 251, 252, 253, 254 });
        Check(host.Rejected == once + 1, "Cop veri reddedilir");
        Check(host.Match.State == DuelMatch.Phase.Shooting, "Cop veri durumu bozmadi");

        // Yanlis protokol surumu el sikismayi tamamlamaz.
        var (at, bt) = LoopbackTransport.Pair();
        var a = new DuelNetSession(at, 0);
        a.Begin();
        var yanlis = DuelNet.Hello(1, 0); yanlis.number = DuelNet.Protocol + 7;
        bt.Send(DuelNet.Encode(yanlis));
        Check(!a.HandshakeDone, "Farkli surumle el sikisilmaz");

        // Rakip cikisi goruluyor.
        bt.Send(DuelNet.Encode(DuelNet.Simple(DuelNet.Kind.Leave, 1)));
        Check(a.PeerLeft, "Rakibin cikisi goruluyor");

        host.Dispose(); guest.Dispose(); a.Dispose();
    }

    // ---------- Yardimcilar ----------

    private static List<(float x, float z)> Dizilis(int adet, float yon)
    {
        var list = new List<(float, float)>();
        for (int i = 0; i < adet; i++) list.Add((yon * (.6f + i * .6f), .5f + i * .3f));
        return list;
    }

    // Ortada duran misketlerden ilk n tanesinin indeksi.
    private static List<int> Ortadakiler(DuelMatch m, int n)
    {
        var list = new List<int>();
        for (int i = 0; i < m.Marbles.Count && list.Count < n; i++)
            if (!m.Marbles[i].out_) list.Add(i);
        return list;
    }
}
