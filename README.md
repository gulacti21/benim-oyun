# MİSKETR · Mahalle

Unity 6.3 LTS (6000.3.23f1) için mevcut `gulacti21/benim-oyun` projesinin geliştirme sürümü.

## Açma ve deneme

Unity Hub'da bu klasörü açın. `Assets/Scenes/LevelSelect.unity` sahnesini açıp Play'e basın. Yeni arayüz Play sırasında oluşturulur. Telefon testi için Window → General → Device Simulator kullanın. Simulator açıkken dokunmaları Simulator penceresinde yapın; diğer Game penceresi giriş odağını paylaşır.

İlk atış: alt çizgideki misketi geriye çekip bırakın. Atıcıyı taşımak için alt çizginin başka bir noktasına dokunun. Güç çubuğu atış kuvvetini gösterir. Normal misket her zaman ücretsizdir.

## Uygulananlar

- Renkli iç kıvrımlar, parlak yansımalar ve yumuşak temas gölgeleriyle stilize cam misketler.
- Hafif dokulu toprak, düzensiz tebeşir çizgileri ve telefonun çentik alanına uyan arayüz.
- Hareketli el öğreticisi, güç göstergesi, +1 geri bildirimi, iOS titreşimi ve animasyonlu sonuç yıldızları.
- Beş mahalle, her birinde 12 bölüm; toplam 60 bölüm. İlk altı bölüm mevcut sürümün isimlerini ve yıldız hedeflerini korur. Parktan itibaren taş engelleri devreye girer.
- Mahalle sekmeleri, arena önizlemeleri, kilitli/açık bölümler ve gerçek yıldız şekilleri. Küçük ekranlarda içerik kaydırılır.
- Kese: Baş Misket, Demir Misket ve Usta Gözü. Başlangıçta her güç için bir ücretsiz kullanım vardır.
- Baş Misket sonraki atışta büyür ve daha güçlü iter. Demir Misket ağırlaşıp daha sert vurur. Usta Gözü ilk fiziksel temas noktasını gösterir; tam bir çoklu çarpışma tahmini değildir.
- Özel atışı seçmek ücretsizdir. Kullanım hakkı veya boncuk yalnızca gerçek atış bırakıldığında düşer. Vazgeçmek, duraklatmak veya haritaya dönmek ücretlendirilmez.
- Altı kuşanılabilir koleksiyon görünümü; görünümler atış gücünü değiştirmez.
- Bölüm ödülleri, üç görev ve mahalle sonu ustalık rozetleri / kozmetik ödülleri.
- Ses/titreşim ayarları; onay isteyen ilerleme sıfırlama ayarlar içinde.

## Boncuk dengesi

Başlangıç: 60 boncuk. İlk galibiyet: 20 + yeni yıldız başına 5 boncuk. Tekrar galibiyet: 3 + varsa iyileştirilen yıldız farkı. İlk ustalık galibiyeti ayrıca 35 boncuk ve ilgili koleksiyon görünümünü verir. Görev ödülleri 30, 50 ve 60 boncuktur ve bir kez alınır.

Baş Misket 12, Demir Misket 10, Usta Gözü 8 boncuk / atıştır. Ücretsiz kullanım varsa önce o harcanır. Satın alınan görünüme tekrar basmak tekrar ücret kesmez.

Gerçek para, reklam, sunucu veya uygulama içi satın alma entegrasyonu yoktur. İlerleme cihazda saklanır. Yerel kayıt, gerçek para ekonomisi için güvenli sunucu doğrulamasının yerine geçmez.

## Kayıt ve eski sürüm

Yeni kayıt anahtarı `MISKETR_Mahalle_v1`. İlk açılışta eski sürümün ilk altı bölümündeki yıldızlar taşınır. Eski kayıt anahtarları silinmez. Ayarlardaki sıfırlama yalnızca yeni sürümün ilerlemesini sıfırlar.

## Kontroller

`MISKETR → Verify Mahalle Systems`: ekonomi, tekrar ödülleri, kayıt aktarımı, koleksiyon, görevler, bölüm hedefleri ve kaynak bağlantıları için 204 kontrol. Testler geçici bellek verisi kullanır; oyuncunun kaydını değiştirmez. Bu kontroller fiziksel iPhone performansı veya bütün bölümlerin zorluk dengesi testi değildir.

9 Eylül kontrolü: Unity'de 204 kontrol geçti; iOS hedefli C# derlemesi ve yerel titreşim dosyasının Objective-C++ sözdizimi kontrolü geçti. Simulator'da Baş Misket'in büyümesi, Demir Misket seçimi, seçim sırasında bakiyenin korunması, duraklatma ve mahalleye dönüş görüldü. Otomatik sürükleme ile atış tetiklenemedi; bu nedenle gerçek dokunmayla özel atış tüketimi ve atış hissi cihazda ayrıca doğrulanmalıdır.

## iPhone / Xcode

File → Build Profiles içinde iOS'u etkinleştirin. Ardından `MISKETR → Build iOS Xcode Project` çalıştırın. Çıktı bu proje içindeki `Builds/iOS` klasörüne gider; eski Xcode projesinin üzerine yazmaz. Xcode'da bağlı iPhone'u ve kendi Signing Team hesabınızı seçin.

Unity'deki önizleme, gerçek cihazda performans / dokunma / titreşim testinin yerine geçmez. App Store gönderiminden önce fiziksel iPhone testi, tüm bölümlerin oynanabilirlik dengesi, kesintiden sonra kayıt, simge/mağaza görselleri ve mağaza gereklilikleri ayrıca tamamlanmalıdır.

## Kod düzeni

Mevcut fizik, ses, sahne ve iOS araçları korunur. Yeni sistemler `Assets/Scripts/Mahalle` altındadır. `MahalleBoot` mevcut iki sahnede eski Canvas'ı devre dışı bırakıp yeni arayüzü oluşturur. `Campaign` bölüm verilerini, `MahalleProfile` tek kayıt içindeki ekonomi ve ilerlemeyi, `MahalleUI` arayüzü yönetir. Görsel shader ve Türkçe yazı tipi `Assets/Resources/Mahalle` altında bulunur.
