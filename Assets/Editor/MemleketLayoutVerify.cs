using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// HARİTA 2 (Memleket) bölüm yerleşimi: statik kurallar. MahalleVerify.Run içinden.
//   - misketler sahanın içinde, üst üste değil, engelin/çukurun içinde değil
//   - bölgeler sahada, çukurlar birbirine ve misketlere binmiyor
//   - her bölge kendi kuralını taşıyor, özel misketler kendi bölgesinden önce yok
//   - çeşitlilik: 6+ misketli her Memleket bölümü, her iki haritadaki bütün
//     bölümlerden en az 0.30 farklı (LevelVarietyVerify ölçüsüyle)
public static class MemleketLayoutVerify
{
    private static int checks;
    private static readonly List<string> problems = new List<string>();
    private static void Check(bool ok, string message) { checks++; if (!ok) problems.Add(message); }

    public static void Run() { try { RunChecks(); } catch (Exception e) { Debug.LogError(e.Message); } }

    public static int RunChecks()
    {
        checks = 0; problems.Clear();
        var db = MemleketCampaign.Database;
        var report = new StringBuilder();
        for (int i = 0; i < MemleketCampaign.Count; i++)
        {
            var l = db.Get(i);
            string n = "M" + (i / 12 + 5) + "-" + (i % 12 + 1).ToString("00") + " " + l.levelName;
            int region = i / 12;
            Check(l.marbles != null && l.marbles.Length >= 5, n + ": hand placed marbles");
            if (l.marbles == null) continue;
            Check(l.shape == ArenaShape.Circle && l.arenaSize >= 3f && l.arenaSize <= 4f, n + ": circle 3-4");
            Check(l.shotCount >= 3 && l.shotCount <= 6, n + ": 3-6 shots");
            for (int a = 0; a < l.marbles.Length; a++)
            {
                var p = new Vector2(l.marbles[a].x, l.marbles[a].z);
                Check(p.magnitude <= l.arenaSize - .35f, n + ": marble " + a + " inside ring (" + p.magnitude.ToString("F2") + "/" + l.arenaSize + ")");
                for (int b = a + 1; b < l.marbles.Length; b++)
                {
                    float d = Vector2.Distance(p, new Vector2(l.marbles[b].x, l.marbles[b].z));
                    Check(d >= .52f, n + ": marbles " + a + "/" + b + " apart (" + d.ToString("F2") + ")");
                }
                if (l.obstacles != null)
                    foreach (var o in l.obstacles) Check(!InObstacle(o, p, .27f), n + ": marble " + a + " clear of obstacle");
                if (l.zones != null)
                    foreach (var z in l.zones)
                        if (z.kind == ZoneKind.Pit) Check(Vector2.Distance(p, new Vector2(z.x, z.z)) >= z.radius + .05f, n + ": marble " + a + " not in a pit");
            }
            if (l.zones != null)
                for (int a = 0; a < l.zones.Length; a++)
                {
                    var z = l.zones[a];
                    Check(new Vector2(z.x, z.z).magnitude + z.radius <= l.arenaSize + .35f, n + ": zone " + a + " on the field");
                    Check(z.z - z.radius > -l.arenaSize - .1f, n + ": zone " + a + " not under the shooter line");
                }
            if (l.obstacles != null)
                foreach (var o in l.obstacles) Check(new Vector2(o.x, o.z).magnitude <= l.arenaSize, n + ": obstacle on the field");

            // Bölge kuralı
            bool sand = Has(l, ZoneKind.Sand), mud = Has(l, ZoneKind.Mud), pit = Has(l, ZoneKind.Pit);
            bool ice = HasKind(l, MarbleKind.Ice), split = HasKind(l, MarbleKind.Split);
            bool slope = l.slope.sqrMagnitude > 1e-6f, walls = l.obstacles != null && l.obstacles.Length > 0;
            switch (region)
            {
                case 0: Check(sand, n + ": Sahil has sand"); break;
                case 1: Check(mud, n + ": Köy has mud"); break;
                case 2: Check(slope || ice, n + ": Yayla has slope or ice"); break;
                case 3: Check(walls || split, n + ": Pazar has stalls or watermelon"); break;
                case 4: Check(pit, n + ": Bayram has a pit"); break;
            }
            if (region < 1) Check(!mud, n + ": no mud before Köy");
            if (region < 2) Check(!ice && !slope, n + ": no ice/slope before Yayla");
            if (region < 3) Check(!split, n + ": no watermelon before Pazar");
            if (region < 4) Check(!pit, n + ": no pit before Bayram");
            // Öğretme bölümleri: yeni kural tek başına.
            if (i == 0 || i == 1) Check(!walls && sand, n + ": teaches sand alone");
            if (i == 12) Check(mud && !walls && !sand, n + ": teaches mud alone");
            if (i == 24 || i == 25) Check(slope && !ice && l.zones == null, n + ": teaches slope alone");
            if (i == 26) Check(ice && !slope && l.zones == null && !walls, n + ": teaches ice alone");
            if (i == 37) Check(split && !walls && l.zones == null, n + ": teaches watermelon alone");
            if (i == 48) Check(pit && !walls && !ice && !split && !slope && l.zones.Length == 1, n + ": teaches pit alone");
            Check(l.mastery == (i % 12 == 11) && (!l.mastery || l.starsToPass == 2), n + ": mastery gate");
            Check(l.oneStarTarget >= 1 && l.oneStarTarget <= l.twoStarTarget && l.twoStarTarget <= l.threeStarTarget && l.threeStarTarget <= l.TotalMarbles(), n + ": star targets");
            if (l.starsToPass == 2) Check(l.twoStarTarget < l.threeStarTarget, n + ": gate below mastery");
        }

        // Çeşitlilik
        var all = new List<Vector2[]>(); var names = new List<string>();
        for (int g = 0; g < Maps.TotalLevels; g++)
        {
            var l = Maps.Get(g);
            if (l.marbles == null || l.marbles.Length == 0) { all.Add(null); names.Add(""); continue; }
            var arr = new Vector2[l.marbles.Length];
            for (int k = 0; k < arr.Length; k++) arr[k] = new Vector2(l.marbles[k].x, l.marbles[k].z);
            all.Add(arr); names.Add(g < 60 ? "H1-" + g : "M" + (g / 12) + "-" + (g % 12 + 1).ToString("00"));
        }
        float worst = float.MaxValue; string worstPair = "";
        for (int a = Campaign.Count; a < all.Count; a++)
        {
            if (all[a] == null || all[a].Length < 6) continue;
            for (int b = 0; b < all.Count; b++)
            {
                if (b == a || all[b] == null || all[b].Length < 6) continue;
                if (b > a == false && b >= Campaign.Count) continue;   // M-M çifti bir kez
                float s = LevelVarietyVerify.Similarity(all[a], all[b]);
                if (s < worst) { worst = s; worstPair = names[a] + " <-> " + names[b]; }
                Check(s >= .30f, "variety " + names[a] + " <-> " + names[b] + " " + s.ToString("F2"));
            }
        }
        Debug.Log("MEMLEKET_VARIETY: en benzer çift " + worstPair + " = " + worst.ToString("F2"));

        if (problems.Count > 0)
        {
            foreach (var p in problems) Debug.LogError("MEMLEKET_LAYOUT: " + p);
            throw new Exception("CHECK FAILED: Memleket layout (" + problems.Count + " sorun, ilk: " + problems[0] + ")");
        }
        Debug.Log("MEMLEKET_LAYOUT_OK: " + checks + " checks.");
        return checks;
    }

    private static bool Has(LevelData l, ZoneKind k) { if (l.zones != null) foreach (var z in l.zones) if (z.kind == k) return true; return false; }
    private static bool HasKind(LevelData l, MarbleKind k) { foreach (var m in l.marbles) if (m.kind == k) return true; return false; }

    // Engelin döndürülmüş dikdörtgeni (+pay) içinde mi?
    private static bool InObstacle(ObstacleSpot o, Vector2 p, float pad)
    {
        var local = Quaternion.Euler(0, -o.angle, 0) * new Vector3(p.x - o.x, 0, p.y - o.z);
        return Mathf.Abs(local.x) < Mathf.Max(.2f, o.width) * .5f + pad && Mathf.Abs(local.z) < Mathf.Max(.2f, o.depth) * .5f + pad;
    }
}
