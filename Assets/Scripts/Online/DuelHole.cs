using UnityEngine;

// KUYU MODUNUN CUKURU.
//
// Bilerek AYRI bir bilesen: MarbleArena kampanyanin da kullandigi ortak kod
// ve oraya "cukur" diye bir kavram sokmak 60 bolumu riske atardi. Cukur
// sadece duello sahnesinde, sadece kuyu modunda yaratilir.
//
// Fiziksel bir delik kazmiyoruz. Gercekten cukur acmak zemin mesh'ini
// degistirmek demek; bunun yerine misketin DURDUGU yere bakiyoruz:
// merkeze yeterince yakin durduysa cukura dusmus sayiliyor. Oyuncunun
// gordugu sey ayni, kural da net.
public class DuelHole : MonoBehaviour
{
    public float Radius { get; private set; } = .45f;

    private LineRenderer ring;
    private Transform disc;

    public static DuelHole Create(Transform parent, float radius, Material lineMaterial)
    {
        var go = new GameObject("Kuyu");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        var hole = go.AddComponent<DuelHole>();
        hole.Radius = Mathf.Max(.1f, radius);
        hole.Build(lineMaterial);
        return hole;
    }

    private void Build(Material lineMaterial)
    {
        // Koyu bir daire: cukurun dibi. Isik almayan duz bir renk yeterli,
        // derinlik hissini golge veriyor.
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Dip";
        var col = quad.GetComponent<Collider>();
        if (col != null) Destroy(col);
        quad.transform.SetParent(transform, false);
        quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localPosition = new Vector3(0f, .012f, 0f);
        quad.transform.localScale = Vector3.one * (Radius * 2f);
        disc = quad.transform;

        var mr = quad.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            mr.GetPropertyBlock(block);
            var koyu = new Color(.12f, .10f, .09f, 1f);
            block.SetColor(Shader.PropertyToID("_BaseColor"), koyu);
            block.SetColor(Shader.PropertyToID("_Color"), koyu);
            mr.SetPropertyBlock(block);
        }

        // Cukurun agzi: tebesirle cizilmis gibi titrek bir halka.
        var lineGo = new GameObject("Agiz");
        lineGo.transform.SetParent(transform, false);
        ring = lineGo.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.widthMultiplier = .045f;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows = false;
        if (lineMaterial != null) ring.material = lineMaterial;
        var kirec = new Color(.96f, .94f, .86f);
        ring.startColor = ring.endColor = kirec;

        const int n = 40;
        ring.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            float a = (i / (float)n) * Mathf.PI * 2f;
            float r = Radius + Mathf.Sin(i * 5.3f) * .012f;
            ring.SetPosition(i, new Vector3(Mathf.Cos(a) * r, .02f, Mathf.Sin(a) * r));
        }
    }

    // Misket cukura dustu mu? Merkeze olan uzaklik yariciapin altindaysa evet.
    // Misketin YARICAPI da hesaba katiliyor: agzin tam kenarinda dengede
    // duran bir misket "dustu" sayilmamali.
    public bool Contains(Vector3 worldPosition)
    {
        Vector3 c = transform.position;
        float dx = worldPosition.x - c.x, dz = worldPosition.z - c.z;
        float limit = Radius - DuelPlacement.MarbleRadius * .5f;
        return (dx * dx) + (dz * dz) <= limit * limit;
    }

    // Cukura dusen misketin gorunumu: dibe cekilip kaybolur.
    public Vector3 SettlePoint => transform.position + new Vector3(0f, -.18f, 0f);
}
