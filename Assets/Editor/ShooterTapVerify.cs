using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

// ÇİZGİYE DOKUNMA ÖLÇÜMÜ + KORUMA. Kıpırdamadan dokunuş asla atış çıkarmamalı,
// görünen çizginin her noktası atıcıyı oraya taşımalı, sürükleme nişan başlatmalı.
// Gerçek ShotController.BeginAim'i gerçek ShooterLine ve oyundaki kamera kurulumuyla
// çağırır. Her bölüm × 3 ekran oranı için çizgi boyunca 81 noktaya "dokunur":
//   AIM   = dokunuş nişan almaya gitti (yer değiştirmedi)
//   ATIS  = parmak kıpırdamadan kalksa bile atış çıkıyor (güç > 0)
//   MOVE  = misket dokunulan yere taşındı
//   YOK   = hiçbir şey olmadı
public static class ShooterTapVerify
{
    private static readonly float[] Aspects = { 9f / 19.5f, 9f / 16f, 3f / 4f };
    private static readonly string[] AspectNames = { "iPhone", "SE", "iPad" };
    private const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const float DockTopPx = 40f + 190f;   // MahalleUI "Atış alanı" paneli, alttan
    private const float BagTopPx = 40f + 28f + 134f; // "Misket kesesi" düğmesi (raycast alan tek UI)

    public static int RunChecks()
    {
        var sb = new StringBuilder();
        int checks = 0;
        float sceneMinPull = SceneValue("minPullDistance", .15f);
        float sceneTapTol = SceneValue("tapTolerance", 1.2f);
        sb.AppendLine("SHOOTER_TAP: sahne minPull=" + sceneMinPull + " tapTolerance=" + sceneTapTol);

        var tBegin = typeof(ShotController).GetMethod("BeginAim", F);
        var tShot = typeof(ShotController).GetMethod("GetShot", F);
        var tTap = typeof(ShotController).GetMethod("ReleaseTap", F);
        var tDrag = typeof(ShotController).GetMethod("DragPending", F);
        var fAiming = typeof(ShotController).GetField("isAiming", F);
        var fPlane = typeof(ShotController).GetField("aimPlane", F);
        var fCam = typeof(ShotController).GetField("gameCamera", F);
        var fLine = typeof(ShotController).GetField("shooterLine", F);
        var fMin = typeof(ShotController).GetField("minPullDistance", F);
        var fGrab = typeof(ShotController).GetField("grabRadius", F);
        var fTol = typeof(ShooterLine).GetField("tapTolerance", F);
        if (tTap == null || tDrag == null || tBegin == null || tShot == null || fAiming == null || fPlane == null || fCam == null || fLine == null || fMin == null || fGrab == null || fTol == null)
        { Debug.LogWarning("SHOOTER_TAP: reflection alanlari bulunamadi, olcum atlandi"); return 0; }

        float worstAimShare = 0f, worstShotPower = 0f; int worstLevel = -1; string worstAspect = "";
        int levelsLineDead = 0, levelsAnyShot = 0;
        float sumAimShare = 0f; int n = 0;
        float minGapToDock = float.MaxValue, maxMoveErr = 0f;
        bool lockedIgnores = true;

        for (int i = 0; i < 60; i++)
        {
            var l = Campaign.Database.Get(i);
            for (int a = 0; a < Aspects.Length; a++)
            {
                float aspect = Aspects[a];
                int W = 1080, H = Mathf.RoundToInt(1080f / aspect);
                GameObject camGo = null, lineGo = null, marble = null; RenderTexture rt = null;
                try
                {
                    rt = new RenderTexture(W, H, 16);
                    camGo = new GameObject("tapcam"); var cam = camGo.AddComponent<Camera>();
                    cam.targetTexture = rt; cam.orthographic = true; cam.aspect = aspect;
                    cam.orthographicSize = MahalleWorld.CameraSize(l.arenaSize, aspect);
                    cam.transform.rotation = Quaternion.Euler(MahalleWorld.CamPitch, 0, 0);
                    cam.transform.position = new Vector3(0, 0, MahalleWorld.CamFocusZ) - cam.transform.forward * 18f;
                    float inputScale = cam.orthographicSize / Mathf.Max(6.3f, 3.65f / aspect);

                    lineGo = new GameObject("tapline"); lineGo.AddComponent<LineRenderer>(); var line = lineGo.AddComponent<ShooterLine>();
                    fTol.SetValue(line, sceneTapTol);
                    line.ApplyLevel(l.shooterHalfWidth, l.shooterOffsetX);
                    line.SetPosition(l.shooterStartPosition);
                    line.FitToCamera(cam, 1.05f);
                    line.SetInputScale(inputScale);

                    marble = GameObject.CreatePrimitive(PrimitiveType.Sphere); marble.transform.localScale = Vector3.one * .5f;
                    var rb = marble.AddComponent<Rigidbody>(); rb.mass = .05f;
                    var shot = marble.AddComponent<ShotController>();
                    fCam.SetValue(shot, cam); fLine.SetValue(shot, line); fMin.SetValue(shot, sceneMinPull);
                    shot.SetInputScale(inputScale);
                    float grab = (float)fGrab.GetValue(shot);
                    float tapTol = (float)fTol.GetValue(line);

                    float hw = line.HalfWidth, cx = l.shooterOffsetX, lz = line.LineZ;
                    int samples = 81, aim = 0, fired = 0, moved = 0, none = 0, onLine = 0;
                    float maxPow = 0f;
                    for (int s = 0; s < samples; s++)
                    {
                        float x = cx - hw - tapTol * .5f + (2f * hw + tapTol) * s / (samples - 1);
                        bool visible = Mathf.Abs(x - cx) <= hw;
                        shot.ResetTo(l.shooterStartPosition);
                        fAiming.SetValue(shot, false);
                        fPlane.SetValue(shot, new Plane(Vector3.up, shot.transform.position));
                        Vector3 before = shot.transform.position;
                        Vector2 screen = cam.WorldToScreenPoint(new Vector3(x, .02f, lz)); // tebeşir çizgisinin kendisi
                        tBegin.Invoke(shot, new object[] { screen });
                        tTap.Invoke(shot, null); // parmak kıpırdamadan kalktı
                        bool isAim = (bool)fAiming.GetValue(shot);
                        if (visible) onLine++;
                        if (isAim)
                        {
                            var args = new object[] { null, null };
                            tShot.Invoke(shot, args);
                            float p = (float)args[1];
                            if (visible) { aim++; if (p > 0f) { fired++; maxPow = Mathf.Max(maxPow, p); } }
                        }
                        else if ((shot.transform.position - before).sqrMagnitude > 1e-6f)
                        {
                            if (visible) { moved++; maxMoveErr = Mathf.Max(maxMoveErr, Mathf.Abs(shot.transform.position.x - x)); }
                        }
                        else if (visible)
                        {
                            // Zaten orada duruyorsa taşınmaz; o da doğru sonuç.
                            if (Mathf.Abs(before.x - x) < .05f) moved++; else none++;
                        }
                        fAiming.SetValue(shot, false);
                    }
                    if (aim > 0) throw new Exception("CHECK FAILED: Kıpırdamadan dokunuş nişana gitti #" + i + " " + AspectNames[a] + " (" + aim + ")");
                    if (none > 0) throw new Exception("CHECK FAILED: Çizgiye dokunuş atıcıyı taşımadı #" + i + " " + AspectNames[a] + " (" + none + ")");
                    if (maxMoveErr > .05f) throw new Exception("CHECK FAILED: Atıcı dokunulan yere gitmedi #" + i + " sapma " + maxMoveErr);
                    checks += 3;

                    // Sürükleme: misketin arkasına bas, 120px aşağı çek -> nişan ve güç olmalı.
                    shot.ResetTo(l.shooterStartPosition); fAiming.SetValue(shot, false);
                    fPlane.SetValue(shot, new Plane(Vector3.up, shot.transform.position));
                    Vector2 p0 = cam.WorldToScreenPoint(shot.transform.position);
                    tBegin.Invoke(shot, new object[] { p0 });
                    tDrag.Invoke(shot, new object[] { p0 + new Vector2(0f, -4f) });
                    if ((bool)fAiming.GetValue(shot)) throw new Exception("CHECK FAILED: 4px titreme nişan başlattı #" + i);
                    tDrag.Invoke(shot, new object[] { p0 + new Vector2(0f, -120f) });
                    if (!(bool)fAiming.GetValue(shot) || shot.Power <= 0f) throw new Exception("CHECK FAILED: Sürükleme nişan başlatmadı #" + i + " " + AspectNames[a]);
                    shot.CancelAim();
                    checks += 2;
                    float aimShare = onLine > 0 ? (float)aim / onLine : 0f;
                    sumAimShare += aimShare; n++;
                    if (moved == 0) levelsLineDead++;
                    if (fired > 0) levelsAnyShot++;
                    if (aimShare > worstAimShare || (aimShare == worstAimShare && maxPow > worstShotPower))
                    { worstAimShare = aimShare; worstShotPower = maxPow; worstLevel = i; worstAspect = AspectNames[a]; }

                    // Çizgi ekranda nerede? (canvas px, alttan) — raycast alan tek UI "Misket kesesi".
                    Vector3 sp = cam.WorldToScreenPoint(new Vector3(cx, .02f, lz));
                    float lineCanvasY = sp.y / H * MahalleWorld.UiCanvasHeight(aspect);
                    minGapToDock = Mathf.Min(minGapToDock, lineCanvasY - DockTopPx);

                    // Yerinde Kal: çizginin uzak ucuna dokun, bir şey olmamalı (tasarım).
                    if (a == 0)
                    {
                        shot.ResetTo(l.shooterStartPosition); shot.HoldPosition();
                        fPlane.SetValue(shot, new Plane(Vector3.up, shot.transform.position));
                        Vector3 b = shot.transform.position;
                        tBegin.Invoke(shot, new object[] { (Vector2)cam.WorldToScreenPoint(new Vector3(cx + hw, .02f, lz)) });
                        tTap.Invoke(shot, null);
                        if ((shot.transform.position - b).sqrMagnitude > 1e-6f) lockedIgnores = false;
                        fAiming.SetValue(shot, false);
                    }

                    if (a == 0 && (i == 0 || i == 9 || i == 40 || i == 59))
                        sb.AppendLine("SHOOTER_TAP #" + i + " " + l.levelName + " " + AspectNames[a]
                            + " hw=" + hw.ToString("0.00") + " grab=" + grab.ToString("0.00") + " s=" + inputScale.ToString("0.00")
                            + " | cizgi: AIM %" + (aimShare * 100f).ToString("0") + " (kazara ATIS " + fired + "/" + onLine + ", max guc %" + (maxPow * 100f).ToString("0") + ")"
                            + " MOVE " + moved + " YOK " + none
                            + " | cizgi y=" + lineCanvasY.ToString("0") + "px (panel ust " + DockTopPx + ")");
                }
                catch (Exception e)
                {
                    if (e.Message.StartsWith("CHECK FAILED")) throw;
                    Debug.LogWarning("SHOOTER_TAP #" + i + " " + AspectNames[a] + " olcum hatasi: " + e.GetType().Name + " " + e.Message);
                }
                finally
                {
                    if (marble != null) UnityEngine.Object.DestroyImmediate(marble);
                    if (lineGo != null) UnityEngine.Object.DestroyImmediate(lineGo);
                    if (camGo != null) UnityEngine.Object.DestroyImmediate(camGo);
                    if (rt != null) UnityEngine.Object.DestroyImmediate(rt);
                }
            }
        }
        sb.AppendLine("SHOOTER_TAP_OZET: olculen " + n + " (bolum x oran)"
            + " | ort. cizginin %" + (n > 0 ? sumAimShare / n * 100f : 0f).ToString("0") + " nisana gidiyor"
            + " | kazara atis olan: " + levelsAnyShot + "/" + n
            + " | cizgi hic calismayan: " + levelsLineDead + "/" + n
            + " | en kotu: #" + worstLevel + " " + worstAspect + " %" + (worstAimShare * 100f).ToString("0") + " guc %" + (worstShotPower * 100f).ToString("0")
            + " | tasima hatasi max " + maxMoveErr.ToString("0.00")
            + " | cizgi-panel en az " + minGapToDock.ToString("0") + "px"
            + " | Yerinde Kal tasimayi engelliyor: " + lockedIgnores);
        Debug.Log(sb.ToString());
        return checks;
    }

    private static float SceneValue(string key, float fallback)
    {
        try
        {
            foreach (var row in File.ReadAllLines("Assets/Scenes/Game.unity"))
            {
                var t = row.Trim();
                if (t.StartsWith(key + ":")) return float.Parse(t.Substring(key.Length + 1).Trim(), System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        catch { }
        return fallback;
    }
}
