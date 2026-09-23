# MİSKETR

Geleneksel Türk misket oyunundan yola çıkan, üstten görünümlü 3D mobil oyun.
Unity 6.3 LTS (6000.3.23f1) · URP · iOS · `com.gulacti.misketr` · sürüm 0.1.0
Dal: `codex/mahalle-konsepti`

Oyuncu bir çizgiden misket atar, hedefleri tebeşirle çizilmiş sahadan çıkarır.
60 bölümlük kampanya + dört oyun modlu düello bölümü.

---

## ASLA DOKUNMA

- `Assets/Settings/Mobile_RPAsset.asset`
- `ProjectSettings/ProjectSettings.asset`

Silme, üzerine yazma yok. Commit/push öncesi planını söyle, onay bekle.
Dosyaları önce yedekle.

- **Eski `~/Projects/MISKETR` projesini kullanma.** Güncel kaynak bu klasör.
- Eski ZIP paketlerini üzerine kopyalama, hiçbiri güncel değil.
- **Takım misketleri KALDIRILDI** (Galatasaray/Fenerbahçe/Beşiktaş/Trabzonspor
  kodları ve logoları). Geri ekleme.
- Preview modunu kullanıcı söylemeden kapatma.

---

## ÇALIŞMA DÜZENİ: MAC vs BULUT

**Bulutta (claude.ai/code, Mac kapalı):** Unity YOK. Derleyemezsin, test
çalıştıramazsın, sahneye bakamazsın. Sadece kod yaz, yeni bir dalda commit et,
PR aç. PR açıklamasına **"Unity'de test edilmedi"** yaz. Asla `main`e birleştirme.
Ölçülmüş sabitlere (aşağıda) bulutta dokunma — ölçmeden değiştirilmez.
LFS dosyalarına (.wav/.ttf/.png) dokunma.

**Mac'te (Unity açık):** bulutun açtığı PR'ı çek, testi çalıştır, yeşilse birleştir.

## TESTLER NASIL ÇALIŞTIRILIR

**Yeni yol (2026-09-24'ten beri, Unity AÇIKKEN):** Unity Pipeline paketi
(`com.unity.pipeline`) kurulu, `unity` CLI açık Editor'a bağlanıyor:

```bash
unity status                       # state "ready" olmalı
unity command eval --timeout 600 --no-banner 'try { MahalleVerify.Run(); return "OK"; } catch (System.Exception e) { return "HATA: " + e.Message; }'
grep -n "MAHALLE_VERIFY\|CHECK FAILED" ~/Library/Logs/Unity/Editor.log | tail
```

Önce açık sahnenin `isDirty` olmadığını kontrol et — test sahne açıyor,
kaydedilmemiş değişiklik kaybolur. İlk çalıştırmada 4291 kontrol geçti.
`.mcp.json` aynı bağlantıyı MCP olarak da tanımlıyor.

**Eski yol (Unity KAPALIYKEN, kuyruk script'i):**

**Unity Editor KAPALI olmalı.** Açıkken batchmode çalışmaz; log
"Successfully changed project path" satırından sonra durur, çıkış kodu 1 olur.

Kullanıcının Mac'inde bir Terminal script'i kuyruk dosyasını izliyor:

```
outputs/misketr-kuyruk/calistir.txt   ← komut adını buraya yaz
outputs/misketr-kuyruk/durum.txt      ← BITTI/CALISIYOR durumu buradan oku
outputs/misketr-kuyruk/unity.log      ← çıktı burada
```

Kullanım:

```bash
Q="$HOME/mnt/mi-sketr-adl-unity-misket-oyunumda/outputs/misketr-kuyruk"
echo "MahalleVerify.Run" > $Q/calistir.txt
# durum.txt BITTI diyene kadar bekle, sonra:
grep -n "error CS\|CHECK FAILED\|MAHALLE_VERIFY" $Q/unity.log
```

İzinli komutlar (script başlangıcında okunuyor, yenisi için script yeniden
başlatılmalı):

| Komut | Ne yapar |
|---|---|
| `MahalleVerify.Run` | **Ana test.** Diğer bütün doğrulamaları da çağırır (~2130 kontrol) |
| `DifficultyOrderVerify.Run` | 60 bölümün zorluk sırası |
| `LevelVarietyVerify.Run` | Bölümler birbirine benziyor mu |
| `DuelVerify.Run` · `DuelNetVerify.Run` | Düello kuralları ve ağ katmanı |
| `DuelPhysicsVerify.Run` | Düello fizik taraması (yavaş, dakikalar sürer) |
| `SpecialMarblesVerify.Run` | Özel misketler |
| `ParkPhysicsVerify.Batch*` | Bölüm fizik ölçümü (BatchChanged/All/Park/Finals/Meydan) |

`MahalleVerify.Run` içinde `ShooterTapVerify` de çalışır: gerçek `ShotController`
ile 60 bölüm × 3 ekran oranında çizgiye 81 dokunuş yapar. Kıpırdamadan dokunuş
nişana giderse, çizgideki bir nokta atıcıyı taşımazsa ya da sürükleme nişan
başlatmazsa **CHECK FAILED** verir.

Her değişiklikten sonra `MahalleVerify.Run` çalıştır. Yeşilse commit et.

---

## ONLINE / DÜELLO — ÇIKARILDI (geri getirilebilir)

2026-09-18: online ve aynı cihazda düello oyundan çıkarıldı. Menüdeki ONLİNE
butonu duruyor ama basılamıyor, altında "ÇOK YAKINDA" yazar.
Kaldırılanlar: `Assets/Scripts/Online/` (DuelController, DuelMatch, DuelNet,
DuelSession, DuelToss, DuelPlacement, DuelHole, WellMatch, DuelTransport),
`Assets/Editor/DuelVerify.cs`, `DuelNetVerify.cs`, `DuelPhysicsVerify.cs`,
MahalleUI'daki düello ekranları ve L.cs'teki düello çevirileri.
Kopyası: `Backups/Online-Kaldirildi/`. Git'te son hali: commit 6e97e61.
Geri getirilirse: MahalleUI'ya düello ekranları, LevelController.ConfigureForDuel,
MahalleWorld'deki DuelSession/DuelHole satırları ve MahalleVerify çağrıları da geri gelmeli.

---

## MİMARİ

```
Assets/Scripts/
  Mahalle/LevelBook.cs      60 bölümün elle tasarlanmış dizilimi (case 0-59)
  Mahalle/Campaign.cs       formülle üretilen taban + fiyatlar + kaplamalar
  Mahalle/MahalleProfile.cs kayıt, boncuk ekonomisi, TEST BAYRAKLARI
  Mahalle/MahalleUI.cs      bütün arayüz (kodla kuruluyor, prefab yok)
  Mahalle/MahalleWorld.cs   zemin, kamera, ışık, engeller
  Mahalle/ParkCorners.cs    Park dekoru (giriş/bank/ağaç)
  Mahalle/MarbleVisual.cs   misket görünümü
  Gameplay/LevelController.cs  bölüm akışı
  Gameplay/MarbleArena.cs      saha ve hedef misketler
  Gameplay/ShotController.cs   atış
  Online/                      düello: kurallar, fizik köprüsü, ağ
Assets/Editor/              bütün doğrulama araçları
Assets/Resources/Mahalle/   Marble / Environment / ContactShadow shader'ları
```

Bölüm verisi **koddan** geliyor: `Campaign.Database` çalışma anında üretiliyor,
`LevelBook.Apply` elle tasarlananları üstüne yazıyor. Diskte duran
`Assets/ScriptableObjects/Levels/*.asset` kullanılmıyor.

---

## DİL (TR / EN)

`Assets/Scripts/Mahalle/L.cs`: koddaki Türkçe metin anahtardır, İngilizce
karşılığı aynı dosyadaki tablodadır. `Text()`/`LabelButton()`/`Toast()` ve
`SetTextL()` metni otomatik çevirir. Değişken içeren metin `L.F("... {0} ...", x)`
ile yazılır (Türkçe şablon = anahtar). Yeni bir yazı eklersen tabloya İngilizcesini
de ekle; eksikse Türkçe görünür. Dil Ayarlar'dan, yoksa telefon diline göre.
Büyük harf için `L.Up()` kullan (ToUpperInvariant "İ"yi bozar).

---

## ÖLÇÜLEN DEĞERLER (tahminle değiştirme)

Bu projede her denge kararı ölçülerek verildi. Bir sabiti değiştirmeden önce
ilgili Verify aracını çalıştır.

**Kampanya fiziği** — misket kütle 0.05, drag 0.6, angular drag 0.5, ölçek 0.5,
`maxShotImpulse` 0.65. Saha çıkışı = `arenaSize + 0.25`.

**Zorluk merdiveni** (896.778 simülasyon): Apartman %97 → Okul %85 → Park %66
→ Toprak %62 → Meydan %57. Geçilemeyen bölüm yok.

**Çeşitlilik**: 1770 bölüm çifti, benzerlik sınırı 0.30'un altında **sıfır** çift.

**Ekonomi**: kese 40 boncuk başlangıç. İlk geçiş 6 boncuk, yıldız başına 3,
**tekrar oynama 0** (eskiden 3'tü, saatte ~700 boncuk farmlanabiliyordu).
Güçler 16/14/10/32, kaplamalar 0/40/80/110/140/180 + dört özel 400'er,
tamir 130 (ömür 150 atış).
Günün ödülü `25 + min(50, (seri-1)*7)` → 25'ten 75'e. Görevler 50/80/120.
Tek seferlik toplam gelir ~1365 + günlük akış. **Bölüm ödülleri bilerek
artırılmadı**: kampanya gelirini şişirmek boncuğu anlamsızlaştırır ve 1.1'deki
satın almaya satacak bir şey bırakmaz.

**Düello (çember/üçgen/dizi)**: kese 12, her el 4 ortaya, 5 el, turda en fazla
3 atış, 4 boş tur el sonu. Atıcı sahanın iç %35'inde durursa misketini kaybeder.

**Atış güçleri mod başına ayrı ölçüldü** — hepsinin hedefi "ortalama atış ~1
misket çıkarsın":
- Çember 3.2 · güç 1.00 → 1.03
- Üçgen 3.6 · güç 0.80 → 1.04 (aynı güçle 1.68 çıkıyordu, köşeler hunileyor)
- Dizi 3.4 · güç 0.55 → 1.46, kıpırdama eşiği 0.08
- Kuyu 3.0 · çukur 0.45 → atışların %34'ü giriyor

**Kütle denendi ve ELENDİ**: 1.4 kat ağırlaştırmak atışların %100'ünü boşa
çıkardı, misket kıpırdamıyor. Zorluk ağırlıktan değil açıdan gelir.

---

## DÜELLO MODLARI

Üçü aynı ekonomiyi paylaşır (kese, el, 5 el sonunda kesesi kalabalık kazanır):

| Mod | Kazanma koşulu |
|---|---|
| **ÇEMBER** | Çemberden çıkardığın misket senin |
| **ÜÇGEN** | Aynı kural, saha üçgen (köşeler 270/30/150) |
| **DİZİ** | Tek sıra, geçmeli dizilir, **kıpırdattığın** misket senin |

**KUYU** ayrı bir oyun (`WellMatch`, kese yok): bir turu almak için önce çukura
gir (**pişersin**), sonra rakibi sahadan çıkar. 3 tur, 2'sini alan kazanır.
Pişmeden çıkarırsan tur bitmez, rakibin misketi çizgiye döner.

Maç başında **sıra belirleme atışı**: boş sahaya birer atış, çizgiye en yakın
duran önce atar, çizgiyi geçen yanar.

---

## AĞ KATMANI (online)

Tasarım: **fizik ağa taşınmıyor.** Atan taraf fiziği kendinde çalıştırır,
sonucu ("şu misketler çıktı, atıcı şurada durdu") rakibe yollar; karşı taraf
kural motoruna uygular. `DuelMatch` saf ve deterministik olduğu için iki cihaz
aynı mesaj dizisiyle aynı duruma varır.

`IDuelTransport` arayüzü var, bugün `LoopbackTransport` ile tek süreçte test
ediliyor (70 kontrol). **Eksik olan tek parça gerçek taşıyıcı (Unity Relay)** ve
o Unity Cloud hesabı istiyor — kullanıcının açması gerekiyor
(Project Settings → Services → Relay + Lobby).

---

## BİLİNEN TUZAKLAR

**URP'de mor nesne = materyali/shader'ı yok.** `CreatePrimitive` Unity'nin
varsayılan materyalini verir, o Standard shader kullanır, URP'de mor çizilir.
Primitive oluşturduktan sonra **mutlaka** materyal ata.

**Görünüme ait işleri collider'dan ÖNCE yap.** `ParkCorners.Part()` içinde
collider satırı telefonda null gelip patlıyordu; metot yarıda kesilince nesne
konumsuz, ölçeksiz ve materyalsiz kalıyordu. Editörde sorun çıkmıyordu.

**`PrimitiveType.Cylinder` kullanma** — telefonda sorun çıkardı. Cube ve Sphere
sorunsuz.

**`Shader.Find` build'de null dönebilir.** Önce
`Resources.Load<Shader>("Mahalle/...")` dene, `Shader.Find` yedek olsun.

**Çalışma anında üretilen materyallere `hideFlags = HideAndDontSave` ver**,
yoksa Unity temizliğinde yok edilip nesne mor kalabiliyor.

**Git LFS**: `.wav/.ttf/.png` LFS'te, Linux VM'de git-lfs YOK. Bu yüzden
dokunulmamış ses/font/görsel dosyaları `git status`ta "M" görünür — gerçek
değişiklik değil, pointer/içerik karışması. **Bunları asla commit etme**, repoyu
bozarsın. Yeni görselleri kullanıcı kendi terminalinden ekler.

**Git kilit dosyaları**: yarıda kesilen bir git çağrısı `.git/index.lock` ve
`.git/HEAD.lock` bırakır, bu VM'de varsayılan olarak silinemez ve kullanıcının
git komutlarını bloklar. `device_request_delete_permission` ile izin iste, sonra
`rm -f .git/*.lock` ve `find .git/objects -name 'tmp_obj_*' -delete`.

**Git kimliği**: VM'de tanımsız. Commit öncesi repoya özel ayarla —
`git config user.name` / `user.email`, kullanıcının önceki commit'lerinden al.

**Push edilemez**: VM'in ağ çıkışı proxy tarafından engelli (HTTP 403).
`git push`u kullanıcı kendi terminalinden yapar.

**Unity Editor açıkken batchmode çalışmaz.** Çıkış kodu 1, derleme hatası 0.
Test çalıştırmadan önce kullanıcıya "Unity kapalı mı?" diye sor, "kapalı"
demesini bekle. Bir günde üç kez boşa denendi.

**Renk/yerleşim hatasını test yakalamaz.** `MahalleVerify` sadece derlemeyi ve
sayısal kuralları ölçer. Görsel değişiklikleri kullanıcı telefonda görmeden
"tamam" deme. Claude oyunu çalıştıramaz, ekran görüntüsü alamaz.

## AÇIK TEST BAYRAKLARI — YAYINDAN ÖNCE KAPAT

`Assets/Scripts/Mahalle/MahalleProfile.cs`:

```csharp
public static readonly bool TestUnlockAllLevels = true;  // → false
public static readonly bool TestInfiniteBeads   = true;  // → false
```

İkisi de açıkken oyunun altında turuncu "TEST" şeridi var ve `MahalleVerify`
yüksek sesle uyarıyor. Ayrıca oyun ekranında bölüm imzası + çizim tanısı
yazıyor (hangi binary'nin oynandığını ve bozuk nesne olup olmadığını gösterir);
bayraklar kapanınca ikisi de kaybolur.

---

## YAPILACAKLAR

**Kullanıcıda:**
- **Mağaza adı:** App Store Connect'te uygulama oluşturulurken ad "Misko: Misket Oyunu"
  (İngilizce "Misko: Marble Game") girilecek. İkon altındaki ad sadece "Misko"
  (Player Settings → Product Name). Oyun içi marka: MİSKO / MISKO.
- `git push` (biriken commit'ler)
- Unity Cloud bağlantısı → online oda kurma bunun arkasında
- Uygulama ikonu yok (1024×1024 PNG, **alpha kanalsız** — Apple `ITMS-90717`
  ile reddediyor; Preview'da Export → Alpha tikini kaldır)
- Apple Developer Program ($99/yıl) → TestFlight ve IAP. Ücretsiz hesapla
  kendi telefonuna kurulabiliyor (7 günde bir yenilenmesi gerekiyor)

**Kodda:**
- Relay taşıyıcısı + oda kur/katıl ekranları (Cloud bağlanınca)
- Müzik yok, sadece ses efektleri var
- ~~Kamera kırpması~~ DÜZELTİLDİ: `MahalleWorld.CameraSize` çember + çizgi dışı
  payını (yan 0.4, üst 0.35) hem yatayda hem üst başlığın altında gösterecek
  kadar açar, eskisinden asla yakın değil. 40 bölüm etkilendi (MEYDAN 12 %20).
  `CameraFitVerify` her `MahalleVerify.Run`'da 60 bölüm × 3 ekran oranını ölçer.
  Düello kamerası değişmedi.
- IAP: para mağazası yok. Boncuk mağazası (KESEM) çalışıyor.
- **Reklam + boncuk satın alma 1.1'e ERTELENDİ** (kullanıcı kararı). Sebep: ilk
  inceleme en riskli olanı ve reklam eklemek altı ayrı ret sebebi açıyor —
  privacy manifest (her SDK kendi dosyasını getirmeli), App Privacy etiketleri,
  ATT izni, Kids kategorisi yasağı, Paid Apps sözleşmesi, "Satın Alımları Geri
  Yükle" düğmesi. 1.0 reklamsız çıkacak, ekonomi günlük ödülle dengelendi.
  Yapılırken `unity:levelplay-unity-integration` ve
  `unity:implement-in-app-purchases` skill'lerini aç, API'yi kafadan yazma.
- Alt sekme ikonları bekleniyor: `Resources/Mahalle/Icons/Alt/` içine
  `SekmeGeri`, `SekmeHarita`, `SekmeKese`, `SekmeGrafik`, `SekmeListe` —
  512×512, şeffaf, **beyaz çizgi** (kod renklendiriyor). Gelene kadar kodla
  çizilen şekiller çalışıyor.
- Bundle ID yayında `com.gulacti.misko` olacak; **şimdi değiştirme**, kayıtları
  sıfırlar.
- Android: Google Play kişisel hesapta 12 test kullanıcısı × 14 gün kesintisiz
  şartı var, Apple'dan uzun sürer. Önce iOS, Android sonra.

---

## TASARIM DİLİ (22-23 Eylül 2026'da kuruldu)

Dört renk. `MahalleUI` içinde sabit:

```
Krem  #F5EFE2   Komur #2D2B25   Amber #F4AE42   Orman #203D34
```

Koyu **yazı** `Komur`, koyu **panel** `Orman`. Bunlar ayrı — karıştırma.

`MahalleGraphic.highlight` projede **tamamen kapalı**. Panelin üst kenarına
3.5px beyaz şerit çiziyordu, düz tasarımda çizgi gibi duruyordu. `true` yapma.

**Eski palet hâlâ duruyor**: `Paper/Ink/Muted/Gold/Cream/Line` sabitleri
dokunmadığımız ekranlarda (İstatistik, Görevler, Ayarlar, sonuç pencereleri,
Kesem) kullanılıyor — 189 yerde. Beş tanesi düz değiştirilebilir; `Ink` tek
başına hem yazı hem panel olduğu için ~60 kullanımın her birine tek tek bakmak
gerekiyor. Kullanıcı bir kez denedi, beğenmedi, geri alındı. Toplu değiştirme
yapma, ekran ekran ilerle ve her adımda kullanıcıya göster.

---

## HARİTA EKRANI (`MahalleMapView` + `MahalleUI.MapScreen`)

**Durak anatomisi** — hepsi aynı: ince kesik tebeşir çemberi (190px), içinde cam
misketler (sıradaki bölümde tek büyük, diğerlerinde üç küçük), karşı tarafında
amber numara etiketi, kömür kalın bölüm adı, üç yıldız. Sıradaki bölümde
yıldız yerine "SIRADAKİ" yazıyor.

**Seçim ≠ başlatma.** Durağa dokunmak bölümü *seçer*, alttaki tek amber OYNA
düğmesi başlatır. Seçili durağa ikinci kez dokunmak da başlatır. Kaydırırken
kazara seçilmesin diye `MahalleStopTap` 26px hareket eşiği koyar — Unity'nin
Button'ı parmak kaydıktan sonra bile tıklama sayıyor.

**Arka plan**, üç kademeli düşüşle: `Resources/Mahalle/Map/Harita<Ad>.png` →
yoksa `Resources/Mahalle/Ground/<Ad>.png` döşenir → o da yoksa eski prosedürel
`MahalleGround`. Fotoğraf varken kodla çizilen dekorlar (`theme.decor`)
çizilmez, ikisi üst üste binmesin diye.

**Harita görselleri 1080×4476 olmak zorunda** (oran 1:4.14 = `ContentHeight`).
ChatGPT 1:2.5 civarı üretiyor; farkı Claude kapatıyor: iki farklı banttan
dönüşümlü uzatma + 130px smoothstep geçiş + unsharp mask + aşağı doğru %6 ışık
düşüşü. Tekil kaynak görseller `Backups/harita-orijinal/` içinde. Import
ayarları: Clamp, mipmap **kapalı**, maxTextureSize **8192**, CompressedHQ —
4096 sınırında Unity küçültüp sonra ekranda büyütüyordu, bulanıklığın sebebi oydu.

**Güvenli alan**: harita hem üst çentik hem alt home indicator şeridine uzanır
(`UstGuvenliPay()` / `AltGuvenliPay()`), böylece uçlarda düz renk bant kalmaz.
Kaydırma `Clamped` — `Elastic`te fazla çekince arka zemin görünüyordu.

**Başlıkta düz krem blok yok**: mahalle adı kendi krem kapsülünde, misket /
yıldız / boncuk hapları haritanın üzerinde yüzüyor.

`MapLayoutVerify` her `MahalleVerify.Run`'da 12 durak × 3 ekran oranı ölçer:
taşma, üst üste binme, başlık ve alt düğme payları.

---

## KAMERA (oyun ekranı)

`MahalleWorld`: `CamFocusZ = -0.5`, `HudTopRef = 356`, `HudGap = 55`.

Çemberin tepesi ile üstteki bilgi paneli arası eskiden 45px'ti, şimdi ~127px ve
beş mahallede de aynı. Odak kaydırması zoom değiştirmez, sadece kadrajı aşağı
alır — ölçülmüş yıldız hedeflerine ve fiziğe dokunmaz. Kullanıcı bu değeri üç
turda ayarladı (-1.1 → -0.4 fazla, -0.75 orta, **-0.5 kabul edildi**).

---

## ATIŞ GİRDİSİ (`ShotController`) — dokunma ≠ sürükleme

Misketin yakınına (`grabRadius` = 1.15 × `InputScale`) basmak **hemen nişan
değildir**. Parmak basılan yerden `DragThresholdPx` (ekran yüksekliğinin
%1.2'si, en az 12px) kayarsa nişan başlar; kaymadan kalkarsa dokunuş sayılır ve
çizgideyse atıcı oraya taşınır (`ReleaseTap` → `TryMoveOnLine`).

Neden (2026-09-23, ölçüldü): eski halde çizgiye dokunuşların ~%45'i nişana
gidiyordu ve `GetShot` gücü misketin merkezinden ölçtüğü için kıpırdamadan
kalkan parmak **%39'a kadar güçle atış** yapıyordu (180/180 durumda). #9 Son
Basamak ve #40 Yan Çizgi'de `halfWidth` (0.85/0.9) `grabRadius`'tan küçük
olduğundan çizgi hiç çalışmıyordu. Düzeltmeden sonra: kazara atış 0/180,
çizginin her noktası taşıyor. Güç hesabı ve ölçülmüş dengeler değişmedi.

- Görünen çizgi (`LineRenderer`) ile `ClampToLine` aynı `halfWidth`'i kullanır;
  "çizgi göründüğünden kısa" değildir.
- `positionLocked` (Yerinde Kal sonrası) taşımayı **bilerek** kapatır, ekranda
  ipucu var.
- Yeni girdi davranışı eklersen `ShooterTapVerify`'ı güncelle.

---

## USTA GÖZÜ (`AimIndicator`)

Kılavuz **sadece** Usta Gözü gücü seçiliyken çalışır. Normal atışta kısa yön
çizgisi vardır, tahmin yoktur. Herkese açmak o gücün tek satış noktasını yok
eder — kullanıcı bunu açıkça reddetti.

Usta Gözü iki çizgi gösterir: ilk temas noktası (küre) + **çarpılan misketin
fırlayacağı yön**. Yön iki kürenin merkezleri arasından hesaplanır. Hedef misket
değil de duvarsa, kendi misketinin sekme açısı çizilir.

---

## ÇALIŞMA TARZI

- Türkçe konuş, teknik terimler İngilizce kalsın.
- Tahmin etme, ölç. Bu projede ekran görüntüsünden piksel sayarak çıkarım
  yapmak birkaç kez yanlış sonuca götürdü; doğru yol oyuna tanı koydurmak.
- Birden fazla yol varsa kısa artı/eksi yaz, sonra kendi tercihini söyle.
- Yanılıyorsa ilk cümlede söyle.
- Tam ve çalışır dosyalar ver, nasıl test edileceğini de yaz.
