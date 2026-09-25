using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// GÜNÜN BÖLÜMÜ HAVUZU ÜRETİCİSİ
// Tohumlu rastgele dizilimler üretir, her birini ParkPhysicsVerify'ın normal
// misketli açgözlü aramasıyla ölçer (tavan), sadece dengeli olanları alır ve
// hedefleri ÖLÇÜLEN tavandan koyar. Çıktı: Resources/Mahalle/Daily/DailyPool.json
//   BatchSample: 6 aday, havuza yazmaz, süreyi görmek için (Logs/DailySample.json)
//   Batch      : 200 aday, havuzu yazar (Unity birkaç saat kapalı kalmalı)
public static class DailyLevelGenerator
{
    private const int Seed = 20260917;
    private const string PoolPath = "Assets/Resources/Mahalle/Daily/DailyPool.json";

    [MenuItem("MISKETR/Daily/Generate Sample (6)")]
    public static void BatchSample() { Generate(6, false); }
    [MenuItem("MISKETR/Daily/Generate Pool")]
    public static void Batch() { Generate(200, true); }

    private static void Generate(int count, bool writePool)
    {
        var started = DateTime.Now;
        var rng = new System.Random(Seed);
        var cands = new List<LevelData>();
        var entries = new List<DailyEntry>();
        for (int i = 0; i < count; i++)
        {
            var e = MakeEntry(rng, i);
            entries.Add(e);
            cands.Add(DailyLevel.Build(e));
        }

        var idx = new int[count];
        for (int i = 0; i < count; i++) idx[i] = i;
        ParkPhysicsVerify.LevelOverride = k => cands[k];
        try { ParkPhysicsVerify.MeasureNormalNow(idx, writePool ? "DailyPoolMeasure.txt" : "DailySampleMeasure.txt"); }
        finally { ParkPhysicsVerify.LevelOverride = null; }

        var ceilings = new Dictionary<int, int>();
        foreach (var row in ParkPhysicsVerify.Ledger) ceilings[row[0]] = row[1];

        var accepted = new List<DailyEntry>();
        var log = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            var e = entries[i];
            int n = e.marbles.Length / 2;
            int c = ceilings.TryGetValue(i, out var v) ? v : -1;
            float ratio = c / (float)n;
            bool ok = c >= 4 && ratio >= .45f && ratio <= .95f;
            log.AppendLine("#" + i + " mahalle " + e.district + " r=" + e.size.ToString("F2") + " misket " + n +
                           " engel " + (e.obstacles?.Length ?? 0) + " atis " + e.shots + " tavan " + c +
                           " (%" + Mathf.RoundToInt(ratio * 100) + ") " + (ok ? "ALINDI" : "ELENDI"));
            if (!ok) continue;
            e.ceiling = c;
            e.three = c;
            e.two = Mathf.Max(2, c - 1);
            e.one = Mathf.Clamp(Mathf.RoundToInt(c * .55f), 2, e.two);
            accepted.Add(e);
        }

        // Aynı mahalle temaları art arda gelmesin: sırayı karıştır.
        var shuffle = new System.Random(Seed + 1);
        for (int i = accepted.Count - 1; i > 0; i--) { int j = shuffle.Next(i + 1); var t = accepted[i]; accepted[i] = accepted[j]; accepted[j] = t; }

        var pool = new DailyPool { entries = accepted.ToArray() };
        string json = JsonUtility.ToJson(pool, true);
        Directory.CreateDirectory("Logs");
        if (writePool)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PoolPath));
            File.WriteAllText(PoolPath, json);
            AssetDatabase.ImportAsset(PoolPath);
        }
        else File.WriteAllText("Logs/DailySample.json", json);
        File.WriteAllText(writePool ? "Logs/DailyPoolReport.txt" : "Logs/DailySampleReport.txt", log.ToString());
        Debug.Log(log.ToString());
        Debug.Log("DAILY_DONE: " + accepted.Count + "/" + count + " alindi, sure " +
                  (DateTime.Now - started).TotalMinutes.ToString("F1") + " dk" + (writePool ? " -> " + PoolPath : " (ornek)"));
    }

    private static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    private static DailyEntry MakeEntry(System.Random r, int i)
    {
        var e = new DailyEntry();
        e.district = i % 5;
        e.size = R(r, 2.9f, 3.5f);
        int n = r.Next(7, 13);
        var spots = new List<Vector2>();
        int pattern = r.Next(5);
        float inner = e.size - .55f;
        int guard = 0;
        while (spots.Count < n && guard++ < 4000)
        {
            Vector2 p;
            switch (pattern)
            {
                case 0: // tek küme
                {
                    var c = new Vector2(R(r, -.8f, .8f), R(r, -.4f, 1.1f));
                    p = c + new Vector2(R(r, -1.1f, 1.1f), R(r, -1.1f, 1.1f)) * .9f;
                    break;
                }
                case 1: // halka
                {
                    float rad = e.size * R(r, .45f, .62f);
                    float a = (spots.Count / (float)n) * Mathf.PI * 2f + R(r, -.12f, .12f);
                    p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad;
                    break;
                }
                case 2: // iki küme
                {
                    var c = spots.Count % 2 == 0 ? new Vector2(-1.05f, .5f) : new Vector2(1.05f, .6f);
                    p = c + new Vector2(R(r, -.7f, .7f), R(r, -.7f, .7f));
                    break;
                }
                case 3: // iki sıra
                {
                    float z = spots.Count % 2 == 0 ? R(r, -.2f, .2f) : R(r, 1f, 1.4f);
                    p = new Vector2(R(r, -1.8f, 1.8f), z);
                    break;
                }
                default: // dağınık
                    p = new Vector2(R(r, -inner, inner), R(r, -inner, inner));
                    break;
            }
            if (p.magnitude > inner) continue;
            bool clash = false;
            foreach (var q in spots) if ((q - p).sqrMagnitude < .6f * .6f) { clash = true; break; }
            if (!clash) spots.Add(p);
        }
        e.marbles = new float[spots.Count * 2];
        for (int k = 0; k < spots.Count; k++) { e.marbles[k * 2] = (float)Math.Round(spots[k].x, 3); e.marbles[k * 2 + 1] = (float)Math.Round(spots[k].y, 3); }

        int walls = e.district <= 1 ? r.Next(1, 3) : r.Next(1, 4);
        var obs = new List<ObstacleSpot>();
        guard = 0;
        while (obs.Count < walls && guard++ < 400)
        {
            float a = R(r, 0f, Mathf.PI * 2f), rad = R(r, .9f, e.size - .6f);
            var c = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad;
            bool clash = false;
            foreach (var q in spots) if ((q - c).sqrMagnitude < 1.0f) { clash = true; break; }
            foreach (var o in obs) if ((new Vector2(o.x, o.z) - c).sqrMagnitude < 1.8f) clash = true;
            if (clash) continue;
            obs.Add(new ObstacleSpot((float)Math.Round(c.x, 2), (float)Math.Round(c.y, 2),
                                     (float)Math.Round(R(r, 1f, 1.6f), 2), (float)Math.Round(R(r, .35f, .45f), 2),
                                     (float)Math.Round(R(r, -45f, 45f), 1)));
        }
        e.obstacles = obs.ToArray();
        e.shots = Mathf.Clamp(Mathf.RoundToInt(spots.Count * .4f), 3, 5);
        e.one = 1; e.two = 1; e.three = 1; e.ceiling = spots.Count;
        return e;
    }
}
