using System;
using UnityEngine;

// BUZLU MİSKET (Harita 2, Yayla).
//   - Buz kabuğu içinde başlar, hiç kıpırdamaz (kinematic). Çarpan misket ondan
//     duvardan seker gibi seker.
//   - Çarpışmanın temas doğrultusundaki hızı BreakSpeed'i geçerse kabuk kırılır,
//     ama misket YERİNDE kalır (kinematic iken çarpıldığı için hız almaz).
//   - Kırıldıktan sonra normal hedef misket; bir sonraki vuruşta hareket eder.
//   - Zayıf darbe (eşiğin altı) hiçbir şey yapmaz: kısa bir tık ve çatlak parıltısı.
//
// [ExecuteAlways]: fizik ölçüm araçları görünmez preview sahnede aynı bileşeni
// kullanır; edit modda da OnCollisionEnter gelsin diye (ölçüldü: gelmiyordu).
[ExecuteAlways]
[RequireComponent(typeof(Rigidbody))]
public class IceShell : MonoBehaviour
{
    // ÖLÇÜLDÜ (IceMarbleVerify.Measure): temas doğrultusundaki bağıl hız, m/s.
    // Gerekçe ve tablo: YAZ_TATILI_ILERLEME.md, Faz 2.
    public const float BreakSpeed = 3.0f;
    public const float ShellScale = 1.24f;   // misket çapına göre kabuk

    public bool Intact { get; private set; }
    public int WeakHits { get; private set; }
    // Son çarpışmanın temas doğrultusundaki hızı (ölçüm aracı eşiği bununla seçer).
    public float LastHitSpeed { get; private set; }
    // Ölçüm aracı için: hızı kaydet ama hiç kırılma (eşik seçilirken).
    public bool MeasureOnly { get; set; }
    public event Action<IceShell> Broken;

    private Rigidbody body;
    private CollisionDetectionMode detection = CollisionDetectionMode.ContinuousDynamic;
    private GameObject shell;
    private Material shellMaterial;
    private float crack;
    private static Material sharedIce;

    private Rigidbody Body { get { if (body == null) body = GetComponent<Rigidbody>(); return body; } }

    // Kabuğu kur (ya da ölçüm aracında sıfırla). visual=false: fizik ölçümünde görüntü yok.
    public void Freeze(bool visual)
    {
        var b = Body;
        if (!Intact && b.collisionDetectionMode != CollisionDetectionMode.ContinuousSpeculative)
            detection = b.collisionDetectionMode;
        Intact = true; WeakHits = 0; crack = 0f; LastHitSpeed = 0f;
        if (!b.isKinematic) { b.linearVelocity = Vector3.zero; b.angularVelocity = Vector3.zero; }
        // Kinematic gövdede ContinuousDynamic desteklenmez; speculative hızlı misketi kaçırmaz.
        b.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        b.isKinematic = true;
        if (visual) BuildShell();
    }

    // Ölçüm aracı durumu geri sararken: kabuksuz normal misket.
    public void Thaw()
    {
        var b = Body;
        Intact = false;
        b.isKinematic = false;
        b.collisionDetectionMode = detection;
        if (shell != null) shell.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!Intact) return;
        float speed = collision.relativeVelocity.magnitude;
        if (collision.contactCount > 0)
            speed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(0).normal));
        LastHitSpeed = Mathf.Max(LastHitSpeed, speed);
        if (MeasureOnly) return;
        if (speed >= BreakSpeed) Break(speed);
        else if (speed > .4f) Weak(speed);
    }

    private void Break(float speed)
    {
        Thaw();
        var b = Body;
        b.linearVelocity = Vector3.zero; b.angularVelocity = Vector3.zero;
        if (Application.isPlaying)
        {
            if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlayIceBreak(Mathf.InverseLerp(BreakSpeed, BreakSpeed * 2.5f, speed));
            MahalleFeedback.Tap();
            Shards();
        }
        Broken?.Invoke(this);
    }

    private void Weak(float speed)
    {
        WeakHits++;
        if (!Application.isPlaying) return;
        crack = 1f;
        if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlayIceTick(speed / BreakSpeed);
    }

    private void Update()
    {
        if (!Application.isPlaying || shellMaterial == null || crack <= 0f) return;
        crack = Mathf.Max(0f, crack - Time.deltaTime * 2.2f);
        shellMaterial.SetFloat("_Crack", crack);
    }

    private static Material IceMaterial()
    {
        if (sharedIce != null) return sharedIce;
        var shader = Resources.Load<Shader>("Mahalle/Ice");
        if (shader == null || !shader.isSupported) shader = Shader.Find("MISKETR/Ice");
        if (shader == null) return null;
        sharedIce = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        return sharedIce;
    }

    private void BuildShell()
    {
        if (shell != null) { shell.SetActive(true); return; }
        var mat = IceMaterial();
        if (mat == null) return;   // shader yoksa kabuğu hiç çizme (mor küre yerine)
        // Görünüm önce, collider sonra (BİLİNEN TUZAKLAR): collider silinmesi patlasa bile
        // kabuk konumlu, ölçekli ve materyalli kalsın.
        shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.name = "Buz kabuğu";
        shell.transform.SetParent(transform, false);
        shell.transform.localPosition = Vector3.zero;
        shell.transform.localScale = Vector3.one * ShellScale;
        var r = shell.GetComponent<Renderer>();
        // Her misketin kendi kopyası: çatlak parıltısı sadece vurulanda görünsün.
        shellMaterial = new Material(mat) { hideFlags = HideFlags.HideAndDontSave };
        r.sharedMaterial = shellMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var c = shell.GetComponent<Collider>();
        if (c != null) DestroyImmediate(c);   // hemen: kabuk gövdenin bileşik collider'ı olmasın
    }

    private void Shards()
    {
        var mat = IceMaterial();
        if (mat == null) return;
        for (int i = 0; i < 7; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Buz parçası";
            go.GetComponent<Renderer>().sharedMaterial = mat;
            float a = i / 7f * Mathf.PI * 2f;
            go.transform.position = transform.position + new Vector3(Mathf.Cos(a), .4f, Mathf.Sin(a)) * .18f;
            go.transform.rotation = UnityEngine.Random.rotation;
            go.transform.localScale = Vector3.one * UnityEngine.Random.Range(.06f, .11f);
            var col = go.GetComponent<Collider>(); if (col != null) DestroyImmediate(col);
            go.AddComponent<IceShard>().velocity = new Vector3(Mathf.Cos(a) * 1.6f, 2.2f, Mathf.Sin(a) * 1.6f);
        }
    }

    private void OnDestroy()
    {
        if (shellMaterial != null) { if (Application.isPlaying) Destroy(shellMaterial); else DestroyImmediate(shellMaterial); }
    }
}
