# YAZ TATİLİ — İLERLEME

Dal: `codex/yaz-tatili` (codex/mahalle-konsepti'den). Push edilmedi.
Plan: `YAZ_TATILI_PLANI.md`. Test: `MahalleVerify.Run` (batch, Unity kapalı).

## Özet
_(iş bitince buraya 10 satırlık özet gelecek)_

| Faz | Durum | Commit | MahalleVerify |
|---|---|---|---|
| 1 Harita altyapısı | ✅ bitti | 47c584e | 5461 kontrol yeşil (Harita 1'in 4291'i + 1170 yeni) |
| 2 Buzlu misket | ✅ bitti | 7d2b35c | 5481 yeşil (+20 buz) |
| 3 Bölünen misket | ✅ bitti | c8ee5ed | 5498 yeşil (+17 bölünme) |
| 4 Zemin kuralları | ✅ bitti | f4657df | 5574 yeşil (+76 zemin) |
| 5 60 bölüm dizilimi | ✅ bitti | 65b19a1 | 15847 yeşil (+10273 yerleşim) |
| 6 Zorluk ölçümü | ✅ bitti | (aşağıda) | 16674 yeşil (sıra kuralları dahil) |
| 7 Harita seçimi, görünüm, dil | ✅ kod bitti | (aşağıda) | aynı koşu; görsel telefonda |

---

## Faz 1 — Harita altyapısı (2026-09-24)

**Kararlar**

- **Global bölüm numarası.** Bölüm `harita × 60 + sıra` ile tek sayıda tutulur
  (0-59 Mahalle, 60-119 Memleket). `GameSession.SelectedLevelIndex` globaldir,
  `GameSession.SelectedMap` ondan türetilir (ikisi ayrışamaz). Neden: kilit
  zinciri, `Finish`, harita ekranı ve oyun akışı zaten tek sayıyla çalışıyordu;
  ikinci bir alan eklemek her çağrıyı değiştirmek demekti. Harita 1'in
  numaraları değişmedi, eski kod yolu aynen çalışıyor.
- **Global bölge numarası.** Memleket bölgeleri 5-9 (`LevelData.district`).
  Tema, zemin rengi, zemin/harita görsel dosyaları bu numarayla bulunur; yeni
  harita = tablolara 5 satır.
- **`Maps` (yeni).** Harita sayısı yalnızca `Maps.Names`'te; `Database(map)`,
  `Get(global)`, `DistrictName(district)`. Harita 3 için: bir `case`, bir ad,
  5 tema, iki dosya listesine 5 ad.
- **`MemleketCampaign` + `MemleketBook` (yeni).** Harita 1'deki
  `Campaign` + `LevelBook` düzeninin aynısı. Şimdilik formülle üretilen taban
  (yer tutucu); gerçek dizilim Faz 5'te `MemleketBook`'a yazılacak.
- **Kayıt.** `MahalleSave`'in eski alanları aynen duruyor. Yeni:
  `MapProgress[] maps` (Harita 2 ve sonrası: `stars[60]`, `districtRewards[5]`,
  `districtPlays[5]`) ve `lastMap`. Eski kayıt (bu alanlar yokken) açılınca
  `Normalize` boş slot ekler; Mahalle ilerlemesi olduğu gibi kalır (testli).
- **Kilit.** Memleket'in 1. bölümü, Mahalle'nin son bölümü (Meydan ustalık
  sınavı) gereken yıldızla geçilince açılır — global zincir bunu kendiliğinden
  yapıyor. `TestUnlockAllLevels` açıkken iki harita da açık.
- **Sayımlar iki haritayı toplar:** toplam yıldız, usta seviyesi, "60 yıldız"
  ve "bölüm kazan" rozetleri, "mahalle tamamla" rozeti, 3-yıldız görevi, en çok
  oynanan bölge. Harita başına: `MapStars(map)`.
- **Memleket bölge ödülü:** 35 boncuk + rozet, **kaplama hediyesi yok**
  (Harita 1'de bölge sonu 1-5. kaplamayı veriyordu; aynı formül Memleket'te özel
  misketleri bedava verirdi). Memleket kaplamaları plan gereği ayrı fazda.
- **Ekonomi aynen:** ilk geçiş 6, yıldız başına 3, tekrar 0 (testli).
- **"Sonraki bölüm"** aynı haritada kalır; Meydan ustalık sınavından sonra
  otomatik Memleket'e geçilmez (harita seçimi Faz 7'de).
- **Harita ekranı okları** haritanın kendi 5 bölgesinde döner.
- **Bölüm adları:** 5 bölge × 12 ad + İngilizceleri `L.cs`'te. Pazar'daki "Dar
  Koridor" Okul'da vardı, "Tezgâh Arası" yapıldı. Memleket adları hem kendi
  içinde hem Harita 1 ile çakışmıyor (test).

**Test (`MapsVerify`, 1170 kontrol):** iki harita da 60 bölüm, global/yerel
numara gidiş-dönüş, bölge sınırları, Harita 1 = `Campaign` nesneleri, tema ve
İngilizce eksiksiz, yeni oyuncuda Memleket kilitli, Meydan ustalık sınavı
geçilince açılıyor, Memleket yıldızı ayrı slota yazılıyor ve Mahalle'yi
etkilemiyor, ekonomi aynı, bölge rozeti tek sefer ve kaplama vermiyor, eski
kayıt (maps alanı olmadan) sorunsuz açılıyor, kayıt yaz-oku yeni alanları
koruyor.

**Harita 1 değişmedi mi?** `Campaign.cs` ve `LevelBook.cs`'e dokunulmadı.
MahalleVerify'ın önceki 4291 kontrolü (zorluk, kamera, harita yerleşimi,
dokunuş testi dahil) aynen geçiyor.

---

## Ölçü notu: "geçme oranı" = temizlenebilirlik

Plandaki Harita 1 merdiveni (Apartman %97 → Meydan %57) bir geçme oranı değil;
`ParkPhysicsVerify.RunAll`'ın **temizlenebilirlik** ölçüsü: açgözlü arama ile
normal misketin çıkarabildiği en çok misket / toplam misket (bkz.
`Logs/AllLevelsPhysicsVerify.txt`, "ZORLUK OZETI"). Harita 2 hedefleri
(Sahil ~%62 → Bayram ~%40) bu yüzden aynı ölçüyle ayarlanacak (Faz 6).
Aynı logda Harita 1 için bir uyarı var: "KAPALI BOLUMLER: PARK 10 (gecis 7,
tavan 6)" — açgözlü arama PARK 10'da 7'yi bulamamış. Eski ölçüm, Harita 1'e
dokunulmadı; ayrıca bakılmalı.

---

## Faz 2 — Buzlu misket (2026-09-24)

**Nasıl çalışıyor** (`Gameplay/IceShell.cs`)
- Bölüm verisinde `MarbleSpot.kind = MarbleKind.Ice` (yeni alan, varsayılan
  Normal → Harita 1'in bütün bölümleri aynen).
- Buzlu misket **kinematic** başlar: kıpırdamaz, çarpan misket duvara çarpmış
  gibi seker. Temas doğrultusundaki bağıl hız ≥ `BreakSpeed` ise kabuk kırılır;
  misket kinematic iken vurulduğu için **hız almaz, yerinde kalır**, sonra normal
  hedef olur. Eşik altı darbe: ince tık sesi + kısa çatlak parıltısı, başka bir
  şey yok.
- Görsel: `Resources/Mahalle/Ice.shader` (yarı saydam açık mavi, fresnel kenar,
  çatlak çizgileri). Kabuk = Sphere primitive, materyal önce atanır, collider
  hemen silinir (bileşik collider olmasın). Kırılınca 7 küçük Cube parçası.
  Ses: mevcut `MarbleHit` klibinin perdesi yükseltilerek (kırılma 1.75+2.3,
  tık 2.6) — yeni ses dosyası yok.
- `[ExecuteAlways]`: ölçüldü, preview sahnede `physicsScene.Simulate` ile
  normal MonoBehaviour'a `OnCollisionEnter` **gelmiyor**, `[ExecuteAlways]`
  olana geliyor. Böylece fizik ölçüm araçları oyunun bileşeninin aynısını
  kullanabiliyor (ayrı bir taklit yok).

**Eşik ölçümü** (`IceMarbleVerify.Measure`, `Logs/IceThreshold.txt`),
atıcı çizgisi → buz mesafesine göre temas hızı (m/s):

| güç | 1.5 | 3.0 | 4.5 | 6.0 | 7.5 |
|---|---|---|---|---|---|
| %30 | 2.28 | 1.41 | 0.55 | 0 | 0 |
| %45 | 4.45 | 2.88 | 2.01 | 1.15 | 0.29 |
| %60 | 6.64 | 4.61 | 3.49 | 2.65 | 1.77 |
| %80 | 9.41 | 7.78 | 6.01 | 4.64 | 3.78 |
| %100 | 12.08 | 10.58 | 9.01 | 7.42 | 5.86 |

Zincir (önce normal misket, o buza): %60 → 1.55, %80 → 4.28, %100 → 6.54.
Yavaş yuvarlanan misket 3 m/s ile gelse bile buza 2.55.

**Seçim: `BreakSpeed = 3.0 m/s`.** Saha ortasındaki buz (~4.5) %55 civarı
güçle kırılır, uzak kenardaki (~7.5) %70+ ister; yakın buz %40 civarı.
Dolaylı (zincir) kırma sadece sert atışta (%80+) olur, atıştan sonra
yuvarlanan misketler asla kırmaz. Yani "kırmak" bilinçli, sert bir atış.

**Test (`IceMarbleVerify.RunChecks`, 20 kontrol):** donuk başlar ve kaymaz;
zayıf darbe kırmaz, kıpırdatmaz, atıcı seker; tam güç kırar, tek sefer olay,
misket yerinde (<0.05 kayma), gövde dinamik olur ve çarpışma modu geri gelir;
ikinci vuruş misketi taşır; eşiğin %85'i kırmaz, %125'i kırar; ölçüm aracı için
Freeze/Thaw geri sarılabilir; shader Resources'ta.

---

## Faz 3 — Bölünen misket (karpuz) (2026-09-24)

**Nasıl çalışıyor** (`Gameplay/SplitMarble.cs`)
- `MarbleSpot.kind = MarbleKind.Split`. Görünüm: yeşil kabuk + koyu damar
  (Marble shader'ı, `MarbleVisual.SetOverride`); parçalar kırmızı iç + koyu
  çekirdek rengi.
- Başka bir **gövdeyle** (misket) çarpışmada temas hızı ≥ `SplitSpeed` →
  bütün misket gizlenir, yerine iki parça: ölçek ×0.7, kütle yarı, hızı devralır,
  çarpma yönünden ±16° iki yana açılır. Zemin darbesi bölmez.
- **Tek kademe:** parçalar `IsPiece` işaretli, bir daha bölünmez. (Ölçüldü:
  fizik geri çağrısı içinde `DestroyImmediate` yasak — ilk denemede parçalar
  bölünmeye devam edip Unity'yi kilitledi. Oyunda bileşen `Destroy` ile
  ertelenerek silinir.)
- **Puanlama kararı:** karpuz **2 misket değerindedir**. Bütün çıkarsa 2,
  bölünürse her parça 1. `LevelData.TotalMarbles()` karpuzu 2 sayar,
  `MarbleArena` toplamı ve skoru `TargetMarble.Worth` ile tutar. Böylece yıldız
  hedefleri, "x / toplam" sayacı ve "hepsi çıktı" kontrolü baştan tutarlı;
  oyuncu bölse de bölmese de aynı değeri alır, bölmek sadece taktik fark yaratır
  (parçalar daha hafif ve iki yöne dağılır).
- Ses: `MarbleHit` perdesi 0.72 (kuru "çıt"). Karpuz gizlenince gölgesi de
  gizlenir (`MarbleVisual.OnDisable`).

**Eşik ölçümü** (`SplitMarbleVerify.Measure`, `Logs/SplitThreshold.txt`),
karpuz saha ortasında (atıcıdan 4.5 önde):

| güç | temas hızı | bölünürse parça yolu | bütün kalırsa yol |
|---|---|---|---|
| %30 | 0.55 | — | 0.30 |
| %45 | 2.01 | — | 1.20 |
| %60 | 3.49 | 2.58 / 2.58 | 2.39 |
| %80 | 6.01 | 4.75 / 4.75 | 4.53 |
| %100 | 9.01 | 7.68 / 7.68 | 7.54 |

**Seçim: `SplitSpeed = 3.0 m/s`** — buzla aynı. İki mekanik de "sert darbe"
diye öğretiliyor, oyuncu tek bir his öğrensin. Ortadaki karpuz %55-60 güçte
bölünür, parçalar sahada kalır (2.6 < 3.5); %80'de parçalar sahadan çıkar.

**Test (`SplitMarbleVerify.RunChecks`, 17 kontrol):** TotalMarbles karpuzu 2
sayar; zayıf darbe bölmez ama iter; sert darbe tek sefer böler, bütün
gizlenir, iki canlı parça, yarım kütle, ×0.7 ölçek, aynı fizik sahnesinde, hız
devralınmış, parçalar arası açı ~32°, ikisi iki yanda, ileri gidiyor; parça
sert vurulsa da yeni parça çıkmaz; zemine sert düşmek bölmez.
**Test edilemeyen:** `MarbleArena.OnSplit` (listede bütünün yerine parçaları
koyma) Play modu istiyor; kod yolu basit ama telefonda bir karpuz bölüp
sayacın 2 arttığına bakılmalı (aşağıdaki liste).

---

## Faz 4 — Zemin kuralları (2026-09-24)

**Nasıl çalışıyor**
- Veri: `LevelData.zones` (`ZoneSpot`: Sand/Mud/Pit, çember merkezine göre
  x, z, yarıçap) ve `LevelData.slope` (x, z ivme, m/s²). Harita 1'de ikisi de
  boş → hiçbir şey kurulmaz (test: 60 bölümün hiçbirinde kural yok).
- Kurallar tek yerde: `GroundRules.Step` — oyunda `GroundZones.FixedUpdate`,
  ölçüm araçlarında her `Simulate` adımından önce **aynı fonksiyon**.
  - **Kum:** bölge içinde ek sürtünme (PhysX damping formülüyle, kararlı).
  - **Çamur:** çok yüksek ek sürtünme → misket birkaç santimde saplanır.
  - **Eğim:** yalnız **hareket eden** (>0.3 m/s) misketlere sabit yan ivme.
    Karar: duran misketi de itseydik PhysX'te yuvarlanan küre hiç durmaz,
    bütün misketler kendiliğinden sahadan akardı; "çimenin tuttuğu hafif
    yamaç" gibi davranıyor. (Test: duran misket 200 adımda <1 cm.)
  - **Çukur (Bayram Yeri):** 2.2 m/s'den yavaş geçen **hedef** misket düşer:
    kinematic olur, collider kapanır, çukur merkezinde çöker, soluklaşır;
    **ne sayılır ne kalır** (`MarbleArena.Capture`). Hızlı misket üstünden
    geçer. Atıcı düşmez, sadece durur (sonraki atışta çizgiye döner).
    Karar gerekçesi: final bölgesi en zor olmalı; çukur "yavaş misket =
    kayıp" diye sert ve kontrollü atışı ödüllendiriyor. Yıldız hedefleri Faz
    6'da çukur hesaba katılarak ölçülecek.
- Görünüm: `Resources/Mahalle/Zone.shader` (yere yatırılmış Quad üstünde
  yumuşak, hafif dalgalı kenarlı daire; kum tanecikli, çamur ıslak parlamalı,
  çukur koyu delik + açık kenar). Cylinder yok, materyal önce, collider
  hemen siliniyor. Eğim: çemberin dışında eğim yönünde üç soluk tebeşir oku.
  Çukur sesi: `MarbleHit` perdesi 0.55.

**Ölçüm** (`GroundRulesVerify.Measure`, `Logs/GroundRules.txt`):

Kum (r=1.0, atıcının yolunda): ek drag 1.5 → %60 atış kumun içinde durur,
%100 atış kumdan hızının %44'üyle çıkar (kumsuz 12.6 → kumlu 8.1 birim).
Ek drag 3 → %100 bile %14'le çıkıyor (neredeyse çamur); 5 → duvar.
**Seçim: `SandDrag = 1.5`** — "çabuk yavaşlar" ama geçilebilir; çamurdan ayrı.

Çamur (r=0.6): çamura girdikten sonra alınan yol, ek drag 14 → %45: 0.15,
%60: 0.29, %80: 0.48, %100: 0.61 (çap 1.2, her güçte içinde saplanıyor).
Drag 8'de tam güç 0.99'a kadar gidiyor (kenara çok yakın).
**Seçim: `MudDrag = 14`.**

Eğim (4.5 ilerideki hedef hizasında yana kayma, birim):

| ivme | %45 | %60 | %80 | %100 |
|---|---|---|---|---|
| 0.4 | 0.26 | 0.11 | 0.05 | 0.03 |
| 0.8 | 0.51 | 0.22 | 0.10 | 0.06 |
| 1.2 | 0.77 | 0.33 | 0.15 | 0.09 |

Sert atış neredeyse etkilenmiyor, yavaş atış ve **çarpışma sonrası
yuvarlanan misketler** belirgin sapıyor. Eğim bölüm verisi (Yayla'da 0.6-1.2
arası kullanılacak), kuralın kendisinde sabit yok.

Çukur (r=0.35): 1.2 geriden fırlatılan misket 4.0 m/s'de çukura 2.21 ile
varıyor → geçer; 3.0 m/s'de 1.48 → düşer. Atıştan sonra saha içinde
yuvarlanan misketler (0.5-2.5 m/s) düşer, doğrudan atışlar (5-9 m/s) geçer.
**Seçim: `PitCaptureSpeed = 2.2`.**

**Test (`GroundRulesVerify.RunChecks`, 76 kontrol):** Harita 1'in 60
bölümünde kural yok; kum tam atışı kısaltır ama geçilir; çamur %60 ve %100'de
misketi içinde durdurur; eğim hareket edeni saptırır, duranı hiç kıpırdatmaz;
çukur yavaşı yutar (kinematic, collider kapalı, merkezde), hızlıyı geçirir;
ölçüm için geri alınabilir; atıcı yutulmaz, durur; bölgeler çember merkezini
izler; shader Resources'ta.

---

## Faz 5 — 60 bölümün dizilimi (2026-09-24)

`Mahalle/MemleketBook.cs`, Harita 1'deki `LevelBook` biçiminde, her bölüm
kendi yorumuyla. Hepsi çember (3.2-4.0), elle yerleştirilmiş misket.

| Bölge | Kural | Öğretme | Sonra |
|---|---|---|---|
| Sahil 1-12 | kum | 01-02 sadece kum | engel, kaya, kumla çevrili ıstaka (sınav) |
| Köy 1-12 | çamur (+kum) | 01 sadece çamur | masalar, kümes, sokak, artı + 4 çamur (sınav) |
| Yayla 1-12 | eğim + **buz** | 01-02 sadece eğim, 03 sadece buz | ikisi birlikte, elmas kafes + 5 buz (sınav) |
| Pazar 1-12 | tezgâh koridorları + **karpuz** | 01 koridor, 02 sadece karpuz | kasalar, terazi, zikzak, labirent + 3 karpuz (sınav) |
| Bayram 1-12 | **çukur** + hepsi | 01 sadece çukur | buz, karpuz, çamur, kum, eğim karışık; final sarmal |

Misket sayısı bölgeler boyunca artıyor (Sahil 7-12 → Bayram 9-15, karpuz 2
sayılır). Ustalık sınavları geçiş için 2 yıldız ister. Atış hakları taban
değer; yıldız hedefleri **şimdilik formül** (1y %40, 2y %60, 3y hepsi) —
Faz 6'da ölçümle `MemleketBook.Tuning` tablosuna yazılacak (Harita 1'de de
hedefler ölçümden sonra konmuştu).

**Çeşitlilik:** ilk dizilimde 21 çift 0.30 sınırının altındaydı (en kötü
0.15: Dalga Çizgisi ↔ Bakır Tezgâhı). `MemleketVarietyTuner` her bölümü
en küçük dokunuşla (±8-30° döndürme, 0.25-0.45 kaydırma; misket, engel, bölge
ve eğim birlikte) ayarladı: 18 bölüm dokunuldu, tablo `MemleketBook.Variation`.
Sonuç: Memleket'in 6+ misketli her bölümü iki haritadaki bütün bölümlerden en
az **0.32** farklı (en yakın çift Bakır Tezgâhı ↔ Pamuk Şeker).

**Test (`MemleketLayoutVerify`, 10273 kontrol):** misketler sahada (kenardan
0.35 içeride), aralar ≥0.52, engel ve çukur içinde misket yok, bölgeler sahada
ve atıcı çizgisinin altında değil; her bölgede kendi kuralı var, özel
misket/kural kendi bölgesinden önce yok; öğretme bölümlerinde yeni kural tek
başına; ustalık sınavı 2 yıldız ve kilit < ustalık; çeşitlilik ≥0.30.
`CameraFitVerify` artık iki haritanın 120 bölümünü ölçüyor (yeşil).
Yerleşim düzeltmeleri: Şemsiye Altı yayı 1.75'e açıldı (misketler 0.49
yakındı), Tavuk Kümesi ızgarası duvardan uzaklaştı, Kaya Yanı kayaları ve
Pazar sınavının labirenti (ince duvar, şeritte misket) yeniden yerleşti.

---

## Faz 6 — Zorluk ölçümü ve ayar (2026-09-24)

**Araç: `MemleketPhysicsVerify`** (Harita 1'in `ParkPhysicsVerify`'ına
dokunulmadı). Aynı yöntem — görünmez preview sahne, gerçek misket fiziği,
açgözlü arama — ama Memleket'in kurallarıyla: oyundaki `IceShell`,
`SplitMarble` bileşenleri ve `GroundRules.Step` birebir kullanılıyor
(bölünen karpuzun parçaları ayrı kayıt olarak izleniyor, çukura düşen misket
kayıp sayılıyor). Aday atışlar: çizgide 7 nokta × her misket × düz/±kesme ×
4 güç (%100/80/60/45) + engelden bant. Sabit, tekrarlanabilir.

İki ölçü:
1. **Tavan / temizlenebilirlik** (açgözlü): Harita 1 merdiveninin ölçüsü.
2. **Sıradan oyuncu** (geçme oranı): her atışta rastgele 10 aday, ±2° nişan
   hatası, ±%15 güç hatası, en iyisini seçer; 150 oyun, sabit tohum
   (`1000 + bölüm·7919`). 1/2/3 yıldız oranı, atış başına misket, buz
   kırma / bölünme / çukur sayısı.

**Aracın doğrulaması (Harita 1'den 6 bölüm, eski araçla karşılaştırma):**
APT01 3=3, APT06 7=7, OKUL07 9↔8, PARK07 10↔9, TOPRAK09 8=8, MEYDAN12 7↔8.
Fark en çok 1 misket (arama noktaları biraz farklı). Harita 1'e dokunulmadığı
için `ParkPhysicsVerify` sonuçları değişmedi (`Campaign`/`LevelBook`
değişmedi, o araç kendi fiziğini kuruyor).

**İlk tam ölçüm** (`Logs/MemleketPhysicsQuick.txt`, 593.373 simülasyon, 183
dk, bölüm başına ~10.000): temizlenebilirlik çok dağınıktı — Yayla'nın
öğretme bölümleri 2 atışta %100, Pazar sınavı %20.

**Atış eğrisi:** açgözlü arama her atışta bir öncekinden bağımsız en iyiyi
seçtiği için 7 atışlık tek koşu 1..7 atışın hepsinin tavanını veriyor
(`BatchCurve`, `Logs/MemleketCurve.tsv`, 200.357 simülasyon). Atış hakkı
buradan seçilir.

**Düzeltmeler (ölçüme göre):**
- Taban %40'ı 7 atışla bile tutmayan 3 bölüm kolaylaştırıldı: Sıcak Kum
  (kum 1.8 → 1.15; %36 → %82), Tavuk Kümesi (yan duvarlar arkaya kısaldı;
  %33 → %56), Pazar sınavı (4 uzun duvar → 3 kısa; %27 → %60).
- 3 atışta bile hedefin 10+ puan üstünde temizlenen 23 bölüm sertleştirildi:
  saha 4.0'a büyüdü, bölümün bütünü merkeze ×0.85-0.95 sıkıştı
  (`MemleketBook.Hardening`). Misketler değebilir ama iç içe geçmez
  (yerleşim testi ≥0.50). Çeşitlilik ayarı yeniden koşuldu (23 bölüm; en
  yakın çift yine 0.32).
- **Eğim hatası bulundu ve düzeltildi:** sabit eğim ivmesi yavaş misketi
  sürtünmeden hızlı itiyordu; hafifçe dürtülen misket hiç durmadan sahadan
  akıyordu (Yayla öğretme bölümleri 2 atışta %100). Artık eğim hızla orantılı
  (0.3 m/s'de 0, 2.3 m/s'de tam): hiçbir hızda sürtünmeyi yenmez, sert atışın
  sapması aynı. Yeni test: "dürtülen misket eğimde durur".
- 3 öğretme bölümüne misket eklendi (Kumsal Girişi +3, Çınar Altı +2,
  Yayla Yolu +3), Davul Zurna'nın iç yayı 5→4, Bayram Harçlığı'nın çukurları
  büyüdü ve çamura/kuma 3 misket kondu, Bayram'ın tamamı ikinci tur
  sertleştirildi (ilk ayarda Pazar'dan kolaydı).

**Atış hakkı seçimi** (bir arama, `Logs/MemleketTuning.txt`): her bölüm için
eğriden 3-7 atış (bölgenin ilk üç öğretme bölümünde 2 de serbest), amaç
hedefe en yakın temizlenebilirlik, kısıtlar: taban %40; bölge içinde bir
öncekinden en fazla **bir misket** kolay; bölge başı önceki bölge sonundan en
fazla ~7 puan (+bir misket) kolay ve bölge başları düşüyor (bir misket payı);
ustalık sınavı bölgesinin en zoru (bir misket payı).
**Neden bir misket payı:** ölçü kaba — 9 misketli bölümde bir misket 11 puan;
daha ince sıra ölçülemez. Bu yüzden 34 bölüm plan merdiveninin ±3 puanı
dışında (tabloda ⚠) ama sıra kurallarının hepsi tutuyor ve
`DifficultyOrderVerify.MemleketChecks` bozulursa **CHECK FAILED** veriyor
(MahalleVerify de çağırıyor).

**Yıldız hedefleri** tavandan türetiliyor (`MemleketBook.Tune`): 1y = tavanın
%60'ı, 2y = ustalık sınavında tavan / diğerlerinde %85'i, 3y = hepsi. Yani
"geçilemeyen bölüm yok" ölçümle garanti; 3 yıldız çoğu bölümde güç veya özel
misket ister (Harita 1'in tasarım kuralı).

**Son tablo** (atış hakkı ve tavan `MemleketBook.Tuning`; temizlenebilirlik = tavan / toplam; hedef = plan merdiveni):

| # | Bölge | Toplam | Atış | Tavan | Temizlenebilirlik | Hedef | 1y/2y/3y |
|---|---|---|---|---|---|---|---|
| 60 | Sahil 01 | 10 | 2 | 5 | %50 ⚠ | %62 | 3/4/10 |
| 61 | Sahil 02 | 7 | 3 | 4 | %57 ⚠ | %61 | 3/3/7 |
| 62 | Sahil 03 | 9 | 5 | 6 | %67 ⚠ | %61 | 4/5/9 |
| 63 | Sahil 04 | 9 | 3 | 6 | %67 ⚠ | %60 | 4/5/9 |
| 64 | Sahil 05 | 9 | 3 | 5 | %56 ⚠ | %59 | 3/4/9 |
| 65 | Sahil 06 | 9 | 3 | 6 | %67 ⚠ | %59 | 4/5/9 |
| 66 | Sahil 07 | 9 | 5 | 7 | %78 ⚠ | %58 | 5/6/9 |
| 67 | Sahil 08 | 9 | 5 | 5 | %56 | %58 | 3/4/9 |
| 68 | Sahil 09 | 11 | 4 | 6 | %55 | %57 | 4/5/11 |
| 69 | Sahil 10 | 10 | 3 | 6 | %60 ⚠ | %56 | 4/5/10 |
| 70 | Sahil 11 | 11 | 3 | 6 | %55 | %56 | 4/5/11 |
| 71 | Sahil 12 | 12 | 5 | 7 | %58 ⚠ | %55 | 5/7/12 |
| 72 | Köy Meydanı 01 | 10 | 3 | 6 | %60 | %58 | 4/5/10 |
| 73 | Köy Meydanı 02 | 8 | 3 | 5 | %62 ⚠ | %57 | 3/4/8 |
| 74 | Köy Meydanı 03 | 9 | 4 | 6 | %67 ⚠ | %57 | 4/5/9 |
| 75 | Köy Meydanı 04 | 7 | 5 | 4 | %57 | %56 | 3/3/7 |
| 76 | Köy Meydanı 05 | 8 | 3 | 4 | %50 ⚠ | %55 | 3/3/8 |
| 77 | Köy Meydanı 06 | 9 | 6 | 5 | %56 | %55 | 3/4/9 |
| 78 | Köy Meydanı 07 | 12 | 4 | 7 | %58 ⚠ | %54 | 5/6/12 |
| 79 | Köy Meydanı 08 | 9 | 4 | 6 | %67 ⚠ | %54 | 4/5/9 |
| 80 | Köy Meydanı 09 | 10 | 4 | 5 | %50 | %53 | 3/4/10 |
| 81 | Köy Meydanı 10 | 10 | 3 | 5 | %50 | %52 | 3/4/10 |
| 82 | Köy Meydanı 11 | 12 | 3 | 7 | %58 ⚠ | %52 | 5/6/12 |
| 83 | Köy Meydanı 12 | 13 | 6 | 7 | %54 | %51 | 5/7/13 |
| 84 | Yayla 01 | 10 | 2 | 5 | %50 ⚠ | %55 | 3/4/10 |
| 85 | Yayla 02 | 8 | 3 | 5 | %62 ⚠ | %54 | 3/4/8 |
| 86 | Yayla 03 | 8 | 2 | 4 | %50 ⚠ | %54 | 3/3/8 |
| 87 | Yayla 04 | 9 | 3 | 4 | %44 ⚠ | %53 | 3/3/9 |
| 88 | Yayla 05 | 10 | 4 | 5 | %50 | %52 | 3/4/10 |
| 89 | Yayla 06 | 12 | 4 | 6 | %50 | %52 | 4/5/12 |
| 90 | Yayla 07 | 12 | 4 | 6 | %50 | %51 | 4/5/12 |
| 91 | Yayla 08 | 11 | 4 | 6 | %55 ⚠ | %51 | 4/5/11 |
| 92 | Yayla 09 | 10 | 4 | 5 | %50 | %50 | 3/4/10 |
| 93 | Yayla 10 | 9 | 3 | 5 | %56 ⚠ | %49 | 3/4/9 |
| 94 | Yayla 11 | 13 | 4 | 6 | %46 | %49 | 4/5/13 |
| 95 | Yayla 12 | 13 | 6 | 6 | %46 | %48 | 4/6/13 |
| 96 | Kasaba Pazarı 01 | 9 | 3 | 5 | %56 ⚠ | %52 | 3/4/9 |
| 97 | Kasaba Pazarı 02 | 10 | 2 | 5 | %50 | %51 | 3/4/10 |
| 98 | Kasaba Pazarı 03 | 10 | 3 | 4 | %40 ⚠ | %51 | 3/3/10 |
| 99 | Kasaba Pazarı 04 | 16 | 6 | 7 | %44 ⚠ | %50 | 5/6/16 |
| 100 | Kasaba Pazarı 05 | 12 | 3 | 6 | %50 | %49 | 4/5/12 |
| 101 | Kasaba Pazarı 06 | 12 | 5 | 6 | %50 | %49 | 4/5/12 |
| 102 | Kasaba Pazarı 07 | 10 | 3 | 5 | %50 | %48 | 3/4/10 |
| 103 | Kasaba Pazarı 08 | 11 | 3 | 6 | %55 ⚠ | %48 | 4/5/11 |
| 104 | Kasaba Pazarı 09 | 14 | 3 | 7 | %50 ⚠ | %47 | 5/6/14 |
| 105 | Kasaba Pazarı 10 | 13 | 5 | 6 | %46 | %46 | 4/5/13 |
| 106 | Kasaba Pazarı 11 | 13 | 3 | 6 | %46 | %46 | 4/5/13 |
| 107 | Kasaba Pazarı 12 | 15 | 5 | 7 | %47 | %45 | 5/7/15 |
| 108 | Bayram Yeri 01 | 9 | 3 | 4 | %44 ⚠ | %49 | 3/3/9 |
| 109 | Bayram Yeri 02 | 9 | 3 | 4 | %44 ⚠ | %48 | 3/3/9 |
| 110 | Bayram Yeri 03 | 15 | 3 | 6 | %40 ⚠ | %47 | 4/5/15 |
| 111 | Bayram Yeri 04 | 10 | 3 | 4 | %40 ⚠ | %47 | 3/3/10 |
| 112 | Bayram Yeri 05 | 13 | 3 | 6 | %46 | %46 | 4/5/13 |
| 113 | Bayram Yeri 06 | 13 | 3 | 6 | %46 | %45 | 4/5/13 |
| 114 | Bayram Yeri 07 | 9 | 3 | 5 | %56 ⚠ | %44 | 3/4/9 |
| 115 | Bayram Yeri 08 | 14 | 5 | 7 | %50 ⚠ | %43 | 5/6/14 |
| 116 | Bayram Yeri 09 | 12 | 3 | 5 | %42 | %42 | 3/4/12 |
| 117 | Bayram Yeri 10 | 10 | 3 | 5 | %50 ⚠ | %42 | 3/4/10 |
| 118 | Bayram Yeri 11 | 17 | 6 | 8 | %47 ⚠ | %41 | 5/7/17 |
| 119 | Bayram Yeri 12 | 15 | 5 | 6 | %40 | %40 | 4/6/15 |

Bölge ortalamaları: Sahil %60 · Köy Meydanı %57 · Yayla %51 · Kasaba Pazarı %49 · Bayram Yeri %45 · **Memleket %53** (Mahalle %73).

---

## Faz 7 — Harita seçimi, görünüm, dil (2026-09-24)

- **Harita seçimi:** harita ekranının başlığındaki bölge kapsülü artık düğme.
  Solunda amber etiketle haritanın adı (MAHALLE / MEMLEKET), sağında ok.
  Dokununca **HARİTALAR** penceresi: her harita bir kart (Orman zemin, Krem
  yazı): ad, alt başlık, yıldız "x / 180" ve ilerleme çubuğu; bulunulan harita
  amber çerçeve + "BURADASIN". Kilitli kartta kilit ve "Mahalle haritasını
  bitir, bu harita açılsın." Açık karta dokunmak o haritanın sıradaki
  bölümüne götürür; oyuncu son baktığı haritada açılır (`lastMap`).
  Karar gerekçesi: planın önerdiği iki yoldan "alt sekmede Haritalar sayfası"
  alt çubuğa altıncı düğme eklemek demekti; kapsül zaten ekranın en okunur
  yeri ve bölge okları kendi haritası içinde dönüyor — harita değiştirmek bir
  üst seviye iş, ayrı pencere daha anlaşılır.
- **Açılış duyurusu:** Memleket ilk açıldığında harita ekranında bir kez
  "Memleket açıldı! Haritalar'dan geçebilirsin." (`mapsAnnounced` kayıtta).
  Test yapısı açıkken susar (her şey zaten açık).
- Harita 2 harita görünümü: görsel yokken temanın renkleriyle çizilen zemin
  (üç kademeli düşüş aynen çalışıyor). Bütün yeni metinler `L.cs`'te
  İngilizceleriyle (MapsVerify denetliyor).
- `CameraFitVerify` 120 bölümü, `MemleketLayoutVerify` yerleşimi ölçüyor.
  `MapLayoutVerify` durak yerleşimi bölgeden bağımsız (12 durak aynı), değişmedi.

---

## Telefonda kontrol edilecek
- (Faz 7'ye kadar Memleket'e arayüzden girilemiyor; görsel kontrol o zaman.)
- Buz kabuğu görünümü (saydamlık, kenar parlaklığı), kırılma parçaları ve
  kırılma/tık sesinin perdesi.
- Karpuz misket görünümü (yeşil/koyu damar), parçaların kırmızı rengi,
  bölünme sesi. Bir karpuzu bölüp iki parçayı çıkar: sayaç toplam 2 artmalı;
  bütün çıkarınca da 2.
- Kum/çamur/çukur disklerinin görünümü ve zeminle uyumu; eğim oklarının
  görünürlüğü; çukura düşen misketin soluk görünmesi ve çukur sesi.
- Harita ekranı başlığı: MAHALLE/MEMLEKET etiketi ile bölge adının
  sığması (uzun bölge adları: "Mahalle Meydanı", "Kasaba Pazarı"), ok.
- HARİTALAR penceresi: kart düzeni, kilitli kart, "BURADASIN", geçiş.
- Memleket'in 5 bölgesinin görsel yokken çizilen zemin renkleri.
- 4.0 sahaya büyütülen bölümlerde kamera (test ölçüyor ama göz de baksın).

## Bekleyen işler (kullanıcıda)
- Memleket görselleri: `Resources/Mahalle/Map/Harita{Sahil,Koy,Yayla,Pazar,Bayram}.png`
  (1080×4476) ve `Resources/Mahalle/Ground/{Sahil,Koy,Yayla,Pazar,Bayram}.png`.
  Gelene kadar temadaki renklerle çizilen zemin görünür.
