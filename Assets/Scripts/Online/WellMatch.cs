using UnityEngine;

// KUYU (CUKUR) OYUNUNUN KURAL MOTORU.
//
// Bu mod diger uclunun (cember, ucgen, dizi) disinda: orada kese var, her el
// ortaya misket konur, bes el sonunda kesesi kalabalik olan kazanir. Kuyuda
// ne kese var ne misket alisverisi -- TUR var.
//
// BIR TURU KAZANMAK ICIN IKI SEY, SIRAYLA:
//   1) Cukura gir  -> PISTIN
//   2) Pistikten sonra rakibin misketini SAHADAN CIKAR -> turu aldin
//
// Sira onemli. Pismeden rakibi disari atarsan tur bitmez; rakibin misketi
// atis cizgisine doner, o kadar. Yani cukur, kazanmanin kapi bekcisi.
//
// Ayni atista hem cukura girip hem rakibi cikarmak turu KAZANDIRMAZ: vurus
// aninda henuz pismemistin. Pismislik atistan ONCEKI duruma gore bakiliyor.
//
// UC TUR oynanir, IKI turu alan maci kazanir (2-0 ya da 2-1).
// Her tur sifirdan baslar: pismislik sifirlanir, iki misket de cizgiye doner.
//
// Neden bu kural iyi: cukur guc kontrolu becerisi, rakibi disari atmak nisan
// ve sertlik becerisi -- ikisi de gerekiyor. Ustelik pistikten sonra misketin
// sahanin tam ortasinda kaliyor, yani rakibi avlamaya cikarken kendin de
// acik hedef oluyorsun.
public class WellMatch
{
    public const int TotalRounds = 3;
    public const int RoundsToWin = 2;
    // Zincir: kazandiran atistan sonra devam edersin, turda en fazla bu kadar.
    public const int MaxShotsPerTurn = 3;
    // Kimse bir sey yapamazsa tur kilitlenmesin: bu kadar bos turdan sonra
    // tur iptal edilir ve bastan baslar (kimseye sayilmaz).
    public const int StaleTurnLimit = 4;

    public enum Phase { Shooting, RoundOver, Finished }

    // Bir atisin turu nasil etkiledigi. Arayuz bunu okuyup ne olduguna gore
    // mesaj gosteriyor.
    public enum Outcome
    {
        Miss,          // hicbir sey olmadi
        Cooked,        // cukura girdi, pisti
        AlreadyCooked, // cukura tekrar girdi, degisen bir sey yok
        RivalReset,    // rakibi cikardi ama pismemisti: rakip cizgiye dondu
        RoundWon,      // pismisti ve rakibi cikardi: tur onun
        SelfOut,       // kendi misketi sahadan cikti
        Nudge,         // rakibe vurdu ama cikaramadi
    }

    private readonly int[] rounds = { 0, 0 };
    private readonly bool[] cooked = { false, false };
    // Iki misketin sahadaki yeri. Kuralin parcasi: iskaladiginda misketin
    // durdugu yerde kalir ve rakip icin acik hedef olur. Agda iki cihazin
    // ayni yeri gormesi icin motorun da bilmesi gerekiyor.
    private readonly float[] px = { 0f, 0f };
    private readonly float[] pz = { 0f, 0f };
    private readonly bool[] onLine = { true, true };

    public Phase State { get; private set; } = Phase.Shooting;
    public int Turn { get; private set; }
    public int Round { get; private set; } = 1;
    public int ShotsThisTurn { get; private set; }
    public int ScorelessTurns { get; private set; }
    public int Starter { get; private set; }
    public int MatchNumber { get; private set; }
    // Son biten turu kim aldi. -1 = tur iptal edildi (kimse alamadi).
    public int LastRoundWinner { get; private set; } = -1;
    public Outcome LastOutcome { get; private set; } = Outcome.Miss;

    public int RoundsWon(int player) => rounds[player & 1];
    public bool Cooked(int player) => cooked[player & 1];
    public bool OnLine(int player) => onLine[player & 1];
    public float X(int player) => px[player & 1];
    public float Z(int player) => pz[player & 1];

    public int Winner => State != Phase.Finished ? -1
                       : rounds[0] >= RoundsToWin ? 0
                       : rounds[1] >= RoundsToWin ? 1 : -1;

    public WellMatch(int starter = 0)
    {
        Starter = starter & 1;
        Turn = Starter;
    }

    // Bir atisin sonucu.
    //   inHole   : atan oyuncunun misketi cukurda durdu
    //   rivalOut : rakibin misketi sahayi terk etti
    //   selfOut  : atan oyuncunun misketi sahayi terk etti
    //   rivalMoved : rakibin misketi kipirdadi (cikmadan)
    //   mx,mz / ox,oz : iki misketin son yeri
    // Donen deger: sira ayni oyuncuda mi kaldi.
    public bool Resolve(bool inHole, bool rivalOut, bool selfOut, bool rivalMoved,
                        float mx, float mz, float ox, float oz)
    {
        if (State != Phase.Shooting) return false;

        int me = Turn, rakip = 1 - Turn;

        // Konumlar: rakibin misketi de itilmis olabilir.
        if (rivalOut) { onLine[rakip] = true; px[rakip] = 0f; pz[rakip] = 0f; }
        else { onLine[rakip] = false; px[rakip] = ox; pz[rakip] = oz; }

        if (selfOut) { onLine[me] = true; px[me] = 0f; pz[me] = 0f; }
        else { onLine[me] = false; px[me] = mx; pz[me] = mz; }

        // ONCELIK SIRASI. Tek atista birden fazla sey olabilir, karar burada
        // veriliyor ki cagiran tarafta belirsizlik kalmasin.
        //
        // 1) Pismisken rakibi cikardin: TUR SENIN.
        if (cooked[me] && rivalOut)
        {
            LastOutcome = Outcome.RoundWon;
            WinRound(me);
            return false;
        }

        // 2) Cukura girdin: pistin, tekrar atarsin.
        if (inHole)
        {
            bool yeni = !cooked[me];
            cooked[me] = true;
            LastOutcome = yeni ? Outcome.Cooked : Outcome.AlreadyCooked;
            ShotsThisTurn++;
            ScorelessTurns = 0;
            if (ShotsThisTurn < MaxShotsPerTurn) return true;
            EndTurn(true);
            return false;
        }

        // 3) Rakibi cikardin ama pismemistin: rakip cizgiye dondu, sira gecer.
        if (rivalOut)
        {
            LastOutcome = Outcome.RivalReset;
            EndTurn(false);
            return false;
        }

        // 4) Kendin sahadan ciktin. Pismislik DURUYOR -- pisme bir durum,
        //    pozisyon degil. Kaybettigin sey yerin ve siran.
        if (selfOut)
        {
            LastOutcome = Outcome.SelfOut;
            EndTurn(false);
            return false;
        }

        // 5) Rakibe vurdun ama cikaramadin / iska.
        LastOutcome = rivalMoved ? Outcome.Nudge : Outcome.Miss;
        EndTurn(false);
        return false;
    }

    private void EndTurn(bool kazandi)
    {
        ShotsThisTurn = 0;
        ScorelessTurns = kazandi ? 0 : ScorelessTurns + 1;
        Turn = 1 - Turn;

        // Tur kilitlendi: kimse cukuru tutturamiyor, kimse rakibi cikaramiyor.
        // Tur iptal edilir ve bastan baslar; kimseye sayilmaz, yoksa mac
        // sonsuza kadar surerdi.
        if (ScorelessTurns >= StaleTurnLimit) CancelRound();
    }

    private void WinRound(int player)
    {
        rounds[player]++;
        LastRoundWinner = player;
        if (rounds[player] >= RoundsToWin) { State = Phase.Finished; return; }
        State = Phase.RoundOver;
    }

    private void CancelRound()
    {
        LastRoundWinner = -1;
        State = Phase.RoundOver;
    }

    // Sonraki tur (ya da iptal edilen turun tekrari).
    public void NextRound()
    {
        if (State != Phase.RoundOver) return;

        // Iptal edilen tur sayilmaz, numarasi ayni kalir.
        if (LastRoundWinner >= 0) Round++;

        cooked[0] = cooked[1] = false;
        onLine[0] = onLine[1] = true;
        px[0] = px[1] = pz[0] = pz[1] = 0f;
        ShotsThisTurn = 0; ScorelessTurns = 0;
        LastOutcome = Outcome.Miss;

        // Turlar arasinda ilk atan degisir.
        Starter = 1 - Starter;
        Turn = Starter;
        State = Phase.Shooting;
    }

    // Cukura giren misket nereye konur: agzin kenarina. Merkezde birakilsa
    // bir sonraki atista kendiliginden yine "iceride" sayilirdi.
    public static float HoleExitDistance(float holeRadius) => holeRadius + .34f;

    public WellMatch Rematch()
    {
        var next = new WellMatch(1 - Starter);
        next.MatchNumber = MatchNumber + 1;
        return next;
    }

    public string RoundText => L.F("TUR {0} / {1}", Mathf.Min(Round, TotalRounds), TotalRounds);
    public string ScoreText => rounds[0] + " - " + rounds[1];

    public string StateText(int player) => L.T(Cooked(player) ? "PİŞTİ" : "ÇİĞ");

    // Atistan sonra oyuncuya gosterilecek kisa mesaj.
    public string OutcomeText(int shooter)
    {
        switch (LastOutcome)
        {
            case Outcome.Cooked: return L.F("{0} PİŞTİ", DuelSession.PlayerName(shooter));
            case Outcome.AlreadyCooked: return L.T("Çukura tekrar girdi");
            case Outcome.RivalReset: return L.T("Pişmeden çıkardı · rakip çizgiye döndü");
            case Outcome.RoundWon: return L.F("{0} TURU ALDI", DuelSession.PlayerName(shooter));
            case Outcome.SelfOut: return L.T("Sahadan çıktı");
            case Outcome.Nudge: return L.T("Vurdu ama çıkaramadı");
            default: return "";
        }
    }
}
