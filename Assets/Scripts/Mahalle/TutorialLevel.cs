using UnityEngine;

// ÖĞRETİCİ: ısınma sahası. Kampanyaya dahil değil; kayıt, boncuk ve
// istatistik tutmaz. Apartman 01'in kopyası, engelsiz ve bol atışlı.
public static class TutorialLevel
{
    private static LevelData cached;

    public static LevelData Get()
    {
        if (cached != null) return cached;
        cached = Object.Instantiate(Campaign.Database.Get(0));
        cached.hideFlags = HideFlags.HideAndDontSave;
        cached.name = "Isinma";
        cached.levelName = "Isınma";
        cached.district = 0;
        cached.mastery = false;
        cached.obstacles = null;
        cached.obstacleCount = 0;
        cached.shotCount = 20;
        cached.starsToPass = 1;
        cached.oneStarTarget = cached.twoStarTarget = cached.threeStarTarget = 3;
        cached.marbles = new[] { new MarbleSpot(0f, .9f), new MarbleSpot(-.7f, .5f), new MarbleSpot(.7f, .5f) };
        return cached;
    }
}
