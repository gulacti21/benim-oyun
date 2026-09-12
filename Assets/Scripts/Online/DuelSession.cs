using UnityEngine;

// Sahneler arasi tasinan duello ayarlari. Tek oyunculu oyunun GameSession'i
// neyse duellonun bu. Kapali oldugunda oyunun geri kalani duellodan habersiz.
public static class DuelSession
{
    public enum Mode { HotSeat, Online }
    // Oyun turu. Gercek misket oyununun iki klasik cesidi; kurallar ayni,
    // degisen sey sahanin sekli ve dolayisiyla acilar.
    // Gercek misket oyununun uc klasik cesidi:
    //   Cember -- misketler cembere dizilir, cikaran kazanir
    //   Ucgen  -- ayni kural, saha ucgen; koseler oyunu sertlestirir
    //   Dizi   -- "kondik/bas oyunu": misketler tek siraya dizilir,
    //             KIPIRDATTIGIN misket senin olur (cikarmak degil)
    //   Kuyu   -- ortada bir cukur; CUKURA DUSURDUGUN misket senin olur,
    //             kendi aticin duserse onu kaybedersin
    public enum GameType { Cember, Ucgen, Dizi, Kuyu }

    public static bool Active { get; private set; }
    public static Mode Kind { get; private set; } = Mode.HotSeat;
    public static GameType Type { get; set; } = GameType.Cember;
    public static bool Triangle => Type == GameType.Ucgen;
    public static bool Row => Type == GameType.Dizi;
    public static bool Well => Type == GameType.Kuyu;
    public static string TypeName => Type == GameType.Ucgen ? "ÜÇGEN"
                                  : Type == GameType.Dizi ? "DİZİ"
                                  : Type == GameType.Kuyu ? "KUYU" : "ÇEMBER";
    // Ilk dizen oyuncu. Atisa DIGERI baslar; rovansta el degisir.
    public static int FirstPlacer { get; set; }
    public static int MatchNumber { get; set; }
    // Iki oyuncunun renkleri. Kozmetik secimden gelir, fizige etkisi YOKTUR.
    public static int[] Skin = { 0, 3 };

    // Duello arenasi. Kampanyadan bagimsiz: burayi degistirmek hicbir bolumu etkilemez.
    // Cember 3.2; ucgen ayni yaricapa yazili oldugu icin alani cok daha
    // kucuk kalir, o yuzden biraz buyuk tutuldu. Olculdu: ucgen 3.2 cok
    // kucuk (bos atis %16, her atis misket cikariyor), 4.0 ise 0.89 ile
    // hedefin altinda kaliyor. 3.6 + guc 0.80 = 1.04, ortadaki deger.
    public const float CircleSize = 3.2f;
    public const float TriangleSize = 3.6f;
    // DIZI: siranin yarim uzunlugu. OLCULDU: dar sira (2.2, aralik .63)
    // zincirleme deviriyor -- tek atis ortalama 2.0, en iyisi 6 misket
    // aliyordu, yani el uc atista bitiyordu. Aralik genisledikce zincir
    // kiriliyor ve nisan onem kazaniyor:
    //   2.2 -> atis basina 2.00   (en iyi 6)
    //   2.8 -> 1.38               (en iyi 2)
    //   3.4 -> 1.38               (en iyi 3, iska %15)   <-- secildi
    //   4.0 -> 1.23               (en iyi 3, iska %23)
    // 3.4 secildi: ortalama makul, usta atis hala uc misket alabiliyor,
    // sira da ekrana sigacak kadar kisa.
    public const float RowSize = 3.4f;
    // Siranin zemindeki yeri. Atici z=-4.2'den atar, yani sira 4.2 birim otede.
    public const float RowZ = 0f;
    // KUYU: saha yine yuvarlak, ama ortasinda bir cukur var. Cukurun boyu
    // ve atis gucu DuelPhysicsVerify'in KUYU bolumunden geliyor.
    public const float WellSize = 3.0f;
    // Cukurun agzi. OLCULDU (cizgiden 4.2 birim, nisan sapmasi dahil):
    //   0.35 -> atislarin %26'si giriyor, 12 sayi ~47 atis (cok uzun)
    //   0.45 -> %34, ~35 atis   <-- secildi
    //   0.50 -> %51, ~23 atis
    //   0.55 -> %51, ~23 atis
    //   0.70 -> %74, ~16 atis (cok kolay, cukur kendiliginden cekiyor)
    // 0.45 secildi. 0.50 ve ustunde atislarin YARISINDAN fazlasi giriyor;
    // ustelik olcumdeki nisan sapmasi gercek oyuncudan dar, yani sahada
    // daha da kolay olurdu. 0.45'te cukur hala vurulabiliyor ama isabet
    // etmek bir sey ifade ediyor.
    public const float HoleRadius = .45f;
    public static float ArenaSize => Triangle ? TriangleSize : Row ? RowSize
                                   : Well ? WellSize : CircleSize;
    // Kuyunun sahasi da cember; farki ortadaki cukur (DuelHole cizer).
    public static ArenaShape Shape => Triangle ? ArenaShape.Triangle
                                    : Row ? ArenaShape.Line : ArenaShape.Circle;

    // Duello fizigi. 2925 atis simule edilerek secildi; hedef "ortalama bir
    // atis ~1 misket cikarsin"di, bu ayar 1.07 veriyor ve bos atis orani en
    // dusuk olan o (%36).
    //
    // Kutle DENENDI VE ELENDI: 1.4 kat agirlastirmak bile atislarin %100'unu
    // bosa cikardi, misket kipirdamiyor. Zorluk agirliktan degil aciyla
    // gelmeli. Guc kampanyaya yakin kaldi (0.80 / 0.65), fark misketin
    // kendisinde: daha sekici ve daha kaygan, temas sonrasi yola devam
    // ediyorlar. Kampanyanin hicbir ayari degismiyor.

    // OLCULDU (3900 atis): ayni guc iki sahada ayni hissi VERMIYOR. Ucgenin
    // koseleri misketi disari hunileyip goturuyor, o yuzden cemberde 1.00 ile
    // atis basina 1.03 misket cikarken ucgende ayni guc 1.68 cikardi -- yani
    // ucgen bedava kazanilan bir sahaya donusuyordu. Ucgen 3.6'da guc 0.80
    // atis basina 1.04 veriyor; iki sahanin da hedefi tuttugu tek eslesme bu.
    public static float Impulse => Triangle ? .8f : 1f;

    // DIZI: kazanmak icin misketi kipirdatmak yeterli oldugundan atis gucu
    // olcumle ayri seciliyor; degeri DuelPhysicsVerify'in DIZI bolumunden
    // geliyor (hedef yine atis basina ~1 misket).
    public const float RowImpulse = .55f;
    // Bir misket bu kadar yer degistirdiyse KIPIRDADI sayilir. Titresim ve
    // fizik gurultusunun ustunde, GERCEK bir temasin altinda olmasi lazim.
    // Olculdu: secili ayarda (sira 3.4, guc .55) gercek temasta en kucuk
    // yer degistirme .094, cogunlukla .30 uzeri. Esik .08 onun altinda.
    // Bu esigin ALTINDA kalan siyirma bilerek sayilmiyor: kuralin adi
    // "kipirdatmak", degip gecmek degil.
    public const float RowNudge = .08f;

    // KUYU atis gucu.
    //
    // Burada beceri "vurup cikarmak" degil "tam cukurda durdurmak". Ilk
    // denemede mac gucuyle (0.55-1.0) olculdu ve atislarin %100'u sahayi
    // terk etti: cukur 4.2 birim otede, oysa o guclerle misket 15-27 birim
    // gidiyor. Yani mac gucu bu mod icin anlamsiz.
    //
    // Guc, sira belirleme atisiyla AYNI mantikla cukur mesafesinden
    // turetiliyor. Fark su: sira atisinda cizgiye ULASMAK yetiyor, burada
    // tam cukurda DURMAK gerekiyor -- yani nisan gucu daha yukari alinmali,
    // aksi halde tam gucte misket cukurun uzerinden gecip gidiyor.
    // Olculdu: 0.23 ile cukura giren atis %0, 0.18 ile %34.
    public const float WellAimPower = .87f;
    public static float WellImpulse =>
        (0f - ShooterZ) / (TossTravelPerImpulse * WellAimPower);
    public const float Bounciness = .45f;
    public const float FrictionMul = .7f;

    // SIRA BELIRLEME ATISININ GUCU.
    //
    // Olculdu: bos sahada mac gucuyle (1.0) atilan misket 22.8'de duruyor,
    // yani 27 birim yol yapiyor. Cizgi ise 7-8 birim otede. Mac gucuyle
    // atilirsa kullanilir guc araligi 0.20-0.30 arasina sikisiyor: oyuncu
    // sliderin %10'luk diliminde isabet etmeye calisir, bu beceri degil sans.
    //
    // O yuzden sira atisinin gucu cizgi mesafesinden TURETILIYOR: cizgi
    // sliderin %75'ine denk gelsin. Altinda kalmak guvenli (kisa atis),
    // ustu yaniyor. Iki sahada cizgi ayni uzaklikta olmadigi icin guc de
    // sahaya gore degisiyor; boylece iki modda da ayni his olusuyor.
    public const float TossTravelPerImpulse = 27f;   // olculdu: guc 1.0 -> 27 birim
    // .75 ile olculdugunde cizgi sliderin %85'ine denk geldi (turetme sabit
    // bir yol payini hesaba katmiyor); %78'e cekmek icin biraz yukseltildi.
    public const float TossAimPower = .68f;          // cizgi sliderin bu kadarinda
    public static float TossImpulse =>
        (DuelToss.Line - ShooterZ) / (TossTravelPerImpulse * TossAimPower);

    // ATICI TEHLIKE BOLGESI. Atici cemberin ORTASINDA kalirsa kaybedilir;
    // kenarda durursa kurtulur. Yaricapin bu kadarlik ic bolgesi tehlikeli.
    //
    // Olcum: atici cemberin herhangi bir yerinde kalma orani %60 cikti --
    // yani kural bu haliyle bir secim degil, her atista odenen vergi olurdu.
    // Ic bolge %50'ye daraltilinca oran ~%30'a dustu: uc atistan birinde risk.
    public const float StrandRadiusFactor = .35f;
    public const float ShooterZ = -4.2f;

    public static void Begin(Mode kind, GameType type = GameType.Cember)
    {
        Active = true; Kind = kind; Type = type;
        FirstPlacer = 0; MatchNumber = 0;
    }

    public static void End() { Active = false; }

    public static string PlayerName(int player)
    {
        if (Kind == Mode.HotSeat) return player == 0 ? "1. OYUNCU" : "2. OYUNCU";
        return player == 0 ? "SEN" : "RAKİP";
    }

    public static Color PlayerColor(int player)
    {
        int skin = Mathf.Clamp(Skin[Mathf.Clamp(player, 0, 1)], 0, Campaign.SkinColors.Length - 1);
        return Campaign.SkinColors[skin];
    }
}
