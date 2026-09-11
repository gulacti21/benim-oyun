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
        float limit = arenaSize - MarbleRadius - EdgeMargin;
        return new Vector2(x, z).sqrMagnitude <= limit * limit;
    }

    // Cemberin icine geri ceker. Disari tasan dokunuslar reddedilmek yerine
    // en yakin gecerli noktaya kaydirilir -- parmakla dizerken bu daha iyi.
    public static Vector2 Clamp(float x, float z, float arenaSize)
    {
        float limit = arenaSize - MarbleRadius - EdgeMargin;
        var p = new Vector2(x, z);
        // Tam sinira degil, kil payi icine. Sinirin uzerine koyulan bir nokta
        // ondalik yuvarlama yuzunden "disarida" sayilabiliyordu.
        return p.magnitude <= limit ? p : p.normalized * (limit - .002f);
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
                    Vector2 dir = dist > .0001f ? d / dist : new Vector2(Mathf.Cos(i * 1.7f), Mathf.Sin(i * 1.7f));
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
        // Iki oyuncu cemberin iki yarisina yerlesir: baslangic simetrik ve adil.
        float side = player == 0 ? -1f : 1f;
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
