using System.Collections.Generic;
using UnityEngine;

// Three environmental studies. Decorations never block a shot; puzzle obstacles stay in MahalleWorld.
public class ParkCorners : MonoBehaviour
{
    private readonly List<Material> materials = new List<Material>();
    private Material grass, leaf, wood, iron, sand, stone, flower;
    public static readonly Color[] Floors = { new Color(.65f,.64f,.53f), new Color(.67f,.53f,.35f), new Color(.43f,.32f,.21f) };

    public void Build(int corner)
    {
        grass = Mat(new Color(.27f,.40f,.18f)); leaf = Mat(new Color(.39f,.52f,.23f));
        wood = Mat(new Color(.48f,.24f,.10f)); iron = Mat(new Color(.13f,.22f,.20f));
        sand = Mat(new Color(.72f,.62f,.42f)); stone = Mat(new Color(.76f,.74f,.63f));
        flower = Mat(new Color(.93f,.65f,.25f));
        if(corner == 0) Entrance(); else if(corner == 1) Bench(); else Tree();
    }
    private Material Mat(Color color)
    {
        var m = new Material(Resources.Load<Shader>("Mahalle/Environment"));
        m.SetColor("_BaseColor",color); materials.Add(m); return m;
    }
    private Transform Part(string name, PrimitiveType type, Vector3 p, Vector3 size, Material m, float yaw=0)
    {
        var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(transform,false);
        var collider=go.GetComponent<Collider>(); collider.enabled=false; Destroy(collider);
        go.transform.localPosition=p; go.transform.localScale=size; go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        go.GetComponent<Renderer>().sharedMaterial=m; return go.transform;
    }
    private void Box(string name,float x,float y,float z,float w,float h,float d,Material m,float yaw=0)
        => Part(name,PrimitiveType.Cube,new Vector3(x,y,z),new Vector3(w,h,d),m,yaw);
    private void Patch(string name,float x,float z,float w,float d,Material m)
        => Part(name,PrimitiveType.Cylinder,new Vector3(x,.008f,z),new Vector3(w,.005f,d),m);
    private void Bed(float x,float z,float length)
    {
        Box("Çim şeridi",x,.005f,z,1.2f,.015f,length,grass);
        for(int i=0;i<9;i++)
        {
            float pz=z-length*.45f+i*length*.11f;
            Part("Çim demeti",PrimitiveType.Sphere,new Vector3(x+.24f*Mathf.Sin(i*2),.08f,pz),new Vector3(.38f,.17f,.43f),leaf);
            if(i%3==0)Part("Sarı çiçek",PrimitiveType.Sphere,new Vector3(x-.1f,.18f,pz),new Vector3(.12f,.1f,.12f),flower);
        }
    }
    private void Entrance()
    {
        // Thin joints leave the whole central playing surface flat and readable.
        for(int row=0;row<12;row++)
        {
            float z=-6+row*.95f;
            Box("Yol derzi",0,.006f,z,6.0f,.008f,.022f,sand);
            for(int col=0;col<4;col++)
                Box("Taş birleşimi",-2.7f+col*1.8f+(row%2)*.9f,.007f,z+.46f,.018f,.008f,.93f,sand);
        }
        for(int side=-1;side<=1;side+=2)
        {
            Bed(side*3.55f,-.2f,11);
            for(int i=0;i<14;i++)Box("Yol bordürü",side*3.02f,.045f,-6+i*.85f,.23f,.09f,.80f,stone);
            Box("Giriş direği",side*2.98f,.6f,4.9f,.22f,1.2f,.22f,iron);
            Part("Direk başlığı",PrimitiveType.Sphere,new Vector3(side*2.98f,1.25f,4.9f),Vector3.one*.28f,flower);
        }
        Box("Giriş eşiği",0,.013f,4.25f,5.8f,.02f,.45f,stone);
    }
    private void Bench()
    {
        Bed(-3.65f,0,12); Bed(3.75f,-1.5f,9);
        // Side-on bench is a clear silhouette, kept outside the chalk arena.
        for(int i=0;i<4;i++)Box("Bank oturağı",2.93f+i*.15f,.47f,.9f,.12f,.09f,2.9f,wood);
        for(int i=0;i<3;i++)Box("Bank sırtlığı",3.50f,.77f+i*.17f,.9f,.10f,.13f,2.9f,wood);
        foreach(float z in new[]{-.22f,2.02f})
        {
            Box("Bank ayağı",3.0f,.23f,z,.10f,.46f,.14f,iron);
            Box("Bank arka ayağı",3.48f,.54f,z,.10f,1.08f,.14f,iron);
            Box("Kolçak",3.25f,.79f,z,.70f,.08f,.10f,iron);
        }
        for(int i=0;i<24;i++)
        {
            float x=(i%2==0?-1:1)*(2.65f+.32f*Mathf.Sin(i*4));
            Part("Dökülmüş yaprak",PrimitiveType.Sphere,new Vector3(x,.025f,-4.8f+(i%12)*.78f),new Vector3(.13f,.018f,.25f),i%3==0?flower:wood,i*47);
        }
        // Paving at the far end marks the walkway behind the seating nook.
        for(int i=0;i<7;i++)Box("Bank arkası yürüyüş yolu",-3+i,.015f,4.1f,.95f,.025f,1.4f,stone);
        Patch("Bank yanı çim adası",-3.3f,2.8f,2,2.7f,grass);
    }
    private void Tree()
    {
        Bed(3.7f,-.7f,11);
        Patch("Ağaç toprağı",-3.3f,2.2f,3.0f,4.4f,wood);
        Part("Ağaç gövdesi",PrimitiveType.Cylinder,new Vector3(-3.35f,.88f,2.3f),new Vector3(.64f,.9f,.7f),wood);
        for(int i=0;i<5;i++)
        {
            float angle=i*72f;
            var root=Part("Yüzey kökü",PrimitiveType.Cube,new Vector3(-3.35f+Mathf.Sin(angle*Mathf.Deg2Rad)*.55f,.06f,2.3f+Mathf.Cos(angle*Mathf.Deg2Rad)*.65f),new Vector3(.18f,.12f,1.15f),wood,angle);
        }
        for(int i=0;i<5;i++)
            Part("Ağaç tacı",PrimitiveType.Sphere,new Vector3(-3.7f+Mathf.Sin(i*2.1f)*.65f,2.0f+(i%2)*.32f,2.5f+Mathf.Cos(i*2.1f)*.8f),new Vector3(1.65f,.8f,1.65f),i%2==0?grass:leaf);
        // Moss and stones frame the dirt clearing, away from the pull-back space.
        for(int i=0;i<13;i++)
        {
            float z=-4.7f+i*.76f;
            Patch("Yosun",-3.14f+.13f*Mathf.Sin(i),z,.66f,.7f,grass);
            Part("Ağaç dibi taşı",PrimitiveType.Sphere,new Vector3(3.06f,.07f,z),new Vector3(.27f,.14f,.35f),stone,i*27);
        }
        for(int i=0;i<6;i++)Patch("Uzak çim",-2.4f+i*.9f,4.5f,.95f,1.2f,leaf);
    }
    private void OnDestroy() { foreach(var m in materials)if(m!=null)Destroy(m); }
}
