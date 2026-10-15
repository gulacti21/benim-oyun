using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

// 1.1 BONCUK SATIŞI (Unity IAP 5). Üç tüketilebilir paket (Monetization.Packs).
// Akış: Connect -> FetchProducts -> PurchaseProduct -> OnPurchasePending:
//   boncuğu ver + KAYDET -> ancak ondan sonra ConfirmPurchase. Kayıt yoksa onaylanmaz,
//   Apple satın almayı bir sonraki açılışta tekrar teslim eder (boncuk kaybolmaz).
// Aynı sipariş iki kez gelirse (TransactionID defteri) boncuk tekrar verilmez.
public class MiskoStore : MonoBehaviour, IStoreBackend
{
    static MiskoStore instance;
    StoreController store;
    bool connected, productsReady;
    readonly Dictionary<string, Action<bool, string>> waiting = new Dictionary<string, Action<bool, string>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("MiskoStore");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MiskoStore>();
    }

    public bool Ready => connected && productsReady;

    async void Awake()
    {
        store = UnityIAPServices.StoreController();
        // Bütün olaylar Connect'ten ÖNCE: önceki oturumdan kalan satın almalar hemen gelebilir.
        store.OnPurchasePending += OnPurchasePending;
        store.OnPurchaseConfirmed += OnPurchaseConfirmed;
        store.OnPurchaseFailed += OnPurchaseFailed;
        store.OnPurchaseDeferred += OnPurchaseDeferred;
        store.OnStoreConnected += OnStoreConnected;
        store.OnStoreDisconnected += OnStoreDisconnected;
        store.OnProductsFetched += OnProductsFetched;
        store.OnProductsFetchFailed += OnProductsFetchFailed;
        store.OnPurchasesFetched += OnPurchasesFetched;
        store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
        try { await store.Connect(); }
        catch (Exception e) { Debug.LogWarning("IAP connect failed: " + e.Message); }
    }

    void OnStoreConnected()
    {
        connected = true;
        var defs = Monetization.Packs.Select(p => new ProductDefinition(p.id, ProductType.Consumable)).ToList();
        store.FetchProducts(defs);
        store.FetchPurchases();   // onaylanmamış eski satın almalar OnPurchasePending ile tekrar gelir
    }
    void OnStoreDisconnected(StoreConnectionFailureDescription failure)
    {
        connected = false;
        Debug.LogWarning("IAP store disconnected: " + failure.Message);
    }
    void OnProductsFetched(List<Product> products)
    {
        productsReady = products != null && products.Count > 0;
        Monetization.Store = this;
    }
    void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogWarning("IAP product fetch failed: " + failure.FailureReason);
        Monetization.Store = this;   // fiyatlar yedek metinden gösterilir; satın alma Ready=false ile kapalı
    }
    void OnPurchasesFetched(Orders orders) { }
    void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        Debug.LogWarning("IAP purchases fetch failed: " + failure.Message);
    }

    public string PriceOf(string productId)
    {
        var p = store != null ? store.GetProductById(productId) : null;
        return p != null && p.availableToPurchase ? p.metadata.localizedPriceString : null;
    }

    public string CurrencyOf(string productId)
    {
        var p = store != null ? store.GetProductById(productId) : null;
        return p != null ? p.metadata.isoCurrencyCode : null;
    }

    public decimal AmountOf(string productId)
    {
        var p = store != null ? store.GetProductById(productId) : null;
        return p != null ? p.metadata.localizedPrice : 0m;
    }

    public void Buy(string productId, Action<bool, string> done)
    {
        var p = store != null ? store.GetProductById(productId) : null;
        if (!Ready || p == null || !p.availableToPurchase) { done?.Invoke(false, L.T("Mağaza şu an kullanılamıyor.")); return; }
        waiting[productId] = done;
        store.PurchaseProduct(p);
    }

    void OnPurchasePending(PendingOrder order)
    {
        var item = order.CartOrdered.Items().FirstOrDefault();
        string id = item != null && item.Product != null ? item.Product.definition.id : null;
        int beads = Monetization.BeadsOf(id);
        string tx = order.Info != null ? order.Info.TransactionID : null;
        if (string.IsNullOrEmpty(tx)) tx = id + "@" + (order.Info != null ? order.Info.Receipt?.GetHashCode().ToString() : "?");

        if (beads <= 0) { store.ConfirmPurchase(order); return; }   // tanımadığımız ürün: sadece kapat
        bool granted = MahalleProfile.GrantPurchase(tx, beads);       // içeride Save() var
        // Daha önce verilmiş (granted=false) ya da şimdi verildi: her iki durumda da artık onaylanabilir.
        store.ConfirmPurchase(order);
        // Bekleyen çağrı yoksa (ör. ilk satın almada sandbox girişi araya girdi) arayüze ayrıca haber ver,
        // yoksa boncuk kayda yazılır ama ekran ancak oyun yeniden açılınca güncellenirdi (TestFlight, 2026-10-15).
        if (!Callback(id, granted, granted ? null : "") && granted) Monetization.RaiseUnsolicitedGrant(beads);
    }
    void OnPurchaseConfirmed(Order order)
    {
        if (order is FailedOrder failed)
            Debug.LogWarning($"IAP confirm failed: {failed.FailureReason} - {failed.Details}");
    }
    void OnPurchaseFailed(FailedOrder failed)
    {
        var item = failed.CartOrdered.Items().FirstOrDefault();
        string id = item != null && item.Product != null ? item.Product.definition.id : null;
        string why = failed.FailureReason == PurchaseFailureReason.UserCancelled ? "" : L.T("Satın alma tamamlanamadı.");
        Callback(id, false, why);
    }
    void OnPurchaseDeferred(DeferredOrder deferred)
    {
        var item = deferred.CartOrdered.Items().FirstOrDefault();
        string id = item != null && item.Product != null ? item.Product.definition.id : null;
        Callback(id, false, L.T("Satın alma onay bekliyor. Onaylanınca boncuklar kesene eklenir."));
    }
    bool Callback(string id, bool ok, string why)
    {
        if (id == null || !waiting.TryGetValue(id, out var cb)) return false;
        waiting.Remove(id);
        cb?.Invoke(ok, why);
        return true;
    }

    // Uygulama öne gelince onaylanmamış satın almaları tekrar iste (ör. ödeme onayı dışarıda tamamlandı).
    void OnApplicationFocus(bool focus)
    {
        if (focus && connected && store != null) store.FetchPurchases();
    }
}
