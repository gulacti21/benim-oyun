using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Mahalle haritası: zemin, tebeşir yolu ve 12 durak.
// Her durak kendi bölümünün tebeşir çemberidir; içinde o bölümün misketleri durur.
public static class MahalleMapView
{
    public const float Scale = 1080f / 356f;       // taslak birimi -> ekran pikseli
    public const float MapHeight = 1258f * Scale;  // zeminin çizildiği alan
    public const float TopPad = 300f;    // başlığın kapladığı yükseklik kadar pay
    public const float BottomPad = 320f; // devam çubuğunun altında kalan pay
    public const float ContentHeight = MapHeight + TopPad + BottomPad;

    private static readonly float[] StopX = { 94, 238, 112, 254, 120, 246, 100, 230, 118, 256, 138, 214 };
    private static readonly float[] StopY = { 88, 182, 280, 378, 476, 574, 672, 770, 868, 966, 1064, 1162 };

    private static readonly Color Gold = new Color(.93f, .65f, .25f);
    private static readonly Color Cream = new Color(1f, .97f, .89f);
    private static readonly Color Shade = new Color(.13f, .09f, .04f, .26f);

    // Bir durağın haritanın tepesinden uzaklığı. Ekranı oraya kaydırmak için kullanılır.
    public static float StopOffset(int local)
    {
        return StopY[Mathf.Clamp(local, 0, 11)] * Scale + TopPad;
    }

    private static Vector2 Stop(int i)
    {
        return new Vector2(StopX[i] * Scale - 540f, MapHeight * .5f - StopY[i] * Scale);
    }

    public static void Build(RectTransform host, int district, TMP_FontAsset font,
                             Action<int> onSelect, Action<string> onBlocked)
    {
        var theme = MahalleTheme.Get(district);

        // Zemin bütün kaydırma alanını kaplar; böylece başlıkla harita arasında ek yeri görünmez.
        var groundRect = Rect("Zemin", host, Vector2.zero, new Vector2(1080f, ContentHeight));
        var ground = groundRect.gameObject.AddComponent<MahalleGround>();
        ground.top = theme.groundTop; ground.mid = theme.groundMid; ground.bottom = theme.groundBottom;
        ground.wash = theme.wash; ground.vignette = theme.vignette; ground.pebble = theme.pebble;
        ground.chalk = theme.chalk; ground.marks = theme.marks; ground.unit = Scale;
        ground.raycastTarget = false;

        // Yol, duraklar ve dekor bu yüzeyin üstünde durur.
        float surfaceY = ContentHeight * .5f - TopPad - MapHeight * .5f;
        var host2 = Rect("Yüzey", host, new Vector2(0f, surfaceY), new Vector2(1080f, MapHeight));

        // --- mahalleye özel dekor ---
        if (theme.decor != null)
            foreach (var d in theme.decor)
            {
                var r = Rect("Dekor " + d.kind, host2, new Vector2(d.x * Scale - 540f, MapHeight * .5f - d.y * Scale), Vector2.one);
                var decor = r.gameObject.AddComponent<MahalleDecor>();
                decor.kind = d.kind; decor.unit = Scale * d.scale; decor.chalk = theme.chalk;
                decor.color = Color.white; decor.raycastTarget = false;
                if (d.flip) r.localScale = new Vector3(-1f, 1f, 1f);
            }

        // --- durakları bağlayan çizgi ---
        var pathRect = Rect("Yol", host2, Vector2.zero, new Vector2(1080f, MapHeight));
        var path = pathRect.gameObject.AddComponent<MahalleChalkPath>();
        var stops = new Vector2[12];
        for (int i = 0; i < 12; i++) stops[i] = Stop(i);
        path.stops = stops;
        path.color = theme.chalk;
        path.lineAlpha = theme.lineAlpha; path.dustAlpha = theme.dustAlpha;
        path.lineWidth = theme.lineWidth; path.dashOn = theme.dashOn; path.dashOff = theme.dashOff;
        path.leadOut = 0f;              // yol ustalık sınavında biter
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

            var node = Rect("Durak " + (index + 1), host2, Stop(local), new Vector2(250f, 250f));
            var hit = node.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            var button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            node.gameObject.AddComponent<MahalleTap>();

            int target = index;
            bool open = unlocked;
            int need = MahalleProfile.Required(index);
            int needBefore = index > 0 ? MahalleProfile.Required(index - 1) : 1;
            string blocked = needBefore > 1
                ? "Bu bölüm için önceki bölümde " + needBefore + " yıldız almalısın."
                : "Önce bir önceki bölümü tamamla.";
            button.onClick.AddListener(() =>
            {
                if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlayUiTap();
                if (open) onSelect(target);
                else onBlocked(blocked);
            });

            if (!unlocked) Locked(node, font, theme, local, mastery, left, levelName);
            else if (current) Current(node, font, theme, local, left, levelName, need);
            else Done(node, font, theme, local, stars, left, levelName);

            if (mastery && unlocked)
            {
                var badge = Art(node, "Ustalık rozeti", MahalleGraphic.Shape.Star,
                                new Vector2(0f, 46f * Scale), Vector2.one * (40f * Scale));
                badge.color = stars >= 3 ? Gold : Alpha(theme.chalk, .55f);
            }
        }
    }

    // ---------- durak halleri ----------

    private static void Locked(RectTransform node, TMP_FontAsset font, MahalleTheme theme,
                               int local, bool mastery, bool left, string levelName)
    {
        // Mahallenin son sınavı: çift çember, ortada rozet. Numarası ve adı yanda yazar,
        // böylece yolun kesik çizgisi yazının üstünden geçmez.
        if (mastery)
        {
            Veil(node, 74f);
            Ring(node, 68f * Scale, Alpha(theme.chalk, .55f), false);
            Ring(node, 55f * Scale, Alpha(theme.chalk, .30f), true);
            var badge = Art(node, "Ustalık rozeti", MahalleGraphic.Shape.Star, Vector2.zero, Vector2.one * (46f * Scale));
            badge.color = Alpha(theme.chalk, .5f);
            // Rozet tek başına yeter: ne numara ne isim yazılır.
            return;
        }

        Veil(node, 46f);
        Ring(node, 44f * Scale, Alpha(theme.chalk, .3f), true);
        Text(node, font, (local + 1).ToString("00"), Vector2.zero, new Vector2(200f, 70f),
             16f * Scale, Alpha(theme.chalk, .46f), TextAlignmentOptions.Center);
    }

    // Çemberin içini hafifçe koyulaştırır; yolun çizgisi rakamın okunmasını bozmasın diye.
    private static void Veil(RectTransform node, float size)
    {
        var veil = Art(node, "İç gölge", MahalleGraphic.Shape.Circle, Vector2.zero, Vector2.one * (size * Scale));
        veil.color = new Color(.12f, .09f, .05f, .20f);
    }

    private static void Done(RectTransform node, TMP_FontAsset font, MahalleTheme theme,
                             int local, int stars, bool left, string levelName)
    {
        Contact(node, 56f, -24f);
        Ring(node, 50f * Scale, Alpha(theme.chalk, .8f), false);

        Marble(node, new Vector2(-8f, 3f), 14f, local % 4);
        Marble(node, new Vector2(8f, 5f), 12f, (local + 2) % 4);
        Marble(node, new Vector2(1f, -8f), 13f, (local + 1) % 4);

        for (int s = 0; s < 3; s++)
        {
            var star = Art(node, "Yıldız " + s, MahalleGraphic.Shape.Star,
                           new Vector2((-13f + s * 13f) * Scale, -40f * Scale), Vector2.one * 30f);
            star.color = s < stars ? Gold : Alpha(theme.chalk, .28f);
        }

        Side(node, font, (local + 1).ToString("00"), left, 2f, 13f * Scale, Gold);
        Side(node, font, levelName, left, -15f, 12.5f * Scale, Cream);
    }

    private static void Current(RectTransform node, TMP_FontAsset font, MahalleTheme theme,
                                int local, bool left, string levelName, int need)
    {
        var wide = Art(node, "Parıltı", MahalleGraphic.Shape.Circle, Vector2.zero, Vector2.one * (116f * Scale));
        wide.color = new Color(1f, .79f, .39f, .10f);
        var near = Art(node, "İç parıltı", MahalleGraphic.Shape.Circle, Vector2.zero, Vector2.one * (78f * Scale));
        near.color = new Color(1f, .79f, .39f, .12f);

        var pulse = Art(node, "Nabız", MahalleGraphic.Shape.ChalkRing, Vector2.zero, Vector2.one * (60f * Scale));
        pulse.color = new Color(.93f, .65f, .25f, .65f);
        pulse.stroke = 2f * Scale;
        pulse.gameObject.AddComponent<MahallePulse>();

        Contact(node, 60f, -28f);
        Ring(node, 60f * Scale, Alpha(theme.chalk, .95f), false);
        Ring(node, 50f * Scale, Alpha(theme.chalk, .45f), false);
        Marble(node, Vector2.zero, 30f, MahalleProfile.EffectiveSkin);

        var pill = Art(node, "Oyna", MahalleGraphic.Shape.Panel,
                       new Vector2(0f, -48f * Scale), new Vector2(54f * Scale, 20f * Scale));
        pill.color = Gold; pill.radius = 10f * Scale;
        Text(pill.rectTransform, font, "OYNA", Vector2.zero, new Vector2(54f * Scale, 20f * Scale),
             11f * Scale, new Color(.15f, .20f, .17f), TextAlignmentOptions.Center);

        // Baraj bölümlerinde oyuncu daha girmeden ne gerektiğini bilir.
        if (need > 1)
        {
            Text(node, font, need + " YILDIZ GEREKLİ", new Vector2(3f, -72f * Scale - 3f), new Vector2(480f, 44f),
                 10f * Scale, new Color(.13f, .09f, .04f, .5f), TextAlignmentOptions.Center);
            Text(node, font, need + " YILDIZ GEREKLİ", new Vector2(0f, -72f * Scale), new Vector2(480f, 44f),
                 10f * Scale, Gold, TextAlignmentOptions.Center);
        }

        Side(node, font, "SIRADAKİ", left, 8f, 9.5f * Scale, Gold);
        Side(node, font, (local + 1).ToString("00"), left, -8f, 14f * Scale, Cream);
        Side(node, font, levelName, left, -25f, 13f * Scale, Cream);
    }

    // ---------- küçük parçalar ----------

    private static void Ring(RectTransform node, float size, Color color, bool dashed)
    {
        var ring = Art(node, "Tebeşir çemberi", MahalleGraphic.Shape.ChalkRing, Vector2.zero, Vector2.one * size);
        ring.color = color; ring.dashed = dashed; ring.stroke = 2.6f * Scale;
    }

    private static void Contact(RectTransform node, float width, float dy)
    {
        var shade = Art(node, "Temas gölgesi", MahalleGraphic.Shape.Circle,
                        new Vector2(0f, dy * Scale), new Vector2(width * Scale, width * .28f * Scale));
        shade.color = Shade;
    }

    private static void Marble(RectTransform node, Vector2 offset, float size, int skin)
    {
        skin = Mathf.Clamp(skin, 0, Campaign.SkinColors.Length - 1);
        var art = Art(node, "Misket", MahalleGraphic.Shape.Marble,
                      new Vector2(offset.x * Scale, offset.y * Scale), Vector2.one * (size * Scale));
        art.color = Campaign.SkinColors[skin];
        art.accent = SpecialMarbles.Accent(skin);
    }

    // Zeminin üstünde okunsun diye her yazının arkasına koyu bir kopya konur.
    private static void Side(RectTransform node, TMP_FontAsset font, string value, bool left, float my,
                             float size, Color color)
    {
        float x = (left ? 46f : -46f) * Scale;
        Vector2 pos = new Vector2(x, -my * Scale);
        Vector2 box = new Vector2(360f, size * 1.6f);
        var align = left ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;

        var back = Text(node, font, value, pos + new Vector2(3f, -3f), box, size, new Color(.13f, .09f, .04f, .45f), align);
        back.rectTransform.pivot = new Vector2(left ? 0f : 1f, .5f);
        back.rectTransform.anchoredPosition = pos + new Vector2(3f, -3f);

        var front = Text(node, font, value, pos, box, size, color, align);
        front.rectTransform.pivot = new Vector2(left ? 0f : 1f, .5f);
        front.rectTransform.anchoredPosition = pos;
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
        t.text = value; t.font = font; t.fontSize = fontSize; t.color = color;
        t.alignment = align; t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    private static Color Alpha(Color c, float a) { c.a = a; return c; }
}
