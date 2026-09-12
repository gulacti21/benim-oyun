using UnityEngine;

// Sahneler arasi tasinan duello ayarlari. Tek oyunculu oyunun GameSession'i
// neyse duellonun bu. Kapali oldugunda oyunun geri kalani duellodan habersiz.
public static class DuelSession
{
    public enum Mode { HotSeat, Online }

    public static bool Active { get; private set; }
    public static Mode Kind { get; private set; } = Mode.HotSeat;
    // Ilk dizen oyuncu. Atisa DIGERI baslar; rovansta el degisir.
    public static int FirstPlacer { get; set; }
    public static int MatchNumber { get; set; }
    // Iki oyuncunun renkleri. Kozmetik secimden gelir, fizige etkisi YOKTUR.
    public static int[] Skin = { 0, 3 };

    // Duello arenasi. Kampanyadan bagimsiz: burayi degistirmek hicbir bolumu etkilemez.
    public const float ArenaSize = 3.2f;

    // Duello fizigi. 2925 atis simule edilerek secildi; hedef "ortalama bir
    // atis ~1 misket cikarsin"di, bu ayar 1.07 veriyor ve bos atis orani en
    // dusuk olan o (%36).
    //
    // Kutle DENENDI VE ELENDI: 1.4 kat agirlastirmak bile atislarin %100'unu
    // bosa cikardi, misket kipirdamiyor. Zorluk agirliktan degil aciyla
    // gelmeli. Guc kampanyaya yakin kaldi (0.80 / 0.65), fark misketin
    // kendisinde: daha sekici ve daha kaygan, temas sonrasi yola devam
    // ediyorlar. Kampanyanin hicbir ayari degismiyor.

    public const float Impulse = .80f;
    public const float Bounciness = .5f;
    public const float FrictionMul = .7f;
    public const float ShooterZ = -4.2f;

    public static void Begin(Mode kind)
    {
        Active = true; Kind = kind;
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
