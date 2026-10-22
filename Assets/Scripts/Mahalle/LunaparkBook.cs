using System.Collections.Generic;
using UnityEngine;

// HARİTA 4 — LUNAPARK: elle tasarlanmış bölümler (SehirBook'un Lunapark karşılığı).
// Konumlar çemberin merkezine göre: x sağa, z ileri (atıcıdan uzağa). Atıcı z=-4.2.
//
// Kural (kullanıcı, 2026-10-20): Şehir'den biraz daha zor. İlk bölge (Giriş Kapısı) SADECE yeni
// özellikler: 1. bölüm TAMPON, 3. bölüm YÜRÜYEN BANT, 5. bölüm MIKNATIS MİSKET, sonra karışık.
// Sonraki bölgelerde eski özellikler (kum, çamur, çukur, buz, karpuz, eğim, kaldırım, rögar, çatapat)
// karışık gelir. Her bölgenin 8. bölümü Kesem bölümü (güçsüz ≤ %20), 12. ustalık (2★ geçer).
// Misket sayısı eski haritalardan ~%20 fazla. Her bölümde 1/2/3 yıldız ayrı.
// İlk bölge (kullanıcı, 2026-10-21): 12 bölümün HEPSİNDE 3 yıldız Kesem'siz kolay alınsın. Telefonda denedi, hiç
// 3 yıldız alamadı → ölçülen atış haklarına +3 eklendi (sıradan oyuncu modeli gerçek oyuncudan iyi çıkıyor).
// Özellikler bölümde KULLANDIRILMALI (kullanıcı: "1'de tampon kullanmadan da oluyor"): misketler tamponun,
// bandın, mıknatısın etkisi altında dizilir; kenarda süs gibi duran özellik olmaz.
// Atış hakkı ve yıldızlar sıradan oyuncu modeliyle ölçülür (Tuning tablosu).
public static class LunaparkBook
{
    public static void Apply(LevelData level, int index)
    {
        switch (index)
        {
            // ================= GİRİŞ KAPISI · sadece yeni özellikler =================

            // 01 Bilet Gişesi — YENİ: tampon. Üç tamponun arasına sıkışmış misketler (langırt gibi): vurulan
            // misket tampona çarpar, tampon onu hızla çemberin dışına fırlatır. (Kullanıcı: "tampon kullanmadan
            // da oluyor" — ilk sürümde tamponlar kenardaydı.)
            case 0:
                Big(level, 3.2f, 5);
                level.marbles = Spots(-.6f, 1.2f, .6f, 1.2f, 0f, 1.5f, -.45f, 2.05f, .45f, 2.05f, 0f, 2.6f, -1.75f, 1.35f, 1.75f, 1.35f);
                level.obstacles = new[] { B(0f, .6f), B(-1.1f, 1.95f), B(1.1f, 1.95f) };
                break;

            // 02 Balon Arabası — balonlar büyük tamponun çevresinde: vurulan balon tampona çarpıp fırlar.
            case 1:
                Big(level, 3.2f, 5);
                level.marbles = Join(Ring(0f, 1.4f, .95f, 8, 22.5f), P(-1.9f, .6f), P(1.9f, .6f));
                level.obstacles = new[] { B(0f, 1.4f, .7f) };
                break;

            // 03 Pamuk Şekerci — YENİ: yürüyen bant. Sağdaki iki bant dışarı taşır: misketi banda itmen yeter,
            // gerisini bant yapar. Soldaki bant İÇERİ taşır: oraya kaçan misket geri gelir.
            case 2:
                Big(level, 3.3f, 5);
                level.marbles = Join(Grid(-.6f, 1.2f, 2, 3, .62f, .62f), P(.4f, 2.4f), P(.4f, 0f));
                level.zones = Zones(Belt(1.5f, 1.2f, .65f, 0f), Belt(2.5f, 1.2f, .5f, 0f), Belt(-2.2f, 1.0f, .6f, 0f));
                break;

            // 04 Giriş Kemeri — iki sıra; iki yanda dışarı yürüyen bant: misketi yana, banda it. Önde tampon düz atışı keser.
            case 3:
                Big(level, 3.4f, 5);
                level.marbles = Join(Row(1.4f, -1.3f, 1.3f, 5), Row(2.1f, -1.0f, 1.0f, 4));
                level.obstacles = new[] { B(0f, .3f) };
                level.zones = Zones(Belt(-2.3f, 1.6f, .55f, 180f), Belt(2.3f, 1.6f, .55f, 0f));
                break;

            // 05 Patlamış Mısır — YENİ: mıknatıs misket. Ortadaki gümüş misket, vurulup kaçan komşularını
            // geri çeker: önce mıknatısı çıkar.
            case 4:
                Big(level, 3.3f, 5);
                level.marbles = Kind(Join(Ring(0f, 1.1f, 1.05f, 8, 0f), P(0f, 1.1f)), MarbleKind.Magnet, 8);
                break;

            // 06 Konfeti Yağmuru — dağınık konfeti, ortada tampon, solda ileri yürüyen bant.
            case 5:
                Big(level, 3.5f, 6);
                level.marbles = Spots(-1.6f, .4f, -.6f, .6f, .6f, .5f, 1.5f, .9f, -1.1f, 1.7f, .1f, 2.0f, 1.2f, 1.9f, -1.9f, 2.4f, 1.9f, 2.6f, -.4f, 2.9f);
                level.obstacles = new[] { B(0f, 1.25f) };
                level.zones = Zones(Belt(-2.3f, 1.0f, .55f, 90f));
                break;

            // 07 Hediyelik Dükkân — 3×3 rafın GÖBEĞİ mıknatıs: komşular kaçamaz; sağda dışarı bant, solda tampon.
            case 6:
                Big(level, 3.4f, 6);
                level.marbles = Kind(Join(Grid(0f, 1.3f, 3, 3, .66f, .66f), P(0f, 2.9f)), MarbleKind.Magnet, 4);
                level.obstacles = new[] { B(-1.9f, .4f) };
                level.zones = Zones(Belt(2.0f, 1.3f, .55f, 0f));
                break;

            // 08 Tren İstasyonu — KESEM BÖLÜMÜ: uzun peron sırası, ortasında mıknatıs; iki tampon girişi
            // kapatır, önde yana bant.
            case 7:
                Big(level, 3.6f, 5);
                level.marbles = Kind(Join(Row(1.7f, -2.1f, 2.1f, 8), Row(2.55f, -1.0f, 1.0f, 3)), MarbleKind.Magnet, 3, 9);
                level.obstacles = new[] { B(-.9f, .45f), B(.9f, .45f) };
                level.zones = Zones(Belt(0f, -1.0f, .6f, 0f));
                break;

            // 09 Fotoğraf Köşesi — 10'luk ıstaka, iki yanında mıknatıs: yana kaçan misket yakalanır.
            case 8:
                Big(level, 3.5f, 6);
                level.marbles = Kind(Join(Rack(0f, 1.5f, 4, .62f), P(-2.05f, 1.0f), P(2.05f, 1.0f)), MarbleKind.Magnet, 10, 11);
                level.obstacles = new[] { B(0f, -.5f) };
                break;

            // 10 Işıklı Yol — nefes: iki sıra, arkada üç dışarı bant: misketi banda kadar itmen yeter.
            case 9:
                Big(level, 3.4f, 5);
                level.marbles = Join(Row(1.8f, -1.6f, 1.6f, 5), Row(1.1f, -1.0f, 1.0f, 3));
                level.zones = Zones(Belt(-1.2f, 2.9f, .5f, 90f), Belt(0f, 3.0f, .5f, 90f), Belt(1.2f, 2.9f, .5f, 90f));
                break;

            // 11 Giriş Buluşması — büyük halka, ortasında mıknatıs; iki tampon, önde bant. Hepsi bir arada.
            case 10:
                Big(level, 3.7f, 6);
                level.marbles = Kind(Join(Ring(0f, 1.2f, 1.6f, 10, 0f), P(0f, 1.2f)), MarbleKind.Magnet, 10);
                level.obstacles = new[] { B(-1.95f, -.7f), B(1.95f, -.7f) };
                level.zones = Zones(Belt(0f, -1.1f, .55f, 0f));
                break;

            // 12 Ustalık Sınavı — ıstaka, arkada mıknatıs, yanlarda iki misket; önde iki tampon ve geri
            // yürüyen bant. Geçmek için 2 yıldız.
            case 11:
                Big(level, 3.8f, 7);
                level.marbles = Kind(Join(Rack(0f, 1.6f, 4, .64f), P(0f, 3.0f), P(-2.2f, 1.2f), P(2.2f, 1.2f)), MarbleKind.Magnet, 10);
                level.obstacles = new[] { B(-1.2f, -.3f), B(1.2f, -.3f) };
                level.zones = Zones(Belt(0f, -1.15f, .55f, 180f));
                break;

            // ================= ÇARPIŞAN ARABALAR · tampon ağırlıklı; kaldırım, çatapat, rögar, kum =================

            // 13 Jeton Makinesi — 10'luk ıstaka; iki yanda tampon, önde kaygan rögar atışı hızlandırır.
            case 12:
                Big(level, 3.6f, 6);
                level.marbles = Rack(0f, 1.7f, 4, .62f);
                level.obstacles = new[] { B(-1.6f, .6f), B(1.6f, .6f) };
                level.zones = Zones(Slick(0f, -.6f, .6f));
                break;

            // 14 Pist Kenarı — pist yayı, içeride üç tampon; önde alçak kaldırım. Yayın ortası çatapat.
            case 13:
                Big(level, 3.6f, 6);
                level.marbles = Kind(ArcAt(0f, 0f, 2.5f, 20f, 160f, 10), MarbleKind.Catapat, 4);
                level.obstacles = new[] { B(-1.2f, 1.2f), B(0f, 1.6f), B(1.2f, 1.2f), O(0f, -.6f, 1.4f, .3f) };
                break;

            // 15 Kırmızı Araba — 4×3 araba bloğu, ortası çatapat; önde tampon, iki yanda kum.
            case 14:
                Big(level, 3.7f, 7);
                level.marbles = Kind(Grid(0f, 1.4f, 4, 3, .66f, .66f), MarbleKind.Catapat, 5);
                level.obstacles = new[] { B(0f, -.3f) };
                level.zones = Zones(Sand(-2.3f, 1.4f, .5f), Sand(2.3f, 1.4f, .5f));
                break;

            // 16 Lastik Duvar — üç tampondan duvar; misketler arkasında. Arkadaki çamura kaçan saplanır.
            case 15:
                Big(level, 3.6f, 7);
                level.marbles = Join(Row(1.4f, -1.5f, 1.5f, 6), Row(2.1f, -1.2f, 1.2f, 5));
                level.obstacles = new[] { B(-1.2f, .3f), B(0f, .3f), B(1.2f, .3f) };
                level.zones = Zones(Mud(0f, 2.9f, .4f));
                break;

            // 17 Çarpışma Köşesi — iki park yeri, ortada mıknatıs; önde iki alçak kaldırım.
            case 16:
                Big(level, 3.7f, 7);
                level.marbles = Kind(Join(Grid(-1.4f, 1.3f, 2, 2, .62f, .62f), Grid(1.4f, 1.3f, 2, 2, .62f, .62f), Tri(0f, 2.5f, .62f), P(0f, 1.0f)), MarbleKind.Magnet, 11);
                level.obstacles = new[] { O(-1.4f, .2f, 1.0f, .3f), O(1.4f, .2f, 1.0f, .3f) };
                break;

            // 18 Yarış Şeridi — iki şerit, ikişer buzlu; şeritlerin yanında geri yürüyen bantlar, önde tampon.
            case 17:
                Big(level, 3.7f, 7);
                level.marbles = Kind(Join(Line(-.6f, .3f, -.6f, 2.7f, 5), Line(.6f, .3f, .6f, 2.7f, 5)), MarbleKind.Ice, 2, 7);
                level.obstacles = new[] { B(0f, -.5f) };
                level.zones = Zones(Belt(-1.7f, 1.5f, .6f, 90f), Belt(1.7f, 1.5f, .6f, 90f));
                break;

            // 19 Dönemeç — virajlı dizi, pist sağa yatık (eğim); sağda tampon.
            case 18:
                Big(level, 3.7f, 7);
                level.marbles = Join(Wave(-2.0f, 2.0f, 9, 1.3f, .5f, 1.3f), P(-1f, 2.6f), P(0f, 2.6f), P(1f, 2.6f));
                level.obstacles = new[] { B(2.4f, .4f) };
                level.slope = new Vector2(.5f, 0f);
                break;

            // 20 Kaza Yeri — KESEM BÖLÜMÜ: 15'lik ıstakanın ucunda mıknatıs, iki tampon, önde kaldırım.
            case 19:
                Big(level, 3.9f, 5);
                level.marbles = Kind(Rack(0f, 1.9f, 5, .6f), MarbleKind.Magnet, 0);
                level.obstacles = new[] { B(-1.0f, .3f), B(1.0f, .3f), O(0f, -.6f, 1.4f, .3f) };
                break;

            // 21 Sıra Bekleme — zikzak sıra, iki karpuz; önde yana bant, arkada tampon.
            case 20:
                Big(level, 3.8f, 7);
                level.marbles = Kind(Join(Zig(-2.2f, 2.2f, 9, 1.0f, 1.6f), Row(2.4f, -1.2f, 1.2f, 4)), MarbleKind.Split, 3, 10);
                level.obstacles = new[] { B(0f, 3.1f) };
                level.zones = Zones(Belt(0f, -.4f, .55f, 180f));
                break;

            // 22 Son Tur — nefes: kenara yakın yay, ortada tampon.
            case 21:
                Big(level, 3.6f, 6);
                level.marbles = ArcAt(0f, 0f, 2.7f, 20f, 160f, 10);
                level.obstacles = new[] { B(0f, 1.0f) };
                break;

            // 23 Arabalar Buluşması — büyük halka, ortada mıknatıs, iki çatapat; önde rögar.
            case 22:
                Big(level, 3.8f, 7);
                level.marbles = Kind(Kind(Join(Ring(0f, 1.3f, 1.7f, 12, 0f), P(0f, 1.3f)), MarbleKind.Magnet, 12), MarbleKind.Catapat, 3, 9);
                level.obstacles = new[] { B(-2.3f, -.5f), B(2.3f, -.5f) };
                level.zones = Zones(Slick(0f, -.9f, .6f));
                break;

            // 24 Ustalık Sınavı — büyük çarpı, iki yanda mıknatıs; önde iki tampon ve çukur.
            case 23:
                Big(level, 4.0f, 8);
                level.marbles = Kind(Join(XShape(0f, 1.5f, 3, .66f), P(-2.4f, 1.5f), P(2.4f, 1.5f)), MarbleKind.Magnet, 13, 14);
                level.obstacles = new[] { B(-.9f, -.4f), B(.9f, -.4f) };
                level.zones = Zones(Pit(0f, -1.2f, .45f));
                break;

            // ================= DÖNME DOLAP · bant ağırlıklı; buz, eğim, kum, karpuz =================

            // 25 Dolap Altı — kabin halkası, iki buzlu kabin; iki yanda dışarı yürüyen bant.
            case 24:
                Big(level, 3.7f, 6);
                level.marbles = Kind(Join(Ring(0f, 1.4f, 1.5f, 10, 18f), P(0f, 1.4f)), MarbleKind.Ice, 0, 5);
                level.zones = Zones(Belt(-2.4f, 1.4f, .5f, 180f), Belt(2.4f, 1.4f, .5f, 0f));
                break;

            // 26 Atlıkarınca Yanı — atlıkarınca (halka, ortası mıknatıs) ve kabin bloğu; kum.
            case 25:
                Big(level, 3.9f, 7);
                level.marbles = Kind(Join(Ring(-1.3f, 1.5f, 1.0f, 7, 0f), P(-1.3f, 1.5f), Grid(1.6f, 1.5f, 2, 3, .62f, .62f)), MarbleKind.Magnet, 7);
                level.zones = Zones(Sand(1.6f, -.2f, .5f));
                break;

            // 27 Gondol — köşeleri buzlu gondol; zemin ileri yatık (eğim), solda bant.
            case 26:
                Big(level, 3.8f, 7);
                level.marbles = Kind(Join(Grid(0f, 1.6f, 3, 3, .68f, .68f), Row(2.9f, -1f, 1f, 3)), MarbleKind.Ice, 0, 2, 6, 8);
                level.zones = Zones(Belt(-2.2f, .5f, .55f, 90f));
                level.slope = new Vector2(0f, .4f);
                break;

            // 28 Yürüyen Yol — iki sıra; arkada dışarı yürüyen iki bant, önde çamur tuzağı.
            case 27:
                Big(level, 3.8f, 7);
                level.marbles = Join(Row(1.0f, -2.0f, 2.0f, 7), Row(2.0f, -1.5f, 1.5f, 5));
                level.zones = Zones(Belt(-1.2f, 2.9f, .5f, 90f), Belt(1.2f, 2.9f, .5f, 90f), Mud(0f, -.6f, .5f));
                break;

            // 29 Dondurmacı — üç top dondurma (karpuz başlı), arkada tamponlar, önde kum.
            case 28:
                Big(level, 3.6f, 6);
                level.marbles = Kind(Join(Tri(-1.2f, 1.2f, .62f), Tri(0f, 2.0f, .62f), Tri(1.2f, 1.2f, .62f), P(0f, .6f)), MarbleKind.Split, 0, 3, 6);
                level.obstacles = new[] { B(-1.9f, 2.4f), B(1.9f, 2.4f) };
                level.zones = Zones(Sand(0f, -.5f, .5f));
                break;

            // 30 Tepe Noktası — yokuş atıcıya doğru (eğim); ıstaka, iki yanda dışarı bant.
            case 29:
                Big(level, 3.8f, 7);
                level.marbles = Join(Rack(0f, 2.0f, 4, .62f), P(-2f, 1f), P(2f, 1f));
                level.zones = Zones(Belt(-1.4f, 0f, .5f, 180f), Belt(1.4f, 0f, .5f, 0f));
                level.slope = new Vector2(0f, -.5f);
                break;

            // 31 Kabin Sırası — iki kabin sırası, iki buzlu, bir çatapat; önde rögar.
            case 30:
                Big(level, 3.9f, 7);
                level.marbles = Kind(Kind(Join(Grid(0f, 1.4f, 5, 2, .7f, .9f), P(0f, 2.9f), P(-1.4f, 2.8f), P(1.4f, 2.8f)), MarbleKind.Ice, 1, 3), MarbleKind.Catapat, 7);
                level.zones = Zones(Slick(0f, -.4f, .55f));
                break;

            // 32 Kızıl Gökyüzü — KESEM BÖLÜMÜ: elmas, ortası mıknatıs, ucu buzlu; iki yanda İÇERİ yürüyen bant.
            case 31:
                Big(level, 3.8f, 5);
                level.marbles = Kind(Kind(Diamond(0f, 1.7f, .62f), MarbleKind.Magnet, 6), MarbleKind.Ice, 0);
                level.zones = Zones(Belt(-2.3f, 1.7f, .5f, 0f), Belt(2.3f, 1.7f, .5f, 180f));
                break;

            // 33 Işıklı Tekerlek — büyük tekerlek, göbeği mıknatıs, iki çatapat; önde çamur.
            case 32:
                Big(level, 3.9f, 7);
                level.marbles = Kind(Kind(Join(Ring(0f, 1.5f, 1.6f, 12, 0f), P(0f, 1.5f)), MarbleKind.Magnet, 12), MarbleKind.Catapat, 0, 6);
                level.zones = Zones(Mud(0f, -.5f, .45f));
                break;

            // 34 Bank Başı — nefes: iki sıra bank, ortada ileri bant.
            case 33:
                Big(level, 3.7f, 6);
                level.marbles = Join(Row(2.3f, -2.2f, 2.2f, 8), Row(.4f, -1.6f, 1.6f, 3));
                level.zones = Zones(Belt(0f, 1.4f, .6f, 90f));
                break;

            // 35 Dolap Buluşması — artı, göbeği mıknatıs, bir buzlu; dört köşe; iki yanda bant, hafif eğim.
            case 34:
                Big(level, 3.9f, 7);
                level.marbles = Kind(Kind(Join(Cross(0f, 1.4f, 2, .62f), P(-1.8f, 2.6f), P(1.8f, 2.6f), P(-1.8f, .3f), P(1.8f, .3f)), MarbleKind.Magnet, 0), MarbleKind.Ice, 3);
                level.zones = Zones(Belt(-2.6f, 1.4f, .45f, 180f), Belt(2.6f, 1.4f, .45f, 0f));
                level.slope = new Vector2(.3f, 0f);
                break;

            // 36 Ustalık Sınavı — 15'lik ıstaka, ucu buzlu, iki çatapat, arkada mıknatıs; iki tampon, önde geri bant.
            case 35:
                Big(level, 4.0f, 8);
                level.marbles = Kind(Kind(Kind(Join(Rack(0f, 1.8f, 5, .6f), P(0f, 3.4f)), MarbleKind.Magnet, 15), MarbleKind.Ice, 0), MarbleKind.Catapat, 10, 14);
                level.obstacles = new[] { B(-1.3f, .2f), B(1.3f, .2f) };
                level.zones = Zones(Belt(0f, -1.0f, .55f, 180f));
                break;

            // ================= OYUN SOKAĞI · mıknatıs ağırlıklı; karpuz, çamur, çukur, kaldırım =================

            // 37 Ördek Havuzu — halka halinde ördekler, ortada havuz (çukur): içeri kaçan düşer. Arkada mıknatıs.
            case 36:
                Big(level, 3.7f, 6);
                level.marbles = Kind(Join(Ring(0f, 1.4f, 1.3f, 9, 0f), P(0f, 3.25f)), MarbleKind.Magnet, 9);
                level.obstacles = new[] { B(-2.2f, .6f), B(2.2f, .6f) };
                level.zones = Zones(Pit(0f, 1.4f, .5f));
                break;

            // 38 Halka Atma — üç halka, ortalarında küçük tampon.
            case 37:
                Big(level, 3.8f, 8);
                level.marbles = Join(Ring(-1.4f, 1.3f, .62f, 5, 90f), Ring(1.4f, 1.3f, .62f, 5, 90f), Ring(0f, 2.5f, .62f, 5, 90f));
                level.obstacles = new[] { B(-1.4f, 1.3f, .4f), B(1.4f, 1.3f, .4f), B(0f, 2.5f, .4f) };
                break;

            // 39 Ödül Rafı — 5×3 raf, üç karpuz; önde iki tampon, iki yanda dışarı bant.
            case 38:
                Big(level, 3.9f, 8);
                level.marbles = Kind(Grid(0f, 1.6f, 5, 3, .66f, .62f), MarbleKind.Split, 2, 7, 12);
                // Ölçüldü (2026-10-21): boydan kaldırım rafı neredeyse geçilmez yapıyordu (15'te 3, %47). İki tampon.
                level.obstacles = new[] { B(-1.0f, .3f), B(1.0f, .3f) };
                level.zones = Zones(Belt(-2.6f, 1.6f, .45f, 180f), Belt(2.6f, 1.6f, .45f, 0f));
                break;

            // 40 Güç Kulesi — iki sütunlu kule, tepesinde mıknatıs; iki tampon; zemin atıcıya yatık.
            case 39:
                Big(level, 3.9f, 7);
                level.marbles = Kind(Join(Grid(0f, 1.7f, 2, 5, .62f, .62f), P(0f, 3.5f)), MarbleKind.Magnet, 10);
                level.obstacles = new[] { B(-1.4f, 1.2f), B(1.4f, 1.2f) };
                level.slope = new Vector2(0f, -.4f);
                break;

            // 41 Kutu Devirme — kutu piramidi (bir çatapat), iki yanda ve arkada top; önde tampon, iki yanda çamur.
            case 40:
                Big(level, 3.8f, 7);
                level.marbles = Kind(Join(Rack(0f, 2.0f, 4, .64f), P(-2.0f, 1.0f), P(2.0f, 1.0f), P(0f, 3.3f)), MarbleKind.Catapat, 6);
                level.obstacles = new[] { B(0f, .3f) };
                level.zones = Zones(Mud(-2.2f, 2.4f, .45f), Mud(2.2f, 2.4f, .45f));
                break;

            // 42 Peluş Ayı — dağınık oyuncaklar, ortada mıknatıs, iki karpuz; önde çukur.
            case 41:
                Big(level, 3.9f, 7);
                level.marbles = Kind(Kind(Spots(-1.8f, .6f, -.7f, .8f, .6f, .7f, 1.7f, .9f, -1.3f, 1.6f, 0f, 1.7f, 1.2f, 1.7f,
                                                -2.1f, 2.3f, -.6f, 2.5f, .6f, 2.5f, 2.0f, 2.4f, -1.2f, 3.0f, 1.0f, 3.1f), MarbleKind.Magnet, 5), MarbleKind.Split, 0, 3);
                level.zones = Zones(Pit(0f, -.3f, .4f));
                break;

            // 43 Pençe Makinesi — kutunun içinde oyuncaklar (üç yanı alçak kaldırım), arkada mıknatıs pençe.
            case 42:
                Big(level, 3.9f, 7);
                level.marbles = Kind(Join(Grid(0f, 2.0f, 4, 3, .62f, .62f), P(0f, 3.3f)), MarbleKind.Magnet, 12);
                level.obstacles = new[] { O(-1.6f, 2.0f, .3f, 1.6f), O(1.6f, 2.0f, .3f, 1.6f), O(0f, .75f, 2.0f, .3f) };
                break;

            // 44 Büyük Ödül — KESEM BÖLÜMÜ: elmas, göbeği mıknatıs, ucu karpuz; iki yanda çamur, önde çukur.
            case 43:
                Big(level, 3.8f, 5);
                level.marbles = Kind(Kind(Diamond(0f, 1.8f, .64f), MarbleKind.Magnet, 6), MarbleKind.Split, 0);
                level.zones = Zones(Mud(-2.3f, 1.8f, .5f), Mud(2.3f, 1.8f, .5f), Pit(0f, -.2f, .45f));
                break;

            // 45 Langırt — dört tampon arasında misketler; önde ileri bant atışı hızlandırır.
            case 44:
                Big(level, 3.8f, 6);
                level.marbles = Spots(-.6f, 1.5f, .6f, 1.5f, 0f, .9f, -1.8f, 1.9f, 1.8f, 1.9f, -.6f, 2.5f, .6f, 2.5f, 0f, 3.0f, -1.2f, 2.8f, 1.2f, 2.8f);
                level.obstacles = new[] { B(-1.2f, 1.2f), B(0f, 2.0f), B(1.2f, 1.2f) };
                level.zones = Zones(Belt(0f, -.4f, .5f, 90f));
                break;

            // 46 Kapanış — nefes: kenara yakın yay, ortada ileri bant.
            case 45:
                Big(level, 3.7f, 6);
                level.marbles = ArcAt(0f, 0f, 2.8f, 20f, 160f, 11);
                level.zones = Zones(Belt(0f, 1.2f, .6f, 90f));
                break;

            // 47 Oyun Buluşması — büyük halka, göbeği mıknatıs, iki karpuz; iki tampon, önde çamur.
            case 46:
                Big(level, 3.9f, 7);
                level.marbles = Kind(Kind(Join(Ring(0f, 1.5f, 1.7f, 12, 15f), P(0f, 1.5f)), MarbleKind.Magnet, 12), MarbleKind.Split, 2, 8);
                level.obstacles = new[] { B(-2.5f, 0f), B(2.5f, 0f) };
                level.zones = Zones(Mud(0f, -.6f, .45f));
                break;

            // 48 Ustalık Sınavı — 5×3 blok, arkada mıknatıs; karpuz, buz; önde kaldırım, iki yanda çukur.
            case 47:
                Big(level, 4.0f, 8);
                level.marbles = Kind(Kind(Kind(Join(Grid(0f, 1.8f, 5, 3, .64f, .64f), P(0f, 3.2f)), MarbleKind.Magnet, 15), MarbleKind.Split, 2, 12), MarbleKind.Ice, 7);
                level.obstacles = new[] { O(0f, .6f, 2.0f, .3f) };
                level.zones = Zones(Pit(-2.4f, 1.0f, .4f), Pit(2.4f, 1.0f, .4f));
                break;

            // ================= SİRK ÇADIRI · her şey karışık, büyük final =================

            // 49 Çadır Kapısı — ıstaka ve iki yan misket; çatapat, iki tampon, önde rögar.
            case 48:
                Big(level, 3.7f, 7);
                level.marbles = Kind(Join(Rack(0f, 1.6f, 4, .62f), P(-2f, 1f), P(2f, 1f)), MarbleKind.Catapat, 4);
                level.obstacles = new[] { B(-1.0f, .3f), B(1.0f, .3f) };
                level.zones = Zones(Slick(0f, -.8f, .55f));
                break;

            // 50 Palyaço — iki halka (burun mıknatıs), iki karpuz; önde çamur.
            case 49:
                Big(level, 3.8f, 8);
                level.marbles = Kind(Kind(Join(Ring(0f, 1.3f, 1.0f, 6, 0f), Ring(0f, 1.3f, 2.0f, 8, 22.5f), P(0f, 1.3f)), MarbleKind.Magnet, 14), MarbleKind.Split, 1, 4);
                level.zones = Zones(Mud(0f, -.9f, .45f));
                break;

            // 51 Cambaz İpi — ip boyunca dizi (buzlu cambaz), zemin yana yatık; iki tampon.
            case 50:
                Big(level, 3.8f, 7);
                level.marbles = Kind(Join(Line(-2.4f, 1.8f, 2.4f, 1.8f, 8), P(-1.2f, 2.8f), P(1.2f, 2.8f), P(0f, 3.0f)), MarbleKind.Ice, 3);
                level.obstacles = new[] { B(-1.2f, .6f), B(1.2f, .6f) };
                level.slope = new Vector2(.6f, 0f);
                break;

            // 52 Hokkabaz — üç top kümesi (ortaları çatapat), ortada mıknatıs; önde yana bant.
            case 51:
                Big(level, 3.7f, 7);
                level.marbles = Kind(Kind(Join(Tri(-1.4f, 1.0f, .62f), Tri(1.4f, 1.0f, .62f), Tri(0f, 2.4f, .62f), P(0f, 1.2f), P(-1.6f, 2.6f), P(1.6f, 2.6f)), MarbleKind.Magnet, 9), MarbleKind.Catapat, 0, 3, 6);
                level.zones = Zones(Belt(0f, -.5f, .5f, 0f));
                break;

            // 53 Tek Teker — tekerlek (iki buzlu), göbeği büyük tampon; arkada üç misket, önde rögar.
            case 52:
                Big(level, 4.0f, 7);
                level.marbles = Kind(Join(Ring(0f, 1.5f, 1.4f, 10, 0f), Row(3.45f, -1.0f, 1.0f, 3)), MarbleKind.Ice, 0, 5);
                level.obstacles = new[] { B(0f, 1.5f, .8f) };
                level.zones = Zones(Slick(0f, -.7f, .5f));
                break;

            // 54 Aslan Kafesi — kafes (üç yanı kaldırım), içinde 3×3, kapıda mıknatıs; dışarıda iki misket.
            case 53:
                Big(level, 3.8f, 7);
                level.marbles = Kind(Join(Grid(0f, 1.7f, 3, 3, .66f, .66f), P(0f, .3f), P(-2.4f, 1.0f), P(2.4f, 1.0f)), MarbleKind.Magnet, 9);
                level.obstacles = new[] { O(-1.5f, 1.7f, .3f, 2.0f), O(1.5f, 1.7f, .3f, 2.0f), O(0f, 3.0f, 2.6f, .3f) };
                break;

            // 55 Davul Sesi — iki davul (göbekleri çatapat); arkada çukur, önde ileri bant.
            case 54:
                Big(level, 3.7f, 7);
                level.marbles = Kind(Join(Ring(-1.3f, 1.5f, .7f, 6, 0f), P(-1.3f, 1.5f), Ring(1.3f, 1.5f, .7f, 6, 30f), P(1.3f, 1.5f)), MarbleKind.Catapat, 6, 13);
                level.zones = Zones(Pit(0f, 2.8f, .45f), Belt(0f, 0f, .5f, 90f));
                break;

            // 56 Işık Hüzmesi — KESEM BÖLÜMÜ: çarpı, göbeği ve tepesi mıknatıs, iki buzlu; iki tampon, çamur.
            case 55:
                Big(level, 4.0f, 5);
                level.marbles = Kind(Kind(Join(XShape(0f, 1.6f, 3, .68f), P(0f, 3.3f)), MarbleKind.Magnet, 0, 13), MarbleKind.Ice, 5, 6);
                level.obstacles = new[] { B(-1.0f, -.3f), B(1.0f, -.3f) };
                level.zones = Zones(Mud(-2.4f, 2.6f, .45f));
                break;

            // 57 Akrobat — insan piramidi (tepesi karpuz, ucu çatapat), iki yanda dışarı bant, önde tampon.
            case 56:
                Big(level, 3.8f, 7);
                level.marbles = Kind(Kind(Join(Rack(0f, 1.8f, 4, .62f), P(-1.6f, 3.0f), P(1.6f, 3.0f)), MarbleKind.Split, 9), MarbleKind.Catapat, 0);
                level.obstacles = new[] { B(0f, .3f) };
                level.zones = Zones(Belt(-2.3f, 1.8f, .5f, 180f), Belt(2.3f, 1.8f, .5f, 0f));
                break;

            // 58 Son Gösteri — nefes: kenara yakın yay, ortada tampon.
            case 57:
                Big(level, 3.7f, 6);
                level.marbles = ArcAt(0f, 0f, 2.8f, 25f, 155f, 10);
                level.obstacles = new[] { B(0f, 1.2f) };
                break;

            // 59 Sirk Buluşması — elmas: göbek mıknatıs, iki buz, bir karpuz; iki tampon, geri bant, çamur.
            case 58:
                Big(level, 4.0f, 8);
                level.marbles = Kind(Kind(Kind(Diamond(0f, 1.8f, .64f), MarbleKind.Magnet, 6), MarbleKind.Ice, 2, 10), MarbleKind.Split, 4);
                level.obstacles = new[] { B(-2.2f, .6f), B(2.2f, .6f) };
                level.zones = Zones(Belt(0f, -.5f, .5f, 180f), Mud(-2.4f, 2.8f, .45f));
                break;

            // 60 Ustalık Sınavı — oyunun en zoru: 15'lik ıstaka, iki yanda mıknatıs, buz, karpuz, iki çatapat;
            // iki tampon, önde geri bant, arkada çukur.
            case 59:
                Big(level, 4.0f, 9);
                level.marbles = Kind(Kind(Kind(Kind(Join(Rack(0f, 1.9f, 5, .6f), P(-2.4f, 1.6f), P(2.4f, 1.6f)), MarbleKind.Magnet, 15, 16), MarbleKind.Catapat, 4, 10), MarbleKind.Ice, 0), MarbleKind.Split, 12);
                level.obstacles = new[] { B(-1.2f, .2f), B(1.2f, .2f) };
                level.zones = Zones(Belt(0f, -1.1f, .5f, 180f), Pit(-2.2f, 3.0f, .4f));
                break;
        }
        if (pendingAuto) { AutoStars(level); pendingAuto = false; }
        Tune(level, index);
        Vary(level, index);
    }

    // ÇEŞİTLİLİK: dizilimi bozmadan küçük döndürme (derece) + kaydırma (x, z). Eski bölümlerin
    // hiçbirine 0.30'dan fazla benzemesin (MemleketLayoutVerify). LunaparkVarietyTuner hesaplar.
    public static System.Func<int, Vector3> VariationOverride;
    private static readonly Dictionary<int, Vector3> Variation = new Dictionary<int, Vector3>
    {
        { 1, new Vector3(0f, 0f, -0.15f) }, { 3, new Vector3(30f, 0.15f, 0f) }, { 6, new Vector3(170f, 0f, 0.6f) }, { 9, new Vector3(-10f, 0.3f, -0.15f) },
        { 0, new Vector3(-20f, 0f, 0f) }, { 2, new Vector3(0f, -.15f, 0f) },
        { 4, new Vector3(180f, 0f, .3f) }, { 5, new Vector3(0f, -.15f, -.15f) },
        { 8, new Vector3(-30f, 0f, 0f) }, 
        { 10, new Vector3(0f, 0f, .3f) }, { 11, new Vector3(0f, -.45f, .15f) }, { 12, new Vector3(180f, 0f, .15f) },
        { 13, new Vector3(0f, 0f, .3f) }, { 14, new Vector3(170f, -.6f, 0f) }, { 15, new Vector3(170f, 0f, 0f) },
        { 16, new Vector3(0f, 0f, .3f) }, { 17, new Vector3(-10f, 0f, 0f) }, { 18, new Vector3(0f, 0f, .15f) },
        { 19, new Vector3(180f, -.15f, -.15f) }, { 21, new Vector3(-30f, 0f, 0f) }, { 22, new Vector3(-20f, 0f, 0f) },
        { 23, new Vector3(0f, 0f, .3f) }, { 24, new Vector3(0f, 0f, .3f) }, { 26, new Vector3(-30f, -.3f, 0f) },
        { 27, new Vector3(-30f, -.15f, 0f) }, { 28, new Vector3(-30f, 0f, -.3f) }, { 29, new Vector3(0f, 0f, .45f) },
        { 30, new Vector3(-20f, 0f, 0f) }, { 31, new Vector3(170f, -.45f, .45f) }, { 32, new Vector3(-10f, .15f, .15f) },
        { 33, new Vector3(0f, 0f, .15f) }, { 34, new Vector3(0f, 0f, .15f) }, { 35, new Vector3(170f, -.45f, 0f) },
        { 36, new Vector3(10f, -.15f, 0f) }, { 38, new Vector3(-10f, 0f, .6f) }, { 39, new Vector3(-30f, 0f, 0f) },
        { 40, new Vector3(180f, 0f, 0f) }, { 41, new Vector3(0f, 0f, .15f) }, { 42, new Vector3(0f, -.3f, .15f) },
        { 43, new Vector3(-10f, .6f, 0f) }, { 44, new Vector3(0f, 0f, .15f) }, { 45, new Vector3(-10f, .3f, 0f) },
        { 46, new Vector3(-10f, .15f, .15f) }, { 47, new Vector3(190f, .15f, 0f) }, { 48, new Vector3(170f, 0f, 0f) },
        { 50, new Vector3(-10f, 0f, 0f) }, { 51, new Vector3(-10f, 0f, 0f) }, { 52, new Vector3(-20f, 0f, 0f) },
        { 53, new Vector3(-10f, .15f, -.3f) }, { 54, new Vector3(-20f, 0f, .15f) }, { 55, new Vector3(0f, -.15f, 0f) },
        { 56, new Vector3(-30f, 0f, -.45f) }, { 57, new Vector3(0f, -.15f, .3f) }, { 58, new Vector3(-30f, .45f, -.3f) },
        { 59, new Vector3(-20f, .15f, 0f) },
    };

    public static Vector3 CurrentVariation(int index) => Variation.TryGetValue(index, out var v) ? v : Vector3.zero;

    private static void Vary(LevelData level, int index)
    {
        Vector3 v;
        if (VariationOverride != null) v = VariationOverride(index);
        else if (!Variation.TryGetValue(index, out v)) return;
        if (v == Vector3.zero) return;
        var rot = Quaternion.Euler(0f, -v.x, 0f);
        Vector2 Move(float x, float z) { var p = rot * new Vector3(x, 0f, z); return new Vector2(p.x + v.y, p.z + v.z); }
        if (level.marbles != null)
            for (int i = 0; i < level.marbles.Length; i++)
            { var p = Move(level.marbles[i].x, level.marbles[i].z); level.marbles[i].x = p.x; level.marbles[i].z = p.y; }
        if (level.obstacles != null)
            for (int i = 0; i < level.obstacles.Length; i++)
            { var p = Move(level.obstacles[i].x, level.obstacles[i].z); level.obstacles[i].x = p.x; level.obstacles[i].z = p.y; level.obstacles[i].angle -= v.x; }
        if (level.zones != null)
            for (int i = 0; i < level.zones.Length; i++)
            { var p = Move(level.zones[i].x, level.zones[i].z); level.zones[i].x = p.x; level.zones[i].z = p.y; level.zones[i].angle -= v.x; }
        if (level.slope.sqrMagnitude > 0f)
        { var sl = rot * new Vector3(level.slope.x, 0f, level.slope.y); level.slope = new Vector2(sl.x, sl.z); }
    }

    // ATIŞ HAKKI VE YILDIZLAR: sıradan oyuncu ölçümünden (2026-10-21, Logs/Luna_r*.tsv, Luna_fix*.tsv;
    // tablo: Claude outputs/LUNAPARK_TABLO.md). Satır: { atış, 1y, 2y, 3y }.
    // Tabloda olmayan bölüm Big() başlangıç değerinde kalır (1y %35, 2y %60, 3y hepsi).
    private static readonly Dictionary<int, int[]> Tuning = new Dictionary<int, int[]>
    {
        { 0, new[] { 9, 6, 7, 8 } },
        { 1, new[] { 8, 5, 6, 7 } },
        { 2, new[] { 8, 6, 7, 8 } },
        { 3, new[] { 8, 5, 6, 7 } },
        { 4, new[] { 9, 7, 8, 9 } },
        { 5, new[] { 9, 6, 7, 8 } },
        { 6, new[] { 9, 7, 8, 9 } },
        { 7, new[] { 8, 5, 7, 8 } },
        { 8, new[] { 9, 7, 9, 10 } },
        { 9, new[] { 8, 6, 7, 8 } },
        { 10, new[] { 10, 8, 9, 10 } },
        { 11, new[] { 11, 9, 11, 12 } },
        { 12, new[] { 7, 8, 9, 10 } },
        { 13, new[] { 6, 5, 6, 10 } },
        { 14, new[] { 5, 9, 10, 11 } },
        { 15, new[] { 6, 9, 10, 11 } },
        { 16, new[] { 8, 7, 9, 11 } },
        { 17, new[] { 7, 5, 7, 8 } },
        { 18, new[] { 6, 10, 11, 12 } },
        { 19, new[] { 6, 11, 13, 15 } },
        { 20, new[] { 7, 10, 11, 12 } },
        { 21, new[] { 6, 8, 9, 10 } },
        { 22, new[] { 6, 11, 12, 13 } },
        { 23, new[] { 9, 8, 13, 15 } },
        { 24, new[] { 7, 8, 9, 10 } },
        { 25, new[] { 7, 12, 13, 14 } },
        { 26, new[] { 7, 5, 6, 8 } },
        { 27, new[] { 5, 9, 10, 11 } },
        { 28, new[] { 7, 8, 9, 10 } },
        { 29, new[] { 5, 8, 9, 10 } },
        { 30, new[] { 7, 10, 11, 12 } },
        { 31, new[] { 5, 8, 10, 13 } },
        { 32, new[] { 5, 11, 12, 13 } },
        { 33, new[] { 6, 9, 10, 11 } },
        { 34, new[] { 7, 11, 12, 13 } },
        { 35, new[] { 8, 9, 15, 16 } },
        { 36, new[] { 4, 7, 8, 10 } },
        { 37, new[] { 8, 12, 13, 14 } },
        { 38, new[] { 9, 10, 12, 13 } },
        { 39, new[] { 6, 9, 10, 11 } },
        { 40, new[] { 7, 9, 10, 11 } },
        { 41, new[] { 5, 9, 11, 12 } },
        { 42, new[] { 8, 5, 6, 7 } },
        { 43, new[] { 5, 7, 9, 13 } },
        { 44, new[] { 6, 8, 9, 10 } },
        { 45, new[] { 6, 9, 10, 11 } },
        { 46, new[] { 7, 11, 12, 13 } },
        { 47, new[] { 8, 6, 10, 11 } },
        { 48, new[] { 4, 10, 11, 12 } },
        { 49, new[] { 8, 12, 14, 15 } },
        { 50, new[] { 8, 9, 10, 11 } },
        { 51, new[] { 3, 10, 11, 12 } },
        { 52, new[] { 8, 9, 10, 11 } },
        { 53, new[] { 7, 6, 7, 8 } },
        { 54, new[] { 4, 11, 12, 13 } },
        { 55, new[] { 5, 8, 10, 14 } },
        { 56, new[] { 5, 10, 11, 12 } },
        { 57, new[] { 6, 8, 9, 10 } },
        { 58, new[] { 9, 6, 8, 9 } },
        { 59, new[] { 8, 10, 16, 17 } },
    };

    private static void Tune(LevelData level, int index)
    {
        if (!Tuning.TryGetValue(index, out var t)) return;
        level.shotCount = t[0];
        if (t.Length < 4) return;
        level.oneStarTarget = t[1]; level.twoStarTarget = t[2]; level.threeStarTarget = t[3];
    }

    private static void Big(LevelData level, float size, int shots)
    {
        Set(level, size, shots, 1, 2, 3);
        pendingAuto = true;
    }
    private static bool pendingAuto;
    private static void AutoStars(LevelData level)
    {
        int n = level.TotalMarbles();
        level.threeStarTarget = n;
        level.oneStarTarget = Mathf.Max(1, Mathf.CeilToInt(n * .35f));
        level.twoStarTarget = Mathf.Clamp(Mathf.CeilToInt(n * .6f), level.oneStarTarget + 1, n - 1);
        if (level.starsToPass >= 2) level.oneStarTarget = Mathf.Min(level.oneStarTarget, level.twoStarTarget - 1);
    }

    // ---------------------------------------------------------------
    // Yardımcılar (SehirBook ile aynı biçim)

    private static void Set(LevelData level, float size, int shots, int one, int two, int three)
    {
        level.shape = ArenaShape.Circle;
        level.arenaSize = size;
        level.shotCount = shots;
        level.starsToPass = level.mastery ? 2 : 1;
        level.oneStarTarget = one;
        level.twoStarTarget = two;
        level.threeStarTarget = three;
        level.marbles = null;
        level.obstacles = null;
        level.zones = null;
        level.slope = Vector2.zero;
        level.obstacleCount = 0;
        level.shooterHalfWidth = 0f;
        level.shooterOffsetX = 0f;
        level.shooterStartPosition = new Vector3(0f, .25f, -4.2f);
    }

    private const float BumperSize = .6f;
    private static ObstacleSpot B(float x, float z, float d = BumperSize) => ObstacleSpot.Bumper(x, z, d);
    private static ObstacleSpot O(float x, float z, float w, float d, float angle = 0f) => new ObstacleSpot(x, z, w, d, angle);
    private static ZoneSpot Belt(float x, float z, float r, float angle) => new ZoneSpot(ZoneKind.Belt, x, z, r, angle);
    private static ZoneSpot Slick(float x, float z, float r) => new ZoneSpot(ZoneKind.Slick, x, z, r);
    private static ZoneSpot Sand(float x, float z, float r) => new ZoneSpot(ZoneKind.Sand, x, z, r);
    private static ZoneSpot Mud(float x, float z, float r) => new ZoneSpot(ZoneKind.Mud, x, z, r);
    private static ZoneSpot Pit(float x, float z, float r) => new ZoneSpot(ZoneKind.Pit, x, z, r);
    private static ZoneSpot[] Zones(params ZoneSpot[] z) => z;

    private static MarbleSpot P(float x, float z) => new MarbleSpot(x, z);

    private static MarbleSpot[] Kind(MarbleSpot[] spots, MarbleKind kind, params int[] which)
    {
        var copy = (MarbleSpot[])spots.Clone();
        if (which == null || which.Length == 0) for (int i = 0; i < copy.Length; i++) copy[i].kind = kind;
        else foreach (int i in which) if (i >= 0 && i < copy.Length) copy[i].kind = kind;
        return copy;
    }

    private static MarbleSpot[] Join(params object[] parts)
    {
        var list = new List<MarbleSpot>();
        foreach (var p in parts)
        {
            if (p is MarbleSpot s) list.Add(s);
            else if (p is MarbleSpot[] arr) list.AddRange(arr);
        }
        return list.ToArray();
    }

    private static MarbleSpot[] Row(float z, float x0, float x1, int n) => Line(x0, z, x1, z, n);

    private static MarbleSpot[] Line(float x0, float z0, float x1, float z1, int n)
    {
        var list = new MarbleSpot[n];
        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? .5f : i / (float)(n - 1);
            list[i] = new MarbleSpot(Mathf.Lerp(x0, x1, t), Mathf.Lerp(z0, z1, t));
        }
        return list;
    }

    private static MarbleSpot[] Ring(float cx, float cz, float r, int n, float phase)
    {
        var list = new MarbleSpot[n];
        for (int i = 0; i < n; i++)
        {
            float a = (phase + i * 360f / n) * Mathf.Deg2Rad;
            list[i] = new MarbleSpot(cx + Mathf.Cos(a) * r, cz + Mathf.Sin(a) * r);
        }
        return list;
    }

    private static MarbleSpot[] ArcAt(float cx, float cz, float r, float from, float to, int n)
    {
        var list = new MarbleSpot[n];
        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? .5f : i / (float)(n - 1);
            float a = Mathf.Lerp(from, to, t) * Mathf.Deg2Rad;
            list[i] = new MarbleSpot(cx + Mathf.Cos(a) * r, cz + Mathf.Sin(a) * r);
        }
        return list;
    }

    private static MarbleSpot[] Spots(params float[] pairs)
    {
        var list = new MarbleSpot[pairs.Length / 2];
        for (int i = 0; i < list.Length; i++) list[i] = new MarbleSpot(pairs[i * 2], pairs[i * 2 + 1]);
        return list;
    }

    private static MarbleSpot[] Tri(float cx, float cz, float gap)
    {
        float h = gap * .866f;
        return new[] { P(cx, cz - h * .66f), P(cx - gap * .5f, cz + h * .34f), P(cx + gap * .5f, cz + h * .34f) };
    }

    private static MarbleSpot[] Wave(float x0, float x1, int n, float z0, float amp, float freq)
    {
        var list = new MarbleSpot[n];
        for (int i = 0; i < n; i++) { float x = Mathf.Lerp(x0, x1, i / (float)(n - 1)); list[i] = P(x, z0 + amp * Mathf.Sin(x * freq)); }
        return list;
    }

    private static MarbleSpot[] Zig(float x0, float x1, int n, float z0, float z1)
    {
        var list = new MarbleSpot[n];
        for (int i = 0; i < n; i++) list[i] = P(Mathf.Lerp(x0, x1, i / (float)(n - 1)), i % 2 == 0 ? z0 : z1);
        return list;
    }

    private static MarbleSpot[] Cross(float cx, float cz, int n, float gap)
    {
        var list = new List<MarbleSpot> { P(cx, cz) };
        for (int k = 1; k <= n; k++) { list.Add(P(cx + k * gap, cz)); list.Add(P(cx - k * gap, cz)); list.Add(P(cx, cz + k * gap)); list.Add(P(cx, cz - k * gap)); }
        return list.ToArray();
    }

    private static MarbleSpot[] XShape(float cx, float cz, int n, float gap)
    {
        var list = new List<MarbleSpot> { P(cx, cz) };
        float d = gap * .7071f;
        for (int k = 1; k <= n; k++) { list.Add(P(cx + k * d, cz + k * d)); list.Add(P(cx - k * d, cz + k * d)); list.Add(P(cx + k * d, cz - k * d)); list.Add(P(cx - k * d, cz - k * d)); }
        return list.ToArray();
    }

    private static MarbleSpot[] Diamond(float cx, float cz, float gap)
    {
        var list = new List<MarbleSpot>();
        int[] rows = { 1, 3, 5, 3, 1 };
        for (int r = 0; r < rows.Length; r++)
            for (int i = 0; i < rows[r]; i++)
                list.Add(P(cx + (i - (rows[r] - 1) * .5f) * gap, cz + (r - 2) * gap * .87f));
        return list.ToArray();
    }

    private static MarbleSpot[] Rack(float cx, float cz, int rows, float gap)
    {
        var list = new List<MarbleSpot>();
        float rowStep = gap * .87f;
        float z0 = cz - (rows - 1) * rowStep * .5f;
        for (int r = 0; r < rows; r++)
            for (int i = 0; i <= r; i++)
                list.Add(P(cx + (i - r * .5f) * gap, z0 + r * rowStep));
        return list.ToArray();
    }

    private static MarbleSpot[] Grid(float cx, float cz, int cols, int rows, float dx, float dz)
    {
        var list = new List<MarbleSpot>();
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                list.Add(P(cx + (c - (cols - 1) * .5f) * dx, cz + (r - (rows - 1) * .5f) * dz));
        return list.ToArray();
    }
}
