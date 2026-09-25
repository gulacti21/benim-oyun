using System;
using System.Collections.Generic;
using UnityEngine;

// HARİTA 2 ZEMİN KURALLARI. Tek yer: oyun (GroundZones.FixedUpdate) ve fizik ölçüm
// araçları (her Simulate adımından önce) AYNI fonksiyonu çağırır.
//
//   KUM   — içindeki misketin hızı ek sürtünmeyle çabuk düşer.
//   ÇAMUR — çok yüksek sürtünme: giren misket birkaç santimde saplanır.
//   EĞİM  — HAREKET EDEN misketlere sabit yan ivme. Duran misket kaymaz (çimenin
//           tuttuğu hafif yamaç); aksi halde fizikte yuvarlanan küre hiç durmaz ve
//           bütün misketler kendi kendine sahadan akardı.
//   ÇUKUR — yavaş yuvarlanan HEDEF misket içine düşer ve kaybolur (sayılmaz, çıkarılamaz).
//           Hızlı misket üstünden atlar. Atıcı düşerse sadece durur (sıradaki atışta
//           zaten çizgiye döner).
//
// Sabitler ölçülerek seçildi: GroundRulesVerify.Measure, YAZ_TATILI_ILERLEME.md Faz 4.
public static class GroundRules
{
    // static (const değil): ölçüm aracı aday değerleri geçici olarak dener. Oyun değiştirmez.
    public static float SandDrag = 1.5f;         // ek linearDamping (taban .6)
    public static float MudDrag = 14f;           // ek linearDamping
    public static float SlopeMinSpeed = .3f;     // bunun altında eğim etkisiz (duran misket)
    public static float SlopeFullSpeed = 2.3f;   // eğim bu hızda tam etkili, altında orantılı azalır
    public static float PitCaptureSpeed = 2.2f;  // bundan yavaş geçen hedef çukura düşer
    public const float PitDepth = .22f;          // düşen misketin çökme miktarı (görsel)

    public static bool HasRules(LevelData level) =>
        level != null && ((level.zones != null && level.zones.Length > 0) || level.slope.sqrMagnitude > 1e-6f);

    // Bir fizik adımı öncesi. center = çemberin merkezi. captured: çukura düşen hedef
    // (çağıran kendi listesinden çıkarır). shooter null olabilir.
    public static void Step(LevelData level, Vector3 center, IList<Rigidbody> targets, Rigidbody shooter,
                            float dt, Action<Rigidbody> captured)
    {
        if (!HasRules(level)) return;
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            var b = targets[i];
            if (b == null || b.isKinematic || !b.gameObject.activeInHierarchy) continue;
            if (Apply(level, center, b, dt, true)) captured?.Invoke(b);
        }
        if (shooter != null && !shooter.isKinematic && shooter.gameObject.activeInHierarchy)
            Apply(level, center, shooter, dt, false);
    }

    // true = hedef çukura düştü.
    private static bool Apply(LevelData level, Vector3 center, Rigidbody b, float dt, bool isTarget)
    {
        Vector3 p = b.position;
        Vector3 v = b.linearVelocity;
        float speed = new Vector2(v.x, v.z).magnitude;
        float drag = 0f;
        if (level.zones != null)
            foreach (var z in level.zones)
            {
                float dx = p.x - (center.x + z.x), dz = p.z - (center.z + z.z);
                if (dx * dx + dz * dz > z.radius * z.radius) continue;
                switch (z.kind)
                {
                    case ZoneKind.Sand: drag += SandDrag; break;
                    case ZoneKind.Mud: drag += MudDrag; break;
                    case ZoneKind.Pit:
                        if (speed < PitCaptureSpeed)
                        {
                            if (!isTarget) { b.linearVelocity = Vector3.zero; b.angularVelocity = Vector3.zero; return false; }
                            Capture(b, new Vector3(center.x + z.x, p.y, center.z + z.z));
                            return true;
                        }
                        break;
                }
            }
        if (drag > 0f)
        {
            // PhysX linearDamping ile aynı biçim: v *= 1 / (1 + drag·dt) — kararlı, işaret değiştirmez.
            float k = 1f / (1f + drag * dt);
            b.linearVelocity = new Vector3(v.x * k, v.y, v.z * k);
            b.angularVelocity *= k;
            v = b.linearVelocity; speed *= k;
        }
        // Eğim hızla orantılı azalır: sabit ivme yavaş misketi sürtünmeden hızlı iterdi ve
        // (ölçüldü) hafifçe dürtülen misket hiç durmadan sahadan akıyordu — Yayla'nın
        // bölümleri 2 atışta %100 temizleniyordu. Böyle hiçbir hızda eğim sürtünmeyi yenemez;
        // sert atışın sapması (tam etki) aynı kalır.
        if (level.slope.sqrMagnitude > 1e-6f && speed > SlopeMinSpeed)
        {
            float f = Mathf.Clamp01((speed - SlopeMinSpeed) / (SlopeFullSpeed - SlopeMinSpeed));
            b.linearVelocity = v + new Vector3(level.slope.x, 0f, level.slope.y) * (dt * f);
        }
        return false;
    }

    private static void Capture(Rigidbody b, Vector3 hole)
    {
        b.linearVelocity = Vector3.zero; b.angularVelocity = Vector3.zero;
        b.isKinematic = true;
        // Çukurdaki misket artık engel değil: collider kapanır, üstünden geçilir.
        var c = b.GetComponent<Collider>(); if (c != null) c.enabled = false;
        var p = new Vector3(hole.x, b.position.y - PitDepth, hole.z);
        b.position = p; b.transform.position = p;
    }

    // Ölçüm aracı durumu geri sararken: çukurdaki misketi eski haline getirir.
    public static void Release(Rigidbody b)
    {
        var c = b.GetComponent<Collider>(); if (c != null) c.enabled = true;
        b.isKinematic = false;
    }

    // Nokta hangi bölgede (ilk eşleşen). Test ve görünüm için.
    public static bool Inside(ZoneSpot z, Vector3 center, Vector3 p)
    {
        float dx = p.x - (center.x + z.x), dz = p.z - (center.z + z.z);
        return dx * dx + dz * dz <= z.radius * z.radius;
    }
}
