using System.Runtime.InteropServices;
using UnityEngine;

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

    public static void RequestPermission()
    {
#if UNITY_IOS && !UNITY_EDITOR
        MisketrRequestNotifyPermission();
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
#else
            Debug.Log("BILDIRIM: gunluk hatirlatma kurulurdu " + Hour + ":" + Minute.ToString("00"));
#endif
        }
        else
        {
#if UNITY_IOS && !UNITY_EDITOR
            MisketrCancelDaily();
#endif
        }
    }
}
