# YAZ TATİLİ — İLERLEME

Dal: `codex/yaz-tatili` (codex/mahalle-konsepti'den). Push edilmedi.
Plan: `YAZ_TATILI_PLANI.md`. Test: `MahalleVerify.Run` (batch, Unity kapalı).

## Özet
_(iş bitince buraya 10 satırlık özet gelecek)_

| Faz | Durum | Commit | MahalleVerify |
|---|---|---|---|
| 1 Harita altyapısı | ✅ bitti | (aşağıda) | 5461 kontrol yeşil (Harita 1'in 4291'i + 1170 yeni) |
| 2 Buzlu misket | ⏳ | | |
| 3 Bölünen misket | ⏳ | | |
| 4 Zemin kuralları | ⏳ | | |
| 5 60 bölüm dizilimi | ⏳ | | |
| 6 Zorluk ölçümü | ⏳ | | |
| 7 Harita seçimi, görünüm, dil | ⏳ | | |

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

## Telefonda kontrol edilecek
- (Faz 7'ye kadar Memleket'e arayüzden girilemiyor; görsel kontrol o zaman.)

## Bekleyen işler (kullanıcıda)
- Memleket görselleri: `Resources/Mahalle/Map/Harita{Sahil,Koy,Yayla,Pazar,Bayram}.png`
  (1080×4476) ve `Resources/Mahalle/Ground/{Sahil,Koy,Yayla,Pazar,Bayram}.png`.
  Gelene kadar temadaki renklerle çizilen zemin görünür.
