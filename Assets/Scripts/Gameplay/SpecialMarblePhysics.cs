using UnityEngine;
[RequireComponent(typeof(Rigidbody),typeof(SphereCollider))]
public class SpecialMarblePhysics : MonoBehaviour
{
    private Rigidbody body;
    private SphereCollider sphere;
    private PhysicsMaterial original,variant;
    private Vector3 normalScale;
    private float normalMass,normalDrag;
    public int ActiveSkin {get;private set;}
    public float ImpulseMultiplier {get;private set;}=1f;
    private void Awake()
    {
        body=GetComponent<Rigidbody>();sphere=GetComponent<SphereCollider>();
        normalScale=transform.localScale;normalMass=body.mass;normalDrag=body.linearDamping;
        original=sphere.sharedMaterial;variant=new PhysicsMaterial("Özel misket");
    }
    public void Apply(MarblePower power)
    {
        if(body==null)Awake();
        transform.localScale=normalScale;body.mass=normalMass;body.linearDamping=normalDrag;
        sphere.sharedMaterial=original;ImpulseMultiplier=1f;
        ActiveSkin=power==MarblePower.None?MahalleProfile.EffectiveSkin:0;
        if(power==MarblePower.None&&SpecialMarbles.IsSpecial(ActiveSkin))
        {
            int kind=ActiveSkin-SpecialMarbles.FirstSkin;
            if(kind==0){body.mass=normalMass*1.25f;ImpulseMultiplier=1.12f;}
            if(kind==1)transform.localScale=normalScale*.8f;
            if(kind==2||kind==3)
            {
                variant.dynamicFriction=original!=null?original.dynamicFriction:.6f;
                variant.staticFriction=original!=null?original.staticFriction:.6f;
                variant.bounciness=original!=null?original.bounciness:0f;
                variant.frictionCombine=original!=null?original.frictionCombine:PhysicsMaterialCombine.Average;
                variant.bounceCombine=original!=null?original.bounceCombine:PhysicsMaterialCombine.Average;
                if(kind==2){variant.bounciness=.65f;variant.bounceCombine=PhysicsMaterialCombine.Maximum;}
                else{variant.dynamicFriction*=.65f;variant.staticFriction*=.65f;variant.frictionCombine=PhysicsMaterialCombine.Minimum;body.linearDamping=normalDrag*.8f;}
                sphere.sharedMaterial=variant;
            }
        }
        else if(power==MarblePower.Big){transform.localScale=normalScale*1.65f;body.mass=normalMass*2f;}
        else if(power==MarblePower.Iron)body.mass=normalMass*2.6f;
    }
    private void OnDestroy()
    {
        if(variant==null)return;
#if UNITY_EDITOR
        if(!Application.isPlaying){DestroyImmediate(variant);return;}
#endif
        Destroy(variant);
    }
}
