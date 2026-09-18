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

## TESTLER NASIL ÇALIŞTIRILIR

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
tamir 130.

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
`Assets/Audio`, `Assets/Fonts`, `Assets/TextMesh Pro` hep değişmiş görünür —
**asla stage etme**, pointer'ları bozarsın.

**`device_bash` dosya silemez**: git `index.lock`/`HEAD.lock` bırakır. Her git
işleminden önce `rm -f .git/index.lock .git/HEAD.lock` (silme izni verilmişse)
ya da `_to_delete/` altına taşı.

**Git kimliği**: commit'lerde
`-c user.name="Ahmet Furkan Gulacti" -c user.email="ahmetfurkangulacti@gmail.com"`

**Ağ**: Linux VM github.com'a erişemiyor, **push'u kullanıcı yapar.**

---

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

---

## ÇALIŞMA TARZI

- Türkçe konuş, teknik terimler İngilizce kalsın.
- Tahmin etme, ölç. Bu projede ekran görüntüsünden piksel sayarak çıkarım
  yapmak birkaç kez yanlış sonuca götürdü; doğru yol oyuna tanı koydurmak.
- Birden fazla yol varsa kısa artı/eksi yaz, sonra kendi tercihini söyle.
- Yanılıyorsa ilk cümlede söyle.
- Tam ve çalışır dosyalar ver, nasıl test edileceğini de yaz.
