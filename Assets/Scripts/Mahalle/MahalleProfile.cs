using System;
using UnityEngine;

[Serializable]
public class MahalleSave
{
    public int version = 3;
    public int[] marbleLife = new int[SpecialMarbles.Count];
    public bool[] districtRewards = new bool[5];
    public int beads = 60;
    public int[] stars = new int[Campaign.Count];
    public int[] stock = { 1, 1, 1, 1 };
    public bool[] skins = NewSkins();
    private static bool[] NewSkins() { var owned = new bool[Campaign.SkinCount]; owned[0] = true; return owned; }
    public int selectedSkin;
    public int wins;
    public int knocked;
    public bool[] claimed = new bool[3];
    public bool tutorialDone;
    public bool sound = true;
    public bool haptics = true;
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
            try { data = JsonUtility.FromJson<MahalleSave>(PlayerPrefs.GetString(SaveKey, "")); }
            catch (Exception) { data = null; }
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
        for(int i=0;i<value.marbleLife.Length;i++)value.marbleLife[i]=Mathf.Clamp(value.marbleLife[i],0,SpecialMarbles.MaxLife);
        value.skins[0] = true;
        value.beads = Mathf.Max(0, value.beads);
        for (int i = 0; i < value.stars.Length; i++) value.stars[i] = Mathf.Clamp(value.stars[i], 0, 3);
        for (int i = 0; i < value.stock.Length; i++) value.stock[i] = Mathf.Max(0, value.stock[i]);
        if (value.selectedSkin < 0 || value.selectedSkin >= Campaign.SkinCount || !value.skins[value.selectedSkin]) value.selectedSkin = 0;
    }
    public static void Save()
    {
#if UNITY_EDITOR
        if (TestMode || PreviewMode) { Changed?.Invoke(); return; }
#endif
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
    // ---------------------------------------------------------------
    // TEST YAPISI. Telefonda butun bolumleri acar ki denge denenebilsin.
    // Editor'deki Preview build'e gecmedigi icin var.
    // YAYINDAN ONCE false YAPILACAK. Acikken oyunun ustunde "TEST" yazar
    // ve MISKETR/Verify Mahalle Systems yuksek sesle uyarir.
    // ---------------------------------------------------------------
    public static readonly bool TestUnlockAllLevels = true;

    public static bool Unlocked(int index)
    {
        if (index < 0 || index >= Campaign.Count) return false;
#if UNITY_EDITOR
        // TestMode = dogrulama testleri calisiyor; onlar gercek kilit kuralini olcmeli.
        if (TestUnlockAllLevels && !TestMode) return true;
        if (PreviewMode) return true;
#else
        if (TestUnlockAllLevels) return true;
#endif
        return index == 0 || Data.stars[index - 1] >= Required(index - 1);
    }

    // Bu bölümü geçip sonrakini açmak için gereken yıldız.
    public static int Required(int index)
    {
        if (index < 0 || index >= Campaign.Count) return 1;
        var level = Campaign.Database.Get(index);
        return level != null ? Mathf.Max(1, level.starsToPass) : 1;
    }
    public static int TotalStars { get { int n = 0; foreach (int s in Data.stars) n += s; return n; } }
    public static int NextLevel { get { for (int i = 0; i < Campaign.Count; i++) if (Unlocked(i) && Data.stars[i] < Required(i)) return i; return Campaign.Count - 1; } }
    public static bool CanUse(MarblePower power) => power != MarblePower.None && (Data.stock[(int)power] > 0 || Data.beads >= Campaign.PowerPrices[(int)power]);
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
        if (power == MarblePower.None) return true;
        int p = (int)power;
        if (p < 0 || p >= Data.stock.Length || p >= Campaign.PowerPrices.Length) return false;
        if (Data.stock[p] > 0) { Data.stock[p]--; usedStock = true; }
        else if (Data.beads >= Campaign.PowerPrices[p]) Data.beads -= Campaign.PowerPrices[p];
        else return false;
        Save(); return true;
    }

    // Güç hak ettiği faydayı sağlayamadıysa ödemeyi geri veririz.
    public static void Refund(MarblePower power, bool usedStock)
    {
        int p = (int)power;
        if (p < 0 || p >= Data.stock.Length || p >= Campaign.PowerPrices.Length) return;
        if (usedStock) Data.stock[p]++;
        else Data.beads += Campaign.PowerPrices[p];
        Save();
    }
    public const int DistrictCompletionBonus = 35;
    public static bool DistrictCompleted(int district)
    {
        if (district < 0 || district >= Campaign.Districts.Length) return false;
        int start = district * Campaign.PerDistrict;
        for (int i = start; i < start + Campaign.PerDistrict; i++)
            if (Data.stars[i] < Required(i)) return false;
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
            if(Data.beads<Campaign.SkinPrices[skin])return false;
            Data.beads-=Campaign.SkinPrices[skin];Data.skins[skin]=true;
            if(SpecialMarbles.IsSpecial(skin))Data.marbleLife[skin-SpecialMarbles.FirstSkin]=SpecialMarbles.MaxLife;
        }
        if(SpecialMarbles.IsSpecial(skin)&&RemainingLife(skin)==0)return false;
        Data.selectedSkin=skin;Save();return true;
    }
    public static bool RepairMarble(int skin)
    {
        if(!SpecialMarbles.IsSpecial(skin)||!Data.skins[skin]||RemainingLife(skin)>=SpecialMarbles.MaxLife||Data.beads<SpecialMarbles.RepairPrice)return false;
        Data.beads-=SpecialMarbles.RepairPrice;Data.marbleLife[skin-SpecialMarbles.FirstSkin]=SpecialMarbles.MaxLife;
        Save();return true;
    }
    public static void RecordMarbleShot(int skin,MarblePower power)
    {
        if(power!=MarblePower.None||!SpecialMarbles.IsSpecial(skin)||!Data.skins[skin]||RemainingLife(skin)<=0)return;
        Data.marbleLife[skin-SpecialMarbles.FirstSkin]--;Save();
    }
    public static RoundReward Finish(int levelIndex, int stars, int score)
    {
        var reward = new RoundReward();
        if (levelIndex < 0 || levelIndex >= Campaign.Count) return reward;
        Data.knocked += Mathf.Max(0, score);
        stars = Mathf.Clamp(stars, 0, 3);
        if (stars > 0)
        {
            int old = Data.stars[levelIndex];
            reward.firstWin = old == 0;
            reward.beads = (reward.firstWin ? 20 : 3) + Mathf.Max(0, stars - old) * 5;
            Data.stars[levelIndex] = Mathf.Max(old, stars);
            Data.wins++;
            int district = levelIndex / Campaign.PerDistrict;
            if (!Data.districtRewards[district] && DistrictCompleted(district))
            {
                Data.districtRewards[district] = true;
                reward.newBadge = true;
                reward.districtBonus = DistrictCompletionBonus;
                reward.beads += reward.districtBonus;
                int skin = district + 1;
                if (!Data.skins[skin]) { Data.skins[skin] = true; reward.newSkin = Campaign.SkinNames[skin]; }
            }
            Data.beads += reward.beads;
        }
        Save(); return reward;
    }
    public static int MissionProgress(int id)
    {
        if (id == 0) return Data.wins;
        if (id == 1) return Data.knocked;
        int count = 0; foreach (int s in Data.stars) if (s == 3) count++; return count;
    }
    public static readonly int[] MissionTargets = { 3, 20, 3 };
    public static readonly int[] MissionRewards = { 30, 50, 60 };
    public static readonly string[] MissionNames = { "Üç kez bölüm kazan", "Toplam 20 misket çıkar", "Üç bölümde üç yıldız al" };
    public static bool Claim(int id)
    {
        if (id < 0 || id >= 3 || Data.claimed[id] || MissionProgress(id) < MissionTargets[id]) return false;
        Data.claimed[id] = true; Data.beads += MissionRewards[id]; Save(); return true;
    }
    public static void Reload() { data = null; }
    public static void Reset() { data = new MahalleSave(); Save(); }
}
