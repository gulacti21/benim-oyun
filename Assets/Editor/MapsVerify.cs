using System;
using UnityEngine;

// HARİTALAR (Faz 1): harita başına veritabanı, global bölüm numarası, ayrı kayıt,
// harita kilidi, eski kaydın sorunsuz açılması, iki haritanın doğru toplanması.
// MahalleVerify.Run içinden çağrılır; TestMode açıkken çalışır (gerçek kayda dokunmaz).
public static class MapsVerify
{
    private static int checks;
    private static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception("CHECK FAILED: " + message); }

    public static int RunChecks()
    {
        checks = 0;

        // --- veri ---
        Check(Maps.Count >= 2, "Maps: at least two maps");
        Check(Maps.Names.Length == Maps.Subtitles.Length, "Maps: every map has a subtitle");
        for (int m = 0; m < Maps.Count; m++)
        {
            var db = Maps.Database(m);
            Check(db != null && db.Count == Maps.PerMap, "Maps: map " + m + " has 60 levels");
            for (int i = 0; i < Maps.PerMap; i++)
            {
                int g = Maps.Global(m, i);
                var l = Maps.Get(g);
                Check(l != null && ReferenceEquals(l, db.Get(i)), "Maps: global lookup " + g);
                Check(Maps.MapOf(g) == m && Maps.Local(g) == i, "Maps: index round trip " + g);
                Check(l.district == g / Campaign.PerDistrict, "Maps: global district " + g);
                Check(Maps.MapOfDistrict(l.district) == m, "Maps: district belongs to map " + g);
                Check(l.mastery == (i % 12 == 11), "Maps: mastery at district end " + g);
                Check(!string.IsNullOrEmpty(l.levelName), "Maps: level name " + g);
                Check(l.shotCount > 0 && l.oneStarTarget > 0 && l.oneStarTarget <= l.twoStarTarget
                      && l.twoStarTarget <= l.threeStarTarget && l.threeStarTarget <= l.TotalMarbles(), "Maps: reachable stars " + g);
            }
        }
        // Harita 1 aynı nesneleri görmeli: Maps, Campaign'in üstüne ince bir katman.
        for (int i = 0; i < Campaign.Count; i++) Check(ReferenceEquals(Maps.Get(i), Campaign.Database.Get(i)), "Maps: map 0 is Campaign " + i);
        for (int d = 0; d < Maps.TotalDistricts; d++)
        {
            Check(!string.IsNullOrEmpty(Maps.DistrictName(d)), "Maps: district name " + d);
            var theme = MahalleTheme.Get(d);
            Check(theme.title == Maps.DistrictName(d), "Maps: theme title matches district " + d);
            Check(theme.levelNames != null && theme.levelNames.Length == 12, "Maps: 12 level names " + d);
        }
        Check(Maps.DistrictName(5) == "Sahil" && Maps.DistrictName(9) == "Bayram Yeri", "Maps: Memleket districts 5-9");
        // Yeni haritalarda bölüm adı tekrarı olmasın (Ustalık Sınavı hariç), Harita 1 adlarıyla
        // da çakışmasın. (Harita 1'in kendi iç tekrarları eski, dokunulmadı.)
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int g = 0; g < Campaign.Count; g++) seen.Add(Maps.Get(g).levelName);
        for (int g = Campaign.Count; g < Maps.TotalLevels; g++)
        {
            string n = Maps.Get(g).levelName;
            if (n == "Ustalık Sınavı") continue;
            Check(seen.Add(n), "Maps: unique level name " + n);
        }

        // --- dil: yeni metinlerin İngilizcesi ---
        for (int m = 0; m < Maps.Count; m++) { Check(L.Has(Maps.Names[m]), "Maps: EN name " + Maps.Names[m]); Check(L.Has(Maps.Subtitles[m]), "Maps: EN subtitle " + m); }
        for (int d = 0; d < Maps.TotalDistricts; d++)
        {
            var theme = MahalleTheme.Get(d);
            Check(L.Has(theme.title) && L.Has(theme.subtitle), "Maps: EN district " + d);
            foreach (var n in theme.levelNames) Check(L.Has(n), "Maps: EN level name " + n);
        }
        foreach (var t in MemleketCampaign.Descriptions) Check(L.Has(t), "Maps: EN description " + t);
        foreach (var t in new[] { "HARİTALAR", "BURADASIN", "{0} / {1} yıldız", "Her haritanın kendi 60 bölümü var. Dokun, geç.",
                                  "{0} haritasını bitir, bu harita açılsın.", "{0} açıldı! Haritalar'dan geçebilirsin.",
                                  "{0} haritası, {1} haritasının son bölümünü geçince açılır." })
            Check(L.Has(t), "Maps: EN map screen text " + t);

        // --- kayıt: yeni oyuncu ---
        var data = new MahalleSave(); MahalleProfile.SetTestData(data);
        Check(data.maps != null && data.maps.Length == Maps.Count - 1, "Maps: new save has map slots");
        Check(data.maps[0].stars.Length == Maps.PerMap && data.stars.Length == Campaign.Count, "Maps: separate star arrays");
        Check(!MahalleProfile.MapUnlocked(1) && !MahalleProfile.Unlocked(Maps.First(1)), "Maps: Memleket locked for new player");
        Check(MahalleProfile.NextLevel == 0, "Maps: new player starts at level 1");

        // --- kilit: Mahalle'nin ustalık sınavı Memleket'i açar ---
        for (int i = 0; i < Campaign.Count - 1; i++) data.stars[i] = 3;
        Check(!MahalleProfile.MapUnlocked(1), "Maps: Memleket locked until Meydan mastery");
        data.stars[Campaign.Count - 1] = MahalleProfile.Required(Campaign.Count - 1) - 1;
        Check(!MahalleProfile.MapUnlocked(1), "Maps: Meydan mastery needs its required stars");
        data.stars[Campaign.Count - 1] = MahalleProfile.Required(Campaign.Count - 1);
        Check(MahalleProfile.MapUnlocked(1) && MahalleProfile.Unlocked(Maps.First(1)), "Maps: beating Meydan mastery opens Memleket");
        Check(!MahalleProfile.Unlocked(Maps.First(1) + 1), "Maps: only the first Memleket level opens");
        Check(MahalleProfile.NextLevelIn(1) == Maps.First(1), "Maps: next Memleket level is its first");

        // --- ilerleme ayrı yazılır ---
        int beadsBefore = data.beads, harita1Stars = 0; foreach (int s in data.stars) harita1Stars += s;
        var reward = MahalleProfile.Finish(Maps.First(1), 2, 5);
        Check(data.maps[0].stars[0] == 2 && MahalleProfile.Stars(Maps.First(1)) == 2, "Maps: Memleket stars stored in map slot");
        int after = 0; foreach (int s in data.stars) after += s;
        Check(after == harita1Stars, "Maps: Mahalle stars untouched by Memleket win");
        Check(reward.firstWin && data.beads == beadsBefore + 6 + 2 * 3, "Maps: same economy on Memleket (6 + 3/star)");
        Check(data.maps[0].districtPlays[0] == 1 && MahalleProfile.DistrictPlays(5) == 1, "Maps: Memleket plays counted per district");
        Check(MahalleProfile.TotalStars == after + 2, "Maps: total stars sum both maps");
        Check(MahalleProfile.MapStars(1) == 2 && MahalleProfile.MapStars(0) == after, "Maps: per-map star totals");
        Check(MahalleProfile.Unlocked(Maps.First(1) + 1), "Maps: Memleket chain continues");
        var replay = MahalleProfile.Finish(Maps.First(1), 2, 5);
        Check(replay.beads == 0, "Maps: replay pays nothing on Memleket");

        // Bölge tamamlama: boncuk + rozet, ama Harita 1 kaplaması hediye edilmez.
        bool[] skinsBefore = (bool[])data.skins.Clone();
        for (int i = 0; i < 11; i++) data.maps[0].stars[i] = 3;
        int b0 = data.beads;
        var last = MahalleProfile.Finish(Maps.First(1) + 11, 3, 10);
        Check(last.newBadge && last.districtBonus == MahalleProfile.DistrictCompletionBonus, "Maps: Memleket district badge");
        Check(MahalleProfile.DistrictCompleted(5) && MahalleProfile.DistrictRewarded(5) && !MahalleProfile.DistrictRewarded(0), "Maps: district reward stored per map");
        Check(string.IsNullOrEmpty(last.newSkin), "Maps: Memleket district gives no Mahalle skin");
        for (int i = 0; i < skinsBefore.Length; i++) Check(data.skins[i] == skinsBefore[i], "Maps: skins unchanged by Memleket " + i);
        Check(data.beads == b0 + 6 + 9 + MahalleProfile.DistrictCompletionBonus, "Maps: district bonus paid once");
        Check(MahalleProfile.AchievementProgress(1) >= 1, "Maps: district achievement counts Memleket");

        // --- eski kayıt: maps alanı yokken sorunsuz açılır ---
        var legacy = new MahalleSave();
        for (int i = 0; i < 20; i++) legacy.stars[i] = 2;
        legacy.beads = 321; legacy.version = 3;
        string json = JsonUtility.ToJson(legacy);
        int cut = json.IndexOf(",\"maps\"", StringComparison.Ordinal);
        Check(cut > 0, "Maps: legacy json has maps field to strip");
        // maps ve lastMap kaydın son iki alanı: kesip kapatınca haritalardan önceki kayıt olur.
        string oldJson = json.Substring(0, cut) + "}";
        Check(!oldJson.Contains("\"maps\"") && !oldJson.Contains("lastMap"), "Maps: simulated pre-map save");
        var loaded = JsonUtility.FromJson<MahalleSave>(oldJson);
        MahalleProfile.SetTestData(loaded);
        Check(loaded.maps != null && loaded.maps.Length == Maps.Count - 1 && loaded.maps[0].stars.Length == Maps.PerMap, "Maps: old save gets map slots");
        Check(loaded.beads == 321 && loaded.stars[19] == 2 && loaded.stars[20] == 0, "Maps: old save keeps Mahalle progress");
        Check(MahalleProfile.MapStars(1) == 0 && loaded.lastMap == 0, "Maps: old save starts Memleket empty");
        Check(MahalleProfile.NextLevel == 20, "Maps: old save resumes where it was");
        // Kayıt yaz-oku turu yeni alanları korur.
        loaded.maps[0].stars[3] = 3; loaded.lastMap = 1;
        var round = JsonUtility.FromJson<MahalleSave>(JsonUtility.ToJson(loaded));
        MahalleProfile.SetTestData(round);
        Check(round.maps[0].stars[3] == 3, "Maps: Memleket stars survive save/load");
        Check(round.lastMap == 1, "Maps: last map survives save/load");

        // --- oyun oturumu ---
        int old = GameSession.SelectedLevelIndex;
        GameSession.SelectedLevelIndex = Maps.Global(1, 7);
        Check(GameSession.SelectedMap == 1, "Maps: session map derived from level");
        GameSession.SelectedLevelIndex = old;

        Debug.Log("MAPS_VERIFY_OK: " + checks + " checks.");
        return checks;
    }
}
