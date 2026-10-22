using System.Collections.Generic;
using System.Text;
using UnityEngine;

// HARİTA 4 çeşitlilik ayarı (SehirVarietyTuner'ın Lunapark karşılığı): her hazır Lunapark bölümü için
// dizilimi bozmadan küçük bir döndürme + kaydırma arar; bütün oynanabilir bölümlerden ve önceki
// Lunapark bölümlerinden en az 0.30 farklı olsun (MemleketLayoutVerify ölçüsü). Sonuç
// LUNAPARK_VARIETY satırları olarak log'a yazılır; LunaparkBook.Variation tablosuna elle geçirilir.
// Tampon ve bant da birlikte döner; tamponun ya da bandın sahadan taşmaması kontrol edilir.
public static class LunaparkVarietyTuner
{
    public static string Run()
    {
        int first = Maps.First(LunaparkCampaign.MapIndex);
        var others = new List<Vector2[]>();
        for (int g = 0; g < first; g++)
        {
            if (!Maps.Ready(g)) continue;
            var l = Maps.Get(g);
            if (l.marbles == null || l.marbles.Length < 6) continue;
            others.Add(Points(l, Vector3.zero));
        }
        var chosen = new List<Vector2[]>();
        var sb = new StringBuilder();
        for (int i = 0; i < LunaparkCampaign.Ready; i++)
        {
            var saved = LunaparkBook.VariationOverride;
            LunaparkBook.VariationOverride = _ => Vector3.zero;
            LunaparkCampaign.ResetCache();
            var l = Maps.Get(first + i);
            LunaparkBook.VariationOverride = saved;
            LunaparkCampaign.ResetCache();
            if (l.marbles == null) continue;
            float bestScore = float.MinValue, bestMin = 0f; Vector3 best = Vector3.zero; Vector2[] bestArr = null;
            foreach (float rot in new[] { 0f, -10f, 10f, -20f, 20f, -30f, 30f, 180f, 170f, 190f })
                for (float dx = -.6f; dx <= .61f; dx += .15f)
                    for (float dz = -.6f; dz <= .61f; dz += .15f)
                    {
                        var v = new Vector3(rot, dx, dz);
                        if (!Fits(l, v)) continue;
                        var arr = Points(l, v);
                        float mn = 9f;
                        foreach (var o in others) mn = Mathf.Min(mn, LevelVarietyVerify.Similarity(arr, o));
                        foreach (var o in chosen) mn = Mathf.Min(mn, LevelVarietyVerify.Similarity(arr, o));
                        float cost = (rot >= 170f ? .7f : Mathf.Abs(rot) / 60f) + Mathf.Abs(dx) + Mathf.Abs(dz);
                        float score = Mathf.Min(mn, .34f) * 10f - cost * .3f;
                        if (score > bestScore) { bestScore = score; bestMin = mn; best = v; bestArr = arr; }
                    }
            if (bestArr != null) chosen.Add(bestArr);
            sb.AppendLine("{ " + i + ", new Vector3(" + best.x.ToString("F0") + "f, " + best.y.ToString("F2") + "f, " + best.z.ToString("F2") + "f) },  // " + l.levelName + " min " + bestMin.ToString("F2"));
        }
        Debug.Log("LUNAPARK_VARIETY\n" + sb + "LUNAPARK_VARIETY_DONE");
        return sb.ToString();
    }

    // Sadece verilen bölümler (diğerleri tablodaki kaydırmalarıyla sabit). Batchmode:
    // -executeMethod LunaparkVarietyTuner.RunSomeCli -lunaIndex 1,3,6,9 → Logs/LunaVariety.txt
    public static void RunSomeCli()
    {
        var args = System.Environment.GetCommandLineArgs(); var idx = new List<int>();
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-lunaIndex") foreach (var p in args[i + 1].Split(',')) idx.Add(int.Parse(p));
        var sb = new StringBuilder();
        int first = Maps.First(LunaparkCampaign.MapIndex);
        foreach (int index in idx)
        {
            var others = new List<Vector2[]>();
            for (int g = 0; g < Maps.TotalLevels; g++)
            {
                if (g == first + index || !Maps.Ready(g)) continue;
                var o = Maps.Get(g);
                if (o.marbles == null || o.marbles.Length < 6) continue;
                others.Add(Points(o, Vector3.zero));
            }
            var saved = LunaparkBook.VariationOverride;
            LunaparkBook.VariationOverride = k => k == index ? Vector3.zero : (saved != null ? saved(k) : LunaparkBook.CurrentVariation(k));
            LunaparkCampaign.ResetCache();
            var l = Maps.Get(first + index);
            LunaparkBook.VariationOverride = saved;
            LunaparkCampaign.ResetCache();
            float bestScore = float.MinValue, bestMin = 0f; Vector3 best = Vector3.zero;
            foreach (float rot in new[] { 0f, -10f, 10f, -20f, 20f, -30f, 30f, 180f, 170f, 190f })
                for (float dx = -.6f; dx <= .61f; dx += .15f)
                    for (float dz = -.6f; dz <= .61f; dz += .15f)
                    {
                        var v = new Vector3(rot, dx, dz);
                        if (!Fits(l, v)) continue;
                        var arr = Points(l, v);
                        float mn = 9f;
                        foreach (var o in others) mn = Mathf.Min(mn, LevelVarietyVerify.Similarity(arr, o));
                        float cost = (rot >= 170f ? .7f : Mathf.Abs(rot) / 60f) + Mathf.Abs(dx) + Mathf.Abs(dz);
                        float score = Mathf.Min(mn, .34f) * 10f - cost * .3f;
                        if (score > bestScore) { bestScore = score; bestMin = mn; best = v; }
                    }
            sb.AppendLine("{ " + index + ", new Vector3(" + best.x.ToString("F0") + "f, " + best.y.ToString("F2") + "f, " + best.z.ToString("F2") + "f) },  // " + l.levelName + " min " + bestMin.ToString("F2"));
        }
        System.IO.Directory.CreateDirectory("Logs");
        System.IO.File.WriteAllText("Logs/LunaVariety.txt", sb.ToString());
        Debug.Log("LUNAPARK_VARIETY_SOME\n" + sb);
    }

    // Misketler halkanın içinde, tampon/bant sahada ve atıcı çizgisinin üstünde kalmalı.
    private static bool Fits(LevelData l, Vector3 v)
    {
        var rot = Quaternion.Euler(0f, -v.x, 0f);
        Vector2 Move(float x, float z) { var p = rot * new Vector3(x, 0f, z); return new Vector2(p.x + v.y, p.z + v.z); }
        foreach (var m in l.marbles) if (Move(m.x, m.z).magnitude > l.arenaSize - .4f) return false;
        if (l.obstacles != null) foreach (var o in l.obstacles) if (Move(o.x, o.z).magnitude > l.arenaSize - .35f) return false;
        if (l.zones != null)
            foreach (var z in l.zones)
            {
                var p = Move(z.x, z.z);
                if (p.magnitude + z.radius > l.arenaSize + .3f || p.y - z.radius < -l.arenaSize) return false;
            }
        return true;
    }

    private static Vector2[] Points(LevelData l, Vector3 v)
    {
        var rot = Quaternion.Euler(0f, -v.x, 0f);
        var arr = new Vector2[l.marbles.Length];
        for (int k = 0; k < arr.Length; k++)
        {
            var p = rot * new Vector3(l.marbles[k].x, 0f, l.marbles[k].z);
            arr[k] = new Vector2(p.x + v.y, p.z + v.z);
        }
        return arr;
    }
}
