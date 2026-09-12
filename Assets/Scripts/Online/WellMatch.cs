using System;

// KUYU (CUKUR) OYUNUNUN KURAL MOTORU.
//
// Diger uc mod (cember, ucgen, dizi) ayni ekonomiyi paylasir: kese, her el
// ortaya konan misketler, bes el. KUYU o ailenin disinda -- geleneksel
// kuralinda kese yoktur, SAYI vardir: "12 sayiya ulasan oyunu kazanir".
// O yuzden DuelMatch'i zorlamak yerine kendi motoru var. Ikisi birbirine
// hic dokunmuyor.
//
// Belgelenen kisim: zemine 3-4 cm derinlikte bir cukur kazilir, oyuncular
// misketlerini cukura sokmaya calisir, 12 sayiya ulasan kazanir.
// Geri kalani (pisme, avlanma, ceza) tasarim: kuralin tamami hicbir
// kaynakta yazili degil, sadece iskeleti var.
//
// Tasarimin mantigi: sadece "cukura sok" olsa oyun iki kisinin sirayla
// ayni atisi tekrarlamasi olurdu, rakibin varligi hic onemli olmazdi.
// PISME kurali oyunu ikiye boluyor: once herkes cukuru kovaliyor, cukura
// giren "pismis" oluyor ve artik rakibin misketini de avlayabiliyor.
// Boylece cukura ilk giren one geciyor ama merkeze yakin durdugu icin
// kendisi de acik hedef oluyor.
public class WellMatch
{
    // Belgelenen hedef: 12 sayi.
    public const int TargetScore = 12;
    // Ust uste atis hakki. Diger modlarla ayni: zincir odullendirilsin ama
    // bir oyuncu tek turda maci bitiremesin.
    public const int MaxShotsPerTurn = 3;

    public enum Phase { Shooting, Finished }

    public enum ShotResult
    {
        Miss = 0,        // ne cukur ne rakip: sira gecer
        InHole = 1,      // cukura girdi: +1 ve pisme
        Hit = 2,         // rakibin misketine vurdu: pismisse +1
        OutOfField = 3,  // sahayi terk etti: sayi yok, misket cizgiye doner
    }

    private readonly int[] score = { 0, 0 };
    private readonly bool[] cooked = { false, false };
    // Iki misketin sahadaki yeri. Kurallarin parcasi: sira gecince misket
    // durdugu yerde kalir, o yuzden motorun da bilmesi gerekiyor (ag
    // uzerinden iki cihaz ayni yeri gormeli).
    private readonly float[] px = { 0f, 0f };
    private readonly float[] pz = { 0f, 0f };
    private readonly bool[] onLine = { true, true };

    public Phase State { get; private set; } = Phase.Shooting;
    public int Turn { get; private set; }
    public int ShotsThisTurn { get; private set; }
    public int Starter { get; private set; }
    public int MatchNumber { get; private set; }

    public int Score(int player) => score[player & 1];
    public bool Cooked(int player) => cooked[player & 1];
    public bool OnLine(int player) => onLine[player & 1];
    public float X(int player) => px[player & 1];
    public float Z(int player) => pz[player & 1];

    public int Winner => State != Phase.Finished ? -1
                       : score[0] >= TargetScore ? 0 : 1;

    public WellMatch(int starter = 0)
    {
        Starter = starter & 1;
        Turn = Starter;
    }

    // Bir atisin sonucu.
    //   result : ne oldu
    //   mx, mz : atan oyuncunun misketinin durdugu yer
    //   ox, oz : rakibin misketinin durdugu yer (vurulmus olabilir)
    // Donen deger: sira ayni oyuncuda mi kaldi.
    public bool Resolve(ShotResult result, float mx, float mz, float ox, float oz)
    {
        if (State != Phase.Shooting) return false;

        int me = Turn, rakip = 1 - Turn;

        // Konumlar her atista guncellenir: rakibin misketi de itilmis olabilir.
        px[rakip] = ox; pz[rakip] = oz; onLine[rakip] = false;

        if (result == ShotResult.OutOfField)
        {
            // Sahadan cikan misket atis cizgisine doner. Sayi yok, ceza da
            // yok: negatif sayi tutmak oyuncuyu kizdiriyor, sirayi
            // kaybetmek zaten yeterli ceza.
            onLine[me] = true; px[me] = 0f; pz[me] = 0f;
            EndTurn();
            return false;
        }

        px[me] = mx; pz[me] = mz; onLine[me] = false;

        bool kazandi = false;

        if (result == ShotResult.InHole)
        {
            score[me]++;
            cooked[me] = true;          // cukura giren artik avlanabilir
            kazandi = true;
        }
        else if (result == ShotResult.Hit)
        {
            // PISMEMIS oyuncunun vurusu sayilmaz. Kural boyle olmasa oyun
            // "cukuru bosver, rakibi kovala"ya donerdi ve cukurun anlami
            // kalmazdi.
            if (cooked[me]) { score[me]++; kazandi = true; }
        }

        if (score[me] >= TargetScore) { State = Phase.Finished; return false; }

        ShotsThisTurn++;
        if (kazandi && ShotsThisTurn < MaxShotsPerTurn) return true;

        EndTurn();
        return false;
    }

    private void EndTurn()
    {
        ShotsThisTurn = 0;
        Turn = 1 - Turn;
    }

    // Cukura giren misket nereye konur: agzin kenarina. Merkezde birakmak
    // onu bir sonraki atista kendiliginden tekrar "iceride" yapardi.
    public static float HoleExitDistance(float holeRadius) => holeRadius + .34f;

    public WellMatch Rematch()
    {
        // Rovansta ilk atan degisir; diger modlarla ayni kural.
        var next = new WellMatch(1 - Starter);
        next.MatchNumber = MatchNumber + 1;
        return next;
    }

    public string ScoreText(int player) => Score(player) + " / " + TargetScore;
}
