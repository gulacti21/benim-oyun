using System.Collections.Generic;
using UnityEngine;

// Dizme asamasinin kurallari. Yine saf mantik: sahne yok, arayuz yok.
// Arayuz sadece parmagin nereye dokundugunu buraya soyler, cevabi burasi verir.
public static class DuelPlacement
{
    public const float MarbleRadius = .25f;   // TargetMarble olcegi .5
    public const float MinGap = .56f;         // iki misket merkezi arasi en az
    public const float EdgeMargin = .30f;     // cember cizgisine yapisik birakma

    // Misket cemberin icinde mi? Cizgiye cok yakin olani oyun kabul etmez,
    // yoksa oyuncu misketini kenara dizip ilk dokunusta kaybeder.
    public static bool Inside(float x, float z, float arenaSize)
    {
        if (DuelSession.Row) return InsideRow(x, z, arenaSize);
        return DuelSession.Triangle ? InsideTriangle(x, z, arenaSize)
                                    : InsideCircle(x, z, arenaSize);
    }

    // DIZI: gecerli yer siranin UZERI. Elle dizme yok ama otomatik dizilisin
    // ve cakisma cozumunun sinira ihtiyaci var.
    private static bool InsideRow(float x, float z, float arenaSize)
    {
        float limit = arenaSize - MarbleRadius * .5f;
        return Mathf.Abs(x) <= limit && Mathf.Abs(z - DuelSession.RowZ) <= MarbleRadius;
    }

    private static bool InsideCircle(float x, float z, float arenaSize)
    {
        float limit = arenaSize - MarbleRadius - EdgeMargin;
        return new Vector2(x, z).sqrMagnitude <= limit * limit;
    }

    // SAHANIN KENDISI. Inside() dizme icindir ve cizgiye pay birakir; bu ise
    // "misket hala sahada mi" sorusunun cevabi, pay yok. Sira belirleme
    // atisinda cizgiyi gecip gecmedigi buradan anlasilir.
    public static bool InsideArena(float x, float z, float arenaSize)
    {
        if (!DuelSession.Triangle) return new Vector2(x, z).sqrMagnitude <= arenaSize * arenaSize;
        var c = TriangleCorners(arenaSize);
        var p = new Vector2(x, z);
        for (int i = 0; i < 3; i++)
            if (EdgeDistance(p, c[i], c[(i + 1) % 3]) > 0f) return false;
        return true;
    }

    // Ucgen: koseler 270, 30 ve 150 derecede, yaricap arenaSize.
    // MarbleArena ile ayni geometri; kenara pay birakilir.
    public static Vector2[] TriangleCorners(float arenaSize)
    {
        var c = new Vector2[3];
        for (int i = 0; i < 3; i++)
        {
            float a = (270f + i * 120f) * Mathf.Deg2Rad;
            c[i] = new Vector2(Mathf.Cos(a) * arenaSize, Mathf.Sin(a) * arenaSize);
        }
        return c;
    }

    private static bool InsideTriangle(float x, float z, float arenaSize)
    {
        var c = TriangleCorners(arenaSize);
        var p = new Vector2(x, z);
        float pay = MarbleRadius + EdgeMargin;
        for (int i = 0; i < 3; i++)
            if (EdgeDistance(p, c[i], c[(i + 1) % 3]) > -pay) return false;
        return true;
    }

    // Noktanin kenarin disinda kalma miktari. Negatifse iceride.
    private static float EdgeDistance(Vector2 point, Vector2 from, Vector2 to)
    {
        Vector2 edge = to - from;
        Vector2 normal = new Vector2(edge.y, -edge.x).normalized;
        return Vector2.Dot(point - from, normal);
    }

    // Cemberin icine geri ceker. Disari tasan dokunuslar reddedilmek yerine
    // en yakin gecerli noktaya kaydirilir -- parmakla dizerken bu daha iyi.
    public static Vector2 Clamp(float x, float z, float arenaSize)
    {
        var p = new Vector2(x, z);
        if (Inside(p.x, p.y, arenaSize)) return p;

        if (DuelSession.Row)
        {
            float lim = arenaSize - MarbleRadius * .5f;
            return new Vector2(Mathf.Clamp(p.x, -lim, lim), DuelSession.RowZ);
        }

        if (!DuelSession.Triangle)
        {
            float limit = arenaSize - MarbleRadius - EdgeMargin;
            // Tam sinira degil, kil payi icine: sinirin uzerine koyulan bir
            // nokta ondalik yuvarlama yuzunden "disarida" sayilabiliyordu.
            return p.magnitude <= limit ? p : p.normalized * (limit - .002f);
        }

        // Ucgende merkeze dogru cekerek ilk gecerli noktayi bul.
        var merkez = new Vector2(0f, arenaSize * .12f);
        for (int i = 1; i <= 60; i++)
        {
            var q = Vector2.Lerp(p, merkez, i / 60f);
            if (Inside(q.x, q.y, arenaSize)) return q;
        }
        return merkez;
    }

    // Ust uste binmeyi cozer. Iki oyuncu birbirini gormeden dizdigi icin
    // ayni noktaya koymalari kacinilmaz; cember acilirken burasi ayirir.
    // Cakisan cifti birbirinden esit iter, sonra hepsini cemberin icinde tutar.
    public static List<Vector2> Resolve(IList<Vector2> spots, float arenaSize)
    {
        var p = new List<Vector2>(spots);
        for (int pass = 0; pass < 24; pass++)
        {
            bool moved = false;
            for (int i = 0; i < p.Count; i++)
                for (int j = i + 1; j < p.Count; j++)
                {
                    Vector2 d = p[j] - p[i];
                    float dist = d.magnitude;
                    if (dist >= MinGap) continue;

                    // Tam ust uste geldiyse yon yok; sabit bir yone acilsin ki
                    // sonuc her cihazda ayni olsun (ag icin onemli).
                    // DIZI'de misketler ayni sirada durmali: ayirma sadece
                    // yan yana olur, yoksa siradan tasarlar.
                    Vector2 dir = DuelSession.Row ? new Vector2(d.x >= 0f ? 1f : -1f, 0f)
                                : dist > .0001f ? d / dist
                                : new Vector2(Mathf.Cos(i * 1.7f), Mathf.Sin(i * 1.7f));
                    float push = (MinGap - dist) * .5f + .001f;
                    p[i] -= dir * push;
                    p[j] += dir * push;
                    moved = true;
                }

            for (int i = 0; i < p.Count; i++) p[i] = Clamp(p[i].x, p[i].y, arenaSize);
            if (!moved) break;
        }
        return p;
    }

    // Oyuncuya hazir bir dizilis verir: "hazirim"a basip gecmek isteyen
    // ya da suresi dolan oyuncu bosta kalmasin.
    public static List<Vector2> DefaultLayout(int player, int count, float arenaSize)
    {
        var list = new List<Vector2>();
        float side = player == 0 ? -1f : 1f;

        // DIZI: tek duz sira. Iki oyuncunun misketleri BIRBIRINE GECMELI
        // diziliyor -- yan yana dizilse oyuncu kendi tarafina nisan alip
        // rakibinin misketine hic dokunmadan oynardi, oysa sira oyununda
        // kimin misketi oldugu onemsiz: kipirdattigin senin olur. Gecmeli
        // dizilis siranin ortasini da en degerli yer yapiyor.
        if (DuelSession.Row)
        {
            int slots = Mathf.Max(2, count * 2);
            float lim = arenaSize - MarbleRadius * .5f;
            for (int i = 0; i < count; i++)
            {
                int slot = i * 2 + (player & 1);
                float t = slots == 1 ? .5f : slot / (float)(slots - 1);
                list.Add(new Vector2(Mathf.Lerp(-lim, lim, t), DuelSession.RowZ));
            }
            return Resolve(list, arenaSize);
        }

        if (DuelSession.Triangle)
        {
            // Ucgende misketler agirlik merkezine yakin dizilir; iki oyuncu
            // ucgenin iki yarisina.
            float yukseklik = arenaSize * .35f;
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? .5f : i / (float)(count - 1);
                list.Add(Clamp(side * (.25f + t * .45f) * arenaSize * .5f,
                               -arenaSize * .05f + t * yukseklik, arenaSize));
            }
            return Resolve(list, arenaSize);
        }

        // Cemberde iki oyuncu cemberin iki yarisina yerlesir.
        float r = (arenaSize - MarbleRadius - EdgeMargin) * .55f;
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? .5f : i / (float)(count - 1);
            float angle = Mathf.Lerp(-50f, 50f, t) * Mathf.Deg2Rad;
            list.Add(new Vector2(side * Mathf.Cos(angle) * r, Mathf.Sin(angle) * r + arenaSize * .18f));
        }
        return Resolve(list, arenaSize);
    }
}
