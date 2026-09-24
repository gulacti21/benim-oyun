using System.Runtime.InteropServices;
using UnityEngine;

// iCLOUD KAYDI + GAME CENTER (iOS eklentisi: Assets/Plugins/iOS/MisketrCloud.mm).
//
// AÇMA ANAHTARI: Enabled. İkisi de ÜCRETLİ Apple Developer hesabı ister; ücretsiz
// hesapla bu yetkiler (entitlement) açıkken Xcode imzalayamaz. Enabled=false iken
// IosPostBuild yetki eklemez ve oyun hiçbir çağrı yapmaz — bugünkü gibi derlenir.
// Hesap alınınca: Enabled=true, App Store Connect'te liderlik tablosu ve başarımları
// aşağıdaki kimliklerle oluştur (CLAUDE.md'de adım adım).
public static class MisketrCloud
{
    public static readonly bool Enabled = false;

    // --- iCloud ---
    private const string SaveKey = "misko_save", SumKey = "misko_sum";

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void MisketrCloudStart();
    [DllImport("__Internal")] private static extern string MisketrCloudGet(string key);
    [DllImport("__Internal")] private static extern void MisketrCloudSet(string key, string value);
    [DllImport("__Internal")] private static extern bool MisketrCloudConsumeChanged();
    private static bool started;
    private static void Start() { if (!started) { started = true; MisketrCloudStart(); } }
#endif

    // Buluttaki kayıt (JSON + sağlama). Yoksa false.
    public static bool Read(out string json, out string sum)
    {
        json = sum = null;
#if UNITY_IOS && !UNITY_EDITOR
        if (!Enabled) return false;
        Start();
        json = MisketrCloudGet(SaveKey);
        sum = MisketrCloudGet(SumKey);
#endif
        return !string.IsNullOrEmpty(json);
    }

    public static void Write(string json, string sum)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (!Enabled) return;
        Start();
        MisketrCloudSet(SaveKey, json);
        MisketrCloudSet(SumKey, sum);
#endif
    }

    // Başka cihazdan (ya da ilk kurulumda geç gelen) yeni kayıt var mı.
    public static bool ConsumeChanged()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (!Enabled) return false;
        Start();
        return MisketrCloudConsumeChanged();
#else
        return false;
#endif
    }
}

public static class MisketrGameCenter
{
    // App Store Connect'te bu kimliklerle oluşturulmalı.
    public const string StarsBoard = "misko.yildiz";      // toplam yıldız
    public const string BestShotBoard = "misko.tek_atis"; // tek atışta en çok misket
    // MahalleProfile.AchievementNames sırasıyla: misko.basarim1 ... misko.basarim6
    public static string AchievementId(int index) => "misko.basarim" + (index + 1);

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void MisketrGcAuthenticate();
    [DllImport("__Internal")] private static extern bool MisketrGcReady();
    [DllImport("__Internal")] private static extern void MisketrGcScore(string board, int value);
    [DllImport("__Internal")] private static extern void MisketrGcAchievement(string identifier, double percent);
    [DllImport("__Internal")] private static extern void MisketrGcShow();
#endif

    private static bool asked;
    private static string lastReported = "";

    // Uygulama başında bir kez: Game Center girişi (gerekirse Apple'ın penceresi).
    public static void Authenticate()
    {
        if (!MisketrCloud.Enabled || asked) return;
        asked = true;
#if UNITY_IOS && !UNITY_EDITOR
        MisketrGcAuthenticate();
#endif
    }

    public static bool Ready
    {
        get
        {
#if UNITY_IOS && !UNITY_EDITOR
            return MisketrCloud.Enabled && MisketrGcReady();
#else
            return false;
#endif
        }
    }

    // Değerler değiştiyse skorları ve başarım yüzdelerini gönderir. Sık çağrılabilir.
    public static void ReportIfChanged()
    {
        if (!Ready) return;
        int count = MahalleProfile.AchievementNames.Length;
        var percents = new double[count];
        var key = new System.Text.StringBuilder();
        key.Append(MahalleProfile.TotalStars).Append('|').Append(MahalleProfile.Data.bestShot);
        for (int i = 0; i < count; i++)
        {
            percents[i] = Mathf.Clamp01((float)MahalleProfile.AchievementProgress(i) / MahalleProfile.AchievementTargets[i]) * 100.0;
            key.Append('|').Append(percents[i].ToString("0"));
        }
        if (key.ToString() == lastReported) return;
        lastReported = key.ToString();
#if UNITY_IOS && !UNITY_EDITOR
        MisketrGcScore(StarsBoard, MahalleProfile.TotalStars);
        if (MahalleProfile.Data.bestShot > 0) MisketrGcScore(BestShotBoard, MahalleProfile.Data.bestShot);
        for (int i = 0; i < count; i++) if (percents[i] > 0) MisketrGcAchievement(AchievementId(i), percents[i]);
#endif
    }

    public static void ShowDashboard()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (MisketrCloud.Enabled) MisketrGcShow();
#endif
    }
}
