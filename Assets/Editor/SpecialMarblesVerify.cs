using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class SpecialMarblesVerify
{
    public static int RunChecks()
    {
        var previous=MahalleProfile.Data;bool wasTest=MahalleProfile.TestMode;int count=0;
        void Check(bool ok,string message){count++;if(!ok)throw new Exception("CHECK FAILED: "+message);}
        MahalleProfile.TestMode=true;
        try
        {
            Check(Campaign.SkinCount==10&&Campaign.SkinPrices.Length==10,"Ten valid marbles");
            for(int skin=6;skin<10;skin++)
            {
                var d=new MahalleSave();MahalleProfile.SetTestData(d);d.beads=399;
                Check(!MahalleProfile.EquipOrBuy(skin)&&d.beads==399,"No underfunded purchase");
                d.beads=400;Check(MahalleProfile.EquipOrBuy(skin)&&d.beads==0&&MahalleProfile.RemainingLife(skin)==150,"400 purchase without district gate");
                Check(!MahalleProfile.RepairMarble(skin),"Full life cannot charge repair");
                foreach(var power in new[]{MarblePower.Big,MarblePower.Iron,MarblePower.Guide,MarblePower.Anchor})MahalleProfile.RecordMarbleShot(skin,power);
                Check(MahalleProfile.RemainingLife(skin)==150,"All powers preserve life");
                for(int i=0;i<149;i++)MahalleProfile.RecordMarbleShot(skin,MarblePower.None);
                Check(MahalleProfile.RemainingLife(skin)==1&&MahalleProfile.EffectiveSkin==skin,"149 actual shots keep last use");
                var ball=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                try
                {
                    var body=ball.AddComponent<Rigidbody>();body.mass=.05f;body.linearDamping=.6f;
                    var shooter=ball.AddComponent<ShotController>();shooter.ResetTo(new Vector3(0,.25f,-4.2f));
                    var physics=ball.GetComponent<SpecialMarblePhysics>();
                    Check(physics.ActiveSkin==skin,"Normal shot applies selected trait");
                    if(skin==6)Check(Mathf.Approximately(body.mass,.0625f)&&Mathf.Approximately(physics.ImpulseMultiplier,1.12f),"Heavy momentum and slower speed");
                    if(skin==7)Check(Mathf.Approximately(ball.transform.localScale.x,.8f),"Thin radius");
                    if(skin==8)Check(Mathf.Approximately(ball.GetComponent<SphereCollider>().sharedMaterial.bounciness,.65f),"Bouncy material");
                    if(skin==9)Check(Mathf.Approximately(body.linearDamping,.48f),"Slippery drag");
                    foreach(var power in new[]{MarblePower.Big,MarblePower.Iron,MarblePower.Guide,MarblePower.Anchor})
                    {
                        physics.Apply(power);
                        Check(physics.ActiveSkin==0&&physics.ImpulseMultiplier==1f&&body.linearDamping==.6f,"No trait stacks with power");
                    }
                    shooter.ResetTo(new Vector3(0,.25f,-4.2f));shooter.CancelAim();
                    Check(MahalleProfile.RemainingLife(skin)==1,"Reset and aim cancellation never consume or refill life");
                    var flags=BindingFlags.NonPublic|BindingFlags.Instance;var type=typeof(ShotController);
                    type.GetField("pendingWearSkin",flags).SetValue(shooter,skin);
                    type.GetField("pendingWearPower",flags).SetValue(shooter,MarblePower.None);
                    type.GetField("hasPendingImpulse",flags).SetValue(shooter,true);
                    type.GetMethod("FixedUpdate",flags).Invoke(shooter,null);
                    type.GetMethod("FixedUpdate",flags).Invoke(shooter,null);
                    Check(MahalleProfile.RemainingLife(skin)==0&&physics.ActiveSkin==skin,"Last real shot spends once and retains its trait");
                    shooter.ResetTo(new Vector3(0,.25f,-4.2f));
                    Check(physics.ActiveSkin==0&&MahalleProfile.EffectiveSkin==0&&body.mass==.05f,"Next shot automatically uses free normal marble");
                }
                finally{UnityEngine.Object.DestroyImmediate(ball);}
                var saved=JsonUtility.FromJson<MahalleSave>(JsonUtility.ToJson(d));MahalleProfile.SetTestData(saved);
                Check(MahalleProfile.RemainingLife(skin)==0,"Reload cannot restore exhausted marble");
                saved.beads=129;Check(!MahalleProfile.RepairMarble(skin)&&saved.beads==129,"Repair requires 130");
                saved.beads=130;Check(MahalleProfile.RepairMarble(skin)&&saved.beads==0&&MahalleProfile.RemainingLife(skin)==150,"130 full renewal");
                Check(!MahalleProfile.RepairMarble(skin),"No repeated full repair");
                Check(MahalleProfile.EquipOrBuy(skin)&&saved.beads==0,"Reequip never charges purchase twice");
            }
            var legacy=new MahalleSave{version=2,beads=37,selectedSkin=9};legacy.skins[6]=legacy.skins[9]=true;
            MahalleProfile.SetTestData(legacy);
            Check(legacy.version==3&&legacy.beads==1237&&!legacy.skins[6]&&!legacy.skins[9]&&legacy.selectedSkin==0,"Team purchase refunds and removes old equipment");
            MahalleProfile.SetTestData(legacy);Check(legacy.beads==1237,"Migration refund occurs once");
            for(int i=24;i<36;i++)
            {
                var l=Campaign.Database.Get(i);
                Check(l.marbles!=null&&l.obstacles!=null&&l.obstacles.Length>=1&&l.obstacles.Length<=3,"Handcrafted Park puzzle "+i);
                Check(l.starsToPass==(i==35?2:1),"Park gate budget");
                for(int j=0;j<l.marbles.Length;j++)
                {
                    var a=new Vector2(l.marbles[j].x,l.marbles[j].z);
                    Check(a.magnitude+.25f<l.arenaSize,"Park target inside circle");
                    for(int k=0;k<j;k++)Check(Vector2.Distance(a,new Vector2(l.marbles[k].x,l.marbles[k].z))>.5f,"Park targets separate");
                    foreach(var wall in l.obstacles)
                    {
                        var q=Quaternion.Euler(0,-wall.angle,0)*new Vector3(a.x-wall.x,0,a.y-wall.z);
                        Check(new Vector2(Mathf.Max(0,Mathf.Abs(q.x)-wall.width/2),Mathf.Max(0,Mathf.Abs(q.z)-wall.depth/2)).magnitude>.26f,"Park target clear of wall");
                    }
                }
            }
            var final=Campaign.Database.Get(35);var before=Campaign.Database.Get(34);var school=Campaign.Database.Get(23);
            // Finalin zorlugu artik mutlak hedefte degil PAYDA (tavan - hedef): Park 12'nin
            // tavani Park 11'den dusuk, o yuzden hedefleri de dusuk olmak zorunda.
            // Pay kontrolu simulasyon gerektirdigi icin ParkPhysicsVerify ozetinde.
            Check(final.starsToPass==2&&before.starsToPass==1,"Park final is the only gated level in Park");
            Check(final.TotalMarbles()>=before.TotalMarbles(),"Park final has at least as many marbles as Park 11");
            Check(final.TotalMarbles()>school.TotalMarbles(),"Park final has more marbles than School final");
            Check(final.shotCount<=school.shotCount,"Park final has no more shots than School final");
            Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("Mahalle/Marble")),"Glass shader compiles");
            foreach(var name in new[]{"Mahalle/Environment","Mahalle/ContactShadow"})
            {
                var sh=Resources.Load<Shader>(name);
                Check(sh!=null,"Shader yuklendi: "+name);
                Check(sh!=null&&!ShaderUtil.ShaderHasError(sh),"Shader derleniyor (mor obje sebebi): "+name);
            }
            Check(Shader.Find("Universal Render Pipeline/Lit")!=null,"URP Lit bulunabiliyor (engel/zemin materyali)");
            return count;
        }
        finally{MahalleProfile.SetTestData(previous);MahalleProfile.TestMode=wasTest;}
    }
}
