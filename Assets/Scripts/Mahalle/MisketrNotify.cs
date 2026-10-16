using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
#endif

// GÜNLÜK HATIRLATMA
// Her gün aynı saatte "bugünün bölümü hazır" bildirimi kurar. Sunucu yok.
// İzin ilk kez günün bölümü açıldığında isteniyor; Ayarlar'dan kapatılabilir.
public static class MisketrNotify
{
    public const int Hour = 19, Minute = 0;   // akşam 19:00

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void MisketrRequestNotifyPermission();
    [DllImport("__Internal")] private static extern void MisketrScheduleDaily(int hour, int minute, string title, string body);
    [DllImport("__Internal")] private static extern void MisketrCancelDaily();
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    // ANDROID: Unity Mobile Notifications. Tek kanal, sabit kimlikli tekrarlayan bildirim (her gün 19:00).
    const string ChannelId = "misko_daily";
    const int NotifyId = 1900;
    static bool channelReady;
    static PermissionRequest permission;   // Android 13+ POST_NOTIFICATIONS isteği; referans tutulmalı
    static void EnsureChannel()
    {
        if (channelReady) return;
        channelReady = true;
        AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
        {
            Id = ChannelId,
            Name = L.T("Günlük hatırlatma"),
            Importance = Importance.Default,
            Description = L.T("Günün bölümü hazır olduğunda haber verir."),
        });
    }
#endif

    public static void RequestPermission()
    {
#if UNITY_IOS && !UNITY_EDITOR
        MisketrRequestNotifyPermission();
#elif UNITY_ANDROID && !UNITY_EDITOR
        EnsureChannel();
        if (AndroidNotificationCenter.UserPermissionToPost != PermissionStatus.Allowed) permission = new PermissionRequest();
#else
        Debug.Log("BILDIRIM: izin istenirdi (sadece iPhone'da).");
#endif
    }

    // Ayar değiştiğinde veya oyun açıldığında çağrılır.
    public static void Refresh()
    {
        if (MahalleProfile.Data.notify)
        {
#if UNITY_IOS && !UNITY_EDITOR
            MisketrScheduleDaily(Hour, Minute, L.T("Günün bölümü hazır"), L.T("Bugünün bölümünü oyna, serini sürdür."));
#elif UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                EnsureChannel();
                var now = System.DateTime.Now;
                var fire = new System.DateTime(now.Year, now.Month, now.Day, Hour, Minute, 0);
                if (fire <= now) fire = fire.AddDays(1);
                AndroidNotificationCenter.CancelNotification(NotifyId);
                AndroidNotificationCenter.SendNotificationWithExplicitID(new AndroidNotification
                {
                    Title = L.T("Günün bölümü hazır"),
                    Text = L.T("Bugünün bölümünü oyna, serini sürdür."),
                    FireTime = fire,
                    RepeatInterval = System.TimeSpan.FromDays(1),
                }, ChannelId, NotifyId);
            }
            catch (System.Exception e) { Debug.LogWarning("Bildirim kurulamadi: " + e.Message); }
#else
            Debug.Log("BILDIRIM: gunluk hatirlatma kurulurdu " + Hour + ":" + Minute.ToString("00"));
#endif
        }
        else
        {
#if UNITY_IOS && !UNITY_EDITOR
            MisketrCancelDaily();
#elif UNITY_ANDROID && !UNITY_EDITOR
            try { AndroidNotificationCenter.CancelNotification(NotifyId); } catch (System.Exception) { }
#endif
        }
    }
}
