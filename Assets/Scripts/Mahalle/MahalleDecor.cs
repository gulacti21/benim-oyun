using UnityEngine;
using UnityEngine.UI;

// Haritanın kenarındaki mahalle eşyaları. Hepsi kodla çizilir, hiçbiri çarpışmaz,
// hiçbiri yolun veya durakların üstüne gelmez.
[RequireComponent(typeof(CanvasRenderer))]
public class MahalleDecor : MaskableGraphic
{
    public DecorKind kind;
    [Tooltip("Bir taslak biriminin kaç piksel olduğu.")]
    public float unit = 3f;
    [Tooltip("Tebeşirle çizilen dekorlar bu rengi kullanır.")]
    public Color chalk = Color.white;

    private static readonly Color Shade   = new Color(.16f, .11f, .04f, .26f);
    private static readonly Color Trunk   = new Color(.30f, .21f, .13f);
    private static readonly Color Leaf1   = new Color(.23f, .35f, .20f);
    private static readonly Color Leaf2   = new Color(.19f, .31f, .17f);
    private static readonly Color Leaf3   = new Color(.27f, .42f, .23f);
    private static readonly Color Leaf4   = new Color(.32f, .48f, .26f);
    private static readonly Color Wood    = new Color(.49f, .29f, .14f);
    private static readonly Color WoodLit = new Color(.55f, .34f, .16f);
    private static readonly Color Iron    = new Color(.17f, .23f, .19f);
    private static readonly Color Stone   = new Color(.61f, .57f, .50f);
    private static readonly Color StoneLit= new Color(.68f, .64f, .57f);
    private static readonly Color Water   = new Color(.43f, .56f, .58f);
    private static readonly Color Concrete= new Color(.73f, .68f, .58f);
    private static readonly Color Door    = new Color(.36f, .29f, .21f);
    private static readonly Color DoorLit = new Color(.48f, .39f, .28f);
    private static readonly Color Terra   = new Color(.66f, .36f, .20f);
    private static readonly Color TerraLit= new Color(.74f, .42f, .24f);
    private static readonly Color Fur     = new Color(.23f, .20f, .16f);
    private static readonly Color Board   = new Color(.91f, .89f, .83f);
    private static readonly Color Rim     = new Color(.69f, .29f, .20f);
    private static readonly Color Net     = new Color(.93f, .90f, .82f);
    private static readonly Color Pole    = new Color(.29f, .31f, .35f);
    private static readonly Color Rubber  = new Color(.17f, .16f, .16f);
    private static readonly Color Bird    = new Color(.37f, .38f, .41f);
    private static readonly Color BirdLit = new Color(.42f, .43f, .46f);
    private static readonly Color Beak    = new Color(.79f, .54f, .23f);
    private static readonly Color Glass   = new Color(.81f, .89f, .90f, .78f);
    private static readonly Color Bulb    = new Color(.91f, .71f, .36f);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Vector2 o = rectTransform.rect.center;
        float u = Mathf.Max(.01f, unit);

        Vector2 P(float x, float y) { return o + new Vector2(x * u, y * u); }
        void Box(float x, float y, float w, float h, Color col)
        { MahalleGraphic.Rounded(vh, new Rect(o.x + x * u, o.y + y * u, w * u, h * u), 1.4f * u, col); }
        void Oval(float x, float y, float rx, float ry, Color col)
        { MahalleGraphic.Disc(vh, P(x, y), rx * u, ry * u, col); }
        void Line(float x1, float y1, float x2, float y2, float t, Color col)
        { MahalleGraphic.Stroke(vh, P(x1, y1), P(x2, y2), t * u, col); }
        void Tri3(float x1, float y1, float x2, float y2, float x3, float y3, Color col)
        { MahalleGraphic.Tri(vh, P(x1, y1), P(x2, y2), P(x3, y3), col); }
        void Frame(float x, float y, float w, float h, float t, Color col)
        {
            Line(x, y, x + w, y, t, col); Line(x + w, y, x + w, y + h, t, col);
            Line(x + w, y + h, x, y + h, t, col); Line(x, y + h, x, y, t, col);
        }
        void Shadow(float rx) { Oval(0, -3, rx, rx * .26f, Shade); }

        switch (kind)
        {
            case DecorKind.Tree:
                Shadow(33);
                Tri3(-4.5f, -4, 4.5f, -4, 2.6f, 30, Trunk);
                Tri3(-4.5f, -4, 2.6f, 30, -2.6f, 30, Trunk);
                Line(-2, 14, -13, 24, 2.4f, Trunk);
                Oval(-15, 40, 20, 15, Leaf1);
                Oval(15, 36, 18, 14, Leaf2);
                Oval(-1, 53, 21, 16, Leaf3);
                Oval(-11, 58, 10, 8, Leaf4);
                break;

            case DecorKind.Bench:
                Shadow(30);
                Box(-25, 26, 50, 5, Wood);
                Box(-25, 17, 50, 5, WoodLit);
                Box(-25, 7.5f, 50, 5.5f, Wood);
                Box(-21, -1, 4, 9, Iron);
                Box(17, -1, 4, 9, Iron);
                break;

            // Yere tebeşirle çizilmiş seksek
            case DecorKind.Seksek:
                Frame(-13, 41, 26, 21, 1.6f, chalk);
                Frame(-13, 20, 26, 21, 1.6f, chalk);
                Frame(-26, -1, 26, 21, 1.6f, chalk);
                Frame(0, -1, 26, 21, 1.6f, chalk);
                Frame(-13, -22, 26, 21, 1.6f, chalk);
                break;

            case DecorKind.Steps:
                Shadow(34);
                Box(-30, -2, 60, 12, Concrete);
                Box(-25, 9, 50, 11, StoneLit);
                Box(-20, 19, 40, 27, Door);
                Box(-14, 23, 28, 23, DoorLit);
                Oval(8, 30, 2, 2, Bulb);
                break;

            case DecorKind.Pot:
                Shadow(15);
                Tri3(-11, 14, 11, 14, 8, -2, Terra);
                Tri3(-11, 14, 8, -2, -8, -2, Terra);
                Box(-13, 12, 26, 6, TerraLit);
                Oval(-5, 24, 9, 7, Leaf1);
                Oval(6, 27, 8, 6, Leaf3);
                break;

            case DecorKind.Bin:
                Shadow(15);
                Tri3(-12, 26, 12, 26, 9, 0, Iron);
                Tri3(-12, 26, 9, 0, -9, 0, Iron);
                Box(-14, 25, 28, 6, new Color(.24f, .31f, .27f));
                Line(-6, 6, -6, 22, 1.4f, new Color(.13f, .18f, .15f));
                Line(0, 6, 0, 22, 1.4f, new Color(.13f, .18f, .15f));
                Line(6, 6, 6, 22, 1.4f, new Color(.13f, .18f, .15f));
                break;

            case DecorKind.Cat:
                Shadow(16);
                Oval(0, 8, 15, 8, Fur);
                Oval(-13, 17, 7, 7, Fur);
                Tri3(-18, 22, -16, 29, -11, 25, Fur);
                Tri3(-9, 23, -8, 30, -3, 25, Fur);
                Line(14, 10, 20, 16, 3.4f, Fur);
                Line(20, 16, 20, 24, 3.4f, Fur);
                Oval(-15, 18, 1.4f, 1.4f, Bulb);
                break;

            case DecorKind.Hoop:
                Shadow(12);
                Box(-2, 0, 4.5f, 60, Pole);
                Box(-22, 56, 44, 28, Board);
                Frame(-9, 61, 18, 13, 1.6f, Rim);
                Line(-11, 58, 11, 58, 2.6f, Rim);
                Line(-9, 58, -6, 50, 1.3f, Net);
                Line(0, 58, 0, 49, 1.3f, Net);
                Line(9, 58, 6, 50, 1.3f, Net);
                break;

            case DecorKind.Goal:
                Shadow(30);
                Box(-30, 0, 4, 46, Concrete);
                Box(26, 0, 4, 46, Concrete);
                Box(-30, 45, 60, 4, StoneLit);
                for (int i = 0; i < 3; i++) Line(-26, 8 + i * 12, 26, 8 + i * 12, .9f, Net);
                for (int i = 0; i < 4; i++) Line(-18 + i * 12, 0, -18 + i * 12, 45, .9f, Net);
                break;

            case DecorKind.Tire:
                Shadow(17);
                Oval(0, 9, 17, 11, Rubber);
                Oval(0, 9, 8, 5, new Color(.56f, .48f, .31f));
                break;

            case DecorKind.Fountain:
                Shadow(30);
                Oval(0, 4, 28, 11, Stone);
                Oval(0, 8, 24, 9, Water);
                Box(-9, 8, 18, 36, StoneLit);
                Box(-13, 42, 26, 8, Concrete);
                Line(0, 40, 3, 26, 2.4f, Glass);
                Oval(-2, 53, 3, 3, Bulb);
                break;

            case DecorKind.Pigeon:
                Shadow(11);
                Oval(0, 8, 11, 7, Bird);
                Oval(-9, 15, 4.6f, 4.6f, BirdLit);
                Tri3(-13, 16, -17, 14.4f, -13, 12.8f, Beak);
                Oval(2, 9, 7, 4, new Color(.31f, .32f, .34f));
                Tri3(10, 8, 17, 5, 10, 6, Bird);
                break;
        }
    }
}
