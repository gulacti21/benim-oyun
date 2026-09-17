using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(200)]
public class MahalleUI : MonoBehaviour
{
    private static readonly Color Paper=new Color(.95f,.91f,.82f), Ink=new Color(.16f,.25f,.23f), Muted=new Color(.44f,.46f,.38f), Gold=new Color(.93f,.65f,.25f), Cream=new Color(1,.97f,.89f), Line=new Color(.83f,.8f,.7f);
    private RectTransform root,page,modal;
    private TMP_FontAsset font;
    private LevelController controller;
    private TextMeshProUGUI scoreLabel,shotsLabel,powerLabel,beadsLabel,hintLabel;
    private MahalleGraphic powerFill;
    private RectTransform hand;
    private int district,tab,lastScore=-1,lastShots=-1;
    // Açılış ekranı uygulama başına BİR kez gösterilir. Bölümden çıkıp
    // mahalleye dönmek LevelSelect sahnesini yeniden yüklüyor; bu bayrak
    // olmadan oyuncu her dönüşünde tebeşir animasyonunu baştan izlerdi.
    // static olduğu için sahne değişimlerini aşar, uygulama kapanınca sıfırlanır.
    private static bool titleShown;
    // Duellodan "ANA MENU" ile cikinca harita degil baslik ekrani acilsin.
    private static bool returnToTitle;
    private DuelController duel;
    private TextMeshProUGUI duelTurnLabel,duelCountA,duelCountB,duelTurnsLabel;
    private bool duelResultShown;
    private int duelScreenShown=-1;
    private TextMeshProUGUI placeCount,placeHint;
    private TextMeshProUGUI tossWho,tossHint,tossScoreA,tossScoreB;
    private TextMeshProUGUI wellCookA,wellCookB,wellRoundLabel;
    private GameObject tossGo;
    private bool resultShown;
    public RectTransform Root => root;
    private void Start()
    {
        font=Resources.Load<TMP_FontAsset>("Mahalle/Nunito SDF");
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=0;
        gameObject.AddComponent<GraphicRaycaster>();
        // TAM EKRAN ZEMİN: güvenli alanın dışı (çentik/Dynamic Island ve ev çubuğu
        // şeritleri) ekranın zemin rengiyle boyanır. İçerik güvenli alanda kalır.
        bleedTop=Bleed("Üst taşma",new Vector2(0,.5f),Vector2.one);
        bleedBottom=Bleed("Alt taşma",Vector2.zero,new Vector2(1,.5f));
        root=Rect("SafeArea",transform);Stretch(root);root.gameObject.AddComponent<SafeAreaFit>();
        Canvas.ForceUpdateCanvases();
        controller=FindFirstObjectByType<LevelController>();
        if(controller!=null)controller.AnchorRefunded+=()=>Toast("Misketin işe yarar bir yerde kalmadı. Hakkın iade edildi.");
        district=MahalleProfile.NextLevel/Campaign.PerDistrict;
        // Harita/başlık sahnesine dönüldüyse öğretici modu kapanmış olmalı.
        if(controller==null)GameSession.TutorialMode=false;
        if(DuelSession.Active&&controller!=null)StartDuel();
        else if(controller!=null)ShowGame();
        else if(returnToTitle){returnToTitle=false;ShowTitle(true);}
        else if(titleShown)ShowHome();
        else ShowTitle();
        // Test yapisi damgasi. Bu yazi ekranda goruniyorsa bu build yayinlanamaz.
        if(MahalleProfile.TestUnlockAllLevels||MahalleProfile.TestInfiniteBeads)
        {
            var stamp=Text(root,MahalleProfile.TestUnlockAllLevels&&MahalleProfile.TestInfiniteBeads
                ?"TEST · TÜM BÖLÜMLER AÇIK · SINIRSIZ BONCUK"
                :MahalleProfile.TestInfiniteBeads?"TEST · SINIRSIZ BONCUK":"TEST · TÜM BÖLÜMLER AÇIK",
                0,0,760,44,25,new Color(1f,.45f,.25f,.85f),TextAlignmentOptions.Center);
            stamp.rectTransform.anchorMin=stamp.rectTransform.anchorMax=new Vector2(.5f,0f);
            stamp.rectTransform.pivot=new Vector2(.5f,0f);
            stamp.rectTransform.anchoredPosition=new Vector2(0,6);
            stamp.transform.SetAsLastSibling();
        }
    }
    private RectTransform Rect(string name,Transform parent)
    {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return (RectTransform)go.transform;}
    private static void Stretch(RectTransform r) {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    private static void Place(RectTransform r,float x,float y,float w,float h)
    {r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    private static void Bottom(RectTransform r,float x,float y,float w,float h)
    {r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
    private MahalleGraphic Art(Transform parent,string name,MahalleGraphic.Shape shape,float x,float y,float w,float h,Color color,bool hit=false)
    {var r=Rect(name,parent);Place(r,x,y,w,h);var g=r.gameObject.AddComponent<MahalleGraphic>();g.shape=shape;g.color=color;g.raycastTarget=hit;return g;}
    private MahalleGraphic Panel(Transform parent,string name,float x,float y,float w,float h,Color color,bool hit=false)
    {return Art(parent,name,MahalleGraphic.Shape.Panel,x,y,w,h,color,hit);}
    private TextMeshProUGUI Text(Transform parent,string value,float x,float y,float w,float h,float size,Color color,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft)
    {
        var r=Rect(value,parent);Place(r,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.text=L.T(value);t.font=font;t.fontSize=size;t.color=color;t.alignment=align;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;
    }
    private Button Button(Transform parent,string name,float x,float y,float w,float h,Color bg,Action action)
    {
        var g=Panel(parent,name,x,y,w,h,bg,true);var b=g.gameObject.AddComponent<Button>();b.targetGraphic=g;
        var colors=b.colors;colors.pressedColor=new Color(.8f,.86f,.8f);colors.selectedColor=Color.white;colors.highlightedColor=Color.white;colors.disabledColor=new Color(.65f,.65f,.65f);b.colors=colors;
        b.onClick.AddListener(()=>{if(SfxPlayer.Instance!=null)SfxPlayer.Instance.PlayUiTap();action();});return b;
    }
    private Button LabelButton(Transform parent,string title,float x,float y,float w,float h,Color bg,Color fg,Action action,float size=34)
    {var b=Button(parent,title,x,y,w,h,bg,action);Text(b.transform,title,12,0,w-24,h,size,fg,TextAlignmentOptions.Center);return b;}
    private Image bleedTop,bleedBottom;
    private Image Bleed(string name,Vector2 min,Vector2 max)
    {
        var r=Rect(name,transform);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
        var img=r.gameObject.AddComponent<Image>();img.color=Color.clear;img.raycastTarget=false;return img;
    }
    private void SetBleed(Color top,Color bottom)
    {if(bleedTop!=null)bleedTop.color=top;if(bleedBottom!=null)bleedBottom.color=bottom;}
    private void ClearPage()
    {CloseModal(false);SetBleed(Color.clear,Color.clear);if(page!=null){page.gameObject.SetActive(false);Destroy(page.gameObject);}page=Rect("Ekran",root);Stretch(page);}
    private void Background(Color color)
    {var g=Panel(page,"Zemin",0,0,1080,2500,color);g.radius=0;Stretch(g.rectTransform);SetBleed(color,color);}
    private void Header(string subtitle)
    {
#if UNITY_EDITOR
        if(MahalleProfile.PreviewMode)subtitle="ÖNİZLEME · TÜM BÖLÜMLER AÇIK · KAYIT YAPILMAZ";
#endif
        Text(page,"MİSKETR",48,24,630,76,64,Ink);
        Text(page,subtitle,50,104,740,42,27,Muted);
        var wallet=Panel(page,"Boncuk",805,35,225,76,Ink);
        Art(wallet.transform,"Boncuk simgesi",MahalleGraphic.Shape.Marble,18,16,44,44,Gold);
        beadsLabel=Text(wallet.transform,MahalleProfile.Beads.ToString(),78,0,130,76,36,Cream);
    }
    private void Nav()
    {
        var bg=Panel(page,"Alt menü",24,0,1032,132,new Color(.21f,.32f,.28f));
        bg.colorB=new Color(.11f,.18f,.16f);bg.radius=34;bg.shadow=14;bg.highlight=true;
        Bottom(bg.rectTransform,24,18,1032,132);
        // Dördüncü sekme "MENÜ": açılış ekranına (başlık menüsü) döner.
        string[] names={"MENÜ","MAHALLE","KESEM","İSTATİSTİK","GÖREVLER"};
        int[] ids={-1,0,1,3,2};
        var shapes=new[]{MahalleGraphic.Shape.Chevron,MahalleGraphic.Shape.TabMap,MahalleGraphic.Shape.Bag,MahalleGraphic.Shape.Star,MahalleGraphic.Shape.TabTask};
        var faded=new Color(1,.97f,.89f,.6f);
        for(int i=0;i<5;i++)
        {
            int id=ids[i];bool on=id==tab;
            System.Action act=id<0?(System.Action)(()=>ShowTitle(true)):()=>{tab=id;ShowHome();};
            var b=Button(bg.transform,names[i],10+i*202,10,196,112,on?new Color(1,1,1,.1f):new Color(1,1,1,0),act);
            b.GetComponent<MahalleGraphic>().radius=26;
            b.gameObject.AddComponent<MahalleTap>();
            var icon=id<0?Art(b.transform,"Simge",shapes[i],72,18,52,52,faded):id==3?Art(b.transform,"Simge",shapes[i],67,14,62,62,on?Gold:faded):Art(b.transform,"Simge",shapes[i],63,12,70,64,on?Gold:faded);
            icon.accent=on?Gold:faded;
            if(id<0)icon.mirror=true;
            Text(b.transform,names[i],4,80,188,36,id==3?21:24,on?Gold:faded,TextAlignmentOptions.Center);
        }
    }
    // ---------------------------------------------------------------
    // AÇILIŞ EKRANI
    // Mahallede biri yere tebeşirle çemberi çiziyor, misketler çembere
    // düşüyor, sonra menü açılıyor. Hiçbir görsel dosya kullanılmaz;
    // çember de misketler de MahalleGraphic ile çizilir.
    // ---------------------------------------------------------------
    private void ShowTitle() { ShowTitle(false); }

    private void ShowTitle(bool instant)
    {
        titleShown=true;
        ClearPage();
        Background(new Color(.14f, .12f, .10f));

        var ring = Art(page, "Tebeşir çemberi", MahalleGraphic.Shape.ChalkRing, 190, 430, 700, 700, new Color(1, .98f, .92f, .92f));
        ring.stroke = 9f;
        ring.progress = 0f;

        // Tebeşirin ucu: çizerken çemberin üstünde gezer, bitince kaybolur.
        var tip = Art(page, "Tebeşir ucu", MahalleGraphic.Shape.Circle, 0, 0, 26, 26, new Color(1, 1, .96f, .9f));
        tip.radius = 13;

        // Çemberin içindeki misket üçgeni, o da tebeşirle.
        var tri = Art(page, "Tebeşir üçgeni", MahalleGraphic.Shape.ChalkTriangle, 320, 580, 440, 400, new Color(1, .98f, .92f, .78f));
        tri.stroke = 7f;
        tri.progress = 0f;

        // Üçgenin içine dizilen altı misket: 1-2-3 ıstaka.
        float[,] spots = { { 540, 682 }, { 499, 764 }, { 581, 764 }, { 458, 846 }, { 540, 846 }, { 622, 846 } };
        var marbles = new MahalleGraphic[spots.GetLength(0)];
        for (int i = 0; i < marbles.Length; i++)
        {
            var col = Campaign.SkinColors[i % 6];
            marbles[i] = Art(page, "Misket " + i, MahalleGraphic.Shape.Marble, spots[i, 0] - 37, spots[i, 1] - 37, 74, 74, col);
            marbles[i].accent = Color.Lerp(col, Color.white, .55f);
            marbles[i].transform.localScale = Vector3.zero;
        }

        // Başlık ve menü: çember kapanana kadar görünmezler.
        var head = Rect("Başlık", page); Stretch(head);
        var headFade = head.gameObject.AddComponent<CanvasGroup>(); headFade.alpha = 0f;
        Text(head, "MİSKETR", 0, 150, 1080, 150, 116, new Color(1, .98f, .92f), TextAlignmentOptions.Center);
        Text(head, "mahallenin en iyi nişancısı kim?", 0, 292, 1080, 50, 31, new Color(1, .97f, .89f, .62f), TextAlignmentOptions.Center);

        var menu = Rect("Menü", page); Stretch(menu);
        var menuFade = menu.gameObject.AddComponent<CanvasGroup>(); menuFade.alpha = 0f; menuFade.interactable = false; menuFade.blocksRaycasts = false;

        int next = MahalleProfile.NextLevel;
        bool resume = next > 0 && MahalleProfile.Data.stars[0] > 0;
        string playText = resume
            ? L.T("DEVAM ET") + " · " + L.Up(L.T(Campaign.Districts[next / Campaign.PerDistrict])) + " " + (next % Campaign.PerDistrict + 1).ToString("00")
            : "OYNA";
        LabelButton(menu, playText, 90, 1245, 900, 132, Gold, new Color(.16f, .20f, .16f), () => { tab = 0; district = next / Campaign.PerDistrict; ShowHome(); }, resume ? 38 : 46);
        LabelButton(menu, "KESEM", 90, 1398, 435, 104, new Color(.24f, .34f, .30f), Cream, () => { tab = 1; ShowHome(); }, 32);
        LabelButton(menu, "GÖREVLER", 555, 1398, 435, 104, new Color(.24f, .34f, .30f), Cream, () => { tab = 2; ShowHome(); }, 32);
        LabelButton(menu, "ONLİNE", 90, 1523, 900, 104, new Color(.30f, .42f, .36f), Cream, ShowOnlineMenu, 32);
        LabelButton(menu, "AYARLAR", 90, 1648, 900, 92, new Color(.20f, .25f, .23f), new Color(1, .97f, .89f, .82f), Settings, 29);

        // Boncuk kesesi sağ üstte.
        var wallet = Panel(menu, "Boncuk", 805, 35, 225, 76, new Color(.20f, .25f, .23f));
        Art(wallet.transform, "Boncuk simgesi", MahalleGraphic.Shape.Marble, 18, 16, 44, 44, Gold);
        beadsLabel = Text(wallet.transform, MahalleProfile.Beads.ToString(), 78, 0, 130, 76, 36, Cream);

        if (instant)
        {
            // Online menusunden geri donuste tebesir animasyonu tekrar oynamasin.
            ring.SetProgress(1f); tri.SetProgress(1f);
            tip.color = new Color(1, 1, .96f, 0);
            foreach (var mm in marbles) mm.transform.localScale = Vector3.one;
            headFade.alpha = 1f; menuFade.alpha = 1f;
            menuFade.interactable = true; menuFade.blocksRaycasts = true;
            return;
        }
        StartCoroutine(DrawTitle(ring, tri, tip, marbles, headFade, menuFade));
    }

    private IEnumerator DrawTitle(MahalleGraphic ring, MahalleGraphic tri, MahalleGraphic tip, MahalleGraphic[] marbles, CanvasGroup head, CanvasGroup menu)
    {
        var ringRect = ring.rectTransform;
        Vector2 centre = ringRect.anchoredPosition + new Vector2(ringRect.sizeDelta.x * .5f, -ringRect.sizeDelta.y * .5f);
        float rx = ringRect.sizeDelta.x * .5f, ry = ringRect.sizeDelta.y * .5f;
        var tipRect = tip.rectTransform;

        // 1) Çember çiziliyor. Sona doğru yavaşlar: el kalemi kaldırıyormuş gibi.
        const float draw = 1.15f;
        for (float t = 0f; t < draw; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / draw);
            ring.SetProgress(k);
            float a = Mathf.PI * .5f - k * Mathf.PI * 2f;
            tipRect.anchoredPosition = centre + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry) - new Vector2(13, -13);
            yield return null;
        }
        ring.SetProgress(1f);
        if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlayUiTap();

        yield return new WaitForSecondsRealtime(.1f);

        // 2) Tebeşir üçgene geçiyor. Uç, üç kenarı sırayla yürüyor.
        var triRect = tri.rectTransform;
        Vector2 triCentre = triRect.anchoredPosition + new Vector2(triRect.sizeDelta.x * .5f, -triRect.sizeDelta.y * .5f);
        float tw2 = triRect.sizeDelta.x, th2 = triRect.sizeDelta.y;
        Vector2[] corner =
        {
            triCentre + new Vector2(0, th2 * .42f),
            triCentre + new Vector2(tw2 * .40f, -th2 * .28f),
            triCentre + new Vector2(-tw2 * .40f, -th2 * .28f)
        };
        const float triDraw = .7f;
        for (float t = 0f; t < triDraw; t += Time.unscaledDeltaTime)
        {
            float k = t / triDraw;
            tri.SetProgress(k);
            float walk = k * 3f;
            int side = Mathf.Min(2, (int)walk);
            tipRect.anchoredPosition = Vector2.Lerp(corner[side], corner[(side + 1) % 3], walk - side) - new Vector2(13, -13);
            yield return null;
        }
        tri.SetProgress(1f);
        tip.color = new Color(1, 1, .96f, 0);
        if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlayUiTap();

        yield return new WaitForSecondsRealtime(.12f);

        // 3) Misketler üçgene diziliyor, biri diğerinin ardından.
        for (int i = 0; i < marbles.Length; i++)
        {
            StartCoroutine(PopMarble(marbles[i].transform));
            yield return new WaitForSecondsRealtime(.07f);
        }

        yield return new WaitForSecondsRealtime(.18f);

        // 4) Başlık, sonra menü.
        yield return Fade(head, .35f);
        yield return Fade(menu, .3f);
        menu.interactable = true; menu.blocksRaycasts = true;
    }

    private IEnumerator PopMarble(Transform marble)
    {
        const float dur = .26f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float k = t / dur;
            // Hafif taşan bir yay: misket yere düşüp bir kez zıplıyormuş gibi.
            float scale = 1f + Mathf.Sin(k * Mathf.PI) * .22f - Mathf.Pow(1f - k, 2f);
            marble.localScale = Vector3.one * Mathf.Max(0f, scale);
            yield return null;
        }
        marble.localScale = Vector3.one;
    }

    private IEnumerator Fade(CanvasGroup group, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }
        group.alpha = 1f;
    }

    // ---------------------------------------------------------------
    // ONLINE
    // ---------------------------------------------------------------
    private void ShowOnlineMenu()
    {
        ClearPage();
        Background(new Color(.14f, .12f, .10f));
        Text(page, "ONLİNE", 0, 150, 1080, 110, 84, new Color(1, .98f, .92f), TextAlignmentOptions.Center);
        Text(page, "ikiniz de misketlerinizi ortaya koyun, sırayla atın,\nçıkardığınız misket sizin olur",
             60, 280, 960, 110, 28, new Color(1, .97f, .89f, .62f), TextAlignmentOptions.Center);

        var kart = Panel(page, "Kurallar", 90, 430, 900, 330, new Color(.19f, .24f, .22f));
        kart.radius = 28;
        string[] satir =
        {
            L.F("Kese {0} misket · her el {1}'er ortaya", DuelMatch.StartingPouch, DuelMatch.AntePerRound),
            "Çıkardığın misket kesene girer",
            L.F("Çıkardıysan tekrar atarsın, turda en fazla {0}", DuelMatch.MaxShotsPerTurn),
            "Atıcın çemberin ortasında kalırsa onu kaybedersin",
            "Maç başı sıra atışı: kenara yakın duran önce atar",
            "Herkes aynı misketi kullanır, güç yok"
        };
        for (int i = 0; i < satir.Length; i++)
        {
            Art(kart.transform, "Nokta", MahalleGraphic.Shape.Marble, 40, 30 + i * 52, 28, 28, Gold);
            Text(kart.transform, satir[i], 92, 18 + i * 52, 780, 50, 25, new Color(.86f, .88f, .8f));
        }

        // Iki klasik misket oyunu. Kurallar ayni, saha sekli farkli.
        Text(page, "AYNI CİHAZDA OYNA · İki kişi tek telefonda", 90, 786, 900, 44, 25,
             new Color(1, .97f, .89f, .55f), TextAlignmentOptions.Center);
        var cember = LabelButton(page, "ÇEMBER", 90, 836, 435, 124, Gold, new Color(.16f, .20f, .16f),
                    () => { DuelSession.Begin(DuelSession.Mode.HotSeat, DuelSession.GameType.Cember);
                            SceneManager.LoadScene(GameSession.GameSceneName); }, 36);
        var ucgen = LabelButton(page, "ÜÇGEN", 555, 836, 435, 124, new Color(.82f, .56f, .22f), new Color(.16f, .20f, .16f),
                    () => { DuelSession.Begin(DuelSession.Mode.HotSeat, DuelSession.GameType.Ucgen);
                            SceneManager.LoadScene(GameSession.GameSceneName); }, 36);
        Text(cember.transform, "yuvarlak saha", 12, 86, 411, 34, 22, new Color(.16f, .20f, .16f, .7f), TextAlignmentOptions.Center);
        Text(ucgen.transform, "sivri uç, dar açılar", 12, 86, 411, 34, 22, new Color(.16f, .20f, .16f, .7f), TextAlignmentOptions.Center);

        // DIZI (kondik) ve KUYU: kazanma kosullari farkli.
        var dizi = LabelButton(page, "DİZİ", 90, 972, 435, 116, new Color(.55f, .62f, .40f), new Color(.16f, .20f, .16f),
                    () => { DuelSession.Begin(DuelSession.Mode.HotSeat, DuelSession.GameType.Dizi);
                            SceneManager.LoadScene(GameSession.GameSceneName); }, 34);
        var kuyu = LabelButton(page, "KUYU", 555, 972, 435, 116, new Color(.42f, .48f, .58f), new Color(.16f, .20f, .16f),
                    () => { DuelSession.Begin(DuelSession.Mode.HotSeat, DuelSession.GameType.Kuyu);
                            SceneManager.LoadScene(GameSession.GameSceneName); }, 34);
        Text(dizi.transform, "tek sıra · kıpırdattığın senin", 12, 78, 411, 34, 21,
             new Color(.16f, .20f, .16f, .7f), TextAlignmentOptions.Center);
        Text(kuyu.transform, "ortada çukur · düşürdüğün senin", 12, 78, 411, 34, 21,
             new Color(.16f, .20f, .16f, .7f), TextAlignmentOptions.Center);

        // Ag katmani henuz yok; akis burada duruyor ki yeri belli olsun.
        var oda = LabelButton(page, "ODA KUR", 90, 1116, 435, 104, new Color(.22f, .26f, .24f), new Color(1, .97f, .89f, .35f),
                              () => Toast("Oda kurma henüz hazır değil."), 30);
        var katil = LabelButton(page, "ODAYA KATIL", 555, 1116, 435, 104, new Color(.22f, .26f, .24f), new Color(1, .97f, .89f, .35f),
                                () => Toast("Odaya katılma henüz hazır değil."), 30);
        oda.interactable = katil.interactable = true;

        LabelButton(page, "ANA MENÜ", 90, 1248, 900, 96, new Color(.20f, .25f, .23f), new Color(1, .97f, .89f, .82f),
                    () => ShowTitle(true), 29);
    }

    // ---- Duello ----
    private void StartDuel()
    {
        duel = controller.gameObject.GetComponent<DuelController>();
        if (duel == null) duel = controller.gameObject.AddComponent<DuelController>();
        duelScreenShown = -1;
        duel.BeginMatch();
    }

    // MAC BASI: SIRA BELIRLEME ATISI.
    // Bos sahaya birer atis; uzak kenara en yakin duran once atar.
    private void ShowToss()
    {
        ClearPage();
        var top = Panel(page, "Sıra atışı başlığı", 24, 12, 1032, 250, Ink); top.radius = 30;
        Text(top.transform, "SIRA ATIŞI", 28, 14, 600, 56, 34, Cream);
        LabelButton(top.transform, "ÇIK", 872, 16, 134, 88, new Color(.28f, .38f, .31f), Cream, LeaveDuel, 30);
        Text(top.transform, "Sahanın UZAK kenarına en yakın duran önce atar · çizgiyi geçen yanar",
             28, 104, 976, 40, 23, new Color(.72f, .76f, .68f));

        for (int p = 0; p < 2; p++)
        {
            var box = Panel(top.transform, "Atış " + p, 24 + p * 502, 150, 482, 88, new Color(.24f, .34f, .29f));
            Art(box.transform, "Renk", MahalleGraphic.Shape.Marble, 16, 18, 50, 50, DuelSession.PlayerColor(p));
            Text(box.transform, DuelSession.PlayerName(p), 78, 12, 210, 36, 24, new Color(.8f, .84f, .75f));
            var value = Text(box.transform, "—", 78, 46, 380, 36, 26, Cream);
            if (p == 0) tossScoreA = value; else tossScoreB = value;
        }

        tossWho = Text(page, "", 48, 0, 984, 62, 34, Cream, TextAlignmentOptions.Center);
        Bottom(tossWho.rectTransform, 48, 300, 984, 62);
        tossHint = Text(page, "", 48, 0, 984, 46, 24, new Color(.81f, .84f, .75f), TextAlignmentOptions.Center);
        Bottom(tossHint.rectTransform, 48, 254, 984, 46);

        var git = LabelButton(page, "MAÇA BAŞLA", 90, 0, 900, 116, Gold, new Color(.16f, .20f, .16f),
                              () => { duelScreenShown = -1; duel.TossDone(); }, 36);
        Bottom((RectTransform)git.transform, 90, 120, 900, 116);
        tossGo = git.gameObject;
        RefreshToss();
    }

    private void RefreshToss()
    {
        if (duel == null || tossWho == null) return;
        var toss = duel.Online ? duel.Net.Toss : DuelToss.Current;
        if (tossScoreA != null) tossScoreA.SetTextL(toss.TextFor(0));
        if (tossScoreB != null) tossScoreB.SetTextL(toss.TextFor(1));

        bool bitti = toss.Finished;
        if (tossGo != null) tossGo.SetActive(bitti);

        if (bitti)
        {
            int k = toss.WinnerOf;
            tossWho.SetTextL(L.F("{0} ÖNCE ATIYOR", DuelSession.PlayerName(k)));
            tossWho.color = DuelSession.PlayerColor(k);
            tossHint.SetTextL(L.F("{0} misketlerini önce dizer", DuelSession.PlayerName(toss.FirstPlacerOf)));
            return;
        }

        int atan = duel.Online ? duel.Net.Toss.TurnOf : DuelToss.Shooter;
        tossWho.SetTextL(duel.Watching ? "RAKİP ATIYOR"
                                      : DuelSession.PlayerName(atan) + " ATIYOR");
        tossWho.color = DuelSession.PlayerColor(atan);
        tossHint.SetTextL(toss.Threw(1 - atan) ? "Rakibinden daha yakın durdurmaya çalış"
                                              : "Uzak kenara olabildiğince yakın durdur");
    }

    // Elin basinda: "ekstra dizme hakkini kullanacak misin?"
    private void ShowOffer()
    {
        ClearPage();
        Background(new Color(.14f, .12f, .10f));
        int me = duel.ActivePlayer;
        Text(page, L.F("EL {0}", duel.Match.Round), 0, 200, 1080, 80, 48, new Color(1, .97f, .89f, .6f), TextAlignmentOptions.Center);
        Art(page, "Renk", MahalleGraphic.Shape.Marble, 480, 320, 120, 120, DuelSession.PlayerColor(me));
        Text(page, DuelSession.PlayerName(me), 0, 470, 1080, 70, 44, new Color(1, .98f, .92f), TextAlignmentOptions.Center);
        Text(page, "Misketlerini kendi elinle dizmek ister misin?", 80, 570, 920, 70, 31,
             new Color(1, .97f, .89f, .72f), TextAlignmentOptions.Center);
        Text(page, "Bu hakkı maç boyunca bir kez kullanabilirsin", 80, 650, 920, 60, 25,
             new Color(1, .97f, .89f, .45f), TextAlignmentOptions.Center);

        LabelButton(page, "ELLE DİZECEĞİM", 90, 800, 900, 128, Gold, new Color(.16f, .20f, .16f),
                    () => { duelScreenShown = -1; duel.ChooseManual(); }, 38);
        LabelButton(page, "OTOMATİK DİZİLSİN", 90, 960, 900, 110, new Color(.24f, .30f, .27f), Cream,
                    () => { duelScreenShown = -1; duel.ChooseAuto(); }, 32);
        LabelButton(page, "ÇIK", 90, 1110, 900, 92, new Color(.34f, .28f, .26f), Cream, LeaveDuel, 29);
    }

    private void ShowPlacement()
    {
        ClearPage();
        int me = duel.ActivePlayer;
        var top = Panel(page, "Dizme başlığı", 24, 12, 1032, 200, Ink); top.radius = 30;
        Art(top.transform, "Renk", MahalleGraphic.Shape.Marble, 28, 26, 58, 58, DuelSession.PlayerColor(me));
        Text(top.transform, DuelSession.PlayerName(me) + " · " + L.F("EL {0}", duel.Match.Round), 100, 20, 900, 60, 34, Cream);
        Text(top.transform, "Sahanın içine dokunarak misketlerini diz. Rakibin dizilişini görmüyorsun.",
             28, 88, 976, 70, 25, new Color(.81f, .84f, .75f));
        placeCount = Text(top.transform, "", 28, 148, 976, 44, 29, Gold);

        var dock = Panel(page, "Dizme çubuğu", 24, 0, 1032, 260, Ink);
        Bottom(dock.rectTransform, 24, 18, 1032, 260);
        LabelButton(dock.transform, "GERİ AL", 20, 20, 320, 100, new Color(.3f, .35f, .32f), Cream, () => duel.UndoSpot(), 30);
        LabelButton(dock.transform, "HAZIR DİZİLİŞ", 356, 20, 320, 100, new Color(.3f, .35f, .32f), Cream, () => duel.FillRemaining(), 28);
        LabelButton(dock.transform, "ÇIK", 692, 20, 320, 100, new Color(.34f, .28f, .26f), Cream, LeaveDuel, 30);
        LabelButton(dock.transform, "HAZIRIM", 20, 140, 992, 104, Gold, new Color(.16f, .20f, .16f),
                    () => { duelScreenShown = -1; duel.ConfirmPlacement(); }, 38);

        placeHint = Text(page, "", 48, 0, 984, 50, 25, new Color(1, .97f, .89f, .7f), TextAlignmentOptions.Center);
        Bottom(placeHint.rectTransform, 48, 292, 984, 50);
        RefreshPlacement();
    }

    private void RefreshPlacement()
    {
        if (duel == null || placeCount == null) return;
        int n = duel.PlacedCount, toplam = duel.NeedCount;
        placeCount.SetTextL(L.F("{0} / {1} misket dizildi", n, toplam));
        if (placeHint != null)
        {
            string uyari = duel.Hint;
            placeHint.SetTextL(uyari.Length > 0 ? uyari
                              : n >= toplam ? "Hazırsan HAZIRIM'a bas"
                              : L.F("{0} misket daha koy", toplam - n));
        }
    }

    private void ShowHandOver()
    {
        ClearPage();
        Background(new Color(.14f, .12f, .10f));
        int next = duel.ActivePlayer;
        Text(page, "TELEFONU VER", 0, 520, 1080, 100, 66, new Color(1, .98f, .92f), TextAlignmentOptions.Center);
        Text(page, L.F("{0} şimdi kendi misketlerini dizecek", DuelSession.PlayerName(next)),
             80, 650, 920, 90, 32, new Color(1, .97f, .89f, .7f), TextAlignmentOptions.Center);
        Text(page, "Diziliş gizli: kimse diğerinin nereye koyduğunu görmeyecek",
             80, 760, 920, 80, 25, new Color(1, .97f, .89f, .45f), TextAlignmentOptions.Center);
        LabelButton(page, "HAZIRIM", 90, 1000, 900, 132, Gold, new Color(.16f, .20f, .16f),
                    () => { duelScreenShown = -1; duel.HandOverDone(); }, 40);
    }

    // KUYU'nun kendi ekrani. Kese yok, sayi yok: TUR var.
    // Bir turu almak icin once cukura girip PISMEK, sonra rakibi sahadan
    // cikarmak gerekiyor. Uc turdan ikisini alan maci kazanir.
    private void ShowWell()
    {
        ClearPage();
        var top = Panel(page, "Kuyu başlığı", 24, 12, 1032, 250, Ink); top.radius = 30;
        wellRoundLabel = Text(top.transform, "", 28, 14, 600, 56, 34, Cream);
        LabelButton(top.transform, "ÇIK", 872, 16, 134, 88, new Color(.28f, .38f, .31f), Cream, LeaveDuel, 30);

        for (int p = 0; p < 2; p++)
        {
            var box = Panel(top.transform, "Oyuncu " + p, 24 + p * 502, 96, 482, 134, new Color(.24f, .34f, .29f));
            Art(box.transform, "Renk", MahalleGraphic.Shape.Marble, 20, 20, 54, 54, DuelSession.PlayerColor(p));
            Text(box.transform, DuelSession.PlayerName(p), 88, 16, 260, 40, 26, new Color(.8f, .84f, .75f));
            // Pisme durumu: turun kapi bekcisi, o yuzden en gorunur yerde.
            var durum = Text(box.transform, "", 352, 14, 120, 44, 24, Gold);
            var tur = Text(box.transform, "0", 88, 56, 380, 60, 44, Cream);
            if (p == 0) { duelCountA = tur; wellCookA = durum; }
            else { duelCountB = tur; wellCookB = durum; }
        }
        Text(top.transform, "Önce çukura gir (PİŞ), sonra rakibi sahadan çıkar · 3 turdan 2'sini alan kazanır",
             28, 236, 976, 34, 21, new Color(.72f, .76f, .68f));

        duelTurnLabel = Text(page, "", 48, 0, 984, 62, 34, Cream, TextAlignmentOptions.Center);
        Bottom(duelTurnLabel.rectTransform, 48, 240, 984, 62);
        duelTurnsLabel = Text(page, "", 48, 0, 984, 46, 24, new Color(.81f, .84f, .75f), TextAlignmentOptions.Center);
        Bottom(duelTurnsLabel.rectTransform, 48, 194, 984, 46);
        RefreshWell();
    }

    private void RefreshWell()
    {
        if (duel == null || duel.Well == null) return;
        var w = duel.Well;

        if (wellRoundLabel != null) wellRoundLabel.SetTextL(L.T("KUYU") + " · " + w.RoundText);
        if (duelCountA != null) duelCountA.SetTextL(L.F("{0} tur", w.RoundsWon(0)));
        if (duelCountB != null) duelCountB.SetTextL(L.F("{0} tur", w.RoundsWon(1)));

        for (int p = 0; p < 2; p++)
        {
            var alan = p == 0 ? wellCookA : wellCookB;
            if (alan == null) continue;
            bool pismis = w.Cooked(p);
            alan.SetTextL(pismis ? "PİŞTİ" : "ÇİĞ");
            alan.color = pismis ? Gold : new Color(.62f, .66f, .70f);
        }

        if (duelTurnLabel != null)
        {
            duelTurnLabel.SetTextL(duel.Watching ? "RAKİP ATIYOR"
                                                : DuelSession.PlayerName(w.Turn) + " ATIYOR");
            duelTurnLabel.color = DuelSession.PlayerColor(w.Turn);
        }
        if (duelTurnsLabel != null)
        {
            // Once son atisin sonucu, sonra siradaki oyuncunun hedefi.
            string olan = w.OutcomeText(1 - w.Turn);
            if (w.ShotsThisTurn > 0)
                duelTurnsLabel.SetTextL(L.F("Bu turda {0} atış hakkın daha var", WellMatch.MaxShotsPerTurn - w.ShotsThisTurn));
            else if (olan.Length > 0)
                duelTurnsLabel.SetTextL(olan);
            else
                duelTurnsLabel.SetTextL(w.Cooked(w.Turn) ? "Piştin: rakibi sahadan çıkar, tur senin"
                                                        : "Önce çukura gir");
        }
    }

    // Tur bitti ya da iptal edildi.
    private void WellRoundOver()
    {
        var w = duel.Well;
        var box = Modal("Tur sonucu", 600);
        bool iptal = w.LastRoundWinner < 0;

        Text(box, iptal ? "TUR İPTAL" : L.F("{0} TURU ALDI", DuelSession.PlayerName(w.LastRoundWinner)),
             30, 40, 924, 80, 46, Ink, TextAlignmentOptions.Center);
        Text(box, w.ScoreText, 30, 140, 924, 90, 62, Muted, TextAlignmentOptions.Center);
        Text(box, "tur", 30, 236, 924, 44, 26, Muted, TextAlignmentOptions.Center);
        if (iptal)
            Text(box, "Dört tur üst üste kimse bir şey yapamadı, tur baştan başlıyor",
                 30, 286, 924, 60, 23, new Color(.72f, .32f, .24f), TextAlignmentOptions.Center);

        LabelButton(box, iptal ? "TEKRAR" : "SONRAKİ TUR", 36, 360, 912, 116, Ink, Cream,
                    () => { CloseModal(false); duelScreenShown = -1; duel.NextRound(); }, 38);
        LabelButton(box, "ANA MENÜ", 36, 490, 912, 96, new Color(.88f, .83f, .69f), Ink, LeaveDuel, 30);
    }

    private void WellResult()
    {
        var w = duel.Well;
        var box = Modal("Kuyu sonucu", 660);
        Text(box, L.F("{0} KAZANDI", DuelSession.PlayerName(w.Winner)), 30, 40, 924, 80, 50, Ink, TextAlignmentOptions.Center);
        Text(box, w.ScoreText, 30, 140, 924, 90, 64, Muted, TextAlignmentOptions.Center);
        Text(box, "tur", 30, 236, 924, 44, 26, Muted, TextAlignmentOptions.Center);
        LabelButton(box, "RÖVANŞ", 36, 320, 912, 116, Ink, Cream,
                    () => { CloseModal(false); duelScreenShown = -1; duel.Rematch(); }, 38);
        Text(box, "ilk atan değişir", 36, 444, 912, 40, 23, Muted, TextAlignmentOptions.Center);
        LabelButton(box, "ANA MENÜ", 36, 500, 912, 100, new Color(.88f, .83f, .69f), Ink, LeaveDuel, 30);
    }

    private void ShowDuel()
    {
        ClearPage();
        var top = Panel(page, "Düello başlığı", 24, 12, 1032, 250, Ink); top.radius = 30;
        Text(top.transform, DuelSession.TypeName + " · " + L.F("EL {0} / {1}", duel.Match.Round, DuelMatch.RoundsPerMatch), 28, 14, 600, 56, 34, Cream);
        LabelButton(top.transform, "ÇIK", 872, 16, 134, 88, new Color(.28f, .38f, .31f), Cream, LeaveDuel, 30);

        // Kazanani KESE belirliyor: cikardigin misket senin olur.
        for (int p = 0; p < 2; p++)
        {
            var box = Panel(top.transform, "Oyuncu " + p, 24 + p * 502, 96, 482, 134, new Color(.24f, .34f, .29f));
            Art(box.transform, "Renk", MahalleGraphic.Shape.Marble, 20, 20, 54, 54, DuelSession.PlayerColor(p));
            Text(box.transform, DuelSession.PlayerName(p), 88, 16, 300, 40, 26, new Color(.8f, .84f, .75f));
            Text(box.transform, "KESE", 396, 16, 76, 40, 22, new Color(.66f, .7f, .62f));
            var value = Text(box.transform, "0", 88, 56, 380, 60, 44, Cream);
            if (p == 0) duelCountA = value; else duelCountB = value;
        }
        Text(top.transform, "Çıkardığın misket senin · atıcın çemberin ORTASINDA kalırsa kaybedersin",
             28, 236, 976, 34, 22, new Color(.72f, .76f, .68f));

        duelTurnLabel = Text(page, "", 48, 0, 984, 62, 34, Cream, TextAlignmentOptions.Center);
        Bottom(duelTurnLabel.rectTransform, 48, 240, 984, 62);
        duelTurnsLabel = Text(page, "", 48, 0, 984, 46, 24, new Color(.81f, .84f, .75f), TextAlignmentOptions.Center);
        Bottom(duelTurnsLabel.rectTransform, 48, 194, 984, 46);
        RefreshDuel();
    }

    private void RefreshDuel()
    {
        if (duel == null || duel.Match == null) return;
        var m = duel.Match;
        if (duelCountA != null) duelCountA.SetTextL(m.Pouch(0).ToString());
        if (duelCountB != null) duelCountB.SetTextL(m.Pouch(1).ToString());
        if (duelTurnLabel != null)
        {
            bool izliyor = duel.Watching;
            duelTurnLabel.SetTextL(izliyor ? "RAKİP ATIYOR"
                                          : DuelSession.PlayerName(m.Turn) + " ATIYOR");
            duelTurnLabel.color = DuelSession.PlayerColor(m.Turn);
        }
        if (duelTurnsLabel != null)
        {
            int kalan = DuelMatch.MaxShotsPerTurn - m.ShotsThisTurn;
            duelTurnsLabel.SetTextL(duel.Watching ? L.F("Sıranı bekle · çemberde {0} misket", m.RemainingInRing)
                : m.ShotsThisTurn > 0 ? L.F("Bu turda {0} atış hakkın daha var", kalan)
                : L.F("Çemberde {0} misket", m.RemainingInRing));
        }
    }

    // El bitti: ara ekran.
    private void ShowRoundOver()
    {
        var m = duel.Match;
        var box = Modal("El sonucu", 620);
        Text(box, L.F("EL {0} BİTTİ", m.Round), 30, 40, 924, 80, 48, Ink, TextAlignmentOptions.Center);
        Text(box, m.Pouch(0) + "  ·  " + m.Pouch(1), 30, 140, 924, 90, 62, Muted, TextAlignmentOptions.Center);
        Text(box, "kesedeki misket", 30, 236, 924, 44, 26, Muted, TextAlignmentOptions.Center);
        if (m.RemainingInRing > 0)
            Text(box, L.F("Çemberde kalan {0} misket sonraki ele devrediyor", m.RemainingInRing),
                 30, 290, 924, 44, 24, new Color(.72f, .32f, .24f), TextAlignmentOptions.Center);
        LabelButton(box, "SONRAKİ EL", 36, 360, 912, 116, Ink, Cream,
                    () => { CloseModal(false); duelScreenShown = -1; duel.NextRound(); }, 38);
        LabelButton(box, "ANA MENÜ", 36, 490, 912, 96, new Color(.88f, .83f, .69f), Ink, LeaveDuel, 30);
    }

    private void DuelResult()
    {
        var m = duel.Match;
        var box = Modal("Düello sonucu", 720);
        string baslik = m.Result == DuelMatch.Outcome.Draw ? "BERABERE"
                      : L.F("{0} KAZANDI", DuelSession.PlayerName(m.Result == DuelMatch.Outcome.PlayerOne ? 0 : 1));
        Text(box, baslik, 30, 40, 924, 80, 50, Ink, TextAlignmentOptions.Center);
        Text(box, m.Pouch(0) + "  ·  " + m.Pouch(1), 30, 140, 924, 90, 64, Muted, TextAlignmentOptions.Center);
        Text(box, "kesedeki misket", 30, 236, 924, 44, 26, Muted, TextAlignmentOptions.Center);

        LabelButton(box, "RÖVANŞ", 36, 320, 912, 116, Ink, Cream,
                    () => { CloseModal(false); duelScreenShown = -1; duel.Rematch(); }, 38);
        Text(box, "ilk dizen değişir", 36, 444, 912, 40, 23, Muted, TextAlignmentOptions.Center);
        LabelButton(box, "ANA MENÜ", 36, 500, 912, 100, new Color(.88f, .83f, .69f), Ink, LeaveDuel, 30);
    }

    private void LeaveDuel()
    {
        DuelSession.End();
        returnToTitle = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSession.LevelSelectSceneName);
    }

    private void ShowHome()
    {
        ClearPage();
        if(tab==0){Background(new Color(.14f,.12f,.10f));MapScreen();}
        else
        {
            Background(Paper);
            Header(tab==1?"HER MİSKETİN BİR HİKÂYESİ VAR":tab==3?"MAHALLEDEKİ İZİN":"KÜÇÜK HEDEFLER, YENİ BONCUKLAR");
            if(tab==1)Collection();else if(tab==3)Stats();else Missions();
        }
        Nav();
    }
    private RectTransform HomeScroll(float height)
    {
        var viewport=Rect("Kaydırılabilir içerik",page);
        viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;
        viewport.offsetMin=new Vector2(0,282);viewport.offsetMax=new Vector2(0,-171);
        viewport.gameObject.AddComponent<RectMask2D>();
        var surface=viewport.gameObject.AddComponent<Image>();surface.color=Color.clear;surface.raycastTarget=true;
        var content=Rect("İçerik",viewport);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,height);content.anchoredPosition=Vector2.zero;
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=45;
        var inner=Rect("Düzen",content);Place(inner,0,-171,1080,height+171);
        return inner;
    }
    // Mahalle haritası. Zemin, yol ve duraklar MahalleMapView içinde çizilir.
    private void MapScreen()
    {
        var theme=MahalleTheme.Get(district);

        // Harita bütün ekranı kaplar. Başlık üstte durur, harita altından kayıp geçer.
        var viewport=Rect("Harita",page);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        var catcher=viewport.gameObject.AddComponent<Image>();catcher.color=Color.clear;catcher.raycastTarget=true;

        var content=Rect("Harita içeriği",viewport);
        content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);
        content.sizeDelta=new Vector2(0,MahalleMapView.ContentHeight);content.anchoredPosition=Vector2.zero;

        var scroll=viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;
        scroll.movementType=ScrollRect.MovementType.Elastic;scroll.elasticity=.09f;
        scroll.scrollSensitivity=45;scroll.decelerationRate=.12f;

        MahalleMapView.Build(content,district,font,OpenLevel,Toast);

        // Ekran, oyuncunun sıradaki bölümüne bakarak açılır.
        Canvas.ForceUpdateCanvases();
        float visible=viewport.rect.height;
        int local=Mathf.Clamp(MahalleProfile.NextLevel-district*Campaign.PerDistrict,0,Campaign.PerDistrict-1);
        float focus=MahalleMapView.StopOffset(local)-visible*.5f;   // başlık ve alt çubuk eşit yükseklikte
        content.anchoredPosition=new Vector2(0,Mathf.Clamp(focus,0,Mathf.Max(0,MahalleMapView.ContentHeight-visible)));

        MapHeader(theme);
        MapDock();
    }

    private void MapHeader(MahalleTheme theme)
    {
        SetBleed(Paper,bleedBottom!=null?bleedBottom.color:Color.clear);
        var head=Panel(page,"Başlık",0,0,1080,300,Paper);head.radius=0;head.shadow=18;head.raycastTarget=true;

        // Sol üstte kuşandığın misket durur; dokununca koleksiyona gider.
        int skin=Mathf.Clamp(MahalleProfile.EffectiveSkin,0,Campaign.SkinNames.Length-1);
        var mine=Button(head.transform,"Kuşandığın misket",48,26,326,78,new Color(.24f,.36f,.31f),()=>{tab=1;ShowHome();});
        var mineFace=mine.GetComponent<MahalleGraphic>();
        mineFace.colorB=new Color(.10f,.18f,.16f);mineFace.radius=39;mineFace.shadow=9;mineFace.highlight=true;
        mine.gameObject.AddComponent<MahalleTap>();
        var art=Art(mine.transform,"Misket",MahalleGraphic.Shape.Marble,16,17,44,44,Campaign.SkinColors[skin]);
        art.accent=SpecialMarbles.Accent(skin);
        Text(mine.transform,Campaign.SkinNames[skin],76,0,238,78,28,Cream);

        // Üst sıranın ortasında mahallenin yıldız ilerlemesi.
        int stars=0;
        for(int i=district*Campaign.PerDistrict;i<(district+1)*Campaign.PerDistrict;i++)stars+=MahalleProfile.Data.stars[i];
        int full=Campaign.PerDistrict*3;
        var meter=Panel(head.transform,"Yıldız ilerlemesi",390,26,396,78,new Color(.88f,.84f,.73f));
        meter.colorB=new Color(.83f,.79f,.67f);meter.radius=39;meter.highlight=true;
        Art(meter.transform,"Yıldız",MahalleGraphic.Shape.Star,20,19,40,40,Gold);
        Text(meter.transform,stars+" / "+full,68,19,116,40,30,Ink);
        var track=Panel(meter.transform,"Yol",190,33,186,12,new Color(.16f,.25f,.23f,.24f));track.radius=6;
        var fill=Panel(meter.transform,"Dolu",190,33,Mathf.Max(12f,186f*stars/full),12,Gold);
        fill.colorB=new Color(.85f,.53f,.15f);fill.radius=6;

        var wallet=Panel(head.transform,"Boncuk",802,26,230,78,new Color(.24f,.36f,.31f));
        wallet.colorB=new Color(.10f,.18f,.16f);wallet.radius=39;wallet.shadow=9;wallet.highlight=true;
        Art(wallet.transform,"Boncuk simgesi",MahalleGraphic.Shape.Marble,16,17,44,44,Gold);
        beadsLabel=Text(wallet.transform,MahalleProfile.Beads.ToString(),76,0,140,78,36,Cream);

        var prev=Button(head.transform,"Önceki mahalle",48,144,96,96,new Color(.88f,.85f,.76f),()=>{district=(district+4)%5;ShowHome();});
        prev.GetComponent<MahalleGraphic>().radius=30;prev.gameObject.AddComponent<MahalleTap>();
        Art(prev.transform,"Sol",MahalleGraphic.Shape.Chevron,26,26,44,44,Ink).mirror=true;

        var next=Button(head.transform,"Sonraki mahalle",936,144,96,96,new Color(.88f,.85f,.76f),()=>{district=(district+1)%5;ShowHome();});
        next.GetComponent<MahalleGraphic>().radius=30;next.gameObject.AddComponent<MahalleTap>();
        Art(next.transform,"Sağ",MahalleGraphic.Shape.Chevron,26,26,44,44,Ink);

        Text(head.transform,theme.title,164,140,752,62,46,Ink,TextAlignmentOptions.Center);
        Text(head.transform,theme.subtitle,164,202,752,44,26,Muted,TextAlignmentOptions.Center);

        for(int i=0;i<5;i++)
        {
            bool on=i==district;
            Art(head.transform,"Nokta "+i,MahalleGraphic.Shape.Circle,483+i*26,on?254:256,on?14:10,on?14:10,
                on?Gold:new Color(.16f,.25f,.23f,.22f));
        }
    }

    private void MapDock()
    {
        // Harita alt çubuğun altında düz kesilmez, koyu zemine doğru erir.
        var dark=new Color(.14f,.12f,.10f);
        // Koyu zemin sadece alt menünün arkasında; devam çubuğu haritanın üstünde durur.
        var blend=Panel(page,"Alt geçiş",0,0,1080,250,new Color(dark.r,dark.g,dark.b,0));
        blend.radius=0;blend.colorB=dark;
        Bottom(blend.rectTransform,0,158,1080,250);
        var floor=Panel(page,"Alt zemin",0,0,1080,158,dark);
        floor.radius=0;floor.raycastTarget=true;
        Bottom(floor.rectTransform,0,0,1080,158);

        int next=MahalleProfile.NextLevel;
        var level=Campaign.Database.Get(next);
        var resume=Button(page,"Devam et",48,0,984,120,Gold,()=>OpenLevel(next));
        var face=resume.GetComponent<MahalleGraphic>();
        face.colorB=new Color(.85f,.53f,.15f);face.radius=32;face.shadow=14;face.highlight=true;
        Bottom((RectTransform)resume.transform,48,166,984,120);
        resume.gameObject.AddComponent<MahalleTap>();
        Text(resume.transform,"DEVAM ET",34,16,700,36,23,new Color(.15f,.2f,.17f,.72f));
        Text(resume.transform,(next%Campaign.PerDistrict+1).ToString("00")+" · "+L.T(level.levelName),34,48,700,54,36,new Color(.13f,.18f,.15f));
        Art(resume.transform,"Ok",MahalleGraphic.Shape.Chevron,878,36,48,48,new Color(.13f,.18f,.15f));
    }

    private void Collection()
    {
        var content=HomeScroll(2840);
        Text(content,"Misket koleksiyonun",48,182,984,65,47,Ink);
        Text(content,"Klasik misketler aşınmaz. Özellikli misketler aşağıda.",48,256,984,76,30,Muted);
        for(int i=0;i<SpecialMarbles.FirstSkin;i++) CollectionCard(content,i,363+(i/2)*302);

        Text(content,"ÖZELLİKLİ MİSKETLER",48,1300,984,58,38,Ink);
        Text(content,L.F("{0} boncuk · {1} atış ömür · Tam yenileme {2} boncuk.",SpecialMarbles.PurchasePrice,SpecialMarbles.MaxLife,SpecialMarbles.RepairPrice),48,1368,984,82,29,Muted);
        for(int i=SpecialMarbles.FirstSkin;i<Campaign.SkinCount;i++)
            CollectionCard(content,i,1478+((i-SpecialMarbles.FirstSkin)/2)*532);

        Text(content,"Normal misket her zaman ücretsiz ve aşınmaz.",48,2610,984,76,29,Muted,TextAlignmentOptions.Center);
        var settings=LabelButton(page,"AYARLAR",48,0,984,86,new Color(.88f,.85f,.76f),Ink,Settings,28);
        Bottom((RectTransform)settings.transform,48,160,984,86);
    }

    private void CollectionCard(RectTransform content,int skin,float y)
    {
        bool special=SpecialMarbles.IsSpecial(skin),owned=MahalleProfile.Data.skins[skin];
        bool selected=MahalleProfile.EffectiveSkin==skin,worn=special&&owned&&MahalleProfile.RemainingLife(skin)==0;
        var card=Panel(content,Campaign.SkinNames[skin],48+(skin%2)*508,y,476,special?508:278,selected?Ink:Cream);
        var marble=Art(card.transform,"Cam misket",MahalleGraphic.Shape.Marble,24,24,150,150,Campaign.SkinColors[skin]);marble.accent=SpecialMarbles.Accent(skin);
        Text(card.transform,Campaign.SkinNames[skin],193,32,265,90,33,selected?Cream:Ink);
        if(special)
        {
            Text(card.transform,owned?L.F("{0} / {1} ATIŞ",MahalleProfile.RemainingLife(skin),SpecialMarbles.MaxLife):L.F("{0} BONCUK",SpecialMarbles.PurchasePrice),193,126,265,48,26,selected?Gold:Muted);
            Text(card.transform,SpecialMarbles.Descriptions[skin-SpecialMarbles.FirstSkin],24,184,428,70,26,selected?Cream:Muted);
        }
        string label=worn?"AŞINDI":selected?"KUŞANILDI":owned?"KUŞAN":L.F("{0} BONCUK · AL",Campaign.SkinPrices[skin]);
        var buy=LabelButton(card.transform,label,24,special?270:194,428,62,selected?new Color(.27f,.39f,.31f):new Color(.89f,.85f,.72f),selected?Gold:Ink,()=>{if(MahalleProfile.EquipOrBuy(skin))ShowHome();else Toast("Yeterli boncuk yok veya misket yenilenmeli.");},25);
        buy.interactable=!selected&&!worn;
        if(special)
        {
            var repair=LabelButton(card.transform,L.F("TAM YENİLE · {0} BONCUK",SpecialMarbles.RepairPrice),24,350,428,62,new Color(.89f,.85f,.72f),Ink,()=>{if(MahalleProfile.RepairMarble(skin))ShowHome();else Toast(L.F("Yenilemek için {0} boncuk gerekiyor.",SpecialMarbles.RepairPrice));},23);
            repair.interactable=owned&&MahalleProfile.RemainingLife(skin)<SpecialMarbles.MaxLife;
            Text(card.transform,"Özel güç atışında özelliği durur, ömrü azalmaz.",24,426,428,62,23,selected?Cream:Muted,TextAlignmentOptions.Center);
        }
    }
    // İSTATİSTİK: üç kart. Rekor ve mahalle sayımı bu sürümle başladı.
    private void Stats()
    {
        var content=HomeScroll(1100);
        Text(content,"Karnen",48,190,984,66,47,Ink);
        Text(content,"Bitirdiğin her bölüm buraya yazılır.",48,268,984,70,30,Muted);
        int fav=MahalleProfile.FavoriteDistrict();
        string[] labels={"TOPLAM ÇIKARDIĞIN MİSKET","TEK ATIŞTA REKORUN","EN ÇOK OYNADIĞIN MAHALLE"};
        string[] values={
            MahalleProfile.Data.knocked.ToString(),
            MahalleProfile.Data.bestShot>0?L.F("{0} MİSKET",MahalleProfile.Data.bestShot):"HENÜZ YOK",
            fav>=0?L.T(Campaign.Districts[fav]):"HENÜZ YOK"};
        string[] notes={
            "Bitirdiğin bölümlerde çemberden çıkardıkların",
            "Tek bir atışla aynı anda çıkardığın en çok misket",
            fav>=0?L.F("{0} bölüm bitirdin",MahalleProfile.Data.districtPlays[fav]):"Bir bölüm bitirince burada görünür"};
        for(int i=0;i<3;i++)
        {
            var card=Panel(content,labels[i],48,370+i*250,984,220,Cream);
            Art(card.transform,"Simge",i==0?MahalleGraphic.Shape.Marble:i==1?MahalleGraphic.Shape.Star:MahalleGraphic.Shape.House,32,62,96,96,Gold);
            Text(card.transform,labels[i],160,22,790,40,25,Muted);
            Text(card.transform,values[i],160,64,790,82,56,Ink);
            Text(card.transform,notes[i],160,150,790,44,25,Muted);
        }
    }
    private void Missions()
    {
        var content=HomeScroll(1340);
        Text(content,"Mahallenin sana işi var",48,190,984,66,47,Ink);
        Text(content,"Oyna, hedefleri tamamla, ödülünü buradan al.",48,268,984,70,30,Muted);
        for(int i=0;i<3;i++)
        {
            int id=i;int value=Mathf.Min(MahalleProfile.MissionProgress(i),MahalleProfile.MissionTargets[i]);bool claimed=MahalleProfile.Data.claimed[i];
            var card=Panel(content,"Görev "+i,48,370+i*260,984,228,Cream);
            Text(card.transform,MahalleProfile.MissionNames[i],28,23,660,58,35,Ink);
            Text(card.transform,L.F("{0} / {1}    +{2} BONCUK",value,MahalleProfile.MissionTargets[i],MahalleProfile.MissionRewards[i]),28,89,655,45,29,Muted);
            Panel(card.transform,"İlerleme",28,161,612,13,Line);
            Panel(card.transform,"Dolgu",28,161,612*(value/(float)MahalleProfile.MissionTargets[i]),13,Gold);
            var b=LabelButton(card.transform,claimed?"ALINDI":value>=MahalleProfile.MissionTargets[i]?"ÖDÜLÜ AL":"SÜRÜYOR",683,64,266,100,claimed?Line:Ink,claimed?Muted:Cream,()=>{if(MahalleProfile.Claim(id))ShowHome();},28);
            b.interactable=!claimed&&value>=MahalleProfile.MissionTargets[i];
        }
        Text(content,"USTALIK ROZETLERİ",48,1203,984,48,28,Muted);
        for(int i=0;i<5;i++)
        {bool won=MahalleProfile.DistrictCompleted(i);var star=Art(content,"Ustalık rozeti",MahalleGraphic.Shape.Star,75+i*198,1280,116,116,won?Gold:Line);Text(content,Campaign.Districts[i],48+i*198,1410,186,70,24,won?Ink:Muted,TextAlignmentOptions.Center);}
    }
    private void OpenLevel(int index)
    {if(!MahalleProfile.Unlocked(index))return;if(index==0&&!MahalleProfile.Data.howToPlayDone&&MahalleProfile.Data.stars[0]==0){StartTutorial(false);return;}GameSession.TutorialMode=false;GameSession.SelectedLevelIndex=index;Time.timeScale=1;SceneManager.LoadScene(GameSession.GameSceneName);}
    private void ShowGame()
    {
        ClearPage();lastScore=lastShots=-1;resultShown=false;
        var top=Panel(page,"Oyun başlığı",24,12,1032,246,Ink);top.radius=30;
        string title=L.T(Campaign.Districts[controller.Level.district])+"  /  "+(controller.LevelIndex%12+1).ToString("00");
        if(controller.Level.district==2 && controller.LevelIndex%12<3)title+=" · "+L.T(controller.Level.levelName);
        if(GameSession.TutorialMode)title+="  ·  "+L.T("NASIL OYNANIR");
        Text(top.transform,title,28,14,830,59,36,Cream);
        if(GameSession.TutorialMode)LabelButton(top.transform,"GEÇ",872,16,134,92,new Color(.28f,.38f,.31f),Cream,SkipTutorial,30);
        else LabelButton(top.transform,"II",902,16,104,92,new Color(.28f,.38f,.31f),Cream,Pause,42);
        var score=Panel(top.transform,"Misket sayacı",24,92,330,100,new Color(.24f,.34f,.29f));
        Text(score.transform,"ÇIKAN MİSKET",18,10,294,29,23,new Color(.76f,.8f,.7f));scoreLabel=Text(score.transform,"0",18,39,294,51,39,Cream);
        var shots=Panel(top.transform,"Atış sayacı",374,92,255,100,new Color(.24f,.34f,.29f));
        Text(shots.transform,"KALAN ATIŞ",18,10,219,29,23,new Color(.76f,.8f,.7f));shotsLabel=Text(shots.transform,"5",18,39,219,51,39,Gold);
        var wallet=Panel(top.transform,"Boncuk",649,117,250,75,new Color(.24f,.34f,.29f));
        Art(wallet.transform,"Boncuk",MahalleGraphic.Shape.Marble,18,16,43,43,Gold);beadsLabel=Text(wallet.transform,MahalleProfile.Beads.ToString(),79,4,156,67,34,Cream);
        Text(top.transform,GameSession.TutorialMode?"HEDEF: BÜTÜN MİSKETLERİ ÇEMBERİN DIŞINA ÇIKAR":L.F("HEDEF: ÇEMBERDEN EN AZ {0} MİSKET ÇIKAR",controller.Level.oneStarTarget),26,202,960,32,25,new Color(.81f,.84f,.75f));

        // BOLUM IMZASI (sadece test yapisi acikken).
        // "Editorde baska, telefonda baska bolum cikiyor" supheleri icin.
        // Bolum verisinin parmak izi: misket sayisi, engel sayisi, saha boyu
        // ve ilk misketin yeri. Ayni bolumde iki tarafta AYNI yaziyorsa
        // bolumler ayni demektir; farkliysa calisan iki binary farklidir.
        if(MahalleProfile.TestUnlockAllLevels||MahalleProfile.TestInfiniteBeads)
        {
            var lv=controller.Level;
            int mn=lv.marbles!=null?lv.marbles.Length:0;
            int en=lv.obstacles!=null?lv.obstacles.Length:0;
            string imza="#"+controller.LevelIndex+" · "+mn+"m "+en+"e · saha "+lv.arenaSize.ToString("0.00");
            if(mn>0)imza+=" · ilk "+lv.marbles[0].x.ToString("0.00")+","+lv.marbles[0].z.ToString("0.00");
            Text(top.transform,imza,26,236,960,30,21,new Color(1f,.55f,.30f,.85f));
            // Cizim tanisi: mor/eksik nesne varsa adiyla yazar.
            Text(top.transform,MahalleWorld.Diagnose(),26,264,960,30,21,new Color(1f,.45f,.35f,.9f));
        }
        var dock=Panel(page,"Atış alanı",24,0,1032,178,Ink);Bottom(dock.rectTransform,24,18,1032,178);
        var bag=Button(dock.transform,"Misket kesesi",16,18,330,140,new Color(.3f,.4f,.3f),OpenBag);
        var icon=Art(bag.transform,"Kese",MahalleGraphic.Shape.Bag,21,36,65,70,Gold);icon.accent=Cream;
        Text(bag.transform,"KESEM",105,24,197,53,34,Cream);Text(bag.transform,"Özel misket seç",105,78,206,40,23,new Color(.8f,.83f,.73f));
        if(GameSession.TutorialMode){tutBag=(RectTransform)bag.transform;bag.gameObject.SetActive(false);}
        powerLabel=Text(dock.transform,"NORMAL MİSKET",378,22,610,53,29,Cream);
        Text(dock.transform,"ATIŞ GÜCÜ",378,79,610,34,21,new Color(.8f,.83f,.73f));
        Panel(dock.transform,"Güç boş",378,126,610,16,new Color(.33f,.42f,.35f));powerFill=Panel(dock.transform,"Güç dolu",378,126,1,16,Gold);
        hintLabel=Text(page,"",48,0,984,62,29,Cream,TextAlignmentOptions.Center);Bottom(hintLabel.rectTransform,48,220,984,62);
        if(GameSession.TutorialMode)TutorialPanel();
        else if(!MahalleProfile.Data.tutorialDone)
        {
            var tutorial=Panel(page,"İlk atış",110,0,860,125,new Color(.16f,.25f,.23f,.94f));Bottom(tutorial.rectTransform,110,302,860,125);
            Text(tutorial.transform,"Geri çek, nişan al, bırak",20,12,820,52,36,Cream,TextAlignmentOptions.Center);
            Text(tutorial.transform,"Atıcıyı taşımak için alt çizgide bir yere dokun.",20,70,820,40,25,new Color(.81f,.84f,.75f),TextAlignmentOptions.Center);
            var g=Art(page,"Öğreten el",MahalleGraphic.Shape.Hand,0,0,65,88,Cream);hand=g.rectTransform;
        }
    }
    private void Update()
    {
        if(DuelSession.Active)
        {
            if(duel==null)return;
            int want=(int)duel.CurrentStep;
            if(want!=duelScreenShown)
            {
                duelScreenShown=want;
                switch(duel.CurrentStep)
                {
                    case DuelController.Step.Toss:      ShowToss(); break;
                    case DuelController.Step.Offer:     ShowOffer(); break;
                    case DuelController.Step.Place:     ShowPlacement(); break;
                    case DuelController.Step.HandOver:  ShowHandOver(); break;
                    case DuelController.Step.Shoot:     if(DuelSession.Well)ShowWell();else ShowDuel(); break;
                    case DuelController.Step.RoundOver: if(DuelSession.Well)WellRoundOver();else ShowRoundOver(); break;
                    case DuelController.Step.Done:      if(DuelSession.Well)WellResult();else DuelResult(); break;
                }
            }
            else if(duel.CurrentStep==DuelController.Step.Toss)RefreshToss();
            else if(duel.CurrentStep==DuelController.Step.Place)RefreshPlacement();
            else if(duel.CurrentStep==DuelController.Step.Shoot){if(DuelSession.Well)RefreshWell();else RefreshDuel();}
            return;
        }
        if(controller==null || scoreLabel==null)return;
        if(lastScore!=controller.Score){lastScore=controller.Score;scoreLabel.SetTextL(controller.Score+" / "+controller.TotalMarbles);}
        if(lastShots!=controller.ShotsLeft){lastShots=controller.ShotsLeft;shotsLabel.SetTextL(controller.ShotsLeft.ToString());}
        beadsLabel.SetTextL(MahalleProfile.Beads.ToString());
        var shooter=controller.Shooter;
        if(shooter!=null)
        {
            powerFill.rectTransform.sizeDelta=new Vector2(Mathf.Max(1,610*shooter.Power),16);
            powerLabel.SetTextL(shooter.SelectedPower==MarblePower.None?(SpecialMarbles.IsSpecial(shooter.ActiveSkin)?L.T(Campaign.SkinNames[shooter.ActiveSkin])+" · "+MahalleProfile.RemainingLife(shooter.ActiveSkin)+"/"+SpecialMarbles.MaxLife:"NORMAL MİSKET"):L.Up(L.T(Campaign.PowerNames[(int)shooter.SelectedPower])));
            hintLabel.SetTextL(controller.WaitingForSettle?"Misketler duruluyor…":shooter.IsAiming?"Gücü ayarla ve bırak":shooter.PositionLocked?"Misketin durduğu yerden atıyorsun.":"Çizgiye dokunarak atıcının yerini değiştirebilirsin.");
            if(hand!=null)
            {
                hand.gameObject.SetActive(!MahalleProfile.Data.tutorialDone && !controller.IsPaused);
                if(Camera.main!=null)
                {
                    var p=Camera.main.WorldToScreenPoint(shooter.transform.position);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(root,p,null,out var local);
                    hand.anchorMin=hand.anchorMax=root.pivot;hand.pivot=new Vector2(.2f,.9f);
                    hand.anchoredPosition=local+new Vector2(24,-25-Mathf.PingPong(Time.unscaledTime*55,100));
                }
            }
            var tutorial=page.Find("İlk atış");if(tutorial!=null&&MahalleProfile.Data.tutorialDone)tutorial.gameObject.SetActive(false);
        }
        if(GameSession.TutorialMode&&tutStep>=0&&controller.State==LevelController.LevelState.Playing)TutorialTick(shooter);
        if(controller.State!=LevelController.LevelState.Playing&&!resultShown){resultShown=true;Results();}
    }
    private RectTransform Modal(string name,float height)
    {
        CloseModal(false);
        modal=Rect(name,root);Stretch(modal);
        var shade=Panel(modal,"Perde",0,0,1080,2600,new Color(.06f,.1f,.08f,.78f),true);shade.radius=0;Stretch(shade.rectTransform);
        var box=Panel(modal,name+" içeriği",48,0,984,height,Paper,true);
        var r=box.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(984,height);
        return r;
    }
    private void CloseModal(bool resume=true)
    {
        if(modal!=null){modal.gameObject.SetActive(false);Destroy(modal.gameObject);modal=null;}
        if(resume&&controller!=null&&controller.State==LevelController.LevelState.Playing)controller.SetPaused(false);
    }
    private void OpenBag()
    {
        // Öğreticide 4. adımda kese anlatılır; 5. adımdan sonra gerçek kese açılır.
        if(GameSession.TutorialMode&&tutStep!=4){if(tutStep==5)TutorialKesemInfo(()=>SetTutStep(6));return;}
        if(controller.WaitingForSettle||controller.State!=LevelController.LevelState.Playing){Toast("Atışın tamamlanmasını bekle.");return;}
        controller.SetPaused(true);var box=Modal("Misket kesen",1130);
        Text(box,"Kesen",36,28,810,65,49,Ink);
        Text(box,"Seçim ücretsiz. Bir kullanım yalnızca atışta harcanır.",36,108,912,74,28,Muted);
        var powerColors=new[]{Gold,Campaign.SkinColors[5],Campaign.SkinColors[1],Campaign.SkinColors[3]};
        for(int i=0;i<Campaign.PowerNames.Length;i++)
        {
            int power=i;bool usable=MahalleProfile.CanUse((MarblePower)i),selected=controller.Shooter.SelectedPower==(MarblePower)i;
            var b=Button(box,Campaign.PowerNames[i],30,215+i*190,924,166,selected?Ink:Cream,()=>{
                if(!usable){Toast("Boncuk kazanmak için normal misketle oynayabilirsin.");return;}
                CloseModal();controller.Shooter.SelectPower((MarblePower)power);
            });
            var m=Art(b.transform,"Özel misket",MahalleGraphic.Shape.Marble,22,30,i==0?100:82,i==0?100:82,powerColors[i]);
            Text(b.transform,Campaign.PowerNames[i],145,17,730,47,36,selected?Cream:Ink);
            Text(b.transform,Campaign.PowerDescriptions[i],145,64,740,52,25,selected?Cream:Muted);
            string cost=MahalleProfile.Data.stock[i]>0?L.F("ÜCRETSİZ HAK: {0}",MahalleProfile.Data.stock[i]):L.F("{0} BONCUK / ATIŞ",Campaign.PowerPrices[i]);
            Text(b.transform,selected?"SEÇİLİ · DOKUNARAK VAZGEÇ":cost,145,119,740,33,24,selected?Gold:usable?Ink:Muted);
        }
        LabelButton(box,"OYUNA DÖN",30,1009,924,87,Ink,Cream,()=>CloseModal(),30);
    }
    private void Pause()
    {
        controller.SetPaused(true);var box=Modal("Mola",806);
        Text(box,"Bir nefes al",36,34,912,81,57,Ink,TextAlignmentOptions.Center);
        LabelButton(box,"DEVAM ET",36,164,912,100,Ink,Cream,()=>CloseModal());
        LabelButton(box,"TEKRAR DENE",36,290,912,100,new Color(.88f,.83f,.69f),Ink,()=>{CloseModal();controller.RestartLevel();ShowGame();});
        LabelButton(box,"MAHALLEYE DÖN",36,416,912,100,new Color(.88f,.83f,.69f),Ink,()=>controller.OpenLevelSelect());
        LabelButton(box,"ANA MENÜ",36,542,912,100,new Color(.88f,.83f,.69f),Ink,()=>{returnToTitle=true;controller.OpenLevelSelect();});
        LabelButton(box,"SES VE TİTREŞİM",36,680,912,78,Paper,Muted,Settings,27);
    }
    private void Results()
    {
        if(GameSession.TutorialMode){TutorialDone();return;}
        bool won=controller.State==LevelController.LevelState.Won;
        int need=MahalleProfile.Required(controller.LevelIndex);
        bool passed=won&&controller.Stars>=need;
        var box=Modal("Bölüm sonucu",won&&passed&&controller.HasNextLevel?1110:1010);
        Text(box,won?(controller.LastReward.newBadge||(controller.Level.mastery&&passed)?"MAHALLE USTASI!":"GÜZEL ATIŞLAR!"):"BİR DAHA DENE",30,34,924,77,51,Ink,TextAlignmentOptions.Center);
        Text(box,controller.Level.levelName,30,114,924,47,31,Muted,TextAlignmentOptions.Center);
        for(int i=0;i<3;i++){var star=Art(box,"Sonuç yıldızı "+i,MahalleGraphic.Shape.Star,259+i*163,i==1?184:201,i==1?142:115,i==1?142:115,Line);if(i<controller.Stars&&won)StartCoroutine(RevealStar(star,i));}
        Text(box,L.F("{0} / {1} misket çıkardın",controller.Score,controller.TotalMarbles),30,366,924,64,40,Ink,TextAlignmentOptions.Center);
        string reward=!won?"Kesendeki güçler yardımcı olabilir. Normal misketle de geçebilirsin.":controller.LastReward.beads>0?L.F("+{0} BONCUK",controller.LastReward.beads):"Bu bölümden boncuk aldın. Yeni yıldız daha fazla kazandırır.";
        Text(box,reward,60,448,864,82,won?38:28,won?Ink:Muted,TextAlignmentOptions.Center);
        if(controller.LastReward.newBadge)Text(box,L.F("MAHALLE TAMAMLANDI · {0} BONCUK BONUS",controller.LastReward.districtBonus)+"\n"+L.T(controller.LastReward.newSkin??"Mahalle misketi zaten kesende")+" · "+L.T("KOLEKSİYON ÖDÜLÜ"),30,538,924,100,28,Muted,TextAlignmentOptions.Center);
        else if(won&&!passed)Text(box,L.F("Sonraki bölümü açmak için {0} yıldız gerekiyor.",need),60,552,864,72,29,new Color(.72f,.32f,.24f),TextAlignmentOptions.Center);
        else Text(box,won?"Yeni rekorlar ve görevler daha fazla boncuk kazandırır.":"İpucu: Alt çizgide yer değiştirip kümeye yandan vur.",60,556,864,68,27,Muted,TextAlignmentOptions.Center);
        if(passed&&controller.HasNextLevel)LabelButton(box,"SONRAKİ BÖLÜM",36,669,912,98,Ink,Cream,()=>controller.LoadNextLevel());
        else LabelButton(box,passed?"MAHALLEYE DÖN":"TEKRAR DENE",36,669,912,98,Ink,Cream,()=>{if(passed)controller.OpenLevelSelect();else{CloseModal();controller.RestartLevel();ShowGame();}});
        LabelButton(box,passed?"REKORUNU GELİŞTİR":"MAHALLEYE DÖN",36,791,912,86,new Color(.88f,.83f,.69f),Ink,()=>{if(passed){CloseModal();controller.RestartLevel();ShowGame();}else controller.OpenLevelSelect();},30);
        if(passed&&controller.HasNextLevel)LabelButton(box,"MAHALLE HARİTASI",36,898,912,76,Paper,Muted,()=>controller.OpenLevelSelect(),27);
        if(won)LabelButton(box,"PAYLAŞ",36,passed&&controller.HasNextLevel?995:898,912,86,Gold,Ink,ShareCard,30);
    }
    // ---------------------------------------------------------------
    // PAYLAŞ KARTI
    // Instagram story ölçüsünde (1080x1920, 9:16) sonuç kartı. Kart ekranda
    // önizleme olarak çizilir, PAYLAŞ'a basınca düğmeler gizlenip kartın
    // bulunduğu bölge ekrandan kesilir ve paylaşım menüsü açılır.
    // ---------------------------------------------------------------
    private bool capturing;
    private void ShareCard()
    {
        CloseModal(false);
        modal=Rect("Paylaş kartı",root);Stretch(modal);
        var shade=Panel(modal,"Perde",0,0,1080,2600,new Color(.03f,.04f,.03f,.95f),true);shade.radius=0;Stretch(shade.rectTransform);
        Canvas.ForceUpdateCanvases();
        float scale=Mathf.Clamp((root.rect.height-190f)/1920f,.3f,1f);
        var card=Rect("Kart",modal);
        card.anchorMin=card.anchorMax=new Vector2(.5f,1);card.pivot=new Vector2(.5f,1);
        card.sizeDelta=new Vector2(1080,1920);card.anchoredPosition=new Vector2(0,-20);card.localScale=Vector3.one*scale;
        BuildShareCard(card);
        var bar=Rect("Paylaş düğmeleri",modal);
        bar.anchorMin=bar.anchorMax=new Vector2(.5f,0);bar.pivot=new Vector2(.5f,0);bar.sizeDelta=new Vector2(984,110);bar.anchoredPosition=new Vector2(0,30);
        var group=bar.gameObject.AddComponent<CanvasGroup>();
        LabelButton(bar,"PAYLAŞ",0,0,620,110,Gold,Ink,()=>StartCoroutine(CaptureCard(card,group)),38);
        LabelButton(bar,"GERİ",640,0,344,110,new Color(.24f,.34f,.30f),Cream,Results,30);
    }
    private void BuildShareCard(RectTransform card)
    {
        var lv=controller.Level;
        int no=controller.LevelIndex%Campaign.PerDistrict+1;
        int stars=controller.State==LevelController.LevelState.Won?controller.Stars:0;
        var chalk=new Color(1,.98f,.92f,.92f);
        var bg=Panel(card,"Zemin",0,0,1080,1920,new Color(.14f,.12f,.10f));bg.radius=0;
        Text(card,L.Up(L.T(Campaign.Districts[lv.district]))+" · "+no.ToString("00"),0,150,1080,70,46,new Color(1,.97f,.89f,.7f),TextAlignmentOptions.Center);
        Text(card,lv.levelName,40,222,1000,100,68,Cream,TextAlignmentOptions.Center);
        var ring=Art(card,"Tebeşir çemberi",MahalleGraphic.Shape.ChalkRing,150,400,780,780,chalk);ring.stroke=10f;ring.progress=1f;
        for(int i=0;i<3;i++)
        {
            float size=i==1?170:135;
            Art(card,"Yıldız "+i,MahalleGraphic.Shape.Star,540-size/2+(i-1)*185,i==1?510:545,size,size,i<stars?Gold:new Color(1,.98f,.92f,.18f));
        }
        Text(card,controller.ShotsUsed.ToString(),150,700,780,210,200,Cream,TextAlignmentOptions.Center);
        Text(card,"ATIŞTA",150,905,780,70,52,new Color(1,.97f,.89f,.75f),TextAlignmentOptions.Center);
        Text(card,L.F("{0} / {1} MİSKET",controller.Score,controller.TotalMarbles),150,990,780,60,40,Gold,TextAlignmentOptions.Center);
        // Çemberin dışına savrulmuş birkaç misket.
        float[,] spots={{110,1250,70},{900,1210,58},{840,320,50},{60,470,44},{960,1330,40}};
        for(int i=0;i<spots.GetLength(0);i++)
        {
            var col=Campaign.SkinColors[i%6];
            var m=Art(card,"Misket "+i,MahalleGraphic.Shape.Marble,spots[i,0],spots[i,1],spots[i,2],spots[i,2],col);
            m.accent=Color.Lerp(col,Color.white,.55f);
        }
        Text(card,"Sen kaç atışta bitirirsin?",0,1340,1080,70,46,Cream,TextAlignmentOptions.Center);
        Text(card,"MİSKETR",0,1560,1080,170,140,new Color(1,.98f,.92f),TextAlignmentOptions.Center);
        Text(card,"mahallenin en iyi nişancısı kim?",0,1730,1080,56,36,new Color(1,.97f,.89f,.62f),TextAlignmentOptions.Center);
    }
    private string ShareText()
    {
        int no=controller.LevelIndex%Campaign.PerDistrict+1;
        return L.F("MİSKETR · {0} {1} bölümünü {2} atışta {3} yıldızla bitirdim. Sen kaç atışta bitirirsin?",L.T(Campaign.Districts[controller.Level.district]),no.ToString("00"),controller.ShotsUsed,controller.Stars);
    }
    private IEnumerator CaptureCard(RectTransform card,CanvasGroup buttons)
    {
        if(capturing)yield break;
        capturing=true;
        buttons.alpha=0f;
        yield return new WaitForEndOfFrame();
        var shot=ScreenCapture.CaptureScreenshotAsTexture();
        buttons.alpha=1f;
        // Overlay canvas'ta dünya köşeleri ekran pikselidir.
        var c=new Vector3[4];card.GetWorldCorners(c);
        int x=Mathf.Clamp(Mathf.RoundToInt(c[0].x),0,shot.width-1);
        int y=Mathf.Clamp(Mathf.RoundToInt(c[0].y),0,shot.height-1);
        int w=Mathf.Clamp(Mathf.RoundToInt(c[2].x)-x,1,shot.width-x);
        int h=Mathf.Clamp(Mathf.RoundToInt(c[2].y)-y,1,shot.height-y);
        var tex=new Texture2D(w,h,TextureFormat.RGB24,false);
        tex.SetPixels(shot.GetPixels(x,y,w,h));tex.Apply();
        Destroy(shot);
        string path=System.IO.Path.Combine(Application.temporaryCachePath,"misketr-kart.png");
        System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());
        Destroy(tex);
        capturing=false;
        MisketrShare.ShareImage(path,ShareText());
    }
    // ---------------------------------------------------------------
    // ÖĞRETİCİ
    // Isınma sahasında 4 adım: yer seç, geri çek, nişan al-bırak, çıkar.
    // Her adım oyuncu onu yapınca geçer. İlk bölüme ilk girişte otomatik,
    // sonra Ayarlar > NASIL OYNANIR ile açılır.
    // ---------------------------------------------------------------
    private static bool tutorialFromSettings;
    private int tutStep=-1;
    private int tutSide;
    private float tutNudge;
    private MahalleGraphic tutSpot;
    private RectTransform tutBag;
    private bool tutKesemDone,tutFired;
    private int tutRetry;
    private const float TutSpotOffset=1.4f,TutSpotReach=.6f;
    private TextMeshProUGUI tutTitle,tutBody;
    private RectTransform tutHand;
    private void StartTutorial(bool fromSettings)
    {
        tutorialFromSettings=fromSettings;
        GameSession.TutorialMode=true;
        GameSession.SelectedLevelIndex=0;
        Time.timeScale=1;
        SceneManager.LoadScene(GameSession.GameSceneName);
    }
    private void EndTutorial(int nextLevel)
    {
        GameSession.TutorialMode=false;
        Time.timeScale=1;
        if(tutorialFromSettings){tutorialFromSettings=false;SceneManager.LoadScene(GameSession.LevelSelectSceneName);return;}
        GameSession.SelectedLevelIndex=MahalleProfile.Unlocked(nextLevel)?nextLevel:0;
        SceneManager.LoadScene(GameSession.GameSceneName);
    }
    private void SkipTutorial()
    {
        MahalleProfile.Data.howToPlayDone=true;MahalleProfile.Save();
        EndTutorial(TutorialLevel.LevelIndex);
    }
    private void TutorialPanel()
    {
        var panel=Panel(page,"Öğretici",60,0,960,190,new Color(.16f,.25f,.23f,.95f));Bottom(panel.rectTransform,60,300,960,190);
        tutTitle=Text(panel.transform,"",28,14,904,54,34,Gold,TextAlignmentOptions.Center);
        tutBody=Text(panel.transform,"",28,70,904,106,29,Cream,TextAlignmentOptions.Center);
        tutSpot=Art(page,"Öğretici noktası",MahalleGraphic.Shape.ChalkRing,0,0,120,120,Gold);tutSpot.stroke=7f;
        tutHand=Art(page,"Öğretici eli",MahalleGraphic.Shape.Hand,0,0,65,88,Cream).rectTransform;
        tutSide=0;tutNudge=0;tutKesemDone=false;tutRetry=0;
        controller.Shooter.AimBlocked+=()=>tutNudge=2.2f;
        SetTutStep(0);
    }
    private void SetTutStep(int step,bool retry=false)
    {
        tutStep=step;tutFired=false;
        if(step==4&&!controller.TutorialFinalPhase)controller.StartTutorialFinal();
        controller.Shooter.AimLocked=step==0||step==5;
        if(tutBag!=null){tutBag.gameObject.SetActive(step>=4);tutBag.localScale=Vector3.one;}
        string[] titles={tutSide==0?"1/5 · YERİNİ SEÇ (SAĞ)":"1/5 · YERİNİ SEÇ (SOL)","2/5 · GERİ ÇEK","3/5 · NİŞAN AL VE BIRAK","BAKALIM…","5/5 · HEPSİNİ ÇIKAR","4/5 · KESEM"};
        string[] bodies={
            tutSide==0?"Atmadan önce yerini ayarlarsın. Misketin çizgi boyunca kayar. Sağdaki parlayan noktaya dokun.":"Güzel! Şimdi soldaki parlayan noktaya dokun. Her atıştan önce en iyi açıyı böyle bulursun.",
            "Misketine dokun ve parmağını GERİ çek. Ne kadar çekersen o kadar güçlü atar.",
            "Kesik çizgi misketin gideceği yönü gösterir. Kümeye çevir ve parmağını bırak.",
            "Misketler duruluyor.",
            "Güçleri öğrendin, saha yeniden dizildi. Şimdi bölümü bitir: bütün misketleri çıkar. İstersen kesendeki güçleri kullanabilirsin, öğreticide bedava.",
            "Harika, çemberden çıkan misket senin! Zor anlar için bir kesen var. Soldaki KESEM'e dokun."};
        if(step>=6)
        {
            string[] pt={"KESEM 1/4 · BAŞ MİSKET","KESEM 2/4 · DEMİR MİSKET","KESEM 3/4 · USTA GÖZÜ","KESEM 4/4 · YERİNDE KAL","YERİNDE KAL · ŞİMDİ ORADAN AT"};
            string[] pb={
                "Misketin büyüdü! Büyük misket sık kümeleri dağıtır. Kümeye at ve farkı gör.",
                "Misketin küçük ve ağır oldu. Dar aralıktan geçip sert vurur. Bir misketi hedefle ve at.",
                "Nişan alırken misketin İLK nereye değeceği gösterilir. Çek, işarete bak, tam isabetle bırak.",
                "Bu güç farklı: misketin atıştan sonra ÇİZGİYE DÖNMEZ, durduğu yerde kalır. Çembere yakın durması için hafif at.",
                "Misketin durduğu yerde kaldı. Bu atışı oradan yapıyorsun: yakından nişan al ve at. Sonra yine çizgiye döner."};
            tutTitle.SetTextL(retry?"YERİNDE KAL · TEKRAR DENE":pt[step-6]);
            tutBody.SetTextL(retry?"Misketin çemberden uzakta kaldı, çizgiye döndü (normal oyunda hakkın iade edilir). Daha HAFİF at, çembere yakın dursun.":pb[step-6]);
            return;
        }
        tutTitle.SetTextL(retry?"TEKRAR DENE":titles[step]);
        tutBody.SetTextL(retry?"Misketi çemberin DIŞINA kadar itmelisin. Biraz daha geri çek, daha güçlü atar.":bodies[step]);
    }
    private void TutorialTick(ShotController shooter)
    {
        if(shooter==null)return;
        bool knocked=controller.TutorialKnocked>0;
        if(tutStep>=6&&tutStep<=10)
        {
            tutHand.gameObject.SetActive(false);tutSpot.gameObject.SetActive(false);
            if(controller.WaitingForSettle){tutFired=true;return;}
            if(!tutFired)
            {
                // Sıradaki gücü kendiliğinden seç (Yerinde Kal ikinci atışı normal misketle).
                var want=tutStep==10?MarblePower.None:(MarblePower)(tutStep-6);
                if(shooter.SelectedPower!=want&&!shooter.IsAiming&&modal==null)shooter.SelectPower(want);
                return;
            }
            if(tutStep==9)
            {
                if(shooter.PositionLocked)SetTutStep(10);
                else if(tutRetry<1){tutRetry++;SetTutStep(9,true);}
                else SetTutStep(4);
                return;
            }
            SetTutStep(tutStep==10?4:tutStep+1);
            return;
        }
        switch(tutStep)
        {
            case 0:
            {
                float startX=controller.Level.shooterStartPosition.x,x=shooter.transform.position.x;
                if(tutSide==0&&x>=startX+TutSpotReach){tutSide=1;tutNudge=0;SetTutStep(0);}
                else if(tutSide==1&&x<=startX-TutSpotReach){tutNudge=0;SetTutStep(1);}
                if(tutNudge>0)
                {
                    tutNudge-=Time.unscaledDeltaTime;
                    tutTitle.SetTextL("ÖNCE YERİNİ SEÇ");
                    tutBody.SetTextL(L.F("Misketini çekmeden önce {0} parlayan noktaya dokun.",L.T(tutSide==0?"sağdaki":"soldaki")));
                    if(tutNudge<=0)SetTutStep(0);
                }
                break;
            }
            case 1: if(shooter.IsAiming)SetTutStep(2); break;
            case 2:
                if(controller.WaitingForSettle)SetTutStep(3);
                else if(!shooter.IsAiming)SetTutStep(knocked?(tutKesemDone?4:5):1);
                break;
            case 3: if(!controller.WaitingForSettle){if(knocked)SetTutStep(tutKesemDone?4:5);else SetTutStep(1,true);} break;
        }
        if(tutStep==5&&tutBag!=null)
        {
            // KESEM düğmesi nabız gibi atar, el onu gösterir.
            tutBag.localScale=Vector3.one*(1f+Mathf.Abs(Mathf.Sin(Time.unscaledTime*4f))*.06f);
            tutSpot.gameObject.SetActive(false);
            bool free=modal==null;
            tutHand.gameObject.SetActive(free);
            if(!free)return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,tutBag.TransformPoint(tutBag.rect.center),null,out var bagLocal);
            tutHand.anchorMin=tutHand.anchorMax=root.pivot;tutHand.pivot=new Vector2(.2f,.9f);
            tutHand.anchoredPosition=bagLocal+new Vector2(10,-10-Mathf.Abs(Mathf.Sin(Time.unscaledTime*4f))*18f);
            return;
        }
        bool show=(tutStep==0||tutStep==1)&&!controller.IsPaused&&!controller.WaitingForSettle&&Camera.main!=null;
        tutHand.gameObject.SetActive(show);
        tutSpot.gameObject.SetActive(show&&tutStep==0);
        if(!show)return;
        Vector3 world=shooter.transform.position;
        if(tutStep==0){var s=controller.Level.shooterStartPosition;world=new Vector3(s.x+(tutSide==0?TutSpotOffset:-TutSpotOffset),world.y,world.z);}
        var p=Camera.main.WorldToScreenPoint(world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root,p,null,out var local);
        if(tutStep==0)
        {
            var sr=tutSpot.rectTransform;
            sr.anchorMin=sr.anchorMax=root.pivot;sr.pivot=new Vector2(.5f,.5f);sr.anchoredPosition=local;
            float pulse=1f+Mathf.Sin(Time.unscaledTime*5f)*.12f;
            sr.localScale=Vector3.one*pulse;
            var c=Gold;c.a=.65f+Mathf.Sin(Time.unscaledTime*5f)*.3f;tutSpot.color=c;
        }
        tutHand.anchorMin=tutHand.anchorMax=root.pivot;tutHand.pivot=new Vector2(.2f,.9f);
        float anim=tutStep==0?Mathf.Abs(Mathf.Sin(Time.unscaledTime*4f))*18f:Mathf.PingPong(Time.unscaledTime*55,100);
        tutHand.anchoredPosition=local+new Vector2(24,-25-anim);
    }
    // KESEM'i gerçek keseyi açmadan anlatır; ücretsiz haklar harcanmaz.
    private void TutorialKesemInfo(Action after)
    {
        controller.SetPaused(true);
        tutHand.gameObject.SetActive(false);
        var box=Modal("Kese anlatımı",1180);
        Text(box,"Kesen",36,28,912,70,50,Ink);
        Text(box,"Zorlandığın bölümlerde atıştan önce bir güç seçebilirsin. Her güçten 1 ÜCRETSİZ hakkın var, sonrası boncukla.",36,104,912,110,28,Muted);
        var colors=new[]{Gold,Campaign.SkinColors[5],Campaign.SkinColors[1],Campaign.SkinColors[3]};
        for(int i=0;i<Campaign.PowerNames.Length;i++)
        {
            var row=Panel(box,Campaign.PowerNames[i],30,236+i*160,924,140,Cream);
            Art(row.transform,"Güç",MahalleGraphic.Shape.Marble,22,28,84,84,colors[i]);
            Text(row.transform,Campaign.PowerNames[i],130,16,770,48,34,Ink);
            Text(row.transform,Campaign.PowerDescriptions[i],130,64,770,64,26,Muted);
        }
        Text(box,"Boncukları bölüm bitirerek ve görevlerle kazanırsın. Her bölüm normal misketle de geçilebilir.",36,892,912,100,27,Muted,TextAlignmentOptions.Center);
        LabelButton(box,"ANLADIM",36,1030,912,110,Ink,Cream,()=>{tutKesemDone=true;CloseModal();after?.Invoke();},38);
    }
    private void TutorialDone()
    {
        if(!tutKesemDone){TutorialKesemInfo(TutorialDone);return;}
        tutStep=-1;
        if(tutHand!=null)tutHand.gameObject.SetActive(false);
        if(tutSpot!=null)tutSpot.gameObject.SetActive(false);
        controller.Shooter.AimLocked=false;
        MahalleProfile.Data.howToPlayDone=true;MahalleProfile.Data.tutorialDone=true;MahalleProfile.Save();
        var box=Modal("Öğretici bitti",880);
        Text(box,"HAZIRSIN!",36,34,912,90,60,Ink,TextAlignmentOptions.Center);
        int beads=controller.LastReward.beads;
        Text(box,L.F("1. bölüm tamam · {0} yıldız",controller.Stars)+(beads>0?L.F(" · +{0} boncuk",beads):""),36,690,912,50,30,Muted,TextAlignmentOptions.Center);
        string[] lines={
            "Ne kadar çok misket çıkarırsan o kadar çok yıldız kazanırsın.",
            "Her bölümde atış hakkın sınırlı. Hedefe ulaşırsan sonraki bölüm açılır.",
            "Zorlanırsan KESEM'deki güçleri dene. Yine de her bölüm normal misketle geçilebilir."};
        var shapes=new[]{MahalleGraphic.Shape.Star,MahalleGraphic.Shape.Marble,MahalleGraphic.Shape.Bag};
        for(int i=0;i<3;i++)
        {
            Art(box,"Simge "+i,shapes[i],56,170+i*130,70,70,Gold);
            Text(box,lines[i],150,160+i*130,780,100,30,Ink);
        }
        LabelButton(box,tutorialFromSettings?"TAMAM":"2. BÖLÜME GEÇ",36,750,912,110,Ink,Cream,()=>EndTutorial(TutorialLevel.LevelIndex+1),38);
    }
    private IEnumerator RevealStar(MahalleGraphic star,int index)
    {
        yield return new WaitForSecondsRealtime(.25f+index*.24f);
        if(star==null)yield break;star.color=Gold;float t=0;
        while(t<.24f&&star!=null){t+=Time.unscaledDeltaTime;star.transform.localScale=Vector3.one*(1+Mathf.Sin(Mathf.Clamp01(t/.24f)*Mathf.PI)*.22f);yield return null;}
    }
    private void Settings()
    {
        var box=Modal("Ayarlar",974);Text(box,"Ayarlar",36,30,912,77,52,Ink);
        LabelButton(box,L.F("SES: {0}",L.T(MahalleProfile.Data.sound?"AÇIK":"KAPALI")),36,158,912,100,Ink,Cream,()=>{MahalleProfile.Data.sound=!MahalleProfile.Data.sound;MahalleProfile.Save();Settings();});
        LabelButton(box,L.F("TİTREŞİM: {0}",L.T(MahalleProfile.Data.haptics?"AÇIK":"KAPALI")),36,282,912,100,Ink,Cream,()=>{MahalleProfile.Data.haptics=!MahalleProfile.Data.haptics;MahalleProfile.Save();Settings();});
        LabelButton(box,L.F("DİL: {0}",L.English?"ENGLISH":"TÜRKÇE"),36,415,912,89,new Color(.88f,.83f,.69f),Ink,ToggleLanguage,29);
        LabelButton(box,"NASIL OYNANIR",36,527,912,89,new Color(.88f,.83f,.69f),Ink,()=>StartTutorial(true),29);
        LabelButton(box,"İLERLEMEYİ SIFIRLA",36,639,912,89,new Color(.88f,.83f,.69f),Ink,ResetPrompt,29);
        Text(box,"Boncuklar oyun içinden kazanılır. Gerçek para işlemi yoktur.",36,751,912,64,25,Muted,TextAlignmentOptions.Center);
        LabelButton(box,"GERİ",36,849,912,87,Ink,Cream,()=>{if(controller!=null)Pause();else CloseModal();},29);
    }
    // Dil değişince ekran yeni dille baştan kurulur. Oyundaysa bölüm yeniden başlar.
    private void ToggleLanguage()
    {
        L.SetEnglish(!L.English);
        Time.timeScale=1;
        if(controller==null)returnToTitle=true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    private void ResetPrompt()
    {
        var box=Modal("Sıfırlama onayı",580);Text(box,"İlerleme sıfırlansın mı?",36,40,912,86,44,Ink,TextAlignmentOptions.Center);
        Text(box,"Bu sürümdeki yıldızların, boncukların ve koleksiyonun başlangıca döner.",52,161,880,115,32,Muted,TextAlignmentOptions.Center);
        LabelButton(box,"VAZGEÇ",36,322,440,100,Ink,Cream,Settings,31);
        LabelButton(box,"SIFIRLA",504,322,444,100,new Color(.72f,.32f,.24f),Cream,()=>{MahalleProfile.Reset();if(controller!=null)controller.OpenLevelSelect();else{district=tab=0;ShowHome();}},31);
    }
    private void Toast(string message) {StartCoroutine(ToastRoutine(message));}
    private IEnumerator ToastRoutine(string message)
    {
        var g=Panel(root,"Bilgi",48,0,984,116,Ink,true);Bottom(g.rectTransform,48,276,984,116);
        Text(g.transform,message,24,12,936,92,28,Cream,TextAlignmentOptions.Center);
        yield return new WaitForSecondsRealtime(2.7f);if(g!=null)Destroy(g.gameObject);
    }
}
