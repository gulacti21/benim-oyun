using System;
using UnityEngine;

public static class DistrictRewardVerify
{
    public static int RunChecks()
    {
        var previous = MahalleProfile.Data;
        bool wasTest = MahalleProfile.TestMode;
        int count=0;
        void Check(bool ok,string text) { count++; if(!ok)throw new Exception("CHECK FAILED: "+text); }
        MahalleProfile.TestMode=true;
        try
        {
            for(int d=0;d<Campaign.Districts.Length;d++)
            {
                var data=new MahalleSave(); MahalleProfile.SetTestData(data);
                int first=d*12,final=first+11;
                for(int i=first;i<final;i++) data.stars[i]=MahalleProfile.Required(i);
                var r=MahalleProfile.Finish(final,1,1);
                Check(!r.newBadge&&r.districtBonus==0&&!data.districtRewards[d]&&!data.skins[d+1],"One-star final no milestone "+d);
                r=MahalleProfile.Finish(final,2,Campaign.Database.Get(final).twoStarTarget);
                Check(r.newBadge&&r.districtBonus==35&&r.beads==38&&data.districtRewards[d]&&data.skins[d+1],"Two-star improvement grants milestone once "+d);
                int wallet=data.beads;
                r=MahalleProfile.Finish(final,2,1);
                Check(!r.newBadge&&r.districtBonus==0&&data.beads==wallet,"Replay pays nothing without a new star "+d);
                var saved=JsonUtility.FromJson<MahalleSave>(JsonUtility.ToJson(data));MahalleProfile.SetTestData(saved);
                r=MahalleProfile.Finish(final,3,1);
                Check(!r.newBadge&&r.districtBonus==0&&r.beads==3&&saved.districtRewards[d],"Reload and star upgrade cannot repeat milestone "+d);
            }
            var missing=new MahalleSave();MahalleProfile.SetTestData(missing);
            for(int i=24;i<36;i++)missing.stars[i]=MahalleProfile.Required(i);
            missing.stars[27]=0;missing.beads=2000;
            Check(MahalleProfile.CanBuySkin(6)&&missing.beads==2000,"Special marbles have no district purchase gate");
            var reward=MahalleProfile.Finish(35,2,1);
            Check(!reward.newBadge,"Skipping a level cannot collect district bonus");
            reward=MahalleProfile.Finish(27,1,1);
            Check(reward.newBadge&&reward.districtBonus==35&&MahalleProfile.CanBuySkin(6),"Completing last missing level pays once");
            var legacy=new MahalleSave { version=1, districtRewards=null, beads=321 };
            legacy.stars[11]=1;legacy.skins[1]=true;
            MahalleProfile.SetTestData(legacy);
            Check(legacy.version==3&&legacy.districtRewards[0]&&legacy.beads==321&&legacy.skins[1],"Legacy rewards migrate without charging or losing items");
            for(int i=0;i<11;i++)legacy.stars[i]=MahalleProfile.Required(i);
            reward=MahalleProfile.Finish(11,2,1);
            Check(!reward.newBadge&&reward.districtBonus==0,"Legacy final payment never duplicated");
            var run=new MahalleSave();MahalleProfile.SetTestData(run);
            int bonus=0;
            for(int i=0;i<36;i++) bonus+=MahalleProfile.Finish(i,MahalleProfile.Required(i),1).districtBonus;
            Check(run.beads==478&&bonus==105,"Minimum first-three-district income is 478 before spending and missions");
            return count;
        }
        finally { MahalleProfile.SetTestData(previous);MahalleProfile.TestMode=wasTest; }
    }
}
