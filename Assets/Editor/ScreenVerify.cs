using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// EKRAN TESTİ: menü tarafındaki her ekranı ve pencereyi dört farklı kayıtla kurar,
// kurulurken hata atan (çöken) ekran var mı bakar. Görünümü (renk, taşma) ÖLÇMEZ.
// Neden: 2026-09-24'te sıfırlanan kayıtla İstatistik açılınca çöküyordu; hiçbir test
// ekran açmadığı için yakalanmadı. MahalleVerify.Run içinden çağrılır (TestMode açık,
// gerçek kayda dokunmaz). Her ekran için yeni bir arayüz nesnesi kurulur, çünkü
// edit modda Destroy çalışmaz; önceki ekran DestroyImmediate ile silinir.
public static class ScreenVerify
{
    private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
    private static int checks;
    private static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception("CHECK FAILED: " + message); }

    public static int RunChecks()
    {
        checks = 0;
        var saves = new List<KeyValuePair<string, Func<MahalleSave>>>
        {
            new KeyValuePair<string, Func<MahalleSave>>("yeni oyuncu", () => { MahalleProfile.SetTestData(new MahalleSave()); return MahalleProfile.Data; }),
            new KeyValuePair<string, Func<MahalleSave>>("sıfırlanmış kayıt", () => { MahalleProfile.Reset(); return MahalleProfile.Data; }),
            new KeyValuePair<string, Func<MahalleSave>>("yarıda kalmış", Midway),
            new KeyValuePair<string, Func<MahalleSave>>("iki harita bitmiş", Finished),
        };
        var screens = new List<KeyValuePair<string, Action<MahalleUI, Type>>>
        {
            Screen("açılış", (ui, t) => Call(ui, t, "ShowTitle", true)),
            Screen("harita", (ui, t) => Home(ui, t, 0)),
            Screen("kesem", (ui, t) => Home(ui, t, 1)),
            Screen("görevler", (ui, t) => Home(ui, t, 2)),
            Screen("istatistik", (ui, t) => Home(ui, t, 3)),
            Screen("haritalar penceresi", (ui, t) => { Home(ui, t, 0); Call(ui, t, "OpenMaps"); }),
            Screen("ayarlar", (ui, t) => { Home(ui, t, 0); Call(ui, t, "Settings"); }),
            Screen("sıfırlama onayı", (ui, t) => { Home(ui, t, 0); Call(ui, t, "ResetPrompt"); }),
            Screen("harita geçişi", (ui, t) => { Home(ui, t, 0); Step((IEnumerator)t.GetMethod("MapIntro", F).Invoke(ui, new object[] { 1 })); }),
        };
        // Harita ekranı Memleket'te de kurulmalı (bölge 5-9).
        for (int d = Maps.FirstDistrict(1); d < Maps.TotalDistricts; d++)
        {
            int district = d;
            screens.Add(Screen("harita bölge " + d, (ui, t) => { t.GetField("district", F).SetValue(ui, district); Home(ui, t, 0); }));
        }

        foreach (var save in saves)
            foreach (var screen in screens)
            {
                // Kaydı fonksiyon kendisi kurar; sıfırlama gerçek Reset yolundan gelmeli
                // (SetTestData onarır, hatayı gizlerdi).
                save.Value();
                GameObject go = null;
                string error = null;
                try
                {
                    var ui = Build(out go, out var t);
                    screen.Value(ui, t);
                    Check(go.GetComponentsInChildren<Graphic>(true).Length > 5, "Screen: " + screen.Key + " drew something (" + save.Key + ")");
                    foreach (var text in go.GetComponentsInChildren<TextMeshProUGUI>(true))
                        Check(text.font != null, "Screen: " + screen.Key + " text has font '" + text.text + "' (" + save.Key + ")");
                }
                catch (Exception e)
                {
                    var inner = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
                    if (inner.Message.StartsWith("CHECK FAILED")) throw inner;
                    error = inner.GetType().Name + ": " + inner.Message + "\n" + inner.StackTrace;
                }
                finally { if (go != null) UnityEngine.Object.DestroyImmediate(go); }
                Check(error == null, "Screen: " + screen.Key + " opens without crashing (" + save.Key + ")\n" + error);
            }
        Debug.Log("SCREEN_VERIFY_OK: " + checks + " checks.");
        return checks;
    }

    private static KeyValuePair<string, Action<MahalleUI, Type>> Screen(string name, Action<MahalleUI, Type> open)
        => new KeyValuePair<string, Action<MahalleUI, Type>>(name, open);

    // MahalleUI.Start'ın arayüz kurulumu (müzik/bildirim gibi oynatma işleri hariç).
    private static MahalleUI Build(out GameObject go, out Type t)
    {
        go = new GameObject("Ekran testi");
        go.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1920);
        var ui = go.AddComponent<MahalleUI>();
        t = typeof(MahalleUI);
        t.GetField("font", F).SetValue(ui, Resources.Load<TMP_FontAsset>("Mahalle/Nunito SDF"));
        var root = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(go.transform, false); root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        t.GetField("root", F).SetValue(ui, root);
        t.GetField("district", F).SetValue(ui, MahalleProfile.NextLevelIn(0) / Campaign.PerDistrict);
        return ui;
    }

    private static void Home(MahalleUI ui, Type t, int tab)
    {
        t.GetField("tab", F).SetValue(ui, tab);
        Call(ui, t, "ShowHome");
    }

    private static void Call(MahalleUI ui, Type t, string name, params object[] args)
    {
        MethodInfo m = null;
        foreach (var candidate in t.GetMethods(F))
            if (candidate.Name == name && candidate.GetParameters().Length == args.Length) { m = candidate; break; }
        if (m == null) throw new Exception("CHECK FAILED: Screen: method " + name + " exists");
        m.Invoke(ui, args);
    }

    // Animasyonu elle ilerletir. Edit modda zaman akmadığı için döngüler bitmez;
    // amaç her adımda kurulan nesnelerin hata atmaması, sınırlı adım yeterli.
    private static void Step(IEnumerator routine)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(routine);
        for (int i = 0; i < 4000 && stack.Count > 0; i++)
        {
            var top = stack.Peek();
            if (!top.MoveNext()) { stack.Pop(); continue; }
            if (top.Current is IEnumerator nested) stack.Push(nested);
        }
    }

    private static MahalleSave Midway()
    {
        var d = new MahalleSave(); MahalleProfile.SetTestData(d);
        for (int i = 0; i < 30; i++) d.stars[i] = 1 + i % 3;
        d.districtPlays[1] = 9; d.knocked = 140; d.bestShot = 4; d.wins = 30; d.beads = 210;
        return d;
    }

    private static MahalleSave Finished()
    {
        var d = new MahalleSave(); MahalleProfile.SetTestData(d);
        for (int i = 0; i < Campaign.Count; i++) d.stars[i] = 3;
        for (int i = 0; i < Maps.PerMap; i++) d.maps[0].stars[i] = 3;
        for (int i = 0; i < d.districtRewards.Length; i++) d.districtRewards[i] = true;
        for (int i = 0; i < d.maps[0].districtRewards.Length; i++) d.maps[0].districtRewards[i] = true;
        d.maps[0].districtPlays[3] = 40;   // en çok oynanan bölge Memleket'te
        for (int i = 0; i < d.skins.Length; i++) d.skins[i] = true;
        d.knocked = 2200; d.bestShot = 9; d.wins = 300; d.beads = 5000; d.lastMap = 1; d.mapsAnnounced = 1;
        d.dailyStreak = 12; d.dailyBestStreak = 30;
        return d;
    }
}
