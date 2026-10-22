using System.Collections.Generic;
using UnityEngine;

// ÇATAPAT MİSKET (Harita 3, Şehir).
//   - Temas doğrultusundaki bağıl hız ≥ PopSpeed olan darbe çatapatı PATLATIR: BlastRadius
//     içindeki bütün gövdeler (misketler, atıcı) merkezden dışarı doğru hız kazanır; yakındaki
//     daha çok. Başka bir çatapat da bu patlamayla vurulursa zincirleme patlar.
//   - Çatapat kendisi de bir misket (1 değer); patlayınca kararır, normal misket gibi kalır.
//   - Fizik geri çağrısı içinde başka gövdeye dokunulmaz (BİLİNEN TUZAKLAR): çarpışma sadece
//     bayrak koyar, patlama bir sonraki adımda FlushPending ile uygulanır. Oyunda MarbleArena,
//     ölçümde MemleketPhysicsVerify her fizik adımından sonra çağırır.
//
// [ExecuteAlways]: fizik ölçüm araçları görünmez preview sahnede aynı bileşeni kullanır.
[ExecuteAlways]
[RequireComponent(typeof(Rigidbody))]
public class Catapat : MonoBehaviour
{
    // Tasarım değerleri (2026-10-19); bölüm hedefleri bunlarla sıradan oyuncu modelinde ölçüldü.
    public const float PopSpeed = 1.5f;      // m/s, bundan hafif dokunuş patlatmaz
    public const float BlastRadius = 1.4f;   // dünya birimi (misket çapı .5)
    public const float BlastSpeed = 3.2f;    // merkezde verilen hız, kenarda %40'ı

    // Kırmızı normal misketlerle karışmasın diye (ekranda denendi): kara gövde, alev deseni.
    public static readonly Color Body = new Color(.10f, .09f, .09f);   // kara barut
    public static readonly Color Band = new Color(1f, .42f, .10f);     // alev turuncusu
    public static readonly Color Burnt = new Color(.55f, .53f, .50f);  // patlamış: kül grisi

    public bool Done { get; private set; }
    private bool pending;

    private static readonly List<Catapat> queue = new List<Catapat>();
    private static readonly Collider[] hits = new Collider[64];

    private void OnCollisionEnter(Collision collision)
    {
        if (Done || pending) return;
        if (collision.rigidbody == null) return;   // zemin/engel patlatmaz, sadece misket
        float speed = collision.relativeVelocity.magnitude;
        if (collision.contactCount > 0)
            speed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(0).normal));
        if (speed < PopSpeed) return;
        pending = true;
        queue.Add(this);
    }

    // Bekleyen patlamaları uygular. Zincir: patlamanın vurduğu çatapat aynı çağrıda patlar.
    public static int FlushPending()
    {
        int popped = 0;
        for (int guard = 0; queue.Count > 0 && guard < 32; guard++)
        {
            var batch = queue.ToArray();
            queue.Clear();
            foreach (var c in batch)
                if (c != null && c.pending && !c.Done) { c.Pop(); popped++; }
        }
        queue.Clear();
        return popped;
    }

    // Ölçüm aracı sahneyi sıfırlarken.
    public static void ClearQueue() { queue.Clear(); }

    private void Pop()
    {
        pending = false;
        Done = true;
        Vector3 center = transform.position;
        var ps = gameObject.scene.GetPhysicsScene();
        int n = ps.OverlapSphere(center, BlastRadius, hits, ~0, QueryTriggerInteraction.Ignore);
        var self = GetComponent<Rigidbody>();
        for (int i = 0; i < n; i++)
        {
            var b = hits[i].attachedRigidbody;
            if (b == null || b == self || b.isKinematic) continue;
            Vector3 d = b.position - center; d.y = 0f;
            float dist = d.magnitude;
            if (dist < .001f) continue;
            float k = Mathf.Lerp(1f, .4f, Mathf.Clamp01(dist / BlastRadius));
            b.WakeUp();
            b.linearVelocity += d / dist * (BlastSpeed * k);
            var other = b.GetComponent<Catapat>();
            if (other != null && !other.Done && !other.pending) { other.pending = true; queue.Add(other); }
        }
        if (Application.isPlaying)
        {
            if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlaySplit(1f);
            MahalleFeedback.Tap();
            var visual = GetComponent<MarbleVisual>();
            if (visual != null) visual.SetOverride(BurntMaterial());
        }
    }

    // --- görünüm ---
    private static Material liveMat, burntMat;
    public static Material LiveMaterial() => liveMat != null ? liveMat : (liveMat = Make(Body, Band));
    public static Material BurntMaterial() => burntMat != null ? burntMat : (burntMat = Make(Burnt, new Color(.35f, .33f, .31f)));

    private static Material Make(Color baseColor, Color swirl)
    {
        Shader shader = Resources.Load<Shader>("Mahalle/Marble");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return null;
        var m = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        m.SetColor("_BaseColor", baseColor);
        if (m.HasProperty("_SwirlColor")) m.SetColor("_SwirlColor", swirl);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .9f);
        var matcap = Resources.Load<Texture2D>("Mahalle/MarbleMatcap");
        if (matcap != null && m.HasProperty("_Matcap")) m.SetTexture("_Matcap", matcap);
        return m;
    }
}
