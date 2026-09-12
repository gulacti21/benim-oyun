using UnityEngine;

// SIRA BELIRLEME ATISI ("cizgi atisi").
//
// Gercek misket oyununda mac baslamadan once kimin once atacagi kura ile
// belirlenmez: iki oyuncu bir cizgiye atar, cizgiye en yakin duran baslar.
// Cizgiyi GECEN yanar, cunku isin puf noktasi gucu tutabilmek.
//
// Burada cizgi sahanin UZAK kenari. Ekstra cizim gerekmiyor, saha cemberi
// zaten orada duruyor; olcum de sahanin uzak kenarina olan uzaklik.
//
// Neden kura degil de atis: kura oyuncuya hicbir sey ogretmez. Cizgi atisi
// ilk saniyeden guc ayarini ogretiyor, ve mac icinde en kritik beceri o
// (fazla vurursan atici cemberden cikar, az vurursan ortada kalir).
//
// Pay to win yok: iki oyuncu da ayni misketle, ayni cizgiye atar.
public static class DuelToss
{
    // Hedef cizgi: sahanin uzak kenari.
    // Cemberde bu yaricapin kendisi. Ucgende koseler 270/30/150 derecede,
    // yani uzak taraf 30 ve 150 dereceyi birlestiren YATAY kenar: z = .5r.
    // (Sivri uc aticiya bakiyor.)
    public static float Line => DuelSession.Triangle ? DuelSession.ArenaSize * .5f
                                                    : DuelSession.ArenaSize;

    // Cizgiyi gecip sahayi terk etmenin cezasi. Herhangi bir gecerli
    // uzaklikdan buyuk olmasi yeterli; kac birim tastigi da eklenir ki
    // iki oyuncu da yaktiysa az tasan kazansin.
    public const float FoulPenalty = 100f;

    // -1 = henuz atmadi.
    private static readonly float[] score = { -1f, -1f };
    private static readonly bool[] foul = { false, false };

    // Su an atan oyuncu.
    public static int Shooter { get; private set; }
    public static bool Done => score[0] >= 0f && score[1] >= 0f;
    public static bool HasShot(int player) => score[Clamp(player)] >= 0f;
    public static float Score(int player) => score[Clamp(player)];
    public static bool Fouled(int player) => foul[Clamp(player)];

    private static int Clamp(int p) => Mathf.Clamp(p, 0, 1);

    public static void Reset(int first)
    {
        score[0] = score[1] = -1f;
        foul[0] = foul[1] = false;
        Shooter = Clamp(first);
    }

    // Atisin durdugu yerden puan uretir. Kucuk olan iyidir.
    //
    // YANMA KURALI: cizgiyi GECMEK yanar. Kisa kalmak yanmaz, sadece kotu
    // puan alir -- atici cizginin gerisinden, sahanin bile disindan basliyor,
    // o yuzden "sahanin disinda kalmak" yanma sayilamaz.
    public static bool Crossed(float z) => z > Line;

    public static float Measure(float x, float z, bool fell)
    {
        float sapma = Mathf.Abs(Line - z);
        // Yandan uzaklasmak da sayilir: cizgiye dik gelmek gerekir.
        float yan = Mathf.Abs(x) * .5f;
        float d = sapma + yan;
        return (fell || Crossed(z)) ? FoulPenalty + d : d;
    }

    public static void Record(int player, float value)
    {
        int p = Clamp(player);
        if (score[p] >= 0f) return;                 // bir oyuncu bir kez atar
        score[p] = Mathf.Max(0f, value);
        foul[p] = value >= FoulPenalty;
        if (!Done) Shooter = 1 - p;
    }

    // Cizgiye en yakin olan kazanir. Esitlikte ilk atan kazanir; bu durum
    // pratikte olmuyor ama kural belirsiz kalmasin.
    public static int Winner
    {
        get
        {
            if (!Done) return -1;
            if (score[0] <= score[1]) return 0;
            return 1;
        }
    }

    // Atisi kazanan ONCE ATAR. Kurallarda once atan, ikinci dizen oyuncudur
    // (dizen taraf misketlerini ortaya koyup bekler), o yuzden ilk dizen
    // atisi KAYBEDEN olur.
    public static int FirstPlacer => Done ? 1 - Winner : 0;

    public static string ScoreText(int player)
    {
        int p = Clamp(player);
        if (score[p] < 0f) return "—";
        if (foul[p]) return "YANDI";
        return score[p].ToString("0.00") + " br";
    }
}
