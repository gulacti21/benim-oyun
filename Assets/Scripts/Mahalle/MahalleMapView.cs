using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Mahalle haritasi: zemin, ince tebesir rotasi ve 12 durak.
// Her durak bir bolumun tebesir cemberidir; yaninda numarasi, adi ve yildizlari durur.
// Duraga dokunmak bolumu SECER; baslatma isi alttaki tek OYNA dugmesindedir.
public static class MahalleMapView
{
    public const float Scale = 1080f / 356f;       // taslak birimi -> ekran pikseli
    public const float MapHeight = 1258f * Scale;  // zeminin cizildigi alan
    public const float TopPad = 380f;              // baslik + centik seridi payi
    public const float BottomPad = 360f;           // OYNA dugmesi ve alt cubuk payi
    public const float ContentHeight = MapHeight + TopPad + BottomPad;

    private static readonly float[] StopX = { 94, 238, 112, 254, 120, 246, 100, 230, 118, 256, 138, 214 };
    private static readonly float[] StopY = { 88, 182, 280, 378, 476, 574, 672, 770, 868, 966, 1064, 1162 };

    // Tasarim paleti
    private static readonly Color Krem  = new Color(.961f, .937f, .886f);
    private static readonly Color Komur = new Color(.176f, .169f, .145f);
    private static readonly Color Amber = new Color(.957f, .682f, .259f);

    // Olculer (1080 genislik referansi)
    private const float RingSize = 190f;   // tebesir cemberinin capi
    private const float NodeW = 760f, NodeH = 240f;
    private const float SideShift = 190f;  // durak kutusunun cembere gore kaymasi
    private const float TextGap = 125f;    // cember merkezi ile yazi blogu arasi

    // Harita arka planlari. Zemin dokulariyla karismasin diye ayri klasor ve "Harita" oneki.
    private static readonly string[] MapFiles = { "HaritaApartman", "HaritaOkul", "HaritaPark", "HaritaToprak", "HaritaMeydan" };
    private static readonly string[] GroundFiles = { "Apartman", "Okul", "Park", "Toprak", "Meydan" };

    // Secim durumu. Ekran her acilista yeniden kurulur.
    private static RectTransform[] nodes = new RectTransform[12];
    private static MahalleGraphic[] marks = new MahalleGraphic[12];
    private static int selected = -1;

    public static int Selected { get { return selected; } }

    // --- yerlesim testinin okudugu olculer ---
    public static bool LeftSide(int local) { return StopX[Mathf.Clamp(local, 0, 11)] < 178f; }
    public static Vector2 NodeCenter(int local)
    {
        local = Mathf.Clamp(local, 0, 11);
        return Stop(local) + new Vector2(LeftSide(local) ? SideShift : -SideShift, 0f);
    }
    public static Vector2 NodeSize { get { return new Vector2(NodeW, NodeH); } }
    public static float RingDiameter { get { return RingSize; } }
    // Yazi blogunun cember merkezine gore uzak kenari (yerel koordinat).
    public static float TextFarEdge(int local)
    {
        float cx = LeftSide(local) ? -SideShift : SideShift;
        return LeftSide(local) ? cx + TextGap + 430f : cx - TextGap - 430f;
    }

    public static float StopOffset(int local)
    {
        return StopY[Mathf.Clamp(local, 0, 11)] * Scale + TopPad;
    }

    private static Vector2 Stop(int i)
    {
        return new Vector2(StopX[i] * Scale - 540f, MapHeight * .5f - StopY[i] * Scale);
    }

    // Duraklardan birini secili hale getirir: ince amber halka ve hafif buyume.
    public static void Select(int local)
    {
        local = Mathf.Clamp(local, 0, 11);
        selected = local;
        for (int i = 0; i < 12; i++)
        {
            if (nodes[i] == null) continue;
            bool on = i == local;
            nodes[i].localScale = Vector3.one * (on ? 1.06f : 1f);
            if (marks[i] != null) marks[i].color = on ? Amber : new Color(0, 0, 0, 0);
        }
    }

    public static void Build(RectTransform host, int district, TMP_FontAsset font,
                             Action<int> onSelect, Action<int> onStart, Action<string> onBlocked)
    {
        var theme = MahalleTheme.Get(district);
        nodes = new RectTransform[12];
        marks = new MahalleGraphic[12];
        selected = -1;

        // --- zemin ---
        var groundRect = Rect("Zemin", host, Vector2.zero, new Vector2(1080f, ContentHeight));
        // 1) mahalleye ozel harita gorseli  2) yoksa 3B zemin dokusu  3) o da yoksa cizilen zemin
        Texture2D foto = null;
        bool doseli = false;
        if (district >= 0 && district < MapFiles.Length)
        {
            foto = Resources.Load<Texture2D>("Mahalle/Map/" + MapFiles[district]);
            if (foto == null) { foto = Resources.Load<Texture2D>("Mahalle/Ground/" + GroundFiles[district]); doseli = true; }
        }
        if (foto != null)
        {
            // Fotograf zemin: dikeyde tekrarlanir, ek dosya gerekmeden butun haritayi kaplar.
            var raw = groundRect.gameObject.AddComponent<RawImage>();
            raw.texture = foto;
            // Harita gorseli butun haritayi tek parca kaplar; zemin dokusu ise dosenir.
            raw.uvRect = doseli
                ? new UnityEngine.Rect(0f, 0f, 1080f / 600f, ContentHeight / 600f)
                : new UnityEngine.Rect(0f, 0f, 1f, 1f);
            raw.raycastTarget = false;
        }
        else
        {
            var ground = groundRect.gameObject.AddComponent<MahalleGround>();
            ground.top = theme.groundTop; ground.mid = theme.groundMid; ground.bottom = theme.groundBottom;
            ground.wash = theme.wash; ground.vignette = theme.vignette; ground.pebble = theme.pebble;
            ground.chalk = theme.chalk; ground.marks = theme.marks; ground.unit = Scale;
            ground.raycastTarget = false;
        }

        float surfaceY = ContentHeight * .5f - TopPad - MapHeight * .5f;
        var host2 = Rect("Yuzey", host, new Vector2(0f, surfaceY), new Vector2(1080f, MapHeight));

        // --- mahalleye ozel cizilen dekor ---
        // Harita fotografi varsa saksi, kedi, bank gibi cizimler kullanilmaz:
        // fotografin kendi dekoru var, ikisi ust uste binerse kalabalik olur.
        if (foto == null && theme.decor != null)
            foreach (var d in theme.decor)
            {
                var r = Rect("Dekor " + d.kind, host2, new Vector2(d.x * Scale - 540f, MapHeight * .5f - d.y * Scale), Vector2.one);
                var decor = r.gameObject.AddComponent<MahalleDecor>();
                decor.kind = d.kind; decor.unit = Scale * d.scale; decor.chalk = theme.chalk;
                decor.color = Color.white; decor.raycastTarget = false;
                if (d.flip) r.localScale = new Vector3(-1f, 1f, 1f);
            }

        // --- duraklari baglayan tek, ince, kesik tebesir rotasi ---
        var pathRect = Rect("Yol", host2, Vector2.zero, new Vector2(1080f, MapHeight));
        var path = pathRect.gameObject.AddComponent<MahalleChalkPath>();
        var stops = new Vector2[12];
        for (int i = 0; i < 12; i++) stops[i] = Stop(i);
        path.stops = stops;
        path.color = Krem;
        path.lineAlpha = .78f;
        path.dustAlpha = 0f;          // kalin yol seridi yok
        path.lineWidth = 6f;
        path.dashOn = 26f; path.dashOff = 30f;
        path.leadIn = 120f; path.leadOut = 0f;
        path.raycastTarget = false;

        // --- duraklar ---
        for (int local = 0; local < 12; local++)
        {
            int index = district * Campaign.PerDistrict + local;
            bool unlocked = MahalleProfile.Unlocked(index);
            int stars = MahalleProfile.Data.stars[index];
            bool current = unlocked && index == MahalleProfile.NextLevel;
            bool mastery = local == 11;
            bool left = StopX[local] < 178f;
            string levelName = Campaign.Database.Get(index).levelName;

            float side = left ? SideShift : -SideShift;
            var node = Rect("Durak " + (index + 1), host2, Stop(local) + new Vector2(side, 0f), new Vector2(NodeW, NodeH));

            var hit = node.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;
            node.gameObject.AddComponent<MahalleTap>();

            // Secim buyumesi bu cocuga uygulanir; basma animasyonu duragin kendisini olcekler.
            var vis = Rect("Görsel", node, Vector2.zero, new Vector2(NodeW, NodeH));
            nodes[local] = vis;

            float cx = left ? -SideShift : SideShift;

            // Secim halkasi (once cizilir, cemberin altinda kalir)
            var mark = Art(vis, "Secim halkasi", MahalleGraphic.Shape.ChalkRing,
                           new Vector2(cx, 0f), Vector2.one * (RingSize + 26f));
            mark.stroke = 5f; mark.color = new Color(0, 0, 0, 0);
            marks[local] = mark;

            Circle(vis, cx, unlocked);
            if (current) Marble(vis, new Vector2(cx, 2f), 108f, MahalleProfile.EffectiveSkin);
            else
            {
                float a = unlocked ? 1f : .62f;
                Marble(vis, new Vector2(cx - 34f, 12f), 58f, local % 4, a);
                Marble(vis, new Vector2(cx + 34f, 18f), 54f, (local + 2) % 4, a);
                Marble(vis, new Vector2(cx + 2f, -26f), 56f, (local + 1) % 4, a);
            }
            if (!unlocked)
            {
                var kilit = Art(vis, "Kilit", MahalleGraphic.Shape.Lock,
                                new Vector2(cx + 66f, -66f), Vector2.one * 52f);
                kilit.color = new Color(Komur.r, Komur.g, Komur.b, .62f);
            }
            if (mastery)
            {
                var badge = Art(vis, "Ustalik rozeti", MahalleGraphic.Shape.Star,
                                new Vector2(cx, RingSize * .5f + 34f), Vector2.one * 52f);
                badge.color = stars >= 3 ? Amber : new Color(Komur.r, Komur.g, Komur.b, .45f);
            }

            Block(vis, font, left, cx, local, levelName, stars, unlocked, current,
                  MahalleProfile.Required(index));

            int target = index;
            int slot = local;
            bool open = unlocked;
            int needBefore = index > 0 ? MahalleProfile.Required(index - 1) : 1;
            string blocked = needBefore > 1
                ? L.F("Bu bölüm için önceki bölümde {0} yıldız almalısın.", needBefore)
                : "Önce bir önceki bölümü tamamla.";
            var tap = node.gameObject.AddComponent<MahalleStopTap>();
            tap.onTap = () =>
            {
                if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlayUiTap();
                if (!open) { onBlocked(blocked); return; }
                if (selected == slot) { onStart(target); return; }   // secili duraga tekrar dokunmak baslatir
                Select(slot);
                onSelect(target);
            };
        }
    }

    // Numara etiketi, bolum adi ve yildizlar. Cemberin karsi tarafinda durur.
    private static void Block(RectTransform node, TMP_FontAsset font, bool left, float cx,
                              int local, string levelName, int stars, bool unlocked, bool current, int need)
    {
        float edge = left ? cx + TextGap : cx - TextGap;
        float pivotX = left ? 0f : 1f;
        var align = left ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
        float dim = unlocked ? 1f : .55f;

        // numara etiketi
        var badge = Art(node, "Numara", MahalleGraphic.Shape.Panel,
                        new Vector2(edge, 44f), new Vector2(78f, 58f));
        badge.radius = 18f;
        badge.color = unlocked ? Amber : new Color(Komur.r, Komur.g, Komur.b, .32f);
        badge.rectTransform.pivot = new Vector2(pivotX, .5f);
        badge.rectTransform.anchoredPosition = new Vector2(edge, 44f);
        var num = Text(node, font, (local + 1).ToString("00"), new Vector2(edge, 44f),
                       new Vector2(78f, 58f), 32f, unlocked ? Krem : new Color(Krem.r, Krem.g, Krem.b, .75f),
                       TextAlignmentOptions.Center);
        num.rectTransform.pivot = new Vector2(pivotX, .5f);
        num.rectTransform.anchoredPosition = new Vector2(edge, 44f);

        // bolum adi
        var name = Text(node, font, levelName, new Vector2(edge, -8f), new Vector2(430f, 58f),
                        42f, new Color(Komur.r, Komur.g, Komur.b, dim), align);
        name.rectTransform.pivot = new Vector2(pivotX, .5f);
        name.rectTransform.anchoredPosition = new Vector2(edge, -8f);
        name.fontStyle = FontStyles.Bold;

        if (current)
        {
            var now = Text(node, font, "SIRADAKİ", new Vector2(edge, -62f), new Vector2(430f, 44f),
                           27f, Amber, align);
            now.rectTransform.pivot = new Vector2(pivotX, .5f);
            now.rectTransform.anchoredPosition = new Vector2(edge, -62f);
            now.fontStyle = FontStyles.Bold;
            now.characterSpacing = 6f;
            return;
        }

        // yildizlar
        for (int s = 0; s < 3; s++)
        {
            float sx = left ? edge + 18f + s * 44f : edge - 18f - (2 - s) * 44f;
            var star = Art(node, "Yildiz " + s, MahalleGraphic.Shape.Star, new Vector2(sx, -62f), Vector2.one * 36f);
            star.color = s < stars ? Amber : new Color(Komur.r, Komur.g, Komur.b, unlocked ? .24f : .16f);
        }

        // baraj bolumu: kac yildiz gerektigi tek satir, gosterissiz
        if (!unlocked && need > 1)
        {
            var req = Text(node, font, L.F("{0} YILDIZ GEREKLİ", need), new Vector2(edge, -108f),
                           new Vector2(430f, 40f), 23f, new Color(Komur.r, Komur.g, Komur.b, .55f), align);
            req.rectTransform.pivot = new Vector2(pivotX, .5f);
            req.rectTransform.anchoredPosition = new Vector2(edge, -108f);
        }
    }

    // ---------- kucuk parcalar ----------

    private static void Circle(RectTransform node, float cx, bool unlocked)
    {
        var shade = Art(node, "Temas golgesi", MahalleGraphic.Shape.Circle,
                        new Vector2(cx, -44f), new Vector2(150f, 42f));
        shade.color = new Color(.13f, .09f, .05f, .14f);

        var ring = Art(node, "Tebesir cemberi", MahalleGraphic.Shape.ChalkRing,
                       new Vector2(cx, 0f), Vector2.one * RingSize);
        ring.color = new Color(Krem.r, Krem.g, Krem.b, unlocked ? .92f : .5f);
        ring.dashed = true;
        ring.stroke = 6f;
    }

    private static void Marble(RectTransform node, Vector2 pos, float size, int skin) { Marble(node, pos, size, skin, 1f); }

    private static void Marble(RectTransform node, Vector2 pos, float size, int skin, float alpha)
    {
        skin = Mathf.Clamp(skin, 0, Campaign.SkinColors.Length - 1);
        var art = Art(node, "Misket", MahalleGraphic.Shape.Marble, pos, Vector2.one * size);
        var c = Campaign.SkinColors[skin]; c.a = alpha;
        art.color = c;
        art.accent = SpecialMarbles.Accent(skin);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
        r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        return r;
    }

    private static MahalleGraphic Art(Transform parent, string name, MahalleGraphic.Shape shape, Vector2 pos, Vector2 size)
    {
        var r = Rect(name, parent, pos, size);
        var g = r.gameObject.AddComponent<MahalleGraphic>();
        g.shape = shape; g.raycastTarget = false;
        return g;
    }

    private static TextMeshProUGUI Text(Transform parent, TMP_FontAsset font, string value, Vector2 pos,
                                        Vector2 size, float fontSize, Color color, TextAlignmentOptions align)
    {
        var r = Rect(value, parent, pos, size);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = L.T(value); t.font = font; t.fontSize = fontSize; t.color = color;
        t.alignment = align; t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }
}
