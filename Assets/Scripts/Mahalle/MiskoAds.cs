using System;
using System.Collections;
using System.Runtime.InteropServices;
using Unity.Services.LevelPlay;
using UnityEngine;

// 1.1 ÖDÜLLÜ REKLAM (Unity LevelPlay 9.5). Sadece oyuncunun kendi seçtiği ödüllü reklam;
// geçiş (interstitial) ve banner YOK. Monetization.Ads'e arka uç olarak bağlanır.
// Akış: ATT izni (iOS) -> LevelPlay.Init -> reklamı önceden yükle -> izlenince ödül (Monetization).
public class MiskoAds : MonoBehaviour, IRewardedBackend
{
    // LevelPlay'de her platform ayrı uygulama: iOS ve Android anahtarları/reklam birimleri farklı.
    public const string IosAppKey = "28ba299f5";                 // LevelPlay > Apps > Misko: Marble Game (iOS)
    public const string IosRewardedAdUnitId = "u0iyqv60k7egixhe"; // Ad units > Boncuk Odulu (Rewarded; paneldeki miktar oyunda kullanılmaz)
    public const string AndroidAppKey = "28c6cdc15";              // LevelPlay > Apps > Misko: Marble Game (Android)
    public const string AndroidRewardedAdUnitId = "tv2l688pyoxtna3w"; // Ad units > Boncuk Odulu (Android; Unity Ads Game ID 6206496, Rewarded_Android)
#if UNITY_ANDROID
    public const string AppKey = AndroidAppKey, RewardedAdUnitId = AndroidRewardedAdUnitId;
#else
    public const string AppKey = IosAppKey, RewardedAdUnitId = IosRewardedAdUnitId;
#endif

    static MiskoAds instance;
    LevelPlayRewardedAd rewarded;
    bool initDone, rewardEarned, closed;
    Action<bool> pending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("MiskoAds");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MiskoAds>();
    }

    public bool Ready => initDone && rewarded != null && rewarded.IsAdReady();
    bool noFill;
    public bool NoFill => noFill;

#if UNITY_IOS && !UNITY_EDITOR
    delegate void AttCallback(int status);
    [DllImport("__Internal")] static extern int _ATT_GetStatus();
    [DllImport("__Internal")] static extern void _ATT_RequestPermission(AttCallback callback);
    static bool attDone;
    [AOT.MonoPInvokeCallback(typeof(AttCallback))]
    static void OnAtt(int status) { attDone = true; }
#endif

    IEnumerator Start()
    {
        // Platform anahtarı henüz yoksa (Android paneli kurulmadan) reklam sistemi hiç başlamaz; düğmeler "HAZIRLANIYOR".
        if (string.IsNullOrEmpty(AppKey) || string.IsNullOrEmpty(RewardedAdUnitId)) yield break;
        // Açılış animasyonu ve menü otursun; ATT penceresi ilk saniyede çıkmasın.
        yield return new WaitForSecondsRealtime(2f);
#if UNITY_IOS && !UNITY_EDITOR
        if (_ATT_GetStatus() == 0)   // 0 = henüz sorulmadı
        {
            attDone = false;
            _ATT_RequestPermission(OnAtt);
            float t = 0;
            while (!attDone && t < 120f) { t += Time.unscaledDeltaTime; yield return null; }
        }
#endif
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        LevelPlay.Init(AppKey);
    }

    void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay SDK initialized successfully");
        rewarded = new LevelPlayRewardedAd(RewardedAdUnitId);
        rewarded.OnAdLoaded += OnAdLoaded;
        rewarded.OnAdLoadFailed += OnAdLoadFailed;
        rewarded.OnAdDisplayed += OnAdDisplayed;
        rewarded.OnAdDisplayFailed += OnAdDisplayFailed;
        rewarded.OnAdRewarded += OnAdRewarded;
        rewarded.OnAdClosed += OnAdClosed;
        initDone = true;
        Monetization.Ads = this;
        LoadAd();   // önceden yükle: oyun sonunda düğme hazır olsun
        StartCoroutine(Watchdog());
    }

    // BEKÇİ (build 13, 2026-10-15): telefonda yükleme isteğine ne "yüklendi" ne "başarısız" cevabı
    // gelmediği görüldü; o zaman hiçbir şey yeniden denemiyor, düğme sonsuza kadar HAZIRLANIYOR
    // kalıyordu. 5 sn'de bir: reklam yoksa ve istek yoksa ya da 20 sn'dir cevapsızsa yeniden iste.
    IEnumerator Watchdog()
    {
        var wait = new WaitForSecondsRealtime(5f);
        while (true)
        {
            yield return wait;
            if (rewarded == null || pending != null) continue;
            bool ready = rewarded.IsAdReady();
            if (ready) { loading = false; continue; }
            if (loading && Time.realtimeSinceStartup - loadStart < 20f) continue;
            if (IsInvoking(nameof(LoadAd))) continue;
            Debug.Log($"MiskoAds watchdog: reklam yok (loading={loading}), yeniden isteniyor");
            loading = false;
            LoadAd();
        }
    }

    void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogWarning($"LevelPlay initialization failed: {error.ErrorMessage}");
        Invoke(nameof(RetryInit), 60f);
    }
    void RetryInit() { LevelPlay.Init(AppKey); }

    float loadStart;
    bool loading;   // aynı anda iki yükleme isteği gitmesin (kapanış + öne gelme aynı anda tetikler)
    void LoadAd()
    {
        if (rewarded == null || rewarded.IsAdReady()) return;
        // Cevapsız kalan yükleme 20 sn sonra yok sayılır, yoksa düğme sonsuza kadar beklerdi.
        if (loading && Time.realtimeSinceStartup - loadStart < 20f) return;
        loading = true; loadStart = Time.realtimeSinceStartup;
        Debug.Log("MiskoAds: LoadAd istendi");
        rewarded.LoadAd();
    }

    // Yükleme başarısız olursa kademeli yeniden dene: 5, 10, 20, 30, 30... sn (eskiden hep 30 sn'ydi,
    // düğme yarım dakika "HAZIRLANIYOR" kalıyordu). Başarılı yüklemede sıfırlanır.
    int loadFails;
    float RetryDelay() => loadFails <= 1 ? 5f : loadFails == 2 ? 10f : loadFails == 3 ? 20f : 30f;

    // Uygulama öne gelince reklam yoksa hemen yükle (arka planda süresi dolmuş/bağlantı yeni gelmiş olabilir).
    void OnApplicationFocus(bool focus)
    {
        if (!focus || !initDone || rewarded == null || rewarded.IsAdReady() || pending != null) return;
        CancelInvoke(nameof(LoadAd));
        LoadAd();
    }

    public void Show(Action<bool> done)
    {
        if (!Ready) { done?.Invoke(false); return; }
        pending = done; rewardEarned = false; closed = false;
        rewarded.ShowAd();
    }

    void OnAdLoaded(LevelPlayAdInfo info) { loading = false; loadFails = 0; noFill = false; Debug.Log("MiskoAds: reklam yuklendi"); }
    void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Rewarded ad failed to load: {error.ErrorMessage}");
        loading = false;
        loadFails++;
        noFill = true;   // "no fill" ya da başka sebep: düğmede "ŞU AN REKLAM YOK"; denemeye devam edilir
        CancelInvoke(nameof(LoadAd));
        Invoke(nameof(LoadAd), RetryDelay());
    }
    void OnAdDisplayed(LevelPlayAdInfo info) { AudioListener.pause = true; }
    void OnAdDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
    {
        Debug.LogWarning($"Rewarded ad failed to display: {error.ErrorMessage}");
        AudioListener.pause = false;
        Finish(false);
        LoadAd();
    }
    // Ödül ve kapanış hangi sırayla gelirse gelsin tek sonuç verilir.
    void OnAdRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
    {
        rewardEarned = true;
        if (closed) Finish(true);
    }
    void OnAdClosed(LevelPlayAdInfo info)
    {
        closed = true;
        AudioListener.pause = false;
        if (rewardEarned) Finish(true);
        else StartCoroutine(WaitLateReward());   // bazı ağlar ödülü kapanıştan sonra gönderir
        LoadAd();
    }
    IEnumerator WaitLateReward()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        Finish(rewardEarned);
    }
    void Finish(bool ok)
    {
        var cb = pending; pending = null;
        cb?.Invoke(ok);
    }

    void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
        if (rewarded != null)
        {
            rewarded.OnAdLoaded -= OnAdLoaded;
            rewarded.OnAdLoadFailed -= OnAdLoadFailed;
            rewarded.OnAdDisplayed -= OnAdDisplayed;
            rewarded.OnAdDisplayFailed -= OnAdDisplayFailed;
            rewarded.OnAdRewarded -= OnAdRewarded;
            rewarded.OnAdClosed -= OnAdClosed;
            rewarded.DestroyAd();
        }
    }
}
