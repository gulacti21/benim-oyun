using UnityEngine;

// HARİTA 2 — MEMLEKET (yaz tatili). 5 bölge × 12 bölüm, global bölge 5-9.
// Taban formülle üretilir, MemleketBook elle tasarlananları üstüne yazar
// (Harita 1'deki Campaign + LevelBook düzeninin aynısı).
public static class MemleketCampaign
{
    public const int PerDistrict = 12;
    public const int Count = 60;
    public const int FirstDistrict = 5;   // global bölge numarası
    public static readonly string[] Districts = { "Sahil", "Köy Meydanı", "Yayla", "Kasaba Pazarı", "Bayram Yeri" };
    public static readonly string[] Descriptions = {
        "Kum ayağının altında. Misket de yavaşlıyor.", "Yağmur yeni dindi, çamura dikkat.",
        "Serin rüzgâr, eğimli çayır.", "Tezgâhların arasından bant atışı.", "Bütün yaz bu güne çıkar." };

    private static LevelDatabase cached;

    public static LevelDatabase Database
    {
        get
        {
            if (cached != null) return cached;
            cached = ScriptableObject.CreateInstance<LevelDatabase>();
            cached.name = "Memleket Campaign";
            cached.hideFlags = HideFlags.HideAndDontSave;
            cached.levels = new LevelData[Count];
            for (int i = 0; i < Count; i++)
            {
                int region = i / PerDistrict, local = i % PerDistrict;
                var level = ScriptableObject.CreateInstance<LevelData>();
                level.hideFlags = HideFlags.HideAndDontSave;
                level.levelName = MahalleTheme.Names(FirstDistrict + region)[local];
                level.district = FirstDistrict + region;
                level.mastery = local == 11;
                level.shape = (local % 3 == 1) ? ArenaShape.Circle : ArenaShape.Triangle;
                level.arenaSize = level.shape == ArenaShape.Circle ? 3.2f + region * .1f : 3.1f + (local > 6 ? .25f : 0f);
                level.triangleRows = local < 4 ? 4 : 5;
                int outer = local > 5 ? 10 : 8;
                level.rings = new[] { new MarbleRing { count = 1, radiusFactor = 0f },
                                      new MarbleRing { count = outer, radiusFactor = .66f, angleOffset = region * 17f } };
                int total = level.shape == ArenaShape.Circle ? outer + 1 : level.triangleRows * (level.triangleRows + 1) / 2;
                level.shotCount = level.mastery ? 4 : 5;
                level.starsToPass = level.mastery ? 2 : 1;
                level.oneStarTarget = Mathf.Max(1, Mathf.CeilToInt(total * .5f));
                level.twoStarTarget = Mathf.CeilToInt(total * .75f);
                level.threeStarTarget = total;
                level.obstacleCount = local >= 3 ? (local % 3 == 0 ? 2 : 1) : 0;
                level.shooterStartPosition = new Vector3(0f, .25f, -4.2f);
                cached.levels[i] = level;
            }
            for (int i = 0; i < Count; i++) MemleketBook.Apply(cached.levels[i], i);
            return cached;
        }
    }

#if UNITY_EDITOR
    // Doğrulama araçları bölüm dizilimini değiştirip yeniden ölçebilsin diye.
    public static void ResetCache() { cached = null; }
#endif
}
