using System;
using UnityEngine;

// BÖLÜNEN MİSKET (Harita 2, Kasaba Pazarı — karpuz).
//   - Temas doğrultusundaki bağıl hız ≥ SplitSpeed olan darbe misketi İKİYE böler:
//     iki küçük parça (ölçek x0.7, kütle yarı), parçalar hızı devralır ve çarpma
//     yönüne dik iki yana hafifçe (±PieceSpread derece) açılır.
//   - Tek kademe: parçalar tekrar bölünmez (IsPiece; oyunda bileşen parçadan silinir).
//   - PUAN: karpuz 1 misket değerindedir. Bütün çıkarsa 1; bölünürse İKİ YARISI BİRLİKTE 1
//     (ikinci yarım da çıkınca sayılır). Kullanıcı kararı, 2026-09-24.
//
// [ExecuteAlways]: fizik ölçüm araçları görünmez preview sahnede aynı bileşeni kullanır.
[ExecuteAlways]
[RequireComponent(typeof(Rigidbody))]
public class SplitMarble : MonoBehaviour
{
    // ÖLÇÜLDÜ (SplitMarbleVerify.Measure). Gerekçe: YAZ_TATILI_ILERLEME.md, Faz 3.
    public const float SplitSpeed = 3.0f;
    public const float PieceScale = .7f;
    public const float PieceMass = .5f;
    public const float PieceSpread = 16f;   // derece, çarpma yönüne dik açılma
    public const int Worth = 1;

    // (bölünen, parça A, parça B). Arena ve ölçüm aracı listelerini bununla günceller.
    public event Action<SplitMarble, Rigidbody, Rigidbody> Split;
    public bool Done { get; private set; }
    public float LastHitSpeed { get; private set; }
    public bool MeasureOnly { get; set; }
    // Parça: bütün misketin kopyası olduğu için bu bileşen onda da var ama asla bölünmez.
    // (Fizik geri çağrısı içinde bileşen silinemiyor; çarpışma mesajı kapalı bileşene de
    // geldiği için "enabled=false" da yetmiyor. Tek kademe bu bayrakla sağlanır.)
    public bool IsPiece { get; private set; }
    // Bölünme anındaki çarpma yönü (test için).
    public Vector3 HitDirection { get; private set; }

    public static readonly Color Rind = new Color(.16f, .50f, .22f);    // kabuk yeşili
    public static readonly Color Stripe = new Color(.06f, .26f, .10f);  // koyu çizgi
    public static readonly Color Flesh = new Color(.93f, .25f, .27f);   // karpuz içi
    public static readonly Color Seed = new Color(.12f, .08f, .06f);

    private void OnCollisionEnter(Collision collision)
    {
        if (Done || IsPiece) return;
        float speed = collision.relativeVelocity.magnitude;
        Vector3 normal = Vector3.zero;
        if (collision.contactCount > 0)
        {
            normal = collision.GetContact(0).normal;
            speed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, normal));
        }
        LastHitSpeed = Mathf.Max(LastHitSpeed, speed);
        if (MeasureOnly || speed < SplitSpeed) return;
        // Zemin çarpışması sayılmaz: sadece başka bir gövde (misket, engel değil) böler.
        if (collision.rigidbody == null) return;
        DoSplit(normal, speed);
    }

    private void DoSplit(Vector3 contactNormal, float speed)
    {
        Done = true;
        var body = GetComponent<Rigidbody>();
        Vector3 v = body.linearVelocity; v.y = 0f;
        Vector3 dir = v.sqrMagnitude > .0004f ? v.normalized : new Vector3(-contactNormal.x, 0f, -contactNormal.z).normalized;
        if (dir.sqrMagnitude < .5f) dir = Vector3.forward;
        HitDirection = dir;
        Vector3 side = Vector3.Cross(Vector3.up, dir);
        float pieceRadius = transform.localScale.x * PieceScale * .5f;

        var a = Piece(side, dir, v, pieceRadius, 1f);
        var b = Piece(side, dir, v, pieceRadius, -1f);

        if (Application.isPlaying)
        {
            if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlaySplit(Mathf.InverseLerp(SplitSpeed, SplitSpeed * 2.5f, speed));
            MahalleFeedback.Tap();
        }
        gameObject.SetActive(false);
        Split?.Invoke(this, a, b);
    }

    private Rigidbody Piece(Vector3 side, Vector3 dir, Vector3 v, float radius, float sign)
    {
        var clone = Instantiate(gameObject, transform.parent);
        // Önizleme sahnesinde Instantiate aktif sahneye koyar; ölçüm aynı sahnede olmalı.
        if (clone.scene != gameObject.scene) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone, gameObject.scene);
        clone.name = name + (sign > 0 ? "_a" : "_b");
        // Tek kademe: parça asla bölünmez. Oyunda bileşen sonra silinir (Destroy ertelenir).
        var own = clone.GetComponent<SplitMarble>();
        if (own != null) { own.IsPiece = true; own.Done = true; if (Application.isPlaying) Destroy(own); }
        clone.transform.localScale = transform.localScale * PieceScale;
        clone.transform.position = new Vector3(transform.position.x, radius, transform.position.z) + side * (radius * sign);
        var body = clone.GetComponent<Rigidbody>();
        var parentBody = GetComponent<Rigidbody>();
        body.mass = parentBody.mass * PieceMass;
        float speed = v.magnitude;
        Vector3 turned = Quaternion.AngleAxis(PieceSpread * sign, Vector3.up) * dir;
        body.linearVelocity = turned * speed;
        body.angularVelocity = parentBody.angularVelocity;
        // Yarımlar: tek başına 0, ikizi de çıkmışsa 1 (MarbleArena ikizi bağlar).
        var piece = clone.GetComponent<TargetMarble>();
        if (piece != null) piece.Worth = 0;
        var visual = clone.GetComponent<MarbleVisual>();
        if (visual != null) visual.SetOverride(FleshMaterial());
        return body;
    }

    // --- görünüm ---
    private static Material rindMat, fleshMat;

    public static Material RindMaterial() => rindMat != null ? rindMat : (rindMat = Make(Rind, Stripe));
    public static Material FleshMaterial() => fleshMat != null ? fleshMat : (fleshMat = Make(Flesh, Seed));

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
