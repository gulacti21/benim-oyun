using System;
using UnityEngine;

[Serializable]
public class MahalleSave
{
    public int version = 3;
    public int[] marbleLife = new int[SpecialMarbles.Count];
    public bool[] districtRewards = new bool[5];
    public int beads = 40;
    public int[] stars = new int[Campaign.Count];
    public int[] stock = { 1, 1, 1, 1 };
    public bool[] skins = NewSkins();
    private static bool[] NewSkins() { var owned = new bool[Campaign.SkinCount]; owned[0] = true; return owned; }
    public int selectedSkin;
    public int wins;
    public int knocked;
    // İSTATİSTİK: tek atışta en çok çıkan misket ve mahalle başına bitirilen bölüm sayısı.
    public int bestShot;
    public int[] districtPlays = new int[5];
    public bool[] claimed = new bool[3];
    public bool tutorialDone;
    // ÖĞRETİCİ: ısınma sahası bitirildi ya da geçildi.
    public bool howToPlayDone;
    // DİL: "" = telefona göre, "tr", "en"
    public string language = "";
    public bool sound = true;
    public bool music = true;
    // GÜNÜN BÖLÜMÜ: ödül alınan son gün (DailyLevel.DayIndex) ve üst üste gün sayısı.
    public int dailyDay = -9999;
    public int dailyStreak;
    // Günün bölümü deneme hakkı: hangi gün ve o gün kaç kez oynandı.
    public int dailyTriesDay = -9999;
    public int dailyTries;
    public int dailyBestStreak;
    // SONSUZ ÇEMBER rekoru.
    public int endlessBest;
    // Günlük hatırlatma bildirimi ve mağaza puanlama isteği (bir kez).
    public bool notify = true;
    public bool notifyAsked;
    public bool reviewAsked;
    public bool haptics = true;
    // HARİTALAR: Harita 1 (Mahalle) yukarıdaki stars / districtRewards /
    // districtPlays alanlarında kalır, eski kayıt aynen açılır. Harita 2 ve
    // sonrası burada, harita başına ayrı. maps[0] = Harita 2 (Memleket).
    public MapProgress[] maps = new MapProgress[0];
    // Oyuncunun en son baktığı harita; oyun o haritada açılır.
    public int lastMap;
    // Oyuncuya "açıldı" diye söylenen en yüksek harita (0 = sadece Mahalle).
    public int mapsAnnounced;
    // Oyuncuya anlatılan yeni özellikler (MahalleUI.Mechanics bitleri: kum, çamur, eğim, buz, karpuz, çukur).
    public int mechanicsSeen;
}

[Serializable]
public class MapProgress
{
    public int[] stars = new int[Maps.PerMap];
    public bool[] districtRewards = new bool[Maps.DistrictsPerMap];
    public int[] districtPlays = new int[Maps.DistrictsPerMap];
}

public struct RoundReward
{
    public int beads;
    public bool firstWin;
    public bool newBadge;
    public int districtBonus;
    public string newSkin;
}

public static class MahalleProfile
{
    public const string SaveKey = "MISKETR_Mahalle_v1";
    private static MahalleSave data;
#if UNITY_EDITOR
    public static bool TestMode { get; set; }
    public static bool PreviewMode { get; private set; }
    public static void BeginPreview()
    {
        if (PreviewMode) return;
        data = JsonUtility.FromJson<MahalleSave>(JsonUtility.ToJson(Data));
        PreviewMode = true;
    }
    public static void EndPreview() { PreviewMode = false; Reload(); }
    public static void SetTestData(MahalleSave save) { data = save; Normalize(data); }
#endif
    public static event Action Changed;
    public static MahalleSave Data
    {
        get
        {
            if (data != null) return data;
            data = LoadSlot(SaveKey, SumKey);                     // güncel kayıt
            if (data == null) data = LoadSlot(BackupKey, BackupSumKey); // bozuksa bir önceki
            if (data == null)
            {
                data = new MahalleSave();
                for (int i = 0; i < 6; i++) data.stars[i] = Mathf.Clamp(PlayerPrefs.GetInt("MISKETR_Stars_" + i, 0), 0, 3);
            }
            Normalize(data);
            return data;
        }
    }
    private static void Normalize(MahalleSave value)
    {
        if (value.stars == null) value.stars = new int[Campaign.Count];
        Array.Resize(ref value.stars, Campaign.Count);
        if (value.stock == null) value.stock = new int[4];
        Array.Resize(ref value.stock, 4);
        if (value.skins == null) value.skins = new bool[Campaign.SkinCount];
        Array.Resize(ref value.skins, Campaign.SkinCount);
        if (value.claimed == null) value.claimed = new bool[3];
        Array.Resize(ref value.claimed, 3);
        if (value.districtRewards == null) value.districtRewards = new bool[Campaign.Districts.Length];
        Array.Resize(ref value.districtRewards, Campaign.Districts.Length);
        if (value.version < 2)
        {
            // Eski sürüm finalde 1 yıldızda bile ödül verdi. İkinci ödeme yapma.
            for (int d = 0; d < value.districtRewards.Length; d++)
                if (value.stars[(d + 1) * Campaign.PerDistrict - 1] > 0) value.districtRewards[d] = true;
            value.version = 2;
        }
        if (value.districtPlays == null) value.districtPlays = new int[Campaign.Districts.Length];
        Array.Resize(ref value.districtPlays, Campaign.Districts.Length);
        if(value.marbleLife==null)value.marbleLife=new int[SpecialMarbles.Count];
        Array.Resize(ref value.marbleLife,SpecialMarbles.Count);
        if(value.version<3)
        {
            // Kaldırılan takım misketlerinin 600 boncuk bedeli bir kez iade edilir.
            for(int i=SpecialMarbles.FirstSkin;i<Campaign.SkinCount;i++)
                if(value.skins[i]){value.beads+=600;value.skins[i]=false;}
            if(SpecialMarbles.IsSpecial(value.selectedSkin))value.selectedSkin=0;
            value.version=3;
        }
        if (value.maps == null) value.maps = new MapProgress[0];
        Array.Resize(ref value.maps, Maps.Count - 1);
        for (int m = 0; m < value.maps.Length; m++)
        {
            var mp = value.maps[m] ?? (value.maps[m] = new MapProgress());
            if (mp.stars == null) mp.stars = new int[Maps.PerMap];
            Array.Resize(ref mp.stars, Maps.PerMap);
            for (int i = 0; i < mp.stars.Length; i++) mp.stars[i] = Mathf.Clamp(mp.stars[i], 0, 3);
            if (mp.districtRewards == null) mp.districtRewards = new bool[Maps.DistrictsPerMap];
            Array.Resize(ref mp.districtRewards, Maps.DistrictsPerMap);
            if (mp.districtPlays == null) mp.districtPlays = new int[Maps.DistrictsPerMap];
            Array.Resize(ref mp.districtPlays, Maps.DistrictsPerMap);
        }
        value.lastMap = Mathf.Clamp(value.lastMap, 0, Maps.Count - 1);
        for(int i=0;i<value.marbleLife.Length;i++)value.marbleLife[i]=Mathf.Clamp(value.marbleLife[i],0,SpecialMarbles.MaxLife);
        value.skins[0] = true;
        value.beads = Mathf.Max(0, value.beads);
        for (int i = 0; i < value.stars.Length; i++) value.stars[i] = Mathf.Clamp(value.stars[i], 0, 3);
        for (int i = 0; i < value.stock.Length; i++) value.stock[i] = Mathf.Max(0, value.stock[i]);
        if (value.selectedSkin < 0 || value.selectedSkin >= Campaign.SkinCount || !value.skins[value.selectedSkin]) value.selectedSkin = 0;
    }
    // ---------------------------------------------------------------
    // KAYIT GÜVENLİĞİ
    // Tek anahtara yazmak risklidir: yazma sırasında oyun kapanır veya veri
    // bozulursa bütün ilerleme gider. Bu yüzden her kayıtta önce ÖNCEKİ
    // sağlam kayıt yedek anahtara kopyalanır, sonra yenisi yazılır. Her
    // kaydın yanında bir sağlama (checksum) durur; okurken tutmuyorsa o slot
    // bozuk sayılır ve yedeğe düşülür.
    public const string SumKey = SaveKey + "_sum";
    public const string BackupKey = SaveKey + "_yedek";
    public const string BackupSumKey = BackupKey + "_sum";

    private static string Checksum(string json)
    {
        unchecked
        {
            uint h = 2166136261;
            foreach (char c in json) { h ^= c; h *= 16777619; }
            return (h ^ 0x4D49534Bu).ToString("X8");   // "MISK"
        }
    }

    private static MahalleSave LoadSlot(string key, string sumKey)
    {
        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(json)) return null;
        string sum = PlayerPrefs.GetString(sumKey, "");
        // Sağlaması olmayan eski kayıtlar da kabul edilir (sürüm geçişi).
        if (!string.IsNullOrEmpty(sum) && sum != Checksum(json)) return null;
        try
        {
            var loaded = JsonUtility.FromJson<MahalleSave>(json);
            return loaded != null && loaded.stars != null ? loaded : null;
        }
        catch (Exception) { return null; }
    }

    public static void Save()
    {
#if UNITY_EDITOR
        if (TestMode || PreviewMode) { Changed?.Invoke(); return; }
#endif
        string json = JsonUtility.ToJson(Data);
        // Önceki kayıt sağlamsa yedeğe al; bozuksa yedeğe dokunma.
        if (LoadSlot(SaveKey, SumKey) != null)
        {
            PlayerPrefs.SetString(BackupKey, PlayerPrefs.GetString(SaveKey, ""));
            PlayerPrefs.SetString(BackupSumKey, PlayerPrefs.GetString(SumKey, ""));
        }
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.SetString(SumKey, Checksum(json));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    // Test için: bir kaydın sağlam okunup okunmadığını söyler.
    public static bool SlotValid(string key, string sumKey) => LoadSlot(key, sumKey) != null;
    // ---------------------------------------------------------------
    // TEST YAPISI. Telefonda butun bolumleri acar ki denge denenebilsin.
    // Editor'deki Preview build'e gecmedigi icin var.
    // YAYINDAN ONCE false YAPILACAK. Acikken oyunun ustunde "TEST" yazar
    // ve MISKETR/Verify Mahalle Systems yuksek sesle uyarir.
    // ---------------------------------------------------------------
    public static readonly bool TestUnlockAllLevels = true;

    // Telefonda deneme yaparken boncuk biriktirmekle ugrasilmasin diye:
    // acikken kese hep dolu gorunur ve harcamalar keseden dusmez.
    // TestUnlockAllLevels ile AYNI anahtara bagli degil ama ayni kural
    // gecerli: YAYINDAN ONCE false YAPILACAK. Acikken oyunun altinda
    // "TEST" serididir ve MahalleVerify yuksek sesle uyarir.
    public static readonly bool TestInfiniteBeads = true;

    // Test kesesi. Gercek bir sayi, cunku arayuzun her yerinde boncuk
    // sayisi yaziliyor; "sonsuz" diye bir deger koyarsak metinler bozulur.
    public const int TestBeads = 999999;

    // Oyunun her yerinde boncuk BURADAN okunur. Test acikken gercek kese
    // hic degismez, sadece gorunen deger buyuktur.
    public static int Beads
    {
        get
        {
#if UNITY_EDITOR
            if (TestInfiniteBeads && !TestMode) return TestBeads;
#else
            if (TestInfiniteBeads) return TestBeads;
#endif
            return Data.beads;
        }
    }

    // Iade. Test acikken harcama yapilmadigi icin iade de yapilmaz --
    // yoksa gercek kese sisip oyuncunun kaydi bozulurdu.
    public static void RefundBeads(int amount)
    {
        if (amount <= 0) return;
#if UNITY_EDITOR
        if (TestInfiniteBeads && !TestMode) return;
#else
        if (TestInfiniteBeads) return;
#endif
        Data.beads += amount;
    }

    // Boncuk harcamasi. Test acikken kese hic azalmaz.
    public static bool SpendBeads(int amount)
    {
        if (amount <= 0) return true;
#if UNITY_EDITOR
        if (TestInfiniteBeads && !TestMode) return true;
#else
        if (TestInfiniteBeads) return true;
#endif
        if (Data.beads < amount) return false;
        Data.beads -= amount;
        return true;
    }

    // ---------------------------------------------------------------
    // HARİTA ERİŞİMİ. Bölüm numarası global (Maps): 0-59 Mahalle, 60-119 Memleket.
    // Harita 1 eski alanlardan (Data.stars), sonrakiler Data.maps'ten okunur.
    private static int[] StarArray(int map) => map == 0 ? Data.stars : Data.maps[map - 1].stars;
    private static bool[] RewardArray(int map) => map == 0 ? Data.districtRewards : Data.maps[map - 1].districtRewards;
    private static int[] PlayArray(int map) => map == 0 ? Data.districtPlays : Data.maps[map - 1].districtPlays;
    public static int Stars(int index) => Maps.Valid(index) ? StarArray(Maps.MapOf(index))[Maps.Local(index)] : 0;
    private static void SetStars(int index, int value) { if (Maps.Valid(index)) StarArray(Maps.MapOf(index))[Maps.Local(index)] = Mathf.Clamp(value, 0, 3); }
    public static int DistrictPlays(int district)
    {
        if (district < 0 || district >= Maps.TotalDistricts) return 0;
        return PlayArray(Maps.MapOfDistrict(district))[district % Maps.DistrictsPerMap];
    }
    public static bool DistrictRewarded(int district)
    {
        if (district < 0 || district >= Maps.TotalDistricts) return false;
        return RewardArray(Maps.MapOfDistrict(district))[district % Maps.DistrictsPerMap];
    }
    // Harita açık mı: ilk bölümü açıksa açıktır (bir önceki haritanın son bölümü geçilmiş).
    public static bool MapUnlocked(int map) => map >= 0 && map < Maps.Count && Unlocked(Maps.First(map));
    public static int MapStars(int map) { int n = 0; if (map >= 0 && map < Maps.Count) foreach (int s in StarArray(map)) n += s; return n; }
    // Bütün haritalardaki yıldızlar (istatistik, rozet, usta seviyesi hepsini sayar).
    private static System.Collections.Generic.IEnumerable<int> AllStars()
    {
        for (int m = 0; m < Maps.Count; m++) foreach (int s in StarArray(m)) yield return s;
    }

    public static bool Unlocked(int index)
    {
        if (index < 0 || index >= Maps.TotalLevels) return false;
#if UNITY_EDITOR
        // TestMode = dogrulama testleri calisiyor; onlar gercek kilit kuralini olcmeli.
        if (TestUnlockAllLevels && !TestMode) return true;
        if (PreviewMode) return true;
#else
        if (TestUnlockAllLevels) return true;
#endif
        // Haritanın ilk bölümü bir önceki haritanın ustalık sınavına bağlı: zincir kesintisiz.
        return index == 0 || Stars(index - 1) >= Required(index - 1);
    }

    // Bu bölümü geçip sonrakini açmak için gereken yıldız.
    public static int Required(int index)
    {
        if (index < 0 || index >= Maps.TotalLevels) return 1;
        var level = Maps.Get(index);
        return level != null ? Mathf.Max(1, level.starsToPass) : 1;
    }
    public static int TotalStars { get { int n = 0; foreach (int s in AllStars()) n += s; return n; } }
    // Harita 1'in sıradaki bölümü (eski davranış).
    public static int NextLevel => NextLevelIn(0);
    public static int NextLevelIn(int map)
    {
        map = Mathf.Clamp(map, 0, Maps.Count - 1);
        int first = Maps.First(map), end = first + Maps.PerMap;
        for (int i = first; i < end; i++) if (Unlocked(i) && Stars(i) < Required(i)) return i;
        return end - 1;
    }
    // Öğreticide güçler bedava: hak da boncuk da harcanmaz.
    public static bool CanUse(MarblePower power) => power != MarblePower.None && (GameSession.TutorialMode || Data.stock[(int)power] > 0 || Beads >= Campaign.PowerPrices[(int)power]);
    // Spend only when a real shot is released. Previewing, cancelling, pausing or leaving never charges.
    public static bool Consume(MarblePower power)
    {
        bool ignored;
        return Consume(power, out ignored);
    }

    // usedStock: ücretsiz hak mı harcandı, yoksa boncuk mu ödendi. İade için gerekir.
    public static bool Consume(MarblePower power, out bool usedStock)
    {
        usedStock = false;
        if (power == MarblePower.None || GameSession.TutorialMode) return true;
        int p = (int)power;
        if (p < 0 || p >= Data.stock.Length || p >= Campaign.PowerPrices.Length) return false;
        if (Data.stock[p] > 0) { Data.stock[p]--; usedStock = true; }
        else if (!SpendBeads(Campaign.PowerPrices[p])) return false;
        Save(); return true;
    }

    // Güç hak ettiği faydayı sağlayamadıysa ödemeyi geri veririz.
    public static void Refund(MarblePower power, bool usedStock)
    {
        if (GameSession.TutorialMode) return;
        int p = (int)power;
        if (p < 0 || p >= Data.stock.Length || p >= Campaign.PowerPrices.Length) return;
        if (usedStock) Data.stock[p]++;
        else RefundBeads(Campaign.PowerPrices[p]);
        Save();
    }
    // USTA SEVİYESİ: yıldız, çıkardığın misket ve günlük seriler puana dönüşür.
    public static int MasteryPoints
    {
        get
        {
            int stars = TotalStars;
            return stars * 10 + Data.knocked + Data.dailyBestStreak * 15;
        }
    }
    public static int MasteryLevel => 1 + MasteryPoints / 250;
    public static int MasteryIntoLevel => MasteryPoints % 250;
    public const int MasteryPerLevel = 250;

    // BAŞARIMLAR: ödül vermez, ilerleme gösterir (ödüller görevlerde).
    public static readonly string[] AchievementNames =
        { "İlk Atış", "Mahalle Ustası", "Keskin Nişancı", "Koleksiyoncu", "Sadık Oyuncu", "Yıldız Avcısı" };
    public static readonly string[] AchievementNotes =
        { "Bir bölüm kazan", "Bir mahalleyi tamamla", "Tek atışta 4 misket çıkar",
          "5 misket kaplaması topla", "5 gün üst üste günün bölümünü bitir", "60 yıldız topla" };
    public static readonly int[] AchievementTargets = { 1, 1, 4, 5, 5, 60 };
    public static int AchievementProgress(int index)
    {
        switch (index)
        {
            case 0: { int n = 0; foreach (int s in AllStars()) if (s > 0) n++; return n; }
            case 1: { int n = 0; for (int d = 0; d < Maps.TotalDistricts; d++) if (DistrictCompleted(d)) n++; return n; }
            case 2: return Data.bestShot;
            case 3: { int n = 0; foreach (bool o in Data.skins) if (o) n++; return n; }
            case 4: return Data.dailyBestStreak;
            default: return TotalStars;
        }
    }
    public static bool AchievementDone(int index) => AchievementProgress(index) >= AchievementTargets[index];
    public const int DistrictCompletionBonus = 35;
    public static bool DistrictCompleted(int district)
    {
        if (district < 0 || district >= Maps.TotalDistricts) return false;
        int start = district * Campaign.PerDistrict;   // bölge numarası global, 12'şer bölüm
        for (int i = start; i < start + Campaign.PerDistrict; i++)
            if (Stars(i) < Required(i)) return false;
        return true;
    }

    public static bool CanBuySkin(int skin)=>skin>=0&&skin<Campaign.SkinCount;
    public static int RemainingLife(int skin)=>SpecialMarbles.IsSpecial(skin)?Data.marbleLife[skin-SpecialMarbles.FirstSkin]:SpecialMarbles.MaxLife;
    public static int EffectiveSkin=>SpecialMarbles.IsSpecial(Data.selectedSkin)&&RemainingLife(Data.selectedSkin)==0?0:Data.selectedSkin;
    public static bool EquipOrBuy(int skin)
    {
        if(!CanBuySkin(skin))return false;
        if(!Data.skins[skin])
        {
            if(Beads<Campaign.SkinPrices[skin])return false;
            if(!SpendBeads(Campaign.SkinPrices[skin]))return false;Data.skins[skin]=true;
            if(SpecialMarbles.IsSpecial(skin))Data.marbleLife[skin-SpecialMarbles.FirstSkin]=SpecialMarbles.MaxLife;
        }
        if(SpecialMarbles.IsSpecial(skin)&&RemainingLife(skin)==0)return false;
        Data.selectedSkin=skin;Save();return true;
    }
    public static bool RepairMarble(int skin)
    {
        if(!SpecialMarbles.IsSpecial(skin)||!Data.skins[skin]||RemainingLife(skin)>=SpecialMarbles.MaxLife||Beads<SpecialMarbles.RepairPrice)return false;
        if(!SpendBeads(SpecialMarbles.RepairPrice))return false;Data.marbleLife[skin-SpecialMarbles.FirstSkin]=SpecialMarbles.MaxLife;
        Save();return true;
    }
    public static void RecordMarbleShot(int skin,MarblePower power)
    {
        if(power!=MarblePower.None||!SpecialMarbles.IsSpecial(skin)||!Data.skins[skin]||RemainingLife(skin)<=0)return;
        Data.marbleLife[skin-SpecialMarbles.FirstSkin]--;Save();
    }
    // Tek atışın sonucu. Sadece rekor kırılınca kaydeder.
    public static void RecordShot(int knockedThisShot)
    {
        if (knockedThisShot <= Data.bestShot) return;
        Data.bestShot = knockedThisShot; Save();
    }
    // En çok bitirilen mahalle; hiç oynanmadıysa -1.
    public static int FavoriteDistrict()
    {
        int best = -1, most = 0;
        for (int i = 0; i < Maps.TotalDistricts; i++)
            if (DistrictPlays(i) > most) { most = DistrictPlays(i); best = i; }
        return best;
    }
    public static bool DailyDoneToday => Data.dailyDay == DailyLevel.DayIndex;
    public static int DailyTriesLeft => Mathf.Max(0, DailyLevel.MaxTries -
        (Data.dailyTriesDay == DailyLevel.DayIndex ? Data.dailyTries : 0));
    // Gün açık mı: geçilmediyse ve hak kaldıysa.
    public static bool DailyOpen => !DailyDoneToday && DailyTriesLeft > 0;
    public static bool DailyUseTry()
    {
        if (!DailyOpen) return false;
        int today = DailyLevel.DayIndex;
        if (Data.dailyTriesDay != today) { Data.dailyTriesDay = today; Data.dailyTries = 0; }
        Data.dailyTries++;
        Save(); return true;
    }
    // Bugün oynanırsa geçerli olacak seri (dün bitirildiyse devam eder).
    public static int DailyStreakShown
    {
        get
        {
            int today = DailyLevel.DayIndex;
            if (Data.dailyDay == today || Data.dailyDay == today - 1) return Data.dailyStreak;
            return 0;
        }
    }
    public static int DailyNextReward => DailyLevel.RewardFor(Data.dailyDay == DailyLevel.DayIndex - 1 ? Data.dailyStreak + 1 : 1);
    public static RoundReward FinishDaily(int stars, int score)
    {
        var reward = new RoundReward();
        Data.knocked += Mathf.Max(0, score);
        int today = DailyLevel.DayIndex;
        if (stars > 0 && Data.dailyDay != today)
        {
            Data.dailyStreak = Data.dailyDay == today - 1 ? Data.dailyStreak + 1 : 1;
            if (Data.dailyStreak > Data.dailyBestStreak) Data.dailyBestStreak = Data.dailyStreak;
            Data.dailyDay = today;
            reward.firstWin = true;
            reward.beads = DailyLevel.RewardFor(Data.dailyStreak);
            Data.beads += reward.beads;
        }
        Save(); return reward;
    }
    // SONSUZ ÇEMBER: sadece rekor tutulur, boncuk yok (kampanyanın ekonomisini bozmasın).
    public static bool FinishEndless(int score)
    {
        Data.knocked += Mathf.Max(0, score);
        bool record = score > Data.endlessBest;
        if (record) Data.endlessBest = score;
        Save(); return record;
    }
    public static RoundReward Finish(int levelIndex, int stars, int score)
    {
        var reward = new RoundReward();
        if (levelIndex < 0 || levelIndex >= Maps.TotalLevels) return reward;
        int map = Maps.MapOf(levelIndex);
        int district = levelIndex / Campaign.PerDistrict;               // global bölge
        int localDistrict = district % Maps.DistrictsPerMap;
        Data.knocked += Mathf.Max(0, score);
        PlayArray(map)[localDistrict]++;
        stars = Mathf.Clamp(stars, 0, 3);
        if (stars > 0)
        {
            int old = Stars(levelIndex);
            reward.firstWin = old == 0;
            // EKONOMI DENGESI:
            //   ilk gecis 20 -> 6, yildiz basina 5 -> 3, TEKRAR OYNAMA 3 -> 0.
            // Tekrar oynamanin 3 boncuk vermesi bir delikti: APARTMAN 01 uc misketlik
            // ve 15 saniye suruyor, yani saatte ~700 boncuk farmlanabiliyordu. Artik
            // gecilmis bir bolumu tekrar oynamak sadece YENI yildiz icin oduyor.
            reward.beads = (reward.firstWin ? 6 : 0) + Mathf.Max(0, stars - old) * 3;
            SetStars(levelIndex, Mathf.Max(old, stars));
            Data.wins++;
            var rewards = RewardArray(map);
            if (!rewards[localDistrict] && DistrictCompleted(district))
            {
                rewards[localDistrict] = true;
                reward.newBadge = true;
                reward.districtBonus = DistrictCompletionBonus;
                reward.beads += reward.districtBonus;
                // Kaplama hediyesi sadece Harita 1'de (skin 1-5). Memleket bölgeleri
                // boncuk + rozet verir; kendi kaplamaları mağaza fazında eklenecek.
                int skin = district + 1;
                if (map == 0 && !Data.skins[skin]) { Data.skins[skin] = true; reward.newSkin = Campaign.SkinNames[skin]; }
            }
            Data.beads += reward.beads;
        }
        Save(); return reward;
    }
    public static int MissionProgress(int id)
    {
        if (id == 0) return Data.wins;
        if (id == 1) return Data.knocked;
        int count = 0; foreach (int s in AllStars()) if (s == 3) count++; return count;
    }
    public static readonly int[] MissionTargets = { 10, 150, 8 };
    public static readonly int[] MissionRewards = { 50, 80, 120 };
    public static readonly string[] MissionNames = { "Üç kez bölüm kazan", "Toplam 20 misket çıkar", "Üç bölümde üç yıldız al" };
    public static bool Claim(int id)
    {
        if (id < 0 || id >= 3 || Data.claimed[id] || MissionProgress(id) < MissionTargets[id]) return false;
        Data.claimed[id] = true; Data.beads += MissionRewards[id]; Save(); return true;
    }
    public static void Reload() { data = null; }
    // Normalize şart: yeni kayıtta maps boş dizi, Memleket okunurken taşıyordu (İstatistik çöküyordu).
    public static void Reset() { data = new MahalleSave(); Normalize(data); Save(); }
}
