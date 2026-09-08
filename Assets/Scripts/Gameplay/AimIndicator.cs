using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AimIndicator : MonoBehaviour
{
    [Header("Line")]
    [SerializeField] private float maxLineLength = 3f;
    [SerializeField] private float heightOffset = 0.05f;

    [Header("Power Colors")]
    [SerializeField] private Color lowPowerColor = new Color(0.35f, 0.85f, 0.4f, 1f);
    [SerializeField] private Color midPowerColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color highPowerColor = new Color(0.95f, 0.25f, 0.2f, 1f);

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        Hide();
    }

    public void Show(Vector3 origin, Vector3 direction, float normalizedPower)
    {
        if (!line.enabled)
        {
            line.enabled = true;
        }

        Vector3 start = origin + Vector3.up * heightOffset;
        Vector3 end = start + direction * (maxLineLength * normalizedPower);

        line.SetPosition(0, start);
        line.SetPosition(1, end);

        Color color = EvaluatePowerColor(normalizedPower);
        line.startColor = color;
        line.endColor = color;
    }

    public void Hide()
    {
        if (line != null)
        {
            line.enabled = false;
        }
    }

    private Color EvaluatePowerColor(float normalizedPower)
    {
        float t = Mathf.Clamp01(normalizedPower);

        if (t < 0.5f)
        {
            return Color.Lerp(lowPowerColor, midPowerColor, t * 2f);
        }

        return Color.Lerp(midPowerColor, highPowerColor, (t - 0.5f) * 2f);
    }
}
