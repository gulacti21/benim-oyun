# App Store metinleri (1.0)

App Store Connect → uygulama → Dağıtım → iOS Uygulaması 1.0. Her dil için ayrı alanlar.
Karakter sınırları parantez içinde; hepsi sınırın altında.

Gizlilik Politikası URL'si: `https://gulacti21.github.io/benim-oyun/gizlilik.html`
Destek URL'si: `https://gulacti21.github.io/benim-oyun/destek.html`
Pazarlama URL'si (isteğe bağlı): `https://gulacti21.github.io/benim-oyun/`
(GitHub Pages açılınca çalışır, bkz. en alttaki adımlar.)

---

## Türkçe

**Ad** (30): Misko: Misket Oyunu

**Alt başlık** (30): Mahallenin en iyi nişancısı

**Tanıtım metni** (170):
Tebeşirle çiz, nişan al, fiskele! Mahallede ve memlekette 120 elle tasarlanmış bölüm, buzlu ve
karpuz misketler, kum, çamur ve çukur seni bekliyor.

**Açıklama** (4000):
Çocukluğunun misket oyunu cebinde. Tebeşirle çizilmiş çembere nişan al, misketini fiskele ve
misketleri çemberin dışına çıkar. Kolay öğrenilir, ustalaşması zaman ister.

İKİ HARİTA, 120 BÖLÜM
• Mahalle: apartman önünden okul bahçesine, parktan meydana 60 bölüm.
• Memleket: yaz tatili başladı! Sahilden yaylaya, kasaba pazarından bayram yerine 60 bölüm daha.
• Her bölüm elle tasarlandı, her mahallenin sonunda bir ustalık sınavı var.

YENİ MEKANİKLER
• Buzlu misket: önce kabuğunu kırman gerek.
• Karpuz misket: sert vurunca ikiye bölünür.
• Kum yavaşlatır, çamur saplar, eğim yolunu değiştirir, çukur yavaş misketi yutar.

GERÇEK FİZİK
Her atış gerçek bir fizik hesabıyla oynanır: açıyı ve gücü sen belirlersin, misketler birbirine
çarpıp saçılır. Doğru açıyı bul, tek atışta birden fazla misket çıkar.

KOLEKSİYON VE GÜÇLER
• Bal Köpüğü'nden Galaksi'ye misket kaplamaları topla.
• Özel misketler ve güçlerle zor bölümleri aş: Baş Misket, Usta Gözü ve daha fazlası.
• Kazandığın boncuklarla kesene yeni misketler ekle.

HER GÜN YENİ BİR ŞEY
• Günün bölümü: her gün yeni bir bölüm, seriyi bozma, ödülün büyüsün.
• Sonsuz mod: süreye karşı yarış.
• Görevler ve başarımlar.

Reklam yok, gerçek parayla satın alma yok, internet gerekmez.

**Anahtar kelimeler** (100):
misket,bilye,nostalji,mahalle,sokak oyunu,nişan,fizik,bulmaca,köy,çember,atış,tebeşir,marbles

**Bu sürümdeki yenilikler:** (ilk sürümde istenmez)

---

## English

**Name** (30): Misko: Marble Game

**Subtitle** (30): Flick marbles out of the ring

**Promotional text** (170):
Draw the ring, take aim, flick! 120 handcrafted levels across The Block and Hometown, with ice and watermelon marbles, sand, mud and pits.

**Description** (4000):
The marble game from your childhood, in your pocket. Aim at the chalk-drawn ring, flick your marble and
knock the others out. Easy to learn, takes time to master.

TWO MAPS, 120 LEVELS
• The Block: 60 levels from the apartment steps to the schoolyard, the park and the town square.
• Hometown: summer holiday is here! 60 more levels from the beach to the highlands, the market and
  the festival grounds.
• Every level is handcrafted, and every area ends with a mastery test.

NEW MECHANICS
• Ice marbles: crack the shell first.
• Watermelon marbles: hit them hard and they split in two.
• Sand slows you down, mud stops you dead, slopes bend your path, pits swallow slow marbles.

REAL PHYSICS
Every shot is played out with real physics: you choose the angle and the power, marbles collide and
scatter. Find the right angle and knock out several marbles in one shot.

COLLECTION AND POWERS
• Collect marble skins, from Honey Foam to Galaxy.
• Beat tough levels with special marbles and powers: Big Shooter, Master's Eye and more.
• Spend the beads you earn to add new marbles to your bag.

SOMETHING NEW EVERY DAY
• Daily level: a new level every day, keep your streak going for bigger rewards.
• Endless mode: race against the clock.
• Missions and achievements.

No ads, no real-money purchases, no internet needed.

**Keywords** (100):
marbles,flick,ring,aim,physics,puzzle,retro,street game,nostalgia,chalk,casual,village,shooter

---

## Diğer alanlar (öneri)

- **Kategori:** Oyunlar → Birincil: Bulmaca (Puzzle), İkincil: Gündelik (Casual)
- **Yaş derecelendirmesi:** anketin hepsine "Yok" → 4+
- **App Privacy (Uygulama Gizliliği):** "Veri toplamıyoruz" (Data Not Collected).
  iCloud ve Game Center Apple'ın kendi hizmeti sayılır, geliştiricinin veri toplaması değildir.
- **Telif hakkı:** 2026 Ahmet Furkan Gülaçtı
- **Destek URL'si:** `docs/destek.html` (SSS + e-posta).

## Gizlilik sayfasını yayına alma (GitHub Pages, ücretsiz)

1. Dalı ana dala birleştir, push et (`docs/` ana dalda olmalı).
2. GitHub → repo → Settings → Pages → Source: "Deploy from a branch" → Branch: `main`, klasör `/docs` → Save.
3. Birkaç dakika sonra: `https://gulacti21.github.io/benim-oyun/gizlilik.html`

## Ret yememek için kontrol listesi (yüklemeden önce)

- [ ] **Test bayrakları kapalı** (`TestUnlockAllLevels`, `TestInfiniteBeads` → false). Açıkken ekranda
      turuncu "TEST" yazısı çıkar, bu tek başına ret sebebidir (kural 2.1).
- [ ] Menüde basılamayan "ONLİNE · Çok yakında" düğmesi gizli (`MahalleUI.ShowOnlineTeaser = false`, yapıldı).
- [ ] Gizlilik politikası oyunun içinden açılıyor (Ayarlar → GİZLİLİK POLİTİKASI, yapıldı) ve link çalışıyor.
- [ ] Destek ve gizlilik linkleri tarayıcıda açılıyor (GitHub Pages).
- [ ] App Privacy: "Data Not Collected". iCloud/Game Center açıldıysa da değişmez (Apple hizmetleri).
- [ ] Uygulama ikonu 1024×1024, **şeffaflık (alpha) yok** (ITMS-90717).
- [ ] Ekran görüntüleri: iPhone 6.9" (1320×2868) ve oyun iPad'i de desteklediği için iPad 13" (2064×2752).
      Simülatörden alınabilir; TEST yazısı olmayan sürümle çek.
- [ ] Açıklamada olmayan özellik yazma, olan özelliği abartma (kural 2.3). Metinler şu anki oyuna göre yazıldı.
- [ ] Kategori "Kids" SEÇME (çocuk kategorisi ek kurallar getirir); yaş 4+ yeterli.
- [ ] App Review notu (isteğe bağlı): "Hesap gerekmez. Memleket haritası Mahalle'nin son bölümü geçilince açılır."
