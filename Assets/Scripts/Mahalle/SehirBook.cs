using System.Collections.Generic;
using UnityEngine;

// HARİTA 3 — ŞEHİR: elle tasarlanmış bölümler (MemleketBook'un Şehir karşılığı).
// Konumlar çemberin merkezine göre: x sağa, z ileri (atıcıdan uzağa). Atıcı z=-4.2.
//
// 2026-10-21 (kullanıcı): Şehir Merkezi'nin 12 bölümünde de 3 yıldız Kesem'siz kolay → atış hakları +3; 1-2. bölümde
// kaldırım misketlerin önüne alındı (özellik süs olmasın).
// Kural (kullanıcı, 2026-10-19): kolay başlasın, ilk atışlar bol misket çıkarsın (dopamin),
// yavaş yavaş zorlaşsın; her bölümde 1, 2, 3 yıldız AYRI alınabilsin.
// Yeni mekanikler 1. bölümden (kullanıcı: düz ısınma bölümü olmasın): 1. bölüm ALÇAK kaldırım
// (yavaş misket takılır, sert atış üstünden aşar — SehirCampaign.CurbHeight), 3. bölüm rögar kapağı (kaygan zemin), 5. bölüm çatapat (vurunca patlar,
// çevresini savurur), sonra karışık.
// Atış hakkı ve yıldız hedefleri sıradan oyuncu modeliyle ölçüldü (MemleketPhysicsVerify.ScheduleHuman,
// 2026-10-19), tek denemede geçme / 2 yıldız / 3 yıldız:
// Alçak kaldırımla (10 cm) son ölçüm:
//   01 %100/100/87 · 02 %100/92/65 · 03 %100/98/55 · 04 %100/83/65 · 05 %100/97/53 · 06 %100/83/48
//   07 %100/95/18 · 08 %100/67/5 · 09 %95/45/3 · 10 %97/77/27 · 11 %93/35/2 · 12 (ustalık, 2★ geçer) %27/0
// 8-9 ve 11-12'de 3 yıldız güç (Kesem) ister: bilerek. Çatapat güçlü çıktı, atış hakları ona göre kısıldı.
public static class SehirBook
{
    public static void Apply(LevelData level, int index)
    {
        switch (index)
        {
            // ================= ŞEHİR MERKEZİ =================

            // 01 Otobüs Durağı — YENİ: kaldırım taşı. Durağın önünde ortası açık iki kaldırım (kapı): düz
            // atış aradan geçer; yana kaçan misket kaldırıma çarpıp geri döner. (Kullanıcı 2026-10-21:
            // "duvarla alakası yok, sağda duvar" — kaldırım eskiden kenardaydı. Boydan kaldırım ölçüldü:
            // geçme %40, fazla zor.)
            case 0:
                Set(level, 3.0f, 5, 2, 4, 6);
                level.marbles = Join(Rack(0f, 1.6f, 3, .62f), P(-1.9f, .4f), P(1.9f, .4f));
                level.obstacles = new[] { O(-.75f, .45f, .7f, .3f), O(.75f, .45f, .7f, .3f) };
                break;

            // 02 Trafik Lambası — iki direk, üçer lamba; iki yanda kaldırım: yana kaçan misket geri seker.
            // (Önlere kaldırım denendi, 2026-10-21: 3 yıldız %15'e düştü; eski hali.)
            case 1:
                Set(level, 3.0f, 4, 2, 5, 6);
                level.marbles = Join(Line(-.45f, .9f, -.45f, 2.2f, 3), Line(.45f, .9f, .45f, 2.2f, 3));
                level.obstacles = new[] { O(-1.55f, 1.4f, .3f, 1.2f), O(1.55f, 1.4f, .3f, 1.2f) };
                break;

            // 03 Site Kapısı — YENİ: rögar kapağı. İki kapı direği rögarın üstünde: vurulan misket
            // kayıp gider.
            case 2:
                Set(level, 3.2f, 4, 3, 6, 8);
                level.marbles = Join(Grid(-1.35f, 1.3f, 2, 2, .62f, .62f), Grid(1.35f, 1.3f, 2, 2, .62f, .62f));
                level.zones = Zones(Slick(-1.35f, 1.3f, .75f), Slick(1.35f, 1.3f, .75f));
                break;

            // 04 Kaldırım Taşı — taş ortayı kapatıyor; yanlardan ya da taştan sektirerek arkadaki
            // yaya ulaş.
            case 3:
                Set(level, 3.2f, 5, 3, 6, 7);
                level.marbles = Join(ArcAt(0f, .9f, 1.5f, 40f, 140f, 5), P(-2.2f, .2f), P(2.2f, .2f));
                level.obstacles = new[] { O(0f, -.4f, 1.6f, .35f) };
                // İki yandaki misket rögarın üstünde: hafif vuruşla bile kayıp gider (2026-10-21: rögar eskiden
                // boş köşedeydi, işe yaramıyordu).
                level.zones = Zones(Slick(-2.2f, .2f, .5f), Slick(2.2f, .2f, .5f));
                break;

            // 05 Yaya Geçidi — YENİ: çatapat. Zebra gibi üç sıra, ortadaki çatapat: ona vuran
            // bütün geçidi dağıtır.
            case 4:
                Set(level, 3.3f, 4, 4, 7, 9);
                level.marbles = Kind(Grid(0f, 1.1f, 3, 3, .9f, .72f), MarbleKind.Catapat, 4);
                break;

            // 06 Metro Çıkışı — kalabalık kapıdan dağılıyor: misketler kenara yakın, nefes bölümü.
            // Ortadaki ikili rögarın üstünde.
            case 5:
                Set(level, 3.2f, 5, 4, 7, 8);
                level.marbles = Join(ArcAt(0f, 0f, 2.35f, 25f, 155f, 6), P(-.4f, 1.2f), P(.4f, 1.2f));
                level.zones = Zones(Slick(0f, 1.2f, .7f));
                break;

            // 07 Rögar Kapağı — büyük kapak ortada; üstündeki misketler hafif vuruşla bile uzağa kayar.
            case 6:
                Set(level, 3.3f, 4, 4, 6, 8);
                level.marbles = Join(Ring(0f, .9f, 1.2f, 7, 90f), P(0f, .9f));
                level.zones = Zones(Slick(0f, .9f, .85f));
                break;

            // 08 Otopark — iki park sırası, aralarında kaldırım; girişte rögar hızlandırır.
            case 7:
                Set(level, 3.5f, 7, 5, 8, 10);
                level.marbles = Kind(Join(Row(.35f, -1.8f, 1.8f, 5), Row(2.05f, -1.8f, 1.8f, 5)), MarbleKind.Catapat, 7);
                // Ölçüldü: tek parça kaldırım arka sırayı kapatıyordu (2 yıldız %0). Ortası açıldı.
                level.obstacles = new[] { O(-1.55f, 1.2f, 1.1f, .3f), O(1.55f, 1.2f, 1.1f, .3f) };
                level.zones = Zones(Slick(0f, -1.5f, .75f));
                break;

            // 09 Alt Geçit — eğik tünel: iki duvar arasında dizi, dışarıda iki küme.
            case 8:
                Set(level, 3.5f, 7, 6, 8, 10);
                // Ölçüldü: dar tünelde 2 yıldız %2'ydi. Duvarlar kısaltıldı, tünel genişledi.
                level.marbles = Kind(Join(Line(-.9f, -.1f, .7f, 1.9f, 4), Tri(-1.9f, 1.7f, .62f), P(2.1f, -.1f), P(2.35f, .55f), P(1.9f, 1.15f)), MarbleKind.Catapat, 3);
                level.obstacles = new[] { O(.37f, .53f, .3f, 1.5f, 38f), O(-.57f, 1.27f, .3f, 1.5f, 38f) };
                break;

            // 10 Gökdelen Önü — iki sütunlu kule; yanlarda kaldırım, önde rögar. Bölgenin ilk sınavı.
            case 9:
                Set(level, 3.6f, 6, 7, 10, 11);
                level.marbles = Kind(Join(Grid(0f, 1.25f, 2, 5, .66f, .62f), P(0f, 3.0f)), MarbleKind.Catapat, 4);
                level.obstacles = new[] { O(-1.75f, .9f, .35f, 1.5f), O(1.75f, .9f, .35f, 1.5f) };
                level.zones = Zones(Slick(0f, -1.2f, .8f));
                break;

            // 11 Şehir Buluşması — büyük halka, ortada çatapatlı üçlü; önde iki alçak kaldırım,
            // girişte rögar. Bölgenin bütün özellikleri bir arada.
            case 10:
                Set(level, 3.7f, 6, 6, 9, 11);
                level.marbles = Join(Ring(0f, .9f, 1.7f, 8, 22.5f), Kind(Tri(0f, .9f, .62f), MarbleKind.Catapat, 0));
                level.obstacles = new[] { O(-1.95f, -.75f, 1.0f, .3f, 30f), O(1.95f, -.75f, 1.0f, .3f, -30f) };
                level.zones = Zones(Slick(0f, -1.45f, .7f));
                break;

            // 12 Ustalık Sınavı — ıstaka, önünde boydan kaldırım: ancak sert atış aşar. İki yanda
            // çatapat bekler. Geçmek için 2 yıldız.
            case 11:
                Set(level, 3.8f, 7, 3, 6, 12);
                level.marbles = Join(Rack(0f, 1.45f, 4, .64f), Kind(new[] { P(-2.1f, 1.0f), P(2.1f, 1.0f) }, MarbleKind.Catapat));
                level.obstacles = new[] { O(0f, -.25f, 1.8f, .3f) };
                level.zones = Zones(Slick(-1.9f, -1.2f, .6f), Slick(1.9f, -1.2f, .6f));
                break;

            // ================= SANAYİ · yeni: kum (13), çukur (16) =================

            // 13 Sanayi Girişi — YENİ (eski): inşaat kumu. Önden geçen atış kumda yavaşlar.
            case 12:
                Big(level, 3.7f, 6);
                level.marbles = Rack(0f, 1.5f, 4, .62f);
                level.zones = Zones(Sand(0f, -.5f, .85f));
                break;

            // 14 Oto Tamirci — 4×3 blok, önde alçak kaldırım, iki yanda kum.
            case 13:
                Big(level, 3.8f, 8);
                level.marbles = Grid(0f, 1.3f, 4, 3, .7f, .7f);
                level.obstacles = new[] { O(0f, -.2f, 1.8f, .3f) };
                level.zones = Zones(Sand(-2.4f, 1.3f, .6f), Sand(2.4f, 1.3f, .6f));
                break;

            // 15 Lastikçi — iki lastik halkası; soldakinin göbeği çatapat. Arkada kum.
            case 14:
                Big(level, 3.8f, 8);
                level.marbles = Join(Ring(-1.25f, 1.0f, .7f, 6, 0f), Kind(new[] { P(-1.25f, 1.0f) }, MarbleKind.Catapat),
                                     Ring(1.25f, 1.0f, .7f, 6, 30f), P(1.25f, 1.0f));
                level.zones = Zones(Sand(0f, -.7f, .7f));
                break;

            // 16 Kaynakçı — YENİ (eski): çukur (açık rögar). Ortadaki çukur yavaş misketi yutar.
            case 15:
                Big(level, 3.7f, 6);
                level.marbles = Join(Row(.1f, -1.8f, 1.8f, 6), Row(2.3f, -1.5f, 1.5f, 5));
                level.zones = Zones(Pit(0f, 1.2f, .55f));
                break;

            // 17 Depo Önü — 3×4 kasa yığını, arka köşelerde çukur, iki yanda kaldırım.
            // (Ölçüldü: önde çukurken itilen misket hep içine düşüyordu, ort. 12'de 3 — fazla zor.)
            case 16:
                Big(level, 3.9f, 9);
                level.marbles = Grid(0f, 1.6f, 3, 4, .72f, .62f);
                level.obstacles = new[] { O(-2.0f, 1.0f, .3f, 1.4f), O(2.0f, 1.0f, .3f, 1.4f) };
                level.zones = Zones(Pit(-1.9f, 2.7f, .4f), Pit(1.9f, 2.7f, .4f));
                break;

            // 18 Forklift Yolu — iki şerit, aralarında çatapat; yanlarda kum.
            case 17:
                Big(level, 3.8f, 8);
                level.marbles = Join(Line(-.55f, 0f, -.55f, 2.8f, 5), Line(.55f, 0f, .55f, 2.8f, 5),
                                     Kind(new[] { P(0f, 1.4f) }, MarbleKind.Catapat),
                                     P(-1.9f, 1.0f), P(1.9f, 1.0f));
                level.zones = Zones(Sand(-1.9f, 2.6f, .6f), Sand(1.9f, 2.6f, .6f));
                break;

            // 19 Hurdalık — dağınık hurda yığını; önde çukur, arkada kum, ortada çatapat.
            case 18:
                Big(level, 3.9f, 9);
                level.marbles = Kind(Spots(-.4f, .8f, .3f, .4f, 0f, 1.4f, .8f, 1.2f, -.9f, 1.6f, -.2f, 2.2f, .6f, 2.4f, 1.5f, .4f,
                                           -1.8f, 2.0f, 1.6f, 2.0f, -1.4f, .9f, 1.2f, 2.9f, -.8f, 2.9f),
                                     MarbleKind.Catapat, 2);
                level.zones = Zones(Pit(-.6f, -.45f, .45f), Sand(2.2f, 2.6f, .6f));
                break;

            // 20 Vinç Altı — KESEM BÖLÜMÜ: arkada 15'lik ıstaka, iki yanda çukur, önde kaldırım.
            case 19:
                Big(level, 3.9f, 6);
                level.marbles = Rack(0f, 2.0f, 5, .6f);
                level.obstacles = new[] { O(0f, -.1f, 1.4f, .3f) };
                level.zones = Zones(Pit(-1.9f, .5f, .45f), Pit(1.9f, .5f, .45f));
                break;

            // 21 Konteyner — üç konteyner; ortadakinin altı yağlı (kaygan), aralarda kum.
            case 20:
                Big(level, 3.8f, 9);
                level.marbles = Join(Grid(-1.5f, 1.0f, 2, 2, .62f, .62f), Grid(1.5f, 1.0f, 2, 2, .62f, .62f), Grid(0f, 2.4f, 2, 2, .62f, .62f));
                level.zones = Zones(Slick(0f, 2.4f, .75f), Sand(0f, .5f, .6f));
                break;

            // 22 Yağ Lekesi — nefes: kenara yakın yay, iki yağ lekesi.
            case 21:
                Big(level, 3.7f, 6);
                level.marbles = Join(ArcAt(0f, 0f, 2.6f, 25f, 155f, 9), P(-.4f, 1.0f), P(.4f, 1.0f));
                level.zones = Zones(Slick(-1.1f, 1.1f, .6f), Slick(1.1f, 1.1f, .6f));
                break;

            // 23 Sanayi Buluşması — büyük artı, dört köşe; çukur, kum, kaldırım.
            case 22:
                Big(level, 3.9f, 9);
                level.marbles = Join(Cross(0f, 1.3f, 2, .62f), P(-1.5f, 2.6f), P(1.5f, 2.6f), P(-1.5f, 0f), P(1.5f, 0f));
                level.obstacles = new[] { O(0f, -1.3f, 1.4f, .3f) };
                level.zones = Zones(Pit(-2.6f, 1.3f, .4f), Sand(2.6f, 1.3f, .5f));
                break;

            // 24 Ustalık Sınavı — büyük çarpı, iki yanda çatapat, önde çukur ve iki eğik kaldırım.
            case 23:
                Big(level, 4.0f, 8);
                level.marbles = Join(XShape(0f, 1.4f, 3, .68f), Kind(new[] { P(-2.4f, 1.4f), P(2.4f, 1.4f) }, MarbleKind.Catapat));
                level.obstacles = new[] { O(-1.6f, -1.0f, 1.0f, .3f, 25f), O(1.6f, -1.0f, 1.0f, .3f, -25f) };
                level.zones = Zones(Pit(0f, -.9f, .45f));
                break;

            // ================= LİMAN · yeni: buz (25), eğim (28) =================

            // 25 Liman Girişi — YENİ (eski): buzlu misket (balık kasası buzu).
            case 24:
                Big(level, 3.7f, 7);
                level.marbles = Kind(Join(Ring(0f, 1.2f, 1.3f, 10, 0f), P(0f, 1.2f), P(0f, 3.0f)), MarbleKind.Ice, 1, 4, 7);
                break;

            // 26 Vapur İskelesi — üç sıra yolcu, önde ıslak zemin.
            case 25:
                Big(level, 3.9f, 9);
                level.marbles = Join(Kind(Row(.5f, -2.1f, 2.1f, 7), MarbleKind.Ice, 1, 5), Row(1.6f, -1.8f, 1.8f, 6));
                level.zones = Zones(Slick(0f, -.9f, .75f));
                break;

            // 27 Martı Yeri — beş martı kümesi, aralarında çatapat.
            case 26:
                Big(level, 3.9f, 9);
                level.marbles = Join(Kind(Tri(-1.6f, .9f, .62f), MarbleKind.Ice, 0), Tri(1.6f, .9f, .62f), Kind(Tri(0f, 2.3f, .62f), MarbleKind.Ice, 0),
                                     Tri(-1.6f, 2.6f, .62f), Kind(new[] { P(0f, 1.0f) }, MarbleKind.Catapat));
                break;

            // 28 Balıkçı Teknesi — YENİ (eski): eğim (rıhtım rampası, denize doğru).
            case 27:
                Big(level, 3.8f, 7);
                level.marbles = Grid(-.3f, 1.3f, 4, 3, .7f, .72f);
                level.slope = new Vector2(1.0f, 0f);
                break;

            // 29 Halat Yığını — halat kangalı + alt yay, yanlarda kaldırım; eğimli.
            case 28:
                Big(level, 3.9f, 8);
                level.marbles = Join(Ring(0f, 1.3f, .75f, 7, 0f), P(0f, 1.3f), ArcAt(0f, 1.3f, 1.75f, 200f, 340f, 5));
                level.obstacles = new[] { O(-2.6f, 1.5f, .3f, 1.2f), O(2.6f, 1.5f, .3f, 1.2f) };
                level.slope = new Vector2(.8f, -.2f);
                break;

            // 30 Fener Altı — buzlu fener sütunu, çevresinde yay; eğim sola.
            case 29:
                Big(level, 3.9f, 8);
                level.marbles = Join(Kind(Line(0f, .3f, 0f, 2.2f, 4), MarbleKind.Ice, 1, 2), ArcAt(0f, .9f, 2.2f, 20f, 160f, 8), P(-1.0f, 0f), P(1.0f, 0f));
                level.slope = new Vector2(-.7f, 0f);
                break;

            // 31 Rıhtım — 5×3 kasa, önde ıslak zemin, yanlarda kaldırım.
            case 30:
                Big(level, 3.9f, 9);
                level.marbles = Join(Kind(Grid(0f, 1.5f, 4, 3, .7f, .75f), MarbleKind.Ice, 1, 6, 10), P(-.7f, 3.0f), P(.7f, 3.0f));
                level.obstacles = new[] { O(-2.4f, 1.5f, .3f, 1.6f), O(2.4f, 1.5f, .3f, 1.6f) };
                level.zones = Zones(Slick(0f, -.4f, .75f));
                break;

            // 32 Gümrük — KESEM BÖLÜMÜ: elmas, göbeği ve dört uydusu buzlu; eğimli.
            case 31:
                Big(level, 4.0f, 7);
                level.marbles = Join(Kind(Diamond(0f, 1.5f, .62f), MarbleKind.Ice, 6),
                                     Kind(new[] { P(-2.2f, 1.5f), P(2.2f, 1.5f) }, MarbleKind.Ice));
                level.slope = new Vector2(.6f, 0f);
                break;

            // 33 Dalgakıran — iki dalga sırası, iki çatapat; eğim denize.
            case 32:
                Big(level, 3.9f, 9);
                level.marbles = Kind(Join(Wave(-2.4f, 2.4f, 9, 1.0f, .5f, 1.3f), Wave(-1.8f, 1.8f, 5, 2.4f, .35f, 1.3f)), MarbleKind.Catapat, 4, 11);
                level.slope = new Vector2(.9f, .2f);
                break;

            // 34 Son Vapur — nefes: kenara yakın yay.
            case 33:
                Big(level, 3.7f, 7);
                level.marbles = Join(Kind(ArcAt(0f, 0f, 2.6f, 20f, 160f, 10), MarbleKind.Ice, 5), P(-.5f, 1.2f), P(.5f, 1.2f));
                break;

            // 35 Liman Buluşması — iki halka, göbekte çatapat; eğim, ıslak zemin.
            case 34:
                Big(level, 4.0f, 9);
                level.marbles = Join(Kind(Ring(0f, 1.3f, 1.75f, 12, 0f), MarbleKind.Ice, 0, 4, 8),
                                     Kind(new[] { P(0f, 1.3f) }, MarbleKind.Catapat));
                level.zones = Zones(Slick(0f, -1.2f, .6f));
                level.slope = new Vector2(.5f, 0f);
                break;

            // 36 Ustalık Sınavı — 15'lik ıstaka (köşeleri buzlu), iki yanda çatapat, önde kaldırım; eğimli.
            case 35:
                Big(level, 4.0f, 9);
                level.marbles = Join(Kind(Rack(0f, 1.6f, 5, .62f), MarbleKind.Ice, 0, 10, 14),
                                     Kind(new[] { P(-2.4f, 1.0f), P(2.4f, 1.0f) }, MarbleKind.Catapat));
                level.obstacles = new[] { O(0f, -.4f, 1.8f, .3f) };
                level.slope = new Vector2(.6f, 0f);
                break;

            // ================= AVM · yeni: karpuz (37), çamur (40) =================

            // 37 AVM Kapısı — YENİ (eski): karpuz misket (manav reyonu).
            case 36:
                Big(level, 3.7f, 7);
                level.marbles = Join(Kind(Row(1.0f, -1.8f, 1.8f, 6), MarbleKind.Split, 2, 3), Row(2.2f, -1.5f, 1.5f, 5), P(0f, -.1f));
                break;

            // 38 Yürüyen Merdiven — iki zikzak basamak, en altta rögar.
            case 37:
                Big(level, 3.9f, 8);
                level.marbles = Join(Kind(Zig(-2.2f, 2.2f, 9, .4f, 1.2f), MarbleKind.Split, 4), Kind(Zig(-1.5f, 1.5f, 4, 2.1f, 2.9f), MarbleKind.Split, 2));
                level.zones = Zones(Slick(0f, -.9f, .6f));
                break;

            // 39 Vitrin Önü — 5×3 vitrin, ortası çatapat; önde kaldırım.
            case 38:
                Big(level, 3.9f, 9);
                level.marbles = Join(Kind(Kind(Grid(0f, 1.3f, 5, 2, .7f, .72f), MarbleKind.Split, 1, 8), MarbleKind.Catapat, 7), P(-.7f, 2.4f), P(.7f, 2.4f));
                level.obstacles = new[] { O(0f, -.3f, 2.0f, .3f) };
                break;

            // 40 Yemek Katı — YENİ (eski): çamur (dökülen ayran). Arka köşelere itilen misket saplanır.
            case 39:
                Big(level, 3.8f, 8);
                level.marbles = Join(Ring(0f, 1.3f, 1.4f, 10, 0f), P(0f, 1.3f), P(0f, 3.2f), P(-2.4f, 1.3f));
                level.zones = Zones(Mud(-1.9f, 2.7f, .55f), Mud(1.9f, 2.7f, .55f));
                break;

            // 41 Sinema Sırası — üç koltuk sırası; yanlarda çamur, aralarda karpuz.
            case 40:
                Big(level, 3.9f, 10);
                level.marbles = Join(Kind(Row(.5f, -1.8f, 1.8f, 6), MarbleKind.Split, 2), Kind(Row(1.4f, -1.8f, 1.8f, 6), MarbleKind.Split, 3), Row(2.3f, -.45f, .45f, 2));
                level.zones = Zones(Mud(-2.9f, 1.0f, .45f), Mud(2.9f, 1.0f, .45f));
                break;

            // 42 Oyun Salonu — 4×4, köşegen boyunca zincirleme çatapat; önde rögar.
            case 41:
                Big(level, 3.8f, 8);
                level.marbles = Kind(Grid(0f, 1.4f, 4, 3, .72f, .72f), MarbleKind.Catapat, 0, 5, 10);
                level.zones = Zones(Slick(0f, -.8f, .7f));
                break;

            // 43 Fıskiye — ortada çamur birikintisi, iki halka; iki karpuz.
            case 42:
                Big(level, 3.9f, 9);
                level.marbles = Kind(Join(Ring(0f, 1.4f, 1.15f, 9, 0f), Ring(0f, 1.4f, 2.0f, 5, 90f)), MarbleKind.Split, 9, 12);
                level.zones = Zones(Mud(0f, 1.4f, .5f));
                break;

            // 44 Asansör — KESEM BÖLÜMÜ: sıkı 4×4 blok, iki yanda çamur, önde kaldırım.
            case 43:
                Big(level, 3.9f, 7);
                level.marbles = Kind(Grid(0f, 1.8f, 4, 4, .62f, .62f), MarbleKind.Split, 1, 14);
                level.obstacles = new[] { O(0f, -.1f, 1.6f, .3f) };
                level.zones = Zones(Mud(-1.8f, .6f, .5f), Mud(1.8f, .6f, .5f));
                break;

            // 45 Otopark Katı — iki uzun sıra, aralarda kaldırım, ortada rögar.
            case 44:
                Big(level, 3.9f, 8);
                level.marbles = Join(Row(.3f, -2.2f, 2.2f, 6), Row(2.1f, -2.2f, 2.2f, 6));
                level.obstacles = new[] { O(-1.1f, 1.2f, .3f, .9f), O(1.1f, 1.2f, .3f, .9f) };
                level.zones = Zones(Slick(0f, 1.2f, .45f));
                break;

            // 46 Kapanış Saati — nefes: kenara yakın yay, bir karpuz.
            case 45:
                Big(level, 3.7f, 7);
                level.marbles = Join(Kind(ArcAt(0f, 0f, 2.6f, 20f, 160f, 10), MarbleKind.Split, 5), P(-.5f, 1.1f), P(.5f, 1.1f));
                break;

            // 47 AVM Buluşması — artı, göbekte çatapat, iki karpuz; çamur, rögar, kaldırım.
            case 46:
                Big(level, 3.9f, 9);
                level.marbles = Join(Kind(Kind(Cross(0f, 1.3f, 2, .62f), MarbleKind.Split, 1, 2), MarbleKind.Catapat, 0),
                                     P(-1.4f, 2.6f), P(1.4f, 2.6f), P(-1.4f, 0f), P(1.4f, 0f));
                level.obstacles = new[] { O(2.5f, .3f, 1.0f, .3f, -30f) };
                level.zones = Zones(Mud(-2.6f, 2.6f, .45f), Slick(0f, -1.4f, .6f));
                break;

            // 48 Ustalık Sınavı — elmas + dört uydu; çatapat göbek, iki karpuz; çamur, kaldırım.
            case 47:
                Big(level, 4.0f, 9);
                level.marbles = Join(Kind(Kind(Diamond(0f, 1.5f, .64f), MarbleKind.Split, 2, 10), MarbleKind.Catapat, 6),
                                     P(-2.2f, .6f), P(2.2f, .6f));
                level.obstacles = new[] { O(0f, -.4f, 1.8f, .3f) };
                level.zones = Zones(Mud(-2.4f, 1.5f, .45f), Mud(2.4f, 1.5f, .45f));
                break;

            // ================= GÖKDELENLER · hepsi karışık, büyük final =================

            // 49 Plaza Önü — halka, göbekte çatapat; önde rögar, arkada kum.
            case 48:
                Big(level, 3.8f, 8);
                level.marbles = Join(Ring(0f, 1.2f, 1.3f, 10, 0f), Kind(new[] { P(0f, 1.2f) }, MarbleKind.Catapat), P(-2.4f, .4f), P(2.4f, .4f));
                level.zones = Zones(Slick(0f, -.9f, .6f), Sand(0f, 3.0f, .5f));
                break;

            // 50 Cam Kule — iki sütunlu yüksek kule, iki buzlu kat; eğimli.
            case 49:
                Big(level, 4.0f, 9);
                level.marbles = Join(Kind(Grid(0f, 1.5f, 2, 6, .66f, .6f), MarbleKind.Ice, 4, 9), P(-1.6f, 1.5f), P(1.6f, 1.5f));
                level.slope = new Vector2(.6f, 0f);
                break;

            // 51 Çatı Katı — 15'lik ıstaka + tepe, ortası karpuz; iki yanda çukur.
            case 50:
                Big(level, 4.0f, 9);
                level.marbles = Join(Kind(Rack(0f, 1.6f, 4, .62f), MarbleKind.Split, 4), P(0f, 3.0f), P(-1.9f, .6f), P(1.9f, .6f));
                level.zones = Zones(Pit(-2.2f, 1.6f, .45f), Pit(2.2f, 1.6f, .45f));
                break;

            // 52 Helikopter Pisti — büyük halka, ortada buzlu misket rögarın üstünde.
            case 51:
                Big(level, 4.0f, 9);
                level.marbles = Join(Ring(0f, 1.4f, 1.9f, 12, 0f), Kind(new[] { P(0f, 1.4f) }, MarbleKind.Ice), P(-.7f, 1.4f), P(.7f, 1.4f));
                level.zones = Zones(Slick(0f, 1.4f, .6f));
                break;

            // 53 Rüzgâr Tüneli — kaldırımlar arası tünel + iki yan sütun; güçlü eğim.
            case 52:
                Big(level, 4.0f, 8);
                level.marbles = Join(Line(0f, 0f, 0f, 3.0f, 5), Line(-1.7f, .5f, -1.7f, 2.5f, 3), Line(1.7f, .5f, 1.7f, 2.5f, 3), P(-2.6f, 1.5f), P(2.6f, 1.5f));
                level.obstacles = new[] { O(-.8f, 1.5f, .3f, 2.6f), O(.8f, 1.5f, .3f, 2.6f) };
                level.slope = new Vector2(1.0f, .3f);
                break;

            // 54 Gece Işıkları — iki dalga; çatapat ve buz serpiştirilmiş; önde çamur.
            case 53:
                Big(level, 4.0f, 9);
                level.marbles = Kind(Kind(Join(Wave(-2.5f, 2.5f, 10, 1.0f, .5f, 1.2f), Wave(-1.5f, 1.5f, 4, 2.5f, .3f, 1.2f)), MarbleKind.Catapat, 4, 12), MarbleKind.Ice, 1, 8);
                level.zones = Zones(Mud(0f, -.6f, .5f));
                break;

            // 55 Köprü Yolu — iki uzun sıra (köprü), önde çukur, iki yanda kaldırım.
            case 54:
                Big(level, 4.0f, 9);
                level.marbles = Join(Row(.9f, -2.3f, 2.3f, 7), Row(1.8f, -2.3f, 2.3f, 7));
                level.obstacles = new[] { O(-2.9f, 1.35f, .3f, 1.3f), O(2.9f, 1.35f, .3f, 1.3f) };
                level.zones = Zones(Pit(0f, -.4f, .5f));
                break;

            // 56 Kule Gölgesi — KESEM BÖLÜMÜ: büyük çarpı; buz, karpuz, eğim, iki çukur.
            case 55:
                Big(level, 4.0f, 8);
                level.marbles = Join(Kind(Kind(XShape(0f, 1.4f, 3, .66f), MarbleKind.Ice, 0), MarbleKind.Split, 5), P(-2.3f, 2.2f), P(2.3f, 2.2f));
                level.zones = Zones(Pit(-2.5f, .3f, .4f), Pit(2.5f, .3f, .4f));
                level.slope = new Vector2(-.6f, 0f);
                break;

            // 57 Son Kat — 4×4 blok, iki köşe çatapat; önde rögar, yanlarda kaldırım.
            case 56:
                Big(level, 3.9f, 9);
                level.marbles = Kind(Grid(0f, 1.4f, 4, 3, .7f, .7f), MarbleKind.Catapat, 0, 11);
                level.obstacles = new[] { O(-2.1f, 1.4f, .3f, 1.6f), O(2.1f, 1.4f, .3f, 1.6f) };
                level.zones = Zones(Slick(0f, -.8f, .7f));
                break;

            // 58 Şehir Manzarası — nefes: kenara yakın yay, ortada üç buzlu.
            case 57:
                Big(level, 3.8f, 8);
                level.marbles = Join(ArcAt(0f, 0f, 2.7f, 18f, 162f, 11), Kind(new[] { P(-.6f, 1.2f), P(0f, 1.6f), P(.6f, 1.2f) }, MarbleKind.Ice));
                break;

            // 59 Büyük Final — elmas + dört uydu + tepe; her şey bir arada.
            case 58:
                Big(level, 4.0f, 10);
                level.marbles = Join(Kind(Kind(Kind(Diamond(0f, 1.4f, .64f), MarbleKind.Catapat, 6), MarbleKind.Ice, 0), MarbleKind.Split, 12),
                                     P(0f, 3.3f));
                level.zones = Zones(Sand(-2.6f, 1.4f, .45f), Pit(2.6f, 1.4f, .4f), Slick(0f, -1.3f, .6f));
                level.slope = new Vector2(.4f, 0f);
                break;

            // 60 Ustalık Sınavı — oyunun en zoru: 15'lik ıstaka (buz, karpuz, çatapat) + dört uydu;
            // önde çukur ve kaldırım, eğimli.
            case 59:
                Big(level, 4.0f, 10);
                level.marbles = Join(Kind(Kind(Kind(Rack(0f, 1.5f, 5, .62f), MarbleKind.Ice, 0, 10, 14), MarbleKind.Split, 7), MarbleKind.Catapat, 4),
                                     P(-2.3f, .6f), P(2.3f, .6f));
                level.obstacles = new[] { O(0f, -1.4f, 1.6f, .3f) };
                level.zones = Zones(Pit(0f, -.6f, .45f));
                level.slope = new Vector2(.5f, 0f);
                break;
        }
        if (pendingAuto) { AutoStars(level); pendingAuto = false; }
        Tune(level, index);
        Vary(level, index);
    }

    // ÇEŞİTLİLİK: dizilimi bozmadan küçük döndürme (derece) + kaydırma (x, z). Eski 120 bölümün
    // hiçbirine 0.30'dan fazla benzemesin (MemleketLayoutVerify). SehirVarietyTuner hesaplar.
    public static System.Func<int, Vector3> VariationOverride;
    private static readonly Dictionary<int, Vector3> Variation = new Dictionary<int, Vector3>
    {
        { 1, new Vector3(-20f, 0f, 0f) }, { 2, new Vector3(-10f, .15f, 0f) },
        { 4, new Vector3(-10f, 0f, 0f) }, { 5, new Vector3(0f, 0f, -.3f) }, { 6, new Vector3(0f, -.45f, 0f) },
        { 9, new Vector3(-20f, 0f, 0f) }, { 10, new Vector3(0f, 0f, -.3f) }, { 11, new Vector3(-10f, 0f, 0f) },
        { 12, new Vector3(0f, 0f, .6f) }, { 13, new Vector3(180f, 0f, 0f) }, { 14, new Vector3(-20f, 0f, 0f) },
        { 15, new Vector3(0f, 0f, -.15f) }, { 16, new Vector3(-20f, .45f, -.45f) }, { 19, new Vector3(-20f, 0f, .15f) },
        { 20, new Vector3(-10f, 0f, .45f) }, { 21, new Vector3(0f, 0f, .3f) }, { 22, new Vector3(0f, -.15f, 0f) },
        { 24, new Vector3(0f, -.3f, 0f) }, { 25, new Vector3(0f, 0f, -.45f) }, { 27, new Vector3(0f, -.3f, 0f) },
        { 28, new Vector3(0f, .15f, .15f) }, { 30, new Vector3(0f, 0f, .3f) }, { 31, new Vector3(-10f, -.15f, 0f) },
        { 33, new Vector3(0f, -.45f, 0f) }, { 35, new Vector3(-20f, 0f, 0f) }, { 37, new Vector3(0f, -.15f, 0f) },
        { 38, new Vector3(-10f, -.15f, .3f) }, { 39, new Vector3(-10f, 0f, .15f) }, { 40, new Vector3(-20f, -.15f, 0f) },
        { 41, new Vector3(180f, 0f, .45f) }, { 43, new Vector3(180f, -.45f, 0f) }, { 45, new Vector3(-20f, 0f, 0f) },
        { 46, new Vector3(0f, 0f, -.3f) }, { 47, new Vector3(-10f, -.45f, 0f) }, { 48, new Vector3(0f, 0f, .15f) },
        { 49, new Vector3(0f, -.3f, .15f) }, { 50, new Vector3(-20f, 0f, 0f) }, { 51, new Vector3(-10f, 0f, 0f) },
        { 53, new Vector3(0f, 0f, -.3f) }, { 54, new Vector3(-10f, 0f, 0f) }, { 55, new Vector3(-10f, 0f, 0f) },
        { 56, new Vector3(20f, -.6f, -.3f) }, { 57, new Vector3(0f, 0f, .45f) }, { 58, new Vector3(180f, 0f, .15f) },
        { 59, new Vector3(0f, 0f, .15f) },
    };

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
            { var p = Move(level.zones[i].x, level.zones[i].z); level.zones[i].x = p.x; level.zones[i].z = p.y; }
        if (level.slope.sqrMagnitude > 0f)
        { var sl = rot * new Vector3(level.slope.x, 0f, level.slope.y); level.slope = new Vector2(sl.x, sl.z); }
    }

    // ATIŞ HAKKI VE YILDIZLAR (13-60): sıradan oyuncu ölçümünden. Satır: { atış, 1y, 2y, 3y }.
    // Tabloda olmayan bölüm Big() başlangıç değerinde kalır (1y %35, 2y %60, 3y hepsi).
    // Satır tek sayıysa sadece atış hakkı, yıldızlar Big() başlangıcında kalır.
    // Ölçüm 2026-10-20 (Logs/SehirTur1.tsv, HumanPlus1.tsv, SehirTur3*.tsv). Kesem (8. bölümler 20/32/44/56) güçsüz ≤ %20.
    // Liman/AVM/Gökdelenler kullanıcı isteğiyle biraz daha zor (hedefler bir misket yukarı).
    private static readonly Dictionary<int, int[]> Tuning = new Dictionary<int, int[]>
    {
        { 0, new[] { 7, 5, 6, 7 } },
        { 1, new[] { 7, 3, 5, 6 } },
        { 2, new[] { 6, 5, 6, 8 } },
        { 3, new[] { 7, 4, 5, 7 } },
        { 4, new[] { 6, 7, 8, 9 } },
        { 5, new[] { 7, 6, 7, 8 } },
        { 6, new[] { 7, 6, 7, 8 } },
        { 7, new[] { 11, 7, 8, 9 } },
        { 8, new[] { 11, 7, 8, 9 } },
        { 9, new[] { 9, 9, 10, 11 } },
        { 10, new[] { 9, 7, 8, 10 } },
        { 11, new[] { 11, 4, 6, 8 } },
        { 12, new[] { 6, 8, 9, 10 } },
        { 13, new[] { 8, 6, 7, 8 } },
        { 14, new[] { 8, 12, 13, 14 } },
        { 15, new[] { 6, 9, 10, 11 } },
        { 16, new[] { 10, 9, 11, 12 } },   // 2026-10-21 yeniden ölçüldü (eski ölçüm varyasyondan önceydi): geçme %67, 2★ %32, 3★ %2
        { 17, new[] { 6, 11, 12, 13 } },
        { 18, new[] { 6, 10, 11, 12 } },
        { 19, new[] { 6, 8, 10, 13 } },
        { 20, new[] { 6, 10, 11, 12 } },
        { 21, new[] { 6, 9, 10, 11 } },
        { 22, new[] { 9, 9, 10, 11 } },
        { 23, new[] { 8, 9, 10, 11 } },
        { 24, new[] { 7, 7, 8, 9 } },
        { 25, new[] { 9, 10, 11, 12 } },
        { 26, new[] { 9, 11, 12, 13 } },
        { 27, new[] { 7, 10, 11, 12 } },
        { 28, new[] { 8, 8, 9, 11 } },
        { 29, new[] { 8, 11, 12, 13 } },
        { 30, new[] { 9, 7, 9, 11 } },
        { 31, new[] { 7, 8, 9, 12 } },
        { 32, new[] { 6, 12, 13, 14 } },
        { 33, new[] { 7, 9, 10, 11 } },
        { 34, new[] { 9, 10, 11, 12 } },
        { 35, new[] { 9, 4, 6, 8 } },
        { 36, new[] { 7, 10, 11, 12 } },
        { 37, new[] { 7, 10, 11, 12 } },
        { 38, new[] { 9, 10, 11, 12 } },
        { 39, new[] { 9, 10, 11, 12 } },
        { 40, new[] { 10, 12, 13, 14 } },
        { 41, new[] { 3, 10, 11, 12 } },
        { 42, new[] { 9, 11, 12, 13 } },
        { 43, new[] { 7, 6, 7, 9 } },
        { 44, new[] { 8, 10, 11, 12 } },
        { 45, new[] { 7, 10, 11, 12 } },
        { 46, new[] { 8, 11, 12, 13 } },
        { 47, new[] { 9, 8, 10, 11 } },
        { 48, new[] { 4, 9, 10, 11 } },
        { 49, new[] { 9, 8, 9, 11 } },
        { 50, new[] { 8, 11, 12, 13 } },
        { 51, new[] { 10, 13, 14, 15 } },
        { 52, new[] { 8, 9, 10, 12 } },
        { 53, new[] { 9, 11, 12, 13 } },
        { 54, new[] { 9, 11, 12, 13 } },
        { 55, new[] { 8, 13, 14, 15 } },
        { 56, new[] { 7, 10, 11, 12 } },
        { 57, new[] { 8, 8, 9, 12 } },
        { 58, new[] { 10, 12, 13, 14 } },
        { 59, new[] { 10, 12, 14, 15 } },
    };

    private static void Tune(LevelData level, int index)
    {
        if (!Tuning.TryGetValue(index, out var t)) return;
        level.shotCount = t[0];
        if (t.Length < 4) return;
        level.oneStarTarget = t[1]; level.twoStarTarget = t[2]; level.threeStarTarget = t[3];
    }

    // Büyük bölüm kurulumu: yıldız hedefleri misket sayısından (Apply sonunda) türetilir.
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
    // Yardımcılar (MemleketBook ile aynı biçim)

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

    private static MarbleSpot P(float x, float z) => new MarbleSpot(x, z);

    // Seçilen sıradaki misketleri özel yapar (ör. çatapat).
    private static MarbleSpot[] Kind(MarbleSpot[] spots, MarbleKind kind, params int[] which)
    {
        var copy = (MarbleSpot[])spots.Clone();
        if (which == null || which.Length == 0) for (int i = 0; i < copy.Length; i++) copy[i].kind = kind;
        else foreach (int i in which) if (i >= 0 && i < copy.Length) copy[i].kind = kind;
        return copy;
    }
    private static ObstacleSpot O(float x, float z, float w, float d, float angle = 0f) => new ObstacleSpot(x, z, w, d, angle);
    private static ZoneSpot Slick(float x, float z, float r) => new ZoneSpot(ZoneKind.Slick, x, z, r);
    private static ZoneSpot[] Zones(params ZoneSpot[] z) => z;

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

    private static ZoneSpot Sand(float x, float z, float r) => new ZoneSpot(ZoneKind.Sand, x, z, r);
    private static ZoneSpot Mud(float x, float z, float r) => new ZoneSpot(ZoneKind.Mud, x, z, r);
    private static ZoneSpot Pit(float x, float z, float r) => new ZoneSpot(ZoneKind.Pit, x, z, r);

    private static MarbleSpot[] Spots(params float[] pairs)
    {
        var list = new MarbleSpot[pairs.Length / 2];
        for (int i = 0; i < list.Length; i++) list[i] = new MarbleSpot(pairs[i * 2], pairs[i * 2 + 1]);
        return list;
    }

    // Dalgalı sıra: z = z0 + amp·sin(x·freq).
    private static MarbleSpot[] Wave(float x0, float x1, int n, float z0, float amp, float freq)
    {
        var list = new MarbleSpot[n];
        for (int i = 0; i < n; i++)
        {
            float x = Mathf.Lerp(x0, x1, i / (float)(n - 1));
            list[i] = P(x, z0 + amp * Mathf.Sin(x * freq));
        }
        return list;
    }

    // Zikzak: x0→x1 boyunca z0 ile z1 arasında gidip gelir.
    private static MarbleSpot[] Zig(float x0, float x1, int n, float z0, float z1)
    {
        var list = new MarbleSpot[n];
        for (int i = 0; i < n; i++) list[i] = P(Mathf.Lerp(x0, x1, i / (float)(n - 1)), i % 2 == 0 ? z0 : z1);
        return list;
    }

    // Artı: merkez + dört kol, her kolda n misket.
    private static MarbleSpot[] Cross(float cx, float cz, int n, float gap)
    {
        var list = new List<MarbleSpot> { P(cx, cz) };
        for (int k = 1; k <= n; k++)
        {
            list.Add(P(cx + k * gap, cz)); list.Add(P(cx - k * gap, cz));
            list.Add(P(cx, cz + k * gap)); list.Add(P(cx, cz - k * gap));
        }
        return list.ToArray();
    }

    // Çarpı: merkez + dört çapraz kol, her kolda n misket.
    private static MarbleSpot[] XShape(float cx, float cz, int n, float gap)
    {
        var list = new List<MarbleSpot> { P(cx, cz) };
        float d = gap * .7071f;
        for (int k = 1; k <= n; k++)
        {
            list.Add(P(cx + k * d, cz + k * d)); list.Add(P(cx - k * d, cz + k * d));
            list.Add(P(cx + k * d, cz - k * d)); list.Add(P(cx - k * d, cz - k * d));
        }
        return list.ToArray();
    }

    // Elmas kafes: satırlar 1,3,5,3,1 (13 misket).
    private static MarbleSpot[] Diamond(float cx, float cz, float gap)
    {
        var list = new List<MarbleSpot>();
        int[] rows = { 1, 3, 5, 3, 1 };
        for (int r = 0; r < rows.Length; r++)
            for (int i = 0; i < rows[r]; i++)
                list.Add(P(cx + (i - (rows[r] - 1) * .5f) * gap, cz + (r - 2) * gap * .87f));
        return list.ToArray();
    }

    // Üçlü küme: ucu atıcıya bakan üçgen.
    private static MarbleSpot[] Tri(float cx, float cz, float gap)
    {
        float h = gap * .866f;
        return new[] { P(cx, cz - h * .66f), P(cx - gap * .5f, cz + h * .34f), P(cx + gap * .5f, cz + h * .34f) };
    }

    // Bilardo ıstakası: ucu atıcıya bakar.
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
