using UnityEngine;
using UnityEngine.UI;

// Arayüzün bütün çizimleri burada üretilir. Hiçbir görsel dosya kullanılmaz.
[RequireComponent(typeof(CanvasRenderer))]
public class MahalleGraphic : MaskableGraphic
{
    public enum Shape
    {
        Panel, Circle, Star, Marble, Lock, Bag, House, Arrow, Triangle, Ring, Hand,
        ChalkRing, ChalkTriangle, Chevron, TabMap, TabTask
    }

    public Shape shape;
    public float radius = 24;
    public Color accent = new Color(1, .85f, .4f);

    [Tooltip("Panelin alt rengi. Alfa 0 ise tek renk çizilir.")]
    public Color colorB = new Color(0, 0, 0, 0);
    [Tooltip("Panelin altına düşen yumuşak gölgenin yüksekliği (piksel).")]
    public float shadow;
    [Tooltip("Panelin üst kenarına ince bir ışık çizgisi ekler.")]
    public bool highlight;
    [Tooltip("Tebeşir çemberini kesik çizgi yapar.")]
    public bool dashed;
    [Tooltip("Tebeşir çemberinin kalınlığı. 0 ise genişliğe göre hesaplanır.")]
    public float stroke;
    [Tooltip("Tebeşir çemberinin ne kadarının çizildiği. 1 = tam çember. Açılış animasyonu bunu 0'dan 1'e götürür.")]
    [Range(0f, 1f)] public float progress = 1f;
    [Tooltip("Oku yatayda çevirir. Kutunun ortasına göre aynalar.")]
    public bool mirror;

    private Color Bottom { get { return colorB.a > .001f ? colorB : color; } }

    // Açılış animasyonu bunu çağırır: değeri yazar ve çizimi tazeler.
    public void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(progress, value)) return;
        progress = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        Vector2 c = r.center;
        float w = r.width, h = r.height;

        switch (shape)
        {
            case Shape.Panel:
                if (shadow > 0f)
                    for (int i = 3; i >= 1; i--)
                        Rounded(vh, new Rect(r.x - i * 1.5f, r.yMin - shadow * i / 3f, w + i * 3f, h),
                                radius + i * 1.5f, new Color(0, 0, 0, .05f));
                RoundedGradient(vh, r, radius, color, Bottom);
                if (highlight)
                    Rounded(vh, new Rect(r.x + radius * .6f, r.yMax - 3.5f, w - radius * 1.2f, 3.5f),
                            1.75f, new Color(1, 1, 1, .30f));
                break;

            case Shape.Circle:
                Disc(vh, c, w * .5f, h * .5f, color);
                break;

            case Shape.Star:
                for (int i = 0; i < 10; i++)
                {
                    float a = (90 + i * 36) * Mathf.Deg2Rad, b = (90 + (i + 1) * 36) * Mathf.Deg2Rad;
                    Tri(vh, c,
                        c + new Vector2(Mathf.Cos(a) * w, Mathf.Sin(a) * h) * (i % 2 == 0 ? .5f : .22f),
                        c + new Vector2(Mathf.Cos(b) * w, Mathf.Sin(b) * h) * (i % 2 == 0 ? .22f : .5f), color);
                }
                break;

            case Shape.Marble:
                Disc(vh, c + new Vector2(w * .025f, -h * .07f), w * .48f, h * .44f, new Color(0, 0, 0, .16f));
                Disc(vh, c, w * .47f, h * .47f, color * .7f);
                Disc(vh, c + new Vector2(-w * .035f, h * .045f), w * .41f, h * .41f, color);
                for (int i = 0; i < 18; i++)
                {
                    float t = i / 17f, y = (t - .5f) * h * .73f, x = Mathf.Sin(t * 5.8f) * w * .19f;
                    Disc(vh, c + new Vector2(x, y), w * (.045f + .06f * Mathf.Sin(t * Mathf.PI)), h * .035f, accent);
                }
                Disc(vh, c + new Vector2(-w * .15f, h * .22f), w * .11f, h * .07f, new Color(1, 1, 1, .85f));
                Disc(vh, c + new Vector2(w * .24f, -h * .2f), w * .045f, h * .045f, new Color(1, 1, 1, .36f));
                break;

            case Shape.Lock:
                Rounded(vh, new Rect(c.x - w * .34f, c.y - h * .4f, w * .68f, h * .52f), w * .1f, color);
                for (int i = 0; i < 14; i++)
                {
                    float a = (i * 180 / 13f) * Mathf.Deg2Rad;
                    Disc(vh, c + new Vector2(Mathf.Cos(a) * w * .24f, Mathf.Sin(a) * h * .3f + h * .08f), w * .045f, h * .045f, color);
                }
                Disc(vh, c - new Vector2(0, h * .08f), w * .05f, h * .07f, accent);
                break;

            case Shape.Bag:
                Disc(vh, c - new Vector2(0, h * .12f), w * .39f, h * .35f, color);
                Tri(vh, c + new Vector2(-w * .3f, h * .4f), c + new Vector2(w * .3f, h * .4f), c - new Vector2(0, h * .1f), color);
                Rounded(vh, new Rect(c.x - w * .27f, c.y + h * .12f, w * .54f, h * .07f), 2, accent);
                break;

            case Shape.House:
                Rounded(vh, new Rect(c.x - w * .36f, c.y - h * .42f, w * .72f, h * .63f), 4, color);
                Tri(vh, c + new Vector2(-w * .48f, h * .16f), c + new Vector2(w * .48f, h * .16f), c + new Vector2(0, h * .48f), accent);
                Rounded(vh, new Rect(c.x - w * .08f, c.y - h * .42f, w * .16f, h * .28f), 3, accent);
                for (int i = 0; i < 2; i++)
                    Rounded(vh, new Rect(c.x - w * .25f + i * w * .34f, c.y - h * .03f, w * .16f, h * .15f), 2, new Color(1, .88f, .55f));
                break;

            case Shape.Arrow:
                Tri(vh, c + new Vector2(-w * .22f, -h * .35f), c + new Vector2(w * .28f, 0), c + new Vector2(-w * .22f, h * .35f), color);
                break;

            case Shape.Triangle:
                Stroke(vh, c + new Vector2(-w * .42f, h * .35f), c + new Vector2(w * .42f, h * .35f), w * .05f, color);
                Stroke(vh, c + new Vector2(w * .42f, h * .35f), c + new Vector2(0, -h * .4f), w * .05f, color);
                Stroke(vh, c + new Vector2(0, -h * .4f), c + new Vector2(-w * .42f, h * .35f), w * .05f, color);
                Disc(vh, c, w * .06f, h * .06f, accent);
                Disc(vh, c + new Vector2(-w * .13f, h * .17f), w * .06f, h * .06f, accent);
                Disc(vh, c + new Vector2(w * .13f, h * .17f), w * .06f, h * .06f, accent);
                break;

            case Shape.Ring:
                for (int i = 0; i < 40; i++)
                {
                    float a = i * Mathf.PI * 2 / 40, b = (i + 1) * Mathf.PI * 2 / 40;
                    Stroke(vh, c + new Vector2(Mathf.Cos(a) * w * .4f, Mathf.Sin(a) * h * .4f),
                               c + new Vector2(Mathf.Cos(b) * w * .4f, Mathf.Sin(b) * h * .4f), w * .04f, color);
                }
                Disc(vh, c, w * .07f, h * .07f, accent);
                break;

            case Shape.Hand:
                Rounded(vh, new Rect(c.x - w * .14f, c.y - h * .15f, w * .24f, h * .62f), w * .11f, color);
                Rounded(vh, new Rect(c.x - w * .28f, c.y - h * .4f, w * .65f, h * .55f), w * .18f, color);
                break;

            // Elle çizilmiş tebeşir çemberi: çizgi hafif titrek, kalınlığı değişken.
            case Shape.ChalkRing:
            {
                float rx = w * .5f, ry = h * .5f;
                float tw = stroke > 0f ? stroke : Mathf.Max(2f, w * .045f);
                const int seg = 72;
                float p = Mathf.Clamp01(progress);
                if (p <= 0f) break;
                float drawn = seg * p;
                const float top = Mathf.PI * .5f;
                for (int i = 0; i < seg; i++)
                {
                    if (i >= drawn) break;
                    if (dashed && (i % 6) >= 4) continue;
                    // Son parça kesirli çizilir: tebeşir kesik kesik değil akıcı ilerler.
                    float f = Mathf.Min(1f, drawn - i);
                    float a = top - i * Mathf.PI * 2 / seg;
                    float b = top - (i + f) * Mathf.PI * 2 / seg;
                    float k1 = 1f + Mathf.Sin(i * 2.3f) * .005f + Mathf.Sin(i * .7f) * .003f;
                    float kn = 1f + Mathf.Sin((i + 1) * 2.3f) * .005f + Mathf.Sin((i + 1) * .7f) * .003f;
                    float k2 = Mathf.Lerp(k1, kn, f);
                    // Tebeşir dokusu artık yamuklukta değil kalınlık ve koyulukta:
                    // çizgi gerçek bir yuvarlak ama bastırma gücü boyunca değişiyor.
                    float t1 = tw * (.70f + .45f * Mathf.Abs(Mathf.Sin(i * 1.7f)));
                    var ink = color; ink.a *= .72f + .28f * Mathf.Abs(Mathf.Sin(i * 3.1f + 1.2f));
                    Stroke(vh, c + new Vector2(Mathf.Cos(a) * rx * k1, Mathf.Sin(a) * ry * k1),
                               c + new Vector2(Mathf.Cos(b) * rx * k2, Mathf.Sin(b) * ry * k2), t1, ink);
                }
                break;
            }

            // Çemberin içindeki misket üçgeni. Tepe noktasından başlar, saat
            // yönünde çizilir; progress ile üç kenar sırayla tamamlanır.
            case Shape.ChalkTriangle:
            {
                float pt = Mathf.Clamp01(progress);
                if (pt <= 0f) break;
                float tt = stroke > 0f ? stroke : Mathf.Max(2f, w * .04f);
                Vector2[] corner =
                {
                    c + new Vector2(0, h * .42f),
                    c + new Vector2(w * .40f, -h * .28f),
                    c + new Vector2(-w * .40f, -h * .28f)
                };
                const int per = 16;                 // kenar başına parça
                float total = 3 * per * pt;
                for (int side = 0; side < 3; side++)
                {
                    Vector2 from = corner[side], to = corner[(side + 1) % 3];
                    Vector2 dir = (to - from).normalized;
                    Vector2 nrm = new Vector2(-dir.y, dir.x);
                    for (int i = 0; i < per; i++)
                    {
                        int step = side * per + i;
                        if (step >= total) { side = 3; break; }
                        float f = Mathf.Min(1f, total - step);
                        float u0 = i / (float)per, u1 = (i + f) / per;
                        // Elle çizilmiş çizgi: kenar boyunca minik sapma.
                        float w0 = Mathf.Sin(step * 1.9f) * tt * .22f;
                        float w1 = Mathf.Sin((step + 1) * 1.9f) * tt * .22f;
                        Vector2 a0 = Vector2.Lerp(from, to, u0) + nrm * w0;
                        Vector2 a1 = Vector2.Lerp(from, to, u1) + nrm * Mathf.Lerp(w0, w1, f);
                        float th = tt * (.70f + .45f * Mathf.Abs(Mathf.Sin(step * 1.3f)));
                        var ink = color; ink.a *= .72f + .28f * Mathf.Abs(Mathf.Sin(step * 2.7f));
                        Stroke(vh, a0, a1, th, ink);
                    }
                }
                break;
            }

            case Shape.Chevron:
            {
                float t = Mathf.Max(2f, w * .13f);
                float m = mirror ? -1f : 1f;
                Stroke(vh, c + new Vector2(-w * .16f * m, h * .3f), c + new Vector2(w * .16f * m, 0), t, color);
                Stroke(vh, c + new Vector2(w * .16f * m, 0), c + new Vector2(-w * .16f * m, -h * .3f), t, color);
                Disc(vh, c + new Vector2(w * .16f * m, 0), t * .5f, t * .5f, color);
                break;
            }

            // Katlanmış mahalle haritası
            case Shape.TabMap:
            {
                float t = Mathf.Max(1.6f, w * .075f);
                Vector2[] p =
                {
                    c + new Vector2(-w * .42f, -h * .30f), c + new Vector2(-w * .14f, -h * .42f),
                    c + new Vector2( w * .14f, -h * .30f), c + new Vector2( w * .42f, -h * .42f),
                    c + new Vector2( w * .42f,  h * .30f), c + new Vector2( w * .14f,  h * .42f),
                    c + new Vector2(-w * .14f,  h * .30f), c + new Vector2(-w * .42f,  h * .42f)
                };
                for (int i = 0; i < p.Length; i++) Stroke(vh, p[i], p[(i + 1) % p.Length], t, color);
                Stroke(vh, p[1], p[6], t, color);
                Stroke(vh, p[2], p[5], t, color);
                break;
            }

            // Görev listesi
            case Shape.TabTask:
            {
                float t = Mathf.Max(1.6f, w * .075f);
                Rect box = new Rect(c.x - w * .34f, c.y - h * .40f, w * .68f, h * .80f);
                Stroke(vh, new Vector2(box.xMin, box.yMin), new Vector2(box.xMax, box.yMin), t, color);
                Stroke(vh, new Vector2(box.xMax, box.yMin), new Vector2(box.xMax, box.yMax), t, color);
                Stroke(vh, new Vector2(box.xMax, box.yMax), new Vector2(box.xMin, box.yMax), t, color);
                Stroke(vh, new Vector2(box.xMin, box.yMax), new Vector2(box.xMin, box.yMin), t, color);
                Stroke(vh, c + new Vector2(-w * .18f, h * .12f), c + new Vector2(w * .06f, h * .12f), t, color);
                Stroke(vh, c + new Vector2(-w * .18f, -h * .06f), c + new Vector2(w * .16f, -h * .06f), t, color);
                break;
            }
        }
    }

    public static void Tri(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color col)
    {
        int i = vh.currentVertCount;
        vh.AddVert(a, col, Vector2.zero); vh.AddVert(b, col, Vector2.zero); vh.AddVert(c, col, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2);
    }

    public static void TriBlend(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color ca, Color cb, Color cc)
    {
        int i = vh.currentVertCount;
        vh.AddVert(a, ca, Vector2.zero); vh.AddVert(b, cb, Vector2.zero); vh.AddVert(c, cc, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2);
    }

    public static void Disc(VertexHelper vh, Vector2 c, float rx, float ry, Color col)
    {
        for (int i = 0; i < 40; i++)
        {
            float a = i * Mathf.PI * 2 / 40, b = (i + 1) * Mathf.PI * 2 / 40;
            Tri(vh, c, c + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry),
                       c + new Vector2(Mathf.Cos(b) * rx, Mathf.Sin(b) * ry), col);
        }
    }

    public static void Stroke(VertexHelper vh, Vector2 a, Vector2 b, float width, Color col)
    {
        Vector2 d = (b - a).normalized;
        Vector2 n = new Vector2(-d.y, d.x) * width * .5f;
        Tri(vh, a + n, a - n, b + n, col);
        Tri(vh, a - n, b - n, b + n, col);
    }

    public static void Quad(VertexHelper vh, Rect r, Color col)
    {
        Tri(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), col);
        Tri(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin), col);
    }

    public static void QuadBlend(VertexHelper vh, Rect r, Color top, Color bottom)
    {
        TriBlend(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), bottom, top, top);
        TriBlend(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin), bottom, top, bottom);
    }

    public static void Rounded(VertexHelper vh, Rect r, float radius, Color col)
    {
        RoundedGradient(vh, r, radius, col, col);
    }

    public static void RoundedGradient(VertexHelper vh, Rect r, float radius, Color top, Color bottom)
    {
        float k = Mathf.Min(radius, Mathf.Min(r.width, r.height) * .5f);
        var points = new Vector2[36];
        int p = 0;
        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 c = new Vector2(corner == 0 || corner == 3 ? r.xMax - k : r.xMin + k, corner < 2 ? r.yMax - k : r.yMin + k);
            for (int j = 0; j <= 8; j++)
            {
                float a = (corner * 90 + j * 90 / 8f) * Mathf.Deg2Rad;
                points[p++] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * k;
            }
        }
        Color mid = Color.Lerp(bottom, top, .5f);
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 a = points[i], b = points[(i + 1) % points.Length];
            TriBlend(vh, r.center, a, b, mid, AtY(r, top, bottom, a.y), AtY(r, top, bottom, b.y));
        }
    }

    private static Color AtY(Rect r, Color top, Color bottom, float y)
    {
        return Color.Lerp(bottom, top, Mathf.InverseLerp(r.yMin, r.yMax, y));
    }
}
