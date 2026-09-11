using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// HICBIR MAP BIRBIRININ AYNISI OLMAYACAK.
//
// Olcu: iki bolumun misket kumeleri arasindaki "karsilikli en yakin komsu" mesafesi.
// A'nin her misketi icin B'deki en yakin miskete olan mesafe, ve tersi; ikisinin
// ortalamasi. Ayrica B'nin AYNASI ile de olculur, cunku aynalanmis bir yerlesim
// oyuncuya ayni bolum gibi gelir; kucuk olan sonuc alinir.
//
// Buyuk sayi = birbirinden uzak = iyi. 0.30 altini "neredeyse ayni" sayiyoruz.
// Istisna: 6 misketten az olan ogretme bolumleri. O kadar az misketle gercekten
// farkli bir sekil cikmiyor, onlari zorlamak tasarimi bozar.
public static class LevelVarietyVerify
{
    private const float Limit = .30f;
    private const int SmallLevel = 6;   // bu kadar az misketli bolumler muaf

    [MenuItem("MISKETR/Verify Level Variety")]
    public static void Run()
    {
        var pts = new List<Vector2[]>();
        var ids = new List<int>();
        for (int i = 0; i < Campaign.Count; i++)
        {
            var lvl = Campaign.Database.Get(i);
            if (lvl == null || lvl.marbles == null || lvl.marbles.Length == 0) continue;
            var arr = new Vector2[lvl.marbles.Length];
            for (int k = 0; k < arr.Length; k++) arr[k] = new Vector2(lvl.marbles[k].x, lvl.marbles[k].z);
            pts.Add(arr); ids.Add(i);
        }

        var report = new List<string>();
        var scores = new List<float>();
        var close = new List<string>();
        int muaf = 0;

        for (int a = 0; a < pts.Count; a++)
            for (int b = a + 1; b < pts.Count; b++)
            {
                float s = Similarity(pts[a], pts[b]);
                scores.Add(s);
                if (s >= Limit) continue;
                if (pts[a].Length < SmallLevel || pts[b].Length < SmallLevel) { muaf++; continue; }
                close.Add(s.ToString("F2") + "  " + Label(ids[a]) + " <-> " + Label(ids[b]) +
                          (ids[a] / 12 == ids[b] / 12 ? "   (ayni mahalle)" : ""));
            }

        scores.Sort();
        float median = scores.Count == 0 ? 0f : scores[scores.Count / 2];
        report.Add("BOLUM CESITLILIGI · " + pts.Count + " bolum, " + scores.Count + " cift");
        report.Add("ortanca benzerlik: " + median.ToString("F2") + "   (buyuk = daha farkli)");
        report.Add("sinir " + Limit.ToString("F2") + " altinda: " + close.Count + " cift  (+ " + muaf + " kucuk bolum muaf)");
        report.Add("");

        close.Sort();
        if (close.Count == 0)
        {
            report.Add("Hicbir map digerinin aynisi degil.");
            Debug.Log("VARIETY OK · ortanca " + median.ToString("F2") + ", sinir altinda cift yok.");
        }
        else
        {
            report.Add("FAZLA BENZER CIFTLER:");
            foreach (var c in close) { report.Add("  " + c); Debug.LogWarning("VARIETY: " + c); }
        }

        report.Add("");
        report.Add("HER BOLUMUN EN YAKINI:");
        for (int a = 0; a < pts.Count; a++)
        {
            float best = float.MaxValue; int who = -1;
            for (int b = 0; b < pts.Count; b++)
            {
                if (a == b) continue;
                float s = Similarity(pts[a], pts[b]);
                if (s < best) { best = s; who = ids[b]; }
            }
            report.Add("  " + Label(ids[a]).PadRight(10) + " en yakin " + Label(who).PadRight(10) + best.ToString("F2") +
                       (best < Limit ? "   <-- BAK" : ""));
        }

        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/LevelVarietyVerify.txt", report);
        Debug.Log("VARIETY_DONE: Logs/LevelVarietyVerify.txt");
    }

    private static float Similarity(Vector2[] a, Vector2[] b)
    {
        var mirror = new Vector2[b.Length];
        for (int i = 0; i < b.Length; i++) mirror[i] = new Vector2(-b[i].x, b[i].y);
        return Mathf.Min(Both(a, b), Both(a, mirror));
    }

    private static float Both(Vector2[] a, Vector2[] b) { return (Nearest(a, b) + Nearest(b, a)) * .5f; }

    private static float Nearest(Vector2[] a, Vector2[] b)
    {
        float sum = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            float best = float.MaxValue;
            for (int j = 0; j < b.Length; j++)
            {
                float d = (a[i] - b[j]).sqrMagnitude;
                if (d < best) best = d;
            }
            sum += Mathf.Sqrt(best);
        }
        return sum / a.Length;
    }

    private static string Label(int index)
    {
        string d = index < 12 ? "APT" : index < 24 ? "OKUL" : index < 36 ? "PARK" : index < 48 ? "TOPRAK" : "MEYDAN";
        return d + (index % 12 + 1).ToString("00");
    }
}
