using System.Collections.Generic;
using UnityEngine;

// SONSUZ ÇEMBER
// Kampanyadan farklı bir oyun: SÜRE yarışı. Atış hakkı sınırsız, süre sınırlı.
// Çıkardığın her misket süre kazandırır. Saha hiç boşalmaz: misket sayısı azalınca
// yeni misketler farklı yerlere dizilir. Zaman ilerledikçe kademe atlanır:
// engeller gelir ve misketler çemberin ortasına, yani çıkarması zor yerlere dizilir.
public static class EndlessLevel
{
    public const float StartTime = 45f;      // başlangıç süresi (sn)
    public const float TimePerMarble = 2.5f; // çıkan her misket bu kadar süre kazandırır
    public const float StageEvery = 30f;     // bu kadar saniyede bir kademe atlar
    public const int MaxStage = 6;
    public const float ArenaSize = 3.2f;
    public const int BoardCount = 10;        // sahada hedeflenen misket sayısı
    public const int RefillBelow = 3;        // bu sayının altına düşünce dolum yapılır
    public const int District = 3;           // zemin sabit: Toprak Saha (kademede değişmez)
    public const float SettleDelay = .15f;   // atış sonrası bekleme (kampanyada .4)
    public const float SettleCutoff = 2.5f;  // bu kadar saniye sonra yavaş misketler durdurulur

    public static int StageAt(float elapsed) => Mathf.Clamp(1 + (int)(elapsed / StageEvery), 1, MaxStage);

    // Kademeye göre engel sayısı: 1-2 kademe engelsiz, sonra birer birer artar.
    public static int WallsFor(int stage) => Mathf.Clamp(stage - 2, 0, 3);

    public static LevelData Build(int stage)
    {
        stage = Mathf.Clamp(stage, 1, MaxStage);
        var l = ScriptableObject.CreateInstance<LevelData>();
        l.hideFlags = HideFlags.HideAndDontSave;
        l.name = "Sonsuz";
        l.levelName = "Sonsuz Çember";
        l.district = District;                     // zemin hep aynı: renk atlaması olmaz
        l.shape = ArenaShape.Circle;
        l.arenaSize = ArenaSize;
        l.shotCount = 9999;                        // süre sınırlı, atış sınırsız
        l.starsToPass = 1;
        l.oneStarTarget = l.twoStarTarget = l.threeStarTarget = BoardCount;
        l.marbles = Spots(stage, BoardCount, null, 1234 + stage * 77);
        l.obstacles = Walls(stage);
        l.obstacleCount = l.obstacles == null ? 0 : l.obstacles.Length;
        l.shooterHalfWidth = 0f; l.shooterOffsetX = 0f;
        l.shooterStartPosition = new Vector3(0f, .25f, -4.2f);
        return l;
    }

    private static ObstacleSpot[] Walls(int stage)
    {
        int walls = WallsFor(stage);
        if (walls <= 0) return null;
        var list = new ObstacleSpot[walls];
        for (int i = 0; i < walls; i++)
        {
            float a = .7f + i * Mathf.PI * 2f / walls;
            float r = ArenaSize * .68f;
            list[i] = new ObstacleSpot(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 1.2f, .4f, a * Mathf.Rad2Deg);
        }
        return list;
    }

    // Yeni misket yerleri. Kademe arttıkça misketler merkeze yaklaşır: kenardaki
    // misket bir dokunuşla çıkar, ortadaki misketi çıkarmak birkaç atış ister.
    public static MarbleSpot[] Spots(int stage, int count, IList<Vector2> busy, int seed)
    {
        var rng = new System.Random(seed);
        float inner = ArenaSize - .55f;
        float pull = Mathf.Lerp(1f, .55f, (stage - 1) / (float)(MaxStage - 1)); // dışarıdan içeriye
        var walls = Walls(stage);
        var list = new List<MarbleSpot>();
        var taken = new List<Vector2>();
        if (busy != null) taken.AddRange(busy);

        int guard = 0;
        while (list.Count < count && guard++ < 3000)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2f;
            float r = inner * pull * Mathf.Sqrt((float)rng.NextDouble());
            var p = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            if (p.magnitude > inner) continue;

            bool clash = false;
            foreach (var q in taken) if ((q - p).sqrMagnitude < .62f * .62f) { clash = true; break; }
            if (!clash && walls != null)
                foreach (var w in walls) if ((new Vector2(w.x, w.z) - p).sqrMagnitude < .85f * .85f) { clash = true; break; }
            if (clash) continue;

            taken.Add(p);
            list.Add(new MarbleSpot((float)System.Math.Round(p.x, 3), (float)System.Math.Round(p.y, 3)));
        }
        return list.ToArray();
    }
}
