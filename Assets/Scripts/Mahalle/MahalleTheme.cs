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
            },

            // ===== HARİTA 2 — MEMLEKET (bölge 5-9) =====
            // Görseller sonra gelecek; o zamana kadar bu renklerle çizilen zemin görünür.

            // 5 — Sahil · sabah, ıslak kum
            new MahalleTheme
            {
                title = "Sahil", subtitle = "Sabah serinliği · ıslak kum",
                groundTop = C("#E2CFA5"), groundMid = C("#D4BD8C"), groundBottom = C("#C2A874"),
                vignette = C("#23384A", .36f), wash = C("#9CD3E8", .08f),
                chalk = C("#FFFFFF"), lineAlpha = .62f, lineWidth = 9f, dashOn = 30f, dashOff = 38f, dustAlpha = .14f,
                pebble = C("#B59C6C"), marks = GroundMarks.None,
                levelNames = new[]
                {
                    "Kumsal Girişi","Havlu Yanı","Kumdan Kale","Şemsiye Altı","Midye Kıyısı","Dalga Çizgisi",
                    "İskele Başı","Kayık Arkası","Sıcak Kum","Son Dalga","Sahil Buluşması","Ustalık Sınavı"
                },
                decor = new DecorPlacement[0]
            },

            // 6 — Köy Meydanı · yağmur sonrası, toprak ve çamur
            new MahalleTheme
            {
                title = "Köy Meydanı", subtitle = "Yağmur sonrası · toprak ve çamur",
                groundTop = C("#9C8566"), groundMid = C("#846D51"), groundBottom = C("#6E5942"),
                vignette = C("#231A10", .44f), wash = C("#000000", 0f),
                chalk = C("#F4EEDF"), lineAlpha = .52f, lineWidth = 9.5f, dashOn = 30f, dashOff = 40f, dustAlpha = .12f,
                pebble = C("#6B573F"), marks = GroundMarks.Cracks,
                levelNames = new[]
                {
                    "Çınar Altı","Kahve Önü","Su Birikintisi","Muhtarlık","Çeşme Başı","Tavuk Kümesi",
                    "Harman Yeri","Samanlık","Dar Sokak","Son Yağmur","Köy Buluşması","Ustalık Sınavı"
                },
                decor = new DecorPlacement[0]
            },

            // 7 — Yayla · serin öğleden sonra, çayır
            new MahalleTheme
            {
                title = "Yayla", subtitle = "Serin rüzgâr · eğimli çayır",
                groundTop = C("#8FA46A"), groundMid = C("#788E56"), groundBottom = C("#627745"),
                vignette = C("#18261A", .40f), wash = C("#CFE6F2", .07f),
                chalk = C("#F6F4EA"), lineAlpha = .58f, lineWidth = 9f, dashOn = 34f, dashOff = 38f, dustAlpha = .12f,
                pebble = C("#5F7043"), marks = GroundMarks.None,
                levelNames = new[]
                {
                    "Yayla Yolu","Çoban Çeşmesi","Buzlu Pınar","Ahşap Ev","Çam Gölgesi","Serin Çayır",
                    "Sisli Tepe","Kaya Yanı","Dik Yamaç","Son Tepe","Yayla Şenliği","Ustalık Sınavı"
                },
                decor = new DecorPlacement[0]
            },

            // 8 — Kasaba Pazarı · kalabalık öğle, taş ve tezgâh
            new MahalleTheme
            {
                title = "Kasaba Pazarı", subtitle = "Kalabalık öğle · tezgâh arası",
                groundTop = C("#B5A48C"), groundMid = C("#9E8D76"), groundBottom = C("#877762"),
                vignette = C("#2B1D12", .42f), wash = C("#FFB35C", .07f),
                chalk = C("#FBF6EA"), lineAlpha = .52f, lineWidth = 9f, dashOn = 36f, dashOff = 36f, dustAlpha = .13f,
                pebble = C("#877660"), marks = GroundMarks.Cobble,
                levelNames = new[]
                {
                    "Pazar Girişi","Karpuz Tezgâhı","Kasa Yığını","Domates Sırası","Terazi Önü","Tezgâh Arası",
                    "Sepetçi","Bakır Tezgâhı","Kalabalık Köşe","Son Tezgâh","Pazar Buluşması","Ustalık Sınavı"
                },
                decor = new DecorPlacement[0]
            },

            // 9 — Bayram Yeri · akşam ışıkları, final
            new MahalleTheme
            {
                title = "Bayram Yeri", subtitle = "Akşam ışıkları · büyük final",
                groundTop = C("#A08B7A"), groundMid = C("#877264"), groundBottom = C("#6F5C50"),
                vignette = C("#2A1430", .48f), wash = C("#FF7A4A", .12f),
                chalk = C("#FFF6E4"), lineAlpha = .50f, lineWidth = 9.5f, dashOn = 38f, dashOff = 34f, dustAlpha = .14f,
                pebble = C("#7C6859"), marks = GroundMarks.Cobble,
                levelNames = new[]
                {
                    "Bayram Sabahı","Salıncak Yeri","Pamuk Şeker","Atlıkarınca","Davul Zurna","Bayram Harçlığı",
                    "Fener Alayı","Lunapark","Gece Çukuru","Son Bayram","Büyük Buluşma","Ustalık Sınavı"
                },
                decor = new DecorPlacement[0]
            },

            // ===== HARİTA 3 — ŞEHİR (bölge 10-14) =====
            // Görseller sonra gelecek; o zamana kadar bu renklerle çizilen zemin görünür.
            // 10 — Şehir Merkezi · sabah trafiği, asfalt ve kaldırım
            new MahalleTheme
            {
                title = "Şehir Merkezi", subtitle = "Sabah trafiği · asfalt ve kaldırım",
                groundTop = C("#8C8D8B"), groundMid = C("#77787A"), groundBottom = C("#626467"),
                vignette = C("#15191F", 0.42f), wash = C("#BFD8F0", 0.06f),
                chalk = C("#FFFFFF"), lineAlpha = 0.6f, lineWidth = 9f, dashOn = 32f, dashOff = 38f, dustAlpha = 0.12f,
                pebble = C("#5C5E60"), marks = GroundMarks.Tiles,
                levelNames = new[]
                {
                    "Otobüs Durağı","Trafik Lambası","Site Kapısı","Kaldırım Taşı","Yaya Geçidi","Metro Çıkışı","Rögar Kapağı","Otopark","Alt Geçit","Gökdelen Önü","Şehir Buluşması","Ustalık Sınavı"
                },
                decor = new DecorPlacement[0]
            },
            // 11 — Sanayi · öğle paydosu, beton ve yağ lekesi
            new MahalleTheme
            {
                title = "Sanayi", subtitle = "Öğle paydosu · beton ve yağ",
                groundTop = C("#9A968D"), groundMid = C("#848077"), groundBottom = C("#6E6A62"),
                vignette = C("#1E1A14", 0.44f), wash = C("#FFC27A", 0.06f),
                chalk = C("#F6F2E8"), lineAlpha = 0.55f, lineWidth = 9.5f, dashOn = 30f, dashOff = 40f, dustAlpha = 0.14f,
                pebble = C("#6A665D"), marks = GroundMarks.Cracks,
                levelNames = new[]
                {
                    "Sanayi Girişi","Oto Tamirci","Lastikçi","Kaynakçı","Depo Önü","Forklift Yolu","Hurdalık","Vinç Altı","Konteyner","Yağ Lekesi","Sanayi Buluşması","Ustalık Sınavı"
                },
                decor = new DecorPlacement[0]
            },
            // 12 — Liman · martı sesleri, ıslak rıhtım
            new MahalleTheme
            {
                title = "Liman", subtitle = "Martı sesleri · ıslak rıhtım",
                groundTop = C("#7F8C93"), groundMid = C("#6B7880"), groundBottom = C("#57646C"),
                vignette = C("#0E1C26", 0.46f), wash = C("#9CD3E8", 0.08f),
                chalk = C("#FFFFFF"), lineAlpha = 0.6f, lineWidth = 9f, dashOn = 34f, dashOff = 38f, dustAlpha = 0.12f,
                pebble = C("#4F5B62"), marks = GroundMarks.Cobble,
                levelNames = new[]
                {
                    "Liman Girişi","Vapur İskelesi","Martı Yeri","Balıkçı Teknesi","Halat Yığını","Fener Altı","Rıhtım","Gümrük","Dalgakıran","Son Vapur","Liman Buluşması","Ustalık Sınavı"
                },
                decor = new DecorPlacement[0]
            },
            // 13 — AVM · hafta sonu kalabalığı, parlak karo
            new MahalleTheme
            {
                title = "AVM", subtitle = "Hafta sonu kalabalığı · parlak karo",
                groundTop = C("#C9C3B8"), groundMid = C("#B5AEA2"), groundBottom = C("#A0998D"),
                vignette = C("#2A2420", 0.36f), wash = C("#FFE7B0", 0.06f),
                chalk = C("#FFFFFF"), lineAlpha = 0.55f, lineWidth = 9f, dashOn = 36f, dashOff = 36f, dustAlpha = 0.1f,
                pebble = C("#9A9386"), marks = GroundMarks.Tiles,
                levelNames = new[]
                {
                    "AVM Kapısı","Yürüyen Merdiven","Vitrin Önü","Yemek Katı","Sinema Sırası","Oyun Salonu","Fıskiye","Asansör","Otopark Katı","Kapanış Saati","AVM Buluşması","Ustalık Sınavı"
                },
                decor = new DecorPlacement[0]
            },
            // 14 — Gökdelenler · gece ışıkları, büyük final
            new MahalleTheme
            {
                title = "Gökdelenler", subtitle = "Gece ışıkları · büyük final",
                groundTop = C("#5C5F6B"), groundMid = C("#4B4E5A"), groundBottom = C("#3B3E49"),
                vignette = C("#10102A", 0.5f), wash = C("#7A8CFF", 0.1f),
                chalk = C("#F4F2FF"), lineAlpha = 0.55f, lineWidth = 9.5f, dashOn = 38f, dashOff = 34f, dustAlpha = 0.14f,
                pebble = C("#40434E"), marks = GroundMarks.Tiles,
                levelNames = new[]
                {
                    "Plaza Önü","Cam Kule","Çatı Katı","Helikopter Pisti","Rüzgâr Tüneli","Gece Işıkları","Köprü Yolu","Kule Gölgesi","Son Kat","Şehir Manzarası","Büyük Final","Ustalık Sınavı"
                },
                decor = new DecorPlacement[0]
            },

            // ===== HARİTA 4 — LUNAPARK (bölge 15-19) =====
            // Görseller gelene kadar bu renklerle çizilen zemin görünür.
            // 15 — Giriş Kapısı · Balonlar ve pamuk şeker
            new MahalleTheme
            {
                title = "Giriş Kapısı", subtitle = "Balonlar ve pamuk şeker",
                groundTop = C("#B88E73"), groundMid = C("#A47C63"), groundBottom = C("#8F6B54"),
                vignette = C("#2A1A14", 0.4f), wash = C("#FFD8A8", 0.08f),
                chalk = C("#FFFFFF"), lineAlpha = 0.58f, lineWidth = 9f, dashOn = 34f, dashOff = 36f, dustAlpha = 0.12f,
                pebble = C("#7E5E4A"), marks = GroundMarks.Cobble,
                levelNames = new[]
                {
                    "Bilet Gişesi","Balon Arabası","Pamuk Şekerci","Giriş Kemeri","Patlamış Mısır","Konfeti Yağmuru","Hediyelik Dükkân","Tren İstasyonu","Fotoğraf Köşesi","Işıklı Yol","Giriş Buluşması","Ustalık Sınavı"
                },
                decor = new DecorPlacement[0]
            },
            // 16 — Çarpışan Arabalar · Lastik duvarlar · sert çarpışma
            new MahalleTheme
            {
                title = "Çarpışan Arabalar", subtitle = "Lastik duvarlar · sert çarpışma",
                groundTop = C("#4A4A50"), groundMid = C("#3E3E44"), groundBottom = C("#323238"),
                vignette = C("#100E14", 0.46f), wash = C("#FF6A5A", 0.08f),
                chalk = C("#FFFFFF"), lineAlpha = 0.58f, lineWidth = 9f, dashOn = 34f, dashOff = 36f, dustAlpha = 0.12f,
                pebble = C("#2A2A30"), marks = GroundMarks.Tiles,
                levelNames = new[]
                {
                    "Jeton Makinesi","Pist Kenarı","Kırmızı Araba","Lastik Duvar","Çarpışma Köşesi","Yarış Şeridi","Dönemeç","Kaza Yeri","Sıra Bekleme","Son Tur","Arabalar Buluşması","Ustalık Sınavı"
                },
                decor = new DecorPlacement[0]
            },
            // 17 — Dönme Dolap · Ahşap iskele · gün batımı
            new MahalleTheme
            {
                title = "Dönme Dolap", subtitle = "Ahşap iskele · gün batımı",
                groundTop = C("#B0895E"), groundMid = C("#9A774F"), groundBottom = C("#846542"),
                vignette = C("#2A1A10", 0.42f), wash = C("#FFB070", 0.1f),
                chalk = C("#FFF8EC"), lineAlpha = 0.58f, lineWidth = 9f, dashOn = 34f, dashOff = 36f, dustAlpha = 0.12f,
                pebble = C("#6E5438"), marks = GroundMarks.Tiles,
                levelNames = new[]
                {
                    "Dolap Altı","Atlıkarınca Yanı","Gondol","Yürüyen Yol","Dondurmacı","Tepe Noktası","Kabin Sırası","Kızıl Gökyüzü","Işıklı Tekerlek","Bank Başı","Dolap Buluşması","Ustalık Sınavı"
                },
                decor = new DecorPlacement[0]
            },
            // 18 — Oyun Sokağı · Halka atma ve peluş ödüller
            new MahalleTheme
            {
                title = "Oyun Sokağı", subtitle = "Halka atma ve peluş ödüller",
                groundTop = C("#A88A62"), groundMid = C("#937752"), groundBottom = C("#7E6444"),
                vignette = C("#24180E", 0.44f), wash = C("#FFC27A", 0.08f),
                chalk = C("#FFFFFF"), lineAlpha = 0.58f, lineWidth = 9f, dashOn = 34f, dashOff = 36f, dustAlpha = 0.12f,
                pebble = C("#6A5238"), marks = GroundMarks.Cracks,
                levelNames = new[]
                {
                    "Ördek Havuzu","Halka Atma","Ödül Rafı","Güç Kulesi","Kutu Devirme","Peluş Ayı","Pençe Makinesi","Büyük Ödül","Langırt","Kapanış","Oyun Buluşması","Ustalık Sınavı"
                },
                decor = new DecorPlacement[0]
            },
            // 19 — Sirk Çadırı · Spot ışıkları · büyük gösteri
            new MahalleTheme
            {
                title = "Sirk Çadırı", subtitle = "Spot ışıkları · büyük gösteri",
                groundTop = C("#8A2A2A"), groundMid = C("#742222"), groundBottom = C("#5E1B1B"),
                vignette = C("#1A0608", 0.5f), wash = C("#FFD27A", 0.1f),
                chalk = C("#FFF4E0"), lineAlpha = 0.58f, lineWidth = 9f, dashOn = 34f, dashOff = 36f, dustAlpha = 0.12f,
                pebble = C("#4A1414"), marks = GroundMarks.None,
                levelNames = new[]
                {
                    "Çadır Kapısı","Palyaço","Cambaz İpi","Hokkabaz","Tek Teker","Aslan Kafesi","Davul Sesi","Işık Hüzmesi","Akrobat","Son Gösteri","Sirk Buluşması","Ustalık Sınavı"
                },
                decor = new DecorPlacement[0]
            }
        };
    }
}
