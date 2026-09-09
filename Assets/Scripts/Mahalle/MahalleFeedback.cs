using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;

public class MahalleFeedback : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void MisketrHaptic();
#endif
    public static void Tap()
    {
        if(!MahalleProfile.Data.haptics) return;
#if UNITY_IOS && !UNITY_EDITOR
        MisketrHaptic();
#endif
    }
    public static void Score(Vector3 point)
    {
        Tap();
        var go=new GameObject("+1");go.transform.position=point+Vector3.up*.7f;
        if(Camera.main!=null)go.transform.rotation=Camera.main.transform.rotation;
        var text=go.AddComponent<TextMeshPro>();text.text="+1";text.fontSize=5;text.alignment=TextAlignmentOptions.Center;
        text.color=new Color(1,.86f,.4f);text.rectTransform.sizeDelta=new Vector2(1,1);
        go.AddComponent<MahalleFeedback>();
        var spark=GameObject.CreatePrimitive(PrimitiveType.Sphere);spark.name="Parıltı";Destroy(spark.GetComponent<Collider>());
        spark.transform.position=point+Vector3.up*.25f;spark.transform.localScale=Vector3.one*.12f;
        spark.AddComponent<MahalleFeedback>();
    }
    private float age;
    private void Update()
    {
        age+=Time.deltaTime;transform.position+=Vector3.up*Time.deltaTime*.7f;
        var t=GetComponent<TextMeshPro>();if(t!=null)t.alpha=1-age/.8f;
        else transform.localScale*=Mathf.Max(0,1-Time.deltaTime*3);
        if(age>.8f)Destroy(gameObject);
    }
}
