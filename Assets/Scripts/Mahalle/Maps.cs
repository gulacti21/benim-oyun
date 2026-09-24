using UnityEngine;

// HARİTALAR: oyun birden fazla kampanya haritasından oluşur.
//   Harita 0 — Mahalle  (Campaign + LevelBook, 60 bölüm, mahalle 0-4)
//   Harita 1 — Memleket (MemleketCampaign + MemleketBook, 60 bölüm, bölge 5-9)
//
// Bölüm numarası GLOBAL tutulur: harita * PerMap + haritadaki sıra. Böylece
// kilit zinciri, kayıt ve oyun akışı tek bir sayıyla çalışır; Harita 1'in
// bölümleri 0-59 olarak kalır ve eski kodun hiçbiri değişmez.
// Bölge (district) numarası da global: Memleket'in bölgeleri 5-9. Tema, zemin
// ve harita görselleri bu numarayla bulunur.
//
// Yeni harita eklemek: Database() ve Names'e bir satır, MahalleTheme'e 5 tema,
// MahalleWorld/MahalleMapView dosya listelerine 5 ad. Harita sayısı başka hiçbir
// yerde sabit yazılmaz.
public static class Maps
{
    public const int PerMap = 60;
    public const int DistrictsPerMap = 5;
    public static readonly string[] Names = { "Mahalle", "Memleket" };
    public static readonly string[] Subtitles = { "Apartmandan meydana", "Yaz tatili, köyde misket" };
    public static int Count => Names.Length;
    public static int TotalLevels => Count * PerMap;
    public static int TotalDistricts => Count * DistrictsPerMap;

    public static LevelDatabase Database(int map)
    {
        switch (map)
        {
            case 0: return Campaign.Database;
            case 1: return MemleketCampaign.Database;
            default: return null;
        }
    }

    public static int MapOf(int globalIndex) => Mathf.Clamp(globalIndex / PerMap, 0, Count - 1);
    public static int Local(int globalIndex) => globalIndex - MapOf(globalIndex) * PerMap;
    public static int Global(int map, int local) => map * PerMap + local;
    public static int First(int map) => map * PerMap;
    public static int MapOfDistrict(int district) => Mathf.Clamp(district / DistrictsPerMap, 0, Count - 1);
    public static int FirstDistrict(int map) => map * DistrictsPerMap;
    public static bool Valid(int globalIndex) => globalIndex >= 0 && globalIndex < TotalLevels;

    public static LevelData Get(int globalIndex)
    {
        if (!Valid(globalIndex)) return null;
        var db = Database(MapOf(globalIndex));
        return db != null ? db.Get(Local(globalIndex)) : null;
    }

    // Bölge adı (global bölge numarası). Harita 0 için Campaign.Districts'in aynısı.
    public static string DistrictName(int district)
    {
        if (district >= 0 && district < Campaign.Districts.Length) return Campaign.Districts[district];
        int d = district - DistrictsPerMap;
        if (d >= 0 && d < MemleketCampaign.Districts.Length) return MemleketCampaign.Districts[d];
        return "";
    }
}
