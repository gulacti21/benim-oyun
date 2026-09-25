using System;
using System.Text;
using UnityEngine;

// HARITA YERLESIMI: 12 durak ekrana sigiyor mu, ust uste biniyor mu,
// yazilar ekran disina tasiyor mu, son durak alt dugmelerin arkasinda mi?
// Ekran gorunmeden olculebilen her seyi burada olcuyoruz.
public static class MapLayoutVerify
{
    private const float ScreenHalf = 540f;
    private const float PlayDockTop = 296f;   // OYNA dugmesinin ust kenari (alttan)
    private const float HeaderBottom = 300f;  // basligin alt kenari (ustten)

    public static int RunChecks()
    {
        int checks = 0;
        var report = new StringBuilder("MAP_LAYOUT: ");
        Vector2 size = MahalleMapView.NodeSize;
        float contentTop = MahalleMapView.ContentHeight * .5f;

        for (int local = 0; local < 12; local++)
        {
            Vector2 c = MahalleMapView.NodeCenter(local);

            // 1) durak kutusu yatayda ekrana sigar
            float lo = c.x - size.x * .5f, hi = c.x + size.x * .5f;
            checks++;
            if (lo < -ScreenHalf - 1f || hi > ScreenHalf + 1f)
                throw new Exception("CHECK FAILED: Durak " + local + " yatayda tasiyor (" + lo.ToString("0") + ".." + hi.ToString("0") + ")");

            // 2) yazi blogunun uzak kenari ekran icinde
            float edge = c.x + MahalleMapView.TextFarEdge(local);
            checks++;
            if (Mathf.Abs(edge) > ScreenHalf + 1f)
                throw new Exception("CHECK FAILED: Durak " + local + " yazisi ekran disinda (" + edge.ToString("0") + ")");

            // 3) cember ekran kenarina yapismaz
            float cx = c.x + (MahalleMapView.LeftSide(local) ? -190f : 190f);
            checks++;
            if (Mathf.Abs(cx) + MahalleMapView.RingDiameter * .5f > ScreenHalf - 8f)
                throw new Exception("CHECK FAILED: Durak " + local + " cemberi kenara cok yakin");

            // 4) komsu duraklar dikeyde ust uste binmez
            if (local > 0)
            {
                float gap = Mathf.Abs(MahalleMapView.NodeCenter(local - 1).y - c.y);
                checks++;
                if (gap < size.y + 20f)
                    throw new Exception("CHECK FAILED: Durak " + local + " ile " + (local - 1) + " ust uste (" + gap.ToString("0") + ")");
            }
        }

        // 5) ilk durak basligin altinda kalir
        float firstTop = contentTop - (MahalleMapView.NodeCenter(0).y + MahalleMapView.ContentHeight * 0f);
        firstTop = contentTop - MahalleMapView.NodeCenter(0).y - MahalleMapView.NodeSize.y * .5f;
        checks++;
        if (firstTop < HeaderBottom - 1f)
            throw new Exception("CHECK FAILED: Ilk durak basligin altinda kalmiyor (" + firstTop.ToString("0") + ")");

        // 6) son durak icerigin altinda OYNA dugmesi kadar pay birakir
        float lastBottom = contentTop - MahalleMapView.NodeCenter(11).y + MahalleMapView.NodeSize.y * .5f;
        float tail = MahalleMapView.ContentHeight - lastBottom;
        checks++;
        if (tail < PlayDockTop)
            throw new Exception("CHECK FAILED: Son durak alt dugmelerin arkasinda kaliyor (pay " + tail.ToString("0") + ")");

        report.Append("12 durak, ust pay " + firstTop.ToString("0") + "px, alt pay " + tail.ToString("0") + "px");
        Debug.Log(report.ToString());
        return checks;
    }
}
