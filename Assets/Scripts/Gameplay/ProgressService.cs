using UnityEngine;

public static class ProgressService
{
    private const string StarsKeyPrefix = "MISKETR_Stars_";
    private const string UnlockedKey = "MISKETR_UnlockedCount";

    public static int UnlockedCount => Mathf.Max(1, PlayerPrefs.GetInt(UnlockedKey, 1));

    public static bool IsUnlocked(int levelIndex)
    {
        return levelIndex >= 0 && levelIndex < UnlockedCount;
    }

    public static int GetStars(int levelIndex)
    {
        return PlayerPrefs.GetInt(StarsKeyPrefix + levelIndex, 0);
    }

    public static void SaveStars(int levelIndex, int stars)
    {
        if (levelIndex < 0 || stars <= GetStars(levelIndex))
        {
            return;
        }

        PlayerPrefs.SetInt(StarsKeyPrefix + levelIndex, stars);
        PlayerPrefs.Save();
    }

    public static void UnlockNextAfter(int levelIndex)
    {
        int target = levelIndex + 2;

        if (target <= UnlockedCount)
        {
            return;
        }

        PlayerPrefs.SetInt(UnlockedKey, target);
        PlayerPrefs.Save();
    }

    public static int TotalStars(int levelCount)
    {
        int total = 0;

        for (int i = 0; i < levelCount; i++)
        {
            total += GetStars(i);
        }

        return total;
    }

    public static void ResetProgress(int levelCount)
    {
        for (int i = 0; i < levelCount; i++)
        {
            PlayerPrefs.DeleteKey(StarsKeyPrefix + i);
        }

        PlayerPrefs.DeleteKey(UnlockedKey);
        PlayerPrefs.Save();
    }
}
