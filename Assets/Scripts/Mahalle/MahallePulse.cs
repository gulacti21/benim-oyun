using UnityEngine;

// Sıradaki durağın dışa doğru açılan altın halkası.
public class MahallePulse : MonoBehaviour
{
    public float from = 1f, to = 1.75f, period = 2.6f;
    private RectTransform rect;
    private MahalleGraphic graphic;
    private float baseAlpha;

    private void Awake()
    {
        rect = (RectTransform)transform;
        graphic = GetComponent<MahalleGraphic>();
        if (graphic != null) baseAlpha = graphic.color.a;
    }

    private void Update()
    {
        float t = Mathf.Repeat(Time.unscaledTime, period) / period;
        rect.localScale = Vector3.one * Mathf.Lerp(from, to, t);
        if (graphic != null)
        {
            Color c = graphic.color;
            c.a = baseAlpha * (1f - t);
            graphic.color = c;
        }
    }
}
