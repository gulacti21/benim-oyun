using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Memleket bölümlerini, iki haritadaki bütün bölümlerden en az 0.30 farklı olacak
// şekilde EN KÜÇÜK dokunuşla döndürür/kaydırır. Sonucu MemleketBook.Variation
// tablosuna yapıştırılacak satırlar olarak Logs/MemleketVariety.txt'ye yazar.
// Oyun sırasında çalışmaz; tablo sabittir.
public static class MemleketVarietyTuner
{
    private const float Want = .32f;   // 0.30 sınırının biraz üstü (yuvarlama payı)

    public static void Run()
    {
        var h1 = new List<Vector2[]>();
        for (int g = 0; g < Campaign.Count; g++)
        {
            var l = Campaign.Database.Get(g);
            if (l.marbles != null && l.marbles.Length >= 6) h1.Add(Points(l));
        }
        var chosen = new Vector3[MemleketCampaign.Count];
        var pts = new Vector2[MemleketCampaign.Count][];
        MemleketBook.VariationOverride = idx => chosen[idx];
        for (int i = 0; i < pts.Length; i++) pts[i] = Points(Build(i));

        var cands = new List<Vector3>();
        foreach (float r in new[] { 0f, 8f, -8f, 15f, -15f, 22f, -22f, 30f, -30f, 40f, -40f })
            foreach (float dx in new[] { 0f, .25f, -.25f, .45f, -.45f })
                foreach (float dz in new[] { 0f, .25f, -.25f, .45f, -.45f })
                    cands.Add(new Vector3(r, dx, dz));
        cands.Sort((a, b) => Cost(a).CompareTo(Cost(b)));

        var log = new List<string>();
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < pts.Length; i++)
            {
                if (pts[i].Length < 6) continue;
                Vector3 best = chosen[i]; float bestMin = -1f;
                foreach (var c in cands)
                {
                    var keep = chosen[i]; chosen[i] = c;
                    var l = Build(i);
                    chosen[i] = keep;
                    if (!Valid(l)) continue;
                    var p = Points(l);
                    float m = MinSim(p, i, h1, pts);
                    if (m > bestMin) { bestMin = m; best = c; }
                    if (m >= Want) break;
                }
                chosen[i] = best;
                pts[i] = Points(Build(i));
                if (pass == 1) log.Add("i=" + i + " min=" + bestMin.ToString("F2") + (bestMin < .30f ? "  <-- BULUNAMADI" : ""));
            }

        var table = new List<string>();
        for (int i = 0; i < chosen.Length; i++)
            if (chosen[i] != Vector3.zero)
                table.Add("        { " + i + ", new Vector3(" + F(chosen[i].x) + ", " + F(chosen[i].y) + ", " + F(chosen[i].z) + ") },");
        MemleketBook.VariationOverride = null;
        var output = new List<string> { "// MemleketBook.Variation" };
        output.AddRange(table); output.Add(""); output.AddRange(log);
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/MemleketVariety.txt", output);
        Debug.Log("MEMLEKET_VARIETY_TUNED: " + table.Count + " bölüm dokunuldu. Logs/MemleketVariety.txt");
    }

    private static string F(float v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "f";
    private static float Cost(Vector3 c) => Mathf.Abs(c.x) / 15f + Mathf.Abs(c.y) / .25f + Mathf.Abs(c.z) / .25f;

    private static LevelData Build(int i)
    {
        var l = ScriptableObject.CreateInstance<LevelData>();
        l.hideFlags = HideFlags.HideAndDontSave;
        MemleketBook.Apply(l, i);
        return l;
    }

    private static Vector2[] Points(LevelData l)
    {
        var a = new Vector2[l.marbles.Length];
        for (int k = 0; k < a.Length; k++) a[k] = new Vector2(l.marbles[k].x, l.marbles[k].z);
        return a;
    }

    private static float MinSim(Vector2[] p, int self, List<Vector2[]> h1, Vector2[][] h2)
    {
        float m = float.MaxValue;
        foreach (var q in h1) m = Mathf.Min(m, LevelVarietyVerify.Similarity(p, q));
        for (int j = 0; j < h2.Length; j++)
            if (j != self && h2[j].Length >= 6) m = Mathf.Min(m, LevelVarietyVerify.Similarity(p, h2[j]));
        return m;
    }

    private static bool Valid(LevelData l)
    {
        foreach (var s in l.marbles) if (new Vector2(s.x, s.z).magnitude > l.arenaSize - .35f) return false;
        if (l.obstacles != null) foreach (var o in l.obstacles) if (new Vector2(o.x, o.z).magnitude > l.arenaSize) return false;
        if (l.zones != null)
            foreach (var z in l.zones)
            {
                if (new Vector2(z.x, z.z).magnitude + z.radius > l.arenaSize + .35f) return false;
                if (z.z - z.radius <= -l.arenaSize - .1f) return false;
            }
        return true;
    }
}
