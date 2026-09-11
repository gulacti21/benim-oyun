using UnityEngine;

// Sahneler arasi tasinan duello ayarlari. Tek oyunculu oyunun GameSession'i
// neyse duellonun bu. Kapali oldugunda oyunun geri kalani duellodan habersiz.
public static class DuelSession
{
    public enum Mode { HotSeat, Online }

    public static bool Active { get; private set; }
    public static Mode Kind { get; private set; } = Mode.HotSeat;
    // Rovansta ilk baslayan degisir; bu deger maclar arasi tasinir.
    public static int StartingPlayer { get; set; }
    public static int MatchNumber { get; set; }
    // Iki oyuncunun renkleri. Kozmetik secimden gelir, fizige etkisi YOKTUR.
    public static int[] Skin = { 0, 3 };

    // Duello arenasi. Kampanyadan bagimsiz: burayi degistirmek hicbir bolumu etkilemez.
    public const float ArenaSize = 3.2f;
    public const float ShooterZ = -4.2f;

    public static void Begin(Mode kind)
    {
        Active = true; Kind = kind;
        StartingPlayer = 0; MatchNumber = 0;
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
