using System;
using System.Text;
using UnityEngine;

// KAMERA KIRPMASI: 60 bölümün her birinde çember + çizgi dışı payı, telefon
// ekranlarında hem yanlardan hem üst başlığın altından görünüyor mu?
// Kamera kurulumundan bağımsız hesapla ölçer (ortografik, 72 derece eğik).
public static class CameraFitVerify
{
    private static readonly float[] Aspects = { 9f / 19.5f, 9f / 16f, 3f / 4f }; // iPhone çentikli, iPhone SE, iPad
    private static readonly string[] AspectNames = { "iPhone 9:19.5", "iPhone SE 9:16", "iPad 3:4" };

    public static int RunChecks()
    {
        int checks = 0;
        var report = new StringBuilder("CAMERA_FIT: ");
        int changed = 0;
        float sin = Mathf.Sin(MahalleWorld.CamPitch * Mathf.Deg2Rad);
        // İki harita: 0-59 Mahalle, 60-119 Memleket.
        for (int i = 0; i < Maps.TotalLevels; i++)
        {
            var l = Maps.Get(i);
            for (int a = 0; a < Aspects.Length; a++)
            {
                float aspect = Aspects[a];
                float size = MahalleWorld.CameraSize(l.arenaSize, aspect);
                float halfWidth = size * aspect;
                bool sides = halfWidth + 1e-4f >= l.arenaSize + MahalleWorld.SideMargin;
                float cover = (MahalleWorld.HudTopRef + MahalleWorld.HudGap) / (1080f / aspect) + MahalleWorld.SafeTopFraction;
                float visibleTop = size * (1f - 2f * cover);
                float farEdge = (l.arenaSize + MahalleWorld.TopMargin - MahalleWorld.CamFocusZ) * sin;
                bool top = visibleTop + 1e-4f >= farEdge;
                float legacy = Mathf.Max(6.3f, 3.65f / aspect);
                bool notCloser = size + 1e-4f >= legacy;
                checks += 3;
                if (!sides) throw new Exception("CHECK FAILED: Camera shows ring sides " + i + " " + AspectNames[a]);
                if (!top) throw new Exception("CHECK FAILED: Camera shows ring top under HUD " + i + " " + AspectNames[a]);
                if (!notCloser) throw new Exception("CHECK FAILED: Camera never closer than before " + i);
                if (a == 0 && size > legacy + 1e-3f)
                {
                    changed++;
                    report.Append("#" + i + " " + l.levelName + " r=" + l.arenaSize.ToString("0.00") + " zoom " + legacy.ToString("0.00") + "->" + size.ToString("0.00") + "; ");
                }
            }
        }
        Debug.Log(report.Append("degisen bolum (iPhone): " + changed).ToString());
        return checks;
    }
}
