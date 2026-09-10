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
            Check(reward.beads==30&&data.beads==90&&MahalleProfile.Unlocked(1),"First win rewards and unlocks atomically");
            reward=MahalleProfile.Finish(0,1,3);
            Check(reward.beads==3&&data.stars[0]==2,"Replay cannot lower record or repeat first win reward");
            reward=MahalleProfile.Finish(0,3,6);
            Check(reward.beads==8&&data.stars[0]==3,"Only incremental stars are rewarded");
            int wallet=data.beads;Check(MahalleProfile.Consume(MarblePower.Big)&&data.stock[0]==0&&data.beads==wallet,"Free trial precedes spending");
            Check(MahalleProfile.Consume(MarblePower.Big)&&data.beads==wallet-12,"Power charged once");
            data.beads=0;Check(!MahalleProfile.Consume(MarblePower.Big)&&data.beads==0,"Insufficient funds never go negative");
            Check(MahalleProfile.Consume(MarblePower.None)&&data.beads==0,"Normal shot always available");
            data.beads=100;Check(MahalleProfile.EquipOrBuy(1)&&data.beads==60&&data.selectedSkin==1,"Skin purchase equips");
            Check(MahalleProfile.EquipOrBuy(1)&&data.beads==60,"Owned skin never charged twice");
            Check(!MahalleProfile.EquipOrBuy(5)&&data.beads==60,"Unaffordable skin refused");
            data.wins=3;Check(MahalleProfile.Claim(0)&&data.beads==90,"Completed mission reward");
            Check(!MahalleProfile.Claim(0)&&data.beads==90,"Mission cannot be claimed twice");
            reward=MahalleProfile.Finish(11,1,3);Check(!reward.newBadge&&reward.districtBonus==0,"Incomplete final never awards district bonus");
            reward=MahalleProfile.Finish(11,1,3);Check(!reward.newBadge&&reward.beads==3,"Mastery bonus paid once");
            var roundTrip=JsonUtility.FromJson<MahalleSave>(JsonUtility.ToJson(data));
            Check(roundTrip.beads==data.beads&&roundTrip.stars[11]==1&&roundTrip.selectedSkin==1&&roundTrip.claimed[0],"Save round trip retains economy, progression and collection");
            checks += SpecialMarblesVerify.RunChecks();
            checks += DistrictRewardVerify.RunChecks();
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
        }
        finally{MahalleProfile.TestMode=false;MahalleProfile.Reload();if(wasPreview)MahalleProfile.BeginPreview();}
    }
}
