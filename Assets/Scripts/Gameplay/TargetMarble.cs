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

    public bool IsScored { get; private set; }
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
