# MİSKO — HARİTA 2: YAZ TATİLİ / MEMLEKET (60 bölüm)

Bu dosya Claude Code için iş tanımıdır. Önce `CLAUDE.md`'yi baştan sona oku;
oradaki kurallar (ASLA DOKUNMA, BİLİNEN TUZAKLAR, ÖLÇÜLEN DEĞERLER, çalışma
tarzı) bu işte de geçerli. Bu plan onlarla çelişirse CLAUDE.md kazanır.

## Hikâye

Mahalle kampanyası bitti. Okullar kapandı, çocuk yazın memlekete gidiyor ve
misketi orada da oynuyor. Aynı nostalji, yeni zeminler, yeni misketler.

**ÖNEMLİ — AYRI HARİTA:** Oyun artık **haritalardan** oluşuyor.
- **Harita 1 — Mahalle:** mevcut 60 bölüm (Apartman → Meydan), hiç değişmiyor.
- **Harita 2 — Memleket:** bu plandaki yeni 60 bölüm.

Harita 2, Harita 1'in devamına eklenmiş 61-120. duraklar **değil**; kendi
sayfası, kendi haritası ve kendi numaralandırması (1-60) olan ayrı bir dünya.
Oyuncu haritalar arasında geçiş yapar (bkz. "Harita seçimi" aşağıda).
İleride Harita 3, 4 eklenebilecek şekilde **genel** kur (harita sayısı sabit
kodlanmasın). Harita 2'nin 5 bölgesi × 12 bölüm:

| # | Bölge | Yeni zemin kuralı | Özel misket |
|---|---|---|---|
| 5 | **Sahil** | Kum bölgeleri: içinden geçen misket çabuk yavaşlar (yüksek drag) | — |
| 6 | **Köy Meydanı** | Çamur birikintisi: giren misket saplanıp durur | — |
| 7 | **Yayla** | Hafif eğim: sahaya sabit küçük bir yan kuvvet | **BUZLU MİSKET** (serin yayla) |
| 8 | **Kasaba Pazarı** | Tezgâh/kasa engelleriyle dar koridorlar, bant atışları | **BÖLÜNEN MİSKET** (karpuz teması) |
| 9 | **Bayram Yeri** | Final: önceki kuralların karışımı + çukur | Buzlu + Bölünen birlikte |

Her bölgenin ilk 2-3 bölümü yeni kuralı **tek başına** öğretir, sonra öncekilerle karışır.
Her bölgenin son bölümü ustalık sınavı (mevcut `mastery` mantığı).

## İki yeni mekanik (öncelik bunlarda)

### Buzlu misket
- Buz kabuğu içinde başlar, **hiç kıpırdamaz** (kinematic).
- **İlk sert darbe** (eşik ölçülerek belirlenecek) kabuğu **kırar ama misket yerinde kalır**.
- Kırıldıktan sonra normal hedef misket gibi davranır; ikinci vuruşta hareket eder.
- Görsel: misketin çevresinde hafif büyük, yarı saydam açık mavi kabuk; kırılınca
  kısa parçacık/çatlak efekti ve ses (mevcut `MarbleHit` perdesi yükseltilebilir).
- Zayıf darbe (eşiğin altı) hiçbir şey yapmaz — bunu oyuncuya hissettir (tık sesi).

### Bölünen misket
- Sert darbe alınca **ikiye bölünür**: iki küçük misket (ölçek ~0.7, kütle yarı),
  hızı devralır, çarpma yönüne dik iki yana hafifçe açılır. **Tek kademe** bölünme
  (parçalar tekrar bölünmez). Eşik ölçülerek.
- Puanlama: parçalar ayrı ayrı sayılır. `TotalMarbles`, yıldız hedefleri ve arayüzdeki
  sayaçlar bununla tutarlı olmalı — kararını ilerleme dosyasına yaz.
- Görsel: karpuz çizgili/yeşil desenli misket, bölünürken kısa "çıt" efekti.

Her iki mekanik için `Assets/Editor/` altında doğrulama aracı yaz ve
`MahalleVerify.Run`'a bağla (ör. buz: eşik altı darbe kıpırdatmaz, eşik üstü kırar,
kırıktan sonra hareket eder; bölünen: tek kademe, parça sayısı, skor tutarlılığı).

## Zorluk (ölç, tahmin etme) — Harita 2, Harita 1'den ZOR

Harita 1 merdiveni (ölçülmüş, 896.778 simülasyon): Apartman %97 → Okul %85 →
Park %66 → Toprak %62 → Meydan %57. **Harita 2 bunun devamıdır ve genel olarak
daha zordur.** Başlangıcı Meydan'ın sonuna yakın, bitişi en zor noktası.

Hedef geçme oranları (güçsüz, normal misket):

| Bölge | Başı | Sonu |
|---|---|---|
| Sahil | ~%62 | ~%55 |
| Köy Meydanı | ~%58 | ~%51 |
| Yayla | ~%55 | ~%48 |
| Kasaba Pazarı | ~%52 | ~%45 |
| Bayram Yeri | ~%49 | ~%40 |

Kurallar:
- Her bölge, bir önceki bölgenin sonundan en fazla ~7 puan kolay başlayabilir
  (yeni kural öğretiliyor); **bölge içinde her bölüm bir öncekinden zor ya da eşit**.
  Bölge başları da bölgeden bölgeye düşmeli. Genel eğilim sürekli aşağı.
- **Taban %40**, geçilemeyen bölüm **yok**. Ustalık sınavları bölgesinin en zoru.
- Harita 2'nin ortalaması Harita 1'in ortalamasından belirgin düşük olmalı.

Ölçüm protokolü (Harita 1'deki titizlikte):
- Mevcut `ParkPhysicsVerify` / `DifficultyOrderVerify` / `MarbleYieldVerify`
  araçlarını Harita 2 için genişlet; Harita 1 sonuçları **değişmemeli**
  (önce ve sonra ölçüp karşılaştır, ilerleme dosyasına yaz).
- Bölüm başına **en az 10.000 simülasyon** (Harita 1 ~15.000/bölüm kullandı);
  sabit tohum (seed), tekrarlanabilir sonuç.
- Her bölüm için ölç ve tabloya yaz: geçme oranı, 1/2/3 yıldız oranları,
  ortalama çıkan misket/atış, buz kırma ve bölünme sayıları.
- Üç ayrı koşuda ölç: (1) güçsüz normal misket, (2) güçlerle, (3) özel
  kaplamalarla. "Güç almadan geçilmez" bölüm olmasın (para tuzağı yok),
  güçler de zorluğu anlamsız kılmasın.
- Yeni mekanikler (kum, çamur, eğim, çukur, buz eşiği, bölünme eşiği) için
  ayrı ayar ölçümü yap; eşikleri ölçerek seç, sayıları gerekçesiyle yaz.
- `DifficultyOrderVerify`'ı Harita 2'ye bağla: sıra kuralları bozulursa
  **CHECK FAILED**. Hedef aralığın ±3 puan dışındaki bölümleri raporla.
- Zorluk kolları: açı/dizilim, engeller, çizgi genişliği, atış sayısı, yıldız
  eşiği, yeni zeminler. **Kütle ile zorluk YOK** (CLAUDE.md: denendi, elendi).
- Çeşitlilik: `LevelVarietyVerify` Harita 2'yi de ölçsün (harita içi ve iki
  harita arası); benzerlik sınırı 0.30.
- Bitince ilerleme dosyasına 60 satırlık zorluk tablosu + Harita 1 ile
  karşılaştırma grafiği yerine özet satırı koy.

## Ekonomi
Mevcut kurallar aynen: ilk geçiş 6 boncuk, yıldız başına 3, tekrar 0. Ödülleri
şişirme. Her bölgeye 1 özel kaplama fikri ekleyebilirsin (fiyatlar mevcut
aralıkta), ama mağazaya eklemeyi ayrı bir fazda yap.

## Teknik çerçeve
- Veri: harita başına ayrı bölüm veritabanı. Harita 1 = mevcut `Campaign` +
  `LevelBook` (dokunma, sonuçları değişmemeli). Harita 2 için aynı yapıyı izleyen
  ayrı bir kaynak (ör. `MemleketCampaign` + `MemleketBook`). Oynanan bölüm
  `(harita, bölüm)` ikilisiyle belirlenir (`GameSession`'a harita alanı).
- Kayıt: Harita 2'nin yıldız/ilerleme verisi **ayrı** tutulsun; mevcut
  `MahalleSave` alanları aynen kalsın, eski kayıt sorunsuz açılsın. Kayıt
  geçişi için doğrulama ekle. İstatistik/görev/rozet sayımları iki haritayı
  doğru toplasın (kararını ilerleme dosyasına yaz).
- Kilit: Harita 2, Harita 1'in son bölümü (Meydan ustalık sınavı) geçilince açılır.
  Test bayrağı `TestUnlockAllLevels` açıkken iki harita da açık.
- **Harita seçimi:** Harita ekranında (`MahalleMapView` / `MapScreen`) haritalar
  arası geçiş. Önerilen: harita ekranının üstünde harita adı kapsülünün iki
  yanında ok ya da alt sekmede "Haritalar" sayfası (iki büyük kart: Mahalle,
  Memleket; kilitliyse kilit ve "Mahalle'yi bitir" yazısı). Oyuncu son oynadığı
  haritada açılsın. TASARIM DİLİ bölümündeki 4 renge uy. Tasarım kararını ilerleme
  dosyasına yaz, "telefonda kontrol edilecek" listesine ekle.
- Harita 2 görselleri sonra gelecek, şimdilik CLAUDE.md'deki üç kademeli düşüş
  (fotoğraf yoksa prosedürel zemin) çalışsın. Beklenen dosyalar:
  `Resources/Mahalle/Map/Harita{Sahil,Koy,Yayla,Pazar,Bayram}.png` (1080×4476),
  `Resources/Mahalle/Ground/{Sahil,Koy,Yayla,Pazar,Bayram}.png`.
  Bunları bekleyen işler listesine yaz, kendin üretme.
- Dil: her yeni metin `L.cs` tablosuna İngilizcesiyle girsin.
- Kamera: `CameraFitVerify` ve `MapLayoutVerify` yeni bölümleri de kapsasın.
- Tuzaklar: `PrimitiveType.Cylinder` yok, primitive'e mutlaka materyal, shader'ı
  `Resources.Load` ile al, runtime materyallere `HideAndDontSave`, görünüm işleri
  collider'dan önce.

## Çalışma şekli (gözetimsiz)
- Yeni dal aç: `codex/yaz-tatili` (şu anki daldan).
- **Fazlar halinde ilerle**, her faz sonunda `MahalleVerify.Run` yeşilse commit et:
  1. Harita altyapısı (harita başına veritabanı, `GameSession` harita alanı,
     ayrı kayıt, kilit) — Harita 1 davranışı ve bütün mevcut testler aynen geçmeli
  2. Buzlu misket + doğrulama
  3. Bölünen misket + doğrulama
  4. Zemin kuralları (kum, çamur, eğim, çukur) + doğrulama
  5. 60 bölümün dizilimi (bölge bölge)
  6. Zorluk ölçümü ve ayar, çeşitlilik
  7. Harita seçimi ekranı, Harita 2 harita görünümü, dil
- Test komutu (Unity Editor KAPALI olmalı):
  `/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath "$PWD" -executeMethod MahalleVerify.Run -logFile /tmp/misko_verify.log`
  sonra `grep -n "error CS\|CHECK FAILED\|MAHALLE_VERIFY" /tmp/misko_verify.log`.
  Batch çalışmıyorsa (çıkış 1, "Successfully changed project path"ta duruyor)
  Unity açıktır: bekle, zorla kapatma.
- **Soru sorma, kimse cevap veremeyecek.** Makul kararı ver ve
  `YAZ_TATILI_ILERLEME.md` dosyasına yaz: her faz, verdiğin kararlar ve nedenleri,
  ölçüm sonuçları (sayılarla), açık kalanlar. Takılırsan notunu bırak, bir
  sonraki bağımsız faza geç.
- **Push etme**, `ProjectSettings/ProjectSettings.asset` ve
  `Assets/Settings/Mobile_RPAsset.asset`'e dokunma, commit'e ekleme.
  Dokunulmamış LFS dosyalarını (.png/.wav/.ttf/.ogg) commit etme.
- Görsel sonuçları "tamam" sayma: test renk/yerleşim hatasını yakalamaz. Bunları
  ilerleme dosyasında "telefonda kontrol edilecek" listesine yaz.
- Bittiğinde: ilerleme dosyasının başına 10 satırlık özet, sonra CLAUDE.md'ye
  yeni kampanyanın kalıcı bilgilerini (mimari, ölçülen değerler, tuzaklar) ekle.
