public static class GameSession
{
    public const string GameSceneName = "Game";
    public const string LevelSelectSceneName = "LevelSelect";

    // GLOBAL bölüm numarası (Maps): 0-59 Mahalle, 60-119 Memleket.
    public static int SelectedLevelIndex { get; set; }
    // Oynanan bölümün haritası. Bölüm numarasından türetilir, ikisi hiç ayrışmaz.
    public static int SelectedMap => Maps.MapOf(SelectedLevelIndex);
    // ÖĞRETİCİ: true iken Game sahnesi kampanya bölümü yerine ısınma sahasını kurar.
    public static bool TutorialMode { get; set; }
    // GÜNÜN BÖLÜMÜ: true iken Game sahnesi tarihe göre günlük bölümü kurar.
    public static bool DailyMode { get; set; }
    // SONSUZ ÇEMBER: kampanya dışı, bitmeyen mod.
    public static bool EndlessMode { get; set; }
}
