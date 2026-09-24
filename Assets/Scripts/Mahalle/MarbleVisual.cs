using UnityEngine;

public class MarbleVisual : MonoBehaviour
{
    private static Material[] materials;
    private Transform shadow;
    private static Material shadowMaterial;
    private int skin;
    // Harita 2 özel hedefleri (karpuz) kaplama tablosu dışında kendi materyalini kullanır.
    private Material overrideMaterial;
    public void SetOverride(Material material)
    {
        overrideMaterial = material;
        var r = GetComponent<Renderer>();
        if (r != null && material != null) r.sharedMaterial = material;
    }
    public void SetSkin(int index)
    {
        skin = Mathf.Clamp(index, 0, Campaign.SkinCount - 1);
        // MOR MISKET HATASI.
        // Materyaller static: sahneler arasi yasiyorlar. Unity sahne degisiminde
        // ya da bellek baskisinda "kullanilmiyor" sandigi bir materyali
        // temizleyebiliyor; o slot null kalinca o desendeki misket MOR cizilir.
        //
        // Onceki duzeltme butun slotlari kontrol edip yeniden kuruyordu ama
        // EKSIKTI: yeniden kurma sadece o an SetSkin cagiran misketi
        // toparliyor, sahnede zaten duran misket eski (yok edilmis) materyale
        // bakmaya devam ediyordu. Telefonda tek bir misketin mor kalmasinin
        // sebebi buydu.
        //
        // Asil cozum asagida: materyallere HideAndDontSave veriliyor, boylece
        // Unity onlari hic temizlemiyor. Ustune LateUpdate'te ucuz bir kontrol
        // var -- materyal yine de kaybolursa misket kendini onariyor.
        bool rebuild = materials == null || materials.Length != Campaign.SkinCount;
        if (!rebuild)
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] == null) { rebuild = true; break; }
        if (rebuild)
        {
            materials = new Material[Campaign.SkinCount];
            Shader shader = Resources.Load<Shader>("Mahalle/Marble");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            Texture matcap = Resources.Load<Texture2D>("Mahalle/MarbleMatcap");
            for (int i=0;i<Campaign.SkinCount;i++)
            {
                materials[i] = new Material(shader);
                // Unity'nin kullanilmayan varlik temizligi bunlara dokunmasin.
                materials[i].hideFlags = HideFlags.HideAndDontSave;
                materials[i].SetColor("_BaseColor", Campaign.SkinColors[i]);
                materials[i].SetColor("_SwirlColor", SpecialMarbles.Accent(i));
                if (materials[i].HasProperty("_Smoothness")) materials[i].SetFloat("_Smoothness",.95f);
                if (matcap != null && materials[i].HasProperty("_Matcap")) materials[i].SetTexture("_Matcap", matcap);
            }
        }
        var renderer = GetComponent<Renderer>();
        // Null materyal = mor obje. Olmasi gerekmiyor ama olursa sessizce mor birakmayalim.
        if (renderer != null && overrideMaterial != null) renderer.sharedMaterial = overrideMaterial;
        else if (renderer != null && materials[skin] != null) renderer.sharedMaterial = materials[skin];
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
            shadowMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name="Misket gölgesi";
        Destroy(go.GetComponent<Collider>()); shadow=go.transform;
        shadow.rotation=Quaternion.Euler(90,0,0);
        go.GetComponent<Renderer>().sharedMaterial=shadowMaterial;
    }
    private void LateUpdate()
    {
        // Emniyet: materyal her seye ragmen kaybolduysa misketi mor birakma.
        // Tek bir null karsilastirmasi, her karede calissa bile bedeli yok.
        var r = GetComponent<Renderer>();
        if (r != null && r.sharedMaterial == null) SetSkin(skin);

        if(shadow==null) return;
        var p=transform.position; shadow.position=new Vector3(p.x+.06f,.024f,p.z-.06f);
        shadow.localScale=Vector3.one*transform.localScale.x*1.6f;
    }
    private void OnDestroy() { if(shadow!=null) Destroy(shadow.gameObject); }
    // Karpuz bölününce bütün misket gizlenir; gölgesi yerde asılı kalmasın.
    private void OnDisable() { if(shadow!=null) shadow.gameObject.SetActive(false); }
    private void OnEnable() { if(shadow!=null) shadow.gameObject.SetActive(true); }
}
