using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class MahalleGraphic : MaskableGraphic
{
    public enum Shape { Panel, Circle, Star, Marble, Lock, Bag, House, Arrow, Triangle, Ring, Hand }
    public Shape shape;
    public float radius=24;
    public Color accent=new Color(1,.85f,.4f);
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();Rect r=rectTransform.rect;Vector2 c=r.center;float w=r.width,h=r.height;
        switch(shape)
        {
            case Shape.Panel: Rounded(vh,r,radius,color);break;
            case Shape.Circle: Disc(vh,c,w*.5f,h*.5f,color);break;
            case Shape.Star:
                for(int i=0;i<10;i++){float a=(90+i*36)*Mathf.Deg2Rad,b=(90+(i+1)*36)*Mathf.Deg2Rad;
                Tri(vh,c,c+new Vector2(Mathf.Cos(a)*w,Mathf.Sin(a)*h)*(i%2==0?.5f:.22f),c+new Vector2(Mathf.Cos(b)*w,Mathf.Sin(b)*h)*(i%2==0?.22f:.5f),color);}break;
            case Shape.Marble:
                Disc(vh,c+new Vector2(w*.025f,-h*.07f),w*.48f,h*.44f,new Color(0,0,0,.16f));
                Disc(vh,c,w*.47f,h*.47f,color*.7f);
                Disc(vh,c+new Vector2(-w*.035f,h*.045f),w*.41f,h*.41f,color);
                for(int i=0;i<18;i++) {float t=i/17f;float y=(t-.5f)*h*.73f;float x=Mathf.Sin(t*5.8f)*w*.19f;Disc(vh,c+new Vector2(x,y),w*(.045f+.06f*Mathf.Sin(t*Mathf.PI)),h*.035f,accent);}
                Disc(vh,c+new Vector2(-w*.15f,h*.22f),w*.11f,h*.07f,new Color(1,1,1,.85f));
                Disc(vh,c+new Vector2(w*.24f,-h*.2f),w*.045f,h*.045f,new Color(1,1,1,.36f));break;
            case Shape.Lock:
                Rounded(vh,new Rect(c.x-w*.34f,c.y-h*.4f,w*.68f,h*.52f),w*.1f,color);
                for(int i=0;i<14;i++){float a=(i*180/13f)*Mathf.Deg2Rad;Disc(vh,c+new Vector2(Mathf.Cos(a)*w*.24f,Mathf.Sin(a)*h*.3f+h*.08f),w*.045f,h*.045f,color);}
                Disc(vh,c-new Vector2(0,h*.08f),w*.05f,h*.07f,accent);break;
            case Shape.Bag:
                Disc(vh,c-new Vector2(0,h*.12f),w*.39f,h*.35f,color);
                Tri(vh,c+new Vector2(-w*.3f,h*.4f),c+new Vector2(w*.3f,h*.4f),c-new Vector2(0,h*.1f),color);
                Rounded(vh,new Rect(c.x-w*.27f,c.y+h*.12f,w*.54f,h*.07f),2,accent);break;
            case Shape.House:
                Rounded(vh,new Rect(c.x-w*.36f,c.y-h*.42f,w*.72f,h*.63f),4,color);
                Tri(vh,c+new Vector2(-w*.48f,h*.16f),c+new Vector2(w*.48f,h*.16f),c+new Vector2(0,h*.48f),accent);
                Rounded(vh,new Rect(c.x-w*.08f,c.y-h*.42f,w*.16f,h*.28f),3,accent);
                for(int i=0;i<2;i++)Rounded(vh,new Rect(c.x-w*.25f+i*w*.34f,c.y-h*.03f,w*.16f,h*.15f),2,new Color(1,.88f,.55f));break;
            case Shape.Arrow:
                Tri(vh,c+new Vector2(-w*.22f,-h*.35f),c+new Vector2(w*.28f,0),c+new Vector2(-w*.22f,h*.35f),color);break;
            case Shape.Triangle:
                Stroke(vh,c+new Vector2(-w*.42f,h*.35f),c+new Vector2(w*.42f,h*.35f),w*.05f,color);
                Stroke(vh,c+new Vector2(w*.42f,h*.35f),c+new Vector2(0,-h*.4f),w*.05f,color);
                Stroke(vh,c+new Vector2(0,-h*.4f),c+new Vector2(-w*.42f,h*.35f),w*.05f,color);
                Disc(vh,c,w*.06f,h*.06f,accent);Disc(vh,c+new Vector2(-w*.13f,h*.17f),w*.06f,h*.06f,accent);Disc(vh,c+new Vector2(w*.13f,h*.17f),w*.06f,h*.06f,accent);break;
            case Shape.Ring:
                for(int i=0;i<40;i++){float a=i*Mathf.PI*2/40,b=(i+1)*Mathf.PI*2/40;Stroke(vh,c+new Vector2(Mathf.Cos(a)*w*.4f,Mathf.Sin(a)*h*.4f),c+new Vector2(Mathf.Cos(b)*w*.4f,Mathf.Sin(b)*h*.4f),w*.04f,color);}
                Disc(vh,c,w*.07f,h*.07f,accent);break;
            case Shape.Hand:
                Rounded(vh,new Rect(c.x-w*.14f,c.y-h*.15f,w*.24f,h*.62f),w*.11f,color);
                Rounded(vh,new Rect(c.x-w*.28f,c.y-h*.4f,w*.65f,h*.55f),w*.18f,color);break;
        }
    }
    public static void Tri(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color col)
    {int i=vh.currentVertCount;vh.AddVert(a,col,Vector2.zero);vh.AddVert(b,col,Vector2.zero);vh.AddVert(c,col,Vector2.zero);vh.AddTriangle(i,i+1,i+2);}
    public static void Disc(VertexHelper vh,Vector2 c,float rx,float ry,Color col)
    {for(int i=0;i<40;i++){float a=i*Mathf.PI*2/40,b=(i+1)*Mathf.PI*2/40;Tri(vh,c,c+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry),c+new Vector2(Mathf.Cos(b)*rx,Mathf.Sin(b)*ry),col);}}
    public static void Stroke(VertexHelper vh,Vector2 a,Vector2 b,float width,Color col)
    {Vector2 d=(b-a).normalized;Vector2 n=new Vector2(-d.y,d.x)*width*.5f;Tri(vh,a+n,a-n,b+n,col);Tri(vh,a-n,b-n,b+n,col);}
    private static void Rounded(VertexHelper vh,Rect r,float radius,Color col)
    {
        float k=Mathf.Min(radius,Mathf.Min(r.width,r.height)*.5f);
        var points=new Vector2[36];int p=0;
        for(int corner=0;corner<4;corner++)
        {Vector2 c=new Vector2(corner==0||corner==3?r.xMax-k:r.xMin+k,corner<2?r.yMax-k:r.yMin+k);
        for(int j=0;j<=8;j++){float a=(corner*90+j*90/8f)*Mathf.Deg2Rad;points[p++]=c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*k;}}
        for(int i=0;i<points.Length;i++)Tri(vh,r.center,points[i],points[(i+1)%points.Length],col);
    }
}
