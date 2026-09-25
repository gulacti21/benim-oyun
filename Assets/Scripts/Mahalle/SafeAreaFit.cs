using UnityEngine;
public class SafeAreaFit : MonoBehaviour
{
    // iPad oyun ekranı: arayüz telefon genişliğinde (1080) ortada bir sütun olur, oyun sahası
    // bütün ekranı kullanır. 0 = sınır yok (telefonlar ve menü ekranları).
    public float maxWidth;
    private Rect last;private Vector2 size;private float parentWidth=-1;
    private void Update()
    {
        Rect safe=Screen.safeArea;var current=new Vector2(Screen.width,Screen.height);
        var parent=transform.parent as RectTransform;float pw=parent!=null?parent.rect.width:0;
        if(safe==last && size==current && Mathf.Approximately(pw,parentWidth))return;last=safe;size=current;parentWidth=pw;
        if(Screen.width<=0||Screen.height<=0)return;
        var r=(RectTransform)transform;r.anchorMin=new Vector2(safe.x/Screen.width,safe.y/Screen.height);r.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);r.offsetMin=r.offsetMax=Vector2.zero;
        if(maxWidth>0&&pw>0)
        {
            float w=pw*(r.anchorMax.x-r.anchorMin.x);
            if(w>maxWidth){float inset=(w-maxWidth)*.5f;r.offsetMin=new Vector2(inset,0);r.offsetMax=new Vector2(-inset,0);}
        }
    }
}
