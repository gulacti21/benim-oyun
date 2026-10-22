using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

// HARİTA 3 çeşitlilik ayarı: her hazır Şehir bölümü için dizilimi bozmadan küçük bir
// döndürme + kaydırma arar; bütün eski bölümlerden ve önceki Şehir bölümlerinden en az
// 0.30 farklı olsun (MemleketLayoutVerify ölçüsü). Sonuç SEHIR_VARIETY satırları olarak
// log'a yazılır; SehirBook.Variation tablosuna elle geçirilir.
// Çalıştırma (editör açıkken): SehirVarietyTuner.Schedule()
public static class SehirVarietyTuner
{
    public static void Schedule() { EditorApplication.delayCall += Run; }

    public static void Run()
    {
        var all = new List<Vector2[]>();
        for (int g = 0; g < 2 * Maps.PerMap; g++)
        {
            var l = Maps.Get(g);
            if (l.marbles == null || l.marbles.Length < 6) continue;
            all.Add(Points(l, Vector3.zero));
        }
        var chosen = new List<Vector2[]>();
        var sb = new StringBuilder();
        int first = Maps.First(2);
        for (int i = 0; i < SehirCampaign.Ready; i++)
        {
            var saved = SehirBook.VariationOverride;
            SehirBook.VariationOverride = _ => Vector3.zero;
            SehirCampaign.ResetCache();
            var l = Maps.Get(first + i);
            SehirBook.VariationOverride = saved;
            SehirCampaign.ResetCache();
            if (l.marbles == null) continue;
            float bestScore = float.MinValue, bestMin = 0f; Vector3 best = Vector3.zero; Vector2[] bestArr = null;
            foreach (float rot in new[] { 0f, -10f, 10f, -20f, 20f, 180f })
                for (float dx = -.6f; dx <= .61f; dx += .15f)
                    for (float dz = -.6f; dz <= .61f; dz += .15f)
                    {
                        var v = new Vector3(rot, dx, dz);
                        var arr = Points(l, v);
                        bool inside = true;
                        foreach (var p in arr) if (p.magnitude > l.arenaSize - .4f) inside = false;
                        if (!inside) continue;
                        float mn = 9f;
                        foreach (var o in all) mn = Mathf.Min(mn, LevelVarietyVerify.Similarity(arr, o));
                        foreach (var o in chosen) mn = Mathf.Min(mn, LevelVarietyVerify.Similarity(arr, o));
                        // En küçük hareketle .34'ü geç; geçemiyorsa en farklıyı al.
                        float cost = (rot == 180f ? .7f : Mathf.Abs(rot) / 60f) + Mathf.Abs(dx) + Mathf.Abs(dz);
                        float score = Mathf.Min(mn, .34f) * 10f - cost * .3f;
                        if (score > bestScore) { bestScore = score; bestMin = mn; best = v; bestArr = arr; }
                    }
            if (bestArr != null) chosen.Add(bestArr);
            sb.AppendLine("SEHIR_VARIETY " + i + " " + l.levelName + " -> { " + i + ", new Vector3(" + best.x.ToString("F0") + "f, " +
                          best.y.ToString("F2") + "f, " + best.z.ToString("F2") + "f) }  min " + bestMin.ToString("F2"));
        }
        Debug.Log(sb.ToString() + "SEHIR_VARIETY_DONE");
    }

    // Tek bölüm: diğer bütün bölümler OLDUĞU GİBİ (tablodaki kaydırmalarıyla) sabit kalır,
    // sadece verilen bölüm için kaydırma aranır. Batchmode: -executeMethod SehirVarietyTuner.RunOneCli -sehirIndex 16
    public static void RunOneCli()
    {
        var args = System.Environment.GetCommandLineArgs(); int idx = -1;
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-sehirIndex") idx = int.Parse(args[i + 1]);
        if (idx >= 0) RunOne(idx);
    }

    public static void RunOne(int index)
    {
        int target = Maps.First(2) + index;
        var others = new List<Vector2[]>();
        for (int g = 0; g < Maps.TotalLevels; g++)
        {
            if (g == target || !Maps.Ready(g)) continue;
            var o = Maps.Get(g);
            if (o.marbles == null || o.marbles.Length < 6) continue;
            others.Add(Points(o, Vector3.zero));
        }
        var saved = SehirBook.VariationOverride;
        SehirBook.VariationOverride = _ => Vector3.zero;
        SehirCampaign.ResetCache();
        var l = Maps.Get(target);
        SehirBook.VariationOverride = saved;
        SehirCampaign.ResetCache();
        float bestScore = float.MinValue, bestMin = 0f; Vector3 best = Vector3.zero;
        foreach (float rot in new[] { 0f, -10f, 10f, -20f, 20f, -30f, 30f, 180f, 170f, 190f })
            for (float dx = -.6f; dx <= .61f; dx += .15f)
                for (float dz = -.6f; dz <= .61f; dz += .15f)
                {
                    var v = new Vector3(rot, dx, dz);
                    var arr = Points(l, v);
                    bool inside = true;
                    foreach (var p in arr) if (p.magnitude > l.arenaSize - .4f) inside = false;
                    if (!inside) continue;
                    float mn = 9f;
                    foreach (var o in others) mn = Mathf.Min(mn, LevelVarietyVerify.Similarity(arr, o));
                    float cost = (rot >= 170f ? .7f : Mathf.Abs(rot) / 60f) + Mathf.Abs(dx) + Mathf.Abs(dz);
                    float score = Mathf.Min(mn, .34f) * 10f - cost * .3f;
                    if (score > bestScore) { bestScore = score; bestMin = mn; best = v; }
                }
        Debug.Log("SEHIR_VARIETY_ONE " + index + " " + l.levelName + " -> new Vector3(" + best.x.ToString("F0") + "f, " +
                  best.y.ToString("F2") + "f, " + best.z.ToString("F2") + "f) min " + bestMin.ToString("F2"));
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
