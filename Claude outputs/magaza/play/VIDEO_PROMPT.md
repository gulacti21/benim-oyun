/hyperframes

Misko adlı mobil misket oyunu için dikey (1080x1920, 30 fps) 20-25 saniyelik bir tanıtım / reklam videosu yap.
Kaynak: `kayit.mp4` (telefondan alınmış gerçek oynanış ekran kaydı). Logo: `Logo.png` (beyaz tebeşir yazısı, şeffaf).
Dil: Türkçe. Hedef: Instagram Reels / TikTok / YouTube Shorts ve Google Play tanıtım videosu.

## Oyun ne (bağlam)
Çocukluğun sokak misket oyunu. Tebeşirle çizilmiş çemberdeki misketleri, kendi misketini fiskeleyerek çemberin
dışına çıkarıyorsun. Gerçek fizik: misketler çarpışıp saçılıyor. 120 bölüm (Mahalle + Memleket haritaları),
buzlu misket (kabuğu kırılır), karpuz misket (ikiye bölünür), kum, çamur, eğim, çukur.
Görsel dil: asfalt / çim / taş zemin, beyaz tebeşir çizgileri, renkli cam misketler.
Renkler: Krem #F5EFE2, Kömür #2D2B25, Amber #F4AE42, Orman #203D34. Font: kalın, yuvarlak (Nunito Black benzeri).

## En önemli kural: İLK 2 SANİYE (hook)
Logo, açılış ekranı, menü ile BAŞLAMA. Video doğrudan aksiyonun ortasında açılsın. Kayıttan en tatmin edici
anı bul (tek atışta birden fazla misketin çemberden fırladığı an) ve videoyu onunla aç:
- 0.0-0.4 sn: atıştan hemen önce, nişan çizgisi gerilmiş halde donmuş kare + üstte büyük soru:
  "Tek atışta kaç misket?" (Amber, kalın, hafif sallanan / pop animasyonu)
- 0.4-2.0 sn: atış gerçek hızda, çarpışma anında 0.3 sn yavaş çekim (time-remap), çarpma anında
  kısa ekran sarsıntısı + beyaz flaş + misketlerin çıktığı yerde tebeşir tozu patlaması.
  Çıkan her misket için sayaç zıplasın: "1… 2… 3!"
Ses yoksa bile anlaşılmalı (çoğu kişi sessiz izler): her şey yazı ve hareketle anlatılsın.

## Akış (gövde)
2-6 sn  "Neredeyse!" anı: kayıttan bir misketin çember çizgisinde durduğu / kıl payı kaldığı an.
        Çizginin üstüne zoom, yazı: "Az kalsın…" → sonraki atışta çıkıyor, yazı: "Şimdi oldu!"
        (kıl payı → zafer kalıbı, izleyiciyi tutar)
6-12 sn Mekanik vitrini, hızlı kesmelerle (her biri ~1.5 sn, ritme oturan sert kesme):
        "Buzu kır" (buzlu misket), "Karpuzu böl" (karpuz ikiye ayrılıyor), "Çamura dikkat",
        "Çukur yutar". Her kesmede tebeşirle elle yazılmış gibi çizilen kısa başlık.
12-17 sn Ustalık / meydan okuma: zor bir bölümün atıştan önceki hali donar, yazı:
        "Bunu tek atışta çıkarabilir misin?" — cevabı GÖSTERME, bir sonraki sahneye geç
        (merak bırak).
17-21 sn Ölçek: iki harita ve 120 bölüm hissi. Harita ekranlarından hızlı dikey kaydırma (parallax),
        üstte "120 bölüm · 2 harita" sayacı hızla dolsun.
21-25 sn Kapanış: asfalt zemin, tebeşir çemberi kendini çizsin, içine renkli misketler yuvarlanıp
        üçgen dizilsin, MİSKO logosu tebeşir efektiyle belirsin. Altında: "Ücretsiz oyna"
        + Google Play / App Store rozet alanı (rozet yerini boş bırak, ben eklerim).

## Stil ve teknik
- Yazılar ekranın üst 1/3'ünde ve güvenli alanda (alt 350 px'e ve sağ kenara yazı koyma:
  Reels/TikTok arayüzü orayı kapatıyor).
- Yazılarda tebeşir dokusu / hafif el yazısı titreşimi; giriş animasyonu "çiziliyor" gibi (stroke reveal).
- Kesmeler müziğin vuruşuna otursun. Müzik yoksa 100-110 BPM varsay ve kesmeleri ona göre yerleştir.
- Ses efekti katmanı için işaret bırak: misket çarpışma "tık", çıkış "pop", tebeşir "sürtme".
- Renk: kayıt biraz canlandırılsın (saturation +10%), siyahlar ezilmesin.
- Arayüzdeki boncuk sayısı, "TEST" yazısı gibi dağınık öğeleri kırp veya bulanıklaştır; HUD kalabilir.
- Gerçekte oyunda olmayan bir şey gösterme veya vaat etme (mağaza kuralları): sadece kayıttaki oynanış.

## Çıktılar
1. `misko_hook_A.mp4` — yukarıdaki akış (25 sn).
2. `misko_hook_B.mp4` — aynı gövde, farklı açılış: kıl payı kaçırılan atışla başla, yazı
   "Sen olsan çıkarır mıydın?" (uydurma istatistik yazma) → sonra aynı bölümü başarıyla çıkar.
3. `misko_15s.mp4` — 15 sn kısa sürüm: hook + 3 mekanik + kapanış.
4. Google Play için yatay olmayan, reklam/oyun dışı metin içermeyen 30 sn sade sürüm
   (YouTube'a liste dışı yüklenecek).
Her birinin ilk karesini ayrıca PNG olarak ver (kapak / thumbnail).
