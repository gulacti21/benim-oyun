public static class GameSession
{
    public const string GameSceneName = "Game";
    public const string LevelSelectSceneName = "LevelSelect";

    public static int SelectedLevelIndex { get; set; }
    // ÖĞRETİCİ: true iken Game sahnesi kampanya bölümü yerine ısınma sahasını kurar.
    public static bool TutorialMode { get; set; }
    // GÜNÜN BÖLÜMÜ: true iken Game sahnesi tarihe göre günlük bölümü kurar.
    public static bool DailyMode { get; set; }
    // SONSUZ ÇEMBER: kampanya dışı, bitmeyen mod.
    public static bool EndlessMode { get; set; }
}
