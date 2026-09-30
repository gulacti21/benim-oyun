using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// ZORUNLU GÜNCELLEME (2026-10-01, kullanıcı kararı: "yumuşak değil, zorunlu"). Menü açılınca,
// internet VARSA bir kez Apple'ın herkese açık sürüm servisine (iTunes Lookup) sorulur.
// App Store'daki sürüm telefondakinden yeniyse "GÜNCELLEME GEREKLİ" penceresi çıkar; tek düğme
// App Store'u açar, pencere kapatılamaz, oyuna girilemez.
// İnternet yoksa, istek başarısızsa ya da oyun henüz App Store'da değilse (TestFlight) hiçbir
// şey göstermez: oyun internetsiz oynanır. Kişisel veri gönderilmez (App Privacy değişmez).
public static class UpdateCheck
{
    public const string AppId = "6817696906";
    public const string StoreUrl = "https://apps.apple.com/app/id" + AppId;
    private const string LookupUrl = "https://itunes.apple.com/lookup?id=" + AppId + "&country=tr";
    private static bool done;

    [Serializable] private class Lookup { public Result[] results; }
    [Serializable] private class Result { public string version; }

    // show(mağazadaki sürüm): sadece telefondaki sürüm eskiyse çağrılır.
    public static IEnumerator Run(Action<string> show)
    {
        if (done || Application.isEditor) yield break;
        done = true;
        if (Application.internetReachability == NetworkReachability.NotReachable) yield break;

        string store = null;
        using (var r = UnityWebRequest.Get(LookupUrl))
        {
            r.timeout = 6;
            yield return r.SendWebRequest();
            if (r.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var l = JsonUtility.FromJson<Lookup>(r.downloadHandler.text);
                    if (l != null && l.results != null && l.results.Length > 0) store = l.results[0].version;
                }
                catch (Exception) { }
            }
        }
        if (string.IsNullOrEmpty(store) || Compare(Application.version, store) >= 0) yield break;

        show(store);
    }

    // "1.2.10" > "1.2.9". Sayı olmayan parçalar 0 sayılır.
    public static int Compare(string a, string b)
    {
        var x = (a ?? "").Split('.'); var y = (b ?? "").Split('.');
        for (int i = 0; i < Math.Max(x.Length, y.Length); i++)
        {
            int p = i < x.Length && int.TryParse(x[i], out int px) ? px : 0;
            int q = i < y.Length && int.TryParse(y[i], out int qy) ? qy : 0;
            if (p != q) return p < q ? -1 : 1;
        }
        return 0;
    }
}
