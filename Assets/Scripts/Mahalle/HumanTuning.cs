using UnityEngine;

// İNSAN AYARI (2026-10-01, kullanıcı kararı). Bölümler eskiden "kusursuz oyuncu" tavanına göre
// ayarlanmıştı; kullanıcı Park'ı özel misket kullanmadan geçemedi. 120 bölüm "sıradan oyuncu"
// modeliyle ölçüldü (MemleketPhysicsVerify.BatchHuman: nişan/güç hatalı, 60 oyun; +1 ve +2 atış
// turları dahil, Logs/HumanAll*.tsv) ve sadece ATIŞ HAKKI ile GEÇME HEDEFİ değiştirildi.
// Dizilim, engel, zemin, fizik aynı.
//
// Kurallar (kullanıcı): yavaş yavaş zorlaşsın. Her bölgede:
//   N (normal, 10 bölüm) : tek denemede geçme oranı eğriye yakın, en az ~%25 (bölge başı yüksek, sonu düşük)
//   U (12. bölüm, sınav) : zor ama Kesem'siz geçilir
//   K (Kesem bölümü)     : Kesem'siz neredeyse geçilmez (sıradan oyuncu ≤ %4), güçle rahat;
//                          hedef kusursuz oyuncunun tavanını AŞMAZ (teorik olarak mümkün kalır).
//                          12.'ye yapışık değil (6-9. bölümler arasından).
// Ustalık sınavında (geçiş 2 yıldız) hedef 2 yıldıza yazılır.
// Bir bölümü değiştirirsen: BatchHuman -humanLevels <g> ile yeniden ölç, bu satırı güncelle.
public static class HumanTuning
{
    // { global bölüm, atış hakkı, geçme hedefi, rol }
    private static readonly (int g, int shots, int pass, char role)[] Rows =
    {
        ( 5, 5, 3, 'N' ),   // Apartman 06 · sıradan oyuncu %67 -> %80
        ( 7, 5, 6, 'K' ),   // Apartman 08 · sıradan oyuncu %17 -> %3
        ( 8, 2, 3, 'N' ),   // Apartman 09 · sıradan oyuncu %58 -> %95
        ( 9, 3, 5, 'N' ),   // Apartman 10 · sıradan oyuncu %43 -> %77
        ( 10, 5, 4, 'N' ),   // Apartman 11 · sıradan oyuncu %0 -> %73
        ( 11, 3, 5, 'U' ),   // Apartman 12 · sıradan oyuncu %0 -> %20
        ( 14, 4, 4, 'N' ),   // Okul 03 · sıradan oyuncu %65 -> %95
        ( 15, 5, 5, 'N' ),   // Okul 04 · sıradan oyuncu %32 -> %87
        ( 16, 5, 7, 'N' ),   // Okul 05 · sıradan oyuncu %8 -> %70
        ( 17, 5, 7, 'N' ),   // Okul 06 · sıradan oyuncu %30 -> %73
        ( 18, 5, 4, 'N' ),   // Okul 07 · sıradan oyuncu %15 -> %72
        ( 19, 4, 5, 'N' ),   // Okul 08 · sıradan oyuncu %12 -> %63
        ( 21, 4, 4, 'N' ),   // Okul 10 · sıradan oyuncu %3 -> %67
        ( 22, 4, 4, 'N' ),   // Okul 11 · sıradan oyuncu %22 -> %70
        ( 23, 5, 8, 'U' ),   // Okul 12 · sıradan oyuncu %0 -> %27
        ( 24, 4, 4, 'N' ),   // Park 01 · sıradan oyuncu %8 -> %68
        ( 25, 5, 3, 'N' ),   // Park 02 · sıradan oyuncu %57 -> %72
        ( 26, 5, 3, 'N' ),   // Park 03 · sıradan oyuncu %7 -> %63
        ( 27, 5, 5, 'N' ),   // Park 04 · sıradan oyuncu %0 -> %63
        ( 28, 6, 4, 'N' ),   // Park 05 · sıradan oyuncu %0 -> %67
        ( 29, 5, 3, 'N' ),   // Park 06 · sıradan oyuncu %2 -> %60
        ( 30, 4, 5, 'N' ),   // Park 07 · sıradan oyuncu %0 -> %53
        ( 31, 5, 3, 'N' ),   // Park 08 · sıradan oyuncu %2 -> %53
        ( 33, 5, 3, 'N' ),   // Park 10 · sıradan oyuncu %0 -> %22 · kullanıcı ayarı 2026-10-01 (kolaydı: 6->5 atış)
        ( 34, 4, 5, 'N' ),   // Park 11 · sıradan oyuncu %0 -> %72
        ( 35, 5, 9, 'U' ),   // Park 12 · sıradan oyuncu %0 -> %33
        ( 37, 6, 3, 'N' ),   // Toprak 02 · sıradan oyuncu %0 -> %48
        ( 38, 6, 4, 'N' ),   // Toprak 03 · sıradan oyuncu %0 -> %33 · kullanıcı ayarı 2026-10-01
        ( 39, 5, 5, 'N' ),   // Toprak 04 · sıradan oyuncu -> %62 · kullanıcı ayarı 2026-10-01 (Toprak Meydan'dan hafif kolay)
        ( 40, 5, 5, 'N' ),   // Toprak 05 · sıradan oyuncu %0 -> %33 · kullanıcı ayarı 2026-10-01
        ( 41, 6, 4, 'N' ),   // Toprak 06 · sıradan oyuncu %0 -> %28 · kullanıcı ayarı 2026-10-01
        ( 43, 5, 4, 'N' ),   // Toprak 08 · sıradan oyuncu -> %35 · kullanıcı ayarı 2026-10-01 (Toprak Meydan'dan hafif kolay)
        ( 44, 5, 5, 'N' ),   // Toprak 09 · sıradan oyuncu -> %48 · kullanıcı ayarı 2026-10-01 (Toprak ort. %45-52)
        ( 45, 4, 5, 'N' ),   // Toprak 10 · sıradan oyuncu %0 -> %32 · kullanıcı ayarı 2026-10-01
        ( 46, 5, 5, 'N' ),   // Toprak 11 · sıradan oyuncu -> %57 · kullanıcı ayarı 2026-10-01 (Toprak ort. %45-52)
        ( 47, 5, 7, 'U' ),   // Toprak 12 · sıradan oyuncu %0 -> %17
        ( 48, 4, 5, 'N' ),   // Meydan 01 · sıradan oyuncu %13 -> %40 · kullanıcı ayarı 2026-10-01
        ( 49, 4, 5, 'N' ),   // Meydan 02 · sıradan oyuncu %10 -> %32 · kullanıcı ayarı 2026-10-01
        ( 50, 5, 4, 'N' ),   // Meydan 03 · sıradan oyuncu %5 -> %23 · kullanıcı ayarı 2026-10-01
        ( 51, 5, 6, 'N' ),   // Meydan 04 · sıradan oyuncu %10 -> %45
        ( 52, 4, 4, 'N' ),   // Meydan 05 · sıradan oyuncu %0 -> %27 · kullanıcı ayarı 2026-10-01
        ( 53, 5, 4, 'N' ),   // Meydan 06 · sıradan oyuncu %5 -> %62
        ( 54, 6, 4, 'N' ),   // Meydan 07 · sıradan oyuncu %0 -> %23 · kullanıcı ayarı 2026-10-01
        ( 55, 4, 5, 'N' ),   // Meydan 08 · sıradan oyuncu %8 -> %45
        ( 57, 4, 5, 'N' ),   // Meydan 10 · sıradan oyuncu %15 -> %13 · kullanıcı ayarı 2026-10-01
        ( 58, 5, 4, 'N' ),   // Meydan 11 · sıradan oyuncu %0 -> %30 · kullanıcı ayarı 2026-10-01
        ( 59, 4, 3, 'U' ),   // Meydan 12 · sıradan oyuncu %2 -> %28
        ( 60, 2, 2, 'N' ),   // Sahil 01 · sıradan oyuncu %22 -> %73
        ( 62, 6, 4, 'N' ),   // Sahil 03 · sıradan oyuncu %37 -> %70
        ( 63, 3, 3, 'N' ),   // Sahil 04 · sıradan oyuncu %27 -> %78
        ( 68, 5, 4, 'N' ),   // Sahil 09 · sıradan oyuncu %20 -> %48
        ( 70, 4, 4, 'N' ),   // Sahil 11 · sıradan oyuncu %3 -> %38
        ( 71, 5, 3, 'U' ),   // Sahil 12 · sıradan oyuncu %0 -> %15
        ( 72, 3, 3, 'N' ),   // Köy 01 · sıradan oyuncu %28 -> %82
        ( 73, 4, 3, 'N' ),   // Köy 02 · sıradan oyuncu %25 -> %80
        ( 76, 4, 3, 'N' ),   // Köy 05 · sıradan oyuncu %13 -> %60
        ( 78, 4, 4, 'N' ),   // Köy 07 · sıradan oyuncu %32 -> %72
        ( 79, 4, 5, 'K' ),   // Köy 08 · sıradan oyuncu %27 -> %2
        ( 81, 4, 4, 'N' ),   // Köy 10 · sıradan oyuncu %68 -> %60
        ( 82, 5, 3, 'N' ),   // Köy 11 · sıradan oyuncu %0 -> %47
        ( 83, 7, 3, 'U' ),   // Köy 12 · sıradan oyuncu %0 -> %3
        ( 84, 2, 2, 'N' ),   // Yayla 01 · sıradan oyuncu %27 -> %85
        ( 86, 3, 3, 'N' ),   // Yayla 03 · sıradan oyuncu %25 -> %62
        ( 87, 4, 3, 'N' ),   // Yayla 04 · sıradan oyuncu %8 -> %40
        ( 88, 5, 5, 'N' ),   // Yayla 05 · sıradan oyuncu %85 -> %42
        ( 90, 4, 3, 'N' ),   // Yayla 07 · sıradan oyuncu %17 -> %58
        ( 92, 5, 3, 'N' ),   // Yayla 09 · sıradan oyuncu %7 -> %35
        ( 95, 7, 3, 'U' ),   // Yayla 12 · sıradan oyuncu %0 -> %10
        ( 97, 3, 3, 'N' ),   // Pazar 02 · sıradan oyuncu %70 -> %62
        ( 99, 7, 4, 'N' ),   // Pazar 04 · sıradan oyuncu %23 -> %57
        ( 101, 5, 4, 'N' ),   // Pazar 06 · sıradan oyuncu %75 -> %48
        ( 102, 3, 4, 'K' ),   // Pazar 07 · sıradan oyuncu %17 -> %3
        ( 103, 4, 3, 'N' ),   // Pazar 08 · sıradan oyuncu %20 -> %50
        ( 106, 4, 4, 'N' ),   // Pazar 11 · sıradan oyuncu %18 -> %33
        ( 107, 6, 4, 'U' ),   // Pazar 12 · sıradan oyuncu %3 -> %20
        ( 109, 4, 3, 'N' ),   // Bayram 02 · sıradan oyuncu %8 -> %35
        ( 110, 4, 4, 'N' ),   // Bayram 03 · sıradan oyuncu %18 -> %47
        ( 111, 5, 4, 'N' ),   // Bayram 04 · sıradan oyuncu %15 -> %45
        ( 113, 4, 4, 'N' ),   // Bayram 06 · sıradan oyuncu %15 -> %50
        ( 114, 4, 4, 'N' ),   // Bayram 07 · sıradan oyuncu %85 -> %72
        ( 117, 3, 2, 'N' ),   // Bayram 10 · sıradan oyuncu %3 -> %48
        ( 118, 7, 3, 'N' ),   // Bayram 11 · sıradan oyuncu %0 -> %23
        ( 119, 4, 3, 'U' ),   // Bayram 12 · sıradan oyuncu %0 -> %7
    };

    // Her bölgenin Kesem bölümü (global numara).
    public static readonly int[] KesemLevels = { 7, 20, 32, 42, 56, 66, 79, 89, 102, 115 };

    // Doğrulama için (DifficultyOrderVerify): satır sayısı ve satırın beklenen değerleri.
    public static int RowCount => Rows.Length;
    public static (int g, int shots, int pass, char role) Row(int i) => Rows[i];

    public static void Apply(LevelData level, int globalIndex)
    {
        if (level == null) return;
        foreach (var r in Rows)
        {
            if (r.g != globalIndex) continue;
            level.shotCount = r.shots;
            int total = level.TotalMarbles();
            if (level.starsToPass >= 2)
            {
                level.twoStarTarget = Mathf.Min(r.pass, total);
                level.oneStarTarget = Mathf.Min(level.oneStarTarget, level.twoStarTarget);
            }
            else
            {
                level.oneStarTarget = Mathf.Min(r.pass, total);
                level.twoStarTarget = Mathf.Max(level.twoStarTarget, level.oneStarTarget);
            }
            level.threeStarTarget = Mathf.Min(total, Mathf.Max(level.threeStarTarget, level.twoStarTarget));
            return;
        }
    }
}
