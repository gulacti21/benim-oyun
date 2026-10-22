# Misko 1.2 — App Store metinleri ve gönderim kontrol listesi

Taslak (2026-10-21). App Store Connect → Misko → yeni sürüm **1.2**. Karakter sınırları parantez içinde.
Not: Google Play için de aynı "Yenilikler" metni kullanılabilir (sınır 500).

---

## 1. Bu sürümdeki yenilikler (4000)

### Türkçe
İki yeni harita, 120 yeni bölüm!

ŞEHİR
Otobüs durağından gökdelenlerin tepesine 60 bölüm. Alçak kaldırımlar, kaygan rögar kapakları ve
dokununca patlayan çatapat misketler.

LUNAPARK
Giriş kapısından sirk çadırına 60 bölüm. Misketi fırlatan tamponlar, misketi taşıyan yürüyen
bantlar ve yanından geçeni kendine çeken mıknatıs misket.

VE DAHASI
• Bütün bölümlerde 1, 2 ve 3 yıldız artık ayrı hedefler.
• Yeni misket: Pespembe. Misketlerine isim verebilirsin.
• Kesem'de indirimli güç paketleri.
• Boncuk sayına dokununca Kesem açılır.
• Harita seçimi yenilendi.

### English
Two new maps, 120 new levels!

CITY
60 levels from the bus stop to the top of the skyscrapers. Low curbs, slippery manhole covers and
firecracker marbles that pop when hit.

FUNFAIR
60 levels from the main gate to the circus tent. Bumpers that launch your marble, conveyor belts
that carry it and a magnet marble that pulls in everything rolling by.

AND MORE
• 1, 2 and 3 stars are now separate goals in every level.
• New marble: Pretty in Pink. You can now name your marbles.
• Discounted power packs in the Bag.
• Tap your bead count to open the Bag.
• A refreshed map picker.


---

## 2. Tanıtım metni (170) — istersen değiştir, sürüm göndermeden de değişebilir

**TR:** Tebeşirle çiz, nişan al, fiskele! 4 harita, 240 elle tasarlanmış bölüm: mahalleden memlekete,
şehirden lunaparka. Tampon, bant, mıknatıs ve daha fazlası!

**EN:** Draw the chalk ring, aim and flick! 4 maps and 240 hand-made levels: from the block to the
countryside, the city and the funfair. Bumpers, belts, magnets and more!

---

## 2b. Alt başlık ve anahtar kelimeler (KARAR, 2026-10-21)
**Alt başlık (TR, 30):** `Çocukluk oyunu, internetsiz`  (eskisi "Mahallenin en iyi nişancısı" — "nişan"
aramaları silahlı oyunlara gidiyor, ölçüldü)

**Anahtar kelimeler (TR, 84/100):**
```
bilye,mahalle,offline,lunapark,top,eski,klasik,zeka,fizik,bilardo,sokak,aile,oyunlar
```
Neden (App Store Türkiye arama önerileri + Misko'nun sırası ölçüldü, Logs: scratchpad kw.py):
- Adda olan (misko, misket, oyunu) ve alt başlıkta olan (çocukluk, internetsiz) kelimeler tekrar yazılmadı.
- Apple kelimeleri alanlar arasında birleştirir: internetsiz+oyunlar, lunapark+oyunu, çocukluk+oyunu, eski+oyun.
- "misket oyunları" aramasında ek olmadan da 2. çıkıyoruz → her ekli hali yazmaya gerek yok; sadece çok aranan
  "oyunlar" ayrıca eklendi.
- Çıkarılanlar: cicoz, 90lar (kimse aramıyor), nostalji (fotoğraf uygulamaları çıkıyor), nişan/atış (silahlı oyunlar),
  yerli ("yerli tüket" uygulamaları), tebeşir/çember/köy (aranmıyor).
- Kaynak notu: Apple'ın sıralaması İngilizceye göre ayarlı, Türkçe eklerde zayıf olabilir; bu yüzden ölçerek seçildi.

**İngilizce (EN) alanları:** sonra ABD mağazasında aynı yöntemle ölçülecek.

---

## 3. Açıklamada değişecek yerler
- "İKİ HARİTA, 120 BÖLÜM" → **"DÖRT HARİTA, 240 BÖLÜM"**; madde ekle:
  • Şehir: otobüs durağından gökdelenlere 60 bölüm.
  • Lunapark: giriş kapısından sirk çadırına 60 bölüm.
- "YENİ MEKANİKLER" listesine: kaldırım, rögar, çatapat, tampon, yürüyen bant, mıknatıs.
- 1.1 açıklamasındaki reklam/satın alma cümlesi doğru kalmalı (ödüllü reklam isteğe bağlı, günde 8;
  boncuk paketleri). **"Reklam yok, satın alma yok" cümlesi KALMAMIŞ olmalı** (1.1'de düzeltilmişti, kontrol et).
- EN açıklamada aynı değişiklikler.

---

## 4. App Review notu (İngilizce yaz)
```
No account is needed. Version 1.2 adds two new maps: City (unlocks after the last level of
Hometown) and Funfair (unlocks after the last level of City). New mechanics are introduced with an
in-game card the first time they appear.
Rewarded ads (optional, up to 8 per day) are in the Bag ("WATCH AD"), on the result screen and in
the continue offer. Bead packs (3 consumable in-app purchases, unchanged from 1.1) are in the Bag →
"BUY BEADS". Power packs in the Bag are bought with in-game beads only, not real money.
The app does not track users; the ATT prompt appears before ads are personalised.
```
> Kontrol et: ATT cümlesi 1.1'deki notla aynı mı? 1.1 notunu App Store Connect'ten kopyala, sadece
> ilk iki cümleyi ekle — eski notta doğru olan hiçbir şeyi silme.

---

## 5. Ekran görüntüleri
1.1'deki görüntülerde Şehir ve Lunapark yok. En az 2 yeni öneriyorum:
- Lunapark oyun ekranı (tampon + bant + mıknatıs görünen bir bölüm, örn. Giriş Buluşması)
- Haritalar penceresi (4 fotoğraflı kart)
Ben simülatörden 6.7" ve 6.5" boyutlarında alabilirim, TEST şeridi kapalı build ile.

---

## 6. Gönderim öncesi kontrol listesi (ret yememek)
- [ ] `MahalleProfile.TestUnlockAllLevels = false` (build 17'de **true**!) — TEST şeridi görünmemeli
- [ ] `Monetization.AdDailyLimit = 8` (✓ 2026-10-21), site ve açıklama da 8 diyor
- [ ] BuildTools BuildNumber bir artır (17'den sonra 18)
- [ ] MahalleVerify yeşil
- [ ] App Privacy: 1.2'de yeni SDK yok (Packages 1.1 ile aynı, 2026-10-21 kontrol edildi) → değişiklik gerekmez
- [ ] IAP ürün kimlikleri değişmedi, yeni IAP yok → IAP'leri sürüme yeniden eklemek gerekmez
- [ ] Info.plist: ATT metni (EN + tr), SKAdNetwork, şifreleme beyanı (1.1 ile aynı build ayarları)
- [ ] Yenilikler metni TR + EN, açıklama TR + EN güncel
- [ ] Review notu güncel (4. madde)
- [ ] TestFlight'ta: reklam izleme, boncuk satın alma (sandbox), güç paketi, Lunapark ve Şehir denendi
- [ ] Ekran görüntüleri (isteğe bağlı ama öneriyorum)
- [ ] Son tıklama (Submit for Review) kullanıcıda

## 7. Android (Google Play) 1.2
- versionCode 2 → 3, versionName 1.2, AAB build (BuildTools)
- Yenilikler metni (500 karakter): yukarıdaki TR/EN kısaltılmış hali
- Data Safety: yeni SDK yok → değişmez
- Kapalı test → üretim başvurusu ~2-3 Kasım (14 gün şartı); 1.2'yi kapalı teste yükleyip üretime 1.2 ile çıkmak mantıklı

---

## 8. Öne çıkarma başvurusu (Featuring Nomination) — taslak
App Store Connect → Uygulamalar → Misko → sol menü **Featuring → Nominations** → "+" → **Create Nomination**.
Tür: **New Content** (yeni harita/bölüm). 1.2 yayın tarihinden **en az 3 hafta önce** gönder.
Ad (sadece senin için): `Misko 1.2 — City & Funfair`

**Açıklama (İngilizce yaz, editörler okuyor):**
```
Misko brings back misket — the marble game Turkish kids played on every street, school yard and
village square for generations — as a physics game for iPhone. I'm a student and I built Misko
entirely on my own: design, code, levels and art direction.

Version 1.2 is the biggest update so far: two new maps and 120 new hand-made levels.
• City: low curbs, slippery manhole covers and firecracker marbles that pop when hit.
• Funfair: bumpers that launch your marble, conveyor belts that carry it, and a magnet marble
  that pulls in everything rolling by.
Every level is hand-designed and tuned with physics simulations so it stays fair and fun.
The game is free, works fully offline, needs no account and is available in Turkish and English.

Misko is a small love letter to a disappearing street game — a way for parents to show their kids
what they played, and for kids to discover it for the first time.
```
**Ek malzeme (giriş gerektirmeyen linkler):** web sayfası https://gulacti21.github.io/benim-oyun/ ,
App Store linki, (varsa) kısa oyun videosu linki.
**Bağlantı:** "Lunapark Açıldı" In-App Event'i kurulursa burada belirt.
**Ülkeler:** Türkiye öncelikli, sonra tüm ülkeler.

---

## 9. Ret kontrolü — durum (2026-10-21, build 18 üzerinden)
| Kontrol | Durum |
|---|---|
| Sürüm 1.2 / build numarası | ✓ 1.2 (18); mağaza build'i 19 olacak |
| ATT izin metni EN + TR (`NSUserTrackingUsageDescription`, tr.lproj) | ✓ var, "izin vermesen de aynı çalışır" yazıyor |
| SKAdNetwork listesi | ✓ 82 kimlik (1.1 ile aynı) |
| Şifreleme beyanı (`ITSAppUsesNonExemptEncryption=false`) | ✓ |
| Gizlilik manifestleri (Unity, IronSource, UnityAds, AdQuality) | ✓ 5 dosya |
| Yeni SDK / paket | ✓ yok (1.1 ile aynı) → App Privacy değişmez |
| Uygulama ikonu 1024, alfa kanalı yok | ✓ RGB |
| IAP: 3 boncuk paketi, tüketilebilir | ✓ değişmedi; "Satın alımları geri yükle" düğmesi tüketilebilirlerde zorunlu değil |
| Sadece iPhone (UIDeviceFamily=1) | ✓ |
| Günlük reklam sınırı = site = açıklama | ✓ 8 |
| TEST şeridi / bütün bölümler açık | ✗ build 18'de AÇIK → mağaza build'inde `TestUnlockAllLevels=false` (zorunlu) |
| Ekran görüntülerinde TEST yazısı, fiyat/"ücretsiz"/"en iyi" yok | ⏳ görüntüler çekilince kontrol |
| Yenilikler / açıklama / review notu güncel | ⏳ App Store Connect'e girilecek (taslak hazır) |
| Yaş derecelendirmesi anketi güncel | ⏳ App Store Connect'te bakılacak (Apple 2025'te soruları yeniledi) |
| 240 bölüm zorluk ölçümü | ⏳ sürüyor |
| Kullanıcı TestFlight'ta: reklam, sandbox satın alma, Lunapark, Şehir | ⏳ build 18 |
