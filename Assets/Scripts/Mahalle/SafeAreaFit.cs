using UnityEngine;
public class SafeAreaFit : MonoBehaviour
{
    private Rect last;private Vector2 size;
    private void Update()
    {
        Rect safe=Screen.safeArea;var current=new Vector2(Screen.width,Screen.height);
        if(safe==last && size==current)return;last=safe;size=current;
        if(Screen.width<=0||Screen.height<=0)return;
        var r=(RectTransform)transform;r.anchorMin=new Vector2(safe.x/Screen.width,safe.y/Screen.height);r.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);r.offsetMin=r.offsetMax=Vector2.zero;
    }
}
