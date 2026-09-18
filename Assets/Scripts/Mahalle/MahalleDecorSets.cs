using System.Collections.Generic;
using UnityEngine;

// MAHALLE DEKORLARI
// Her mahallenin kendi eşyaları. Kurallar:
//  1) Dekor oynanışa karışmaz: çarpışma yok, hepsi çemberin ve atış koridorunun dışında.
//  2) Silindir kullanılmaz (telefonda sorun çıkardı), sadece Cube ve Sphere.
//  3) Materyal ve konum collider'dan ÖNCE verilir (ParkCorners'taki telefon hatası).
public class MahalleDecorSets : MonoBehaviour
{
    private readonly List<Material> materials = new List<Material>();
    private Material beton, boya, kiremit, cim, demir, tebesir, su, tahta, toprak;

    public void Build(int district, float arenaSize)
    {
        beton = Mat(new Color(.62f, .60f, .56f)); boya = Mat(new Color(.80f, .74f, .62f));
        kiremit = Mat(new Color(.55f, .27f, .18f)); cim = Mat(new Color(.29f, .42f, .20f));
        demir = Mat(new Color(.30f, .33f, .35f)); tebesir = Mat(new Color(.93f, .92f, .86f));
        su = Mat(new Color(.36f, .60f, .68f)); tahta = Mat(new Color(.47f, .29f, .14f));
        toprak = Mat(new Color(.50f, .36f, .22f));

        float kenar = Mathf.Max(3.9f, arenaSize + .75f);   // dekor bu mesafenin dışında durur
        switch (district)
        {
            case 0: ApartmanOnu(kenar); break;
            case 1: OkulBahcesi(kenar); break;
            case 2: Park(kenar); break;
            case 3: ToprakSaha(kenar); break;
            default: MahalleMeydani(kenar); break;
        }
    }

    // ---- yardımcılar ----
    private Material Mat(Color color)
    {
        var shader = Resources.Load<Shader>("Mahalle/Environment");
        if (shader == null || !shader.isSupported) shader = Shader.Find("Universal Render Pipeline/Lit");
        var m = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        m.SetColor("_BaseColor", color); materials.Add(m); return m;
    }
    private Transform Part(string name, PrimitiveType type, Vector3 p, Vector3 size, Material m, float yaw = 0f)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(transform, false);
        go.transform.localPosition = p; go.transform.localScale = size; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        var r = go.GetComponent<Renderer>(); if (r != null) r.sharedMaterial = m;
        var c = go.GetComponent<Collider>(); if (c != null) { c.enabled = false; Destroy(c); }
        return go.transform;
    }
    private void Box(string n, float x, float y, float z, float w, float h, float d, Material m, float yaw = 0f)
        => Part(n, PrimitiveType.Cube, new Vector3(x, y, z), new Vector3(w, h, d), m, yaw);
    private void Ball(string n, float x, float y, float z, float r, Material m)
        => Part(n, PrimitiveType.Sphere, new Vector3(x, y, z), Vector3.one * r, m);
    // Yere çizilmiş ince şerit (tebeşir/kireç çizgisi, derz).
    private void Cizgi(string n, float x, float z, float w, float d, Material m, float yaw = 0f)
        => Box(n, x, .008f, z, w, .016f, d, m, yaw);

    // ---- APARTMAN ÖNÜ: bina cephesi, kapı, basamaklar, saksı ----
    private void ApartmanOnu(float k)
    {
        float z = k + 1.5f;
        Box("Apartman cephesi", 0, 1.6f, z + .9f, 11f, 3.2f, 1.2f, boya);
        Box("Kapı", -.2f, .75f, z + .3f, 1.25f, 1.5f, .18f, tahta);
        Box("Kapı kemeri", -.2f, 1.55f, z + .3f, 1.45f, .14f, .22f, beton);
        for (int i = 0; i < 3; i++) Box("Basamak " + i, -.2f, .05f + i * .09f, z - .35f - i * .26f, 2.2f - i * .3f, .1f, .28f, beton);
        Box("Saksı", 1.6f, .18f, z - .2f, .42f, .36f, .42f, kiremit);
        Ball("Saksı bitkisi", 1.6f, .52f, z - .2f, .55f, cim);
        Box("Saksı 2", -2.1f, .15f, z - .1f, .36f, .3f, .36f, kiremit);
        Ball("Saksı bitkisi 2", -2.1f, .42f, z - .1f, .46f, cim);
        // Karo derzleri: zemin tek renk kalmasın.
        for (int i = -3; i <= 3; i++) Cizgi("Karo derzi " + i, i * 1.5f, 0f, .04f, 14f, beton);
    }

    // ---- OKUL BAHÇESİ: pota, saha çizgileri, bayrak direği ----
    private void OkulBahcesi(float k)
    {
        float x = k + .9f;
        Box("Pota direği", -x, 1.3f, 2.2f, .16f, 2.6f, .16f, demir);
        Box("Pota kolu", -x + .35f, 2.45f, 2.2f, .7f, .1f, .1f, demir);
        Box("Pota tabelası", -x + .78f, 2.35f, 2.2f, .12f, .78f, 1.15f, boya);
        Box("Pota çemberi ön", -x + .95f, 2.02f, 2.2f, .1f, .06f, .62f, kiremit);
        Box("Pota çemberi yan", -x + .78f, 2.02f, 2.52f, .38f, .06f, .08f, kiremit);
        Box("Pota çemberi yan 2", -x + .78f, 2.02f, 1.88f, .38f, .06f, .08f, kiremit);
        // Asfalt saha çizgileri
        Cizgi("Saha çizgisi", 0, k + 1.2f, 13f, .1f, tebesir);
        Cizgi("Yan çizgi", -x - .5f, 0f, .1f, 12f, tebesir);
        Cizgi("Yan çizgi 2", x + .5f, 0f, .1f, 12f, tebesir);
        for (int i = 0; i < 10; i++) Cizgi("Orta çizgi " + i, 0, -5.5f + i * 1.2f, .09f, .6f, tebesir);
        // Bayrak direği
        Box("Bayrak direği", x + .6f, 1.8f, 3.4f, .12f, 3.6f, .12f, beton);
        Box("Bayrak", x + .95f, 3.1f, 3.4f, .62f, .42f, .04f, kiremit);
        // Seksek kareleri
        for (int i = 0; i < 4; i++) Cizgi("Seksek " + i, x + .2f, -3.2f + i * .8f, .7f, .7f, tebesir);
    }

    // ---- PARK (özel üç bölüm dışındakiler): çim şeridi, ağaç, bank ----
    private void Park(float k)
    {
        Cizgi("Çim şeridi", -k - 1.3f, 0f, 2.6f, 13f, cim);
        Cizgi("Çim şeridi 2", k + 1.3f, 0f, 2.6f, 13f, cim);
        Box("Ağaç gövdesi", -k - 1.1f, .9f, 3.4f, .3f, 1.8f, .3f, tahta);
        Ball("Ağaç tacı", -k - 1.1f, 2.3f, 3.4f, 2.5f, cim);
        Box("Bank oturağı", k + 1.2f, .42f, -1.6f, .6f, .1f, 1.9f, tahta);
        Box("Bank sırtlığı", k + 1.5f, .72f, -1.6f, .1f, .5f, 1.9f, tahta);
        Box("Bank ayağı", k + 1.2f, .21f, -2.4f, .5f, .42f, .1f, demir);
        Box("Bank ayağı 2", k + 1.2f, .21f, -.8f, .5f, .42f, .1f, demir);
        Box("Yürüyüş yolu", 0, .006f, k + 2.2f, 14f, .012f, 1.6f, beton);
    }

    // ---- TOPRAK SAHA: kale, kireç çizgileri, toprak yamaları ----
    private void ToprakSaha(float k)
    {
        float z = k + 1.8f;
        Box("Kale direği sol", -1.9f, .85f, z, .13f, 1.7f, .13f, tebesir);
        Box("Kale direği sağ", 1.9f, .85f, z, .13f, 1.7f, .13f, tebesir);
        Box("Kale üst direği", 0, 1.68f, z, 3.95f, .13f, .13f, tebesir);
        for (int i = 0; i < 7; i++) Box("File " + i, -1.75f + i * .58f, .85f, z + .28f, .03f, 1.6f, .03f, tebesir);
        Box("File üst", 0, 1.6f, z + .28f, 3.8f, .03f, .03f, tebesir);
        // Kireç çizgiler
        Cizgi("Kale çizgisi", 0, z - .45f, 9f, .1f, tebesir);
        Cizgi("Ceza sahası", 0, z - 2.3f, 6.2f, .1f, tebesir);
        Cizgi("Ceza yan", -3.1f, z - 1.35f, .1f, 1.9f, tebesir);
        Cizgi("Ceza yan 2", 3.1f, z - 1.35f, .1f, 1.9f, tebesir);
        Cizgi("Orta saha çizgisi", 0, -k - 1.6f, 11f, .1f, tebesir);
        // Toprak yamaları ve çim tutamları
        for (int i = 0; i < 6; i++)
        {
            float x = (i % 2 == 0 ? -1 : 1) * (k + .8f + (i % 3) * .5f);
            Cizgi("Toprak yaması " + i, x, -4f + i * 1.5f, 1.2f + (i % 3) * .4f, .9f, toprak);
            Ball("Çim tutamı " + i, x + .4f, .06f, -3.6f + i * 1.5f, .34f, cim);
        }
    }

    // ---- MAHALLE MEYDANI: çeşme, taş döşeme, çınar gölgesi ----
    private void MahalleMeydani(float k)
    {
        float z = k + 2f;
        Box("Çeşme havuzu", 0, .22f, z, 2.6f, .44f, 1.5f, beton);
        Box("Çeşme suyu", 0, .45f, z, 2.2f, .06f, 1.1f, su);
        Box("Çeşme gövdesi", 0, .95f, z + .5f, 1.1f, 1.5f, .5f, beton);
        Box("Çeşme başlığı", 0, 1.78f, z + .5f, 1.35f, .2f, .7f, kiremit);
        Box("Musluk", 0, 1.05f, z + .18f, .1f, .1f, .3f, demir);
        Ball("Su damlası", 0, .72f, z + .1f, .16f, su);
        // Taş döşeme: kenarlarda derzli karolar
        for (int i = -4; i <= 4; i++)
        {
            Cizgi("Döşeme derzi " + i, i * 1.25f, 0f, .05f, 14f, beton);
            Cizgi("Döşeme derzi yatay " + i, 0f, i * 1.25f, 14f, .05f, beton);
        }
        // Çınar ve gölgesi
        Box("Çınar gövdesi", k + 1.6f, 1.2f, -2.2f, .42f, 2.4f, .42f, tahta);
        Ball("Çınar tacı", k + 1.6f, 3.1f, -2.2f, 3.2f, cim);
        Ball("Çınar gölgesi", k + 1.2f, .01f, -2.2f, 3f, toprak);
        // Bakkal tentesi
        Box("Bakkal duvarı", -k - 2.2f, 1.4f, 1.2f, 1.2f, 2.8f, 5f, boya);
        Box("Tente", -k - 1.3f, 1.55f, 1.2f, .9f, .1f, 3.4f, kiremit);
    }

    private void OnDestroy() { foreach (var m in materials) if (m != null) Destroy(m); materials.Clear(); }
}
