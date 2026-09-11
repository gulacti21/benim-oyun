using System;
using System.Collections.Generic;

// MISKETR ONLINE — maç kural motoru.
//
// Burası bilerek saf C#: Unity'ye, sahneye, fiziğe hiç bakmaz. Sebebi şu —
// kuralların doğruluğunu sahne kurmadan, telefon bağlamadan, ağ yazmadan
// test edebilmek. Fizik ve arayüz bunun üstüne oturacak.
//
// KURALLAR
//   Hazırlık : iki oyuncu da 7'şer misket dizer, aynı anda ve birbirini görmeden.
//   Atış     : sırayla, 6'şar tur. Bir tur bir atıştır; o atışta RAKİBİN misketini
//              çıkardıysan aynı turda tekrar atarsın, turda en fazla 3 atış.
//   Ceza     : kendi misketini çıkarırsan gider ve sana yazılır. Kendi misketini
//              çıkarmak turu uzatmaz — kötü atış ödüllendirilmez.
//   Bitiş    : iki oyuncunun da turu bitince, ya da birinin çemberinde misket
//              kalmayınca. Çemberde misketi çok olan kazanır.
//   Rövanş   : ilk başlayan oyuncu değişir.
public class DuelMatch
{
    public const int MarblesPerPlayer = 7;
    public const int TurnsPerPlayer = 6;
    public const int MaxShotsPerTurn = 3;

    public enum Phase { Placing, Shooting, Finished }
    public enum Outcome { None, PlayerOne, PlayerTwo, Draw }

    // Çemberdeki her misket. Sahip 0 veya 1; cikti true olunca çemberden ayrılmıştır.
    public struct Marble
    {
        public int owner;
        public bool out_;
        public float x, z;
    }

    private readonly List<Marble> marbles = new List<Marble>();
    private readonly bool[] placed = new bool[2];
    private readonly int[] turnsUsed = new int[2];

    public Phase State { get; private set; } = Phase.Placing;
    public int StartingPlayer { get; private set; }
    public int Turn { get; private set; }
    public int ShotsThisTurn { get; private set; }
    public int MatchNumber { get; private set; }

    public IReadOnlyList<Marble> Marbles => marbles;
    public int TurnsLeft(int player) => Math.Max(0, TurnsPerPlayer - turnsUsed[player]);
    public bool HasPlaced(int player) => placed[player];

    // Bir oyuncunun çemberde kalan misketi. Kazananı bu belirler.
    public int InRing(int player)
    {
        int n = 0;
        for (int i = 0; i < marbles.Count; i++)
            if (marbles[i].owner == player && !marbles[i].out_) n++;
        return n;
    }

    public DuelMatch(int startingPlayer = 0)
    {
        StartingPlayer = startingPlayer & 1;
        Turn = StartingPlayer;
    }

    // ---------------- Hazırlık ----------------

    // Oyuncu dizilişini kilitler. İkisi de kilitlemeden çember açılmaz;
    // "aynı anda ve gizli" kuralı böyle korunuyor.
    public bool Place(int player, IList<(float x, float z)> spots)
    {
        if (State != Phase.Placing || player < 0 || player > 1 || placed[player]) return false;
        if (spots == null || spots.Count != MarblesPerPlayer) return false;

        foreach (var s in spots)
            marbles.Add(new Marble { owner = player, out_ = false, x = s.x, z = s.z });

        placed[player] = true;
        if (placed[0] && placed[1]) State = Phase.Shooting;
        return true;
    }

    // ---------------- Atış ----------------

    // Bir atış çözüldükten sonra çağrılır. knocked = çemberi terk eden
    // misketlerin indeksleri. Dönen değer: sıra aynı oyuncuda mı kaldı.
    public bool ResolveShot(IList<int> knocked)
    {
        if (State != Phase.Shooting) return false;

        int shooter = Turn;
        int opponentOut = 0;

        if (knocked != null)
        {
            foreach (int i in knocked)
            {
                if (i < 0 || i >= marbles.Count) continue;
                var m = marbles[i];
                if (m.out_) continue;
                m.out_ = true;
                marbles[i] = m;
                if (m.owner != shooter) opponentOut++;
            }
        }

        ShotsThisTurn++;

        // Biri tükendiyse maç orada biter; kalan turları oynamanın anlamı yok.
        if (InRing(0) == 0 || InRing(1) == 0) { Finish(); return false; }

        // Rakibin misketini çıkardıysan ve tur hakkın dolmadıysa tekrar atarsın.
        if (opponentOut > 0 && ShotsThisTurn < MaxShotsPerTurn) return true;

        EndTurn();
        return false;
    }

    // Oyuncu atmadan turu bitirirse (süre doldu, bağlantı koptu) burası çalışır.
    public void ForfeitTurn()
    {
        if (State != Phase.Shooting) return;
        EndTurn();
    }

    private void EndTurn()
    {
        turnsUsed[Turn]++;
        ShotsThisTurn = 0;

        if (turnsUsed[0] >= TurnsPerPlayer && turnsUsed[1] >= TurnsPerPlayer) { Finish(); return; }

        // Turu biten oyuncu varsa sıra hep diğerine geçer; ikisinin de turu
        // varsa normal sırayla devam eder.
        int other = 1 - Turn;
        Turn = turnsUsed[other] < TurnsPerPlayer ? other : Turn;
    }

    private void Finish() { State = Phase.Finished; }

    // ---------------- Sonuç ----------------

    public Outcome Result
    {
        get
        {
            if (State != Phase.Finished) return Outcome.None;
            int a = InRing(0), b = InRing(1);
            if (a > b) return Outcome.PlayerOne;
            if (b > a) return Outcome.PlayerTwo;
            return Outcome.Draw;
        }
    }

    // Rövanş: aynı kurallar, ilk başlayan değişir.
    public DuelMatch Rematch()
    {
        var next = new DuelMatch(1 - StartingPlayer);
        next.MatchNumber = MatchNumber + 1;
        return next;
    }
}
