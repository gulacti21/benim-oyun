using System;

// 1.1 GELİR: ödüllü reklam + boncuk satışı (kullanıcı kararı, 2026-10-14).
// Oyun bu sınıfı konuşur; reklam ağı (LevelPlay) ve mağaza (Unity IAP) arka uç olarak
// Ads / Store alanlarına bağlanır. Arka uç yoksa (editör, paket kurulmadan) her şey
// "hazır değil" döner ve oyun normal çalışır.
public interface IRewardedBackend
{
    bool Ready { get; }
    // Son yükleme denemesi "reklam yok" (no fill) ile bitti ve henüz reklam gelmedi.
    bool NoFill { get; }
    // done(true) sadece reklam sonuna kadar izlenip ödül hak edildiğinde.
    void Show(Action<bool> done);
}

public interface IStoreBackend
{
    bool Ready { get; }
    string PriceOf(string productId);   // yerel fiyat ("₺39,99"); bilinmiyorsa null
    string CurrencyOf(string productId);   // ISO para birimi ("TRY"); bilinmiyorsa null
    decimal AmountOf(string productId);    // sayısal fiyat (49.99); bilinmiyorsa 0
    // done(true, null) boncuk verilip kayıt alındığında; done(false, sebep) aksi halde.
    void Buy(string productId, Action<bool, string> done);
}

public static class Monetization
{
    // DENGE (kullanıcı kararı 2026-10-15): az boncuk, çok reklam. 10 × 8 = günde en fazla 80 boncuk
    // (ilk taslak 20 × 5 = 100'dü; satın almayı cazip tutmak için düşürüldü). Sonsuz artık boncuk vermiyor.
    public const int AdRewardBeads = 10, AdDailyLimit = 8;   // 1.2: 12 denendi; kullanıcı 2026-10-21 "8 kalsın" (site ve mağaza metniyle tutarlı)
    // Günün ödülü ×2 için arka arkaya izlenecek reklam (bedeli olsun diye 2; kullanıcı kararı 2026-10-15).
    public const int DailyX2Ads = 2;
    // Reklam ağı hazır mı (boncuk reklamı sınırından bağımsız: devam ve ×2 için).
    public static bool AdNetworkReady => Ads != null && Ads.Ready;
    // İnternet yoksa düğmede "HAZIRLANIYOR" yerine "İNTERNET GEREKLİ" yazar (sonsuza kadar beklemesin).
    public static bool Offline => UnityEngine.Application.internetReachability == UnityEngine.NetworkReachability.NotReachable;
    public static bool AdNoFill => Ads != null && !Ads.Ready && Ads.NoFill;
    // Reklam düğmesinin durumu: 0 hazırlanıyor, 1 reklam yok (ağ boş), 2 internet yok, 3 hazır.
    public static int AdState => AdNetworkReady ? 3 : Offline ? 2 : AdNoFill ? 1 : 0;
    public static void ShowAd(Action<bool> done)
    {
        if (!AdNetworkReady) { done?.Invoke(false); return; }
        Ads.Show(done);
    }

    public struct Pack { public string id; public int beads; public string fallbackPrice; public string label; }
    // Ürün kimlikleri App Store Connect'te birebir aynı açılmalı (Consumable).
    public static readonly Pack[] Packs =
    {
        new Pack{ id="com.gulacti.misko.boncuk150",  beads=150,  fallbackPrice="₺49,99",  label="Küçük Kese" },
        new Pack{ id="com.gulacti.misko.boncuk400",  beads=400,  fallbackPrice="₺99,99",  label="Orta Kese" },
        new Pack{ id="com.gulacti.misko.boncuk1000", beads=1000, fallbackPrice="₺199,99", label="Büyük Kese" },
    };

    public static IRewardedBackend Ads;
    public static IStoreBackend Store;

    // Bekleyen bir Buy() çağrısı olmadan gelen satın alma (ilk sandbox girişi, yarıda kalan satın alma,
    // "Satın almayı onayla" sonrası). Boncuk kayda yazıldıktan sonra çağrılır; arayüz tazelenir.
    public static event Action<int> UnsolicitedGrant;
    public static void RaiseUnsolicitedGrant(int beads) { UnsolicitedGrant?.Invoke(beads); }

    public static bool AdReady => Ads != null && Ads.Ready && MahalleProfile.AdsLeftToday > 0;
    public static bool StoreReady => Store != null && Store.Ready;

    public static int BeadsOf(string productId)
    {
        foreach (var p in Packs) if (p.id == productId) return p.beads;
        return 0;
    }

    public static string PriceOf(Pack p)
    {
        string s = Store != null ? Store.PriceOf(p.id) : null;
        return string.IsNullOrEmpty(s) ? p.fallbackPrice : s;
    }
    public static string CurrencyOf(Pack p) => Store != null ? Store.CurrencyOf(p.id) : null;
    public static decimal AmountOf(Pack p) => Store != null ? Store.AmountOf(p.id) : 0m;

    // Reklamı göster; sonuna kadar izlenirse boncuk eklenir ve kaydedilir.
    public static void WatchAdForBeads(Action<bool> done)
    {
        if (!AdReady) { done?.Invoke(false); return; }
        Ads.Show(ok =>
        {
            if (ok) MahalleProfile.GrantAdBeads(AdRewardBeads);
            done?.Invoke(ok);
        });
    }

    public static void Buy(Pack p, Action<bool, string> done)
    {
        if (!StoreReady) { done?.Invoke(false, "Mağaza şu an kullanılamıyor."); return; }
        Store.Buy(p.id, done);
    }
}
