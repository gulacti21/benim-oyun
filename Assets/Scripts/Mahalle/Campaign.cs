using UnityEngine;

public enum MarblePower { None = -1, Big = 0, Iron = 1, Guide = 2, Anchor = 3 }

public static class Campaign
{
    public const int PerDistrict = 12;
    public const int Count = 60;
    public static readonly string[] Districts = { "Apartman Önü", "Okul Bahçesi", "Park", "Toprak Saha", "Mahalle Meydanı" };
    public static readonly string[] Descriptions = {
        "İlk çizgi, ilk atış. Mahalle seni bekliyor.", "Teneffüs başladı. Şimdi sıra sende.",
        "Taşların arasından kendi yolunu bul.", "Geniş saha, yeni dizilimler, büyük atışlar.", "Bütün yollar bu meydana çıkar." };
    public static readonly string[] SkinNames = { "Bal Köpüğü", "Deniz Camı", "Kedi Gözü", "Gün Batımı", "Galaksi", "Usta İncisi", "Ağır Misket", "İnce Misket", "Sekici Misket", "Kaygan Misket" };
    public static readonly Color[] SkinColors = {
        new Color(.96f,.62f,.19f), new Color(.08f,.72f,.76f), new Color(.34f,.69f,.28f),
        new Color(.96f,.34f,.29f), new Color(.46f,.34f,.83f), new Color(.82f,.88f,.96f),
        new Color(.34f,.29f,.22f), new Color(.12f,.56f,.75f), new Color(.62f,.22f,.77f), new Color(.10f,.52f,.35f) };
    public static readonly int[] SkinPrices = { 0, 40, 80, 110, 140, 180, 400, 400, 400, 400 };
    public static int SkinCount => SkinNames.Length;
    public static readonly string[] PowerNames = { "Baş Misket", "Demir Misket", "Usta Gözü", "Yerinde Kal" };
    public static readonly string[] PowerDescriptions = {
        "Bir sonraki atışta büyür. Sık kümeleri dağıtır.",
        "Küçük ve ağır. Dar aralıktan sert vurur.",
        "İlk temas noktasını gösterir. Hassas nişan al.",
        "Bu atıştan sonra çizgiye dönmezsin. Misketin durduğu yerden devam edersin, sadece bir tur." };
    public static readonly int[] PowerPrices = { 16, 14, 10, 32 };
    private static LevelDatabase cached;

    public static LevelDatabase Database
    {
        get
        {
            if (cached != null) return cached;
            cached = ScriptableObject.CreateInstance<LevelDatabase>();
            cached.name = "Mahalle Campaign";
            cached.levels = new LevelData[Count];
            for (int i = 0; i < Count; i++)
            {
                int district = i / PerDistrict, local = i % PerDistrict;
                var level = ScriptableObject.CreateInstance<LevelData>();
                // Bölüm ismi mahallenin köşesini anlatır; liste MahalleTheme içindedir.
                level.levelName = MahalleTheme.Names(district)[local];
                level.district = district;
                level.mastery = local == 11;
                level.shape = (local == 2 || local == 4 || local == 6 || local == 10) ? ArenaShape.Circle : ArenaShape.Triangle;
                level.arenaSize = level.shape == ArenaShape.Circle ? 2.8f + district * .12f : 2.8f + (local > 6 ? .25f : 0f);
                level.triangleRows = local == 9 ? 2 : (local < 4 ? 3 : 4);
                if (local == 11) level.triangleRows = 3;
                int outer = local > 5 ? 8 : 6;
                level.rings = new[] { new MarbleRing { count = 1, radiusFactor = 0f }, new MarbleRing { count = outer, radiusFactor = .65f, angleOffset = district * 13f } };
                int total = level.shape == ArenaShape.Circle ? outer + 1 : level.triangleRows * (level.triangleRows + 1) / 2;
                level.shotCount = local == 9 ? 2 : (level.mastery ? 3 : 5);
                // Baraj bölümleri: mahalle sonu sınavı hep, son iki mahallede bir de sekizinci bölüm.
                level.starsToPass = level.mastery || (district >= 3 && local == 7) ? 2 : 1;
                level.oneStarTarget = Mathf.Max(1, Mathf.CeilToInt(total * (district < 2 ? .34f : .45f)));
                level.twoStarTarget = Mathf.CeilToInt(total * .72f);
                level.threeStarTarget = total;
                // Obstacles enter only after the player learns aiming. The centre lane stays open.
                level.obstacleCount = district >= 2 && local >= 3 ? (local % 3 == 0 ? 2 : 1) : 0;
                level.shooterStartPosition = new Vector3(0f, .25f, -4.2f);
                cached.levels[i] = level;
            }
            // İlk altı bölümün arena ölçüleri ve yıldız hedefleri eski kayıtla uyumlu kalır.
            // İsimleri artık mahallenin köşelerini anlatır.
            float[] sizes = { 2.6f, 3f, 3f, 3.4f, 3.6f, 3.8f };
            int[] rows = { 3, 4, 3, 5, 3, 5 }, shots = { 5, 5, 4, 5, 5, 4 };
            int[] one = { 2, 3, 2, 5, 5, 5 }, two = { 4, 6, 4, 9, 9, 8 }, three = { 6, 9, 6, 13, 13, 12 };
            for (int i = 0; i < 6; i++)
            {
                var l = cached.levels[i]; l.arenaSize = sizes[i]; l.triangleRows = rows[i]; l.shotCount = shots[i];
                l.oneStarTarget = one[i]; l.twoStarTarget = two[i]; l.threeStarTarget = three[i];
                l.shape = i == 2 || i == 4 ? ArenaShape.Circle : ArenaShape.Triangle;
                l.rings = i == 4 ? new[] { new MarbleRing { count = 1, radiusFactor = 0 }, new MarbleRing { count = 6, radiusFactor = .38f }, new MarbleRing { count = 8, radiusFactor = .72f, angleOffset = 22.5f } } : new[] { new MarbleRing { count = 1, radiusFactor = 0 }, new MarbleRing { count = 6, radiusFactor = .38f, angleOffset = 22.5f } };
                l.shooterStartPosition = new Vector3(0,.25f,-4.2f);
            }
            // Elle tasarlanmış bölümler formülle üretilenin üstüne yazar.
            for (int i = 0; i < Count; i++) LevelBook.Apply(cached.levels[i], i);
            return cached;
        }
    }
}
