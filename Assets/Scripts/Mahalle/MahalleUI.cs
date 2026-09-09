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
        district=MahalleProfile.NextLevel/Campaign.PerDistrict;
        if(controller==null)ShowHome();else ShowGame();
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
        Text(page,"MİSKETR",48,24,630,76,64,Ink);
        Text(page,subtitle,50,104,740,42,27,Muted);
        var wallet=Panel(page,"Boncuk",805,35,225,76,Ink);
        Art(wallet.transform,"Boncuk simgesi",MahalleGraphic.Shape.Marble,18,16,44,44,Gold);
        beadsLabel=Text(wallet.transform,MahalleProfile.Data.beads.ToString(),78,0,130,76,36,Cream);
    }
    private void Nav()
    {
        var bg=Panel(page,"Alt menü",24,0,1032,112,Ink);Bottom(bg.rectTransform,24,18,1032,112);
        string[] names={"MAHALLE","KESEM","GÖREVLER"};
        for(int i=0;i<3;i++){int id=i;var b=LabelButton(bg.transform,names[i],i*344+8,8,328,96,i==tab?new Color(.25f,.37f,.32f):Ink,i==tab?Gold:Cream,()=>{tab=id;ShowHome();},29);}
    }
    private void ShowHome()
    {
        ClearPage();Background(Paper);Header(tab==0?"MAHALLENİN MİSKET USTASI OL":tab==1?"HER MİSKETİN BİR HİKÂYESİ VAR":"KÜÇÜK HEDEFLER, YENİ BONCUKLAR");
        if(tab==0)Map();else if(tab==1)Collection();else Missions();Nav();
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
    private void Map()
    {
        var content=HomeScroll(1510);
        for(int i=0;i<5;i++)
        {
            int d=i;bool unlocked=MahalleProfile.Unlocked(i*12);
            var b=Button(content,"Mahalle "+i,48+i*198,185,186,84,district==i?Ink:new Color(.88f,.85f,.76f),()=>{district=d;ShowHome();});
            Text(b.transform,Campaign.Districts[i],10,7,166,70,25,district==i?Cream:Muted,TextAlignmentOptions.Center);
            if(!unlocked)Art(b.transform,"Kilit",MahalleGraphic.Shape.Lock,156,5,20,25,district==i?Gold:Muted);
        }
        var hero=Panel(content,"Mahalle kartı",48,297,984,167,new Color(.87f,.82f,.68f));
        var house=Art(hero.transform,"Mahalle evi",MahalleGraphic.Shape.House,32,22,117,123,Ink);house.accent=Gold;
        Text(hero.transform,Campaign.Districts[district],182,17,744,59,45,Ink);
        Text(hero.transform,Campaign.Descriptions[district],184,81,720,60,28,Muted);
        int earned=0;for(int i=district*12;i<(district+1)*12;i++)earned+=MahalleProfile.Data.stars[i];
        Text(content,"12 BÖLÜM  /  "+earned+" · 36 YILDIZ",52,479,950,40,24,Muted);
        for(int row=0;row<4;row++)
        {
            for(int col=0;col<3;col++)
            {
                int local=row*3+col,index=district*12+local;var level=Campaign.Database.Get(index);
                bool unlocked=MahalleProfile.Unlocked(index),current=index==MahalleProfile.NextLevel;
                Color bg=current?Ink:unlocked?Cream:new Color(.89f,.86f,.78f);
                float x=48+col*334,y=540+row*280;
                var card=Button(content,"Bölüm "+(index+1),x,y,316,258,bg,()=>{if(unlocked)OpenLevel(index);else Toast("Önce bir önceki bölümü tamamla.");});
                Text(card.transform,(local+1).ToString("00"),22,12,110,66,48,current?Gold:unlocked?Ink:Muted);
                var mini=Art(card.transform,"Arena önizlemesi",level.shape==ArenaShape.Triangle?MahalleGraphic.Shape.Triangle:MahalleGraphic.Shape.Ring,204,24,88,88,current?Cream:Muted);mini.accent=current?Gold:new Color(.12f,.58f,.55f);
                Text(card.transform,level.mastery?"USTALIK SINAVI":level.levelName,24,128,268,64,30,current?Cream:Muted);
                if(unlocked)
                {
                    for(int s=0;s<3;s++)Art(card.transform,"Yıldız "+s,MahalleGraphic.Shape.Star,24+s*34,217,25,25,MahalleProfile.Data.stars[index]>s?Gold:current?new Color(.4f,.49f,.41f):Line);
                    if(current)Text(card.transform,"OYNA",170,207,115,37,23,Gold,TextAlignmentOptions.MidlineRight);
                }
                else Art(card.transform,"Kilitli",MahalleGraphic.Shape.Lock,26,215,25,28,Muted);
            }
        }
        var play=LabelButton(page,"KALDIĞIN YERDEN DEVAM ET",48,0,984,100,Ink,Cream,()=>OpenLevel(MahalleProfile.NextLevel),34);
        Bottom((RectTransform)play.transform,48,160,984,100);
    }
    private void Collection()
    {
        var content=HomeScroll(1240);
        var title=Text(content,"Misket koleksiyonun",48,182,984,65,47,Ink);
        Text(content,"Görünümünü seç. Özel atış güçleri oyun içindeki kesende.",48,256,984,76,30,Muted);
        for(int i=0;i<6;i++)
        {
            int skin=i;bool owned=MahalleProfile.Data.skins[i],selected=MahalleProfile.Data.selectedSkin==i;
            float x=48+(i%2)*508,y=363+(i/2)*302;
            var card=Panel(content,Campaign.SkinNames[i],x,y,476,278,selected?Ink:Cream);
            var marble=Art(card.transform,"Cam misket",MahalleGraphic.Shape.Marble,24,24,150,150,Campaign.SkinColors[i]);marble.accent=Color.Lerp(Campaign.SkinColors[(i+2)%6],Color.white,.4f);
            Text(card.transform,Campaign.SkinNames[i],193,40,265,96,33,selected?Cream:Ink);
            string label=selected?"KUŞANILDI":owned?"KUŞAN":Campaign.SkinPrices[i]+" BONCUK · AÇ";
            var b=LabelButton(card.transform,label,24,194,428,62,selected?new Color(.27f,.39f,.31f):new Color(.89f,.85f,.72f),selected?Gold:Ink,()=>{if(MahalleProfile.EquipOrBuy(skin))ShowHome();else Toast("Yeterli boncuk yok. Bölümlerden ve görevlerden kazanabilirsin.");},25);
        }
        Text(content,"Ustalık sınavları da yeni misket görünümleri kazandırır.",48,1304,984,80,29,Muted,TextAlignmentOptions.Center);
        var settings=LabelButton(page,"AYARLAR",48,0,984,86,new Color(.88f,.85f,.76f),Ink,Settings,28);Bottom((RectTransform)settings.transform,48,160,984,86);
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
        {bool won=MahalleProfile.Data.stars[i*12+11]>0;var star=Art(content,"Ustalık rozeti",MahalleGraphic.Shape.Star,75+i*198,1280,116,116,won?Gold:Line);Text(content,Campaign.Districts[i],48+i*198,1410,186,70,24,won?Ink:Muted,TextAlignmentOptions.Center);}
    }
    private void OpenLevel(int index)
    {if(!MahalleProfile.Unlocked(index))return;GameSession.SelectedLevelIndex=index;Time.timeScale=1;SceneManager.LoadScene(GameSession.GameSceneName);}
    private void ShowGame()
    {
        ClearPage();lastScore=lastShots=-1;resultShown=false;
        var top=Panel(page,"Oyun başlığı",24,12,1032,246,Ink);top.radius=30;
        string title=Campaign.Districts[controller.Level.district]+"  /  "+(controller.LevelIndex%12+1).ToString("00");
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
            powerLabel.SetText(shooter.SelectedPower==MarblePower.None?"NORMAL MİSKET":Campaign.PowerNames[(int)shooter.SelectedPower].ToUpper(new System.Globalization.CultureInfo("tr-TR")));
            hintLabel.SetText(controller.WaitingForSettle?"Misketler duruluyor…":shooter.IsAiming?"Gücü ayarla ve bırak":"Çizgiye dokunarak atıcının yerini değiştirebilirsin.");
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
        controller.SetPaused(true);var box=Modal("Misket kesen",940);
        Text(box,"Misket kesen",36,28,810,65,49,Ink);
        Text(box,"Seçim ücretsiz. Bir kullanım yalnızca atışta harcanır.",36,108,912,74,28,Muted);
        for(int i=0;i<3;i++)
        {
            int power=i;bool usable=MahalleProfile.CanUse((MarblePower)i),selected=controller.Shooter.SelectedPower==(MarblePower)i;
            var b=Button(box,Campaign.PowerNames[i],30,215+i*190,924,166,selected?Ink:Cream,()=>{
                if(!usable){Toast("Boncuk kazanmak için normal misketle oynayabilirsin.");return;}
                CloseModal();controller.Shooter.SelectPower((MarblePower)power);
            });
            var m=Art(b.transform,"Özel misket",MahalleGraphic.Shape.Marble,22,30,i==0?100:82,i==0?100:82,i==1?Campaign.SkinColors[5]:i==2?Campaign.SkinColors[1]:Gold);
            Text(b.transform,Campaign.PowerNames[i],145,17,730,47,36,selected?Cream:Ink);
            Text(b.transform,Campaign.PowerDescriptions[i],145,64,740,52,25,selected?Cream:Muted);
            string cost=MahalleProfile.Data.stock[i]>0?"ÜCRETSİZ HAK: "+MahalleProfile.Data.stock[i]:Campaign.PowerPrices[i]+" BONCUK / ATIŞ";
            Text(b.transform,selected?"SEÇİLİ · DOKUNARAK VAZGEÇ":cost,145,119,740,33,24,selected?Gold:usable?Ink:Muted);
        }
        LabelButton(box,"OYUNA DÖN",30,819,924,87,Ink,Cream,()=>CloseModal(),30);
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
        bool won=controller.State==LevelController.LevelState.Won;var box=Modal("Bölüm sonucu",1010);
        Text(box,won?controller.Level.mastery?"MAHALLE USTASI!":"GÜZEL ATIŞLAR!":"BİR DAHA DENE",30,34,924,77,51,Ink,TextAlignmentOptions.Center);
        Text(box,controller.Level.levelName,30,114,924,47,31,Muted,TextAlignmentOptions.Center);
        for(int i=0;i<3;i++){var star=Art(box,"Sonuç yıldızı "+i,MahalleGraphic.Shape.Star,259+i*163,i==1?184:201,i==1?142:115,i==1?142:115,Line);if(i<controller.Stars&&won)StartCoroutine(RevealStar(star,i));}
        Text(box,controller.Score+" / "+controller.TotalMarbles+" misket çıkardın",30,366,924,64,40,Ink,TextAlignmentOptions.Center);
        string reward=won?"+"+controller.LastReward.beads+" BONCUK":"Kesendeki güçler yardımcı olabilir. Normal misketle de geçebilirsin.";
        Text(box,reward,60,448,864,82,won?38:28,won?Ink:Muted,TextAlignmentOptions.Center);
        if(controller.LastReward.newBadge)Text(box,Campaign.Districts[controller.Level.district]+" Ustası\n"+(controller.LastReward.newSkin??"Koleksiyonun parlıyor!")+" · KOLEKSİYON ÖDÜLÜ",30,538,924,100,28,Muted,TextAlignmentOptions.Center);
        else Text(box,won?"Yeni rekorlar ve görevler daha fazla boncuk kazandırır.":"İpucu: Alt çizgide yer değiştirip kümeye yandan vur.",60,556,864,68,27,Muted,TextAlignmentOptions.Center);
        if(won&&controller.HasNextLevel)LabelButton(box,"SONRAKİ BÖLÜM",36,669,912,98,Ink,Cream,()=>controller.LoadNextLevel());
        else LabelButton(box,won?"MAHALLEYE DÖN":"TEKRAR DENE",36,669,912,98,Ink,Cream,()=>{if(won)controller.OpenLevelSelect();else{CloseModal();controller.RestartLevel();ShowGame();}});
        LabelButton(box,won?"REKORUNU GELİŞTİR":"MAHALLEYE DÖN",36,791,912,86,new Color(.88f,.83f,.69f),Ink,()=>{if(won){CloseModal();controller.RestartLevel();ShowGame();}else controller.OpenLevelSelect();},30);
        if(won&&controller.HasNextLevel)LabelButton(box,"MAHALLE HARİTASI",36,898,912,76,Paper,Muted,()=>controller.OpenLevelSelect(),27);
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
