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
    private float maxHalfWidth = -1f;
    private float centerX;

    public float LineZ => transform.position.z;
    public float HalfWidth => halfWidth;

    // Bölüm hattı daraltabilir veya yana kaydırabilir: nereden atacağın da bir karar olsun.
    public void ApplyLevel(float halfWidthLimit, float offsetX)
    {
        centerX = offsetX;
        maxHalfWidth = halfWidthLimit > 0f ? halfWidthLimit : 4f;
        halfWidth = maxHalfWidth;
        Refresh();
    }

    private void Awake()
    {
        if (maxHalfWidth < 0f) maxHalfWidth = halfWidth;
        Refresh();
    }

    // Çizginin uçları ekranın dışına taşarsa oyuncu misketi geri çekemez:
    // parmağını ekran dışına götürmesi gerekir. Bu yüzden çizgiyi kameranın
    // gerçekten gösterdiği alana sığdırıp kenarda parmak payı bırakıyoruz.
    public void FitToCamera(Camera view, float margin)
    {
        if (maxHalfWidth < 0f) maxHalfWidth = halfWidth;
        if (view == null) { Refresh(); return; }

        float visibleHalf = view.orthographic
            ? view.orthographicSize * view.aspect
            : Mathf.Abs(view.transform.position.y) * Mathf.Tan(view.fieldOfView * .5f * Mathf.Deg2Rad) * view.aspect;

        halfWidth = Mathf.Clamp(visibleHalf - margin, 1.2f, maxHalfWidth);
        Refresh();
    }

    public void SetPosition(Vector3 worldPosition)
    {
        transform.position = new Vector3(centerX, 0f, worldPosition.z);
        Refresh();
    }

    public bool IsNear(Vector3 worldPoint)
    {
        return Mathf.Abs(worldPoint.z - LineZ) <= tapTolerance
            && Mathf.Abs(worldPoint.x - centerX) <= halfWidth + tapTolerance;
    }

    public Vector3 ClampToLine(Vector3 worldPoint, float height)
    {
        float x = Mathf.Clamp(worldPoint.x, centerX - halfWidth, centerX + halfWidth);
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
        transform.position = new Vector3(centerX, transform.position.y, transform.position.z);
    }
}
