public static class GameSession
{
    public const string GameSceneName = "Game";
    public const string LevelSelectSceneName = "LevelSelect";

    public static int SelectedLevelIndex { get; set; }
    // ÖĞRETİCİ: true iken Game sahnesi kampanya bölümü yerine ısınma sahasını kurar.
    public static bool TutorialMode { get; set; }
}
