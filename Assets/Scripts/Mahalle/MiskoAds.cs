using System;
using System.Collections;
using System.Runtime.InteropServices;
using Unity.Services.LevelPlay;
using UnityEngine;

// 1.1 ÖDÜLLÜ REKLAM (Unity LevelPlay 9.5). Sadece oyuncunun kendi seçtiği ödüllü reklam;
// geçiş (interstitial) ve banner YOK. Monetization.Ads'e arka uç olarak bağlanır.
// Akış: ATT izni (iOS) -> LevelPlay.Init -> reklamı önceden yükle -> izlenince 20 boncuk.
public class MiskoAds : MonoBehaviour, IRewardedBackend
{
    public const string AppKey = "28ba299f5";                 // LevelPlay > Apps > Misko: Marble Game
    public const string RewardedAdUnitId = "u0iyqv60k7egixhe"; // Ad units > Boncuk Odulu (Rewarded, 20)

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
    }

    void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogWarning($"LevelPlay initialization failed: {error.ErrorMessage}");
        Invoke(nameof(RetryInit), 60f);
    }
    void RetryInit() { LevelPlay.Init(AppKey); }

    void LoadAd() { if (rewarded != null) rewarded.LoadAd(); }

    public void Show(Action<bool> done)
    {
        if (!Ready) { done?.Invoke(false); return; }
        pending = done; rewardEarned = false; closed = false;
        rewarded.ShowAd();
    }

    void OnAdLoaded(LevelPlayAdInfo info) { }
    void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Rewarded ad failed to load: {error.ErrorMessage}");
        CancelInvoke(nameof(LoadAd));
        Invoke(nameof(LoadAd), 30f);
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
