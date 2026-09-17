using UnityEngine;

// SIRA BELIRLEME ATISI ("cizgi atisi").
//
// Gercek misket oyununda kimin once atacagi kura ile belirlenmez: iki oyuncu
// bir cizgiye atar, cizgiye en yakin duran baslar. (Geleneksel kural:
// "oyuncular bir cizgiye misket atarlar, en yakin olanin atisi sirayla
// baslar".) Cizgiyi GECEN yanar, cunku isin puf noktasi gucu tutmak.
//
// Burada cizgi sahanin uzak kenari: ekstra cizim gerekmiyor, saha zaten
// orada duruyor.
//
// Neden kura degil: kura oyuncuya hicbir sey ogretmez. Cizgi atisi ilk
// saniyeden guc ayarini ogretiyor, ve mac icinde en kritik beceri o
// (fazla vurursan atici sahadan cikar, az vurursan ortada kalirsin).
//
// Pay to win yok: iki oyuncu da ayni misketle, ayni cizgiye atar.
//
// Durum ORNEK tabanli tutulur ki online oyunda iki tarafin durumu ayri ayri
// izlenebilsin; tek cihazdaki mac icin statik yuz (Current) var.
public class DuelToss
{
    // Hedef cizgi: sahanin uzak kenari. Sahanin seklinden turer, mac
    // durumuna bagli degildir -- o yuzden statik.
    // Cemberde bu yaricapin kendisi. Ucgende koseler 270/30/150 derecede,
    // yani uzak taraf 30 ve 150 dereceyi birlestiren YATAY kenar: z = .5r.
    // (Sivri uc aticiya bakiyor.)
    // DIZI modunda cizgi ZATEN var: misketlerin dizildigi sira. Sira
    // belirleme atisi da ona yapilir, gercek oyundaki gibi.
    public static float Line => DuelSession.Row ? DuelSession.RowZ
                              : DuelSession.Triangle ? DuelSession.ArenaSize * .5f
                              : DuelSession.ArenaSize;

    // Cizgiyi gecmenin cezasi. Herhangi bir gecerli uzakliktan buyuk olmasi
    // yeterli; kac birim tastigi da eklenir ki iki oyuncu da yaktiysa az
    // tasan kazansin.
    public const float FoulPenalty = 100f;

    // YANMA KURALI: cizgiyi GECMEK yanar. Kisa kalmak yanmaz, sadece kotu
    // puan alir -- atici cizginin gerisinden, sahanin bile disindan
    // basliyor, o yuzden "sahanin disinda kalmak" yanma sayilamaz.
    public static bool Crossed(float z) => z > Line;

    public static float Measure(float x, float z, bool fell)
    {
        float sapma = Mathf.Abs(Line - z);
        // Yandan uzaklasmak da sayilir: cizgiye dik gelmek gerekir.
        float yan = Mathf.Abs(x) * .5f;
        float d = sapma + yan;
        return (fell || Crossed(z)) ? FoulPenalty + d : d;
    }

    // ---------------- Ornek durumu ----------------

    private readonly float[] score = { -1f, -1f };   // -1 = henuz atmadi
    private readonly bool[] foul = { false, false };

    public int TurnOf { get; private set; }
    public bool Finished => score[0] >= 0f && score[1] >= 0f;
    public bool Threw(int player) => score[Clamp(player)] >= 0f;
    public float ScoreOf(int player) => score[Clamp(player)];
    public bool FouledBy(int player) => foul[Clamp(player)];

    private static int Clamp(int p) => Mathf.Clamp(p, 0, 1);

    public void ResetTo(int first)
    {
        score[0] = score[1] = -1f;
        foul[0] = foul[1] = false;
        TurnOf = Clamp(first);
    }

    public void RecordFor(int player, float value)
    {
        int p = Clamp(player);
        if (score[p] >= 0f) return;                 // bir oyuncu bir kez atar
        score[p] = Mathf.Max(0f, value);
        foul[p] = value >= FoulPenalty;
        if (!Finished) TurnOf = 1 - p;
    }

    // Cizgiye en yakin olan kazanir. Esitlikte ilk oyuncu; pratikte olmuyor
    // ama kural belirsiz kalmasin.
    public int WinnerOf => !Finished ? -1 : (score[0] <= score[1] ? 0 : 1);

    // Atisi kazanan ONCE ATAR. Kurallarda once atan, ikinci dizen oyuncudur
    // (dizen taraf misketlerini ortaya koyup bekler), o yuzden ilk dizen
    // atisi KAYBEDEN olur.
    public int FirstPlacerOf => Finished ? 1 - WinnerOf : 0;

    public string TextFor(int player)
    {
        int p = Clamp(player);
        if (score[p] < 0f) return "—";
        if (foul[p]) return L.T("YANDI");
        return score[p].ToString("0.00") + " br";
    }

    // ---------------- Statik yuz (tek cihazdaki mac) ----------------

    public static DuelToss Current { get; private set; } = new DuelToss();

    public static void Reset(int first) { Current = new DuelToss(); Current.ResetTo(first); }
    public static void Record(int player, float value) => Current.RecordFor(player, value);

    public static int Shooter => Current.TurnOf;
    public static bool Done => Current.Finished;
    public static bool HasShot(int player) => Current.Threw(player);
    public static float Score(int player) => Current.ScoreOf(player);
    public static bool Fouled(int player) => Current.FouledBy(player);
    public static int Winner => Current.WinnerOf;
    public static int FirstPlacer => Current.FirstPlacerOf;
    public static string ScoreText(int player) => Current.TextFor(player);
}
