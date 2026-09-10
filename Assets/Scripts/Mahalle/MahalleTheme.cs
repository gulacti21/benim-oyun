using UnityEngine;

public enum GroundMarks { None, Tiles, Court, Cracks, Cobble }
public enum DecorKind { Tree, Bench, Seksek, Steps, Pot, Bin, Cat, Hoop, Goal, Tire, Fountain, Pigeon }

// Dekorun yerleşimi taslak biriminde tutulur; harita çizerken ölçeklenir.
public struct DecorPlacement
{
    public DecorKind kind;
    public float x, y, scale;
    public bool flip;
    public DecorPlacement(DecorKind kind, float x, float y, float scale, bool flip = false)
    { this.kind = kind; this.x = x; this.y = y; this.scale = scale; this.flip = flip; }
}

// Her mahallenin kendi zemini, ışığı, yol malzemesi ve dekoru vardır.
// Değişmeyen tek şey altın vurgu rengidir: oyunun bütünlüğü oradan gelir.
public class MahalleTheme
{
    public string title, subtitle;
    public Color groundTop, groundMid, groundBottom;
    public Color vignette;
    public Color wash;                 // günün saati yıkaması, alfa 0 ise yok
    public Color chalk;                // yolun ve durak çemberlerinin rengi
    public float lineAlpha = .55f, lineWidth = 9f, dashOn = 33f, dashOff = 40f, dustAlpha = .16f;
    public Color pebble;
    public GroundMarks marks = GroundMarks.None;
    public string[] levelNames;
    public DecorPlacement[] decor;

    private static Color C(string hex)
    {
        Color c; ColorUtility.TryParseHtmlString(hex, out c); return c;
    }
    private static Color C(string hex, float a)
    {
        Color c = C(hex); c.a = a; return c;
    }

    private static MahalleTheme[] all;

    public static MahalleTheme Get(int district)
    {
        if (all == null) all = Build();
        return all[Mathf.Clamp(district, 0, all.Length - 1)];
    }

    public static string[] Names(int district) { return Get(district).levelNames; }

    private static MahalleTheme[] Build()
    {
        return new[]
        {
            // 0 — Apartman Önü · sabah serinliği, karo ve beton
            new MahalleTheme
            {
                title = "Apartman Önü", subtitle = "Sabah serinliği · karo ve beton",
                groundTop = C("#C8BEAA"), groundMid = C("#B1A591"), groundBottom = C("#9B8F7B"),
                vignette = C("#212833", .42f), wash = C("#000000", 0f),
                chalk = C("#FBF7EC"), lineAlpha = .52f, lineWidth = 9f, dashOn = 33f, dashOff = 40f, dustAlpha = .15f,
                pebble = C("#8E8574"), marks = GroundMarks.Tiles,
                levelNames = new[]
                {
                    "Kapı Eşiği","Merdiven Yanı","Saksılı Köşe","Garaj Önü","Kapıcı Dairesi","Bisiklet Yeri",
                    "Çamaşır İpi","Dar Aralık","Kova Arkası","Son Basamak","Apartman Buluşması","Ustalık Sınavı"
                },
                decor = new[]
                {
                    new DecorPlacement(DecorKind.Steps, 298, 250, 1f),
                    new DecorPlacement(DecorKind.Pot,    50, 440, 1f),
                    new DecorPlacement(DecorKind.Bin,   312, 716, .95f),
                    new DecorPlacement(DecorKind.Cat,    60, 908, 1f),
                    new DecorPlacement(DecorKind.Pot,   302, 1068, .85f)
                }
            },

            // 1 — Okul Bahçesi · öğle güneşi, asfalt ve saha çizgileri
            new MahalleTheme
            {
                title = "Okul Bahçesi", subtitle = "Öğle güneşi · asfalt ve saha çizgileri",
                groundTop = C("#989BA3"), groundMid = C("#82858D"), groundBottom = C("#71747B"),
                vignette = C("#1B1F26", .38f), wash = C("#000000", 0f),
                chalk = C("#F4F3EC"), lineAlpha = .60f, lineWidth = 10.3f, dashOn = 48f, dashOff = 30f, dustAlpha = .12f,
                pebble = C("#7C7F86"), marks = GroundMarks.Court,
                levelNames = new[]
                {
                    "Seksek Alanı","Pota Altı","Duvar Dibi","Tören Çizgileri","Bayrak Direği","Kantin Önü",
                    "Sıra Arası","Dar Koridor","Teneffüs Zili","Son Ders","Okul Turnuvası","Ustalık Sınavı"
                },
                decor = new[]
                {
                    new DecorPlacement(DecorKind.Hoop,   306, 232, 1f),
                    new DecorPlacement(DecorKind.Seksek,  48, 470, 1f),
                    new DecorPlacement(DecorKind.Bench,  306, 712, .9f),
                    new DecorPlacement(DecorKind.Hoop,    44, 916, .85f, true),
                    new DecorPlacement(DecorKind.Seksek, 300, 1080, .9f)
                }
            },

            // 2 — Park · ikindi güneşi, toprak ve tebeşir
            new MahalleTheme
            {
                title = "Park", subtitle = "İkindi güneşi · toprak ve tebeşir",
                groundTop = C("#A9884F"), groundMid = C("#8F7042"), groundBottom = C("#7A5C39"),
                vignette = C("#2E1E0E", .42f), wash = C("#000000", 0f),
                chalk = C("#F7F1DF"), lineAlpha = .55f, lineWidth = 9f, dashOn = 33f, dashOff = 40f, dustAlpha = .16f,
                pebble = C("#9C8058"), marks = GroundMarks.None,
                levelNames = new[]
                {
                    "Giriş Yolu","Bank Yanı","Ağaç Dibi","Çim Kenarı","Salıncak Altı","Havuz Başı",
                    "Yürüyüş Yolu","Çiçeklik","Dar Patika","Son Bank","Park Buluşması","Ustalık Sınavı"
                },
                decor = new[]
                {
                    new DecorPlacement(DecorKind.Tree,   312, 236, 1f),
                    new DecorPlacement(DecorKind.Bench,   46, 432, .95f),
                    new DecorPlacement(DecorKind.Seksek, 300, 706, 1f),
                    new DecorPlacement(DecorKind.Tree,    40, 918, .92f, true),
                    new DecorPlacement(DecorKind.Tree,   320, 1058, .8f)
                }
            },

            // 3 — Toprak Saha · tozlu öğle sonrası, kuru toprak
            new MahalleTheme
            {
                title = "Toprak Saha", subtitle = "Tozlu öğle sonrası · kuru toprak",
                groundTop = C("#C6A96E"), groundMid = C("#AE9058"), groundBottom = C("#977946"),
                vignette = C("#3A2708", .40f), wash = C("#E0B96A", .10f),
                chalk = C("#5E4622"), lineAlpha = .42f, lineWidth = 10.3f, dashOn = 21f, dashOff = 42f, dustAlpha = .12f,
                pebble = C("#8A6F3E"), marks = GroundMarks.Cracks,
                levelNames = new[]
                {
                    "Çakıllı Köşe","Kale Arkası","Geniş Açıklık","Orta Saha","Yan Çizgi","Toz Bulutu",
                    "Taşlı Zemin","Kale Direği","Dar Aralık","Son Vuruş","Saha Turnuvası","Ustalık Sınavı"
                },
                decor = new[]
                {
                    new DecorPlacement(DecorKind.Goal, 300, 240, 1f),
                    new DecorPlacement(DecorKind.Tire,  50, 446, 1f),
                    new DecorPlacement(DecorKind.Tire, 310, 720, .85f),
                    new DecorPlacement(DecorKind.Goal,  46, 912, .9f, true),
                    new DecorPlacement(DecorKind.Tire, 304, 1070, .9f)
                }
            },

            // 4 — Mahalle Meydanı · akşamüstü, taş döşeme ve çeşme
            new MahalleTheme
            {
                title = "Mahalle Meydanı", subtitle = "Akşamüstü · taş döşeme ve çeşme",
                groundTop = C("#A2988A"), groundMid = C("#8A8174"), groundBottom = C("#736A5E"),
                vignette = C("#2A1B2A", .46f), wash = C("#FF8C3C", .11f),
                chalk = C("#F1EBDC"), lineAlpha = .48f, lineWidth = 9f, dashOn = 39f, dashOff = 36f, dustAlpha = .13f,
                pebble = C("#867C6E"), marks = GroundMarks.Cobble,
                levelNames = new[]
                {
                    "Çeşme Çevresi","Kaldırım","Mozaik Döşeme","Çınar Gölgesi","Bakkal Önü","Taş Basamak",
                    "Avlu Kapısı","Işık Altı","Dar Geçit","Son Meydan","Mahalle Buluşması","Ustalık Sınavı"
                },
                decor = new[]
                {
                    new DecorPlacement(DecorKind.Fountain, 300, 252, 1f),
                    new DecorPlacement(DecorKind.Pigeon,    56, 440, 1f),
                    new DecorPlacement(DecorKind.Tree,     312, 716, 1f),
                    new DecorPlacement(DecorKind.Pigeon,    44, 912, .9f, true),
                    new DecorPlacement(DecorKind.Fountain, 302, 1068, .8f)
                }
            }
        };
    }
}
