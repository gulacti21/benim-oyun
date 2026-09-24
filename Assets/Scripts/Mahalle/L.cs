using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

// DİL SİSTEMİ (Türkçe / İngilizce)
// Koddaki Türkçe metin ANAHTARDIR. İngilizce seçiliyse tablodan karşılığı
// gelir; tabloda yoksa Türkçe olduğu gibi görünür (oyun bozulmaz).
// Değişken içeren metinler {0}, {1} şablonuyla L.F ile yazılır.
// Dil: Ayarlar'dan seçilir; seçilmemişse telefon Türkçe ise Türkçe, değilse İngilizce.
public static class L
{
    private static readonly CultureInfo Tr = new CultureInfo("tr-TR");

    public static bool English
    {
        get
        {
            string saved = MahalleProfile.Data.language;
            if (saved == "en") return true;
            if (saved == "tr") return false;
            return Application.systemLanguage != SystemLanguage.Turkish;
        }
    }

    public static void SetEnglish(bool english)
    {
        MahalleProfile.Data.language = english ? "en" : "tr";
        MahalleProfile.Save();
    }

    public static string T(string tr)
    {
        if (string.IsNullOrEmpty(tr) || !English) return tr;
        return En.TryGetValue(tr, out var en) ? en : tr;
    }

    public static string F(string trTemplate, params object[] args) => string.Format(T(trTemplate), args);

    public static string Up(string s) => English ? s.ToUpperInvariant() : s.ToUpper(Tr);

    public static void SetTextL(this TMP_Text label, string value) => label.SetText(T(value));

    public static bool Has(string tr) => En.ContainsKey(tr);

    private static readonly Dictionary<string, string> En = new Dictionary<string, string>
    {
        // ---- Genel / menü ----
        {"mahallenin en iyi nişancısı kim?", "who's the best shot on the block?"},
        {"DEVAM ET", "CONTINUE"}, {"Devam et", "Continue"}, {"OYNA", "PLAY"}, {"Oyna", "Play"},
        {"KESEM", "POUCH"}, {"GÖREVLER", "TASKS"}, {"ONLİNE", "ONLINE"}, {"AYARLAR", "SETTINGS"},
        {"MENÜ", "MENU"}, {"MAHALLE", "MAP"}, {"İSTATİSTİK", "STATS"},
        {"GERİ", "BACK"}, {"TAMAM", "OK"}, {"ANLADIM", "GOT IT"}, {"VAZGEÇ", "CANCEL"}, {"SIFIRLA", "RESET"},
        {"ANA MENÜ", "MAIN MENU"}, {"TEKRAR DENE", "TRY AGAIN"}, {"MAHALLEYE DÖN", "BACK TO MAP"},
        {"SES VE TİTREŞİM", "SOUND & VIBRATION"}, {"Mola", "Paused"}, {"Bir nefes al", "Take a breath"},
        {"OYUNA DÖN", "BACK TO GAME"}, {"PAYLAŞ", "SHARE"}, {"Atışın tamamlanmasını bekle.", "Wait for the shot to finish."},
        {"HER MİSKETİN BİR HİKÂYESİ VAR", "EVERY MARBLE HAS A STORY"},
        {"MAHALLEDEKİ İZİN", "YOUR MARK ON THE BLOCK"},
        {"KÜÇÜK HEDEFLER, YENİ BONCUKLAR", "SMALL GOALS, NEW BEADS"},
        {"ÖNİZLEME · TÜM BÖLÜMLER AÇIK · KAYIT YAPILMAZ", "PREVIEW · ALL LEVELS OPEN · NOT SAVED"},
        {"TEST · TÜM BÖLÜMLER AÇIK · SINIRSIZ BONCUK", "TEST · ALL LEVELS OPEN · UNLIMITED BEADS"},
        {"TEST · SINIRSIZ BONCUK", "TEST · UNLIMITED BEADS"}, {"TEST · TÜM BÖLÜMLER AÇIK", "TEST · ALL LEVELS OPEN"},
        {"Misketin işe yarar bir yerde kalmadı. Hakkın iade edildi.", "Your marble didn't stop anywhere useful. Your charge was refunded."},

        // ---- Ayarlar ----
        {"Ayarlar", "Settings"}, {"SES: {0}", "SOUND: {0}"}, {"TİTREŞİM: {0}", "VIBRATION: {0}"},
        {"AÇIK", "ON"}, {"KAPALI", "OFF"}, {"NASIL OYNANIR", "HOW TO PLAY"},
        {"İLERLEMEYİ SIFIRLA", "RESET PROGRESS"},
        {"Boncuklar oyun içinden kazanılır. Gerçek para işlemi yoktur.", "Beads are earned in the game. There are no real-money purchases."},
        {"İlerleme sıfırlansın mı?", "Reset your progress?"},
        {"Bu sürümdeki yıldızların, boncukların ve koleksiyonun başlangıca döner.", "Your stars, beads and collection will go back to the start."},

        // ---- Mahalleler, açıklamalar, tema ----
        {"Apartman Önü", "Front Stoop"}, {"Okul Bahçesi", "Schoolyard"}, {"Park", "Park"},
        {"Toprak Saha", "Dirt Field"}, {"Mahalle Meydanı", "Town Square"},
        {"İlk çizgi, ilk atış. Mahalle seni bekliyor.", "First line, first shot. The block is waiting."},
        {"Teneffüs başladı. Şimdi sıra sende.", "Recess has started. Your turn now."},
        {"Taşların arasından kendi yolunu bul.", "Find your own way between the stones."},
        {"Geniş saha, yeni dizilimler, büyük atışlar.", "Wide field, new layouts, big shots."},
        {"Bütün yollar bu meydana çıkar.", "All roads lead to this square."},
        {"Sabah serinliği · karo ve beton", "Cool morning · tiles and concrete"},
        {"Öğle güneşi · asfalt ve saha çizgileri", "Noon sun · asphalt and court lines"},
        {"İkindi güneşi · toprak ve tebeşir", "Afternoon sun · dirt and chalk"},
        {"Tozlu öğle sonrası · kuru toprak", "Dusty afternoon · dry dirt"},
        {"Akşamüstü · taş döşeme ve çeşme", "Early evening · cobblestones and fountain"},

        // ---- Bölüm adları ----
        {"Kapı Eşiği", "Doorstep"}, {"Merdiven Yanı", "By the Stairs"}, {"Saksılı Köşe", "Flowerpot Corner"},
        {"Garaj Önü", "Garage Front"}, {"Kapıcı Dairesi", "Janitor's Door"}, {"Bisiklet Yeri", "Bike Rack"},
        {"Çamaşır İpi", "Clothesline"}, {"Dar Aralık", "Tight Gap"}, {"Kova Arkası", "Behind the Bucket"},
        {"Son Basamak", "Last Step"}, {"Apartman Buluşması", "Stoop Meetup"}, {"Ustalık Sınavı", "Master Test"},
        {"Seksek Alanı", "Hopscotch"}, {"Pota Altı", "Under the Hoop"}, {"Duvar Dibi", "Against the Wall"},
        {"Tören Çizgileri", "Assembly Lines"}, {"Bayrak Direği", "Flagpole"}, {"Kantin Önü", "Canteen Front"},
        {"Sıra Arası", "Between the Desks"}, {"Dar Koridor", "Narrow Hallway"}, {"Teneffüs Zili", "Recess Bell"},
        {"Son Ders", "Last Class"}, {"Okul Turnuvası", "School Tournament"},
        {"Giriş Yolu", "Entrance Path"}, {"Bank Yanı", "By the Bench"}, {"Ağaç Dibi", "Under the Tree"},
        {"Çim Kenarı", "Grass Edge"}, {"Salıncak Altı", "Under the Swings"}, {"Havuz Başı", "By the Pond"},
        {"Yürüyüş Yolu", "Walking Path"}, {"Çiçeklik", "Flower Bed"}, {"Dar Patika", "Narrow Trail"},
        {"Son Bank", "Last Bench"}, {"Park Buluşması", "Park Meetup"},
        {"Çakıllı Köşe", "Gravel Corner"}, {"Kale Arkası", "Behind the Goal"}, {"Geniş Açıklık", "Open Ground"},
        {"Orta Saha", "Midfield"}, {"Yan Çizgi", "Sideline"}, {"Toz Bulutu", "Dust Cloud"},
        {"Taşlı Zemin", "Rocky Ground"}, {"Kale Direği", "Goalpost"}, {"Son Vuruş", "Final Kick"},
        {"Saha Turnuvası", "Field Tournament"},
        {"Çeşme Çevresi", "Around the Fountain"}, {"Kaldırım", "Sidewalk"}, {"Mozaik Döşeme", "Mosaic Floor"},
        {"Çınar Gölgesi", "Plane Tree Shade"}, {"Bakkal Önü", "Corner Shop"}, {"Taş Basamak", "Stone Steps"},
        {"Avlu Kapısı", "Courtyard Gate"}, {"Işık Altı", "Under the Lamp"}, {"Dar Geçit", "Narrow Pass"},
        {"Son Meydan", "Last Square"}, {"Mahalle Buluşması", "Block Meetup"},

        // ---- Harita 2: Memleket ----
        {"Mahalle", "The Block"}, {"Memleket", "Hometown"},
        {"Apartmandan meydana", "From the stoop to the square"}, {"Yaz tatili, köyde misket", "Summer break, marbles in the village"},
        {"{0} haritası, {1} haritasının son bölümünü geçince açılır.", "{0} opens when you beat the last level of {1}."},
        {"Sahil", "Beach"}, {"Köy Meydanı", "Village Square"}, {"Yayla", "Highlands"},
        {"Kasaba Pazarı", "Town Market"}, {"Bayram Yeri", "Fairground"},
        {"Kum ayağının altında. Misket de yavaşlıyor.", "Sand under your feet. The marble slows down too."},
        {"Yağmur yeni dindi, çamura dikkat.", "The rain just stopped. Watch the mud."},
        {"Serin rüzgâr, eğimli çayır.", "Cool breeze, sloping meadow."},
        {"Tezgâhların arasından bant atışı.", "Bank shots between the stalls."},
        {"Bütün yaz bu güne çıkar.", "The whole summer leads to this day."},
        {"Sabah serinliği · ıslak kum", "Cool morning · wet sand"},
        {"Yağmur sonrası · toprak ve çamur", "After the rain · dirt and mud"},
        {"Serin rüzgâr · eğimli çayır", "Cool breeze · sloping meadow"},
        {"Kalabalık öğle · tezgâh arası", "Busy noon · between the stalls"},
        {"Akşam ışıkları · büyük final", "Evening lights · the big finale"},
        {"Kumsal Girişi", "Beach Entrance"}, {"Havlu Yanı", "By the Towel"}, {"Kumdan Kale", "Sandcastle"},
        {"Şemsiye Altı", "Under the Umbrella"}, {"Midye Kıyısı", "Mussel Shore"}, {"Dalga Çizgisi", "Wave Line"},
        {"İskele Başı", "Pier Head"}, {"Kayık Arkası", "Behind the Boat"}, {"Sıcak Kum", "Hot Sand"},
        {"Son Dalga", "Last Wave"}, {"Sahil Buluşması", "Beach Meetup"},
        {"Çınar Altı", "Under the Plane Tree"}, {"Kahve Önü", "Teahouse Front"}, {"Su Birikintisi", "Puddle"},
        {"Muhtarlık", "Village Office"}, {"Çeşme Başı", "At the Fountain"}, {"Tavuk Kümesi", "Chicken Coop"},
        {"Harman Yeri", "Threshing Floor"}, {"Samanlık", "Haystack"}, {"Dar Sokak", "Narrow Lane"},
        {"Son Yağmur", "Last Rain"}, {"Köy Buluşması", "Village Meetup"},
        {"Yayla Yolu", "Highland Road"}, {"Çoban Çeşmesi", "Shepherd's Spring"}, {"Buzlu Pınar", "Icy Spring"},
        {"Ahşap Ev", "Wooden House"}, {"Çam Gölgesi", "Pine Shade"}, {"Serin Çayır", "Cool Meadow"},
        {"Sisli Tepe", "Misty Hill"}, {"Kaya Yanı", "By the Rock"}, {"Dik Yamaç", "Steep Slope"},
        {"Son Tepe", "Last Hill"}, {"Yayla Şenliği", "Highland Fest"},
        {"Pazar Girişi", "Market Gate"}, {"Karpuz Tezgâhı", "Watermelon Stall"}, {"Kasa Yığını", "Crate Stack"},
        {"Domates Sırası", "Tomato Row"}, {"Terazi Önü", "By the Scales"}, {"Tezgâh Arası", "Between the Stalls"},
        {"Sepetçi", "Basket Maker"}, {"Bakır Tezgâhı", "Copper Stall"}, {"Kalabalık Köşe", "Crowded Corner"},
        {"Son Tezgâh", "Last Stall"}, {"Pazar Buluşması", "Market Meetup"},
        {"Bayram Sabahı", "Holiday Morning"}, {"Salıncak Yeri", "Swing Ride"}, {"Pamuk Şeker", "Cotton Candy"},
        {"Atlıkarınca", "Carousel"}, {"Davul Zurna", "Drum and Pipe"}, {"Bayram Harçlığı", "Holiday Money"},
        {"Fener Alayı", "Lantern Parade"}, {"Lunapark", "Funfair"}, {"Gece Çukuru", "Night Pit"},
        {"Son Bayram", "Last Holiday"}, {"Büyük Buluşma", "Grand Meetup"},

        // ---- Misketler ve güçler ----
        {"Bal Köpüğü", "Honey Foam"}, {"Deniz Camı", "Sea Glass"}, {"Kedi Gözü", "Cat's Eye"},
        {"Gün Batımı", "Sunset"}, {"Galaksi", "Galaxy"}, {"Usta İncisi", "Master Pearl"},
        {"Ağır Misket", "Heavy Marble"}, {"İnce Misket", "Slim Marble"}, {"Sekici Misket", "Bouncy Marble"},
        {"Kaygan Misket", "Slick Marble"},
        {"Daha ağır darbe, daha düşük hız.", "Harder hit, lower speed."},
        {"Dar aralıklara sığar; isabeti daha zor.", "Fits tight gaps; harder to aim."},
        {"Daha canlı seker; duracağı yeri iyi hesapla.", "Bounces livelier; plan where it stops."},
        {"Daha uzun yuvarlanır; mesafeyi iyi ayarla.", "Rolls farther; judge the distance."},
        {"Baş Misket", "Big Shooter"}, {"Demir Misket", "Iron Shooter"}, {"Usta Gözü", "Master's Eye"}, {"Yerinde Kal", "Stay Put"},
        {"Bir sonraki atışta büyür. Sık kümeleri dağıtır.", "Grows for the next shot. Breaks up tight clusters."},
        {"Küçük ve ağır. Dar aralıktan sert vurur.", "Small and heavy. Hits hard through tight gaps."},
        {"İlk temas noktasını gösterir. Hassas nişan al.", "Shows the first contact point. Aim precisely."},
        {"Bu atıştan sonra çizgiye dönmezsin. Misketin durduğu yerden devam edersin, sadece bir tur.", "After this shot you don't go back to the line. You shoot from where your marble stops, for one turn."},

        // ---- Koleksiyon (KESEM sekmesi) ----
        {"Misket koleksiyonun", "Your marble collection"},
        {"Klasik misketler aşınmaz. Özellikli misketler aşağıda.", "Classic marbles never wear out. Special marbles are below."},
        {"ÖZELLİKLİ MİSKETLER", "SPECIAL MARBLES"},
        {"{0} boncuk · {1} atış ömür · Tam yenileme {2} boncuk.", "{0} beads · {1}-shot life · Full repair {2} beads."},
        {"Normal misket her zaman ücretsiz ve aşınmaz.", "The normal marble is always free and never wears out."},
        {"{0} / {1} ATIŞ", "{0} / {1} SHOTS"}, {"{0} BONCUK", "{0} BEADS"}, {"{0} BONCUK · AL", "{0} BEADS · BUY"},
        {"AŞINDI", "WORN OUT"}, {"KUŞANILDI", "EQUIPPED"}, {"KUŞAN", "EQUIP"},
        {"Yeterli boncuk yok veya misket yenilenmeli.", "Not enough beads, or the marble needs a repair."},
        {"TAM YENİLE · {0} BONCUK", "FULL REPAIR · {0} BEADS"},
        {"Yenilemek için {0} boncuk gerekiyor.", "You need {0} beads to repair."},
        {"Özel güç atışında özelliği durur, ömrü azalmaz.", "On power shots its trait pauses and it doesn't wear."},

        // ---- İstatistik ----
        {"Karnen", "Report Card"}, {"Bitirdiğin her bölüm buraya yazılır.", "Every level you finish is recorded here."},
        {"TOPLAM ÇIKARDIĞIN MİSKET", "TOTAL MARBLES KNOCKED OUT"}, {"TEK ATIŞTA REKORUN", "BEST SINGLE SHOT"},
        {"EN ÇOK OYNADIĞIN MAHALLE", "MOST PLAYED AREA"}, {"{0} MİSKET", "{0} MARBLES"}, {"HENÜZ YOK", "NONE YET"},
        {"Bitirdiğin bölümlerde çemberden çıkardıkların", "Knocked out of the ring in levels you finished"},
        {"Tek bir atışla aynı anda çıkardığın en çok misket", "Most marbles knocked out with a single shot"},
        {"{0} bölüm bitirdin", "{0} levels finished"}, {"Bir bölüm bitirince burada görünür", "Shows up after you finish a level"},

        // ---- Görevler ----
        {"Mahallenin sana işi var", "The block has jobs for you"},
        {"Oyna, hedefleri tamamla, ödülünü buradan al.", "Play, reach the goals, collect your reward here."},
        {"Üç kez bölüm kazan", "Win three levels"}, {"Toplam 20 misket çıkar", "Knock out 20 marbles in total"},
        {"Üç bölümde üç yıldız al", "Get three stars in three levels"},
        {"{0} / {1}    +{2} BONCUK", "{0} / {1}    +{2} BEADS"},
        {"ALINDI", "CLAIMED"}, {"ÖDÜLÜ AL", "CLAIM"}, {"SÜRÜYOR", "IN PROGRESS"}, {"USTALIK ROZETLERİ", "MASTERY BADGES"},

        // ---- Harita ----
        {"SIRADAKİ", "NEXT"}, {"{0} YILDIZ GEREKLİ", "{0} STARS NEEDED"},
        {"Bu bölüm için önceki bölümde {0} yıldız almalısın.", "You need {0} stars on the previous level to open this one."},
        {"Önce bir önceki bölümü tamamla.", "Finish the previous level first."},

        // ---- Oyun ekranı ----
        {"NASIL OYNANIR ", "HOW TO PLAY "}, {"ÇIKAN MİSKET", "KNOCKED OUT"}, {"KALAN ATIŞ", "SHOTS LEFT"},
        {"HEDEF: BÜTÜN MİSKETLERİ ÇEMBERİN DIŞINA ÇIKAR", "GOAL: KNOCK ALL MARBLES OUT OF THE RING"},
        {"HEDEF: ÇEMBERDEN EN AZ {0} MİSKET ÇIKAR", "GOAL: KNOCK AT LEAST {0} MARBLES OUT OF THE RING"},
        {"KESE", "POUCH"}, {"Özel misket seç", "Pick a power"}, {"NORMAL MİSKET", "NORMAL MARBLE"}, {"ATIŞ GÜCÜ", "SHOT POWER"},
        {"Geri çek, nişan al, bırak", "Pull back, aim, release"},
        {"Atıcıyı taşımak için alt çizgide bir yere dokun.", "Tap anywhere on the bottom line to move your shooter."},
        {"Misketler duruluyor…", "Marbles are settling…"}, {"Gücü ayarla ve bırak", "Set the power and release"},
        {"Misketin durduğu yerden atıyorsun.", "You're shooting from where your marble stopped."},
        {"Çizgiye dokunarak atıcının yerini değiştirebilirsin.", "Tap the line to move your shooter."},
        {"Kesen", "Your Pouch"},
        {"Seçim ücretsiz. Bir kullanım yalnızca atışta harcanır.", "Picking is free. A charge is only used when you shoot."},
        {"Boncuk kazanmak için normal misketle oynayabilirsin.", "Play with the normal marble to earn beads."},
        {"ÜCRETSİZ HAK: {0}", "FREE CHARGES: {0}"}, {"{0} BONCUK / ATIŞ", "{0} BEADS / SHOT"},
        {"SEÇİLİ · DOKUNARAK VAZGEÇ", "SELECTED · TAP TO CANCEL"},

        // ---- Sonuç ----
        {"MAHALLE USTASI!", "BLOCK MASTER!"}, {"GÜZEL ATIŞLAR!", "NICE SHOTS!"}, {"BİR DAHA DENE", "TRY ONCE MORE"},
        {"{0} / {1} misket çıkardın", "You knocked out {0} / {1}"},
        {"Kesendeki güçler yardımcı olabilir. Normal misketle de geçebilirsin.", "The powers in your pouch can help. You can also pass with the normal marble."},
        {"+{0} BONCUK", "+{0} BEADS"},
        {"Bu bölümden boncuk aldın. Yeni yıldız daha fazla kazandırır.", "You already got beads here. New stars earn more."},
        {"MAHALLE TAMAMLANDI · {0} BONCUK BONUS", "AREA COMPLETE · {0} BONUS BEADS"},
        {"Mahalle misketi zaten kesende", "The area marble is already in your pouch"}, {"KOLEKSİYON ÖDÜLÜ", "COLLECTION REWARD"},
        {"Sonraki bölümü açmak için {0} yıldız gerekiyor.", "You need {0} stars to open the next level."},
        {"Yeni rekorlar ve görevler daha fazla boncuk kazandırır.", "New records and tasks earn more beads."},
        {"İpucu: Alt çizgide yer değiştirip kümeye yandan vur.", "Tip: move along the bottom line and hit the cluster from the side."},
        {"SONRAKİ BÖLÜM", "NEXT LEVEL"}, {"REKORUNU GELİŞTİR", "BEAT YOUR RECORD"}, {"MAHALLE HARİTASI", "AREA MAP"},

        // ---- Paylaş kartı ----
        {"ATIŞTA", "SHOTS"}, {"{0} / {1} MİSKET", "{0} / {1} MARBLES"}, {"Sen kaç atışta bitirirsin?", "How many shots will you need?"},
        {"MİSKO · {0} {1} bölümünü {2} atışta {3} yıldızla bitirdim. Sen kaç atışta bitirirsin?",
         "MISKO · I finished {0} {1} in {2} shots with {3} stars. How many shots will you need?"},

        // ---- Öğretici ----
        {"1/5 · YERİNİ SEÇ (SAĞ)", "1/5 · PICK YOUR SPOT (RIGHT)"}, {"1/5 · YERİNİ SEÇ (SOL)", "1/5 · PICK YOUR SPOT (LEFT)"},
        {"2/5 · GERİ ÇEK", "2/5 · PULL BACK"}, {"3/5 · NİŞAN AL VE BIRAK", "3/5 · AIM AND RELEASE"},
        {"BAKALIM…", "LET'S SEE…"}, {"5/5 · HEPSİNİ ÇIKAR", "5/5 · KNOCK THEM ALL OUT"}, {"4/5 · KESEM", "4/5 · POUCH"},
        {"Atmadan önce yerini ayarlarsın. Misketin çizgi boyunca kayar. Sağdaki parlayan noktaya dokun.",
         "Before shooting you pick your spot. Your marble slides along the line. Tap the glowing spot on the right."},
        {"Güzel! Şimdi soldaki parlayan noktaya dokun. Her atıştan önce en iyi açıyı böyle bulursun.",
         "Nice! Now tap the glowing spot on the left. This is how you find the best angle before every shot."},
        {"Misketine dokun ve parmağını GERİ çek. Ne kadar çekersen o kadar güçlü atar.",
         "Touch your marble and pull your finger BACK. The further you pull, the harder it shoots."},
        {"Kesik çizgi misketin gideceği yönü gösterir. Kümeye çevir ve parmağını bırak.",
         "The dashed line shows where your marble will go. Point it at the cluster and let go."},
        {"Misketler duruluyor.", "Marbles are settling."},
        {"Güçleri öğrendin, saha yeniden dizildi. Şimdi bölümü bitir: bütün misketleri çıkar. İstersen kesendeki güçleri kullanabilirsin, öğreticide bedava.",
         "You've learned the powers and the board is reset. Now finish the level: knock out every marble. You can use your pouch powers, they're free in the tutorial."},
        {"Harika, çemberden çıkan misket senin! Zor anlar için bir kesen var. Soldaki KESEM'e dokun.",
         "Great, a marble knocked out of the ring is yours! You have a pouch for tough moments. Tap POUCH on the left."},
        {"KESEM 1/4 · BAŞ MİSKET", "POUCH 1/4 · BIG SHOOTER"}, {"KESEM 2/4 · DEMİR MİSKET", "POUCH 2/4 · IRON SHOOTER"},
        {"KESEM 3/4 · USTA GÖZÜ", "POUCH 3/4 · MASTER'S EYE"}, {"KESEM 4/4 · YERİNDE KAL", "POUCH 4/4 · STAY PUT"},
        {"YERİNDE KAL · ŞİMDİ ORADAN AT", "STAY PUT · NOW SHOOT FROM THERE"}, {"YERİNDE KAL · TEKRAR DENE", "STAY PUT · TRY AGAIN"},
        {"Misketin büyüdü! Büyük misket sık kümeleri dağıtır. Kümeye at ve farkı gör.",
         "Your marble grew! A big marble breaks up tight clusters. Shoot at the cluster and see the difference."},
        {"Misketin küçük ve ağır oldu. Dar aralıktan geçip sert vurur. Bir misketi hedefle ve at.",
         "Your marble is now small and heavy. It slips through gaps and hits hard. Pick a marble and shoot."},
        {"Nişan alırken misketin İLK nereye değeceği gösterilir. Çek, işarete bak, tam isabetle bırak.",
         "While aiming you see where your marble will hit FIRST. Pull, check the marker, release on target."},
        {"Bu güç farklı: misketin atıştan sonra ÇİZGİYE DÖNMEZ, durduğu yerde kalır. Çembere yakın durması için hafif at.",
         "This one is different: after the shot your marble DOESN'T GO BACK to the line, it stays where it stops. Shoot softly so it stops near the ring."},
        {"Misketin durduğu yerde kaldı. Bu atışı oradan yapıyorsun: yakından nişan al ve at. Sonra yine çizgiye döner.",
         "Your marble stayed where it stopped. You shoot from there this time: aim up close and shoot. Then it goes back to the line."},
        {"Misketin çemberden uzakta kaldı, çizgiye döndü (normal oyunda hakkın iade edilir). Daha HAFİF at, çembere yakın dursun.",
         "Your marble stopped too far from the ring and went back to the line (in a normal game the charge is refunded). Shoot SOFTER so it stops near the ring."},
        {"Misketi çemberin DIŞINA kadar itmelisin. Biraz daha geri çek, daha güçlü atar.",
         "You need to push a marble OUT of the ring. Pull back a bit more for a stronger shot."},
        {"ÖNCE YERİNİ SEÇ", "PICK YOUR SPOT FIRST"},
        {"Misketini çekmeden önce {0} parlayan noktaya dokun.", "Before pulling, tap the glowing spot on the {0}."},
        {"sağdaki", "right"}, {"soldaki", "left"},
        {"Zorlandığın bölümlerde atıştan önce bir güç seçebilirsin. Her güçten 1 ÜCRETSİZ hakkın var, sonrası boncukla.",
         "On tough levels you can pick a power before a shot. You get 1 FREE charge of each; after that they cost beads."},
        {"Boncukları bölüm bitirerek ve görevlerle kazanırsın. Her bölüm normal misketle de geçilebilir.",
         "You earn beads by finishing levels and tasks. Every level can be passed with the normal marble."},
        {"HAZIRSIN!", "YOU'RE READY!"}, {"1. bölüm tamam · {0} yıldız", "Level 1 done · {0} stars"}, {" · +{0} boncuk", " · +{0} beads"},
        {"Ne kadar çok misket çıkarırsan o kadar çok yıldız kazanırsın.", "The more marbles you knock out, the more stars you earn."},
        {"Her bölümde atış hakkın sınırlı. Hedefe ulaşırsan sonraki bölüm açılır.", "Every level has limited shots. Reach the goal to open the next level."},
        {"Zorlanırsan KESEM'deki güçleri dene. Yine de her bölüm normal misketle geçilebilir.", "If you get stuck, try the powers in your POUCH. Still, every level can be passed with the normal marble."},
        {"2. BÖLÜME GEÇ", "GO TO LEVEL 2"}, {"GEÇ", "SKIP"},

        {"DİL: {0}", "LANGUAGE: {0}"},
        {"MİSKO", "MISKO"}, {"Kesem", "Pouch"},
        {"GÜNÜN BÖLÜMÜ", "DAILY LEVEL"}, {"1. bölümü bitirince açılır", "Opens after level 1"},
        {"BUGÜN TAMAM · SERİ {0} GÜN", "DONE TODAY · {0}-DAY STREAK"}, {"SERİ {0} GÜN · +{1} BONCUK", "{0}-DAY STREAK · +{1} BEADS"},
        {"Günün bölümü çok yakında!", "Daily level coming soon!"}, {"Günün bölümü 1. bölümü bitirince açılır.", "The daily level opens after you finish level 1."},
        {"GÜNÜN BÖLÜMÜ TAMAM!", "DAILY LEVEL DONE!"}, {"+{0} BONCUK · SERİ {1} GÜN", "+{0} BEADS · {1}-DAY STREAK"},
        {"Bugünün ödülünü aldın. Yarın yeni bölüm!", "You got today's reward. New level tomorrow!"},
        {"Bugün istediğin kadar deneyebilirsin. Yarın yeni bölüm gelir.", "Try as often as you like today. A new level comes tomorrow."},
        {"MİSKO · Günün bölümünü ({0}) {1} atışta {2} yıldızla bitirdim. Sen kaç atışta bitirirsin?", "MISKO · I beat the daily level ({0}) in {1} shots with {2} stars. How many shots will you need?"},
        {"Günün Bölümü", "Daily Level"},
        {"SONSUZ", "ENDLESS"}, {"SONSUZ ÇEMBER", "ENDLESS RING"}, {"Sonsuz Çember", "Endless Ring"},
        {"REKOR {0}", "BEST {0}"}, {"SÜRE YARIŞI", "TIME RUSH"}, {"KADEME {0}", "STAGE {0}"}, {"SÜRE", "TIME"},
        {"+{0} SANİYE", "+{0} SECONDS"}, {"SÜRE BİTTİ", "TIME UP"},
        {"REKOR: {0}", "BEST: {0}"}, {"REKOR: {0} · HER MİSKET +{1} SANİYE", "BEST: {0} · EVERY MARBLE +{1} SECONDS"},
        {"YENİ REKOR!", "NEW BEST!"}, {"misket çıkardın", "marbles knocked out"},
        {"MİSKO · Sonsuz Çember'de {0} misket çıkardım. Sen kaç yaparsın?", "MISKO · I knocked out {0} marbles in Endless Ring. Can you beat it?"},
        {"MİSKET", "MARBLES"},
        {"BİLDİRİM: {0}", "NOTIFICATIONS: {0}"}, {"Günün bölümü hazır", "Today's level is ready"},
        {"Bugünün bölümünü oyna, serini sürdür.", "Play today's level and keep your streak."},
        {"USTA SEVİYESİ {0}", "MASTERY LEVEL {0}"}, {"{0} / {1} puan", "{0} / {1} points"},
        {"BAŞARIMLAR", "ACHIEVEMENTS"},
        {"İlk Atış", "First Shot"}, {"Bir bölüm kazan", "Win a level"},
        {"Mahalle Ustası", "Area Master"}, {"Bir mahalleyi tamamla", "Complete an area"},
        {"Keskin Nişancı", "Sharp Shooter"}, {"Tek atışta 4 misket çıkar", "Knock out 4 marbles in one shot"},
        {"Koleksiyoncu", "Collector"}, {"5 misket kaplaması topla", "Collect 5 marble skins"},
        {"Sadık Oyuncu", "Regular"}, {"5 gün üst üste günün bölümünü bitir", "Finish the daily level 5 days in a row"},
        {"Yıldız Avcısı", "Star Hunter"}, {"60 yıldız topla", "Collect 60 stars"},
        {"HAKLARIN BİTTİ · YARIN YENİ BÖLÜM", "NO TRIES LEFT · NEW LEVEL TOMORROW"},
        {"SERİ {0} GÜN", "{0}-DAY STREAK"},
        {"{0} HAK · +{1} BONCUK", "{0} TRIES · +{1} BEADS"}, {"TEKRAR DENE · {0} HAK", "TRY AGAIN · {0} LEFT"},
        {"Bugünlük hakkın bitti. Yarın yeni bölüm gelir.", "You're out of tries for today. A new level comes tomorrow."},
        {"Bugünün bölümünü geçtin. Yarın yenisi gelir.", "You already beat today's level. A new one comes tomorrow."}, {"MÜZİK: {0}", "MUSIC: {0}"}, {"ÇOK YAKINDA", "COMING SOON"}, {"Online oyun çok yakında!", "Online play is coming soon!"}, {"İstatistik", "Stats"}, {"Görevler", "Tasks"},
        {"tur", "rounds"}, {"ÇIK", "EXIT"}, {"SIRA ATIŞI", "LAG SHOT"}, {"DÜELLO", "DUEL"},
    };
}
