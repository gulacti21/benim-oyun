using UnityEngine;

// SONSUZ ÇEMBER
// Kampanya bitince oynanacak bir şey kalsın diye: saha boşaldıkça yenisi
// diziliyor, her tur biraz daha zor. Çıkardığın her misket bir atış hakkı
// kazandırıyor; hak bitince oyun biter. Tek ölçü: toplam çıkan misket.
// Dizilim tura göre üretilir, yani herkeste aynı sırayla gelir.
public static class EndlessLevel
{
    public const int StartShots = 5;

    public static LevelData Build(int wave)
    {
        wave = Mathf.Max(1, wave);
        var rng = new System.Random(9000 + wave * 7919);
        float size = Mathf.Max(2.7f, 3.4f - (wave - 1) * .04f);
        int count = Mathf.Clamp(8 + (wave - 1) / 2, 8, 14);

        var l = ScriptableObject.CreateInstance<LevelData>();
        l.hideFlags = HideFlags.HideAndDontSave;
        l.name = "Sonsuz";
        l.levelName = "Sonsuz Çember";
        l.district = (wave - 1) % 5;          // her turda başka mahalle zemini
        l.shape = ArenaShape.Circle;
        l.arenaSize = size;
        l.shotCount = StartShots;
        l.starsToPass = 1;
        l.oneStarTarget = l.twoStarTarget = l.threeStarTarget = count;

        // Misketler: iç halka + dış halka, tura göre dönerek.
        var spots = new System.Collections.Generic.List<MarbleSpot>();
        int inner = Mathf.Min(4, count / 3);
        float phase = (float)rng.NextDouble() * Mathf.PI * 2f;
        for (int i = 0; i < inner; i++)
        {
            float a = phase + i * Mathf.PI * 2f / Mathf.Max(1, inner);
            spots.Add(new MarbleSpot(Mathf.Cos(a) * size * .22f, Mathf.Sin(a) * size * .22f));
        }
        int outer = count - inner;
        for (int i = 0; i < outer; i++)
        {
            float a = -phase + i * Mathf.PI * 2f / Mathf.Max(1, outer);
            float r = size * (.52f + (float)rng.NextDouble() * .1f);
            spots.Add(new MarbleSpot(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }
        l.marbles = spots.ToArray();

        // Engeller üçüncü turdan sonra birer birer gelir (en fazla 3).
        int walls = Mathf.Clamp((wave - 1) / 3, 0, 3);
        var obs = new ObstacleSpot[walls];
        for (int i = 0; i < walls; i++)
        {
            float a = phase * 1.7f + i * Mathf.PI * 2f / Mathf.Max(1, walls);
            float r = size * .72f;
            obs[i] = new ObstacleSpot(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 1.2f, .4f, a * Mathf.Rad2Deg);
        }
        l.obstacles = walls > 0 ? obs : null;
        l.obstacleCount = walls;
        l.shooterHalfWidth = 0f; l.shooterOffsetX = 0f;
        l.shooterStartPosition = new Vector3(0f, .25f, -4.2f);
        return l;
    }
}
