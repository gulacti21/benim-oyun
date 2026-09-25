using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TargetMarble : MonoBehaviour
{
    [SerializeField] private Color scoredColor = new Color(0.95f, 0.78f, 0.25f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

    private Rigidbody body;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;

    // Online duelloda misketin sahibi: 0 veya 1. Kampanyada -1 kalir,
    // yani tek oyunculu oyun bu alani hic kullanmaz.
    public int Owner { get; set; } = -1;

    public bool IsScored { get; private set; }
    // Kaç misket değerinde. Karpuz yarımı 0: HERHANGİ iki yarım birlikte 1 (MarbleArena sayar).
    public int Worth { get; set; } = 1;
    public bool IsHalf { get; set; }
    public Rigidbody Body => body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void MarkScored()
    {
        if (IsScored)
        {
            return;
        }

        IsScored = true;
        ApplyScoredColor();
    }

    // Çukura düştü: sayılmaz, soluk görünür.
    public bool IsLost { get; private set; }
    public void MarkLost()
    {
        if (IsLost || IsScored) return;
        IsLost = true;
        if (meshRenderer == null) return;
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(propertyBlock);
        var dim = new Color(.35f, .30f, .26f, 1f);
        propertyBlock.SetColor(BaseColorId, dim);
        propertyBlock.SetColor(LegacyColorId, dim);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    private void ApplyScoredColor()
    {
        if (meshRenderer == null)
        {
            return;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        meshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, scoredColor);
        propertyBlock.SetColor(LegacyColorId, scoredColor);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }
}
