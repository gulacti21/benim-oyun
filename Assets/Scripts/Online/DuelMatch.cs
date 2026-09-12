using System;
using System.Collections.Generic;

// MISKETR ONLINE — maç kural motoru.
//
// Saf C#: Unity'ye, sahneye, fiziğe hiç bakmaz. Kuralların doğruluğu
// sahne kurmadan, telefon bağlamadan, ağ yazmadan test edilebilsin diye.
//
// KURALLAR
//   Hazırlık : iki oyuncu da 7'şer misket dizer, aynı anda ve birbirini
//              görmeden. Ortaya ayrıca kimsenin olmayan bir BÜYÜK misket konur.
//   Atış     : sırayla. İLK DİZEN İKİNCİ ATAR.
//              Puan aldığın atıştan sonra aynı turda tekrar atarsın,
//              turda en fazla 3 atış. Atıcı kaldığı yerden devam eder.
//   Puan     : çıkardığın misket sana yazılır.
//              Kendi misketini çıkarırsan puan RAKİBE yazılır.
//              Ortadaki büyük misket 2 puandır, kim çıkarırsa onun.
//   Bitiş    : çemberde misket kalmayınca. Toplam 16 puan paylaşılır.
//              Çok toplayan kazanır.
//   Rövanş   : ilk dizen değişir, dolayısıyla ilk atan da değişir.
public class DuelMatch
{
    public const int MarblesPerPlayer = 7;
    public const int MaxShotsPerTurn = 3;
    public const int BigMarbleValue = 2;
    public const int Neutral = 2;              // büyük misketin "sahibi"
    // Herkes DORT tur oynar. Puan alip almamasi fark etmez; dordu de
    // dolunca mac biter. Onde olan kazanir, esitse bir tur UZATMA oynanir,
    // orada da esitlik bozulmazsa berabere.
    // Cember bundan once bosalirsa mac orada biter.
    public const int TurnsPerPlayer = 4;

    public enum Phase { Placing, Shooting, Finished }
    public enum Outcome { None, PlayerOne, PlayerTwo, Draw }

    public struct Marble
    {
        public int owner;      // 0, 1 veya Neutral
        public bool out_;
        public float x, z;
        public bool big;
    }

    private readonly List<Marble> marbles = new List<Marble>();
    private readonly bool[] placed = new bool[2];
    private readonly int[] score = new int[2];

    public Phase State { get; private set; } = Phase.Placing;
    // Ilk dizen oyuncu. Atisa DIGERI baslar.
    public int FirstPlacer { get; private set; }
    public int Turn { get; private set; }
    public int ShotsThisTurn { get; private set; }
    public int MatchNumber { get; private set; }
    public bool Overtime { get; private set; }
    private readonly int[] turnsUsed = new int[2];
    private int overtimeTurnsLeft;

    public int TurnsLeft(int player) => Math.Max(0, TurnsPerPlayer - turnsUsed[player & 1]);

    public IReadOnlyList<Marble> Marbles => marbles;
    public int Score(int player) => score[player & 1];
    public bool HasPlaced(int player) => placed[player & 1];

    public int InRing(int player)
    {
        int n = 0;
        for (int i = 0; i < marbles.Count; i++)
            if (marbles[i].owner == player && !marbles[i].out_) n++;
        return n;
    }

    public int RemainingInRing
    {
        get { int n = 0; for (int i = 0; i < marbles.Count; i++) if (!marbles[i].out_) n++; return n; }
    }

    public DuelMatch(int firstPlacer = 0)
    {
        FirstPlacer = firstPlacer & 1;
        Turn = 1 - FirstPlacer;      // ilk dizen ikinci atar
    }

    // ---------------- Hazırlık ----------------

    public bool Place(int player, IList<(float x, float z)> spots)
    {
        if (State != Phase.Placing || player < 0 || player > 1 || placed[player]) return false;
        if (spots == null || spots.Count != MarblesPerPlayer) return false;

        foreach (var s in spots)
            marbles.Add(new Marble { owner = player, out_ = false, x = s.x, z = s.z, big = false });

        placed[player] = true;
        if (placed[0] && placed[1]) State = Phase.Shooting;
        return true;
    }

    // Ortadaki büyük misket. Kimsenin değil; çıkaran 2 puan alır.
    public void PlaceBigMarble(float x, float z)
    {
        marbles.Add(new Marble { owner = Neutral, out_ = false, x = x, z = z, big = true });
    }

    // ---------------- Atış ----------------

    // knocked = çemberi terk eden misketlerin indeksleri.
    // Dönen değer: sıra aynı oyuncuda mı kaldı.
    public bool ResolveShot(IList<int> knocked)
    {
        if (State != Phase.Shooting) return false;

        int shooter = Turn, other = 1 - shooter, gained = 0;

        if (knocked != null)
        {
            foreach (int i in knocked)
            {
                if (i < 0 || i >= marbles.Count) continue;
                var m = marbles[i];
                if (m.out_) continue;
                m.out_ = true; marbles[i] = m;

                if (m.big) { score[shooter] += BigMarbleValue; gained += BigMarbleValue; }
                else if (m.owner == shooter) score[other] += 1;   // kendi misketin rakibe yazılır
                else { score[shooter] += 1; gained++; }
            }
        }

        ShotsThisTurn++;

        // Çember boşaldıysa maç biter.
        if (RemainingInRing == 0) { State = Phase.Finished; return false; }

        // Puan aldıysan ve tur hakkın dolmadıysa tekrar atarsın.
        if (gained > 0 && ShotsThisTurn < MaxShotsPerTurn) return true;

        EndTurn();
        return false;
    }

    public void ForfeitTurn()
    {
        if (State != Phase.Shooting) return;
        EndTurn();
    }

    private void EndTurn()
    {
        turnsUsed[Turn]++;
        ShotsThisTurn = 0;
        Turn = 1 - Turn;

        if (Overtime)
        {
            overtimeTurnsLeft--;
            if (overtimeTurnsLeft <= 0) State = Phase.Finished;
            return;
        }

        // Ikisi de dort turunu oynamadiysa mac surer.
        if (turnsUsed[0] < TurnsPerPlayer || turnsUsed[1] < TurnsPerPlayer) return;

        // Dort tur doldu. Onde olan varsa biter; esitse bir tur uzatma.
        if (score[0] != score[1]) { State = Phase.Finished; return; }
        Overtime = true;
        overtimeTurnsLeft = 2;      // ikisine de birer tur
    }

    // ---------------- Sonuç ----------------

    public Outcome Result
    {
        get
        {
            if (State != Phase.Finished) return Outcome.None;
            if (score[0] > score[1]) return Outcome.PlayerOne;
            if (score[1] > score[0]) return Outcome.PlayerTwo;
            return Outcome.Draw;
        }
    }

    // Rövanş: ilk dizen değişir, dolayısıyla ilk atan da değişir.
    public DuelMatch Rematch()
    {
        var next = new DuelMatch(1 - FirstPlacer);
        next.MatchNumber = MatchNumber + 1;
        return next;
    }
}
