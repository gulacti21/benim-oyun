// ÖĞRETİCİ: bütün anlatım 1. bölümün (Apartman Önü 01) üzerinde yapılır.
// Dizilim ve hedefler aynen gerçek bölümdür; öğretici sırasında sadece
// atış hakkı bitince kaybetme yoktur (LevelController.EvaluateTutorialTurn).
public static class TutorialLevel
{
    public const int LevelIndex = 0;
    public static LevelData Get() => Campaign.Database.Get(LevelIndex);
}
