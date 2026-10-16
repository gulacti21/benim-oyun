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
#elif UNITY_ANDROID && !UNITY_EDITOR
        AndroidTick();
#endif
    }
#if UNITY_ANDROID && !UNITY_EDITOR
    // Android: 15 ms hafif titreşim (Handheld.Vibrate çok uzun ve sert). Handheld referansı, Unity'nin
    // VIBRATE iznini manifest'e eklemesi için burada duruyor.
    static AndroidJavaObject vibrator; static bool vibratorTried;
    static bool neverVibrate;   // sabit false olsaydı derleyici Handheld çağrısını silerdi
    static void AndroidTick()
    {
        try
        {
            if (!vibratorTried)
            {
                vibratorTried = true;
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            if (vibrator == null) return;
            using (var ver = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                if (ver.GetStatic<int>("SDK_INT") >= 26)
                    using (var fx = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = fx.CallStatic<AndroidJavaObject>("createOneShot", 15L, 80))
                        vibrator.Call("vibrate", effect);
                else vibrator.Call("vibrate", 15L);
            }
        }
        catch (System.Exception) { if (neverVibrate) Handheld.Vibrate(); }
    }
#endif
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
