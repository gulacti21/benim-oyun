using System;
using System.Collections.Generic;

// MISKETR ONLINE — mac kural motoru.
//
// Saf C#: Unity'ye, sahneye, fizige hic bakmaz. Kurallarin dogrulugu
// sahne kurmadan, telefon baglamadan, ag yazmadan test edilebilsin diye.
//
// KURALLAR
//   Kese     : herkes 20 misketle baslar. Kaybeden, kesesi eriyen olur.
//   El       : her el ikisi de keseden 5'er misket cikarip cembere dizer.
//              Ortadaki misketler artik kimsenin degil; kim cikarirsa onun.
//   Dizme    : sadece ILK EL elle dizilir. Sonraki eller otomatik dizilir.
//              Herkesin ayrica 1 EKSTRA DIZME HAKKI vardir; istedigi elin
//              basinda kullanip o eli kendi eliyle dizebilir.
//   Atis     : sirayla. ILK DIZEN IKINCI ATAR.
//              Misket cikardiysan ayni turda tekrar atarsin, en fazla 3.
//   ATICI    : atisin sonunda atici misketin cemberin ICINDE durduysa onu
//              KAYBEDERSIN. Keseden duser ve ortada bir hedef olur; rakip
//              onu cikarip kendi kesesine katabilir.
//              Yani her atista "ne kadar sert vurayim" hesabi vardir:
//              sert vurursan atici cemberi gecip disari cikar, canin yanmaz.
//   El biter : cemberde misket kalmayinca. Dort tur ust uste kimse misket
//              cikaramazsa el tikanmis sayilir, kalan misketler sonraki
//              ele devreder.
//   Mac      : 5 el. Sonunda kesesi kalabalik olan kazanir. Bir oyuncunun
//              kesesi el basinda bosalirsa mac orada biter.
public class DuelMatch
{
    // 4000 mac simule edilerek secildi.
    //   Kese 20 / ortaya 5 iken: el basina 16 atis (macta ~80), kese HIC
    //   bosalmiyordu -- mac hem cok uzun hem gerilimsizdi.
    //   Kese 12 / ortaya 4: bes elde 20 misket ortaya koymak gerekiyor ama
    //   elinde 12 var, yani kazanmadan maci bitiremiyorsun.
    public const int StartingPouch = 12;
    public const int AntePerRound = 4;
    public const int RoundsPerMatch = 5;
    public const int MaxShotsPerTurn = 3;
    public const int StaleTurnLimit = 4;
    // Ilk el disinda elle dizme hakki: herkese bir tane.
    public const int ExtraPlacements = 1;

    public enum Phase { Placing, Shooting, RoundOver, Finished }
    public enum Outcome { None, PlayerOne, PlayerTwo, Draw }

    public struct Marble
    {
        public int placedBy;   // sadece renk icin: bu misketi kim ortaya koydu
        public bool out_;
        public float x, z;
        public bool stranded;  // cemberde kalmis bir atici mi
    }

    private readonly List<Marble> marbles = new List<Marble>();
    private readonly bool[] placed = new bool[2];
    private readonly int[] pouch = { StartingPouch, StartingPouch };
    private readonly int[] anted = new int[2];
    private readonly int[] rights = { ExtraPlacements, ExtraPlacements };
    private readonly bool[] usingRight = new bool[2];

    public Phase State { get; private set; } = Phase.Placing;
    public int FirstPlacer { get; private set; }
    public int Turn { get; private set; }
    public int ShotsThisTurn { get; private set; }
    public int Round { get; private set; } = 1;
    public int ScorelessTurns { get; private set; }
    public int MatchNumber { get; private set; }

    public IReadOnlyList<Marble> Marbles => marbles;
    public int Pouch(int player) => pouch[player & 1];
    public int Anted(int player) => anted[player & 1];
    public bool HasPlaced(int player) => placed[player & 1];
    public int RemainingInRing
    {
        get { int n = 0; for (int i = 0; i < marbles.Count; i++) if (!marbles[i].out_) n++; return n; }
    }
    // Bu el ortaya konacak misket sayisi: kese yetmiyorsa kalan kadar.
    public int AnteFor(int player) => Math.Min(AntePerRound, pouch[player & 1]);
    public int PlacementRights(int player) => rights[player & 1];
    // Bu el elle mi diziliyor? Ilk el herkes icin elle; sonrakiler ancak
    // oyuncu ekstra hakkini kullanirsa.
    public bool PlacesByHand(int player) => Round == 1 || usingRight[player & 1];

    // Elin basinda "hakkimi kullanacagim" demek.
    public bool UsePlacementRight(int player)
    {
        player &= 1;
        if (State != Phase.Placing || Round == 1 || placed[player]) return false;
        if (rights[player] <= 0 || usingRight[player]) return false;
        rights[player]--; usingRight[player] = true;
        return true;
    }

    public DuelMatch(int firstPlacer = 0)
    {
        FirstPlacer = firstPlacer & 1;
        Turn = 1 - FirstPlacer;      // ilk dizen ikinci atar
    }

    // ---------------- El hazirligi ----------------

    // Oyuncu kesesinden misketleri cikarip cembere dizer.
    public bool Place(int player, IList<(float x, float z)> spots)
    {
        player &= 1;
        if (State != Phase.Placing || placed[player]) return false;
        int gereken = AnteFor(player);
        if (spots == null || spots.Count != gereken) return false;

        foreach (var s in spots)
            marbles.Add(new Marble { placedBy = player, out_ = false, x = s.x, z = s.z });

        pouch[player] -= gereken;
        anted[player] = gereken;
        placed[player] = true;
        if (placed[0] && placed[1]) State = Phase.Shooting;
        return true;
    }

    // ---------------- Atis ----------------

    // knocked          : atisi yapanin kazandigi misketlerin indeksleri
    //                    (cemberde disari cikanlar, dizide kipirdayanlar,
    //                     kuyuda cukura dusenler -- kural moduna gore degisir,
    //                     motor sadece "bunlar kazanildi" bilgisini alir)
    // shooterStranded  : atici misketini KAYBETTI mi
    // sx, sz           : aticinin durdugu yer
    // strandedBecomesTarget : kaybedilen atici ortada yeni bir hedef olur mu.
    //                    Cember/ucgende olur (cemberin ortasinda oylece durur).
    //                    KUYUDA OLMAZ: atici cukurun dibindedir, kimse ona
    //                    vuramaz. Ikisinde de keseden duser.
    // Donen deger      : sira ayni oyuncuda mi kaldi
    public bool ResolveShot(IList<int> knocked, bool shooterStranded, float sx = 0f, float sz = 0f,
                            bool strandedBecomesTarget = true)
    {
        if (State != Phase.Shooting) return false;

        int shooter = Turn, kazanc = 0;

        if (knocked != null)
        {
            foreach (int i in knocked)
            {
                if (i < 0 || i >= marbles.Count) continue;
                var m = marbles[i];
                if (m.out_) continue;
                m.out_ = true; marbles[i] = m;
                kazanc++;
            }
        }
        pouch[shooter] += kazanc;

        // Atici cemberde kaldi: keseden duser, ortada hedef olur.
        if (shooterStranded)
        {
            pouch[shooter] -= 1;
            if (strandedBecomesTarget)
                marbles.Add(new Marble { placedBy = shooter, out_ = false, x = sx, z = sz, stranded = true });
        }

        ShotsThisTurn++;
        ScorelessTurns = kazanc > 0 ? 0 : ScorelessTurns;

        if (RemainingInRing == 0) { EndRound(); return false; }

        // Cikardiysan, aticin da cemberde kalmadiysa ve hakkin varsa devam.
        if (kazanc > 0 && !shooterStranded && ShotsThisTurn < MaxShotsPerTurn) return true;

        EndTurn(kazanc > 0);
        return false;
    }

    public void ForfeitTurn()
    {
        if (State != Phase.Shooting) return;
        EndTurn(false);
    }

    private void EndTurn(bool kazandi)
    {
        ShotsThisTurn = 0;
        ScorelessTurns = kazandi ? 0 : ScorelessTurns + 1;
        Turn = 1 - Turn;

        // El tikandi: kimse misket cikaramiyor. Kalan misketler ortada kalir
        // ve sonraki ele devreder.
        if (ScorelessTurns >= StaleTurnLimit) EndRound();
    }

    private void EndRound()
    {
        ShotsThisTurn = 0; ScorelessTurns = 0;
        if (Round >= RoundsPerMatch) { State = Phase.Finished; return; }
        State = Phase.RoundOver;
    }

    // Sonraki ele gecis. Cemberde kalan misketler ortada kalir.
    public void NextRound()
    {
        if (State != Phase.RoundOver) return;

        // Cikmis misketleri listeden at; kalanlar ortada durmaya devam eder.
        for (int i = marbles.Count - 1; i >= 0; i--) if (marbles[i].out_) marbles.RemoveAt(i);

        Round++;
        placed[0] = placed[1] = false;
        usingRight[0] = usingRight[1] = false;
        anted[0] = anted[1] = 0;
        FirstPlacer = 1 - FirstPlacer;     // her el ilk dizen degisir
        Turn = 1 - FirstPlacer;

        // Kesesi bos olan ortaya koyamaz: mac biter.
        if (pouch[0] <= 0 || pouch[1] <= 0) { State = Phase.Finished; return; }
        State = Phase.Placing;
    }

    // ---------------- Sonuc ----------------

    public Outcome Result
    {
        get
        {
            if (State != Phase.Finished) return Outcome.None;
            if (pouch[0] > pouch[1]) return Outcome.PlayerOne;
            if (pouch[1] > pouch[0]) return Outcome.PlayerTwo;
            return Outcome.Draw;
        }
    }

    public DuelMatch Rematch()
    {
        var next = new DuelMatch(1 - FirstPlacer);
        next.MatchNumber = MatchNumber + 1;
        return next;
    }
}
