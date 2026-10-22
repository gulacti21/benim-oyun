using UnityEngine;

// HARİTA 3 — ŞEHİR. 5 bölge × 12 bölüm, global bölge 10-14.
// 1.2 (kullanıcı kararı 2026-10-19): "yavaş yavaş, bir anda 60 değil". Şimdilik ilk Ready
// bölüm elle tasarlandı (SehirBook); kalanlar formülden yer tutucu, KİLİTLİ ("çok yakında").
// Yeni bölüm eklemek: SehirBook'a case yaz, Ready'yi artır, HumanTuning ile ölç.
public static class SehirCampaign
{
    public const int PerDistrict = 12;
    public const int Count = 60;
    public const int Ready = 60;
    // ALÇAK KALDIRIM (ölçüldü, CurbVerify 2026-10-19): Şehir'in engelleri 10 cm. Yuvarlanan misket
    // 2.5 m/s ve üstünde gelirse üstünden aşar, daha yavaşsa takılıp geri seker. (8 cm de 2.5'te
    // aşıyor ama 4 m/s'den sonra fazla sıçrıyor; 12 cm 4 m/s ister, çarpışmadan çıkan misket aşamaz.)
    public const float CurbHeight = .10f;
    // Şehir ve sonraki haritalar (Harita 4 Lunapark da) aynı alçak kaldırımı kullanır.
    public static bool IsCity(LevelData l) => l != null && l.district >= FirstDistrict;
    public const int FirstDistrict = 10;   // global bölge numarası
    public static readonly string[] Districts = { "Şehir Merkezi", "Sanayi", "Liman", "AVM", "Gökdelenler" };
    public static readonly string[] Descriptions = {
        "Kaldırım taşları ve rögar kapakları.", "Beton zemin, yağ lekesi.",
        "Islak rıhtım, halat yığınları.", "Parlak karo, kalabalık.", "Gece ışıkları altında büyük final." };

    private static LevelDatabase cached;

    public static LevelDatabase Database
    {
        get
        {
            if (cached != null) return cached;
            cached = ScriptableObject.CreateInstance<LevelDatabase>();
            cached.name = "Sehir Campaign";
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
                level.arenaSize = 3.2f;
                level.rings = new[] { new MarbleRing { count = 1, radiusFactor = 0f },
                                      new MarbleRing { count = 8, radiusFactor = .5f, angleOffset = region * 17f + local * 7f } };
                level.shotCount = 5;
                level.starsToPass = level.mastery ? 2 : 1;
                level.oneStarTarget = 4; level.twoStarTarget = 6; level.threeStarTarget = 9;
                level.shooterStartPosition = new Vector3(0f, .25f, -4.2f);
                cached.levels[i] = level;
            }
            for (int i = 0; i < Count; i++) SehirBook.Apply(cached.levels[i], i);
            for (int i = 0; i < Count; i++) HumanTuning.Apply(cached.levels[i], 2 * Maps.PerMap + i);
            for (int i = 0; i < Count; i++) HumanTuning.SpreadStars(cached.levels[i], 2 * Maps.PerMap + i);
            return cached;
        }
    }

#if UNITY_EDITOR
    public static void ResetCache() { cached = null; }
#endif
}
