using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MahalleVerify
{
    private static int checks;
    private static void Check(bool ok,string message) {checks++;if(!ok)throw new Exception("CHECK FAILED: "+message);}
    [MenuItem("MISKETR/Verify Mahalle Systems")]
    public static void Run()
    {
        checks=0;
        // Preview modu acikken butun bolumler kilitsiz gorunur; test o yuzden
        // "yeni oyuncuda sadece 1. bolum acik" kontrolunde patlar. Test suresince
        // askiya al, sonunda kullanicinin biraktigi gibi geri ac.
        bool wasPreview=MahalleProfile.PreviewMode;
        if(wasPreview)MahalleProfile.EndPreview();
        MahalleProfile.TestMode=true;
        try
        {
            var data=new MahalleSave();MahalleProfile.SetTestData(data);
            Check(Campaign.Database.Count==60,"All five districts contain 12 levels");
            for(int i=0;i<60;i++)
            {
                var l=Campaign.Database.Get(i);int total=l.TotalMarbles();
                Check(l.oneStarTarget>0&&l.oneStarTarget<=l.twoStarTarget&&l.twoStarTarget<=l.threeStarTarget&&l.threeStarTarget<=total,"Reachable star targets "+i);
                Check(l.mastery==(i%12==11)&&l.district==i/12,"District boundaries "+i);
                Check(l.shotCount>0,"Playable setup "+i);
                Check(l.starsToPass>=1&&l.starsToPass<=3&&l.starsToPass<=3,"Reachable pass gate "+i);
                Check(l.obstacles==null||l.obstacles.Length<=3,"Obstacle budget "+i);
            }
            Check(MahalleProfile.Unlocked(0)&&!MahalleProfile.Unlocked(1),"New profile only unlocks first level");
            var reward=MahalleProfile.Finish(0,2,4);
            Check(reward.beads==12&&data.beads==52&&MahalleProfile.Unlocked(1),"First win rewards and unlocks atomically");
            reward=MahalleProfile.Finish(0,1,3);
            Check(reward.beads==0&&data.stars[0]==2,"Replay cannot lower record or repeat first win reward");
            reward=MahalleProfile.Finish(0,3,6);
            Check(reward.beads==3&&data.stars[0]==3,"Only incremental stars are rewarded");
            int wallet=data.beads;Check(MahalleProfile.Consume(MarblePower.Big)&&data.stock[0]==0&&data.beads==wallet,"Free trial precedes spending");
            Check(MahalleProfile.Consume(MarblePower.Big)&&data.beads==wallet-16,"Power charged once");
            data.beads=0;Check(!MahalleProfile.Consume(MarblePower.Big)&&data.beads==0,"Insufficient funds never go negative");
            Check(MahalleProfile.Consume(MarblePower.None)&&data.beads==0,"Normal shot always available");
            data.beads=100;Check(MahalleProfile.EquipOrBuy(1)&&data.beads==60&&data.selectedSkin==1,"Skin purchase equips");
            Check(MahalleProfile.EquipOrBuy(1)&&data.beads==60,"Owned skin never charged twice");
            Check(!MahalleProfile.EquipOrBuy(5)&&data.beads==60,"Unaffordable skin refused");
            data.wins=10;Check(MahalleProfile.Claim(0)&&data.beads==60+MahalleProfile.MissionRewards[0],"Completed mission reward");
            Check(!MahalleProfile.Claim(0)&&data.beads==60+MahalleProfile.MissionRewards[0],"Mission cannot be claimed twice");
            reward=MahalleProfile.Finish(11,1,3);Check(!reward.newBadge&&reward.districtBonus==0,"Incomplete final never awards district bonus");
            reward=MahalleProfile.Finish(11,1,3);Check(!reward.newBadge&&reward.beads==0,"Mastery bonus paid once");
            var roundTrip=JsonUtility.FromJson<MahalleSave>(JsonUtility.ToJson(data));
            Check(roundTrip.beads==data.beads&&roundTrip.stars[11]==1&&roundTrip.selectedSkin==1&&roundTrip.claimed[0],"Save round trip retains economy, progression and collection");
            checks += SpecialMarblesVerify.RunChecks();
            checks += DistrictRewardVerify.RunChecks();
            checks += CameraFitVerify.RunChecks();
            checks += ShooterTapVerify.RunChecks();
            checks += MapLayoutVerify.RunChecks();
            checks += MapsVerify.RunChecks();
            checks += IceMarbleVerify.RunChecks();
            checks += SplitMarbleVerify.RunChecks();
            checks += GroundRulesVerify.RunChecks();
            checks += MemleketLayoutVerify.RunChecks();
            // GÖRÜNÜM TUTARLILIĞI: bir mahalledeki bütün bölümler aynı zemin ve
            // aynı kamera kuralıyla kurulmalı; bölüme özel ortam denemesi kalmamalı.
            for(int d=0;d<5;d++)
            {
                var ilk=Campaign.Database.Get(d*12);
                for(int local=1;local<12;local++)
                {
                    var lv=Campaign.Database.Get(d*12+local);
                    Check(lv.district==ilk.district,"Same district for every level in area "+d);
                    Check(MahalleTheme.Get(lv.district).title==MahalleTheme.Get(ilk.district).title,"Same theme across the area "+d);
                }
            }
            // KAYIT GÜVENLİĞİ: bozuk kayıt yedekten dönmeli.
            {
                MahalleProfile.TestMode=false;
                var once=new MahalleSave{beads=123};
                MahalleProfile.SetTestData(once);MahalleProfile.Save();          // 1. kayıt
                MahalleProfile.Data.beads=456;MahalleProfile.Save();             // 2. kayıt, 1. yedeğe gitti
                Check(MahalleProfile.SlotValid(MahalleProfile.SaveKey,MahalleProfile.SumKey),"Save slot valid after write");
                Check(MahalleProfile.SlotValid(MahalleProfile.BackupKey,MahalleProfile.BackupSumKey),"Backup slot written");
                PlayerPrefs.SetString(MahalleProfile.SaveKey,"{bozuk");            // kayıt bozuldu
                Check(!MahalleProfile.SlotValid(MahalleProfile.SaveKey,MahalleProfile.SumKey),"Corrupt save detected");
                MahalleProfile.Reload();
                Check(MahalleProfile.Data.beads==123,"Corrupt save falls back to backup");
                PlayerPrefs.DeleteKey(MahalleProfile.SaveKey);PlayerPrefs.DeleteKey(MahalleProfile.SumKey);
                PlayerPrefs.DeleteKey(MahalleProfile.BackupKey);PlayerPrefs.DeleteKey(MahalleProfile.BackupSumKey);
                MahalleProfile.Reload();MahalleProfile.TestMode=true;MahalleProfile.SetTestData(data);
            }
            // SONSUZ ÇEMBER: süre yarışı. Kademe ilerledikçe engel gelmeli, misketler
            // çemberin içinde ve birbirinden ayrık kalmalı, dolum yeri bulabilmeli.
            {
                Check(EndlessLevel.StageAt(0f)==1&&EndlessLevel.StageAt(EndlessLevel.StageEvery*3+1f)==4,"Endless stage rises with time");
                Check(EndlessLevel.StageAt(9999f)==EndlessLevel.MaxStage,"Endless stage is capped");
                Check(EndlessLevel.WallsFor(1)==0&&EndlessLevel.WallsFor(EndlessLevel.MaxStage)<=3,"Endless obstacles grow within limits");
                Check(EndlessLevel.TimePerMarble>0f&&EndlessLevel.StartTime>=30f,"Endless time rules sane");
                for(int st=1;st<=EndlessLevel.MaxStage;st++)
                {
                    var lv=EndlessLevel.Build(st);
                    Check(lv.marbles.Length==EndlessLevel.BoardCount,"Endless board is full at stage "+st);
                    foreach(var m in lv.marbles)
                        Check(new Vector2(m.x,m.z).magnitude<=lv.arenaSize-.4f,"Endless marbles inside ring "+st);
                    for(int a=0;a<lv.marbles.Length;a++)
                        for(int b=a+1;b<lv.marbles.Length;b++)
                            Check(Vector2.Distance(new Vector2(lv.marbles[a].x,lv.marbles[a].z),
                                                   new Vector2(lv.marbles[b].x,lv.marbles[b].z))>.6f,"Endless marbles never overlap "+st);
                    var dolum=EndlessLevel.Spots(st,8,null,st*13);
                    Check(dolum.Length==8,"Endless refill finds room at stage "+st);
                }
                var s1=EndlessLevel.Build(3);var s2=EndlessLevel.Build(3);
                Check(s1.marbles.Length==s2.marbles.Length&&Mathf.Approximately(s1.marbles[0].x,s2.marbles[0].x),"Endless stage is the same for everyone");
            }
            // USTA SEVİYESİ ve BAŞARIMLAR: puan artışı seviyeye dönüşüyor mu, ilerleme hedefi aşıyor mu.
            {
                var fresh=new MahalleSave();MahalleProfile.SetTestData(fresh);
                Check(MahalleProfile.MasteryLevel==1&&MahalleProfile.MasteryIntoLevel==0,"Mastery starts at level 1");
                fresh.knocked=MahalleProfile.MasteryPerLevel;
                Check(MahalleProfile.MasteryLevel==2&&MahalleProfile.MasteryIntoLevel==0,"Mastery level rises with points");
                for(int i=0;i<MahalleProfile.AchievementNames.Length;i++)
                {
                    Check(MahalleProfile.AchievementTargets[i]>0,"Achievement target positive "+i);
                    Check(MahalleProfile.AchievementProgress(i)>=0,"Achievement progress never negative "+i);
                    Check(MahalleProfile.AchievementNotes[i].Length>0,"Achievement has a description "+i);
                }
                fresh.bestShot=MahalleProfile.AchievementTargets[2];
                Check(MahalleProfile.AchievementDone(2),"Achievement completes at its target");
                MahalleProfile.SetTestData(data);
            }
            // GÜNÜN BÖLÜMÜ havuzu: varsa her girdi oynanabilir ve hedefleri tutarlı olmalı.
            if(DailyLevel.Available)
            {
                var pool=DailyLevel.Pool;
                foreach(var e in pool.entries)
                {
                    var dl=DailyLevel.Build(e);int n=dl.TotalMarbles();
                    Check(n>=4&&e.shots>0,"Daily playable setup");
                    Check(e.one>0&&e.one<=e.two&&e.two<=e.three&&e.three<=e.ceiling&&e.ceiling<=n,"Daily targets within measured ceiling");
                    Check(e.size>=2.4f&&e.size<=3.8f,"Daily arena size");
                }
                Check(DailyLevel.Today()!=null,"Daily level for today");
                data.dailyDay=DailyLevel.DayIndex-1;data.dailyStreak=3;int before=data.beads;
                var dr=MahalleProfile.FinishDaily(1,3);
                Check(dr.beads==DailyLevel.RewardFor(4)&&data.dailyStreak==4&&data.beads==before+dr.beads,"Daily streak reward");
                Check(MahalleProfile.FinishDaily(3,5).beads==0,"Daily reward once per day");
                Check(!MahalleProfile.DailyOpen,"Daily closes after a win");
                data.dailyDay=-9999;data.dailyTriesDay=DailyLevel.DayIndex;data.dailyTries=0;
                for(int t=0;t<DailyLevel.MaxTries;t++)
                {
                    Check(MahalleProfile.DailyTriesLeft==DailyLevel.MaxTries-t,"Daily tries counted down");
                    Check(MahalleProfile.DailyUseTry(),"Daily try allowed while open");
                }
                Check(MahalleProfile.DailyTriesLeft==0&&!MahalleProfile.DailyOpen&&!MahalleProfile.DailyUseTry(),"Daily closes after three tries");
                data.dailyDay=DailyLevel.DayIndex-3;var gap=MahalleProfile.FinishDaily(1,2);
                Check(gap.beads==DailyLevel.RewardFor(1)&&data.dailyStreak==1,"Daily streak resets after a missed day");
            }
            else Debug.LogWarning("GUNLUK: DailyPool.json yok, gunun bolumu kapali.");
            Check(Resources.Load<TMPro.TMP_FontAsset>("Mahalle/Nunito SDF")!=null,"Turkish font included");
            Check(Resources.Load<Shader>("Mahalle/Marble")!=null,"Marble shader included");
            foreach(var scene in new[]{"Assets/Scenes/LevelSelect.unity","Assets/Scenes/Game.unity"})Check(File.Exists(scene),"Scene exists "+scene);
            var game=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
            Check(UnityEngine.Object.FindFirstObjectByType<LevelController>()!=null,"Existing game controller retained");
            Check(UnityEngine.Object.FindFirstObjectByType<ShotController>()!=null,"Existing shooter retained");
            Check(UnityEngine.Object.FindFirstObjectByType<MarbleArena>()!=null,"Existing arena retained");
            EditorSceneManager.OpenScene("Assets/Scenes/LevelSelect.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("MAHALLE_VERIFY_OK: "+checks+" checks passed.");
            if(MahalleProfile.TestUnlockAllLevels)
                Debug.LogWarning("TEST YAPISI ACIK: MahalleProfile.TestUnlockAllLevels = true. "+
                                 "Butun bolumler kilitsiz ve ekranda TEST damgasi var. Yayindan once false yap.");
            if(MahalleProfile.TestInfiniteBeads)
                Debug.LogWarning("TEST YAPISI ACIK: MahalleProfile.TestInfiniteBeads = true. "+
                                 "Kese hep dolu gorunuyor ve harcamalar dusmuyor. YAYINDAN ONCE false YAPILACAK.");
        }
        finally{MahalleProfile.TestMode=false;MahalleProfile.Reload();if(wasPreview)MahalleProfile.BeginPreview();}
    }
}
