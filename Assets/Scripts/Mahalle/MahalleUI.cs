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
    private bool resultShown;
    public RectTransform Root => root;
    private void Start()
    {
        font=Resources.Load<TMP_FontAsset>("Mahalle/Nunito SDF");
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=0;
        gameObject.AddComponent<GraphicRaycaster>();
        root=Rect("SafeArea",transform);Stretch(root);root.gameObject.AddComponent<SafeAreaFit>();
        Canvas.ForceUpdateCanvases();
        controller=FindFirstObjectByType<LevelController>();
        if(controller!=null)controller.AnchorRefunded+=()=>Toast("Misketin işe yarar bir yerde kalmadı. Hakkın iade edildi.");
        district=MahalleProfile.NextLevel/Campaign.PerDistrict;
        if(controller==null)ShowHome();else ShowGame();
        // Test yapisi damgasi. Bu yazi ekranda goruniyorsa bu build yayinlanamaz.
        if(MahalleProfile.TestUnlockAllLevels)
        {
            var stamp=Text(root,"TEST · TÜM BÖLÜMLER AÇIK",0,0,700,44,26,new Color(1f,.45f,.25f,.85f),TextAlignmentOptions.Center);
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
        var r=Rect(value,parent);Place(r,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.text=value;t.font=font;t.fontSize=size;t.color=color;t.alignment=align;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;
    }
    private Button Button(Transform parent,string name,float x,float y,float w,float h,Color bg,Action action)
    {
        var g=Panel(parent,name,x,y,w,h,bg,true);var b=g.gameObject.AddComponent<Button>();b.targetGraphic=g;
        var colors=b.colors;colors.pressedColor=new Color(.8f,.86f,.8f);colors.selectedColor=Color.white;colors.highlightedColor=Color.white;colors.disabledColor=new Color(.65f,.65f,.65f);b.colors=colors;
        b.onClick.AddListener(()=>{if(SfxPlayer.Instance!=null)SfxPlayer.Instance.PlayUiTap();action();});return b;
    }
    private Button LabelButton(Transform parent,string title,float x,float y,float w,float h,Color bg,Color fg,Action action,float size=34)
    {var b=Button(parent,title,x,y,w,h,bg,action);Text(b.transform,title,12,0,w-24,h,size,fg,TextAlignmentOptions.Center);return b;}
    private void ClearPage()
    {CloseModal(false);if(page!=null){page.gameObject.SetActive(false);Destroy(page.gameObject);}page=Rect("Ekran",root);Stretch(page);}
    private void Background(Color color)
    {var g=Panel(page,"Zemin",0,0,1080,2500,color);g.radius=0;Stretch(g.rectTransform);}
    private void Header(string subtitle)
    {
#if UNITY_EDITOR
        if(MahalleProfile.PreviewMode)subtitle="ÖNİZLEME · TÜM BÖLÜMLER AÇIK · KAYIT YAPILMAZ";
#endif
        Text(page,"MİSKETR",48,24,630,76,64,Ink);
        Text(page,subtitle,50,104,740,42,27,Muted);
        var wallet=Panel(page,"Boncuk",805,35,225,76,Ink);
        Art(wallet.transform,"Boncuk simgesi",MahalleGraphic.Shape.Marble,18,16,44,44,Gold);
        beadsLabel=Text(wallet.transform,MahalleProfile.Data.beads.ToString(),78,0,130,76,36,Cream);
    }
    private void Nav()
    {
        var bg=Panel(page,"Alt menü",24,0,1032,132,new Color(.21f,.32f,.28f));
        bg.colorB=new Color(.11f,.18f,.16f);bg.radius=34;bg.shadow=14;bg.highlight=true;
        Bottom(bg.rectTransform,24,18,1032,132);
        string[] names={"MAHALLE","KESEM","GÖREVLER"};
        var shapes=new[]{MahalleGraphic.Shape.TabMap,MahalleGraphic.Shape.Bag,MahalleGraphic.Shape.TabTask};
        var faded=new Color(1,.97f,.89f,.6f);
        for(int i=0;i<3;i++)
        {
            int id=i;bool on=i==tab;
            var b=Button(bg.transform,names[i],10+i*337,10,331,112,on?new Color(1,1,1,.1f):new Color(1,1,1,0),()=>{tab=id;ShowHome();});
            b.GetComponent<MahalleGraphic>().radius=26;
            b.gameObject.AddComponent<MahalleTap>();
            var icon=Art(b.transform,"Simge",shapes[i],131,12,70,64,on?Gold:faded);
            icon.accent=on?Gold:faded;
            Text(b.transform,names[i],10,80,311,36,24,on?Gold:faded,TextAlignmentOptions.Center);
        }
    }
    private void ShowHome()
    {
        ClearPage();
        if(tab==0){Background(new Color(.14f,.12f,.10f));MapScreen();}
        else
        {
            Background(Paper);
            Header(tab==1?"HER MİSKETİN BİR HİKÂYESİ VAR":"KÜÇÜK HEDEFLER, YENİ BONCUKLAR");
            if(tab==1)Collection();else Missions();
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
        beadsLabel=Text(wallet.transform,MahalleProfile.Data.beads.ToString(),76,0,140,78,36,Cream);

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
        Text(resume.transform,(next%Campaign.PerDistrict+1).ToString("00")+" · "+level.levelName,34,48,700,54,36,new Color(.13f,.18f,.15f));
        Art(resume.transform,"Ok",MahalleGraphic.Shape.Chevron,878,36,48,48,new Color(.13f,.18f,.15f));
    }

    private void Collection()
    {
        var content=HomeScroll(2840);
        Text(content,"Misket koleksiyonun",48,182,984,65,47,Ink);
        Text(content,"Klasik misketler aşınmaz. Özellikli misketler aşağıda.",48,256,984,76,30,Muted);
        for(int i=0;i<SpecialMarbles.FirstSkin;i++) CollectionCard(content,i,363+(i/2)*302);

        Text(content,"ÖZELLİKLİ MİSKETLER",48,1300,984,58,38,Ink);
        Text(content,"400 boncuk · 150 atış ömür · Tam yenileme 100 boncuk.",48,1368,984,82,29,Muted);
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
            Text(card.transform,owned?MahalleProfile.RemainingLife(skin)+" / 150 ATIŞ":"400 BONCUK",193,126,265,48,26,selected?Gold:Muted);
            Text(card.transform,SpecialMarbles.Descriptions[skin-SpecialMarbles.FirstSkin],24,184,428,70,26,selected?Cream:Muted);
        }
        string label=worn?"AŞINDI":selected?"KUŞANILDI":owned?"KUŞAN":Campaign.SkinPrices[skin]+" BONCUK · AL";
        var buy=LabelButton(card.transform,label,24,special?270:194,428,62,selected?new Color(.27f,.39f,.31f):new Color(.89f,.85f,.72f),selected?Gold:Ink,()=>{if(MahalleProfile.EquipOrBuy(skin))ShowHome();else Toast("Yeterli boncuk yok veya misket yenilenmeli.");},25);
        buy.interactable=!selected&&!worn;
        if(special)
        {
            var repair=LabelButton(card.transform,"TAM YENİLE · 100 BONCUK",24,350,428,62,new Color(.89f,.85f,.72f),Ink,()=>{if(MahalleProfile.RepairMarble(skin))ShowHome();else Toast("Yenilemek için 100 boncuk gerekiyor.");},23);
            repair.interactable=owned&&MahalleProfile.RemainingLife(skin)<SpecialMarbles.MaxLife;
            Text(card.transform,"Özel güç atışında özelliği durur, ömrü azalmaz.",24,426,428,62,23,selected?Cream:Muted,TextAlignmentOptions.Center);
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
            Text(card.transform,value+" / "+MahalleProfile.MissionTargets[i]+"    +"+MahalleProfile.MissionRewards[i]+" BONCUK",28,89,655,45,29,Muted);
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
    {if(!MahalleProfile.Unlocked(index))return;GameSession.SelectedLevelIndex=index;Time.timeScale=1;SceneManager.LoadScene(GameSession.GameSceneName);}
    private void ShowGame()
    {
        ClearPage();lastScore=lastShots=-1;resultShown=false;
        var top=Panel(page,"Oyun başlığı",24,12,1032,246,Ink);top.radius=30;
        string title=Campaign.Districts[controller.Level.district]+"  /  "+(controller.LevelIndex%12+1).ToString("00");
        if(controller.Level.district==2 && controller.LevelIndex%12<3)title+=" · "+controller.Level.levelName;
        Text(top.transform,title,28,14,830,59,36,Cream);
        LabelButton(top.transform,"II",902,16,104,92,new Color(.28f,.38f,.31f),Cream,Pause,42);
        var score=Panel(top.transform,"Misket sayacı",24,92,330,100,new Color(.24f,.34f,.29f));
        Text(score.transform,"ÇIKAN MİSKET",18,10,294,29,23,new Color(.76f,.8f,.7f));scoreLabel=Text(score.transform,"0",18,39,294,51,39,Cream);
        var shots=Panel(top.transform,"Atış sayacı",374,92,255,100,new Color(.24f,.34f,.29f));
        Text(shots.transform,"KALAN ATIŞ",18,10,219,29,23,new Color(.76f,.8f,.7f));shotsLabel=Text(shots.transform,"5",18,39,219,51,39,Gold);
        var wallet=Panel(top.transform,"Boncuk",649,117,250,75,new Color(.24f,.34f,.29f));
        Art(wallet.transform,"Boncuk",MahalleGraphic.Shape.Marble,18,16,43,43,Gold);beadsLabel=Text(wallet.transform,MahalleProfile.Data.beads.ToString(),79,4,156,67,34,Cream);
        Text(top.transform,"HEDEF: "+controller.Level.oneStarTarget+" MİSKETİ ÇİZGİ DIŞINA ÇIKAR",26,202,960,32,25,new Color(.81f,.84f,.75f));
        var dock=Panel(page,"Atış alanı",24,0,1032,178,Ink);Bottom(dock.rectTransform,24,18,1032,178);
        var bag=Button(dock.transform,"Misket kesesi",16,18,330,140,new Color(.3f,.4f,.3f),OpenBag);
        var icon=Art(bag.transform,"Kese",MahalleGraphic.Shape.Bag,21,36,65,70,Gold);icon.accent=Cream;
        Text(bag.transform,"KESEM",105,24,197,53,34,Cream);Text(bag.transform,"Özel misket seç",105,78,206,40,23,new Color(.8f,.83f,.73f));
        powerLabel=Text(dock.transform,"NORMAL MİSKET",378,22,610,53,29,Cream);
        Text(dock.transform,"ATIŞ GÜCÜ",378,79,610,34,21,new Color(.8f,.83f,.73f));
        Panel(dock.transform,"Güç boş",378,126,610,16,new Color(.33f,.42f,.35f));powerFill=Panel(dock.transform,"Güç dolu",378,126,1,16,Gold);
        hintLabel=Text(page,"",48,0,984,62,29,Cream,TextAlignmentOptions.Center);Bottom(hintLabel.rectTransform,48,220,984,62);
        if(!MahalleProfile.Data.tutorialDone)
        {
            var tutorial=Panel(page,"İlk atış",110,0,860,125,new Color(.16f,.25f,.23f,.94f));Bottom(tutorial.rectTransform,110,302,860,125);
            Text(tutorial.transform,"Geri çek, nişan al, bırak",20,12,820,52,36,Cream,TextAlignmentOptions.Center);
            Text(tutorial.transform,"Atıcıyı taşımak için alt çizgide bir yere dokun.",20,70,820,40,25,new Color(.81f,.84f,.75f),TextAlignmentOptions.Center);
            var g=Art(page,"Öğreten el",MahalleGraphic.Shape.Hand,0,0,65,88,Cream);hand=g.rectTransform;
        }
    }
    private void Update()
    {
        if(controller==null || scoreLabel==null)return;
        if(lastScore!=controller.Score){lastScore=controller.Score;scoreLabel.SetText(controller.Score+" / "+controller.TotalMarbles);}
        if(lastShots!=controller.ShotsLeft){lastShots=controller.ShotsLeft;shotsLabel.SetText(controller.ShotsLeft.ToString());}
        beadsLabel.SetText(MahalleProfile.Data.beads.ToString());
        var shooter=controller.Shooter;
        if(shooter!=null)
        {
            powerFill.rectTransform.sizeDelta=new Vector2(Mathf.Max(1,610*shooter.Power),16);
            powerLabel.SetText(shooter.SelectedPower==MarblePower.None?(SpecialMarbles.IsSpecial(shooter.ActiveSkin)?Campaign.SkinNames[shooter.ActiveSkin]+" · "+MahalleProfile.RemainingLife(shooter.ActiveSkin)+"/150":"NORMAL MİSKET"):Campaign.PowerNames[(int)shooter.SelectedPower].ToUpper(new System.Globalization.CultureInfo("tr-TR")));
            hintLabel.SetText(controller.WaitingForSettle?"Misketler duruluyor…":shooter.IsAiming?"Gücü ayarla ve bırak":shooter.PositionLocked?"Misketin durduğu yerden atıyorsun.":"Çizgiye dokunarak atıcının yerini değiştirebilirsin.");
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
        if(controller.WaitingForSettle||controller.State!=LevelController.LevelState.Playing){Toast("Atışın tamamlanmasını bekle.");return;}
        controller.SetPaused(true);var box=Modal("Misket kesen",1130);
        Text(box,"Misket kesen",36,28,810,65,49,Ink);
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
            string cost=MahalleProfile.Data.stock[i]>0?"ÜCRETSİZ HAK: "+MahalleProfile.Data.stock[i]:Campaign.PowerPrices[i]+" BONCUK / ATIŞ";
            Text(b.transform,selected?"SEÇİLİ · DOKUNARAK VAZGEÇ":cost,145,119,740,33,24,selected?Gold:usable?Ink:Muted);
        }
        LabelButton(box,"OYUNA DÖN",30,1009,924,87,Ink,Cream,()=>CloseModal(),30);
    }
    private void Pause()
    {
        controller.SetPaused(true);var box=Modal("Mola",680);
        Text(box,"Bir nefes al",36,34,912,81,57,Ink,TextAlignmentOptions.Center);
        LabelButton(box,"DEVAM ET",36,164,912,100,Ink,Cream,()=>CloseModal());
        LabelButton(box,"TEKRAR DENE",36,290,912,100,new Color(.88f,.83f,.69f),Ink,()=>{CloseModal();controller.RestartLevel();ShowGame();});
        LabelButton(box,"MAHALLEYE DÖN",36,416,912,100,new Color(.88f,.83f,.69f),Ink,()=>controller.OpenLevelSelect());
        LabelButton(box,"SES VE TİTREŞİM",36,554,912,78,Paper,Muted,Settings,27);
    }
    private void Results()
    {
        bool won=controller.State==LevelController.LevelState.Won;
        int need=MahalleProfile.Required(controller.LevelIndex);
        bool passed=won&&controller.Stars>=need;
        var box=Modal("Bölüm sonucu",1010);
        Text(box,won?(controller.LastReward.newBadge||(controller.Level.mastery&&passed)?"MAHALLE USTASI!":"GÜZEL ATIŞLAR!"):"BİR DAHA DENE",30,34,924,77,51,Ink,TextAlignmentOptions.Center);
        Text(box,controller.Level.levelName,30,114,924,47,31,Muted,TextAlignmentOptions.Center);
        for(int i=0;i<3;i++){var star=Art(box,"Sonuç yıldızı "+i,MahalleGraphic.Shape.Star,259+i*163,i==1?184:201,i==1?142:115,i==1?142:115,Line);if(i<controller.Stars&&won)StartCoroutine(RevealStar(star,i));}
        Text(box,controller.Score+" / "+controller.TotalMarbles+" misket çıkardın",30,366,924,64,40,Ink,TextAlignmentOptions.Center);
        string reward=won?"+"+controller.LastReward.beads+" BONCUK":"Kesendeki güçler yardımcı olabilir. Normal misketle de geçebilirsin.";
        Text(box,reward,60,448,864,82,won?38:28,won?Ink:Muted,TextAlignmentOptions.Center);
        if(controller.LastReward.newBadge)Text(box,"MAHALLE TAMAMLANDI · "+controller.LastReward.districtBonus+" BONCUK BONUS\n"+(controller.LastReward.newSkin??"Mahalle misketi zaten kesende")+" · KOLEKSİYON ÖDÜLÜ",30,538,924,100,28,Muted,TextAlignmentOptions.Center);
        else if(won&&!passed)Text(box,"Sonraki bölümü açmak için "+need+" yıldız gerekiyor.",60,552,864,72,29,new Color(.72f,.32f,.24f),TextAlignmentOptions.Center);
        else Text(box,won?"Yeni rekorlar ve görevler daha fazla boncuk kazandırır.":"İpucu: Alt çizgide yer değiştirip kümeye yandan vur.",60,556,864,68,27,Muted,TextAlignmentOptions.Center);
        if(passed&&controller.HasNextLevel)LabelButton(box,"SONRAKİ BÖLÜM",36,669,912,98,Ink,Cream,()=>controller.LoadNextLevel());
        else LabelButton(box,passed?"MAHALLEYE DÖN":"TEKRAR DENE",36,669,912,98,Ink,Cream,()=>{if(passed)controller.OpenLevelSelect();else{CloseModal();controller.RestartLevel();ShowGame();}});
        LabelButton(box,passed?"REKORUNU GELİŞTİR":"MAHALLEYE DÖN",36,791,912,86,new Color(.88f,.83f,.69f),Ink,()=>{if(passed){CloseModal();controller.RestartLevel();ShowGame();}else controller.OpenLevelSelect();},30);
        if(passed&&controller.HasNextLevel)LabelButton(box,"MAHALLE HARİTASI",36,898,912,76,Paper,Muted,()=>controller.OpenLevelSelect(),27);
    }
    private IEnumerator RevealStar(MahalleGraphic star,int index)
    {
        yield return new WaitForSecondsRealtime(.25f+index*.24f);
        if(star==null)yield break;star.color=Gold;float t=0;
        while(t<.24f&&star!=null){t+=Time.unscaledDeltaTime;star.transform.localScale=Vector3.one*(1+Mathf.Sin(Mathf.Clamp01(t/.24f)*Mathf.PI)*.22f);yield return null;}
    }
    private void Settings()
    {
        var box=Modal("Ayarlar",750);Text(box,"Ayarlar",36,30,912,77,52,Ink);
        LabelButton(box,"SES: "+(MahalleProfile.Data.sound?"AÇIK":"KAPALI"),36,158,912,100,Ink,Cream,()=>{MahalleProfile.Data.sound=!MahalleProfile.Data.sound;MahalleProfile.Save();Settings();});
        LabelButton(box,"TİTREŞİM: "+(MahalleProfile.Data.haptics?"AÇIK":"KAPALI"),36,282,912,100,Ink,Cream,()=>{MahalleProfile.Data.haptics=!MahalleProfile.Data.haptics;MahalleProfile.Save();Settings();});
        LabelButton(box,"İLERLEMEYİ SIFIRLA",36,415,912,89,new Color(.88f,.83f,.69f),Ink,ResetPrompt,29);
        Text(box,"Boncuklar oyun içinden kazanılır. Gerçek para işlemi yoktur.",36,527,912,64,25,Muted,TextAlignmentOptions.Center);
        LabelButton(box,"GERİ",36,625,912,87,Ink,Cream,()=>{if(controller!=null)Pause();else CloseModal();},29);
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
