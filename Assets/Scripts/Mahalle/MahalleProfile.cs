using System;
using UnityEngine;

[Serializable]
public class MahalleSave
{
    public int version = 1;
    public int beads = 60;
    public int[] stars = new int[Campaign.Count];
    public int[] stock = { 1, 1, 1 };
    public bool[] skins = { true, false, false, false, false, false };
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
        if (value.stock == null) value.stock = new int[3];
        Array.Resize(ref value.stock, 3);
        if (value.skins == null) value.skins = new bool[6];
        Array.Resize(ref value.skins, 6);
        if (value.claimed == null) value.claimed = new bool[3];
        Array.Resize(ref value.claimed, 3);
        value.skins[0] = true;
        value.beads = Mathf.Max(0, value.beads);
        for (int i = 0; i < value.stars.Length; i++) value.stars[i] = Mathf.Clamp(value.stars[i], 0, 3);
        for (int i = 0; i < 3; i++) value.stock[i] = Mathf.Max(0, value.stock[i]);
        if (value.selectedSkin < 0 || value.selectedSkin >= 6 || !value.skins[value.selectedSkin]) value.selectedSkin = 0;
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
    public static bool Unlocked(int index)
    {
        if (index < 0 || index >= Campaign.Count) return false;
#if UNITY_EDITOR
        if (PreviewMode) return true;
#endif
        return index == 0 || Data.stars[index - 1] > 0;
    }
    public static int TotalStars { get { int n = 0; foreach (int s in Data.stars) n += s; return n; } }
    public static int NextLevel { get { for (int i = 0; i < Campaign.Count; i++) if (Data.stars[i] == 0 && Unlocked(i)) return i; return Campaign.Count - 1; } }
    public static bool CanUse(MarblePower power) => power != MarblePower.None && (Data.stock[(int)power] > 0 || Data.beads >= Campaign.PowerPrices[(int)power]);
    // Spend only when a real shot is released. Previewing, cancelling, pausing or leaving never charges.
    public static bool Consume(MarblePower power)
    {
        if (power == MarblePower.None) return true;
        int p = (int)power;
        if (p < 0 || p >= 3) return false;
        if (Data.stock[p] > 0) Data.stock[p]--;
        else if (Data.beads >= Campaign.PowerPrices[p]) Data.beads -= Campaign.PowerPrices[p];
        else return false;
        Save(); return true;
    }
    public static bool EquipOrBuy(int skin)
    {
        if (skin < 0 || skin >= 6) return false;
        if (!Data.skins[skin])
        {
            if (Data.beads < Campaign.SkinPrices[skin]) return false;
            Data.beads -= Campaign.SkinPrices[skin]; Data.skins[skin] = true;
        }
        Data.selectedSkin = skin; Save(); return true;
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
            if (reward.firstWin && levelIndex % Campaign.PerDistrict == 11)
            {
                reward.newBadge = true; reward.beads += 35;
                int skin = levelIndex / Campaign.PerDistrict + 1;
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
