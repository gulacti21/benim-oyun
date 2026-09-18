using System;
using System.Globalization;
using UnityEngine;

// GÜNÜN BÖLÜMÜ
// Havuz (Resources/Mahalle/Daily/DailyPool.json) Editor'de DailyLevelGenerator ile
// üretilir ve her dizilim fizikle ölçülür. Oyunda sunucu yok: telefonun tarihi
// havuzdan hangi bölümün geleceğini seçer, herkes aynı gün aynı bölümü oynar.
[Serializable]
public class DailyEntry
{
    public float size;
    public int shots, one, two, three, district, ceiling;
    public float[] marbles;          // x,z çiftleri
    public ObstacleSpot[] obstacles;
}

[Serializable]
public class DailyPool
{
    public int version = 1;
    public DailyEntry[] entries;
}

public static class DailyLevel
{
    public const string ResourcePath = "Mahalle/Daily/DailyPool";
    private static readonly DateTime Epoch = new DateTime(2026, 1, 1);
    private static DailyPool pool;
    private static LevelData built;
    private static int builtDay = -1;

    public static DailyPool Pool
    {
        get
        {
            if (pool != null) return pool;
            var text = Resources.Load<TextAsset>(ResourcePath);
            if (text == null) return null;
            try { pool = JsonUtility.FromJson<DailyPool>(text.text); } catch (Exception) { pool = null; }
            return pool;
        }
    }

    public static bool Available => Pool != null && Pool.entries != null && Pool.entries.Length > 0;

    public static int DayIndex => (int)(DateTime.Now.Date - Epoch).TotalDays;

    public static string DateLabel =>
        DateTime.Now.ToString("d MMMM", L.English ? CultureInfo.InvariantCulture : new CultureInfo("tr-TR"));

    public static LevelData Today()
    {
        int day = DayIndex;
        if (built != null && builtDay == day) return built;
        if (!Available) return null;
        var e = Pool.entries[((day % Pool.entries.Length) + Pool.entries.Length) % Pool.entries.Length];
        built = Build(e);
        builtDay = day;
        return built;
    }

    public static LevelData Build(DailyEntry e)
    {
        var l = ScriptableObject.CreateInstance<LevelData>();
        l.hideFlags = HideFlags.HideAndDontSave;
        l.name = "Gunun bolumu";
        l.levelName = "Günün Bölümü";
        l.district = Mathf.Clamp(e.district, 0, 4);
        l.mastery = false;
        l.shape = ArenaShape.Circle;
        l.arenaSize = e.size;
        l.shotCount = e.shots;
        l.oneStarTarget = e.one; l.twoStarTarget = e.two; l.threeStarTarget = e.three;
        l.starsToPass = 1;
        int n = e.marbles != null ? e.marbles.Length / 2 : 0;
        l.marbles = new MarbleSpot[n];
        for (int i = 0; i < n; i++) l.marbles[i] = new MarbleSpot(e.marbles[i * 2], e.marbles[i * 2 + 1]);
        l.obstacles = e.obstacles != null && e.obstacles.Length > 0 ? e.obstacles : null;
        l.obstacleCount = l.obstacles != null ? l.obstacles.Length : 0;
        l.shooterHalfWidth = 0f; l.shooterOffsetX = 0f;
        l.shooterStartPosition = new Vector3(0f, .25f, -4.2f);
        return l;
    }

    // Günde en fazla bu kadar deneme; yıldız alınca gün zaten kapanır.
    public const int MaxTries = 3;

    // Seri bonusu: 15 boncuk, üst üste her gün +5, en fazla +30.
    public static int RewardFor(int streak) => 15 + Mathf.Min(30, Mathf.Max(0, streak - 1) * 5);
}
