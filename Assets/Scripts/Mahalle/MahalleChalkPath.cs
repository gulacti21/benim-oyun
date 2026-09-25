using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Durakları birbirine bağlayan, elle çizilmiş izlenimi veren kesik çizgi.
// Tebeşir mi, saha boyası mı, çubukla kazınmış iz mi olduğuna tema karar verir.
[RequireComponent(typeof(CanvasRenderer))]
public class MahalleChalkPath : MaskableGraphic
{
    public Vector2[] stops;
    public float lineWidth = 9f, dashOn = 33f, dashOff = 40f;
    public float lineAlpha = .55f, dustAlpha = .16f;
    public float leadIn = 200f, leadOut = 220f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (stops == null || stops.Length < 2) return;

        List<Vector2> line = Sample();
        Color dust = color; dust.a = dustAlpha;
        Color ink = color; ink.a = lineAlpha;

        for (int i = 1; i < line.Count; i++)
            MahalleGraphic.Stroke(vh, line[i - 1], line[i], lineWidth * 3f, dust);

        Dashes(vh, line, ink);
    }

    private List<Vector2> Sample()
    {
        var line = new List<Vector2>();
        Vector2 first = stops[0], last = stops[stops.Length - 1];
        line.Add(new Vector2(first.x, first.y + leadIn));

        for (int i = 0; i < stops.Length - 1; i++)
        {
            Vector2 a = stops[i], b = stops[i + 1];
            float my = (a.y + b.y) * .5f;
            Vector2 c1 = new Vector2(a.x, my), c2 = new Vector2(b.x, my);
            for (int s = 1; s <= 26; s++)
            {
                float t = s / 26f, it = 1f - t;
                Vector2 p = it * it * it * a + 3f * it * it * t * c1 + 3f * it * t * t * c2 + t * t * t * b;
                // Çizgi cetvelle değil elle çizilmiş gibi hafif titrer.
                p.x += Mathf.Sin((i * 26 + s) * .9f) * lineWidth * .18f;
                line.Add(p);
            }
        }
        line.Add(new Vector2(last.x, last.y - leadOut));
        return line;
    }

    private void Dashes(VertexHelper vh, List<Vector2> line, Color ink)
    {
        bool on = true;
        float acc = 0f, need = dashOn;
        int tick = 0;

        for (int i = 1; i < line.Count; i++)
        {
            Vector2 a = line[i - 1], b = line[i];
            float seg = Vector2.Distance(a, b);
            if (seg < .001f) continue;
            float walked = 0f;

            while (walked < seg)
            {
                float step = Mathf.Min(need - acc, seg - walked);
                if (step <= .0001f) break;
                if (on)
                {
                    Vector2 p1 = Vector2.Lerp(a, b, walked / seg);
                    Vector2 p2 = Vector2.Lerp(a, b, (walked + step) / seg);
                    float w = lineWidth * (.82f + .34f * Mathf.Abs(Mathf.Sin(tick * 1.7f)));
                    MahalleGraphic.Stroke(vh, p1, p2, w, ink);
                }
                walked += step; acc += step;
                if (acc >= need - .001f) { on = !on; acc = 0f; need = on ? dashOn : dashOff; tick++; }
            }
        }
    }
}
