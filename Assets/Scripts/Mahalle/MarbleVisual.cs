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
        if (shadowMaterial == null)
        {
            // Load an included, fixed transparent pass; runtime URP keywords can be stripped in players.
            var shader = Resources.Load<Shader>("Mahalle/ContactShadow");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("MISKETR: Contact shadow shader unavailable; skipping shadow.");
                return;
            }
            shadowMaterial = new Material(shader);
        }
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name="Misket gölgesi";
        Destroy(go.GetComponent<Collider>()); shadow=go.transform;
        shadow.rotation=Quaternion.Euler(90,0,0);
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
