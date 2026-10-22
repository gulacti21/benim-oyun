using UnityEngine;

// HARİTA 4 — LUNAPARK. 5 bölge × 12 bölüm, global bölge 15-19, global bölüm 180-239.
// 2026-10-20 (kullanıcı): Şehir'den biraz daha zor; ilk bölge (12 bölüm) SADECE yeni özellikler
// (tampon, yürüyen bant, mıknatıs misket), sonraki bölgelerde eski özellikler karışık gelir.
// Bölge bölge ekleniyor: ilk Ready bölüm elle tasarlandı (LunaparkBook), kalanlar KİLİTLİ ("çok yakında").
public static class LunaparkCampaign
{
    public const int PerDistrict = 12;
    public const int Count = 60;
    public const int Ready = 60;
    public const int FirstDistrict = 15;   // global bölge numarası
    public const int MapIndex = 3;
    public static readonly string[] Districts = { "Giriş Kapısı", "Çarpışan Arabalar", "Dönme Dolap", "Oyun Sokağı", "Sirk Çadırı" };
    public static readonly string[] Descriptions = {
        "Balonlar, pamuk şeker, ilk tampon.", "Lastik duvarlar, sert çarpışmalar.",
        "Yürüyen yollar, dönen kabinler.", "Halka atma, mıknatıslı ödüller.", "Işıklar altında büyük gösteri." };

    private static LevelDatabase cached;

    public static LevelDatabase Database
    {
        get
        {
            if (cached != null) return cached;
            cached = ScriptableObject.CreateInstance<LevelDatabase>();
            cached.name = "Lunapark Campaign";
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
                // Yer tutucu (kilitli bölüm): basit halka. Oynanmaz, sadece veri tutarlı olsun.
                level.shape = ArenaShape.Circle;
                level.arenaSize = 3.3f;
                level.rings = new[] { new MarbleRing { count = 1, radiusFactor = 0f },
                                      new MarbleRing { count = 9, radiusFactor = .5f, angleOffset = region * 19f + local * 11f } };
                level.shotCount = 5;
                level.starsToPass = level.mastery ? 2 : 1;
                level.oneStarTarget = 4; level.twoStarTarget = 6; level.threeStarTarget = 10;
                level.shooterStartPosition = new Vector3(0f, .25f, -4.2f);
                cached.levels[i] = level;
            }
            for (int i = 0; i < Count; i++) LunaparkBook.Apply(cached.levels[i], i);
            for (int i = 0; i < Count; i++) HumanTuning.Apply(cached.levels[i], MapIndex * Maps.PerMap + i);
            for (int i = 0; i < Count; i++) HumanTuning.SpreadStars(cached.levels[i], MapIndex * Maps.PerMap + i);
            return cached;
        }
    }

#if UNITY_EDITOR
    public static void ResetCache() { cached = null; }
#endif
}
