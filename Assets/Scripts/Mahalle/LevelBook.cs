using UnityEngine;

// Elle tasarlanmış bölümler. Formülle üretilen bölümlerin üstüne yazar.
// Konumlar çemberin merkezine göredir: x sağa, z ileri (atıcıdan uzağa).
public static class LevelBook
{
    public static bool Apply(LevelData level, int index)
    {
        switch (index)
        {
            // ---------------- APARTMAN ÖNÜ · konu: temel ----------------

            // 01 Kapı Eşiği — sadece çek ve bırak.
            case 0:
                Set(level, 2.6f, 5, 1, 3, 3);
                level.marbles = Row(0.2f, -0.62f, 0f, 0.62f);
                return true;

            // 02 Merdiven Yanı — güç ayarı: az çekersen misket çemberden çıkmaz.
            case 1:
                Set(level, 2.8f, 5, 3, 6, 6);
                level.marbles = Spots(0f, 0.6f, -0.31f, 0.06f, 0.31f, 0.06f,
                                      -0.62f, -0.48f, 0f, -0.48f, 0.62f, -0.48f);
                return true;

            // 03 Saksılı Köşe — küme sağda. Atıcıyı çizgide kaydırmadan olmaz.
            case 2:
                Set(level, 3.0f, 5, 3, 6, 6);
                level.marbles = Spots(1.05f, 0.55f, 1.67f, 0.55f,
                                      0.74f, 0f, 1.36f, 0f, 1.98f, 0f, 1.36f, -0.55f);
                return true;

            // 04 Garaj Önü — iki ayrı küme. İlk gerçek karar: hangisi önce?
            case 3:
                Set(level, 3.1f, 5, 5, 8, 8);
                level.marbles = Spots(-1.75f, 0.5f, -1.13f, 0.5f, -1.44f, -0.05f, -1.44f, 0.95f,
                                      1.75f, 0.2f, 1.13f, 0.2f, 1.44f, -0.35f, 1.44f, 0.75f);
                return true;

            // 05 Kapıcı Dairesi — ortadakini vurursan halka dağılır.
            case 4:
                Set(level, 2.9f, 5, 6, 7, 7);
                level.marbles = Spots(0f, 0f,
                                      1.15f, 0f, 0.575f, 0.996f, -0.575f, 0.996f,
                                      -1.15f, 0f, -0.575f, -0.996f, 0.575f, -0.996f);
                return true;

            // 06 Bisiklet Yeri — ilk engel. Düz yol kapalı, yandan gireceksin.
            case 5:
                Set(level, 3.0f, 5, 4, 7, 7);
                level.marbles = Spots(-0.62f, 0.3f, 0f, 0.3f, 0.62f, 0.3f,
                                      -0.93f, 0.85f, -0.31f, 0.85f, 0.31f, 0.85f, 0.93f, 0.85f);
                level.obstacles = new[] { new ObstacleSpot(0f, -0.95f, 1.15f, 0.42f) };
                return true;

            // 07 Çamaşır İpi — misketler kenara yakın: itmesi kolay, isabet zor.
            case 6:
                Set(level, 3.2f, 5, 7, 8, 8);
                level.marbles = Arc(2.35f, 20f, 160f, 8);
                return true;

            // 08 Dar Aralık — iki taşın arasından geçmek ya da sektirmek.
            case 7:
                Set(level, 2.7f, 5, 5, 6, 7);
                // Ok ucu: tepe en uzakta, kollar aticiya dogru iniyor. Ortadan vurursan
                // kollar disari degil yana savrulur; kollari uclarindan almak gerekir.
                level.marbles = Spots(-1.8f, 0.9f, -1.2f, 1.25f, -0.6f, 1.6f, 0f, 1.95f, 0.6f, 1.6f, 1.2f, 1.25f, 1.8f, 0.9f);
                level.obstacles = new[]
                {
                    new ObstacleSpot(-1.05f, -0.6f, 1.3f, 0.4f),
                    new ObstacleSpot(1.05f, -0.6f, 1.3f, 0.4f)
                };
                return true;

            // 09 Kova Arkası — iki atış, beş misket. Zincirleme vuruş şart.
            case 8:
                Set(level, 2.3f, 2, 4, 5, 5);
                level.marbles = Spots(0f, 0f, 0f, 0.55f, -0.55f, 0f, 0.55f, 0f, 0f, -0.55f);
                return true;

            // 10 Son Basamak — geniş dizilim, dar çizgi. Ortadan atmak zorundasın.
            case 9:
                Set(level, 3.3f, 3, 6, 9, 9);
                level.marbles = Spots(-2.1f, 0.3f, -1.5f, 0.3f, -1.8f, -0.25f,
                                      -0.31f, 0.35f, 0f, 0.9f, 0.31f, 0.35f,
                                      2.1f, 0.3f, 1.5f, 0.3f, 1.8f, -0.25f);
                level.shooterHalfWidth = 0.85f;
                return true;

            // 11 Apartman Buluşması — kalabalık küme, iki yan engel.
            case 10:
                Set(level, 3.3f, 4, 9, 11, 12);
                level.marbles = Spots(-1.55f, 0.2f, -0.93f, 0.2f, -0.31f, 0.2f, 0.31f, 0.2f, 0.93f, 0.2f, 1.55f, 0.2f,
                                      -0.62f, 0.75f, 0f, 0.75f, 0.62f, 0.75f,
                                      -0.31f, 1.3f, 0.31f, 1.3f, 0f, 1.85f);
                level.obstacles = new[]
                {
                    new ObstacleSpot(-1.35f, -0.9f, 1f, 0.4f, 18f),
                    new ObstacleSpot(1.35f, -0.9f, 1f, 0.4f, -18f)
                };
                return true;

            // 12 Ustalık Sınavı — üç ayrı hedef grubu, iki farklı giriş.
            // Üç atışta 7 misket: tek sık kümeye vurarak sınav geçilemez.
            case 11:
                Set(level, 3.35f, 3, 6, 8, 10);
                level.marbles = Spots(-1.2f, -.1f, -1.85f, .2f, -1.45f, .85f,
                                      1f, .3f, 1.6f, .6f, 2.25f, .95f,
                                      -.55f, 1.5f, 0f, 1.8f, .55f, 2.1f, -.5f, 2.3f);
                level.obstacles = new[]
                {
                    new ObstacleSpot(-1.4f, -2f, 1f, .4f),
                    new ObstacleSpot(1.45f, -2f, 1f, .4f, -18f)
                };
                level.starsToPass = 2;
                return true;
            // ---------------- OKUL BAHÇESİ · konu: açı ----------------
            // İki kaldıraç: ayrık kümelerin konumu ve yolu kapatan duvarlar.
            // 01-11 çember ve atış hakkı sabit; final daha geniş alanda üç atış ister.
            // 01-03 öğret, 04-08 uygula, 09-11 zorla, 12 sınav.

            // 01 Seksek Alanı — İki çapraz dörtlü: ilk teması seç, aynı dik atışı tekrarlama.
            case 12:
                Set(level, 3.2f, 4, 5, 7, 8);
                // KASE. Agzi aticiya donuk bir yay: iki uc acikta, dip korumada.
                // Uclardan baslamak zorundasin, dibe dogrudan yol yok.
                level.marbles = Spots(-1.598f, 1.618f, -1.302f, 1.107f, -0.85f, 0.728f, -0.295f, 0.526f,
                                      0.295f, 0.526f, 0.85f, 0.728f, 1.302f, 1.107f, 1.598f, 1.618f);
                return true;

            // 02 Pota Altı — Yakın kısa sıra ve uzak kanat: iki ayrı geliş açısı.
            case 13:
                Set(level, 3.2f, 4, 5, 8, 8);
                level.marbles = Spots(-1.1f, -0.65f, -1.7f, -0.4f, -2.3f, -0.15f,
                                      -1.5f, 0.3f, 1f, 0.6f, 1.6f, 0.85f,
                                      2.2f, 1.1f, 1.55f, 1.5f);
                return true;

            // 03 Duvar Dibi — Tek siper: açık sağ kanattan sonra soldaki yay için çapraz yol bul.
            case 14:
                Set(level, 3.2f, 4, 5, 8, 8);
                level.marbles = Spots(-1.1f, 0.1f, -1.7f, 0.35f, -2.2f, 0.75f,
                                      -1.65f, 1.05f, 1.1f, -0.3f, 1.7f, -0.3f,
                                      1.4f, 0.25f, 2f, 0.25f);
                level.obstacles = new[]
                {
                    new ObstacleSpot(-1.5f, -1.7f, 1.2f, 0.4f)
                };
                return true;

            // 04 Tören Çizgileri — Ortadaki duvarın iki ucundan ters eğimli sıralara gir.
            case 15:
                Set(level, 3.2f, 4, 6, 8, 8);
                level.marbles = Spots(-0.85f, 0.1f, -1.3f, 0.55f, -1.75f, 1f,
                                      -2.2f, 1.45f, 0.95f, 1.65f, 1.35f, 1.15f,
                                      1.75f, 0.65f, 2.15f, 0.15f);
                level.obstacles = new[]
                {
                    new ObstacleSpot(0f, -1.2f, 1.7f, 0.4f)
                };
                return true;

            // 05 Bayrak Direği — Uzun orta duvar iki cebi ayırır; ceplere farklı taraftan yaklaş.
            case 16:
                Set(level, 3.2f, 4, 8, 9, 10);
                level.marbles = Spots(-1.05f, -0.2f, -1.65f, -0.2f, -1.95f, 0.4f,
                                      -1.65f, 1f, -1.05f, 1f, 1.05f, -0.2f,
                                      1.65f, -0.2f, 1.95f, 0.4f, 1.65f, 1f,
                                      1.05f, 1f);
                level.obstacles = new[]
                {
                    new ObstacleSpot(0f, 0.2f, 0.4f, 2.2f)
                };
                return true;

            // 06 Kantin Önü — Eğik siperin açık ucunu oku; geniş sol yay ve sık sağ küme farklı temas ister.
            case 17:
                Set(level, 3.2f, 4, 7, 10, 10);
                level.marbles = Spots(-0.9f, -0.25f, -1.55f, -0.4f, -2.15f, -0.1f,
                                      -2.2f, 0.6f, -1.75f, 1.15f, 1.1f, 0.3f,
                                      1.7f, 0.3f, 2.3f, 0.3f, 1.4f, 0.85f,
                                      2f, 0.85f);
                level.obstacles = new[]
                {
                    new ObstacleSpot(1.5f, -1.9f, 1.3f, 0.4f, 18f)
                };
                return true;

            // 07 Sıra Arası — İki siper: ortadaki boşluğa düz atış yerine iki yana çapraz giriş.
            case 18:
                Set(level, 3.2f, 4, 6, 8, 10);
                // ZIKZAK. Alt ve ust noktalar sirayla diziliyor: duz bir atis
                // hicbir zaman iki misketi birden bulmuyor.
                level.marbles = Spots(-2.2f, 1.4f, -1.65f, 0.7f, -1.1f, 1.4f, -0.55f, 0.7f, 0f, 1.4f,
                                      0.55f, 0.7f, 1.1f, 1.4f, 1.65f, 0.7f, 2.2f, 1.4f, 0f, 2.2f);
                level.obstacles = new[]
                {
                    new ObstacleSpot(-1.4f, -0.8f, 1.3f, 0.4f, 15f),
                    new ObstacleSpot(1.4f, -0.8f, 1.3f, 0.4f, -15f)
                };
                return true;

            // 08 Dar Koridor — Şaşırtmalı kısa duvarlar: yakın sol ve uzak sağ cebe ayrı giriş.
            case 19:
                Set(level, 3.2f, 3, 5, 7, 8);
                // Koridordan duz gecen atis sadece ortadaki ikiliye ulasir.
                // Yan kumeler icin cizginin ucuna gidip duvarin disindan dolasmak gerekir.
                // Uc atis, uc ayri karar.
                // Yan kumeler kenara cok yakindi: vurulan misket zaten disari fisliyordu.
                // Merkeze cekildi, artik cikmak icin gercekten yol kat etmeleri gerekiyor.
                level.marbles = Spots(-1.65f, 0.45f, -2.05f, 0.95f, -1.4f, 1.1f, -1.85f, 1.6f,
                                      1.65f, 0.45f, 2.05f, 0.95f, 1.4f, 1.1f, 1.85f, 1.6f,
                                      -0.3f, 1.9f, 0.3f, 1.9f);
                level.obstacles = new[]
                {
                    new ObstacleSpot(-0.85f, -0.55f, 0.4f, 1.9f),
                    new ObstacleSpot(0.85f, -0.55f, 0.4f, 1.9f)
                };
                return true;

            // 09 Teneffüs Zili — Yelpazeler açılıyor: duvarın kenarından doğru ilk misketi seç.
            case 20:
                Set(level, 3.2f, 4, 8, 9, 11);
                // Halka ve ortasindaki tek misket. On siradan vurursan halka acilir ama
                // arkaya kacar; halkayi kirmak icin yandan girmek gerekir.
                level.marbles = Spots(1.4f, 1f, 1.13f, 1.82f, 0.43f, 2.33f, -0.43f, 2.33f, -1.13f, 1.82f,
                                      -1.4f, 1f, -1.13f, 0.18f, -0.43f, -0.33f, 0.43f, -0.33f, 1.13f, 0.18f,
                                      0f, 1f);
                level.obstacles = new[]
                {
                    new ObstacleSpot(0f, -1.35f, 1.7f, 0.4f)
                };
                return true;

            // 10 Son Ders — Dört yakın, sekiz uzak misket: büyük kümenin iki kanadına açı bul.
            case 21:
                // Atis sayisi 4'ten 3'e indirildi: yerlesim ayni, baski artiyor.
                // 4 atisla 12 misketin 11'i cikiyordu, "zorla" evresi icin fazla kolayd.
                Set(level, 3.2f, 3, 7, 9, 11);
                // Ic ice iki yay, ikisi de aticiya aciliyor. Ic yaya ulasmak icin
                // once dis yayda bir bosluk acmak gerekiyor.
                level.marbles = Spots(1.126f, 1.55f, 0.65f, 2.026f, 0f, 2.2f, -0.65f, 2.026f, -1.126f, 1.55f,
                                      1.832f, 1.567f, 1.419f, 2.238f, 0.772f, 2.69f, 0f, 2.85f,
                                      -0.772f, 2.69f, -1.419f, 2.238f, -1.832f, 1.567f);
                level.obstacles = new[]
                {
                    new ObstacleSpot(0f, -0.5f, 1.8f, 0.4f)
                };
                return true;

            // 11 Okul Turnuvası — Ters eğimli iki siper: bir kümeye işe yarayan geliş açısı diğerini tutmaz.
            case 22:
                Set(level, 3.2f, 4, 9, 10, 12);
                // Simetri yok: solda 3x3 sikisik blok, sagda uc dagimik misket.
                // Bloga dalmak kolay ama uctekileri ayri ayri toplamak atis yiyor.
                level.marbles = Spots(-1.9f, 0.4f, -1.3f, 0.4f, -0.7f, 0.4f,
                                      -1.9f, 1f, -1.3f, 1f, -0.7f, 1f,
                                      -1.9f, 1.6f, -1.3f, 1.6f, -0.7f, 1.6f,
                                      1.8f, 0.6f, 2.3f, 1.3f, 1.6f, 2f);
                level.obstacles = new[]
                {
                    new ObstacleSpot(-0.15f, -0.5f, 0.4f, 2f),
                    new ObstacleSpot(2.85f, 0.9f, 0.35f, 1.6f)
                };
                return true;

            // 12 Ustalık Sınavı — çapraz girişler ve üç ayrı hedef grubu.
            // 11'den daha geniş alan, bir eksik atış ve daha yüksek geçiş hedefi.
            // Kümeler tek vuruşta topluca boşalmasın; üç bilinçli ilk temas seç.
            case 23:
                Set(level, 3.45f, 4, 5, 7, 11);
                // KUM SAATI: iki ucgen dar bir belde birlesiyor. Belden vurursan
                // iki tarafi birden acarsin ama o acikligi bulmak zor.
                level.marbles = Spots(-1.3f, 2.6f, 0f, 2.6f, 1.3f, 2.6f, -0.65f, 2.05f, 0.65f, 2.05f, 0f, 1.5f,
                                      0f, 0.9f, -0.65f, 0.35f, 0.65f, 0.35f, -1.3f, -0.2f, 0f, -0.2f, 1.3f, -0.2f);
                level.obstacles = new[]
                {
                    new ObstacleSpot(-2.4f, 1.2f, 0.4f, 1.8f, -15f),
                    new ObstacleSpot(2.4f, 1.2f, 0.4f, 1.8f, 15f)
                };
                level.starsToPass = 2;
                return true;

            // ---------------- PARK · konu: sekme ve koridor ----------------
            // 01 Giriş Yolu — Eğik duvar: açık üçlü geçişi, uzak hedefler sekme çalışmasını sağlar.
            case 24:
                Set(level, 3.45f, 4, 6, 9, 10);
                level.marbles = Spots(-1.6f, -0.7f, -2.2f, -0.35f, -1.6f, 0f, 0.25f, 0.6f, 0.9f, 0.6f, 1.55f, 0.6f, 2.2f, 0.6f, 0.55f, 1.2f, 1.2f, 1.2f, 1.85f, 1.2f);
                level.obstacles = new[] { new ObstacleSpot(0.35f, -0.65f, 1.8f, 0.38f, 25f) };
                return true;

            // 02 Bank Yanı — Yan duvarı kullanarak siperin arkasına gir.
            case 25:
                Set(level, 3.45f, 4, 3, 6, 8);
                level.marbles = Spots(1.15f, -0.6f, 1.8f, -0.35f, 2.35f, 0f, -1.6f, 0.35f, -0.95f, 0.35f, -0.3f, 0.35f, -1.3f, 0.95f, -0.65f, 0.95f, -1.3f, 1.6f, -0.65f, 1.6f);
                level.obstacles = new[] { new ObstacleSpot(-2.65f, 0.3f, 0.35f, 2.5f, 0f), new ObstacleSpot(-0.85f, -0.65f, 1.8f, 0.38f, 0f) };
                return true;

            // 03 Ağaç Dibi — Ters taraftan sekme: doğrudan açık küme ve siperli hedefler.
            case 26:
                Set(level, 3.45f, 4, 5, 7, 9);
                // Ortada derin bir kolon, iki yanda kenara yapisik yaylar. Kolon arkadan
                // one dogru cozulur, yaylar ise siyirmayla. Iki ayri teknik.
                level.marbles = Spots(0.2f, 0.4f, 0.2f, 1f, 0.2f, 1.6f, 0.2f, 2.2f,
                                      -1.2f, 0.8f, -1.4f, 1.6f, -1f, 2.3f,
                                      1.2f, 0.8f, 1.4f, 1.6f, 1f, 2.3f);
                // Yaylar kenardan ice cekildi: artik tek dokunusla disari cikmiyorlar,
                // disaridaki iki siper de yana savrulani geri tutuyor.
                level.obstacles = new[] { new ObstacleSpot(-2.5f, 0.6f, 0.4f, 2.2f), new ObstacleSpot(2.5f, 0.6f, 0.4f, 2.2f), new ObstacleSpot(0.2f, -0.8f, 1.4f, 0.4f) };
                return true;

            // 04 Çim Kenarı — Geniş koridorda ilk temas açısını seç.
            case 27:
                Set(level, 3.45f, 4, 7, 9, 12);
                level.marbles = Spots(-1.65f, -0.4f, -2.3f, -0.1f, -1.9f, 0.5f, -1.3f, 0.85f, -0.6f, 0.8f, 0f, 0.95f, 0.6f, 1.1f, 1.2f, 1.25f, 1.8f, 1.4f, 0.2f, 1.65f, 0.85f, 1.85f, 1.5f, 2f);
                level.obstacles = new[] { new ObstacleSpot(-1.05f, -0.5f, 0.38f, 1.9f, 18f), new ObstacleSpot(1.05f, -0.5f, 0.38f, 1.9f, -18f) };
                return true;

            // 05 Salıncak Altı — Ortadaki siperden sekerek iki yana açıl.
            case 28:
                Set(level, 3.45f, 4, 9, 10, 12);
                level.marbles = Spots(-1.1f, 0.25f, -1.75f, 0.25f, -2.3f, 0.6f, -1.4f, 0.85f, -2.05f, 1.2f, -1.4f, 1.5f, 1.1f, 0.25f, 1.75f, 0.25f, 2.3f, 0.6f, 1.4f, 0.85f, 2.05f, 1.2f, 1.4f, 1.5f);
                level.obstacles = new[] { new ObstacleSpot(0f, -0.9f, 2f, 0.4f, 0f), new ObstacleSpot(-2.5f, -0.85f, 0.35f, 1.5f, 22f), new ObstacleSpot(2.5f, -0.85f, 0.35f, 1.5f, -22f) };
                return true;

            // 06 Havuz Başı — Siper ve uzak duvar: önce açık taraf, sonra duvar arkasındaki sıra.
            case 29:
                Set(level, 3.45f, 4, 5, 7, 9);
                // L: uzak kenarda yatay sira, sag kenarda asagi inen kol, merkezde uclu.
                // Uc ayri yon, uc ayri atis. Hicbiri digerinin yolundan vurulmuyor.
                level.marbles = Spots(-1.8f, 1.9f, -1.25f, 2f, -0.7f, 2.1f, -0.15f, 2.15f, 0.45f, 2.15f,
                                      1.3f, 1.9f, 1.6f, 1.35f, 1.9f, 0.8f, 2.2f, 0.25f,
                                      -0.8f, 1.15f, -0.2f, 1f, 0.4f, 1.1f);
                // Uzak kenardaki duvar L'nin ust kolunu disari birakmiyor: siyirip
                // cikarmak yerine aci bulmak gerekiyor.
                level.obstacles = new[] { new ObstacleSpot(-1.2f, 0f, 0.4f, 1.8f, 10f), new ObstacleSpot(1f, -0.6f, 1.5f, 0.4f, -15f), new ObstacleSpot(-0.9f, 2.9f, 1.4f, 0.35f, 5f) };
                return true;

            // 07 Yürüyüş Yolu — Şaşırtmalı koridor: açık giriş ve sekmeli kısa yol.
            case 30:
                Set(level, 3.45f, 4, 9, 10, 12);
                level.marbles = Spots(-2.1f, -0.3f, -1.5f, 0f, -2.1f, 0.4f, -1.5f, 0.7f, -1.8f, 1.3f, -1.1f, 1.4f, 0.75f, 0.5f, 1.4f, 0.65f, 2.05f, 0.8f, 1f, 1.25f, 1.65f, 1.4f, 2.25f, 1.55f);
                level.obstacles = new[] { new ObstacleSpot(-1f, -0.6f, 0.38f, 1.7f, 0f), new ObstacleSpot(0.1f, 0.5f, 0.38f, 1.7f, 0f), new ObstacleSpot(2.7f, -0.55f, 0.35f, 1.6f, -15f) };
                return true;

            // 08 Çiçeklik — Çapraz siperler farklı sekme açısı ister.
            case 31:
                Set(level, 3.45f, 4, 7, 9, 11);
                // Bilardo istakasi: ucu aticiya bakiyor. Tam ortadan vurus sadece uc
                // misketi alir, arkadakiler yana acilir. Kenardan girmek gerekir.
                level.marbles = Spots(0f, 0.9f, -0.31f, 1.44f, 0.31f, 1.44f,
                                      -0.62f, 1.98f, 0f, 1.98f, 0.62f, 1.98f,
                                      -0.93f, 2.52f, -0.31f, 2.52f, 0.31f, 2.52f, 0.93f, 2.52f,
                                      -2.5f, 1.2f, 2.5f, 1.2f);
                // Istaka uzak kenara tasindi ve arkasindaki duvar kaldirildi: arka sira
                // itilince artik disari cikabiliyor. Onceden %50'de takiliyordu.
                level.obstacles = new[] { new ObstacleSpot(-1.3f, -0.8f, 1.2f, 0.4f, 25f), new ObstacleSpot(1.3f, -0.8f, 1.2f, 0.4f, -25f) };
                return true;

            // 09 Dar Patika — Geniş hedef yelpazesi; koridor girişinde yön kaybını önle.
            case 32:
                Set(level, 3.45f, 3, 7, 8, 10);
                // Ortadaki uzun bolen: iki kumeye ayni noktadan ulasilamaz.
                // Sol kume derinde ve acik, sag kume sig ama kenar duvari cikisi kesiyor.
                // Uc atisla ikisini birden toplayamazsin: hangisini feda edeceksin?
                level.marbles = Spots(-1.3f, 0.5f, -1.9f, 0.65f, -2.5f, 0.8f, -1.5f, 1.2f, -2.1f, 1.45f, -1.35f, 1.85f, -2f, 2.05f,
                                      1.35f, -0.1f, 1.95f, 0.05f, 2.55f, 0.2f, 1.55f, 0.55f, 2.15f, 0.8f, 1.4f, 1.2f, 2.05f, 1.35f);
                level.obstacles = new[] { new ObstacleSpot(0f, -0.1f, 0.4f, 3.2f, 8f), new ObstacleSpot(3.05f, 0.7f, 0.35f, 1.8f, 0f) };
                return true;

            // 10 Son Bank — İki yan siper ve uzak açık duvar; açıları sırayla değiştir.
            case 33:
                // Tavan olculdu: 14 misketin ancak 6'si cikiyor. Gecis 7'ydi, yani
                // normal misketle gecilemiyordu. 6'ya cekildi: pay 0, KURNAZ bolum.
                Set(level, 3.45f, 4, 6, 8, 10);
                // Kenarda dort kolay misket, merkezde on tane sikisik.
                // Kolaylari toplamak hedefe yetmez; merkezdekiler cikmak icin
                // butun cemberi kat etmek zorunda. Uc atis, iki kolay bir zor.
                level.marbles = Spots(-2.7f, 0.4f, 2.7f, 0.4f, -2.4f, 1.9f, 2.4f, 1.9f,
                                      -0.6f, 0.35f, 0f, 0.35f, 0.6f, 0.35f,
                                      -0.9f, 0.9f, -0.3f, 0.9f, 0.3f, 0.9f, 0.9f, 0.9f,
                                      -0.6f, 1.45f, 0f, 1.45f, 0.6f, 1.45f);
                level.obstacles = new[] { new ObstacleSpot(0f, -0.6f, 1.6f, 0.4f, 0f), new ObstacleSpot(-1.9f, -0.9f, 1.2f, 0.4f, 25f) };
                return true;

            // 11 Park Buluşması — Birbirinden ayrılmış hedefler; sekme çıkışını da hesapla.
            case 34:
                Set(level, 3.45f, 4, 10, 11, 13);
                // Tuzak: on siradaki bes misket kolay gorunur ama duz vurursan
                // arkalarindaki kumeyi merkeze gomerler. Dogru oyun once onlari
                // yandan kesip cikarmak. Acgozlu oynayan bolumu kendine kapatir.
                level.marbles = Spots(-1.5f, -0.2f, -0.75f, -0.35f, 0f, -0.4f, 0.75f, -0.35f, 1.5f, -0.2f,
                                      -1.1f, 0.6f, -0.4f, 0.65f, 0.4f, 0.65f, 1.1f, 0.6f,
                                      -0.75f, 1.25f, 0f, 1.3f, 0.75f, 1.25f,
                                      0f, 1.9f, 0f, 2.5f);
                level.obstacles = new[] { new ObstacleSpot(-2.5f, -0.4f, 1.1f, 0.4f, -35f), new ObstacleSpot(2.5f, -0.4f, 1.1f, 0.4f, 35f) };
                return true;

            // 12 Ustalık Sınavı — Üç atış, üç ayrı giriş; 10 hedef geçiş, 13 hedef ustalık.
            case 35:
                Set(level, 3.5f, 4, 8, 10, 12);
                // Uc grup, uc gercekten ayri giris acisi. Eskiden girisler yan yanaydi
                // ve tek yerden hepsine ulasiliyordu. Simdi: sol grup duvarin disindan
                // dolasarak, orta grup siperin ustunden, sag grup kenardan.
                level.marbles = Spots(-2.6f, 0.9f, -2.05f, 1.45f, -2.55f, 1.8f, -2f, 2.2f,
                                      -0.35f, 1.9f, 0.3f, 2f, -0.05f, 2.5f, 0.65f, 2.55f,
                                      2.3f, 0.2f, 2.75f, 0.75f, 2.2f, 1.15f, 2.8f, 1.5f, 2.35f, 1.95f, 1.9f, 0.65f);
                level.obstacles = new[] { new ObstacleSpot(0.15f, 1.15f, 1.7f, 0.35f, -10f), new ObstacleSpot(0f, -0.3f, 0.4f, 2f, 20f), new ObstacleSpot(-3.15f, -0.2f, 0.35f, 1.4f, 0f) };
                level.starsToPass = 2;
                return true;

            // ---------------- TOPRAK SAHA · konu: atis ekonomisi ----------------
            // Saha genis, misketler uzaga gitmek zorunda, atis basina verim dusuk.
            // Her atisin nereye gidecegi bir karar. Ritim: 5-7-9-11 kurnaz, 4-6-8-10 nefes.

            // 01 Cakilli Kose — genis sahada mesafe ogretilir: misket uzun yol kat etmeli.
            case 36:
                Set(level, 3.5f, 3, 4, 7, 9);
                // Uzun bir capraz cizgi ve sag dipte ayri bir ucluk. Cizgi bir ucundan
                // sokulunca akar; ucluk ise ayri bir yolculuk. Uc atis, iki is.
                level.marbles = Spots(-2.6f, 0.1f, -2f, 0.55f, -1.4f, 1f, -0.8f, 1.45f,
                                      -0.2f, 1.9f, 0.4f, 2.35f, 1f, 2.8f,
                                      2.2f, 0.3f, 2.7f, 0.85f, 2.1f, 1.1f);
                level.obstacles = new[] { new ObstacleSpot(0f, -0.6f, 1.5f, 0.4f) };
                return true;

            // 02 Kale Arkasi — siperin arkasi. Iki misket disarida, kalabalik ardinda.
            case 37:
                Set(level, 3.5f, 4, 5, 8, 10);
                level.marbles = Spots(-1.9f, -0.2f, 1.9f, -0.2f,
                                      -1f, 0.9f, -0.35f, 1f, 0.3f, 1f, 0.95f, 0.9f,
                                      -0.7f, 1.6f, -0.05f, 1.7f, 0.6f, 1.6f, 0f, 2.3f);
                level.obstacles = new[] { new ObstacleSpot(0f, -0.4f, 2f, 0.4f) };
                return true;

            // 03 Genis Aciklik — dagitilmis misketler. Her atis bir tane; ekonomi dersi burada baslar.
            case 38:
                Set(level, 3.55f, 4, 6, 9, 9);
                level.marbles = Spots(-1.7f, 0.3f, -0.9f, 0.85f, -1.6f, 1.4f, -0.4f, 1.5f, -0.05f, 0.45f,
                                      0.55f, 1.15f, 1.2f, 0.5f, 1.8f, 1.1f, 0.9f, 1.95f, 0.15f, 2.2f);
                level.obstacles = new[] { new ObstacleSpot(-1.15f, -0.6f, 1.3f, 0.4f, 18f), new ObstacleSpot(1.15f, -0.6f, 1.3f, 0.4f, -18f) };
                return true;

            // 04 Orta Saha — ortadaki bolen iki kumeyi ayirir. Bol atis var, karar rahat.
            case 39:
                Set(level, 3.6f, 4, 8, 11, 12);
                level.marbles = Spots(-0.85f, 0.35f, -1.45f, 0.6f, -0.95f, 1f, -1.55f, 1.3f, -0.9f, 1.65f, -1.5f, 2f,
                                      0.85f, 0.35f, 1.45f, 0.6f, 0.95f, 1f, 1.55f, 1.3f, 0.9f, 1.65f, 1.5f, 2f);
                level.obstacles = new[] { new ObstacleSpot(0f, 0.4f, 0.4f, 1.6f), new ObstacleSpot(-2.15f, -0.5f, 0.35f, 1.5f, 20f), new ObstacleSpot(2.15f, -0.5f, 0.35f, 1.5f, -20f) };
                return true;

            // 05 Yan Cizgi — KURNAZ. Atis cizgisi daraltildi: aciyi yerinden degil, sekmeden alacaksin.
            case 40:
                Set(level, 3.6f, 4, 8, 9, 11);
                level.marbles = Spots(-1.9f, 0.3f, -2f, 0.95f, -1.7f, 1.55f, -1.25f, 2f,
                                      -0.85f, 0.55f, -0.95f, 1.2f, -0.5f, 1.75f,
                                      1f, 0.45f, 1.6f, 0.8f, 1.1f, 1.3f, 1.7f, 1.65f, 1.05f, 2.05f);
                level.obstacles = new[] { new ObstacleSpot(0.2f, -0.5f, 1.6f, 0.4f, 12f), new ObstacleSpot(-0.15f, 0.9f, 0.35f, 1.1f, -15f) };
                level.shooterHalfWidth = 0.9f;
                return true;

            // 06 Toz Bulutu — nefes. Kenarda dort kolay, merkezde sekiz sikisik.
            case 41:
                Set(level, 3.6f, 4, 6, 8, 10);
                // Ic ice iki kare. Dis kare kenarda, ic kare korumada: disi temizlemek
                // kolay ama ici ancak dis kare acilinca goruyorsun.
                level.marbles = Spots(-1.5f, -0.1f, 0f, -0.1f, 1.5f, -0.1f, -1.5f, 1.4f, 1.5f, 1.4f,
                                      -1.5f, 2.9f, 0f, 2.9f, 1.5f, 2.9f,
                                      -0.6f, 0.8f, 0.6f, 0.8f, -0.6f, 2f, 0.6f, 2f, 0f, 1.4f);
                level.obstacles = new[] { new ObstacleSpot(0f, -0.9f, 1.8f, 0.4f), new ObstacleSpot(-2.6f, 1.4f, 0.4f, 1.6f, -20f), new ObstacleSpot(2.6f, 1.4f, 0.4f, 1.6f, 20f) };
                return true;

            // 07 Tasli Zemin — KURNAZ. Uc tas: iki yan siper ve uzak bir duvar. Alt ve ust sira ayri islerdir.
            case 42:
                Set(level, 3.6f, 4, 6, 7, 8);
                level.marbles = Spots(-2.5f, 0.5f, -1.9f, 0.6f, -1.2f, 0.7f, -0.5f, 0.75f, 0.2f, 0.75f, 0.9f, 0.7f, 1.6f, 0.6f, 2.4f, 0.5f,
                                      -1.5f, 2.25f, -0.8f, 2.35f, -0.1f, 2.4f, 0.6f, 2.35f);
                level.obstacles = new[] { new ObstacleSpot(-1.5f, -0.6f, 0.4f, 1.8f, 20f), new ObstacleSpot(1.5f, -0.6f, 0.4f, 1.8f, -20f), new ObstacleSpot(0f, 1.6f, 1.8f, 0.35f) };
                return true;

            // 08 Kale Diregi — iki direk arasindan gecis. Mahalle ortasindaki ikinci kilit.
            case 43:
                Set(level, 3.6f, 4, 4, 6, 9);
                level.marbles = Spots(-1.7f, 0.75f, -1.15f, 0.9f, -0.6f, 1f, -0.05f, 1.05f, 0.5f, 1f, 1.05f, 0.9f, 1.6f, 0.75f,
                                      -1f, 1.7f, -0.35f, 1.8f, 0.3f, 1.75f, 0.95f, 1.6f, 0f, 2.4f);
                level.obstacles = new[] { new ObstacleSpot(-1.1f, -0.3f, 0.35f, 1.6f), new ObstacleSpot(1.1f, -0.3f, 0.35f, 1.6f), new ObstacleSpot(0f, 2.9f, 1.6f, 0.35f) };
                level.starsToPass = 2;
                return true;

            // 09 Dar Aralik — KURNAZ. Ortada dar bir koridor, iki yanda erisimi zor kumeler.
            case 44:
                Set(level, 3.65f, 4, 8, 9, 11);
                level.marbles = Spots(0f, 1f, 0f, 1.7f, 0f, 2.4f,
                                      -2.1f, 0.4f, -2.6f, 1f, -2f, 1.4f, -2.5f, 2f, -1.8f, 2.2f,
                                      2.1f, 0.4f, 2.6f, 1f, 2f, 1.4f, 2.5f, 2f, 1.8f, 2.2f, 1.5f, 2.7f);
                level.obstacles = new[] { new ObstacleSpot(-0.9f, 0.2f, 0.4f, 2.4f), new ObstacleSpot(0.9f, 0.2f, 0.4f, 2.4f), new ObstacleSpot(-1.6f, -1.2f, 1.6f, 0.4f, 15f) };
                return true;

            // 10 Son Vurus — nefes ama uc atis. Ekonominin en cıplak hali: az atis, cok misket.
            case 45:
                Set(level, 3.65f, 3, 6, 9, 11);
                level.marbles = Spots(-2.6f, 0.7f, -2.2f, 1.5f, -1.5f, 2.2f, -0.6f, 2.6f, 0.4f, 2.65f, 1.3f, 2.35f, 2f, 1.75f, 2.5f, 0.95f,
                                      -1.2f, 0.9f, -0.3f, 1.1f, 0.6f, 1.05f, 1.4f, 0.8f, 0f, 1.8f, 1f, 1.75f);
                level.obstacles = new[] { new ObstacleSpot(0f, -0.8f, 1.6f, 0.4f) };
                return true;

            // 11 Saha Turnuvasi — KURNAZ. On sira tuzak: duz vurursan arkadaki kuleyi merkeze gomer.
            case 46:
                Set(level, 3.65f, 4, 8, 9, 11);
                // V: iki kol aticiya aciliyor, arkalarinda dort misket. Kolun ucundan
                // vurursan zincir yukari akar; ortadan vurursan hicbir sey olmaz.
                level.marbles = Spots(-2.4f, 2f, -1.95f, 1.55f, -1.5f, 1.1f, -1.05f, 0.65f, -0.6f, 0.2f,
                                      2.4f, 2f, 1.95f, 1.55f, 1.5f, 1.1f, 1.05f, 0.65f, 0.6f, 0.2f,
                                      -0.9f, 2.6f, -0.3f, 2.95f, 0.3f, 2.95f, 0.9f, 2.6f);
                level.obstacles = new[] { new ObstacleSpot(0f, -0.9f, 1.4f, 0.4f, 0f), new ObstacleSpot(0f, 1.8f, 1.2f, 0.35f, 0f) };
                return true;

            // 12 Ustalik Sinavi — SINAV. Uc ayri giris ve merkezde iki tuzak misketi.
            case 47:
                Set(level, 3.7f, 4, 7, 9, 12);
                level.marbles = Spots(-2.7f, 0.6f, -2.85f, 1.3f, -2.35f, 1.85f, -2f, 2.5f,
                                      -0.4f, 2.2f, 0.3f, 2.35f, -0.1f, 2.9f, 0.7f, 2.95f,
                                      2.3f, 0.3f, 2.8f, 0.9f, 2.25f, 1.4f, 2.7f, 1.85f, 2.15f, 2.35f, 1.7f, 0.8f,
                                      -1f, 0.9f, -0.3f, 1f);
                level.obstacles = new[] { new ObstacleSpot(0.7f, 0.2f, 0.4f, 2.4f, 15f), new ObstacleSpot(-1.6f, -0.6f, 1.5f, 0.4f, -20f), new ObstacleSpot(-0.2f, 1.6f, 1.5f, 0.35f, 5f) };
                level.starsToPass = 2;
                return true;

            // ---------------- MAHALLE MEYDANI · konu: hepsi bir arada ----------------
            // Son mahalle. Onceki dort mahallenin her dersi burada ayni bolumde karsina cikar.
            // Temizlenebilirlik hedefi ~%55: Toprak Saha'nin (%62) altinda, yani en zoru.
            // Ritim: 5-7-9-11 kurnaz (pay 0), 4-6-8-10 nefes (pay 2-3), 12 sinav.

            // 01 Cesme Cevresi — ogret/nefes.
            case 48:
                Set(level, 3.7f, 4, 6, 8, 11);
                level.marbles = Spots(0.53f, 1.73f, -0.53f, 1.73f, -0.53f, 0.67f, 0.53f, 0.67f, 1.6f, 1.2f, 1.131f, 2.331f,
                                      0f, 2.8f, -1.131f, 2.331f, -1.6f, 1.2f, -1.131f, 0.069f, -0f, -0.4f, 1.131f, 0.069f);
                level.obstacles = new[] { new ObstacleSpot(-2.6f, 0.4f, 0.4f, 1.6f, 25f), new ObstacleSpot(2.6f, 0.4f, 0.4f, 1.6f, -25f) };
                return true;

            // 02 Kaldirim — ogret/nefes.
            case 49:
                Set(level, 3.7f, 4, 6, 8, 11);
                level.marbles = Spots(-1.15f, 0.1f, -1.15f, 0.75f, -1.15f, 1.4f, -1.15f, 2.05f, -1.15f, 2.7f, 0.95f, 0.55f,
                                      0.95f, 1.2f, 0.95f, 1.85f, 0.95f, 2.5f, 2.3f, 0.3f, 2.6f, 0.9f, 2.1f, 1.5f);
                level.obstacles = new[] { new ObstacleSpot(0f, 1.1f, 0.35f, 2.2f), new ObstacleSpot(-1.6f, -0.8f, 1.6f, 0.4f, 10f) };
                return true;

            // 03 Mozaik Doseme — ogret/nefes.
            case 50:
                Set(level, 3.7f, 4, 5, 7, 10);
                level.marbles = Spots(1.7f, 1.5f, 1.133f, 2f, 0.567f, 2.5f, 0f, 3f, -0.567f, 2.5f, -1.133f, 2f, -1.7f, 1.5f,
                                      -1.133f, 1f, -0.567f, 0.5f, 0f, 0f, 0.567f, 0.5f, 1.133f, 1f, 0f, 1.5f);
                level.obstacles = new[] { new ObstacleSpot(0f, -0.9f, 1.5f, 0.4f), new ObstacleSpot(-2.5f, 1.2f, 0.35f, 1.4f, -20f), new ObstacleSpot(2.5f, 1.2f, 0.35f, 1.4f, 20f) };
                return true;

            // 04 Cinar Golgesi — nefes.
            case 51:
                Set(level, 3.75f, 4, 6, 8, 11);
                level.marbles = Spots(1.597f, 1.481f, 1.237f, 2.066f, 0.672f, 2.461f, 0f, 2.6f, -0.672f, 2.461f, -1.237f, 2.066f,
                                      -1.597f, 1.481f, -1.5f, 0f, -0.85f, -0.1f, 0.85f, -0.1f, 1.5f, 0f, -2.2f, 0.6f, 2.2f, 0.6f);
                level.obstacles = new[] { new ObstacleSpot(0f, 0.9f, 0.9f, 0.9f), new ObstacleSpot(-2.9f, -0.3f, 0.35f, 1.3f, 15f), new ObstacleSpot(2.9f, -0.3f, 0.35f, 1.3f, -15f) };
                return true;

            // 05 Bakkal Onu — KURNAZ.
            case 52:
                Set(level, 3.75f, 4, 7, 8, 10);
                level.marbles = Spots(-2.3f, 0.3f, -1.65f, 0.45f, -1f, 0.6f, -0.35f, 0.75f, 0.3f, 0.7f, 0.65f, 1.25f, 0.3f, 1.8f,
                                      -0.35f, 1.95f, -1f, 2.1f, -1.65f, 2.25f, -1.4f, 2.85f, -0.75f, 2.95f, -0.1f, 2.9f);
                level.obstacles = new[] { new ObstacleSpot(1.5f, 0.6f, 0.4f, 2.2f, -12f), new ObstacleSpot(-1.3f, 1.35f, 1.6f, 0.35f, 8f) };
                return true;

            // 06 Tas Basamak — nefes.
            case 53:
                Set(level, 3.75f, 4, 5, 7, 10);
                // Basamaklar kenara dogru kaydirildi, riser duvarlari kisaltildi.
                // Onceki hali 13 misketin ancak 4'unu birakiyordu (%31) -- Meydan bile o kadar zor degil.
                level.marbles = Spots(-2.3f, 0.6f, -1.7f, 0.6f, -1.1f, 0.6f, -0.5f, 0.6f,
                                      -0.2f, 1.7f, 0.4f, 1.7f, 1f, 1.7f, 1.6f, 1.7f, 2.2f, 1.7f,
                                      -1.5f, 2.7f, -0.9f, 2.7f, -0.3f, 2.7f, 0.3f, 2.7f);
                level.obstacles = new[] { new ObstacleSpot(1f, 1.15f, 1.4f, 0.35f), new ObstacleSpot(-1.1f, 2.2f, 1.3f, 0.35f) };
                return true;

            // 07 Avlu Kapisi — KURNAZ.
            case 54:
                Set(level, 3.8f, 4, 8, 9, 11);
                level.marbles = Spots(-1.2f, 0.5f, -0.4f, 0.5f, 0.4f, 0.5f, 1.2f, 0.5f, -1.2f, 2.9f, -0.4f, 2.9f, 0.4f, 2.9f,
                                      1.2f, 2.9f, -1.2f, 1.3f, -1.2f, 2.1f, 1.2f, 1.3f, 1.2f, 2.1f, -0.55f, 1.7f, 0.55f, 1.7f);
                level.obstacles = new[] { new ObstacleSpot(-1.5f, -0.5f, 1.8f, 0.4f), new ObstacleSpot(1.5f, -0.5f, 1.8f, 0.4f), new ObstacleSpot(0f, 1.7f, 0.5f, 0.5f) };
                return true;

            // 08 Isik Alti — nefes.
            case 55:
                Set(level, 3.8f, 4, 6, 8, 11);
                // Yelpaze disari acildi ve uzaktaki duvar kaldirildi: isin artik
                // kenara ulasiyor. Onceki hali 14 misketin 5'ini birakiyordu (%36).
                level.marbles = Spots(0.918f, 1.111f, 0.333f, 1.365f, -0.333f, 1.365f, -0.918f, 1.111f,
                                      1.377f, 1.766f, 0.499f, 2.148f, -0.499f, 2.148f, -1.377f, 1.766f,
                                      1.835f, 2.421f, 0.665f, 2.93f, -0.665f, 2.93f, -1.835f, 2.421f,
                                      -3.2f, -0.8f, 3.2f, -0.8f);
                level.obstacles = new[] { new ObstacleSpot(-2.7f, 0.6f, 0.4f, 1.6f, -25f), new ObstacleSpot(2.7f, 0.6f, 0.4f, 1.6f, 25f) };
                return true;

            // 09 Dar Gecit — KURNAZ.
            case 56:
                // Tavan 7 olculdu; gecis 8'di, yani normal misketle gecilemiyordu. 7'ye cekildi.
                Set(level, 3.8f, 4, 7, 9, 11);
                // Capraz koridor. Iki uzun duvar 35 derece egik; koridor asagi soldan
                // yukari saga akiyor. Duz atis hicbir zaman koridora girmiyor,
                // sag taraftaki kume ise ancak duvarin disindan dolasarak aliniyor.
                level.marbles = Spots(-1.413f, -1.202f, -1.051f, -0.686f, -0.689f, -0.17f, -0.328f, 0.346f,
                                      0.034f, 0.862f, 0.395f, 1.378f, 0.757f, 1.894f, 1.119f, 2.41f,
                                      1.813f, -0.169f, 2.215f, 0.404f, 2.617f, 0.977f,
                                      2.546f, -0.256f, 2.948f, 0.318f, 3.019f, 1.551f);
                level.obstacles = new[] { new ObstacleSpot(-1.45f, 0.05f, 0.4f, 2.8f, 35f), new ObstacleSpot(1.15f, 1.15f, 0.4f, 2.8f, 35f) };
                return true;

            // 10 Son Meydan — nefes.
            case 57:
                Set(level, 3.8f, 3, 4, 6, 9);
                level.marbles = Spots(0.8f, 1.3f, 1.391f, 1.862f, 1.583f, 2.828f, 0.247f, 2.061f, -0.105f, 2.796f, -0.964f, 3.277f,
                                      -0.647f, 1.77f, -1.455f, 1.663f, -2.179f, 0.994f, -0.647f, 0.83f, -0.795f, 0.028f,
                                      -0.382f, -0.867f, 0.247f, 0.539f, 0.964f, 0.151f, 1.942f, 0.267f);
                level.obstacles = new[] { new ObstacleSpot(0f, 1.3f, 0.5f, 0.5f), new ObstacleSpot(-1.9f, -1.2f, 1.6f, 0.4f, 25f) };
                return true;

            // 11 Mahalle Bulusmasi — KURNAZ.
            case 58:
                Set(level, 3.8f, 4, 6, 8, 10);
                level.marbles = Spots(0f, 1.5f, 0.62f, 1.5f, 0.31f, 2.037f, -0.31f, 2.037f, -0.62f, 1.5f, -0.31f, 0.963f,
                                      0.31f, 0.963f, 1.24f, 1.5f, 0.62f, 2.574f, -0.62f, 2.574f, -1.24f, 1.5f, -0.62f, 0.426f,
                                      0.62f, 0.426f, -2.7f, 0.4f, 2.7f, 0.4f);
                level.obstacles = new[] { new ObstacleSpot(0f, -0.4f, 2f, 0.4f), new ObstacleSpot(-2.4f, 1.6f, 0.4f, 1.5f, -30f), new ObstacleSpot(2.4f, 1.6f, 0.4f, 1.5f, 30f) };
                return true;

            // 12 Ustalik Sinavi — SINAV.
            case 59:
                Set(level, 3.85f, 4, 5, 7, 11);
                level.marbles = Spots(0.346f, 0.964f, 0.716f, 1.602f, 0.347f, 2.216f, -0.348f, 2.346f, -0.949f, 1.984f,
                                      -1.209f, 1.337f, -1.066f, 0.656f, -0.593f, 0.15f, 0.067f, -0.06f, 0.747f, 0.059f,
                                      1.305f, 0.464f, 1.64f, 1.066f, 1.702f, 1.752f, 1.492f, 2.407f, 1.052f, 2.934f, 0.45f, 3.265f);
                level.obstacles = new[] { new ObstacleSpot(0f, 1.4f, 0.35f, 0.35f), new ObstacleSpot(-2.4f, -0.6f, 1.5f, 0.4f, 30f), new ObstacleSpot(2.7f, 2.3f, 1.4f, 0.35f, -40f) };
                level.starsToPass = 2;
                return true;

        }

        return false;
    }

    // Ortak ayarlar: çember büyüklüğü, atış hakkı ve yıldız hedefleri.
    private static void Set(LevelData level, float size, int shots, int one, int two, int three)
    {
        level.shape = ArenaShape.Circle;
        level.arenaSize = size;
        level.shotCount = shots;
        level.oneStarTarget = one;
        level.twoStarTarget = two;
        level.threeStarTarget = three;
        level.starsToPass = 1;
        level.marbles = null;
        level.obstacles = null;
        level.obstacleCount = 0;
        level.shooterHalfWidth = 0f;
        level.shooterOffsetX = 0f;
        level.shooterStartPosition = new Vector3(0f, .25f, -4.2f);
    }

    private static MarbleSpot[] Spots(params float[] pairs)
    {
        var list = new MarbleSpot[pairs.Length / 2];
        for (int i = 0; i < list.Length; i++) list[i] = new MarbleSpot(pairs[i * 2], pairs[i * 2 + 1]);
        return list;
    }

    private static MarbleSpot[] Row(float z, params float[] xs)
    {
        var list = new MarbleSpot[xs.Length];
        for (int i = 0; i < xs.Length; i++) list[i] = new MarbleSpot(xs[i], z);
        return list;
    }

    // Çemberin uzak kenarına yay şeklinde dizer.
    private static MarbleSpot[] Arc(float radius, float fromDegrees, float toDegrees, int count)
    {
        var list = new MarbleSpot[count];
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0f : i / (float)(count - 1);
            float a = Mathf.Lerp(fromDegrees, toDegrees, t) * Mathf.Deg2Rad;
            list[i] = new MarbleSpot(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius);
        }
        return list;
    }
}
