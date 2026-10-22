# Misko — LinkedIn Gönderisi

## Gönderi metni (İngilizce)

This week I stopped guessing how hard my levels were.

Misko is a mobile adaptation of the traditional Turkish marble game: knock the marbles out of the chalk circle. Five neighbourhoods, twelve levels each. Sixty levels, and every one of them needs a star target that feels fair.

The obvious way to set those targets is intuition. Play a level a few times, pick a number that feels right. I tried that first. It produced levels that were quietly impossible next to levels that were over before they started.

So I built a measurement tool instead. It plays every level in the editor, searching for the best sequence of shots the physics actually allows, and reports the ceiling: the maximum number of marbles that can be knocked out of that circle. Every star target in the game is now derived from a measured ceiling rather than a guess.

Two findings I did not expect.

The ceiling turned out to be a reasonable proxy for a strong human player. On one of the harder levels the tool cleared nine marbles. My own best was eight.

And the lever that actually controls difficulty is the number of obstacles, not the size of the circle. Enlarging the ring makes a level easier, not harder — it pushes the marbles closer to the edge they need to cross.

Difficulty you can measure is difficulty you can design. The alternative is shipping a guess.

Misko is coming to the App Store.

---

## Görsel prompt'ları (ChatGPT'ye yapıştır)

Üçü de aynı gönderiye uyar, farklı hisler verir. Birini seç ya da üçünü de üretip karşılaştır.

### 1 — Editoryal: çember + veri katmanı
*Gönderinin ana fikrini (ölçüm) en doğrudan anlatan seçenek.*

```
Editorial-style illustration for a professional software engineering article.
A top-down view of a chalk circle drawn on grey asphalt, with a cluster of
glass marbles arranged inside it in a triangle formation. Overlaid on the
scene are subtle technical measurement elements: thin white dashed trajectory
arcs showing predicted shot paths, faint coordinate grid lines, and small
numeric annotations near individual marbles. Muted, restrained colour
palette: grey asphalt, soft white chalk, and marbles in amber, deep blue and
green. Clean negative space at the top of the frame. Soft, even daylight, no
harsh shadows. Photorealistic textures on asphalt and glass, but the overlay
graphics are flat, precise and diagrammatic — the contrast between the
physical scene and the data layer is the point of the image. Landscape
orientation, 1200x628 pixels, no text, no logos, no watermarks.
```

### 2 — Çalışma masası: solo geliştirici
*Daha insani, daha "süreç" hissi. Kişisel hikâye anlatan gönderilerde etkileşimi yüksek olur.*

```
A clean, well-composed photograph of a solo game developer's desk, shot from
a slight overhead angle. A laptop displays a 3D game editor with a circular
level layout and a debug overlay of numbers and graphs. Next to it, a
smartphone stands upright showing the same game running: a chalk circle with
marbles on asphalt, seen from above. A small open notebook beside them holds
hand-drawn circle diagrams and arrows. A few real glass marbles rest on the
desk surface. Natural window light from the left, warm neutral tones, shallow
depth of field with the phone screen in sharp focus. Calm and professional,
not cluttered. Landscape orientation, 1200x628 pixels, no text overlays, no
logos, no visible brand names.
```

### 3 — Soyut veri görseli
*En "tech LinkedIn" olanı. Oyunu göstermez, metodolojiyi gösterir. Akışta dikkat çeker.*

```
A minimal data visualisation poster on a deep charcoal background. Sixty
small circles arranged in five horizontal rows of twelve, each circle
representing a game level. Inside each circle, a thin arc is filled to a
different degree, like a progress ring, showing a measured value. The rows
grow progressively fuller from top to bottom. A single accent colour — warm
amber — against otherwise white and grey linework. Generous negative space.
Flat vector style, precise geometry, no gradients, no skeuomorphism.
Landscape orientation, 1200x628 pixels, no text, no numbers, no logos.
```

---

## Notlar

- LinkedIn'de görsel için ideal ölçü **1200x628 px** (yatay). Kare (1080x1080) de çalışır ve mobilde daha çok yer kaplar.
- Prompt'larda "no text" yazma sebebi: yapay zeka görsellerinde yazı neredeyse her zaman bozuk çıkıyor. Yazı isteyeceksen sonradan Canva'da ekle.
- Alternatif: oyunun **gerçek ekran görüntüsü**. Gönderi ölçüm ve testten bahsettiği için, gerçek bir bölüm ekranı ya da test aracının çıktısı üretilmiş bir görselden daha inandırıcı durur. Üretilmiş görselle yan yana koyup karşılaştırmanı öneririm.
