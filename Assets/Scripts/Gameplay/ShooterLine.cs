using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ShooterLine : MonoBehaviour
{
    [SerializeField] private float halfWidth = 4f;
    [SerializeField] private float lineHeight = 0.02f;
    [SerializeField] private float lineWidth = 0.06f;
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField] private float tapTolerance = 1.2f;

    private LineRenderer line;

    public float LineZ => transform.position.z;

    private void Awake()
    {
        Refresh();
    }

    public void SetPosition(Vector3 worldPosition)
    {
        transform.position = new Vector3(0f, 0f, worldPosition.z);
        Refresh();
    }

    public bool IsNear(Vector3 worldPoint)
    {
        return Mathf.Abs(worldPoint.z - LineZ) <= tapTolerance
            && Mathf.Abs(worldPoint.x) <= halfWidth + tapTolerance;
    }

    public Vector3 ClampToLine(Vector3 worldPoint, float height)
    {
        float x = Mathf.Clamp(worldPoint.x, -halfWidth, halfWidth);
        return new Vector3(x, height, LineZ);
    }

    private void Refresh()
    {
        if (line == null)
        {
            line = GetComponent<LineRenderer>();
        }

        if (line == null)
        {
            return;
        }

        line.useWorldSpace = false;
        line.loop = false;
        line.widthMultiplier = lineWidth;
        line.startColor = lineColor;
        line.endColor = lineColor;
        line.positionCount = 2;
        line.SetPosition(0, new Vector3(-halfWidth, lineHeight, 0f));
        line.SetPosition(1, new Vector3(halfWidth, lineHeight, 0f));
    }
}
