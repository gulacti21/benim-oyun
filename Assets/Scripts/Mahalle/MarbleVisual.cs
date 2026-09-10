using UnityEngine;

public class MarbleVisual : MonoBehaviour
{
    private static Material[] materials;
    private Transform shadow;
    private static Material shadowMaterial;
    private int skin;
    public void SetSkin(int index)
    {
        skin = Mathf.Clamp(index, 0, Campaign.SkinCount - 1);
        // Materyaller static: sahneler arasi yasiyorlar. Unity sahne degisiminde
        // kullanilmayan bir materyali temizlerse o slot null kalir ve o desendeki
        // misket MOR cizilir. Eskiden yalnizca materials[0] kontrol ediliyordu,
        // bu yuzden hata tek bir miskette gorunuyordu. Artik hepsi kontrol ediliyor.
        bool rebuild = materials == null || materials.Length != Campaign.SkinCount;
        if (!rebuild)
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] == null) { rebuild = true; break; }
        if (rebuild)
        {
            materials = new Material[Campaign.SkinCount];
            Shader shader = Resources.Load<Shader>("Mahalle/Marble");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            for (int i=0;i<Campaign.SkinCount;i++)
            {
                materials[i] = new Material(shader);
                materials[i].SetColor("_BaseColor", Campaign.SkinColors[i]);
                materials[i].SetColor("_SwirlColor", SpecialMarbles.Accent(i));
                if (materials[i].HasProperty("_Smoothness")) materials[i].SetFloat("_Smoothness",.95f);
            }
        }
        var renderer = GetComponent<Renderer>();
        // Null materyal = mor obje. Olmasi gerekmiyor ama olursa sessizce mor birakmayalim.
        if (renderer != null && materials[skin] != null) renderer.sharedMaterial = materials[skin];
        else if (renderer != null) Debug.LogWarning("MISKETR: misket materyali yok, desen "+skin);
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
