using UnityEngine;
using UnityEngine.Rendering;

public class MahalleWorld : MonoBehaviour
{
    private Material groundMaterial, stoneMaterial;
    private GameObject decor;
    private static readonly Color[] GroundColors={new Color(.57f,.42f,.28f),new Color(.53f,.52f,.44f),new Color(.47f,.49f,.30f),new Color(.64f,.43f,.28f),new Color(.57f,.51f,.40f)};
    public static void Apply(LevelController controller)
    {
        var world=FindFirstObjectByType<MahalleWorld>();
        if(world==null) world=new GameObject("Mahalle ortamı").AddComponent<MahalleWorld>();
        world.Build(controller);
    }
    private void Build(LevelController controller)
    {
        if(decor!=null) {decor.SetActive(false);Destroy(decor);}
        decor=new GameObject("Çevre ve engeller"); decor.transform.SetParent(transform);
        int district=controller.Level.district;
        int corner=controller.LevelIndex%12;
        bool parkStudy=district==2 && corner<3;
        var ground=GameObject.Find("Ground");
        if(ground!=null)
        {
            if(groundMaterial==null)
            {
                groundMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));
                var texture=new Texture2D(128,128,TextureFormat.RGBA32,true);
                for(int y=0;y<128;y++) for(int x=0;x<128;x++)
                {float grain=.82f+Mathf.PerlinNoise(x*.38f,y*.38f)*.28f; texture.SetPixel(x,y,new Color(grain,grain,grain,1));}
                texture.wrapMode=TextureWrapMode.Repeat; texture.Apply();
                groundMaterial.SetTexture("_BaseMap",texture); groundMaterial.SetTextureScale("_BaseMap",Vector2.one*12);
                groundMaterial.SetFloat("_Smoothness",.04f);
            }
            groundMaterial.SetColor("_BaseColor",parkStudy?ParkCorners.Floors[corner]:GroundColors[district]);
            ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;
        }
        var camera=Camera.main;
        if(camera!=null)
        {
            camera.orthographic=true; camera.orthographicSize=Mathf.Max(6.3f,3.65f/camera.aspect);
            camera.transform.rotation=Quaternion.Euler(72,0,0);
            camera.transform.position=new Vector3(0,0,-1.1f)-camera.transform.forward*18;
            camera.backgroundColor=new Color(.18f,.16f,.12f);
        }
        var light=FindFirstObjectByType<Light>();
        if(light!=null) {light.color=new Color(1,.94f,.82f);light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(52,-30,0);light.shadows=LightShadows.Soft;}
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.65f,.69f);
        if(parkStudy)
        {
            decor.AddComponent<ParkCorners>().Build(corner);
            if(light!=null)light.color=corner==1?new Color(1,.84f,.65f):corner==2?new Color(.89f,.95f,.85f):new Color(1,.97f,.88f);
        }
        if(controller.Shooter.GetComponent<MarbleVisual>()==null) controller.Shooter.gameObject.AddComponent<MarbleVisual>();
        controller.Shooter.GetComponent<MarbleVisual>().SetSkin(MahalleProfile.EffectiveSkin);
        var line=FindFirstObjectByType<ShooterLine>();
        if(line!=null)
        {
            // Bölüm hattı daraltabilir veya yana kaydırabilir.
            line.ApplyLevel(controller.Level.shooterHalfWidth,controller.Level.shooterOffsetX);
            line.SetPosition(controller.Level.shooterStartPosition);
            // Kenarda parmak payı: misketi geri çekmek için ekranda yer kalsın.
            line.FitToCamera(camera,1.05f);
        }
        if(stoneMaterial==null) {stoneMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));stoneMaterial.SetColor("_BaseColor",new Color(.3f,.32f,.28f));}
        var arena=FindFirstObjectByType<MarbleArena>();
        Vector3 centre=arena!=null?arena.transform.position:Vector3.zero;
        var designed=controller.Level.obstacles;
        if(designed!=null && designed.Length>0)
        {
            // Bölüme özel engeller. Bunlar dekor değil: misket çarpar ve seker.
            for(int i=0;i<designed.Length;i++)
            {
                var spot=designed[i];
                var stone=GameObject.CreatePrimitive(PrimitiveType.Cube);stone.name="Engel "+(i+1);
                stone.transform.SetParent(decor.transform);
                stone.transform.position=new Vector3(centre.x+spot.x,.22f,centre.z+spot.z);
                stone.transform.localScale=new Vector3(Mathf.Max(.2f,spot.width),.44f,Mathf.Max(.2f,spot.depth));
                stone.transform.rotation=Quaternion.Euler(0,spot.angle,0);
                stone.GetComponent<Renderer>().sharedMaterial=stoneMaterial;
            }
        }
        else for(int i=0;i<controller.Level.obstacleCount;i++)
        {
            var stone=GameObject.CreatePrimitive(PrimitiveType.Cube);stone.name="Park taşı";stone.transform.SetParent(decor.transform);
            stone.transform.position=new Vector3(centre.x+(i==0?-1.4f:1.4f),.22f,centre.z-2.5f);
            stone.transform.localScale=new Vector3(.65f,.44f,.42f); stone.transform.rotation=Quaternion.Euler(0,i==0?12:-8,0);
            stone.GetComponent<Renderer>().sharedMaterial=stoneMaterial;
        }
        // Small pebbles live outside the aiming corridor and have no colliders.
        for(int i=0;i<(parkStudy?0:18);i++)
        {
            var pebble=GameObject.CreatePrimitive(PrimitiveType.Sphere);pebble.name="Çevre taşı";Destroy(pebble.GetComponent<Collider>());
            pebble.transform.SetParent(decor.transform);float z=-3.5f+(i%9)*.85f;
            pebble.transform.position=new Vector3((i<9?-1:1)*(3.45f+.12f*Mathf.Sin(i)),.035f,z);
            pebble.transform.localScale=new Vector3(.08f+.06f*(i%3),.07f,.13f);pebble.GetComponent<Renderer>().sharedMaterial=stoneMaterial;
        }
    }
    private void OnDestroy() {if(groundMaterial!=null) {Destroy(groundMaterial.GetTexture("_BaseMap"));Destroy(groundMaterial);}if(stoneMaterial!=null)Destroy(stoneMaterial);}
}
