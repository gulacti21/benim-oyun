using UnityEngine;
using UnityEngine.UI;

// Haritanın zemini: renk geçişi, zemin izleri, çakıl, günün saati yıkaması ve kenar karartması.
[RequireComponent(typeof(CanvasRenderer))]
public class MahalleGround : MaskableGraphic
{
    public Color top = Color.gray, mid = Color.gray, bottom = Color.gray;
    public Color wash = new Color(0, 0, 0, 0);
    public Color vignette = new Color(0, 0, 0, .4f);
    public Color pebble = new Color(.6f, .5f, .35f);
    public Color chalk = Color.white;
    public GroundMarks marks = GroundMarks.None;
    [Tooltip("Bir taslak biriminin kaç piksel olduğu.")]
    public float unit = 3f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        float u = Mathf.Max(.01f, unit);
        float half = r.yMin + r.height * .5f;

        // Taslak koordinatını (sol üst başlangıçlı) arayüz koordinatına çevirir.
        Vector2 M(float mx, float my) { return new Vector2(r.xMin + mx * u, r.yMax - my * u); }

        MahalleGraphic.QuadBlend(vh, new Rect(r.xMin, half, r.width, r.yMax - half), top, mid);
        MahalleGraphic.QuadBlend(vh, new Rect(r.xMin, r.yMin, r.width, half - r.yMin), mid, bottom);

        Marks(vh, r, u, M);
        Pebbles(vh, r, u, M);

        if (wash.a > .001f) MahalleGraphic.Quad(vh, r, wash);

        Vignette(vh, r);
    }

    private void Marks(VertexHelper vh, Rect r, float u, System.Func<float, float, Vector2> M)
    {
        Color joint = new Color(.18f, .15f, .09f, .13f);
        switch (marks)
        {
            case GroundMarks.Tiles:
                for (float y = 0; y * u < r.height; y += 54)
                    MahalleGraphic.Stroke(vh, M(0, y), M(356, y), 1.2f * u, joint);
                for (float x = 0; x * u <= r.width; x += 59)
                    MahalleGraphic.Stroke(vh, M(x, 0), M(x, r.height / u), 1.2f * u, joint);
                break;

            case GroundMarks.Court:
            {
                Color paint = chalk; paint.a = .26f;
                MahalleGraphic.Stroke(vh, M(0, 392), M(356, 392), 3f * u, paint);
                MahalleGraphic.Stroke(vh, M(20, 1010), M(336, 1010), 3f * u, paint);
                Circle(vh, M(178, 392), 74f * u, 3f * u, paint);
                Circle(vh, M(178, 1010), 52f * u, 3f * u, paint);
                Box(vh, M(98, 14), M(258, 106), 3f * u, paint);
                break;
            }

            case GroundMarks.Cracks:
            {
                Color crack = new Color(.42f, .32f, .15f, .26f);
                Poly(vh, u, M, crack, new[] { 18f, 214f, 62f, 250f, 50f, 306f, 94f, 342f });
                Poly(vh, u, M, crack, new[] { 304f, 424f, 266f, 476f, 290f, 528f });
                Poly(vh, u, M, crack, new[] { 38f, 704f, 84f, 748f, 68f, 806f });
                Poly(vh, u, M, crack, new[] { 322f, 906f, 288f, 956f, 310f, 1012f });
                Poly(vh, u, M, crack, new[] { 146f, 1122f, 188f, 1164f });
                Poly(vh, u, M, crack, new[] { 210f, 60f, 246f, 96f });
                break;
            }

            case GroundMarks.Cobble:
            {
                Color seam = new Color(.16f, .14f, .10f, .17f);
                for (int row = 0; row * 34 * u < r.height; row++)
                {
                    float y = row * 34f;
                    for (int i = 0; i < 8; i++)
                    {
                        float x = i * 54f + (row % 2) * 27f - 24f;
                        Box(vh, M(x, y), M(x + 50, y + 30), 1.3f * u, seam);
                    }
                }
                break;
            }
        }
    }

    private void Pebbles(VertexHelper vh, Rect r, float u, System.Func<float, float, Vector2> M)
    {
        Color c = pebble; c.a = .55f;
        int seed = 12345;
        for (int i = 0; i < 40; i++)
        {
            seed = seed * 1103515245 + 12345;
            float x = 14f + Mathf.Abs(seed / 65536 % 1000) / 1000f * 328f;
            seed = seed * 1103515245 + 12345;
            float y = 20f + Mathf.Abs(seed / 65536 % 1000) / 1000f * (r.height / u - 40f);
            seed = seed * 1103515245 + 12345;
            float rad = 2f + Mathf.Abs(seed / 65536 % 1000) / 1000f * 2.4f;
            MahalleGraphic.Disc(vh, M(x, y), rad * u, rad * .62f * u, c);
        }
    }

    private void Vignette(VertexHelper vh, Rect r)
    {
        Color edge = vignette, clear = vignette; clear.a = 0f;
        float v = r.width * .34f, hgt = Mathf.Min(r.height * .12f, r.width * .5f);
        MahalleGraphic.QuadBlend(vh, new Rect(r.xMin, r.yMax - hgt, r.width, hgt), edge, clear);
        MahalleGraphic.QuadBlend(vh, new Rect(r.xMin, r.yMin, r.width, hgt), clear, edge);
        Band(vh, new Rect(r.xMin, r.yMin, v, r.height), edge, clear);
        Band(vh, new Rect(r.xMax - v, r.yMin, v, r.height), clear, edge);
    }

    private static void Band(VertexHelper vh, Rect r, Color left, Color right)
    {
        MahalleGraphic.TriBlend(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), left, left, right);
        MahalleGraphic.TriBlend(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin), left, right, right);
    }

    private static void Circle(VertexHelper vh, Vector2 c, float radius, float width, Color col)
    {
        for (int i = 0; i < 48; i++)
        {
            float a = i * Mathf.PI * 2 / 48, b = (i + 1) * Mathf.PI * 2 / 48;
            MahalleGraphic.Stroke(vh, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                                      c + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width, col);
        }
    }

    private static void Box(VertexHelper vh, Vector2 a, Vector2 b, float width, Color col)
    {
        MahalleGraphic.Stroke(vh, new Vector2(a.x, a.y), new Vector2(b.x, a.y), width, col);
        MahalleGraphic.Stroke(vh, new Vector2(b.x, a.y), new Vector2(b.x, b.y), width, col);
        MahalleGraphic.Stroke(vh, new Vector2(b.x, b.y), new Vector2(a.x, b.y), width, col);
        MahalleGraphic.Stroke(vh, new Vector2(a.x, b.y), new Vector2(a.x, a.y), width, col);
    }

    private static void Poly(VertexHelper vh, float u, System.Func<float, float, Vector2> M, Color col, float[] pts)
    {
        for (int i = 0; i + 3 < pts.Length; i += 2)
            MahalleGraphic.Stroke(vh, M(pts[i], pts[i + 1]), M(pts[i + 2], pts[i + 3]), 1.6f * u, col);
    }
}
