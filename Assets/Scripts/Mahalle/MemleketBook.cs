using System.Collections.Generic;
using UnityEngine;

// HARİTA 2 — MEMLEKET: 60 elle tasarlanmış bölüm (LevelBook'un Memleket karşılığı).
// Konumlar çemberin merkezine göre: x sağa, z ileri (atıcıdan uzağa). Atıcı z=-4.2.
//
// Bölge = yeni zemin kuralı + (varsa) özel misket:
//   0-11  SAHİL          kum
//   12-23 KÖY MEYDANI    çamur (+kum)
//   24-35 YAYLA          eğim + BUZLU misket
//   36-47 KASABA PAZARI  tezgâh/kasa koridorları + BÖLÜNEN (karpuz) misket
//   48-59 BAYRAM YERİ    çukur + hepsinin karışımı
// Her bölgenin ilk 2-3 bölümü yeni kuralı tek başına öğretir, sonra karışır;
// 12. bölüm ustalık sınavı (geçiş 2 yıldız).
//
// Atış hakkı ve yıldız hedefleri: Tune() — Faz 6'da fizik ölçümüyle ayarlandı
// (MemleketTuning tablosu, YAZ_TATILI_ILERLEME.md).
public static class MemleketBook
{
    public static void Apply(LevelData level, int index)
    {
        switch (index)
        {
            // ================= SAHİL · kum =================

            // 01 Kumsal Girişi — kumdan geçen atış yavaşlar. Tek kural, tek küme.
            case 0:
                Set(level, 3.2f, 5);
                // Ölçüldü: 7 misketle 3 atışta %86 — hedef %62. Arkaya üç misket eklendi.
                level.marbles = Join(Ring(0f, .8f, .64f, 6, 0f), P(0f, .8f), P(-1.3f, 2.1f), P(0f, 2.35f), P(1.3f, 2.1f));
                level.zones = Zones(Sand(0f, -1.3f, .9f));
                break;

            // 02 Havlu Yanı — iki kum havlusu; üstündeki misketler zor çıkar.
            case 1:
                Set(level, 3.3f, 5);
                level.marbles = Join(Row(1.3f, -.62f, .62f, 3), P(-1.6f, -.1f), P(-1.6f, .55f), P(1.6f, .2f), P(1.6f, .85f));
                level.zones = Zones(Sand(-1.6f, .2f, .75f), Sand(1.6f, .5f, .75f));
                break;

            // 03 Kumdan Kale — kale duvarı kumun kenarında; merkezdeki kale.
            case 2:
                Set(level, 3.4f, 5);
                level.marbles = Join(Ring(0f, .7f, 1.0f, 8, 22.5f), P(0f, .7f));
                level.zones = Zones(Sand(0f, .7f, 1.2f));
                break;

            // 04 Şemsiye Altı — şemsiye direği ortada, yan yol kumlu.
            case 3:
                Set(level, 3.4f, 5);
                level.marbles = Join(ArcAt(0f, .2f, 1.75f, 35f, 145f, 7), P(2.3f, -.6f), P(-2.3f, -.6f));
                level.obstacles = new[] { O(0f, .2f, .45f, .45f) };
                level.zones = Zones(Sand(-1.5f, -1.3f, .8f));
                break;

            // 05 Midye Kıyısı — üç midye kümesi, orta şerit açık, yanlar kumlu.
            case 4:
                Set(level, 3.5f, 5);
                level.marbles = Join(Tri(-1.6f, 1.0f, .62f), Tri(0f, 2.0f, .62f), Tri(1.6f, 1.0f, .62f));
                level.zones = Zones(Sand(-.85f, -.3f, .65f), Sand(.85f, -.3f, .65f));
                break;

            // 06 Dalga Çizgisi — dalgalı sıra; arkadaki kum kaçanı yakalar.
            case 5:
                Set(level, 3.5f, 5);
                level.marbles = Wave(-2.4f, 2.4f, 9, .5f, .55f, 1.6f);
                level.zones = Zones(Sand(0f, 2.3f, 1.0f));
                break;

            // 07 İskele Başı — iki iskele duvarı arasında dar kanal; ağzı kumlu.
            case 6:
                Set(level, 3.6f, 5);
                level.marbles = Join(Line(0f, -.2f, 0f, 2.3f, 5), P(-1.8f, 1.6f), P(1.8f, 1.6f), P(-1.8f, .2f), P(1.8f, .2f));
                level.obstacles = new[] { O(-.7f, .9f, .35f, 2.3f), O(.7f, .9f, .35f, 2.3f) };
                level.zones = Zones(Sand(0f, -1.35f, .6f));
                break;

            // 08 Kayık Arkası — eğik kayık gövdesi kümeyi saklıyor.
            case 7:
                Set(level, 3.6f, 5);
                level.marbles = Join(Ring(.3f, 1.6f, .64f, 5, 90f), P(.3f, 1.6f), P(-1.8f, -.4f), P(-2.25f, .25f), P(-1.4f, .25f));
                level.obstacles = new[] { O(.3f, .4f, 2.0f, .45f, -20f) };
                level.zones = Zones(Sand(1.9f, -.9f, .8f));
                break;

            // 09 Sıcak Kum — küme kumun içinde: dışarı itmek için önce kumdan çıkarmalı.
            // (Ölçüldü: kum 1.8 yarıçaplıyken 7 atışta bile tavan %36 — taban %40'ın altı.
            //  Kum kümeyi saracak kadar küçültüldü.)
            case 8:
                Set(level, 3.7f, 5);
                level.marbles = Join(Grid(0f, .9f, 3, 3, .72f, .72f), P(-2.6f, .4f), P(2.6f, .4f));
                level.zones = Zones(Sand(0f, .9f, 1.15f));
                break;

            // 10 Son Dalga — iki yay; arada kum şeridi.
            case 9:
                Set(level, 3.7f, 4);
                level.marbles = Join(Arc(2.45f, 30f, 150f, 7), Arc(1.25f, 60f, 120f, 3));
                level.zones = Zones(Sand(0f, 1.75f, .7f));
                break;

            // 11 Sahil Buluşması — büyük halka, merkez kumda, iki kaya.
            case 10:
                Set(level, 3.8f, 5);
                level.marbles = Join(Ring(0f, .8f, 1.55f, 10, 18f), P(0f, .8f));
                level.obstacles = new[] { O(-2.7f, 2.0f, .5f, .5f, 30f), O(2.7f, 2.0f, .5f, .5f, -30f) };
                level.zones = Zones(Sand(0f, .8f, .7f));
                break;

            // 12 Ustalık Sınavı — kumla çevrili ıstaka, önünde kama.
            case 11:
                Set(level, 3.9f, 4); level.starsToPass = 2;
                level.marbles = Join(Rack(0f, .9f, 4, .64f), P(-2.8f, -1.0f), P(2.8f, -1.0f));
                level.obstacles = new[] { O(0f, -.75f, 1.4f, .4f) };
                level.zones = Zones(Sand(-1.7f, 1.4f, .85f), Sand(1.7f, 1.4f, .85f), Sand(0f, 3.0f, .75f));
                break;

            // ================= KÖY MEYDANI · çamur =================

            // 01 Çınar Altı — sıranın arkası çamur: iten saplar.
            case 12:
                Set(level, 3.4f, 5);
                // Ölçüldü: 8 misketle 3 atışta %75. Çamurun iki yanına iki misket eklendi.
                level.marbles = Join(Row(.55f, -1.55f, 1.55f, 6), P(-.32f, 1.7f), P(.32f, 1.7f), P(-2.3f, 1.25f), P(2.3f, 1.25f));
                level.zones = Zones(Mud(0f, 1.7f, .7f));
                break;

            // 02 Kahve Önü — iki masa, ortada birikinti.
            case 13:
                Set(level, 3.4f, 5);
                level.marbles = Join(P(-1.2f, 1.5f), P(-1.85f, .85f), P(-1.2f, .2f), P(1.2f, 1.5f), P(1.85f, .85f), P(1.2f, .2f), P(0f, 1.75f), P(0f, 1.12f));
                level.obstacles = new[] { O(-1.2f, .85f, .5f, .5f), O(1.2f, .85f, .5f, .5f) };
                level.zones = Zones(Mud(0f, .25f, .5f));
                break;

            // 03 Su Birikintisi — üç birikinti, zikzak sıra.
            case 14:
                Set(level, 3.5f, 5);
                level.marbles = Zig(-2.2f, 2.2f, 9, .5f, 1.6f);
                level.zones = Zones(Mud(-1.25f, -.25f, .5f), Mud(1.2f, 2.25f, .5f), Mud(-.2f, 2.5f, .45f));
                break;

            // 04 Muhtarlık — bina arkada, önü iki sıra.
            case 15:
                Set(level, 3.5f, 5);
                level.marbles = Join(Row(.45f, -1.3f, 1.3f, 4), Row(1.1f, -.95f, .95f, 3));
                level.obstacles = new[] { O(0f, 1.95f, 2.4f, .45f) };
                level.zones = Zones(Mud(-1.9f, 1.4f, .5f), Sand(0f, -1.2f, .7f));
                break;

            // 05 Çeşme Başı — elmas çeşme, çevresi halka, arkası çamur.
            case 16:
                Set(level, 3.6f, 5);
                level.marbles = Ring(0f, .9f, 1.1f, 8, 0f);
                level.obstacles = new[] { O(0f, .9f, .6f, .6f, 45f) };
                level.zones = Zones(Mud(0f, 2.65f, .6f), Sand(0f, -.85f, .5f));
                break;

            // 06 Tavuk Kümesi — üç duvarlı kümes, ağzı çamurlu.
            case 17:
                Set(level, 3.6f, 5);
                level.marbles = Join(Grid(0f, .95f, 2, 3, .7f, .58f), P(-2.2f, .4f), P(2.2f, .4f), P(0f, -.8f));
                // Ölçüldü: yan duvarlar tam boyken tavan 7 atışta %33 (misketler kümeste hapsoluyordu).
                // Yan duvarlar arkaya kısaltıldı; ön sıra yandan kaçabiliyor.
                level.obstacles = new[] { O(-.95f, 1.45f, .35f, 1.0f), O(.95f, 1.45f, .35f, 1.0f), O(0f, 2.05f, 2.25f, .35f) };
                level.zones = Zones(Mud(0f, -.1f, .38f));
                break;

            // 07 Harman Yeri — geniş halka, ortası çamur: içe iten kaybeder.
            case 18:
                Set(level, 3.7f, 5);
                level.marbles = Ring(0f, .6f, 1.95f, 12, 15f);
                level.zones = Zones(Mud(0f, .6f, .95f));
                break;

            // 08 Samanlık — üç saman yığını (kum), aralarda çamur.
            case 19:
                Set(level, 3.7f, 5);
                level.marbles = Join(Tri(-1.5f, 1.2f, .6f), Tri(1.5f, 1.2f, .6f), Tri(0f, 2.5f, .6f));
                level.zones = Zones(Sand(-1.5f, 1.4f, .7f), Sand(1.5f, 1.4f, .7f), Sand(0f, 2.7f, .7f), Mud(0f, .3f, .55f));
                break;

            // 09 Dar Sokak — iki uzun duvar, sokağın sonu çamur.
            case 20:
                Set(level, 3.8f, 4);
                level.marbles = Join(Zig(-.3f, .3f, 6, -.4f, 2.6f, true), P(-2.4f, .2f), P(2.4f, .2f), P(-2.4f, 1.9f), P(2.4f, 1.9f));
                level.obstacles = new[] { O(-1.1f, 1.0f, .35f, 3.0f), O(1.1f, 1.0f, .35f, 3.0f) };
                level.zones = Zones(Mud(0f, 3.15f, .5f));
                break;

            // 10 Son Yağmur — beşgen + dış dört; her yöne birikinti.
            case 21:
                Set(level, 3.8f, 4);
                level.marbles = Join(Ring(0f, .9f, 1.2f, 5, 90f), P(0f, .9f), P(-2.5f, -.3f), P(2.5f, -.3f), P(-2.3f, 2.4f), P(2.3f, 2.4f));
                level.zones = Zones(Mud(-1.65f, .5f, .5f), Mud(1.65f, .5f, .5f), Mud(0f, 2.55f, .5f), Sand(-.8f, -1.4f, .5f), Sand(.8f, -1.4f, .5f));
                break;

            // 11 Köy Buluşması — ıstaka, iki yanında çamur, arkasında kum.
            case 22:
                Set(level, 3.9f, 5);
                level.marbles = Join(Rack(0f, 1.1f, 4, .63f), P(-2.6f, 2.3f), P(2.6f, 2.3f));
                level.obstacles = new[] { O(-1.25f, -1.1f, .4f, .4f), O(1.25f, -1.1f, .4f, .4f) };
                level.zones = Zones(Mud(-1.95f, 1.1f, .6f), Mud(1.95f, 1.1f, .6f), Sand(0f, 3.0f, .7f));
                break;

            // 12 Ustalık Sınavı — artı işareti, dört köşede çamur.
            case 23:
                Set(level, 4.0f, 4); level.starsToPass = 2;
                level.marbles = Cross(0f, 1.1f, 3, .62f);
                level.obstacles = new[] { O(0f, -1.65f, 1.6f, .4f) };
                level.zones = Zones(Mud(-1.15f, 2.25f, .5f), Mud(1.15f, 2.25f, .5f), Mud(-1.15f, -.05f, .5f), Mud(1.15f, -.05f, .5f));
                break;

            // ================= YAYLA · eğim + buzlu misket =================

            // 01 Yayla Yolu — sağa eğim; çapraz sıra.
            case 24:
                Set(level, 3.5f, 5); level.slope = new Vector2(.8f, 0f);
                // Ölçüldü: 7 misketle 3 atışta %100. Karşı çaprazda ikinci kısa sıra eklendi.
                level.marbles = Join(Line(-1.8f, -.2f, 1.8f, 1.9f, 7), Line(-1.6f, 2.0f, -.4f, 2.7f, 3));
                break;

            // 02 Çoban Çeşmesi — sola eğim; çeşmenin çevresi.
            case 25:
                Set(level, 3.5f, 5); level.slope = new Vector2(-.9f, 0f);
                level.marbles = Ring(0f, 1.05f, 1.05f, 8, 22.5f);
                level.obstacles = new[] { O(0f, 1.05f, .55f, .55f) };
                break;

            // 03 Buzlu Pınar — buzlu misket, tek başına. Önce kır, sonra it.
            case 26:
                Set(level, 3.5f, 5);
                level.marbles = Join(Kind(Tri(0f, .9f, .72f), MarbleKind.Ice), Arc(2.2f, 40f, 140f, 5));
                break;

            // 04 Ahşap Ev — eğim atıcıya doğru; ev arkada, ikinci sırada buz.
            case 27:
                Set(level, 3.6f, 5); level.slope = new Vector2(0f, -.7f);
                level.marbles = Join(Row(.4f, -1.3f, 1.3f, 5), Kind(Row(1.1f, -.95f, .95f, 4), MarbleKind.Ice, 1, 2));
                level.obstacles = new[] { O(0f, 2.05f, 1.8f, .5f) };
                break;

            // 05 Çam Gölgesi — üç çam, aralarında çiftler.
            case 28:
                Set(level, 3.6f, 5); level.slope = new Vector2(.9f, 0f);
                level.marbles = Kind(Spots(-1.95f, 1.35f, -1.3f, 2.05f, -.65f, 1.4f, .65f, .55f, 1.3f, 1.25f, 1.95f, .6f, -.62f, 2.65f, .62f, 2.65f, 0f, 1.6f, -1.2f, .3f),
                                     MarbleKind.Ice, 2, 8);
                level.obstacles = new[] { O(-1.3f, 1.4f, .45f, .45f), O(1.3f, .6f, .45f, .45f), O(0f, 2.4f, .45f, .45f) };
                break;

            // 06 Serin Çayır — 4x3 çayır, köşelerde buz; çapraz eğim.
            case 29:
                Set(level, 3.7f, 5); level.slope = new Vector2(-.7f, -.4f);
                level.marbles = Kind(Grid(0f, 1.0f, 4, 3, .74f, .74f), MarbleKind.Ice, 0, 3, 11);
                break;

            // 07 Sisli Tepe — iç içe iki halka, tepede buz; eğim sert.
            case 30:
                Set(level, 3.7f, 5); level.slope = new Vector2(0f, -1.0f);
                level.marbles = Join(Kind(new[] { P(0f, 1.25f) }, MarbleKind.Ice), Kind(Ring(0f, 1.25f, .72f, 5, 90f), MarbleKind.Ice, 0), Ring(0f, 1.25f, 1.6f, 6, 30f));
                break;

            // 08 Kaya Yanı — iki eğik kaya, S kıvrımlı dizi.
            case 31:
                Set(level, 3.8f, 5); level.slope = new Vector2(1.0f, 0f);
                level.marbles = Kind(Join(Line(-2.3f, -.1f, -.7f, .9f, 4), Line(-.2f, 1.2f, 1.0f, 2.8f, 4), Line(1.6f, 1.0f, 2.4f, .2f, 3)), MarbleKind.Ice, 3, 5, 9);
                level.obstacles = new[] { O(-1.7f, 2.0f, 1.2f, .45f, 30f), O(2.3f, 2.3f, 1.1f, .45f, -20f) };
                level.zones = Zones(Sand(0f, -1.45f, .6f));
                break;

            // 09 Dik Yamaç — iki sütun yokuş yukarı, tepede buz; eğim en dik.
            case 32:
                Set(level, 3.8f, 4); level.slope = new Vector2(0f, -1.2f);
                level.marbles = Kind(Join(Line(-.4f, 0f, -.4f, 2.6f, 5), Line(.4f, .3f, .4f, 2.9f, 5)), MarbleKind.Ice, 4, 9);
                break;

            // 10 Son Tepe — V dizisi, uçlarda ve tepede buz; içi kumlu.
            case 33:
                Set(level, 3.9f, 4); level.slope = new Vector2(-1.1f, .3f);
                level.marbles = Kind(Join(new[] { P(0f, .2f) }, Line(-.55f, .85f, -2.1f, 2.6f, 4), Line(.55f, .85f, 2.1f, 2.6f, 4)), MarbleKind.Ice, 0, 4, 8);
                level.zones = Zones(Sand(0f, 1.95f, .6f));
                break;

            // 11 Yayla Şenliği — halka + ortada buz üçgeni.
            case 34:
                Set(level, 3.9f, 5); level.slope = new Vector2(.8f, -.6f);
                level.marbles = Join(Ring(0f, 1.0f, 1.85f, 10, 0f), Kind(Tri(0f, 1.0f, .64f), MarbleKind.Ice));
                level.zones = Zones(Sand(-2.7f, 2.3f, .55f));
                break;

            // 12 Ustalık Sınavı — elmas kafes, beş buz, iki kaya huni gibi.
            case 35:
                Set(level, 4.0f, 4); level.starsToPass = 2; level.slope = new Vector2(1.0f, -.5f);
                level.marbles = Kind(Diamond(0f, 1.3f, .66f), MarbleKind.Ice, 0, 3, 6, 9, 12);
                level.obstacles = new[] { O(-2.2f, .1f, 1.2f, .4f, 40f), O(2.2f, .1f, 1.2f, .4f, -40f) };
                break;

            // ================= KASABA PAZARI · tezgâh koridorları + karpuz =================

            // 01 Pazar Girişi — iki tezgâh, üç şerit. Bant atışı.
            case 36:
                Set(level, 3.6f, 5);
                level.marbles = Join(Line(-1.8f, .2f, -1.8f, 1.6f, 3), Line(0f, .5f, 0f, 1.9f, 3), Line(1.8f, .2f, 1.8f, 1.6f, 3));
                level.obstacles = new[] { O(-.9f, 1.0f, .35f, 2.4f), O(.9f, 1.0f, .35f, 2.4f) };
                break;

            // 02 Karpuz Tezgâhı — karpuz misket tek başına: sert vur, ikiye bölünsün.
            case 37:
                Set(level, 3.6f, 5);
                level.marbles = Join(Kind(Row(.9f, -1.25f, 1.25f, 3), MarbleKind.Split), Row(2.05f, -1.8f, 1.8f, 4));
                break;

            // 03 Kasa Yığını — dama tahtası kasalar, aralarda misket, ortada karpuz.
            case 38:
                Set(level, 3.7f, 5);
                level.marbles = Join(P(0f, .6f), P(0f, 2.0f), P(-.8f, 1.3f), P(.8f, 1.3f), P(-1.9f, .6f), P(1.9f, .6f), P(-1.9f, 2.0f), P(1.9f, 2.0f),
                                     Kind(new[] { P(0f, 1.3f) }, MarbleKind.Split));
                level.obstacles = new[] { O(-.85f, .55f, .55f, .55f), O(.85f, .55f, .55f, .55f), O(-.85f, 2.05f, .55f, .55f), O(.85f, 2.05f, .55f, .55f) };
                break;

            // 04 Domates Sırası — üç sıra, aralarında ince tezgâhlar, orta sıra karpuz.
            case 39:
                Set(level, 3.7f, 5);
                level.marbles = Join(Row(.1f, -1.4f, 1.4f, 5), Kind(Row(1.05f, -1.0f, 1.0f, 3), MarbleKind.Split), Row(2.0f, -1.4f, 1.4f, 5));
                level.obstacles = new[] { O(-.6f, .58f, 1.7f, .28f), O(.7f, 1.52f, 1.7f, .28f) };
                break;

            // 05 Terazi Önü — T biçimli terazi, iki kefe, kefelerde karpuz.
            case 40:
                Set(level, 3.8f, 5);
                level.marbles = Join(Kind(Diamond4(-1.55f, 1.55f, .6f), MarbleKind.Split, 4), Kind(Diamond4(1.55f, 1.55f, .6f), MarbleKind.Split, 4));
                level.obstacles = new[] { O(0f, 1.2f, .35f, 1.6f), O(0f, 2.15f, 2.0f, .35f) };
                break;

            // 06 Tezgâh Arası — zikzak koridor.
            case 41:
                Set(level, 3.8f, 5);
                level.marbles = Join(Row(.85f, -1.8f, 1.8f, 4), Row(1.85f, -1.8f, 1.8f, 4), Kind(Spots(-2.6f, 1.35f, 2.6f, 1.35f), MarbleKind.Split));
                // Ölçüldü: 2 birimlik uzun tezgâhlarla karpuz 1 sayılınca 7 atışta bile %40,
                // her atış tek misket (rota). Tezgâhlar kısaldı ve yana çekildi, orta koridor açık.
                level.obstacles = new[] { O(-1.55f, .3f, 1.2f, .35f), O(1.55f, 1.35f, 1.2f, .35f), O(-1.55f, 2.4f, 1.2f, .35f) };
                break;

            // 07 Sepetçi — üç sepet ağzı, arkalarında üçlüler, ortada karpuz.
            case 42:
                Set(level, 3.8f, 5);
                level.marbles = Join(Tri(-1.6f, 1.15f, .6f), Kind(Tri(0f, 2.15f, .6f), MarbleKind.Split, 0), Tri(1.6f, 1.15f, .6f));
                level.obstacles = new[] { O(-1.6f, .3f, .9f, .3f), O(0f, 1.3f, .9f, .3f), O(1.6f, .3f, .9f, .3f) };
                break;

            // 08 Bakır Tezgâhı — elmas tezgâhın çevresinde halka, iki yanda duvar.
            case 43:
                Set(level, 3.9f, 5);
                level.marbles = Kind(Ring(0f, 1.2f, 1.4f, 9, 10f), MarbleKind.Split, 2, 6);
                level.obstacles = new[] { O(0f, 1.2f, .7f, .7f, 45f), O(-2.65f, 1.2f, .35f, 1.8f), O(2.65f, 1.2f, .35f, 1.8f) };
                break;

            // 09 Kalabalık Köşe — sağ arka köşede kalabalık, önünde eğik duvar.
            case 44:
                Set(level, 3.9f, 4);
                level.marbles = Kind(Grid(1.25f, 1.75f, 3, 4, .64f, .64f), MarbleKind.Split, 4, 7);
                level.obstacles = new[] { O(-.35f, 1.2f, .35f, 2.2f, 20f) };
                break;

            // 10 Son Tezgâh — uzun masa önde, arkasında sıra; uçlardan dolan.
            case 45:
                Set(level, 4.0f, 4);
                level.marbles = Join(Kind(Row(1.55f, -1.9f, 1.9f, 7), MarbleKind.Split, 1, 5), P(-2.5f, -.3f), P(2.5f, -.3f), P(-2.9f, .55f), P(2.9f, .55f));
                level.obstacles = new[] { O(0f, .8f, 3.2f, .35f) };
                break;

            // 11 Pazar Buluşması — iki duvar, üç bölme; önde kum, arkada çamur.
            case 46:
                Set(level, 4.0f, 5);
                level.marbles = Join(Kind(Line(0f, .2f, 0f, 2.2f, 4), MarbleKind.Split, 2), Tri(-2.2f, 1.0f, .62f), Tri(2.2f, 1.0f, .62f), P(-2.4f, 2.6f), P(2.4f, 2.6f));
                level.obstacles = new[] { O(-1.3f, 1.2f, .35f, 2.0f), O(1.3f, 1.2f, .35f, 2.0f) };
                level.zones = Zones(Sand(0f, -1.25f, .6f), Mud(0f, 2.95f, .55f));
                break;

            // 12 Ustalık Sınavı — tezgâh labirenti, üç karpuz.
            case 47:
                Set(level, 4.0f, 4); level.starsToPass = 2;
                // Duvarlar sırayla sol/sağ; misketler aradaki üç şeritte. Duvar ince (0.25) ki
                // şeritteki misket duvara değmesin (0.4 boşluk > 0.25 yarıçap + 0.125).
                level.marbles = Kind(Join(Row(.6f, -1.8f, 1.6f, 4), Row(1.4f, -1.5f, 1.8f, 4), Row(2.2f, -1.6f, 1.7f, 4)),
                                    MarbleKind.Split, 1, 6, 10);
                // Ölçüldü: dört uzun duvarla tavan 7 atışta %27. Duvarlar kısaltıldı, en arkadaki kalktı.
                level.obstacles = new[] { O(-1.4f, .2f, 1.3f, .25f), O(1.4f, 1.0f, 1.3f, .25f), O(-1.4f, 1.8f, 1.3f, .25f) };
                break;

            // ================= BAYRAM YERİ · çukur + hepsi =================

            // 01 Bayram Sabahı — çukur tek başına: yavaş kalan misket düşer.
            case 48:
                Set(level, 3.6f, 5);
                level.marbles = Join(Ring(0f, .6f, .66f, 6, 0f), P(0f, .6f), P(-1.8f, 1.9f), P(1.8f, 1.9f));
                level.zones = Zones(Pit(0f, 1.95f, .4f));
                break;

            // 02 Salıncak Yeri — salıncak direkleri, dış yanlarda çukur.
            case 49:
                Set(level, 3.7f, 5);
                level.marbles = Join(Line(0f, 0f, 0f, 2.4f, 5), P(-2.0f, .3f), P(2.0f, .3f), P(-1.2f, 2.9f), P(1.2f, 2.9f));
                level.obstacles = new[] { O(-1.2f, 1.2f, .3f, 1.8f, 15f), O(1.2f, 1.2f, .3f, 1.8f, -15f) };
                level.zones = Zones(Pit(-2.3f, 1.9f, .4f), Pit(2.3f, 1.9f, .4f));
                break;

            // 03 Pamuk Şeker — üç pamuk, ortalarında çukur; giriş kumlu.
            case 50:
                Set(level, 3.7f, 5);
                level.marbles = Join(Diamond4(-1.45f, 1.0f, .6f), Diamond4(1.45f, 1.0f, .6f), Diamond4(0f, 2.35f, .6f));
                level.zones = Zones(Pit(0f, 1.05f, .4f), Sand(0f, -1.25f, .6f));
                break;

            // 04 Atlıkarınca — dönen direğin çevresinde halka, önde çukur, iki buz.
            case 51:
                Set(level, 3.8f, 5);
                level.marbles = Kind(Ring(0f, 1.15f, 1.35f, 10, 18f), MarbleKind.Ice, 2, 7);
                level.obstacles = new[] { O(0f, 1.15f, .8f, .8f, 45f) };
                level.zones = Zones(Pit(0f, -.95f, .35f));
                break;

            // 05 Davul Zurna — iki yay, iç yayda karpuz davullar, merkezde çukur.
            case 52:
                Set(level, 3.8f, 5);
                // Ölçüldü: iç yayda 5 misketle 3 atışta %64 (Bayram için kolay) ve sıkıştırmaya yer yoktu; iç yay 4 misket.
                level.marbles = Join(Kind(ArcAt(0f, .7f, 1.25f, 40f, 140f, 4), MarbleKind.Split, 1, 3), ArcAt(0f, .7f, 2.3f, 30f, 150f, 7));
                level.zones = Zones(Pit(0f, .7f, .4f));
                break;

            // 06 Bayram Harçlığı — dağınık çiftler; iki çukur, çamur ve kum.
            case 53:
                Set(level, 3.8f, 5);
                // İkisi çamurda, biri kumda: saplanan harçlık (ölçüldü, onlarsız 3 atışta %60).
                level.marbles = Spots(-2.2f, -.3f, -1.7f, .1f, -.5f, .5f, 0f, .9f, .8f, 1.4f, 1.3f, 1.05f, -.3f, 2.1f, .2f, 2.55f, 2.1f, 2.3f, 2.5f, 1.8f,
                                      -1.9f, 1.1f, -1.35f, .8f, 1.3f, 2.95f);
                // Ölçüldü: 0.4 çukurlarla 3 atışta %60 (Bayram için kolay); çukurlar büyüdü.
                level.zones = Zones(Pit(-1.25f, 2.4f, .55f), Pit(1.45f, .15f, .55f), Mud(-1.65f, .95f, .6f), Sand(1.3f, 2.9f, .5f));
                break;

            // 07 Fener Alayı — uzun yay, üç buz fener, ortada çukur, hafif eğim.
            case 54:
                Set(level, 3.9f, 5); level.slope = new Vector2(.6f, 0f);
                level.marbles = Kind(Arc(2.5f, 20f, 160f, 9), MarbleKind.Ice, 1, 4, 7);
                level.zones = Zones(Pit(0f, .65f, .45f));
                break;

            // 08 Lunapark — 3x4 ızgara, iki buz iki karpuz; tamponlar, arkada çukur.
            case 55:
                Set(level, 3.9f, 5);
                level.marbles = Kind(Kind(Grid(0f, 1.25f, 3, 4, .7f, .7f), MarbleKind.Ice, 1, 10), MarbleKind.Split, 4, 8);
                level.obstacles = new[] { O(-2.2f, .4f, .5f, .5f, 45f), O(2.2f, .4f, .5f, .5f, 45f) };
                level.zones = Zones(Pit(0f, 3.05f, .4f));
                break;

            // 09 Gece Çukuru — üç çukur, halka ve dış üçlü; önde çamur.
            case 56:
                Set(level, 4.0f, 4);
                level.marbles = Join(Ring(0f, 1.3f, .9f, 8, 22.5f), P(0f, 1.3f), P(-2.6f, 2.3f), P(2.6f, 2.3f), P(0f, -.7f));
                level.zones = Zones(Pit(-1.6f, .7f, .42f), Pit(1.6f, .7f, .42f), Pit(0f, 2.75f, .42f), Mud(0f, -1.65f, .5f));
                break;

            // 10 Son Bayram — çarpı dizisi, uçlarda buz, merkezde karpuz; eğim, çukur, çamur, kum.
            case 57:
                Set(level, 4.0f, 4); level.slope = new Vector2(-.8f, -.4f);
                level.marbles = Kind(Kind(XShape(0f, 1.3f, 2, .66f), MarbleKind.Ice, 1, 4), MarbleKind.Split, 0);
                level.zones = Zones(Pit(2.4f, 2.6f, .4f), Mud(-2.4f, 2.6f, .5f), Sand(0f, -.9f, .6f));
                break;

            // 11 Büyük Buluşma — 15'lik ıstaka, üç buz iki karpuz, iki yanda çukur.
            case 58:
                Set(level, 4.0f, 5); level.slope = new Vector2(0f, -.5f);
                level.marbles = Kind(Kind(Rack(0f, 1.3f, 5, .62f), MarbleKind.Ice, 0, 10, 14), MarbleKind.Split, 6, 8);
                level.zones = Zones(Pit(-2.35f, 1.4f, .4f), Pit(2.35f, 1.4f, .4f));
                break;

            // 12 Ustalık Sınavı — FİNAL. Sarmal, merkezde karpuz, yolunda buzlar;
            // iki çukur, çamur, kum ve eğim.
            case 59:
                Set(level, 4.0f, 4); level.starsToPass = 2; level.slope = new Vector2(.7f, 0f);
                level.marbles = Kind(Kind(Spiral(0f, 1.3f, 14, .45f, .145f, 40f), MarbleKind.Split, 0), MarbleKind.Ice, 4, 8, 12);
                level.zones = Zones(Pit(-2.3f, 2.8f, .4f), Pit(2.5f, -.4f, .4f), Mud(0f, 3.35f, .45f), Sand(0f, -1.35f, .5f));
                break;
        }
        Vary(level, index);
        Harden(level, index);
        Tune(level, index);
    }

    // ---------------------------------------------------------------
    // ÇEŞİTLİLİK AYARI: bölümün bütününü (misket, engel, bölge, eğim) çember
    // merkezi etrafında döndürür ve kaydırır. Değerler MemleketVarietyTuner'ın
    // bulduğu en küçük dokunuş: her Memleket bölümü, iki haritadaki bütün
    // bölümlerden en az 0.30 farklı olsun (LevelVarietyVerify ölçüsü).
    // Satır: { derece, dx, dz }
    public static System.Func<int, Vector3> VariationOverride;
    public static Vector3 VariationOf(int index) => Variation.TryGetValue(index, out var v) ? v : Vector3.zero;
    private static readonly Dictionary<int, Vector3> Variation = new Dictionary<int, Vector3>
    {
        { 0, new Vector3(15f, 0f, 0.25f) },
        { 2, new Vector3(0f, 0.45f, 0f) },
        { 4, new Vector3(0f, 0f, 0.25f) },
        { 6, new Vector3(-8f, 0f, 0f) },
        { 8, new Vector3(-8f, 0f, 0f) },
        { 12, new Vector3(8f, 0f, 0.25f) },
        { 15, new Vector3(0f, 0f, -0.45f) },
        { 16, new Vector3(8f, 0f, -0.25f) },
        { 19, new Vector3(-15f, 0f, 0f) },
        { 22, new Vector3(-8f, 0f, 0f) },
        { 25, new Vector3(22f, -0.25f, 0f) },
        { 27, new Vector3(0f, 0f, 0.25f) },
        { 29, new Vector3(-40f, 0f, -0.25f) },
        { 30, new Vector3(-8f, 0f, 0f) },
        { 34, new Vector3(-8f, 0f, 0f) },
        { 35, new Vector3(-8f, 0.45f, 0f) },
        { 40, new Vector3(0f, 0f, 0.25f) },
        { 42, new Vector3(0f, 0f, 0.25f) },
        { 43, new Vector3(-22f, 0f, 0f) },
        { 48, new Vector3(-8f, 0f, 0f) },
        { 50, new Vector3(-15f, 0f, 0f) },
        { 51, new Vector3(-8f, 0f, 0f) },
        { 52, new Vector3(-15f, 0f, 0f) },
        { 54, new Vector3(-22f, 0f, 0f) },
        { 55, new Vector3(0f, 0f, 0.45f) },
        { 58, new Vector3(0f, 0.45f, 0.45f) },
    };

    // ---------------------------------------------------------------
    // SERTLEŞTİRME (Faz 6 ölçümünden): 3 atışta bile hedefin 10+ puan üstünde temizlenen
    // bölümler. Saha büyür, bölümün bütünü merkeze doğru sıkışır: misketin çıkmak için
    // gideceği yol uzar, dizilimin biçimi aynı kalır. Satır: { saha, sıkıştırma }
    private static readonly Dictionary<int, Vector2> Hardening = new Dictionary<int, Vector2>
    {
        { 1, new Vector2(4.0f, .9f) },  { 3, new Vector2(4.0f, .9f) },  { 4, new Vector2(4.0f, .85f) },
        { 12, new Vector2(4.0f, .9f) }, { 14, new Vector2(4.0f, .9f) }, { 16, new Vector2(4.0f, .85f) },
        { 24, new Vector2(4.0f, .85f) }, { 25, new Vector2(4.0f, .85f) }, { 27, new Vector2(4.0f, .9f) },
        { 29, new Vector2(4.0f, .9f) }, { 31, new Vector2(4.0f, .9f) }, { 32, new Vector2(4.0f, .9f) },
        { 33, new Vector2(4.0f, .9f) }, { 34, new Vector2(4.0f, .9f) }, { 35, new Vector2(4.0f, .95f) },
        { 37, new Vector2(4.0f, .85f) }, { 40, new Vector2(4.0f, .9f) },
        // Bayram Yeri ikinci tur: ilk ayarda Pazar'dan kolay kalmıştı (ort. %56 / %48).
        { 48, new Vector2(4.0f, .85f) }, { 49, new Vector2(4.0f, .85f) }, { 50, new Vector2(4.0f, .85f) },
        { 51, new Vector2(4.0f, .85f) }, { 52, new Vector2(4.0f, .85f) }, { 53, new Vector2(4.0f, .85f) },
        { 54, new Vector2(4.0f, .85f) }, { 55, new Vector2(4.0f, .85f) }, { 56, new Vector2(4.0f, .85f) },
        { 57, new Vector2(4.0f, .85f) }, { 58, new Vector2(4.0f, .88f) }, { 59, new Vector2(4.0f, .88f) },
    };

    private static void Harden(LevelData level, int index)
    {
        if (!Hardening.TryGetValue(index, out var h)) return;
        level.arenaSize = Mathf.Max(level.arenaSize, h.x);
        float f = h.y;
        if (level.marbles != null) for (int i = 0; i < level.marbles.Length; i++) { level.marbles[i].x *= f; level.marbles[i].z *= f; }
        if (level.obstacles != null) for (int i = 0; i < level.obstacles.Length; i++) { level.obstacles[i].x *= f; level.obstacles[i].z *= f; }
        if (level.zones != null) for (int i = 0; i < level.zones.Length; i++) { level.zones[i].x *= f; level.zones[i].z *= f; }
    }

    private static void Vary(LevelData level, int index)
    {
        Vector3 v;
        if (VariationOverride != null) v = VariationOverride(index);
        else if (!Variation.TryGetValue(index, out v)) return;
        if (v == Vector3.zero) return;
        var rot = Quaternion.Euler(0f, -v.x, 0f);   // +derece = saat yönünün tersi (üstten)
        Vector2 Move(float x, float z) { var p = rot * new Vector3(x, 0f, z); return new Vector2(p.x + v.y, p.z + v.z); }
        if (level.marbles != null)
            for (int i = 0; i < level.marbles.Length; i++)
            { var p = Move(level.marbles[i].x, level.marbles[i].z); level.marbles[i].x = p.x; level.marbles[i].z = p.y; }
        if (level.obstacles != null)
            for (int i = 0; i < level.obstacles.Length; i++)
            { var p = Move(level.obstacles[i].x, level.obstacles[i].z); level.obstacles[i].x = p.x; level.obstacles[i].z = p.y; level.obstacles[i].angle -= v.x; }   // engel de misketlerle aynı dönüşü alır
        if (level.zones != null)
            for (int i = 0; i < level.zones.Length; i++)
            { var p = Move(level.zones[i].x, level.zones[i].z); level.zones[i].x = p.x; level.zones[i].z = p.y; }
        if (level.slope.sqrMagnitude > 0f)
        { var s = rot * new Vector3(level.slope.x, 0f, level.slope.y); level.slope = new Vector2(s.x, s.z); }
    }

    // ---------------------------------------------------------------
    // ATIŞ HAKKI VE YILDIZ HEDEFLERİ — Faz 6 ölçümünden (MemleketPhysicsVerify).
    // Satır: { atış hakkı, o atış hakkıyla ölçülen TAVAN (normal misket, açgözlü arama) }.
    // Hedefler tavandan türetilir, böylece "geçilemeyen bölüm yok" kuruluşundan doğru:
    //   1 yıldız = tavanın %60'ı (yukarı yuvarlanır)  -> normal misketle her zaman mümkün
    //   2 yıldız = ustalık sınavında TAVAN (kilit normal misketle açılır, pay yok),
    //              diğerlerinde tavanın %85'i
    //   3 yıldız = bütün misketler (tavanın üstündeyse güç / özel misket ister)
    // İsteğe bağlı üçüncü sayı: 2 yıldız hedefini elle koyar (telefonda denenip düşürülenler).
    // Tabloda olmayan bölüm formülle (1y %40, 2y %60) kalır.
    private static readonly Dictionary<int, int[]> Tuning = new Dictionary<int, int[]>
    {
        { 0, new[] { 2, 5 } },
        { 1, new[] { 3, 4 } },
        { 2, new[] { 5, 6 } },
        { 3, new[] { 3, 6 } },
        { 4, new[] { 3, 5 } },
        { 5, new[] { 3, 6 } },
        { 6, new[] { 5, 7 } },
        { 7, new[] { 5, 5 } },
        { 8, new[] { 4, 6 } },
        { 9, new[] { 3, 6 } },
        { 10, new[] { 3, 6 } },
        { 11, new[] { 5, 7, 6 } },   // 2y elle 6: kullanıcı telefonda 7'yi zor buldu (2026-09-24)
        { 12, new[] { 3, 6 } },
        { 13, new[] { 3, 5 } },
        { 14, new[] { 4, 6 } },
        { 15, new[] { 5, 4 } },
        { 16, new[] { 3, 4 } },
        { 17, new[] { 6, 5 } },
        { 18, new[] { 4, 7 } },
        { 19, new[] { 4, 6 } },
        { 20, new[] { 4, 5 } },
        { 21, new[] { 3, 5 } },
        { 22, new[] { 3, 7 } },
        { 23, new[] { 6, 7 } },
        { 24, new[] { 2, 5 } },
        { 25, new[] { 3, 5 } },
        { 26, new[] { 2, 4 } },
        { 27, new[] { 3, 4 } },
        { 28, new[] { 4, 5 } },
        { 29, new[] { 4, 6 } },
        { 30, new[] { 4, 6 } },
        { 31, new[] { 4, 6 } },
        { 32, new[] { 4, 5 } },
        { 33, new[] { 3, 5 } },
        { 34, new[] { 4, 6 } },
        { 35, new[] { 6, 6 } },
        { 36, new[] { 3, 5 } },
        { 37, new[] { 2, 3 } },
        { 38, new[] { 3, 4 } },
        { 39, new[] { 6, 6 } },
        { 40, new[] { 3, 5 } },
        { 41, new[] { 4, 5 } },
        { 42, new[] { 3, 5 } },
        { 43, new[] { 3, 5 } },
        { 44, new[] { 3, 6 } },
        { 45, new[] { 3, 5 } },
        { 46, new[] { 3, 6 } },
        { 47, new[] { 5, 5 } },
        { 48, new[] { 3, 4 } },
        { 49, new[] { 3, 4 } },
        { 50, new[] { 4, 8 } },
        { 51, new[] { 4, 6 } },
        { 52, new[] { 3, 6 } },
        { 53, new[] { 3, 6 } },
        { 54, new[] { 3, 5 } },
        { 55, new[] { 4, 6 } },
        { 56, new[] { 3, 5 } },
        { 57, new[] { 3, 4 } },
        { 58, new[] { 6, 7 } },
        { 59, new[] { 4, 6 } },
    };

    // Ölçülen tavan; ölçülmemişse -1. DifficultyOrderVerify zorluk sırasını bununla denetler.
    public static int MeasuredCeiling(int index) => Tuning.TryGetValue(index, out var t) ? t[1] : -1;

    private static void Tune(LevelData level, int index)
    {
        int total = level.TotalMarbles();
        if (!Tuning.TryGetValue(index, out var t))
        {
            level.oneStarTarget = Mathf.Max(1, Mathf.CeilToInt(total * .4f));
            level.twoStarTarget = Mathf.Max(level.oneStarTarget, Mathf.CeilToInt(total * .6f));
            level.threeStarTarget = total;
            return;
        }
        level.shotCount = t[0];
        int ceiling = Mathf.Clamp(t[1], 1, total);
        level.threeStarTarget = total;
        level.oneStarTarget = Mathf.Max(1, Mathf.CeilToInt(ceiling * .6f));
        level.twoStarTarget = level.starsToPass >= 2 ? ceiling : Mathf.Max(level.oneStarTarget, Mathf.RoundToInt(ceiling * .85f));
        if (t.Length > 2) level.twoStarTarget = t[2];
        // Kilit ile ustalık ayrı sayı olmalı (DifficultyOrderVerify kuralı).
        if (level.twoStarTarget >= level.threeStarTarget) level.twoStarTarget = level.threeStarTarget - 1;
        if (level.oneStarTarget > level.twoStarTarget) level.oneStarTarget = level.twoStarTarget;
    }

    // ---------------------------------------------------------------
    // Yardımcılar

    private static void Set(LevelData level, float size, int shots)
    {
        level.shape = ArenaShape.Circle;
        level.arenaSize = size;
        level.shotCount = shots;
        level.starsToPass = 1;
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
    private static ObstacleSpot O(float x, float z, float w, float d, float angle = 0f) => new ObstacleSpot(x, z, w, d, angle);
    private static ZoneSpot Sand(float x, float z, float r) => new ZoneSpot(ZoneKind.Sand, x, z, r);
    private static ZoneSpot Mud(float x, float z, float r) => new ZoneSpot(ZoneKind.Mud, x, z, r);
    private static ZoneSpot Pit(float x, float z, float r) => new ZoneSpot(ZoneKind.Pit, x, z, r);
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

    private static MarbleSpot[] Spots(params float[] pairs)
    {
        var list = new MarbleSpot[pairs.Length / 2];
        for (int i = 0; i < list.Length; i++) list[i] = new MarbleSpot(pairs[i * 2], pairs[i * 2 + 1]);
        return list;
    }

    // Seçilen sıradaki misketleri (boşsa hepsini) özel yapar.
    private static MarbleSpot[] Kind(MarbleSpot[] spots, MarbleKind kind, params int[] which)
    {
        var copy = (MarbleSpot[])spots.Clone();
        if (which == null || which.Length == 0) for (int i = 0; i < copy.Length; i++) copy[i].kind = kind;
        else foreach (int i in which) if (i >= 0 && i < copy.Length) copy[i].kind = kind;
        return copy;
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

    private static MarbleSpot[] Arc(float r, float from, float to, int n) => ArcAt(0f, 0f, r, from, to, n);

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

    // Üçlü küme: ucu atıcıya bakan üçgen.
    private static MarbleSpot[] Tri(float cx, float cz, float gap)
    {
        float h = gap * .866f;
        return new[] { P(cx, cz - h * .66f), P(cx - gap * .5f, cz + h * .34f), P(cx + gap * .5f, cz + h * .34f) };
    }

    // Dörtlü elmas.
    private static MarbleSpot[] Diamond4(float cx, float cz, float gap)
        => new[] { P(cx, cz - gap), P(cx - gap, cz), P(cx + gap, cz), P(cx, cz + gap), P(cx, cz) };

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

    // Zikzak: yatay (x0→x1, z0 ve z1 arasında gidip gelir) ya da dikey (vertical=true: x0/x1 arası, z0→z1).
    private static MarbleSpot[] Zig(float a0, float a1, int n, float b0, float b1, bool vertical = false)
    {
        var list = new MarbleSpot[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            float along = Mathf.Lerp(vertical ? b0 : a0, vertical ? b1 : a1, t);
            float across = (i % 2 == 0) ? (vertical ? a0 : b0) : (vertical ? a1 : b1);
            list[i] = vertical ? P(across, along) : P(along, across);
        }
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

    // Arşimet sarmalı: ilk misket merkezde, sonra yarıçap her misketle büyür.
    // Ardışık misketler arası ~0.6 kalsın diye açı adımı yarıçapa göre küçülür.
    private static MarbleSpot[] Spiral(float cx, float cz, int n, float r0, float growPerStep, float phase)
    {
        var list = new List<MarbleSpot> { P(cx, cz) };
        float a = phase * Mathf.Deg2Rad, r = r0 + .15f;
        for (int i = 1; i < n; i++)
        {
            list.Add(P(cx + Mathf.Cos(a) * r, cz + Mathf.Sin(a) * r));
            a += .62f / r;
            r += growPerStep;
        }
        return list.ToArray();
    }
}
