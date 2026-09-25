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
    private TextMeshProUGUI hintShadow;
    private ShooterLine shooterLine;
    private RectTransform oynaRect;
    private static readonly Color CardBg=new Color(.22f,.31f,.27f), CardBgB=new Color(.15f,.22f,.19f);
    private float scorePop;
    private int district,tab,lastScore=-1,lastShots=-1;
    private int selectedLevel=-1;          // haritada secili bolum
    private TextMeshProUGUI playLabel;     // alttaki tek OYNA dugmesinin yazisi
    // Açılış ekranı uygulama başına BİR kez gösterilir. Bölümden çıkıp
    // mahalleye dönmek LevelSelect sahnesini yeniden yüklüyor; bu bayrak
    // olmadan oyuncu her dönüşünde tebeşir animasyonunu baştan izlerdi.
    // static olduğu için sahne değişimlerini aşar, uygulama kapanınca sıfırlanır.
    private static bool titleShown;
    // Bir ekrandan cikinca harita degil baslik ekrani acilsin.
    private static bool returnToTitle;
    private bool resultShown;
    public RectTransform Root => root;
    private void Start()
    {
        font=Resources.Load<TMP_FontAsset>("Mahalle/Nunito SDF");
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=0;
        // iPAD OYUN EKRANI: telefondan geniş ekranda (9:16'dan geniş) arayüz yüksekliğe göre
        // ölçeklenir ve ortada 1080'lik sütun olur. Genişliğe göre ölçeklenince üst panel
        // ekranın %25'ini kaplıyor, kamera sahayı ufacık gösteriyordu (simülatörde görüldü).
        // Kamera aynı sütunu hesaba katar: MahalleWorld.UiCanvasHeight. Telefonlar değişmedi.
        bool column=MahalleWorld.UiColumn(Screen.width/(float)Mathf.Max(1,Screen.height))&&FindFirstObjectByType<LevelController>()!=null;
        if(column)scaler.matchWidthOrHeight=1;
        gameObject.AddComponent<GraphicRaycaster>();
        // TAM EKRAN ZEMİN: güvenli alanın dışı (çentik/Dynamic Island ve ev çubuğu
        // şeritleri) ekranın zemin rengiyle boyanır. İçerik güvenli alanda kalır.
        bleedTop=Bleed("Üst taşma",new Vector2(0,.5f),Vector2.one);
        bleedBottom=Bleed("Alt taşma",Vector2.zero,new Vector2(1,.5f));
        root=Rect("SafeArea",transform);Stretch(root);root.gameObject.AddComponent<SafeAreaFit>().maxWidth=column?1080f:0f;
        Canvas.ForceUpdateCanvases();
        // Sahnede ses dinleyicisi yoksa (LevelSelect) hiçbir ses duyulmaz: kameraya ekle.
        if(FindFirstObjectByType<AudioListener>()==null)
        {var cam=Camera.main!=null?Camera.main.gameObject:gameObject;cam.AddComponent<AudioListener>();}
        MusicPlayer.Ensure();
        MisketrGameCenter.Authenticate();
        MisketrNotify.Refresh();
        controller=FindFirstObjectByType<LevelController>();
        if(controller!=null)controller.AnchorRefunded+=()=>Toast("Misketin işe yarar bir yerde kalmadı. Hakkın iade edildi.");
        district=MahalleProfile.NextLevelIn(CurrentMap())/Campaign.PerDistrict;
        // Harita/başlık sahnesine dönüldüyse öğretici modu kapanmış olmalı.
        if(controller==null){GameSession.TutorialMode=false;GameSession.DailyMode=false;GameSession.EndlessMode=false;}
        if(controller!=null)ShowGame();
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
    {
        var b=Button(parent,title,x,y,w,h,bg,action);
        var t=Text(b.transform,title,12,0,w-24,h,size,fg,TextAlignmentOptions.Center);
        // İngilizce metinler Türkçeden uzun olabiliyor: sığmıyorsa taşmak yerine küçülsün.
        t.enableAutoSizing=true;t.fontSizeMin=size*.62f;t.fontSizeMax=size;
        return b;
    }
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
    private void Header(string title,string subtitle)
    {
#if UNITY_EDITOR
        if(MahalleProfile.PreviewMode)subtitle="ÖNİZLEME · TÜM BÖLÜMLER AÇIK · KAYIT YAPILMAZ";
#endif
        Text(page,title,48,24,630,76,64,Ink);
        Text(page,subtitle,50,104,740,42,27,Muted);
        var wallet=Panel(page,"Boncuk",805,35,225,76,Ink);
        Art(wallet.transform,"Boncuk simgesi",MahalleGraphic.Shape.Marble,18,16,44,44,Gold);
        beadsLabel=Text(wallet.transform,MahalleProfile.Beads.ToString(),78,0,130,76,36,Cream);
    }
    private void Nav()
    {
        // Krem cubuk: ust haplarla ve OYNA dugmesiyle ayni dil.
        var bg=Panel(page,"Alt menu",24,0,1032,132,Krem);
        bg.colorB=new Color(.918f,.890f,.831f);bg.radius=34;bg.shadow=16;bg.highlight=false;
        Bottom(bg.rectTransform,24,18,1032,132);
        // Dorduncu sekme "MENÜ": acilis ekranina (baslik menusu) doner.
        string[] names={"MENÜ","HARİTA","KESEM","İSTATİSTİK","GÖREVLER"};   // "Mahalle" Memleket'te yanlış duruyordu
        string[] files={"Alt/SekmeGeri","Alt/SekmeHarita","Alt/SekmeKese","Alt/SekmeGrafik","Alt/SekmeListe"};
        int[] ids={-1,0,1,3,2};
        var shapes=new[]{MahalleGraphic.Shape.Chevron,MahalleGraphic.Shape.TabMap,MahalleGraphic.Shape.Bag,MahalleGraphic.Shape.Bars,MahalleGraphic.Shape.TabTask};
        var pasif=new Color(Komur.r,Komur.g,Komur.b,.70f);
        for(int i=0;i<5;i++)
        {
            int id=ids[i];bool on=id==tab;
            System.Action act=id<0?(System.Action)(()=>ShowTitle(true)):()=>{tab=id;ShowHome();};
            var b=Button(bg.transform,names[i],10+i*202,4,196,120,new Color(1,1,1,0),act);
            b.GetComponent<MahalleGraphic>().radius=26;
            b.gameObject.AddComponent<MahalleTap>();
            // Secili sekme: koyu yesil dolu hap, uzerinde acik ikon.
            if(on){var hap=Art(b.transform,"Seçim hapı",MahalleGraphic.Shape.Panel,44,0,108,76,Orman);hap.radius=28;}
            var renk=on?Krem:pasif;
            var cizim=IconOrShape(b.transform,files[i],shapes[i],68,9,60,58,renk);
            if(cizim!=null){cizim.accent=renk;if(id<0)cizim.mirror=true;}
            var yazi=Text(b.transform,Yumusat(names[i]),4,80,188,34,23,on?Orman:pasif,TextAlignmentOptions.Center);
            if(on)yazi.fontStyle=FontStyles.Bold;
        }
    }
    // ---------------------------------------------------------------
    // AÇILIŞ EKRANI
    // Mahallede biri yere tebeşirle çemberi çiziyor, misketler çembere
    // düşüyor, sonra menü açılıyor. Hiçbir görsel dosya kullanılmaz;
    // çember de misketler de MahalleGraphic ile çizilir.
    // ---------------------------------------------------------------
    // Ana menu karti: solda simge, sagda baslik ve (varsa) alt yazi.
    // Konum ekranin ALTINDAN olculur, boylece farkli telefon oranlarinda kaymaz.
    private Button MenuCard(Transform parent, string title, string sub, MahalleGraphic.Shape icon,
                            float x, float y, float w, float h, Color bg, Color fg, Action action,
                            string iconFile = null)
    {
        var b = Button(parent, title, 0, 0, w, h, bg, action);
        Bottom((RectTransform)b.transform, x, y, w, h);
        var g = (MahalleGraphic)b.targetGraphic;
        g.radius = 30; g.colorB = new Color(bg.r * .72f, bg.g * .72f, bg.b * .72f, bg.a); g.shadow = 10;

        float ik = h * .56f, ix = 26f;
        IconOrShape(b.transform, iconFile, icon, ix, (h - ik) * .5f, ik, ik, fg);

        float tx = ix + ik + 24f;
        var t = Text(b.transform, title, tx, sub == null ? 0 : h * .16f - 4f, w - tx - 22f,
                     sub == null ? h : h * .44f, 34, fg);
        t.fontStyle = FontStyles.Bold;
        t.enableAutoSizing = true; t.fontSizeMin = 22; t.fontSizeMax = 34;
        if (sub != null)
        {
            var st = Text(b.transform, sub, tx, h * .56f, w - tx - 22f, h * .3f, 22,
                          new Color(fg.r, fg.g, fg.b, .62f));
            st.enableAutoSizing = true; st.fontSizeMin = 16; st.fontSizeMax = 23;
        }
        return b;
    }

    // Ana menu arka plani. Assets/Resources/Mahalle/MenuBackground varsa
    // ekrani kaplayacak sekilde serilir; yoksa duz renk kalir ve oyun calisir.
    // Ekrani KAPLAR (kirpar), germez: farkli telefon oranlarinda doku bozulmaz.
    private void MenuBackdrop()
    {
        var tex = Resources.Load<Sprite>("Mahalle/MenuBackground");
        if (tex == null) return;

        // GUVENLI ALANIN DISINA TASIYORUZ. Arayuz centik ve alt cubugun icine
        // girmiyor ama zemin girmeli, yoksa ust ve altta siyah bant kaliyor.
        // Maske olmadigi icin cocuk nesne kendi kabindan tasabiliyor.
        const float TASMA = 340f;
        var r = Rect("Menü zemini", page); Stretch(r);
        r.offsetMin = new Vector2(0, -TASMA);
        r.offsetMax = new Vector2(0, TASMA);
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = tex; img.preserveAspect = false; img.raycastTarget = false;
        img.type = Image.Type.Simple;

        // Bantlari boyayan duz renk katmanini kapatiyoruz: artik fotograf kapliyor.
        SetBleed(Color.clear, Color.clear);

        // KARARTMA YOK. Ustte ve altta karartma denendi ve kaldirildi:
        // ortayi karartmayinca arada acik bir bant ve yatay bir sinir olusuyordu.
        // Fotografin kendi isigi yeterli, yazilar zaten koyu alanda duruyor.
    }

    // Menu simgesi: Assets/Resources/Mahalle/Icons/<ad>.png varsa o kullanilir,
    // yoksa kodla cizilen sekle dusulur. Boylece gorseller gelene kadar oyun calisir.
    // Gorsel varsa onu kullanir, yoksa kodla cizilen sekle duser.
    // Cizilen sekil dondurulur ki gerekirse aynalansin.
    private MahalleGraphic IconOrShape(Transform parent, string dosya, MahalleGraphic.Shape shape,
                             float x, float y, float w, float h, Color renk)
    {
        var sp = dosya == null ? null : Resources.Load<Sprite>("Mahalle/Icons/" + dosya);
        if (sp == null) return Art(parent, dosya ?? "simge", shape, x, y, w, h, renk);
        var r = Rect(dosya, parent); Place(r, x, y, w, h);
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = sp; img.color = renk; img.preserveAspect = true; img.raycastTarget = false;
        return null;
    }

    // "MENÜ" -> "Menü". Ceviri anahtari buyuk harfli kalir, ekranda yumusak gorunur.
    private static string Yumusat(string deger)
    {
        var metin = L.T(deger);
        if (string.IsNullOrEmpty(metin)) return metin;
        var kultur = new System.Globalization.CultureInfo("tr-TR");
        metin = metin.ToLower(kultur);
        return char.ToUpper(metin[0], kultur) + metin.Substring(1);
    }

    // Menüdeki basılamayan ONLİNE · Çok yakında düğmesi. Online gelince true (ya da düğmeyi aç).
    public static readonly bool ShowOnlineTeaser = false;
    // Gizlilik politikası (App Store kural 5.1.1: uygulama içinden de erişilebilir olmalı).
    public const string PrivacyUrl = "https://gulacti21.github.io/benim-oyun/gizlilik.html";

    private void ShowTitle() { ShowTitle(false); }

    private void ShowTitle(bool instant)
    {
        titleShown=true;onHome=false;
        ClearPage();
        Background(new Color(.14f, .12f, .10f));
        MenuBackdrop();

        // Tebesir sahnesi ayri bir kapta ve ekranin ORTASINA gore hizali.
        // Menu alta, baslik uste yaslandigi icin aradaki bosluk boylece her
        // telefon oraninda dengeli bolunuyor; uzun ekranda asagida kalmiyor.
        var sahne = Rect("Tebeşir sahnesi", page);
        sahne.anchorMin = sahne.anchorMax = new Vector2(.5f, .5f);
        sahne.pivot = new Vector2(.5f, .5f);
        sahne.sizeDelta = new Vector2(1080, 700);
        sahne.anchoredPosition = new Vector2(0, 154);
        // KISA EKRAN (iPad): başlık (alt kenarı ~540) ile alttaki menü yığını (650) arasına
        // 700'lük sahne sığmıyor, logo çembere, çember OYNA'ya biniyordu. Sığmazsa küçült ve
        // aradaki boşluğun ortasına koy. Telefonlarda boşluk yeterli, hiçbir şey değişmez.
        {
            Canvas.ForceUpdateCanvases();
            float h = page.rect.height, top = 545f, bottom = h - 670f, gap = bottom - top;
            if (h > 0 && gap < 700f)
            {
                sahne.localScale = Vector3.one * Mathf.Clamp(gap / 700f, .3f, 1f);
                sahne.anchoredPosition = new Vector2(0, h * .5f - (top + gap * .5f));
            }
        }

        var ring = Art(sahne, "Tebeşir çemberi", MahalleGraphic.Shape.ChalkRing, 190, 0, 700, 700, new Color(1, .98f, .92f, .92f));
        ring.stroke = 9f;
        ring.progress = 0f;

        // Tebeşirin ucu: çizerken çemberin üstünde gezer, bitince kaybolur.
        var tip = Art(sahne, "Tebeşir ucu", MahalleGraphic.Shape.Circle, 0, 0, 26, 26, new Color(1, 1, .96f, .9f));
        tip.radius = 13;

        // Çemberin içindeki misket üçgeni, o da tebeşirle.
        var tri = Art(sahne, "Tebeşir üçgeni", MahalleGraphic.Shape.ChalkTriangle, 320, 150, 440, 400, new Color(1, .98f, .92f, .78f));
        tri.stroke = 7f;
        tri.progress = 0f;

        // Üçgenin içine dizilen altı misket: 1-2-3 ıstaka.
        float[,] spots = { { 540, 252 }, { 499, 334 }, { 581, 334 }, { 458, 416 }, { 540, 416 }, { 622, 416 } };
        var marbles = new MahalleGraphic[spots.GetLength(0)];
        for (int i = 0; i < marbles.Length; i++)
        {
            var col = Campaign.SkinColors[i % 6];
            marbles[i] = Art(sahne, "Misket " + i, MahalleGraphic.Shape.Marble, spots[i, 0] - 37, spots[i, 1] - 37, 74, 74, col);
            marbles[i].accent = Color.Lerp(col, Color.white, .55f);
            marbles[i].transform.localScale = Vector3.zero;
        }

        // Başlık ve menü: çember kapanana kadar görünmezler.
        var head = Rect("Başlık", page); Stretch(head);
        var headFade = head.gameObject.AddComponent<CanvasGroup>(); headFade.alpha = 0f;
        var logo = Resources.Load<Sprite>("Mahalle/Logo");
        if (logo != null)
        {
            // Basliktaki tebesir yazisi bir yazi tipi degil, elle cizilmis bir
            // logo. O yuzden gorsel olarak konuyor; yoksa duz yaziya dusuyor.
            var lr = Rect("MİSKO logo", head); Place(lr, 110, 168, 860, 290);
            var li = lr.gameObject.AddComponent<Image>();
            li.sprite = logo; li.preserveAspect = true; li.raycastTarget = false;
        }
        else Text(head, "MİSKO", 0, 168, 1080, 150, 116, new Color(1, .98f, .92f), TextAlignmentOptions.Center);
        // Alt baslik logonun ALTINDA. Once ust uste biniyorlardi.
        var slogan = Text(head, "mahallenin en iyi nişancısı kim?", 0, 466, 1080, 60, 38,
                          new Color(1, .97f, .89f, .88f), TextAlignmentOptions.Center);
        slogan.characterSpacing = 3f;
        slogan.fontStyle = FontStyles.Bold | FontStyles.Italic;

        var menu = Rect("Menü", page); Stretch(menu);
        var menuFade = menu.gameObject.AddComponent<CanvasGroup>(); menuFade.alpha = 0f; menuFade.interactable = false; menuFade.blocksRaycasts = false;

        // DEVAM ET: oyuncunun son baktığı haritanın sıradaki bölümü.
        int next = MahalleProfile.NextLevelIn(CurrentMap());
        bool resume = next > 0 && MahalleProfile.Data.stars[0] > 0;

        // AYARLAR sol ustte, boncuk sag ustte. Ikisi de hap seklinde.
        var ayar = Button(menu, "Ayarlar", 40, 30, 300, 88, CardBg, Settings);
        { var g = (MahalleGraphic)ayar.targetGraphic; g.radius = 44; g.colorB = CardBgB; g.shadow = 8; }
        IconOrShape(ayar.transform, "Ayarlar", MahalleGraphic.Shape.Gear, 22, 15, 58, 58, Cream);
        Text(ayar.transform, "AYARLAR", 86, 0, 200, 88, 28, Cream);

        var wallet = Panel(menu, "Boncuk", 740, 30, 300, 88, CardBg);
        wallet.radius = 44; wallet.colorB = CardBgB; wallet.shadow = 8;
        Art(wallet.transform, "Boncuk simgesi", MahalleGraphic.Shape.Marble, 20, 16, 56, 56, Gold);
        beadsLabel = Text(wallet.transform, MahalleProfile.Beads.ToString(), 84, 0, 200, 88, 34, Cream);

        // Menu yigini ekranin altina yaslanir: uzun telefonlarda tasmaz,
        // kisa telefonlarda da alt guvenli alanin uzerinde kalir.
        const float MX = 60f, MW = 960f, HW = 465f, H = 130f;

        string playText = resume
            ? L.T("DEVAM ET") + " · " + L.Up(L.T(Maps.DistrictName(next / Campaign.PerDistrict))) + " " + (next % Campaign.PerDistrict + 1).ToString("00")
            : "OYNA";
        var oyna = Button(menu, playText, 0, 0, MW, 150, Gold, () => { tab = 0; district = next / Campaign.PerDistrict; ShowHome(); });
        Bottom((RectTransform)oyna.transform, MX, 500, MW, 150);
        { var g = (MahalleGraphic)oyna.targetGraphic; g.radius = 34; g.colorB = new Color(.82f, .53f, .16f); g.shadow = 14; }
        // Ok SADECE "OYNA" halinde. "DEVAM ET · ..." uzun oldugu icin okla
        // cakisiyordu; yaziyi kucultmek yerine oku kaldiriyoruz, yazi da
        // butonun tamamini kullaniyor.
        float yaziGenis = resume ? MW - 140 : MW - 70 - 140;
        var oynaYazi = Text(oyna.transform, playText, 70, 0, yaziGenis, 150, resume ? 44 : 62, new Color(.14f, .16f, .13f), TextAlignmentOptions.Center);
        oynaYazi.fontStyle = FontStyles.Bold;
        if (!resume)
            Art(oyna.transform, "Ok", MahalleGraphic.Shape.Arrow, MW - 96, 51, 48, 48, new Color(.14f, .16f, .13f));
        oynaRect = (RectTransform)oyna.transform;

        MenuCard(menu, "KESEM", null, MahalleGraphic.Shape.Bag, MX, 352, HW, H, CardBg, Cream, () => { tab = 1; ShowHome(); }, "Kesem");
        MenuCard(menu, "GÖREVLER", null, MahalleGraphic.Shape.TabTask, MX + 495, 352, HW, H, CardBg, Cream, () => { tab = 2; ShowHome(); }, "Gorevler");

        // GUNUN BOLUMU: alt yazi mevcut kilit durumunu gosterir.
        int seri = MahalleProfile.DailyStreakShown;
        bool dailyKilit = !DailyLevel.Available || MahalleProfile.Data.stars[0] == 0;
        string dailyAlt = !DailyLevel.Available ? L.T("ÇOK YAKINDA")
                        : MahalleProfile.Data.stars[0] == 0 ? L.T("1. bölümü bitirince açılır")
                        : MahalleProfile.DailyDoneToday ? L.F("BUGÜN TAMAM · SERİ {0} GÜN", seri)
                        : MahalleProfile.DailyTriesLeft == 0 ? L.T("HAKLARIN BİTTİ · YARIN YENİ BÖLÜM")
                        : L.F("{0} HAK · +{1} BONCUK", MahalleProfile.DailyTriesLeft, MahalleProfile.DailyNextReward);
        MenuCard(menu, "GÜNÜN BÖLÜMÜ", dailyAlt,
                 dailyKilit ? MahalleGraphic.Shape.Lock : MahalleGraphic.Shape.Calendar,
                 MX, 208, MW, H, new Color(.42f, .22f, .15f), new Color(1, .93f, .84f), StartDaily,
                 dailyKilit ? "Kilit" : "Gunluk");

        string sonsuzAlt = MahalleProfile.Data.endlessBest > 0
                         ? L.F("REKOR {0}", MahalleProfile.Data.endlessBest) : L.T("Süre yarışı");
        MenuCard(menu, "SONSUZ", sonsuzAlt, MahalleGraphic.Shape.Clock, MX, 64, ShowOnlineTeaser ? HW : MW, H, CardBg, Cream, StartEndless, "Sonsuz");

        // ONLINE: bu surumde yok. Basilamayan "ÇOK YAKINDA" dugmesi App Store'da
        // "tamamlanmamis ozellik" (kural 2.1) diye ret sebebi olabiliyor; yayinda gizli.
        if (ShowOnlineTeaser)
        {
            var online = MenuCard(menu, "ONLİNE", L.T("Çok yakında"), MahalleGraphic.Shape.People,
                                  MX + 495, 64, HW, H, new Color(.18f, .19f, .18f), new Color(1, .97f, .89f, .42f), () => { }, "Online");
            online.interactable = false;
        }

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
        if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlayChalk(false);
        for (float t = 0f; t < draw; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / draw);
            ring.SetProgress(k);
            float a = Mathf.PI * .5f - k * Mathf.PI * 2f;
            tipRect.anchoredPosition = centre + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry) - new Vector2(13, -13);
            yield return null;
        }
        ring.SetProgress(1f);

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
        if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlayChalk(true);
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

        yield return new WaitForSecondsRealtime(.12f);

        // 3) Misketler üçgene diziliyor, biri diğerinin ardından.
        for (int i = 0; i < marbles.Length; i++)
        {
            StartCoroutine(PopMarble(marbles[i].transform));
            if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlayMarbleTick(i);
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
    private void ShowHome()
    {
        onHome=true;
        ClearPage();
        if(tab==0){Background(new Color(.74f,.67f,.55f));MapScreen();}
        else
        {
            Background(Paper);
            Header(tab==1?"Kesem":tab==3?"İstatistik":"Görevler",tab==1?"HER MİSKETİN BİR HİKÂYESİ VAR":tab==3?"MAHALLEDEKİ İZİN":"KÜÇÜK HEDEFLER, YENİ BONCUKLAR");
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
    // Mahalle haritasi. Zemin, yol ve duraklar MahalleMapView icinde cizilir.
    // HARİTALAR: oyuncunun baktığı harita. Kilitliyse Mahalle'ye düşer.
    private static int CurrentMap()
    {
        int m=MahalleProfile.Data.lastMap;
        return MahalleProfile.MapUnlocked(m)?m:0;
    }
    private static void RememberMap(int map)
    {
        if(MahalleProfile.Data.lastMap==map)return;
        MahalleProfile.Data.lastMap=map;MahalleProfile.Save();
    }
    // Oklar haritanın kendi 5 bölgesi içinde döner, başka haritaya geçmez.
    private int CycleDistrict(int step)
    {
        int first=Maps.FirstDistrict(Maps.MapOfDistrict(district));
        return first+((district-first+step)%Maps.DistrictsPerMap+Maps.DistrictsPerMap)%Maps.DistrictsPerMap;
    }

    private void MapScreen()
    {
        var theme=MahalleTheme.Get(district);

        var viewport=Rect("Harita",page);
        Stretch(viewport);
        // Harita alt guvenli alanin (home indicator seridi) altina kadar uzar,
        // boylece ekranin dibinde siyah bant kalmaz. Dugmeler guvenli alanda kalir.
        viewport.offsetMin=new Vector2(0,-AltGuvenliPay());
        // Harita ust guvenli alanin (centik / durum cubugu seridi) icine de uzanir,
        // boylece basligin ustunde bos krem bant kalmaz. Baslik yerinde durur.
        viewport.offsetMax=new Vector2(0,UstGuvenliPay());
        viewport.gameObject.AddComponent<RectMask2D>();
        var catcher=viewport.gameObject.AddComponent<Image>();catcher.color=Color.clear;catcher.raycastTarget=true;

        var content=Rect("Harita icerigi",viewport);
        content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);
        content.sizeDelta=new Vector2(0,MahalleMapView.ContentHeight);content.anchoredPosition=Vector2.zero;

        var scroll=viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;
        // Clamped: icerik bitince yaylanma yok. Elastic'te fazla cekilince
        // haritanin arkasindaki koyu zemin gorunuyordu.
        scroll.movementType=ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity=45;scroll.decelerationRate=.12f;

        // Acilista secili bolum: oyuncunun siradaki bolumu, bu mahallede degilse mahallenin ilki.
        int first=district*Campaign.PerDistrict;
        int next=MahalleProfile.NextLevelIn(Maps.MapOfDistrict(district));
        selectedLevel=(next>=first&&next<first+Campaign.PerDistrict)?next:first;

        MahalleMapView.Build(content,district,font,SelectLevel,OpenLevel,Toast);
        MahalleMapView.Select(selectedLevel-first);

        // Ekran secili bolume bakarak acilir.
        Canvas.ForceUpdateCanvases();
        float visible=viewport.rect.height;
        float focus=MahalleMapView.StopOffset(selectedLevel-first)-visible*.5f;
        content.anchoredPosition=new Vector2(0,Mathf.Clamp(focus,0,Mathf.Max(0,MahalleMapView.ContentHeight-visible)));

        MapHeader(theme);
        MapDock();
        AnnounceMaps();
    }

    // Ust guvenli alan payi (centik / durum cubugu seridi).
    private float UstGuvenliPay()
    {
        if(root==null||Screen.height<=0) return 0f;
        var safe=Screen.safeArea;
        if(safe.height<=0) return 0f;
        float ust=Screen.height-safe.yMax;
        if(ust<=0) return 0f;
        float tamYukseklik=root.rect.height*Screen.height/safe.height;
        return ust/Screen.height*tamYukseklik;
    }

    // Alt guvenli alan payi, canvas birimine cevrilmis hali.
    // Cihazda home indicator yoksa 0 doner ve hicbir sey degismez.
    private float AltGuvenliPay()
    {
        if(root==null||Screen.height<=0) return 0f;
        var safe=Screen.safeArea;
        if(safe.height<=0||safe.y<=0) return 0f;
        float tamYukseklik=root.rect.height*Screen.height/safe.height;
        return safe.y/Screen.height*tamYukseklik;
    }

    // Duraga dokununca bolum secilir; baslatma alttaki tek dugmededir.
    private void SelectLevel(int index)
    {
        selectedLevel=index;
        if(playLabel!=null) playLabel.text=PlayCaption(index);
    }

    private string PlayCaption(int index)
    {
        var lv=Maps.Get(index);
        return L.T("OYNA")+"  \u00b7  "+(index%Campaign.PerDistrict+1).ToString("00")+"  "+L.T(lv.levelName);
    }

    private void MapHeader(MahalleTheme theme)
    {
        SetBleed(Color.clear,new Color(.078f,.157f,.133f));
        // Duz krem blok yok: baslik ogeleri haritanin uzerinde yuzen haplar.
        // Seffaf katman yalnizca dokunmalari yakalar, haritaya kazara basilmasin.
        var head=Panel(page,"Baslik",0,0,1080,300,new Color(0,0,0,0));
        head.radius=0;head.shadow=0;head.raycastTarget=true;

        // Sol ustte kusandigin misket durur; dokununca koleksiyona gider.
        int skin=Mathf.Clamp(MahalleProfile.EffectiveSkin,0,Campaign.SkinNames.Length-1);
        var mine=Button(head.transform,"Kusandigin misket",48,26,326,78,Orman,()=>{tab=1;ShowHome();});
        var mineFace=mine.GetComponent<MahalleGraphic>();
        mineFace.colorB=new Color(.086f,.172f,.145f);mineFace.radius=39;mineFace.shadow=8;mineFace.highlight=false;
        mine.gameObject.AddComponent<MahalleTap>();
        var art=Art(mine.transform,"Misket",MahalleGraphic.Shape.Marble,16,17,44,44,Campaign.SkinColors[skin]);
        art.accent=SpecialMarbles.Accent(skin);
        Text(mine.transform,Campaign.SkinNames[skin],76,0,238,78,28,Krem);

        // Ust siranin ortasinda mahallenin yildiz ilerlemesi.
        int stars=0;
        for(int i=district*Campaign.PerDistrict;i<(district+1)*Campaign.PerDistrict;i++)stars+=MahalleProfile.Stars(i);
        int full=Campaign.PerDistrict*3;
        var meter=Panel(head.transform,"Yildiz ilerlemesi",390,26,396,78,new Color(.906f,.878f,.816f));
        meter.colorB=new Color(.863f,.831f,.757f);meter.radius=39;meter.highlight=false;
        Art(meter.transform,"Yildiz",MahalleGraphic.Shape.Star,20,19,40,40,Amber);
        Text(meter.transform,stars+" / "+full,68,19,116,40,30,Komur);
        var track=Panel(meter.transform,"Yol",190,33,186,12,new Color(Komur.r,Komur.g,Komur.b,.18f));track.radius=6;
        var fill=Panel(meter.transform,"Dolu",190,33,Mathf.Max(12f,186f*stars/full),12,Amber);
        fill.colorB=new Color(.882f,.576f,.176f);fill.radius=6;

        var wallet=Panel(head.transform,"Boncuk",802,26,230,78,Orman);
        wallet.colorB=new Color(.086f,.172f,.145f);wallet.radius=39;wallet.shadow=8;wallet.highlight=false;
        Art(wallet.transform,"Boncuk simgesi",MahalleGraphic.Shape.Marble,16,17,44,44,Amber);
        beadsLabel=Text(wallet.transform,MahalleProfile.Beads.ToString(),76,0,140,78,36,Krem);

        var prev=Button(head.transform,"Onceki mahalle",48,144,96,96,new Color(.906f,.878f,.816f),()=>{district=CycleDistrict(-1);ShowHome();});
        prev.GetComponent<MahalleGraphic>().radius=30;prev.gameObject.AddComponent<MahalleTap>();
        Art(prev.transform,"Sol",MahalleGraphic.Shape.Chevron,26,26,44,44,Komur).mirror=true;

        var next=Button(head.transform,"Sonraki mahalle",936,144,96,96,new Color(.906f,.878f,.816f),()=>{district=CycleDistrict(1);ShowHome();});
        next.GetComponent<MahalleGraphic>().radius=30;next.gameObject.AddComponent<MahalleTap>();
        Art(next.transform,"Sag",MahalleGraphic.Shape.Chevron,26,26,44,44,Komur);

        // Mahalle adi kendi krem kapsulunde: her zeminde okunur, blok gibi durmaz.
        // HARİTALAR: kapsüle dokunmak harita seçimini açar. Solda haritanın adı, sağda ok.
        var isimBtn=Button(head.transform,"Mahalle adı",164,120,752,160,Krem,OpenMaps);
        var isim=isimBtn.GetComponent<MahalleGraphic>();
        isim.colorB=new Color(.933f,.906f,.851f);isim.radius=46;isim.shadow=10;isim.highlight=false;
        isimBtn.gameObject.AddComponent<MahalleTap>();
        int shownMap=Maps.MapOfDistrict(district);
        var etiket=Panel(isim.transform,"Harita etiketi",22,56,150,48,Amber);etiket.radius=24;etiket.highlight=false;
        var etiketYazi=Text(etiket.transform,L.Up(L.T(Maps.Names[shownMap])),0,0,150,48,22,Komur,TextAlignmentOptions.Center);
        etiketYazi.fontStyle=FontStyles.Bold;etiketYazi.enableAutoSizing=true;etiketYazi.fontSizeMin=14;etiketYazi.fontSizeMax=22;
        Art(isim.transform,"Harita oku",MahalleGraphic.Shape.Chevron,690,60,40,40,new Color(Komur.r,Komur.g,Komur.b,.55f));
        var baslik=Text(isim.transform,theme.title,0,18,752,64,48,Komur,TextAlignmentOptions.Center);
        baslik.fontStyle=FontStyles.Bold;
        Text(isim.transform,theme.subtitle,0,82,752,44,26,new Color(Komur.r,Komur.g,Komur.b,.62f),TextAlignmentOptions.Center);

        for(int i=0;i<5;i++)
        {
            bool on=i==district%Maps.DistrictsPerMap;
            Art(isim.transform,"Nokta "+i,MahalleGraphic.Shape.Circle,319+i*26,on?126:128,on?14:10,on?14:10,
                on?Amber:new Color(Komur.r,Komur.g,Komur.b,.24f));
        }
    }

    // HARİTALAR: her harita bir kart. Açık olana dokunmak o haritaya geçer, oyuncu
    // o haritanın sıradaki bölümünde açılır. Kilitli kartta kilit ve ne yapılacağı yazar.
    private void OpenMaps()
    {
        const float CardH=330f,Gap=28f;
        float h=210+Maps.Count*(CardH+Gap)+140;
        var box=Modal("Haritalar",h);
        var face=box.GetComponent<MahalleGraphic>();if(face!=null){face.color=Krem;face.colorB=Krem;face.highlight=false;}
        var baslik=Text(box,"HARİTALAR",36,40,912,80,52,Komur,TextAlignmentOptions.Center);baslik.fontStyle=FontStyles.Bold;
        Text(box,"Her haritanın kendi 60 bölümü var. Dokun, geç.",36,118,912,60,26,new Color(Komur.r,Komur.g,Komur.b,.62f),TextAlignmentOptions.Center);
        int current=Maps.MapOfDistrict(district);
        for(int m=0;m<Maps.Count;m++)
        {
            int map=m;float y=210+m*(CardH+Gap);
            bool open=MahalleProfile.MapUnlocked(map);
            var card=Button(box,"Harita "+map,36,y,912,CardH,open?Orman:new Color(Komur.r,Komur.g,Komur.b,.82f),()=>
            {
                if(!MahalleProfile.MapUnlocked(map)){Toast(L.F("{0} haritası, {1} haritasının son bölümünü geçince açılır.",L.T(Maps.Names[map]),L.T(Maps.Names[Mathf.Max(0,map-1)])));return;}
                RememberMap(map);district=MahalleProfile.NextLevelIn(map)/Campaign.PerDistrict;tab=0;CloseModal(false);ShowHome();
            });
            var cf=card.GetComponent<MahalleGraphic>();cf.radius=36;cf.shadow=10;cf.highlight=false;
            // Seçili harita kartı tamamen turuncu görünür (kullanıcı bu hali istedi, 2026-09-25).
            if(map==current){var sec=Art(card.transform,"Seçili çerçeve",MahalleGraphic.Shape.Panel,-6,-6,924,CardH+12,Amber);sec.radius=42;sec.transform.SetAsFirstSibling();}
            card.gameObject.AddComponent<MahalleTap>();
            var ad=Text(card.transform,L.Up(L.T(Maps.Names[map])),40,34,560,70,50,Krem);ad.fontStyle=FontStyles.Bold;
            Text(card.transform,Maps.Subtitles[map],40,104,820,50,28,new Color(Krem.r,Krem.g,Krem.b,.72f));
            if(open)
            {
                int stars=MahalleProfile.MapStars(map),full=Maps.PerMap*3;
                Art(card.transform,"Yıldız",MahalleGraphic.Shape.Star,40,190,44,44,Amber);
                Text(card.transform,L.F("{0} / {1} yıldız",stars,full),96,188,400,48,30,Krem);
                var track=Panel(card.transform,"Yol",40,262,832,14,new Color(Krem.r,Krem.g,Krem.b,.18f));track.radius=7;track.highlight=false;
                var fill=Panel(card.transform,"Dolu",40,262,Mathf.Max(14f,832f*stars/full),14,Amber);fill.radius=7;fill.highlight=false;
                if(map==current)
                {
                    var burada=Panel(card.transform,"Buradasın",672,34,200,52,Amber);burada.radius=26;burada.highlight=false;
                    var t=Text(burada.transform,"BURADASIN",0,0,200,52,22,Komur,TextAlignmentOptions.Center);t.fontStyle=FontStyles.Bold;
                }
            }
            else
            {
                Art(card.transform,"Kilit",MahalleGraphic.Shape.Lock,40,186,52,52,new Color(Krem.r,Krem.g,Krem.b,.8f));
                Text(card.transform,L.F("{0} haritasını bitir, bu harita açılsın.",L.T(Maps.Names[Mathf.Max(0,map-1)])),108,178,760,70,28,new Color(Krem.r,Krem.g,Krem.b,.85f));
            }
        }
        LabelButton(box,"GERİ",36,h-120,912,88,Komur,Krem,()=>CloseModal(false),29);
    }

    // Harita yeni açıldıysa bir kez haber ver (kayıtta hangi haritaya kadar söylendiği durur).
    private void AnnounceMaps()
    {
        for(int m=MahalleProfile.Data.mapsAnnounced+1;m<Maps.Count;m++)
        {
            if(!MahalleProfile.MapUnlocked(m))break;
            MahalleProfile.Data.mapsAnnounced=m;MahalleProfile.Save();
            // Test yapısı her şeyi açık gösterir; orada duyuru gürültü olur.
            if(!MahalleProfile.TestUnlockAllLevels)PlayMapIntro(m);
        }
        if(pendingMapIntro>0){int m=pendingMapIntro;pendingMapIntro=-1;PlayMapIntro(m);}
    }

    // HARİTA GEÇİŞİ: yeni harita açılınca bir kez oynar (~5 sn), kodla çizilir.
    // Tebeşir çemberinden çıkan misket yeni haritanın çemberine yuvarlanır, harita adı
    // gelir, misket yağar. Test yapısında her şey açık olduğu için kayıttan tetiklenmez;
    // orada haritanın son bölümü geçilince oynar (pendingMapIntro) ki telefonda denensin.
    private static int pendingMapIntro=-1;
    private bool introPlaying;
    private void PlayMapIntro(int map){if(introPlaying||map<1||map>=Maps.Count)return;StartCoroutine(MapIntro(map));}
    private static RectTransform Centre(RectTransform r,float x,float y,float w,float h)
    {r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;}
    private IEnumerator MapIntro(int map)
    {
        introPlaying=true;
        // Canvas'ın kendisine: çentik ve alt şerit dahil bütün ekranı örter.
        var layer=Rect("Harita geçişi",transform);Stretch(layer);layer.SetAsLastSibling();
        var group=layer.gameObject.AddComponent<CanvasGroup>();group.alpha=0;
        var bg=Panel(layer,"Perde",0,0,1080,2600,Orman,true);bg.radius=0;bg.colorB=new Color(.078f,.157f,.133f);Stretch(bg.rectTransform);
        // Arka plan: önceki haritanın son bölgesi (üst ucu), yeni çember çizilirken yeni
        // haritanın ilk bölgesine (alt ucu, başlangıç) geçer. İkisi de yavaşça kayar.
        var oldPhoto=Photo(layer,MahalleMapView.MapPhoto(Maps.FirstDistrict(map)-1));
        // Özel geçiş görseli varsa o (Resources/Mahalle/Gecis/<Harita>.png), yoksa ilk bölgenin haritası.
        var custom=Resources.Load<Texture2D>("Mahalle/Gecis/"+Maps.Names[map]);
        var newPhoto=Photo(layer,custom!=null?custom:MahalleMapView.MapPhoto(Maps.FirstDistrict(map)));
        if(newPhoto!=null)newPhoto.color=new Color(1,1,1,0);
        // Özel görsel aydınlık ve yazı için boşluklu çizildi: tül daha açık, alt kısım (yazılar) koyu.
        var tint=Panel(layer,"Karartma",0,0,1080,2600,new Color(Orman.r,Orman.g,Orman.b,custom!=null?.30f:.55f));
        tint.colorB=new Color(.078f,.157f,.133f,custom!=null?.78f:.82f);tint.radius=0;Stretch(tint.rectTransform);
        var rain=Rect("Misket yağmuru",layer);Stretch(rain);
        // Düzen 1080x1900'lük bir kutuda; kısa ekranda (iPad) kutu küçülür, taşmaz.
        var box=Centre(Rect("Geçiş içeriği",layer),0,0,1080,1900);
        Canvas.ForceUpdateCanvases();
        StartCoroutine(PanPhotos(layer,oldPhoto,newPhoto));
        box.localScale=Vector3.one*Mathf.Min(1f,layer.rect.height/1900f);
        yield return Fade(group,.35f);

        // 1) Önceki harita tamam: başlık ve üç yıldız.
        var done=Text(box,L.F("{0} TAMAMLANDI",L.Up(L.T(Maps.Names[map-1]))),0,0,1000,80,46,Krem,TextAlignmentOptions.Center);
        Centre(done.rectTransform,0,720,1000,80);done.fontStyle=FontStyles.Bold;
        for(int i=0;i<3;i++)
        {
            var star=Art(box,"Yıldız "+i,MahalleGraphic.Shape.Star,0,0,0,0,Amber);
            Centre(star.rectTransform,(i-1)*110f,i==1?610:595,i==1?86:70,i==1?86:70);
            star.transform.localScale=Vector3.zero;StartCoroutine(PopMarble(star.transform));
            if(SfxPlayer.Instance!=null)SfxPlayer.Instance.PlayMarbleTick(i);
            yield return new WaitForSecondsRealtime(.12f);
        }

        // 2) Eski haritanın çemberi, içinde oyuncunun misketi.
        Vector2 a=new Vector2(-250,370),b=new Vector2(0,-40),ctrl=new Vector2(360,300);
        var chalk=new Color(Krem.r,Krem.g,Krem.b,.85f);
        var ringA=Art(box,"Eski çember",MahalleGraphic.Shape.ChalkRing,0,0,0,0,chalk);Centre(ringA.rectTransform,a.x,a.y,240,240);ringA.SetProgress(0f);
        int skin=Mathf.Clamp(MahalleProfile.EffectiveSkin,0,Campaign.SkinColors.Length-1);
        var marble=Art(box,"Misket",MahalleGraphic.Shape.Marble,0,0,0,0,Campaign.SkinColors[skin]);
        marble.accent=SpecialMarbles.Accent(skin);Centre(marble.rectTransform,a.x,a.y,70,70);marble.transform.localScale=Vector3.zero;
        if(SfxPlayer.Instance!=null)SfxPlayer.Instance.PlayChalk(false);
        for(float t=0;t<.5f;t+=Time.unscaledDeltaTime){ringA.SetProgress(Mathf.SmoothStep(0,1,t/.5f));yield return null;}
        ringA.SetProgress(1f);
        yield return PopMarble(marble.transform);
        yield return new WaitForSecondsRealtime(.2f);

        // 3) Misket yola çıkar, arkasında tebeşir noktaları bırakır.
        const float roll=1.5f;float lastDot=-1f;int dots=0;
        for(float t=0;t<roll;t+=Time.unscaledDeltaTime)
        {
            float k=Mathf.SmoothStep(0,1,t/roll),u=1-k;
            Vector2 p=u*u*a+2*u*k*ctrl+k*k*b;
            marble.rectTransform.anchoredPosition=p;
            if(t-lastDot>.055f)
            {
                lastDot=t;
                var dot=Art(box,"İz",MahalleGraphic.Shape.Circle,0,0,0,0,new Color(Krem.r,Krem.g,Krem.b,.5f));
                Centre(dot.rectTransform,p.x,p.y,14,14);dot.transform.SetSiblingIndex(marble.transform.GetSiblingIndex());
                if(++dots%5==0&&SfxPlayer.Instance!=null)SfxPlayer.Instance.PlayMarbleTick(dots/5%6);
            }
            yield return null;
        }
        marble.rectTransform.anchoredPosition=b;

        // 4) Yeni haritanın çemberi çizilir.
        var ringB=Art(box,"Yeni çember",MahalleGraphic.Shape.ChalkRing,0,0,0,0,Amber);Centre(ringB.rectTransform,b.x,b.y,380,380);ringB.SetProgress(0f);
        ringB.transform.SetSiblingIndex(marble.transform.GetSiblingIndex());
        if(SfxPlayer.Instance!=null)SfxPlayer.Instance.PlayChalk(false);
        for(float t=0;t<.8f;t+=Time.unscaledDeltaTime)
        {
            float k=Mathf.SmoothStep(0,1,t/.8f);ringB.SetProgress(k);
            if(newPhoto!=null)newPhoto.color=new Color(1,1,1,k);
            yield return null;
        }
        if(newPhoto!=null)newPhoto.color=Color.white;
        ringB.SetProgress(1f);
        StartCoroutine(PopMarble(marble.transform));

        // 5) Harita adı + misket yağmuru.
        var pill=Panel(box,"Yeni harita",0,0,0,0,Amber);Centre(pill.rectTransform,0,-330,460,62);pill.radius=31;pill.highlight=false;
        var pillText=Text(pill.transform,"YENİ HARİTA AÇILDI",0,0,460,62,26,Komur,TextAlignmentOptions.Center);pillText.fontStyle=FontStyles.Bold;
        var title=Text(box,L.Up(L.T(Maps.Names[map])),0,0,1040,150,118,Amber,TextAlignmentOptions.Center);
        Centre(title.rectTransform,0,-440,1040,150);title.fontStyle=FontStyles.Bold;
        title.enableAutoSizing=true;title.fontSizeMin=60;title.fontSizeMax=118;
        var sub=Text(box,Maps.Subtitles[map],0,0,960,60,32,new Color(Krem.r,Krem.g,Krem.b,.8f),TextAlignmentOptions.Center);
        Centre(sub.rectTransform,0,-535,960,60);
        pill.transform.localScale=title.transform.localScale=sub.transform.localScale=Vector3.zero;
        if(SfxPlayer.Instance!=null)SfxPlayer.Instance.PlayWin();
        StartCoroutine(MarbleRain(rain,layer.rect.height));
        StartCoroutine(PopMarble(pill.transform));
        yield return new WaitForSecondsRealtime(.12f);
        StartCoroutine(PopMarble(title.transform));
        yield return new WaitForSecondsRealtime(.18f);
        yield return PopMarble(sub.transform);
        yield return new WaitForSecondsRealtime(.4f);

        // 6) Düğmeler.
        var buttons=Rect("Düğmeler",box);Stretch(buttons);
        var bgroup=buttons.gameObject.AddComponent<CanvasGroup>();bgroup.alpha=0;
        System.Action close=()=>{if(layer!=null)Destroy(layer.gameObject);introPlaying=false;};
        var go=LabelButton(buttons,"HADİ GİDELİM",0,0,0,0,Amber,Komur,()=>
        {close();RememberMap(map);district=MahalleProfile.NextLevelIn(map)/Campaign.PerDistrict;tab=0;ShowHome();},40);
        Centre((RectTransform)go.transform,0,-700,760,120);
        var goFace=go.GetComponent<MahalleGraphic>();goFace.radius=60;goFace.shadow=10;goFace.highlight=false;
        foreach(var t in go.GetComponentsInChildren<TextMeshProUGUI>()){t.fontStyle=FontStyles.Bold;t.rectTransform.sizeDelta=new Vector2(736,120);}
        var later=LabelButton(buttons,"SONRA",0,0,0,0,new Color(0,0,0,0),new Color(Krem.r,Krem.g,Krem.b,.7f),()=>close(),30);
        Centre((RectTransform)later.transform,0,-830,400,80);
        foreach(var t in later.GetComponentsInChildren<TextMeshProUGUI>())t.rectTransform.sizeDelta=new Vector2(376,80);
        yield return Fade(bgroup,.3f);
    }
    // Tam ekran harita görseli (ekranı dolduracak kadar kırpılır). Görsel yoksa null.
    private RawImage Photo(RectTransform parent,Texture2D tex)
    {
        if(tex==null)return null;
        var r=Rect("Harita görseli",parent);Stretch(r);
        var img=r.gameObject.AddComponent<RawImage>();img.texture=tex;img.raycastTarget=false;
        return img;
    }
    // Eski harita üst uçtan aşağı, yeni harita alt uçta yukarı doğru hafifçe kayar.
    private IEnumerator PanPhotos(RectTransform layer,RawImage oldPhoto,RawImage newPhoto)
    {
        for(float t=0;layer!=null;t+=Time.unscaledDeltaTime)
        {
            float k=Mathf.Clamp01(t/7f);
            if(oldPhoto!=null)Crop(oldPhoto,layer,1f-.06f*k);
            if(newPhoto!=null)Crop(newPhoto,layer,.06f*k);
            yield return null;
        }
    }
    // Görseli en-boy oranını bozmadan ekrana doldurur; along 0 = alt uç, 1 = üst uç.
    private static void Crop(RawImage img,RectTransform layer,float along)
    {
        var tex=img.texture;float lw=layer.rect.width,lh=layer.rect.height;
        if(tex==null||lw<=0||lh<=0)return;
        float h=Mathf.Clamp01((float)tex.width/tex.height*lh/lw),w=1f;
        if(h>=1f){h=1f;w=Mathf.Clamp01((float)tex.height/tex.width*lw/lh);}
        img.uvRect=new Rect((1f-w)*.5f,(1f-h)*Mathf.Clamp01(along),w,h);
    }
    // Yukarıdan düşen renkli misketler; ekranın altından çıkınca silinir.
    private IEnumerator MarbleRain(RectTransform parent,float height)
    {
        const int count=30;
        var items=new RectTransform[count];var speed=new float[count];
        for(int i=0;i<count;i++)
        {
            float size=UnityEngine.Random.Range(34f,62f);
            var m=Art(parent,"Yağan misket",MahalleGraphic.Shape.Marble,0,0,0,0,Campaign.SkinColors[UnityEngine.Random.Range(0,Campaign.SkinColors.Length)]);
            items[i]=Centre(m.rectTransform,UnityEngine.Random.Range(-500f,500f),height*.5f+60+UnityEngine.Random.Range(0f,900f),size,size);
            speed[i]=UnityEngine.Random.Range(700f,1300f);
        }
        for(float t=0;t<4f&&parent!=null;t+=Time.unscaledDeltaTime)
        {
            for(int i=0;i<count;i++)
                if(items[i]!=null){speed[i]+=900f*Time.unscaledDeltaTime;items[i].anchoredPosition-=new Vector2(0,speed[i]*Time.unscaledDeltaTime);}
            yield return null;
        }
        if(parent!=null)foreach(var r in items)if(r!=null)Destroy(r.gameObject);
    }

    // Harita altinda tek ana eylem dugmesi. Siyah geciste yok; alt cubuk Nav() cizer.
    private void MapDock()
    {
        int first=district*Campaign.PerDistrict;
        if(selectedLevel<first||selectedLevel>=first+Campaign.PerDistrict)selectedLevel=first;

        var play=Button(page,"Oyna",48,0,984,124,Amber,()=>OpenLevel(selectedLevel));
        var face=play.GetComponent<MahalleGraphic>();
        // highlight kapali: ust kenara cizilen beyaz serit duz tasarimda cizgi gibi duruyordu.
        face.colorB=new Color(.902f,.604f,.180f);face.radius=34;face.shadow=14;face.highlight=false;
        Bottom((RectTransform)play.transform,48,172,984,124);
        play.gameObject.AddComponent<MahalleTap>();
        playLabel=Text(play.transform,PlayCaption(selectedLevel),46,0,800,124,40,Komur);
        playLabel.fontStyle=FontStyles.Bold;
        Art(play.transform,"Ok",MahalleGraphic.Shape.Chevron,884,38,48,48,Komur);
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
        var content=HomeScroll(MisketrCloud.Enabled?2300:2180);
        Text(content,"Karnen",48,190,984,66,47,Ink);
        Text(content,"Bitirdiğin her bölüm buraya yazılır.",48,268,984,70,30,Muted);
        int fav=MahalleProfile.FavoriteDistrict();
        string[] labels={"TOPLAM ÇIKARDIĞIN MİSKET","TEK ATIŞTA REKORUN","EN ÇOK OYNADIĞIN MAHALLE"};
        string[] values={
            MahalleProfile.Data.knocked.ToString(),
            MahalleProfile.Data.bestShot>0?L.F("{0} MİSKET",MahalleProfile.Data.bestShot):"HENÜZ YOK",
            fav>=0?L.T(Maps.DistrictName(fav)):"HENÜZ YOK"};
        string[] notes={
            "Bitirdiğin bölümlerde çemberden çıkardıkların",
            "Tek bir atışla aynı anda çıkardığın en çok misket",
            fav>=0?L.F("{0} bölüm bitirdin",MahalleProfile.DistrictPlays(fav)):"Bir bölüm bitirince burada görünür"};
        for(int i=0;i<3;i++)
        {
            var card=Panel(content,labels[i],48,370+i*250,984,220,Cream);
            Art(card.transform,"Simge",i==0?MahalleGraphic.Shape.Marble:i==1?MahalleGraphic.Shape.Star:MahalleGraphic.Shape.House,32,62,96,96,Gold);
            Text(card.transform,labels[i],160,22,790,40,25,Muted);
            Text(card.transform,values[i],160,64,790,82,56,Ink);
            Text(card.transform,notes[i],160,150,790,44,25,Muted);
        }

        // USTA SEVİYESİ: yıldız, çıkardığın misket ve günlük seriler puana dönüşür.
        var seviye=Panel(content,"Usta seviyesi",48,1140,984,190,Ink);seviye.radius=28;seviye.highlight=false;
        Text(seviye.transform,L.F("USTA SEVİYESİ {0}",MahalleProfile.MasteryLevel),36,24,700,46,30,Gold);
        Text(seviye.transform,L.F("{0} / {1} puan",MahalleProfile.MasteryIntoLevel,MahalleProfile.MasteryPerLevel),36,74,700,44,26,new Color(1,.97f,.89f,.7f));
        Panel(seviye.transform,"Yol",36,134,912,16,new Color(1,1,1,.14f)).radius=8;
        var dolu=Panel(seviye.transform,"Dolu",36,134,Mathf.Max(16f,912f*MahalleProfile.MasteryIntoLevel/MahalleProfile.MasteryPerLevel),16,Gold);
        dolu.radius=8;dolu.colorB=new Color(.85f,.53f,.15f);
        Art(seviye.transform,"Rozet",MahalleGraphic.Shape.Star,846,28,90,90,new Color(1,.84f,.42f,.9f));

        // BAŞARIMLAR
        Text(content,"BAŞARIMLAR",48,1372,984,48,28,Muted);
        for(int i=0;i<MahalleProfile.AchievementNames.Length;i++)
        {
            int hedef=MahalleProfile.AchievementTargets[i];
            int ilerleme=Mathf.Min(MahalleProfile.AchievementProgress(i),hedef);
            bool tamam=ilerleme>=hedef;
            var kart=Panel(content,"Başarım "+i,48,1430+i*118,984,102,tamam?new Color(.93f,.88f,.74f):Cream);
            kart.radius=22;
            Art(kart.transform,"Simge",MahalleGraphic.Shape.Star,24,22,58,58,tamam?Gold:Line);
            Text(kart.transform,L.T(MahalleProfile.AchievementNames[i]),100,14,600,40,27,Ink);
            Text(kart.transform,L.T(MahalleProfile.AchievementNotes[i]),100,52,600,36,22,Muted);
            Text(kart.transform,ilerleme+" / "+hedef,712,14,240,40,26,tamam?Ink:Muted,TextAlignmentOptions.Right);
            Panel(kart.transform,"Yol",712,64,240,10,Line).radius=5;
            var d2=Panel(kart.transform,"Dolu",712,64,Mathf.Max(10f,240f*ilerleme/hedef),10,tamam?Gold:Ink);d2.radius=5;
        }
        // Game Center: liderlik tabloları ve başarımlar (ücretli hesapla açılır, MisketrCloud.Enabled).
        if(MisketrCloud.Enabled)
            LabelButton(content,"GAME CENTER · SIRALAMA",48,2170,984,96,Ink,Cream,MisketrGameCenter.ShowDashboard,30);
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
    {if(!MahalleProfile.Unlocked(index))return;if(index==0&&!MahalleProfile.Data.howToPlayDone&&MahalleProfile.Data.stars[0]==0){StartTutorial(false);return;}GameSession.TutorialMode=false;GameSession.DailyMode=false;GameSession.EndlessMode=false;GameSession.SelectedLevelIndex=index;RememberMap(Maps.MapOf(index));Time.timeScale=1;SceneManager.LoadScene(GameSession.GameSceneName);}
    // OYUN ICI ARAYUZ. Renkler: krem #F5EFE2, komur #2D2B25, amber #EAA640.
    // Ust bolum ekranin ustune, alt panel altina yaslanir; ortadaki oyun alani
    // bos birakilir, uzerine hicbir arayuz konmaz.
    private static readonly Color Krem   = new Color(.961f, .937f, .886f);
    private static readonly Color Komur  = new Color(.176f, .169f, .145f);
    private static readonly Color Amber  = new Color(.918f, .651f, .251f);
    private static readonly Color Orman  = new Color(.125f, .239f, .204f);

    // Zemin uzerine yazilan metin. Mahallelerin zemini koyu asfalttan acik
    // betona kadar degistigi icin tek renk her yerde okunmuyor: krem yazinin
    // arkasina koyu bir kopya koyuyoruz, ikisi birlikte her zeminde okunur.
    private TextMeshProUGUI YerYazisi(Transform parent,string value,float x,float y,float w,float h,
                                      float size,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft)
    {
        var golge=Text(parent,value,x+3,y+3,w,h,size,new Color(.06f,.05f,.04f,.55f),align);
        golge.raycastTarget=false;
        var ana=Text(parent,value,x,y,w,h,size,Krem,align);
        return ana;
    }

    private void ShowGame()
    {
        ClearPage();lastScore=lastShots=-1;resultShown=false;scorePop=0f;
        shooterLine=FindFirstObjectByType<ShooterLine>();

        // --- BASLIK: bolum adi ve numarasi ---
        string ustBaslik, altBaslik;
        if(GameSession.DailyMode)      { ustBaslik=L.T("GÜNÜN BÖLÜMÜ"); altBaslik=DailyLevel.DateLabel; }
        else if(GameSession.EndlessMode){ ustBaslik=L.T("SONSUZ ÇEMBER"); altBaslik=L.F("KADEME {0}",controller.EndlessStage); }
        else
        {
            ustBaslik=L.T(Maps.DistrictName(controller.Level.district));
            altBaslik=GameSession.TutorialMode ? L.T("NASIL OYNANIR")
                    : L.F("BÖLÜM {0}",(controller.LevelIndex%12+1).ToString("00"));
        }
        // Baslikta golge YOK: denendi, iki kopya ust uste bulanik duruyordu.
        var bas=Text(page,ustBaslik,56,26,760,72,54,Krem);
        bas.fontStyle=FontStyles.Bold; bas.enableAutoSizing=true; bas.fontSizeMin=34; bas.fontSizeMax=54;
        var alt=Text(page,altBaslik,58,100,760,40,26,new Color(Krem.r,Krem.g,Krem.b,.82f));
        alt.characterSpacing=9f; alt.fontStyle=FontStyles.Bold;

        // --- DURAKLAT: krem yuvarlak dugme ---
        if(GameSession.TutorialMode)
        {
            var gec=LabelButton(page,"GEÇ",876,24,132,104,Krem,Komur,SkipTutorial,32);
            ((MahalleGraphic)gec.targetGraphic).radius=52;
        }
        else
        {
            var mola=LabelButton(page,"II",904,24,104,104,Krem,Komur,Pause,44);
            ((MahalleGraphic)mola.targetGraphic).radius=52;
        }

        // --- BILGI PANELI: tek koyu serit, ince ayiricilarla uce bolunmus ---
        var bilgi=Panel(page,"Bilgi paneli",48,150,984,206,Komur); bilgi.radius=42;
        const float SUT=328f;
        var ayirici=new Color(Krem.r,Krem.g,Krem.b,.22f);
        Panel(bilgi.transform,"Ayırıcı 1",SUT,28,2,76,ayirici).radius=1;
        Panel(bilgi.transform,"Ayırıcı 2",SUT*2,28,2,76,ayirici).radius=1;

        var solEtiket=new Color(Krem.r,Krem.g,Krem.b,.66f);
        Text(bilgi.transform,"ÇIKAN MİSKET",0,24,SUT,28,22,solEtiket,TextAlignmentOptions.Center).characterSpacing=3f;
        scoreLabel=Text(bilgi.transform,"0",0,58,SUT,54,42,Krem,TextAlignmentOptions.Center);
        scoreLabel.fontStyle=FontStyles.Bold;

        Text(bilgi.transform,GameSession.EndlessMode?"SÜRE":"KALAN ATIŞ",SUT,24,SUT,28,22,solEtiket,TextAlignmentOptions.Center).characterSpacing=3f;
        shotsLabel=Text(bilgi.transform,"5",SUT,58,SUT,54,42,Amber,TextAlignmentOptions.Center);
        shotsLabel.fontStyle=FontStyles.Bold;

        Art(bilgi.transform,"Boncuk",MahalleGraphic.Shape.Marble,SUT*2+44,40,52,52,Amber);
        beadsLabel=Text(bilgi.transform,MahalleProfile.Beads.ToString(),SUT*2+108,38,190,56,36,Krem);
        beadsLabel.fontStyle=FontStyles.Bold;

        // --- HEDEF KAPSULU: krem, ortalanmis, solunda amber nokta ---
        string hedef=GameSession.EndlessMode
                   ? L.F("Her misket +{0} saniye",EndlessLevel.TimePerMarble.ToString("0.#"))
                   : GameSession.TutorialMode ? L.T("Bütün misketleri çemberin dışına çıkar")
                   : L.F("En az {0} misketi çemberden çıkar",PassTarget());
        // Hedef artik ayri bir krem kutu degil, ayni panelin alt seridi.
        // Ayri kutu cemberle HUD'un arasini daraltiyordu.
        Panel(bilgi.transform,"Hedef ayırıcı",40,132,904,2,new Color(Krem.r,Krem.g,Krem.b,.16f)).radius=1;
        Art(bilgi.transform,"Hedef noktası",MahalleGraphic.Shape.Circle,44,158,18,18,Amber).radius=9;
        Text(bilgi.transform,hedef,76,144,840,48,25,new Color(Krem.r,Krem.g,Krem.b,.92f));

        // BOLUM IMZASI (sadece test yapisi acikken). Editor ile telefon ayni
        // bolumu mu oynuyor, onu karsilastirmak icin.
        if(MahalleProfile.TestUnlockAllLevels||MahalleProfile.TestInfiniteBeads)
        {
            var lv=controller.Level;
            int mn=lv.marbles!=null?lv.marbles.Length:0;
            int en=lv.obstacles!=null?lv.obstacles.Length:0;
            string imza="#"+controller.LevelIndex+" · "+mn+"m "+en+"e · saha "+lv.arenaSize.ToString("0.00");
            if(mn>0)imza+=" · ilk "+lv.marbles[0].x.ToString("0.00")+","+lv.marbles[0].z.ToString("0.00");
            Text(page,imza,48,366,984,30,21,new Color(1f,.42f,.20f,.9f),TextAlignmentOptions.Center);
            Text(page,MahalleWorld.Diagnose(),48,394,984,30,21,new Color(1f,.35f,.28f,.9f),TextAlignmentOptions.Center);
        }

        // --- ALT PANEL: tek krem panel ---
        // Alt cubuk artik koyu: ustteki bilgi paneliyle ayni aile, ortadaki oyun alani one cikiyor.
        var dock=Panel(page,"Atış alanı",0,0,1008,190,Komur);
        dock.colorB=new Color(.129f,.125f,.106f); dock.radius=52; dock.shadow=14;
        Bottom(dock.rectTransform,36,40,1008,190);

        // Dokunma alani amber dairenin degil, yaziyla birlikte butun solun
        // uzeri: oyuncu "KESEM" yazisina da basabilsin.
        var kese=Button(dock.transform,"Misket kesesi",22,28,412,134,new Color(1,1,1,0),OpenBag);
        ((MahalleGraphic)kese.targetGraphic).radius=40;
        var daire=Panel(kese.transform,"Kese dairesi",8,7,120,120,Amber); daire.radius=60;
        IconOrShape(daire.transform,"Kesem",MahalleGraphic.Shape.Bag,20,20,80,80,Komur);
        Text(kese.transform,"KESEM",154,18,240,44,34,Krem).fontStyle=FontStyles.Bold;
        Text(kese.transform,"Özel misket seç",154,68,260,40,24,new Color(Krem.r,Krem.g,Krem.b,.62f));
        if(GameSession.TutorialMode){tutBag=(RectTransform)kese.transform;kese.gameObject.SetActive(false);}

        Panel(dock.transform,"Ayırıcı",446,40,2,110,new Color(Krem.r,Krem.g,Krem.b,.22f)).radius=1;

        Art(dock.transform,"Seçili misket",MahalleGraphic.Shape.Marble,486,44,58,58,Amber);
        powerLabel=Text(dock.transform,"NORMAL MİSKET",562,40,420,44,30,Krem);
        powerLabel.fontStyle=FontStyles.Bold;
        powerLabel.enableAutoSizing=true; powerLabel.fontSizeMin=20; powerLabel.fontSizeMax=30;
        Text(dock.transform,"ATIŞ GÜCÜ",562,88,420,34,22,new Color(Krem.r,Krem.g,Krem.b,.62f)).characterSpacing=2f;
        Panel(dock.transform,"Güç boş",562,132,412,14,new Color(Krem.r,Krem.g,Krem.b,.18f)).radius=7;
        powerFill=Panel(dock.transform,"Güç dolu",562,132,1,14,Amber); powerFill.radius=7;

        // --- YARDIM METNI: kutu yok, ortalanmis ---
        var hintGolge=Text(page,"",51,0,984,54,34,new Color(.06f,.05f,.04f,.55f),TextAlignmentOptions.Center);
        hintGolge.fontStyle=FontStyles.Bold;
        Bottom(hintGolge.rectTransform,51,297,984,54);
        hintLabel=Text(page,"",48,0,984,54,34,Krem,TextAlignmentOptions.Center);
        hintLabel.fontStyle=FontStyles.Bold;
        Bottom(hintLabel.rectTransform,48,300,984,54);
        hintShadow=hintGolge;

        if(GameSession.TutorialMode)
        {
            // Ogreticide alttaki genel ipucu yazisi gizlenir: ogretici panelinin
            // metniyle ust uste biniyordu.
            hintLabel.gameObject.SetActive(false);
            if(hintShadow!=null)hintShadow.gameObject.SetActive(false);
            TutorialPanel();
        }
        else if(!MahalleProfile.Data.tutorialDone)
        {
            var ipGolge=Text(page,"Atıcıyı taşımak için çizgiye dokun.",51,0,984,44,26,
                             new Color(.06f,.05f,.04f,.5f),TextAlignmentOptions.Center);
            Bottom(ipGolge.rectTransform,51,251,984,44);
            var ipucu=Text(page,"Atıcıyı taşımak için çizgiye dokun.",48,0,984,44,26,
                           new Color(Krem.r,Krem.g,Krem.b,.88f),TextAlignmentOptions.Center);
            Bottom(ipucu.rectTransform,48,254,984,44);
        }
        SlopeBadge();
        // HARİTA 2: bu bölümde ilk kez karşılaşılan özellik varsa oyun başlamadan anlat.
        if(!GameSession.TutorialMode&&!GameSession.DailyMode&&!GameSession.EndlessMode)ShowNewMechanics();
    }

    // HARİTA 2 EĞİM ROZETİ: bilgi panelinin altında, sağda. Ok eğimin indiği yönü
    // gösterir (dünyada x = ekranda sağ, z = ekranda yukarı; kamera sadece eğik bakıyor).
    private void SlopeBadge()
    {
        if(controller==null||controller.Level==null)return;
        var s=controller.Level.slope;
        if(s.sqrMagnitude<1e-6f)return;
        var rozet=Panel(page,"Eğim rozeti",764,372,268,72,Komur);rozet.radius=36;rozet.highlight=false;rozet.raycastTarget=false;
        var ok=Art(rozet.transform,"Eğim oku",MahalleGraphic.Shape.Chevron,24,14,44,44,Amber);
        ok.rectTransform.pivot=new Vector2(.5f,.5f);ok.rectTransform.anchoredPosition=new Vector2(46,-36);
        ok.rectTransform.localEulerAngles=new Vector3(0,0,Mathf.Atan2(s.y,s.x)*Mathf.Rad2Deg);
        var yazi=Text(rozet.transform,"EĞİM",86,0,170,72,30,Krem);yazi.fontStyle=FontStyles.Bold;
    }

    // ---------------------------------------------------------------
    // YENİ ÖZELLİK KARTLARI. Bir özellik (kum, çamur, eğim, buzlu misket, karpuz,
    // çukur) oyuncunun karşısına İLK çıktığı bölümde, oyun başlamadan tek tek anlatılır.
    // Kayıtta bit olarak tutulur; bir kez gösterilir. Test yapısında da gösterilir
    // (telefonda denenebilsin diye) — Ayarlar'daki sıfırlama tekrar gösterir.
    public static readonly string[] MechanicTitles = { "KUM", "ÇAMUR", "EĞİM", "BUZLU MİSKET", "KARPUZ MİSKET", "ÇUKUR" };
    public static readonly string[] MechanicTexts = {
        "Kumun içinden geçen misket çabuk yavaşlar. Kumun ötesine ulaşmak için sert at.",
        "Çamura giren misket saplanıp kalır. Hedefleri çamura değil, çemberin dışına it.",
        "Burası yokuş: yuvarlanan misketler okların gösterdiği yöne kayar. Yavaş misket daha çok kayar.",
        "Buzlu misket kıpırdamaz. Önce sert bir atışla buzunu kır, sonra dışarı çıkar. Hafif vuruş işe yaramaz.",
        "Sert vurursan ikiye bölünür. İki yarısı birlikte 1 misket sayılır: ikisini de çemberin dışına çıkar.",
        "Yavaş yuvarlanan misket çukura düşer ve kaybolur, sayılmaz. Hızlı misket çukurun üstünden geçer." };
    public static int MechanicsIn(LevelData l)
    {
        int m=0;
        if(l==null)return 0;
        if(l.zones!=null)foreach(var z in l.zones){if(z.kind==ZoneKind.Sand)m|=1;else if(z.kind==ZoneKind.Mud)m|=2;else if(z.kind==ZoneKind.Pit)m|=32;}
        if(l.slope.sqrMagnitude>1e-6f)m|=4;
        if(l.marbles!=null)foreach(var s in l.marbles){if(s.kind==MarbleKind.Ice)m|=8;else if(s.kind==MarbleKind.Split)m|=16;}
        return m;
    }
    private void ShowNewMechanics()
    {
        int fresh=MechanicsIn(controller.Level)&~MahalleProfile.Data.mechanicsSeen;
        if(fresh==0)return;
        int bit=0;while((fresh&(1<<bit))==0)bit++;
        controller.SetPaused(true);
        var box=Modal("Yeni özellik",900);
        var face=box.GetComponent<MahalleGraphic>();if(face!=null){face.color=Krem;face.colorB=Krem;face.highlight=false;}
        var etiket=Panel(box,"Yeni etiketi",36,36,200,56,Amber);etiket.radius=28;etiket.highlight=false;
        var yeni=Text(etiket.transform,"YENİ",0,0,200,56,28,Komur,TextAlignmentOptions.Center);yeni.fontStyle=FontStyles.Bold;
        var baslik=Text(box,MechanicTitles[bit],36,110,912,80,58,Komur,TextAlignmentOptions.Center);baslik.fontStyle=FontStyles.Bold;
        MechanicPicture(box,bit);
        Text(box,MechanicTexts[bit],60,480,864,220,34,new Color(Komur.r,Komur.g,Komur.b,.85f),TextAlignmentOptions.Center);
        LabelButton(box,"ANLADIM",36,760,912,104,Komur,Krem,()=>
        {
            MahalleProfile.Data.mechanicsSeen|=1<<bit;MahalleProfile.Save();
            CloseModal(false);
            if((MechanicsIn(controller.Level)&~MahalleProfile.Data.mechanicsSeen)!=0)ShowNewMechanics();   // sıradaki
            else controller.SetPaused(false);
        },38);
    }
    // Kartın ortasındaki küçük çizim: zemin için renkli disk, misket için misket.
    private void MechanicPicture(RectTransform box,int bit)
    {
        float cx=492,cy=330;
        Color sand=new Color(.90f,.80f,.58f),mud=new Color(.36f,.26f,.17f),pit=new Color(.2f,.15f,.1f);
        switch(bit)
        {
            case 0: Art(box,"Kum",MahalleGraphic.Shape.Circle,cx-130,cy-110,260,220,sand); Art(box,"Misket",MahalleGraphic.Shape.Marble,cx-40,cy-40,80,80,Campaign.SkinColors[1]); break;
            case 1: Art(box,"Çamur",MahalleGraphic.Shape.Circle,cx-130,cy-110,260,220,mud); Art(box,"Misket",MahalleGraphic.Shape.Marble,cx-30,cy-10,80,80,Campaign.SkinColors[3]); break;
            case 2: for(int i=0;i<3;i++)Art(box,"Ok",MahalleGraphic.Shape.Chevron,cx-150+i*110,cy-40,80,80,Amber); break;
            case 3: Art(box,"Buz",MahalleGraphic.Shape.Circle,cx-80,cy-80,160,160,new Color(.72f,.9f,1f,.7f)); Art(box,"Misket",MahalleGraphic.Shape.Marble,cx-50,cy-50,100,100,Campaign.SkinColors[1]); break;
            case 4:
            {
                var k=Art(box,"Karpuz",MahalleGraphic.Shape.Marble,cx-190,cy-60,120,120,SplitMarble.Rind);k.accent=SplitMarble.Stripe;
                Art(box,"Ok",MahalleGraphic.Shape.Chevron,cx-50,cy-30,60,60,Komur);
                var a=Art(box,"Yarım",MahalleGraphic.Shape.Marble,cx+40,cy-75,84,84,SplitMarble.Flesh);a.accent=SplitMarble.Seed;
                var b=Art(box,"Yarım",MahalleGraphic.Shape.Marble,cx+110,cy+5,84,84,SplitMarble.Flesh);b.accent=SplitMarble.Seed;
                break;
            }
            default: Art(box,"Çukur",MahalleGraphic.Shape.Circle,cx-110,cy-90,220,180,pit); Art(box,"Misket",MahalleGraphic.Shape.Marble,cx-160,cy-110,70,70,Campaign.SkinColors[2]); break;
        }
    }

    // iCLOUD / GAME CENTER: menüdeyken iki saniyede bir bak. Başka cihazdan daha yeni
    // kayıt geldiyse al (oyun sırasında asla), değişen skor/başarımları gönder.
    private float nextCloudTick;
    private bool onHome;
    private void CloudTick()
    {
        if(!MisketrCloud.Enabled||controller!=null||Time.unscaledTime<nextCloudTick)return;
        nextCloudTick=Time.unscaledTime+2f;
        if(MisketrCloud.ConsumeChanged()&&MahalleProfile.AdoptCloudIfNewer())
        {
            district=MahalleProfile.NextLevelIn(CurrentMap())/Campaign.PerDistrict;
            if(onHome&&modal==null)ShowHome();
            Toast("İlerlemen iCloud'dan yüklendi.");
        }
        MisketrGameCenter.ReportIfChanged();
    }
    private void Update()
    {
        CloudTick();
        if(controller==null || scoreLabel==null)return;
        if(GameSession.EndlessMode)
        {
            if(lastScore!=controller.EndlessScore){lastScore=controller.EndlessScore;scoreLabel.SetTextL(controller.EndlessScore.ToString());}
            // Süre: son 10 saniyede kırmızı ve nabız gibi atar.
            int kalan=Mathf.CeilToInt(controller.EndlessTimeLeft);
            if(lastShots!=kalan){lastShots=kalan;shotsLabel.SetTextL(kalan.ToString());}
            bool acil=controller.EndlessTimeLeft<=10f;
            shotsLabel.color=acil?Color.Lerp(new Color(.95f,.35f,.28f),Cream,Mathf.PingPong(Time.unscaledTime*4f,1f)):Gold;
            if(hintLabel!=null)
                hintLabel.SetTextL(controller.EndlessGainFlash>0f?L.F("+{0} SANİYE",EndlessLevel.TimePerMarble.ToString("0.#")):
                                   controller.WaitingForSettle?"Misketler duruluyor…":"");
        }
        else
        {
            if(lastScore!=controller.Score)
            {
                lastScore=controller.Score;scoreLabel.SetTextL(controller.Score+" / "+controller.TotalMarbles);
                scorePop=1f;   // artti: kisa bir ziplama
            }
            if(lastShots!=controller.ShotsLeft){lastShots=controller.ShotsLeft;shotsLabel.SetTextL(controller.ShotsLeft.ToString());}
            // Son atis: sayac nabiz gibi atsin, oyuncu farketsin.
            shotsLabel.color=controller.ShotsLeft<=1
                ?Color.Lerp(new Color(.96f,.42f,.30f),Gold,Mathf.PingPong(Time.unscaledTime*3f,1f)):Gold;
        }
        beadsLabel.SetTextL(MahalleProfile.Beads.ToString());
        // Sayi ziplamasi: misket cikinca gozle gorulur bir tepki.
        if(scorePop>0f)
        {
            scorePop=Mathf.Max(0f,scorePop-Time.unscaledDeltaTime*4.2f);
            float k=1f+Mathf.Sin(scorePop*Mathf.PI)*.24f;
            scoreLabel.rectTransform.localScale=new Vector3(k,k,1f);
        }
        var shooter=controller.Shooter;
        if(shooter!=null)
        {
            powerFill.rectTransform.sizeDelta=new Vector2(Mathf.Max(1,412*shooter.Power),14);
            powerLabel.SetTextL(shooter.SelectedPower==MarblePower.None?(SpecialMarbles.IsSpecial(shooter.ActiveSkin)?L.T(Campaign.SkinNames[shooter.ActiveSkin])+" · "+MahalleProfile.RemainingLife(shooter.ActiveSkin)+"/"+SpecialMarbles.MaxLife:"NORMAL MİSKET"):L.Up(L.T(Campaign.PowerNames[(int)shooter.SelectedPower])));
            // Cizgi cok kisaldiysa atici zaten neredeyse hic oynamiyor;
            // "yerini degistirebilirsin" demek yaniltici oluyor, o yuzden susuyoruz.
            bool cizgiOynar = shooterLine != null && shooterLine.HalfWidth > 1.6f;
            string ipucuMetni=controller.WaitingForSettle?"Misketler duruluyor…"
                             :shooter.IsAiming?"Gücü ayarla ve bırak"
                             :shooter.PositionLocked?"Misketin durduğu yerden atıyorsun."
                             :cizgiOynar?"Çizgiye dokunarak atıcının yerini değiştirebilirsin.":"";
            hintLabel.SetTextL(ipucuMetni);
            if(hintShadow!=null)hintShadow.SetTextL(ipucuMetni);
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
        // Güvenli alanın (alt çubuk, çentik) ve iPad sütununun dışını da karart: yoksa altta açık şerit kalıyordu.
        shade.rectTransform.offsetMin=new Vector2(-2000,-2000);shade.rectTransform.offsetMax=new Vector2(2000,2000);
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
        controller.SetPaused(true);var box=Modal("Mola",680);
        Text(box,"Bir nefes al",36,34,912,81,57,Ink,TextAlignmentOptions.Center);
        LabelButton(box,"DEVAM ET",36,164,912,100,Ink,Cream,()=>CloseModal());
        if(GameSession.DailyMode)
            LabelButton(box,L.F("TEKRAR DENE · {0} HAK",MahalleProfile.DailyTriesLeft),36,290,912,100,new Color(.88f,.83f,.69f),Ink,
                        ()=>{if(!MahalleProfile.DailyUseTry()){Toast("Bugünlük hakkın bitti. Yarın yeni bölüm gelir.");return;}CloseModal();controller.RestartLevel();ShowGame();},30);
        else LabelButton(box,"TEKRAR DENE",36,290,912,100,new Color(.88f,.83f,.69f),Ink,()=>{CloseModal();controller.RestartLevel();ShowGame();});
        LabelButton(box,"HARİTAYA DÖN",36,416,912,100,new Color(.88f,.83f,.69f),Ink,()=>controller.OpenLevelSelect());
        LabelButton(box,"AYARLAR",36,554,912,78,Paper,Muted,Settings,27);
    }
    // Hedef yazısı: sonraki bölümü açmak için gereken misket. Ustalık sınavında 2 yıldız
    // gerekiyor, 1 yıldızın sayısını yazmak oyuncuyu yanıltıyordu (kullanıcı, 2026-09-24).
    private int PassTarget()
    {
        var l=controller.Level;
        int need=GameSession.DailyMode?1:MahalleProfile.Required(controller.LevelIndex);
        return need>=3?l.threeStarTarget:need==2?l.twoStarTarget:l.oneStarTarget;
    }
    private void Results()
    {
        if(GameSession.TutorialMode){TutorialDone();return;}
        if(GameSession.DailyMode){DailyResults();return;}
        if(GameSession.EndlessMode){EndlessResults();return;}
        bool won=controller.State==LevelController.LevelState.Won;
        int need=MahalleProfile.Required(controller.LevelIndex);
        bool passed=won&&controller.Stars>=need;
        // Test yapısında harita geçişi kayıttan tetiklenmez: son bölümü geçince haritaya dönüşte oynasın.
        if(passed&&MahalleProfile.TestUnlockAllLevels&&!GameSession.DailyMode&&!GameSession.EndlessMode
           &&Maps.Local(controller.LevelIndex)==Maps.PerMap-1&&Maps.MapOf(controller.LevelIndex)+1<Maps.Count)
            pendingMapIntro=Maps.MapOf(controller.LevelIndex)+1;
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
        else LabelButton(box,passed?"HARİTAYA DÖN":"TEKRAR DENE",36,669,912,98,Ink,Cream,()=>{if(passed)controller.OpenLevelSelect();else{CloseModal();controller.RestartLevel();ShowGame();}});
        LabelButton(box,passed?"REKORUNU GELİŞTİR":"HARİTAYA DÖN",36,791,912,86,new Color(.88f,.83f,.69f),Ink,()=>{if(passed){CloseModal();controller.RestartLevel();ShowGame();}else controller.OpenLevelSelect();},30);
        if(passed&&controller.HasNextLevel)LabelButton(box,"MAHALLE HARİTASI",36,898,912,76,Paper,Muted,()=>controller.OpenLevelSelect(),27);
        // Oyuncu iyi bir anda: ilk kez 3 yıldızla geçtiyse ve yeterince oynadıysa
        // iOS'un kendi puanlama penceresi bir kez açılır.
        if(passed&&controller.Stars==3&&!MahalleProfile.Data.reviewAsked&&MahalleProfile.MasteryPoints>=200)
        {
            MahalleProfile.Data.reviewAsked=true;MahalleProfile.Save();
            StartCoroutine(AskReview());
        }
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
        Text(card,GameSession.EndlessMode?L.Up(L.T("SONSUZ ÇEMBER")):GameSession.DailyMode?L.Up(L.T("GÜNÜN BÖLÜMÜ")):L.Up(L.T(Maps.DistrictName(lv.district)))+" · "+no.ToString("00"),0,150,1080,70,46,new Color(1,.97f,.89f,.7f),TextAlignmentOptions.Center);
        Text(card,GameSession.EndlessMode?L.F("KADEME {0}",controller.EndlessStage):GameSession.DailyMode?DailyLevel.DateLabel:lv.levelName,40,222,1000,100,68,Cream,TextAlignmentOptions.Center);
        var ring=Art(card,"Tebeşir çemberi",MahalleGraphic.Shape.ChalkRing,150,400,780,780,chalk);ring.stroke=10f;ring.progress=1f;
        for(int i=0;i<3;i++)
        {
            float size=i==1?170:135;
            Art(card,"Yıldız "+i,MahalleGraphic.Shape.Star,540-size/2+(i-1)*185,i==1?510:545,size,size,i<stars?Gold:new Color(1,.98f,.92f,.18f));
        }
        Text(card,GameSession.EndlessMode?controller.EndlessScore.ToString():controller.ShotsUsed.ToString(),150,700,780,210,200,Cream,TextAlignmentOptions.Center);
        Text(card,GameSession.EndlessMode?"MİSKET":"ATIŞTA",150,905,780,70,52,new Color(1,.97f,.89f,.75f),TextAlignmentOptions.Center);
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
        Text(card,"MİSKO",0,1560,1080,170,140,new Color(1,.98f,.92f),TextAlignmentOptions.Center);
        Text(card,"mahallenin en iyi nişancısı kim?",0,1730,1080,56,36,new Color(1,.97f,.89f,.62f),TextAlignmentOptions.Center);
    }
    private string ShareText()
    {
        if(GameSession.EndlessMode)return L.F("MİSKO · Sonsuz Çember'de {0} misket çıkardım. Sen kaç yaparsın?",controller.EndlessScore);
        if(GameSession.DailyMode)return L.F("MİSKO · Günün bölümünü ({0}) {1} atışta {2} yıldızla bitirdim. Sen kaç atışta bitirirsin?",DailyLevel.DateLabel,controller.ShotsUsed,controller.Stars);
        int no=controller.LevelIndex%Campaign.PerDistrict+1;
        return L.F("MİSKO · {0} {1} bölümünü {2} atışta {3} yıldızla bitirdim. Sen kaç atışta bitirirsin?",L.T(Maps.DistrictName(controller.Level.district)),no.ToString("00"),controller.ShotsUsed,controller.Stars);
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
        // Anlatim paneli oyun ekraninin geri kalaniyla ayni aile: komur zemin,
        // amber baslik, krem metin. Eski yesil, altindaki komur atis cubuguyla
        // yamali duruyordu.
        var panel=Panel(page,"Öğretici",60,0,960,190,new Color(Komur.r,Komur.g,Komur.b,.95f));
        panel.radius=42;Bottom(panel.rectTransform,60,300,960,190);
        tutTitle=Text(panel.transform,"",28,14,904,54,34,Amber,TextAlignmentOptions.Center);
        tutBody=Text(panel.transform,"",28,70,904,106,29,Krem,TextAlignmentOptions.Center);
        tutSpot=Art(page,"Öğretici noktası",MahalleGraphic.Shape.ChalkRing,0,0,120,120,Amber);tutSpot.stroke=7f;
        tutHand=Art(page,"Öğretici eli",MahalleGraphic.Shape.Hand,0,0,65,88,new Color(1,.97f,.89f,0f)).rectTransform;
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
    // ---------------------------------------------------------------
    // GÜNÜN BÖLÜMÜ
    // ---------------------------------------------------------------
    private void StartEndless()
    {
        GameSession.TutorialMode=false;GameSession.DailyMode=false;GameSession.EndlessMode=true;
        Time.timeScale=1;
        SceneManager.LoadScene(GameSession.GameSceneName);
    }
    private void LeaveEndless()
    {
        GameSession.EndlessMode=false;returnToTitle=true;Time.timeScale=1;
        SceneManager.LoadScene(GameSession.LevelSelectSceneName);
    }
    private void EndlessResults()
    {
        bool rekor=controller.LastEndlessRecord;
        var box=Modal("Sonsuz sonucu",900);
        Text(box,rekor?"YENİ REKOR!":"SÜRE BİTTİ",30,34,924,77,rekor?52:48,Ink,TextAlignmentOptions.Center);
        Text(box,L.F("KADEME {0}",controller.EndlessStage),30,114,924,47,29,Muted,TextAlignmentOptions.Center);
        Art(box,"Büyük yıldız",MahalleGraphic.Shape.Star,412,180,160,160,rekor?Gold:Line);
        Text(box,controller.EndlessScore.ToString(),30,346,924,120,92,Ink,TextAlignmentOptions.Center);
        Text(box,"misket çıkardın",30,462,924,50,30,Muted,TextAlignmentOptions.Center);
        Text(box,L.F("REKOR: {0}",MahalleProfile.Data.endlessBest),30,516,924,50,30,rekor?Gold:Muted,TextAlignmentOptions.Center);
        LabelButton(box,"TEKRAR DENE",36,586,912,98,Ink,Cream,()=>{CloseModal();controller.RestartLevel();ShowGame();});
        LabelButton(box,"PAYLAŞ",36,700,912,90,Gold,Ink,ShareCard,32);
        LabelButton(box,"ANA MENÜ",36,806,912,86,new Color(.88f,.83f,.69f),Ink,LeaveEndless,30);
    }
    private void StartDaily()
    {
        if(!DailyLevel.Available){Toast("Günün bölümü çok yakında!");return;}
        if(MahalleProfile.Data.stars[0]==0){Toast("Günün bölümü 1. bölümü bitirince açılır.");return;}
        if(MahalleProfile.DailyDoneToday){Toast("Bugünün bölümünü geçtin. Yarın yenisi gelir.");return;}
        if(!MahalleProfile.DailyUseTry()){Toast("Bugünlük hakkın bitti. Yarın yeni bölüm gelir.");return;}
        if(!MahalleProfile.Data.notifyAsked)
        {
            MahalleProfile.Data.notifyAsked=true;MahalleProfile.Save();
            MisketrNotify.RequestPermission();MisketrNotify.Refresh();
        }
        GameSession.TutorialMode=false;GameSession.EndlessMode=false;GameSession.DailyMode=true;
        Time.timeScale=1;
        SceneManager.LoadScene(GameSession.GameSceneName);
    }
    private void LeaveDaily()
    {
        GameSession.DailyMode=false;returnToTitle=true;Time.timeScale=1;
        SceneManager.LoadScene(GameSession.LevelSelectSceneName);
    }
    private void DailyResults()
    {
        bool won=controller.State==LevelController.LevelState.Won;
        int beads=controller.LastReward.beads;
        var box=Modal("Günün bölümü sonucu",won?1010:860);
        Text(box,won?"GÜNÜN BÖLÜMÜ TAMAM!":"BİR DAHA DENE",30,34,924,77,won?48:51,Ink,TextAlignmentOptions.Center);
        Text(box,DailyLevel.DateLabel,30,114,924,47,31,Muted,TextAlignmentOptions.Center);
        for(int i=0;i<3;i++){var star=Art(box,"Sonuç yıldızı "+i,MahalleGraphic.Shape.Star,259+i*163,i==1?184:201,i==1?142:115,i==1?142:115,Line);if(i<controller.Stars&&won)StartCoroutine(RevealStar(star,i));}
        Text(box,L.F("{0} / {1} misket çıkardın",controller.Score,controller.TotalMarbles),30,366,924,64,40,Ink,TextAlignmentOptions.Center);
        string line=beads>0?L.F("+{0} BONCUK · SERİ {1} GÜN",beads,MahalleProfile.Data.dailyStreak)
                   :won?L.T("Bugünün ödülünü aldın. Yarın yeni bölüm!")
                   :L.T("Bugün istediğin kadar deneyebilirsin. Yarın yeni bölüm gelir.");
        Text(box,line,60,448,864,82,beads>0?38:28,beads>0?Ink:Muted,TextAlignmentOptions.Center);
        float y=560;
        if(!won&&MahalleProfile.DailyOpen)
        {
            LabelButton(box,L.F("TEKRAR DENE · {0} HAK",MahalleProfile.DailyTriesLeft),36,y,912,98,Ink,Cream,
                        ()=>{if(!MahalleProfile.DailyUseTry()){Toast("Bugünlük hakkın bitti. Yarın yeni bölüm gelir.");return;}CloseModal();controller.RestartLevel();ShowGame();},34);
            y+=122;
        }
        else if(!won)Text(box,L.T("Bugünlük hakkın bitti. Yarın yeni bölüm gelir."),60,y+18,864,60,28,Muted,TextAlignmentOptions.Center);
        if(won){LabelButton(box,"PAYLAŞ",36,y,912,98,Gold,Ink,ShareCard,34);y+=122;}
        else y+=80;
        LabelButton(box,"ANA MENÜ",36,y,912,86,new Color(.88f,.83f,.69f),Ink,LeaveDaily,30);
    }
    private IEnumerator AskReview()
    {
        yield return new WaitForSecondsRealtime(1.6f);   // yıldızlar açıldıktan sonra
#if UNITY_IOS && !UNITY_EDITOR
        UnityEngine.iOS.Device.RequestStoreReview();
#else
        Debug.Log("PUANLAMA: magaza puanlama penceresi acilirdi (sadece iPhone'da).");
#endif
    }
    private IEnumerator RevealStar(MahalleGraphic star,int index)
    {
        yield return new WaitForSecondsRealtime(.25f+index*.24f);
        if(star==null)yield break;star.color=Gold;float t=0;
        while(t<.24f&&star!=null){t+=Time.unscaledDeltaTime;star.transform.localScale=Vector3.one*(1+Mathf.Sin(Mathf.Clamp01(t/.24f)*Mathf.PI)*.22f);yield return null;}
    }
    private void Settings()
    {
        var box=Modal("Ayarlar",1320);Text(box,"Ayarlar",36,30,912,77,52,Ink);
        LabelButton(box,L.F("SES: {0}",L.T(MahalleProfile.Data.sound?"AÇIK":"KAPALI")),36,158,912,100,Ink,Cream,()=>{MahalleProfile.Data.sound=!MahalleProfile.Data.sound;MahalleProfile.Save();Settings();});
        LabelButton(box,L.F("MÜZİK: {0}",L.T(MahalleProfile.Data.music?"AÇIK":"KAPALI")),36,406,912,100,Ink,Cream,()=>{MahalleProfile.Data.music=!MahalleProfile.Data.music;MahalleProfile.Save();MusicPlayer.Refresh();Settings();});
        LabelButton(box,L.F("TİTREŞİM: {0}",L.T(MahalleProfile.Data.haptics?"AÇIK":"KAPALI")),36,282,912,100,Ink,Cream,()=>{MahalleProfile.Data.haptics=!MahalleProfile.Data.haptics;MahalleProfile.Save();Settings();});
        LabelButton(box,L.F("BİLDİRİM: {0}",L.T(MahalleProfile.Data.notify?"AÇIK":"KAPALI")),36,527,912,100,Ink,Cream,()=>{
            MahalleProfile.Data.notify=!MahalleProfile.Data.notify;
            if(MahalleProfile.Data.notify&&!MahalleProfile.Data.notifyAsked){MahalleProfile.Data.notifyAsked=true;MisketrNotify.RequestPermission();}
            MahalleProfile.Save();MisketrNotify.Refresh();Settings();});
        LabelButton(box,L.F("DİL: {0}",L.English?"ENGLISH":"TÜRKÇE"),36,649,912,89,new Color(.88f,.83f,.69f),Ink,ToggleLanguage,29);
        LabelButton(box,"NASIL OYNANIR",36,761,912,89,new Color(.88f,.83f,.69f),Ink,()=>StartTutorial(true),29);
        LabelButton(box,"İLERLEMEYİ SIFIRLA",36,873,912,89,new Color(.88f,.83f,.69f),Ink,ResetPrompt,29);
        LabelButton(box,"GİZLİLİK POLİTİKASI",36,985,912,89,new Color(.88f,.83f,.69f),Ink,()=>Application.OpenURL(PrivacyUrl),29);
        Text(box,"Boncuklar oyun içinden kazanılır. Gerçek para işlemi yoktur.",36,1097,912,64,25,Muted,TextAlignmentOptions.Center);
        LabelButton(box,"GERİ",36,1195,912,87,Ink,Cream,()=>{if(controller!=null)Pause();else CloseModal();},29);
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
