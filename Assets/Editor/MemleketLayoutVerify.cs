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
            // Atış hakkı ölçümle seçildi (Faz 6): 3-7; bölgenin ilk üç öğretme bölümünde 2 de olabilir.
            // Üst sınır 10: Bayram Yeri "bol misket" bölgesi (2026-10-01 kullanıcı kararı, HumanTuning).
            Check(l.shotCount >= (i % 12 <= 2 ? 2 : 3) && l.shotCount <= 10, n + ": shots 3-10 (teaching 2-10)");
            for (int a = 0; a < l.marbles.Length; a++)
            {
                var p = new Vector2(l.marbles[a].x, l.marbles[a].z);
                Check(p.magnitude <= l.arenaSize - .35f, n + ": marble " + a + " inside ring (" + p.magnitude.ToString("F2") + "/" + l.arenaSize + ")");
                for (int b = a + 1; b < l.marbles.Length; b++)
                {
                    float d = Vector2.Distance(p, new Vector2(l.marbles[b].x, l.marbles[b].z));
                    Check(d >= .499f, n + ": marbles " + a + "/" + b + " do not overlap (" + d.ToString("F2") + ")");   // değebilir, iç içe geçemez
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

        // HARİTA 3 (Şehir): hazır bölümlerin yerleşimi. Mekanikler 1. bölümden: 1'de kaldırım taşı,
        // 3'te rögar, sonra karışık; çukur/çamur/buz/karpuz/eğim yok. Düz bölüm yok (kullanıcı).
        for (int i = 0; i < SehirCampaign.Ready; i++)
        {
            var l = Maps.Get(Maps.First(2) + i);
            string n = "S" + (i / 12 + 10) + "-" + (i % 12 + 1).ToString("00") + " " + l.levelName;
            Check(l.marbles != null && l.marbles.Length >= 5, n + ": hand placed marbles");
            if (l.marbles == null) continue;
            Check(l.shape == ArenaShape.Circle && l.arenaSize >= 3f && l.arenaSize <= 4f, n + ": circle 3-4");
            Check(l.shotCount >= 3 && l.shotCount <= 12, n + ": shots 3-12");
            for (int a = 0; a < l.marbles.Length; a++)
            {
                var p = new Vector2(l.marbles[a].x, l.marbles[a].z);
                var mk = l.marbles[a].kind;
                Check(mk == MarbleKind.Normal || mk == MarbleKind.Catapat || (mk == MarbleKind.Ice && i >= 24) || (mk == MarbleKind.Split && i >= 36),
                      n + ": marble " + a + " kind allowed here (ice from 25, watermelon from 37)");
                Check(p.magnitude <= l.arenaSize - .35f, n + ": marble " + a + " inside ring (" + p.magnitude.ToString("F2") + "/" + l.arenaSize + ")");
                for (int b = a + 1; b < l.marbles.Length; b++)
                    Check(Vector2.Distance(p, new Vector2(l.marbles[b].x, l.marbles[b].z)) >= .499f, n + ": marbles " + a + "/" + b + " do not overlap");
                if (l.obstacles != null)
                    foreach (var o in l.obstacles) Check(!InObstacle(o, p, .27f), n + ": marble " + a + " clear of obstacle");
                if (l.zones != null)
                    foreach (var z in l.zones)
                        if (z.kind == ZoneKind.Pit) Check(Vector2.Distance(p, new Vector2(z.x, z.z)) >= z.radius + .05f, n + ": marble " + a + " not in a pit");
            }
            bool walls = l.obstacles != null && l.obstacles.Length > 0, slick = Has(l, ZoneKind.Slick);
            bool sandZ = Has(l, ZoneKind.Sand), pitZ = Has(l, ZoneKind.Pit), mudZ = Has(l, ZoneKind.Mud), slopeS = l.slope.sqrMagnitude > 1e-6f;
            bool iceM = HasKind(l, MarbleKind.Ice), splitM = HasKind(l, MarbleKind.Split);
            if (l.zones != null)
                foreach (var z in l.zones)
                {
                    Check(z.kind == ZoneKind.Slick || (z.kind == ZoneKind.Sand && i >= 12) || (z.kind == ZoneKind.Pit && i >= 15) || (z.kind == ZoneKind.Mud && i >= 39),
                          n + ": zone allowed here (sand from 13, pit from 16, mud from 40)");
                    Check(new Vector2(z.x, z.z).magnitude + z.radius <= l.arenaSize + .35f, n + ": zone on the field");
                    Check(z.z - z.radius > -l.arenaSize - .1f, n + ": zone not under the shooter line");
                }
            if (l.obstacles != null)
                foreach (var o in l.obstacles) Check(new Vector2(o.x, o.z).magnitude <= l.arenaSize, n + ": obstacle on the field");
            Check(!slopeS || i >= 27, n + ": no slope before 28");
            // İlk 12 bölüm sadece yeni özellikler (kullanıcı kararı); eski özellikler 13'ten sonra.
            if (i < 12) Check(!sandZ && !pitZ && !mudZ && !slopeS && !iceM && !splitM, n + ": first 12 only new city features");
            // Eski özelliği öğreten bölüm: o özellik tek başına.
            if (i == 12) Check(sandZ && !walls && !slick && !pitZ && !HasKind(l, MarbleKind.Catapat), n + ": teaches sand alone");
            if (i == 15) Check(pitZ && !walls && !slick && !sandZ && !HasKind(l, MarbleKind.Catapat), n + ": teaches pit alone");
            if (i == 24) Check(iceM && l.zones == null && !walls && !slopeS && !HasKind(l, MarbleKind.Catapat), n + ": teaches ice alone");
            if (i == 27) Check(slopeS && l.zones == null && !walls && !iceM && !HasKind(l, MarbleKind.Catapat), n + ": teaches slope alone");
            if (i == 36) Check(splitM && l.zones == null && !walls && !slopeS && !HasKind(l, MarbleKind.Catapat), n + ": teaches watermelon alone");
            if (i == 39) Check(mudZ && !walls && !slick && !splitM && !HasKind(l, MarbleKind.Catapat), n + ": teaches mud alone");
            Check(l.mastery == (i % 12 == 11) && (!l.mastery || l.starsToPass == 2), n + ": mastery gate");
            if (i < 2) Check(!slick, n + ": no manhole before level 3");
            if (i == 0) Check(walls && !slick, n + ": teaches curbstone alone");
            if (i == 2) Check(slick && !walls, n + ": teaches manhole alone");
            bool pop = HasKind(l, MarbleKind.Catapat);
            if (i < 4) Check(!pop, n + ": no firecracker before level 5");
            if (i == 4) Check(pop && !walls && !slick, n + ": teaches firecracker alone");
            Check(walls || slick || pop || sandZ || pitZ || mudZ || slopeS || iceM || splitM, n + ": every Şehir level has a feature");
        }

        // HARİTA 4 (Lunapark): ilk 12 bölüm SADECE yeni özellikler: 1'de tampon, 3'te yürüyen bant,
        // 5'te mıknatıs misket. Bantın üstünde başlangıçta misket olmaz (kendi kendine yürürdü).
        for (int i = 0; i < LunaparkCampaign.Ready; i++)
        {
            var l = Maps.Get(Maps.First(LunaparkCampaign.MapIndex) + i);
            string n = "L" + (i / 12 + 15) + "-" + (i % 12 + 1).ToString("00") + " " + l.levelName;
            Check(l.marbles != null && l.marbles.Length >= 6, n + ": hand placed marbles");
            if (l.marbles == null) continue;
            Check(l.shape == ArenaShape.Circle && l.arenaSize >= 3f && l.arenaSize <= 4.1f, n + ": circle 3-4.1");
            Check(l.shotCount >= 3 && l.shotCount <= 12, n + ": shots 3-12");
            for (int a = 0; a < l.marbles.Length; a++)
            {
                var p = new Vector2(l.marbles[a].x, l.marbles[a].z);
                Check(p.magnitude <= l.arenaSize - .35f, n + ": marble " + a + " inside ring (" + p.magnitude.ToString("F2") + "/" + l.arenaSize + ")");
                for (int b = a + 1; b < l.marbles.Length; b++)
                    Check(Vector2.Distance(p, new Vector2(l.marbles[b].x, l.marbles[b].z)) >= .499f, n + ": marbles " + a + "/" + b + " do not overlap");
                if (l.obstacles != null)
                    foreach (var o in l.obstacles)
                        Check(o.kind == ObstacleKind.Bumper ? Vector2.Distance(p, new Vector2(o.x, o.z)) >= o.width * .5f + .3f : !InObstacle(o, p, .27f), n + ": marble " + a + " clear of obstacle");
                if (l.zones != null)
                    foreach (var z in l.zones)
                        if (z.kind == ZoneKind.Pit || z.kind == ZoneKind.Belt)
                            Check(Vector2.Distance(p, new Vector2(z.x, z.z)) >= z.radius + (z.kind == ZoneKind.Belt ? .26f : .05f), n + ": marble " + a + " not on a belt/pit");
            }
            if (l.zones != null)
                foreach (var z in l.zones)
                {
                    Check(new Vector2(z.x, z.z).magnitude + z.radius <= l.arenaSize + .35f, n + ": zone on the field");
                    Check(z.z - z.radius > -l.arenaSize - .1f, n + ": zone not under the shooter line");
                    if (l.obstacles != null)
                        foreach (var o in l.obstacles) Check(Vector2.Distance(new Vector2(o.x, o.z), new Vector2(z.x, z.z)) >= z.radius + o.width * .5f, n + ": obstacle not on a zone");
                }
            if (l.obstacles != null)
                foreach (var o in l.obstacles) Check(new Vector2(o.x, o.z).magnitude <= l.arenaSize - .3f, n + ": obstacle on the field");
            bool bumper = false, stone = false;
            if (l.obstacles != null) foreach (var o in l.obstacles) { if (o.kind == ObstacleKind.Bumper) bumper = true; else stone = true; }
            bool belt = Has(l, ZoneKind.Belt), magnet = HasKind(l, MarbleKind.Magnet);
            bool old = stone || Has(l, ZoneKind.Sand) || Has(l, ZoneKind.Mud) || Has(l, ZoneKind.Pit) || Has(l, ZoneKind.Slick) || l.slope.sqrMagnitude > 1e-6f
                       || HasKind(l, MarbleKind.Ice) || HasKind(l, MarbleKind.Split) || HasKind(l, MarbleKind.Catapat);
            if (i < 12) Check(!old, n + ": first 12 only new Lunapark features");
            if (i == 0) Check(bumper && !belt && !magnet, n + ": teaches bumper alone");
            if (i < 2) Check(!belt, n + ": no belt before level 3");
            if (i == 2) Check(belt && !bumper && !magnet, n + ": teaches belt alone");
            if (i < 4) Check(!magnet, n + ": no magnet before level 5");
            if (i == 4) Check(magnet && !bumper && !belt, n + ": teaches magnet alone");
            Check(l.mastery == (i % 12 == 11) && (!l.mastery || l.starsToPass == 2), n + ": mastery gate");
            Check(bumper || belt || magnet || old, n + ": every Lunapark level has a feature");
        }

        // Çeşitlilik (sadece oynanabilir bölümler; kilitli yer tutucular karşılaştırılmaz)
        var all = new List<Vector2[]>(); var names = new List<string>();
        for (int g = 0; g < Maps.TotalLevels; g++)
        {
            var l = Maps.Get(g);
            if (!Maps.Ready(g) || l.marbles == null || l.marbles.Length == 0) { all.Add(null); names.Add(""); continue; }
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
