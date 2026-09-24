using System.Collections.Generic;
using UnityEngine;

// HARİTA 2: bölümün zemin bölgelerini (kum/çamur/çukur) ve eğimini sahaya kurar,
// her fizik adımından önce GroundRules.Step'i çalıştırır. Harita 1 bölümlerinde
// kural yoksa hiçbir şey yapmaz ve hiçbir şey çizmez.
public class GroundZones : MonoBehaviour
{
    private LevelData level;
    private MarbleArena arena;
    private Rigidbody shooter;
    private readonly List<Rigidbody> bodies = new List<Rigidbody>();
    private readonly Dictionary<Rigidbody, TargetMarble> owners = new Dictionary<Rigidbody, TargetMarble>();
    private readonly List<GameObject> drawn = new List<GameObject>();
    private static Material zoneBase;

    public static void Setup(MarbleArena arena, Rigidbody shooter, LevelData level)
    {
        if (arena == null) return;
        var zones = arena.GetComponent<GroundZones>();
        if (zones == null)
        {
            if (!GroundRules.HasRules(level)) return;
            zones = arena.gameObject.AddComponent<GroundZones>();
        }
        zones.arena = arena; zones.shooter = shooter; zones.level = level;
        zones.Redraw();
    }

    private void FixedUpdate()
    {
        if (!GroundRules.HasRules(level) || arena == null) return;
        bodies.Clear(); owners.Clear();
        var list = arena.SpawnedMarbles;
        for (int i = 0; i < list.Count; i++)
        {
            var m = list[i];
            if (m == null || m.IsScored || m.Body == null) continue;
            bodies.Add(m.Body); owners[m.Body] = m;
        }
        GroundRules.Step(level, arena.transform.position, bodies, shooter, Time.fixedDeltaTime, OnCaptured);
    }

    private void OnCaptured(Rigidbody body)
    {
        if (owners.TryGetValue(body, out var marble)) arena.Capture(marble);
        if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlayPit();
    }

    private void Redraw()
    {
        ReleaseDrawn();
        if (!GroundRules.HasRules(level)) return;
        Vector3 c = arena.transform.position;
        if (level.zones != null)
            foreach (var z in level.zones) drawn.Add(Disc(z, c));
        if (level.slope.sqrMagnitude > 1e-6f) SlopeArrows(c);
    }

    private static Material ZoneMaterial()
    {
        if (zoneBase != null) return zoneBase;
        var shader = Resources.Load<Shader>("Mahalle/Zone");
        if (shader == null || !shader.isSupported) shader = Shader.Find("MISKETR/Zone");
        if (shader == null) return null;
        zoneBase = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        return zoneBase;
    }

    private static readonly Color SandColor = new Color(.90f, .80f, .58f, .88f);
    private static readonly Color MudColor = new Color(.30f, .22f, .15f, .90f);
    private static readonly Color PitColor = new Color(.42f, .32f, .22f, .92f);

    private GameObject Disc(ZoneSpot z, Vector3 center)
    {
        var mat = ZoneMaterial();
        // Görünüm önce, collider sonra (BİLİNEN TUZAKLAR). Quad: Cylinder kullanılmıyor.
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = z.kind == ZoneKind.Sand ? "Kum" : z.kind == ZoneKind.Mud ? "Çamur" : "Çukur";
        go.transform.SetParent(transform, true);
        go.transform.position = new Vector3(center.x + z.x, .012f + (int)z.kind * .002f, center.z + z.z);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = new Vector3(z.radius * 2f, z.radius * 2f, 1f);
        var r = go.GetComponent<Renderer>();
        if (mat != null)
        {
            var own = new Material(mat) { hideFlags = HideFlags.HideAndDontSave };
            own.SetColor("_Color", z.kind == ZoneKind.Sand ? SandColor : z.kind == ZoneKind.Mud ? MudColor : PitColor);
            own.SetFloat("_Kind", (int)z.kind);
            r.sharedMaterial = own;
        }
        else r.enabled = false;   // shader yoksa mor kare çizme
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var col = go.GetComponent<Collider>(); if (col != null) DestroyImmediate(col);
        return go;
    }

    // Eğim: çemberin dışında, eğim yönünü gösteren üç soluk tebeşir oku.
    private void SlopeArrows(Vector3 center)
    {
        var chalk = arena.GetComponent<LineRenderer>();
        Material mat = chalk != null ? chalk.sharedMaterial : null;
        if (mat == null) return;
        Vector3 dir = new Vector3(level.slope.x, 0f, level.slope.y).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, dir);
        float reach = arena.Size + .55f;
        for (int k = -1; k <= 1; k++)
        {
            var go = new GameObject("Eğim oku");
            go.transform.SetParent(transform, true);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat;
            lr.useWorldSpace = true;
            lr.widthMultiplier = .06f;
            var col = new Color(1f, 1f, 1f, .45f); lr.startColor = col; lr.endColor = col;
            Vector3 tip = center + dir * reach + side * (k * .9f) + Vector3.up * .02f;
            lr.positionCount = 3;
            lr.SetPosition(0, tip - dir * .28f + side * .22f);
            lr.SetPosition(1, tip);
            lr.SetPosition(2, tip - dir * .28f - side * .22f);
            drawn.Add(go);
        }
    }

    // Çizilenleri ve her diskin kendi materyal kopyasını bırakır (paylaşılan tebeşire dokunmaz).
    private void ReleaseDrawn()
    {
        foreach (var go in drawn)
            if (go != null)
            {
                var r = go.GetComponent<Renderer>();
                if (r != null && !(r is LineRenderer) && r.sharedMaterial != null && r.sharedMaterial != zoneBase) Destroy(r.sharedMaterial);
                Destroy(go);
            }
        drawn.Clear();
    }

    private void OnDestroy() { ReleaseDrawn(); }
}
