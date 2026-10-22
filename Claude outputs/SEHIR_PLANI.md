# Harita 3 — Şehir: kalan 48 bölümün planı

Kurallar (kullanıcı kararları):
- Yavaş yavaş: her seferinde bir bölge (12 bölüm), telefonda denenir, beğenilirse sıradaki.
- İlk 12 bölüm (Şehir Merkezi) sadece yeni özellikler: alçak kaldırım, rögar kapağı, çatapat. **Bitti.**
- Kalan bölgelerde eski özellikler de gelir: kum, çamur, eğim, buzlu misket, karpuz, çukur.
- Her bölümde 1, 2 ve 3 yıldız ayrı alınabilir (testte zorunlu).
- Her bölge: 1-2. bölüm yeni gelen eski özelliği tek başına öğretir (kolay), sonra karışır,
  6-9. bölümlerden biri "Kesem bölümü", 10. bölüm nefes, 11. buluşma, 12. ustalık sınavı (2 yıldız).
- Zorluk sıradan oyuncu modeliyle ölçülür: bölge başı bir önceki bölgenin sonundan biraz kolay,
  bölge içinde yavaşça zorlaşır.

| Bölge | Bölümler | Eski özellik (yeni gelen) | Şehir özelliği | Hikâye |
|---|---|---|---|---|
| **2. Sanayi** | 13-24 | **kum** (inşaat kumu, 13'te), **çukur** (açık rögar, 16'da) | alçak kaldırım, çatapat | beton, yağ lekesi, hurdalık |
| **3. Liman** | 25-36 | **buzlu misket** (balık kasası buzu, 25'te), **eğim** (rıhtım rampası, 28'de) | rögar (ıslak zemin) | martılar, halat, vapur |
| **4. AVM** | 37-48 | **karpuz** (manav reyonu, 37'de), **çamur** (fıskiye kenarı, 40'ta) | rögar (parlak karo), çatapat | yürüyen merdiven, sinema |
| **5. Gökdelenler** | 49-60 | hepsi karışık, yenisi yok | hepsi | gece ışıkları, büyük final |

Hedef geçme oranları (tek deneme, bölge ortalaması):
Şehir Merkezi ~%100 → Sanayi ~%85 → Liman ~%75 → AVM ~%65 → Gökdelenler ~%55.
Ustalık sınavları ~%25-40, Kesem bölümleri güçsüz ≤ %5.

Her bölge için iş sırası:
1. 12 bölümün dizilimi + kart metinleri (eski kartlar zaten var, Şehir'de tekrar gösterilmez).
2. Çeşitlilik ayarı (SehirVarietyTuner), yerleşim testleri.
3. Sıradan oyuncu ölçümü, atış hakkı ve yıldız hedefleri.
4. MahalleVerify, Play modunda görsel kontrol.
5. Telefonda deneme, notlarına göre düzeltme.

Görseller (sen üretirsin, ben yazarım promptları): 5 harita (1080×4476) + 5 zemin.
Boyut: uygulama şu an 174 MB, sınır 200 MB. Görseller ~23 MB; kod kırpmayla dengelenecek.
