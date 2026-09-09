using UnityEngine;

public class MarbleVisual : MonoBehaviour
{
    private static Material[] materials;
    private Transform shadow;
    private static Material shadowMaterial;
    private int skin;
    public void SetSkin(int index)
    {
        skin = Mathf.Clamp(index, 0, 5);
        if (materials == null || materials[0] == null)
        {
            materials = new Material[6];
            Shader shader = Resources.Load<Shader>("Mahalle/Marble");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            for (int i=0;i<6;i++)
            {
                materials[i] = new Material(shader);
                materials[i].SetColor("_BaseColor", Campaign.SkinColors[i]);
                materials[i].SetColor("_SwirlColor", Color.Lerp(Campaign.SkinColors[(i+2)%6],Color.white,.4f));
                if (materials[i].HasProperty("_Smoothness")) materials[i].SetFloat("_Smoothness",.95f);
            }
        }
        var renderer = GetComponent<Renderer>(); if (renderer != null) renderer.sharedMaterial = materials[skin];
    }
    private void Start()
    {
        SetSkin(skin);
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name="Misket gölgesi";
        Destroy(go.GetComponent<Collider>()); shadow=go.transform;
        shadow.rotation=Quaternion.Euler(90,0,0);
        if (shadowMaterial == null)
        {
            var tex = new Texture2D(32,32,TextureFormat.RGBA32,false);
            for(int y=0;y<32;y++) for(int x=0;x<32;x++)
            { float d=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/16f; tex.SetPixel(x,y,new Color(.08f,.05f,.03f,Mathf.Pow(Mathf.Clamp01(1-d),1.4f)*.44f)); }
            tex.Apply();
            shadowMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            shadowMaterial.SetTexture("_BaseMap",tex); shadowMaterial.SetFloat("_Surface",1);
            shadowMaterial.SetFloat("_SrcBlend",5); shadowMaterial.SetFloat("_DstBlend",10);
            shadowMaterial.SetFloat("_ZWrite",0); shadowMaterial.renderQueue=3000;
            shadowMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        go.GetComponent<Renderer>().sharedMaterial=shadowMaterial;
    }
    private void LateUpdate()
    {
        if(shadow==null) return;
        var p=transform.position; shadow.position=new Vector3(p.x+.06f,.024f,p.z-.06f);
        shadow.localScale=Vector3.one*transform.localScale.x*1.6f;
    }
    private void OnDestroy() { if(shadow!=null) Destroy(shadow.gameObject); }
}
