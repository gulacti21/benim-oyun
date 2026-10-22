using System.Collections.Generic;
using UnityEngine;

// HARİTA 4 — LUNAPARK MEKANİKLERİ (2026-10-20). Tek yer: oyun ve fizik ölçüm araçları aynı kodu kullanır.
//
//   TAMPON (LunaparkBumper) — yuvarlak, çarpışan arabaların lastik tamponu. Çarpan misket
//       geldiğinden daha hızlı geri fırlar (BumperGain), en az BumperMin. Fizik geri çağrısı içinde
//       başka gövdeye dokunulmaz (BİLİNEN TUZAKLAR): çarpışma sadece kuyruğa yazar, hız bir sonraki
//       adımda FlushPending ile verilir (Catapat ile aynı düzen).
//   YÜRÜYEN BANT (ZoneKind.Belt) — GroundRules.Step: banttaki misket bandın yönünde BeltSpeed'e
//       kadar hızlanır. Bant bitince normal sürtünmeyle durur.
//   MIKNATIS MİSKET (LunaparkMagnet) — GroundRules.Step: kendisi neredeyse dururken, MagnetRadius
//       içinde HAREKET EDEN misketleri kendine çeker (yakında daha çok). Duran misketi çekmez:
//       böylece sahne her zaman durulur. 1 misket değerinde; önce mıknatısı çıkarmak işi kolaylaştırır.
//
// [ExecuteAlways]: ölçüm araçları görünmez preview sahnede aynı bileşenleri kullanır ve fizik
// mesajları sadece ExecuteAlways bileşenlere gelir.
public static class Lunapark
{
    // Tasarım değerleri. Bölüm hedefleri bunlarla ölçülür.
    // 2026-10-20 ikinci sürüm (kullanıcı: "mıknatıs, bant ne işe yarıyor anlamadım", "tampon kullanmadan da
    // oluyor"): ilk değerlerde etkiler gözle seçilmiyordu. Hepsi belirgin hale getirildi.
    public const float BumperGain = 1.5f;    // çıkış hızı = geliş × bu
    public const float BumperMin = 3.0f;     // m/s, hafif dokunuş bile en az bu hızla seker
    public const float BumperMax = 7.0f;     // m/s
    public const float BumperHeight = .45f;
    public const float BeltSpeed = 2.0f;     // m/s, bandın taşıma hızı
    public const float BeltAccel = 6.0f;     // m/s², banda giren misket bu ivmeyle hızlanır
    public const float MagnetRadius = 1.6f;  // dünya birimi (misket çapı .5); yerde kırmızı halka gösterir
    public const float MagnetPull = 9.0f;    // m/s², mıknatısın dibinde; kenarda sıfır
    public const float MagnetActiveSpeed = .3f;   // mıknatıs bundan hızlı giderken çekmez
    public const float MagnetMinTarget = .08f;    // bundan yavaş misket çekilmez (durulma)

    public static readonly Color BumperRed = new Color(.86f, .18f, .20f);
    public static readonly Color BumperWhite = new Color(.97f, .95f, .90f);
    public static readonly Color BumperRubber = new Color(.10f, .10f, .11f);
    public static readonly Color MagnetBody = new Color(.80f, .82f, .86f);   // gümüş
    public static readonly Color MagnetBand = new Color(.86f, .16f, .18f);   // kırmızı kutup
    public static readonly Color BeltColor = new Color(.20f, .21f, .24f, .95f);

    // Oyunda MarbleArena, ölçümde MemleketPhysicsVerify her fizik adımından sonra çağırır.
    public static void FlushPending() { LunaparkBumper.Flush(); }
    public static void ClearQueue() { LunaparkBumper.Clear(); }

    // GroundRules.Step'ten: yürüyen bant (tek gövde).
    public static Vector3 Belt(Vector3 v, float angle, float dt)
    {
        float a = angle * Mathf.Deg2Rad;
        var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
        float along = Vector3.Dot(v, dir);
        if (along >= BeltSpeed) return v;
        return v + dir * Mathf.Min(BeltAccel * dt, BeltSpeed - along);
    }

    // GroundRules.Step'ten: mıknatıslar hareket eden gövdeleri çeker.
    public static void Magnets(LevelData level, Vector3 center, IList<Rigidbody> targets, Rigidbody shooter, float dt)
    {
        var mags = LunaparkMagnet.Active;
        if (mags.Count == 0) return;
        float limit = level.arenaSize + .25f;
        for (int m = 0; m < mags.Count; m++)
        {
            var mg = mags[m];
            if (mg == null || mg.Body == null || mg.Body.isKinematic || !mg.gameObject.activeInHierarchy) continue;
            Vector3 mp = mg.Body.position;
            if (new Vector2(mp.x - center.x, mp.z - center.z).sqrMagnitude > limit * limit) continue;   // sahadan çıktı
            if (mg.Body.linearVelocity.magnitude > MagnetActiveSpeed) continue;
            var scene = mg.gameObject.scene;
            for (int i = 0; i <= targets.Count; i++)
            {
                var b = i < targets.Count ? targets[i] : shooter;
                if (b == null || b == mg.Body || b.isKinematic || b.gameObject.scene != scene) continue;
                Vector3 d = mp - b.position; d.y = 0f;
                float dist = d.magnitude;
                if (dist > MagnetRadius || dist < .001f) continue;
                Vector3 v = b.linearVelocity;
                if (new Vector2(v.x, v.z).magnitude < MagnetMinTarget) continue;
                float k = 1f - dist / MagnetRadius;
                b.linearVelocity = v + d / dist * (MagnetPull * k * dt);
            }
        }
    }

    public static bool HasMagnet(LevelData level)
    {
        if (level == null || level.marbles == null) return false;
        foreach (var s in level.marbles) if (s.kind == MarbleKind.Magnet) return true;
        return false;
    }

    // --- görünüm ---
    private static Material magnetMat;
    public static Material MagnetMaterial() => magnetMat != null ? magnetMat : (magnetMat = MakeMarble(MagnetBody, MagnetBand));
    private static Material MakeMarble(Color baseColor, Color swirl)
    {
        Shader shader = Resources.Load<Shader>("Mahalle/Marble");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return null;
        var m = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        m.SetColor("_BaseColor", baseColor);
        if (m.HasProperty("_SwirlColor")) m.SetColor("_SwirlColor", swirl);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .95f);
        var matcap = Resources.Load<Texture2D>("Mahalle/MarbleMatcap");
        if (matcap != null && m.HasProperty("_Matcap")) m.SetTexture("_Matcap", matcap);
        return m;
    }

    // Tamponu kurar (oyun ve ölçüm). Görünüm önce, collider sonra (BİLİNEN TUZAKLAR).
    // Cylinder kullanılmaz (telefonda sorun): gövde basık küre, çarpışma dikey kapsül.
    public static GameObject BuildBumper(Vector3 position, float diameter, Transform parent, Material red, Material white, Material black = null)
    {
        var root = new GameObject("Tampon");
        if (parent != null) root.transform.SetParent(parent, true);
        root.transform.position = new Vector3(position.x, 0f, position.z);
        if (red != null || white != null)
        {
            // Kırmızı misketle karışmasın (2026-10-21, ekranda görüldü): dışta kalın KARA lastik halka,
            // içte kırmızı gövde, tepede beyaz başlık. Toplam boyut collider ile aynı.
            var rim = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rim.name = "Tampon lastiği";
            Object.DestroyImmediate(rim.GetComponent<Collider>());
            rim.transform.SetParent(root.transform, false);
            rim.transform.localPosition = new Vector3(0f, BumperHeight * .3f, 0f);
            rim.transform.localScale = new Vector3(diameter, BumperHeight * .6f, diameter);
            rim.GetComponent<Renderer>().sharedMaterial = black;
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Tampon gövdesi";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, BumperHeight * .5f, 0f);
            body.transform.localScale = new Vector3(diameter * .74f, BumperHeight, diameter * .74f);
            body.GetComponent<Renderer>().sharedMaterial = red;
            var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "Tampon başlığı";
            Object.DestroyImmediate(cap.GetComponent<Collider>());
            cap.transform.SetParent(root.transform, false);
            cap.transform.localPosition = new Vector3(0f, BumperHeight * .82f, 0f);
            cap.transform.localScale = new Vector3(diameter * .4f, BumperHeight * .4f, diameter * .4f);
            cap.GetComponent<Renderer>().sharedMaterial = white;
        }
        var col = root.AddComponent<CapsuleCollider>();
        col.direction = 1;
        col.radius = diameter * .5f;
        col.height = Mathf.Max(diameter, 1.0f);
        col.center = new Vector3(0f, col.height * .5f - .05f, 0f);
        root.AddComponent<LunaparkBumper>();
        return root;
    }
}

[ExecuteAlways]
public class LunaparkBumper : MonoBehaviour
{
    private struct Hit { public Rigidbody body; public Vector3 center; public float approach; }
    private static readonly List<Hit> queue = new List<Hit>();

    private void OnCollisionEnter(Collision collision)
    {
        var b = collision.rigidbody;
        if (b == null || b.isKinematic) return;
        float approach = collision.relativeVelocity.magnitude;
        if (collision.contactCount > 0)
            approach = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(0).normal));
        if (approach < .05f) return;
        queue.Add(new Hit { body = b, center = transform.position, approach = approach });
    }

    public static void Flush()
    {
        if (queue.Count == 0) return;
        foreach (var h in queue)
        {
            var b = h.body;
            if (b == null || b.isKinematic) continue;
            Vector3 n = b.position - h.center; n.y = 0f;
            if (n.sqrMagnitude < 1e-6f) continue;
            n.Normalize();
            Vector3 v = b.linearVelocity;
            Vector3 tangent = v - n * Vector3.Dot(v, n); tangent.y = 0f;
            float outSpeed = Mathf.Clamp(h.approach * Lunapark.BumperGain, Lunapark.BumperMin, Lunapark.BumperMax);
            b.WakeUp();
            b.linearVelocity = new Vector3(tangent.x + n.x * outSpeed, v.y, tangent.z + n.z * outSpeed);
            if (Application.isPlaying)
            {
                if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlaySplit(.6f);
                MahalleFeedback.Tap();
            }
        }
        queue.Clear();
    }

    public static void Clear() { queue.Clear(); }
}

[ExecuteAlways]
[RequireComponent(typeof(Rigidbody))]
public class LunaparkMagnet : MonoBehaviour
{
    public static readonly List<LunaparkMagnet> Active = new List<LunaparkMagnet>();
    public Rigidbody Body { get; private set; }
    private void OnEnable() { Body = GetComponent<Rigidbody>(); if (!Active.Contains(this)) Active.Add(this); }
    private void OnDisable() { Active.Remove(this); }
}

// Mıknatısın çekim alanı: yerde, mıknatısı izleyen soluk kırmızı halka (misketle dönmesin diye ayrı nesne).
// Mıknatıs sahadan çıkınca (limit) ya da yok olunca halka da kaybolur.
public class LunaparkMagnetRing : MonoBehaviour
{
    public Transform target;
    public Vector3 center;
    public float limit;
    private LineRenderer line;

    public static void Attach(Transform magnet, Vector3 arenaCenter, float arenaLimit, Material chalk)
    {
        if (chalk == null) return;
        var go = new GameObject("Mıknatıs alanı");
        var ring = go.AddComponent<LunaparkMagnetRing>();
        ring.target = magnet; ring.center = arenaCenter; ring.limit = arenaLimit;
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = chalk;
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.widthMultiplier = .045f;
        var col = new Color(Lunapark.MagnetBand.r, Lunapark.MagnetBand.g, Lunapark.MagnetBand.b, .55f);
        lr.startColor = col; lr.endColor = col;
        const int seg = 48;
        lr.positionCount = seg;
        for (int i = 0; i < seg; i++)
        {
            float a = i * Mathf.PI * 2f / seg;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * Lunapark.MagnetRadius, 0f, Mathf.Sin(a) * Lunapark.MagnetRadius));
        }
        ring.line = lr;
        ring.LateUpdate();
    }

    private void LateUpdate()
    {
        if (target == null || !target.gameObject.activeInHierarchy) { Destroy(gameObject); return; }
        Vector3 p = target.position;
        bool inside = new Vector2(p.x - center.x, p.z - center.z).sqrMagnitude <= limit * limit;
        if (line != null) line.enabled = inside;
        transform.position = new Vector3(p.x, .025f, p.z);
    }
}
