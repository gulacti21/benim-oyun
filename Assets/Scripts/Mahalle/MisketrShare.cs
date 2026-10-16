using System.Runtime.InteropServices;
using UnityEngine;

// PAYLAŞ KARTI: kaydedilen PNG'yi telefonun paylaşım menüsüyle açar.
// Editörde paylaşım menüsü yok; dosya Finder'da gösterilir.
public static class MisketrShare
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void MisketrShareImage(string path, string text);
#endif

    public static void ShareImage(string path, string text)
    {
#if UNITY_IOS && !UNITY_EDITOR
        MisketrShareImage(path, text);
#elif UNITY_ANDROID && !UNITY_EDITOR
        // Android: sistemin paylaşım menüsü (metin). Görsel paylaşmak FileProvider ister; metin yeterli.
        try
        {
            using (var intentClass = new AndroidJavaClass("android.content.Intent"))
            using (var intent = new AndroidJavaObject("android.content.Intent"))
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intent.Call<AndroidJavaObject>("setType", "text/plain");
                intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);
                using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Misko"))
                    activity.Call("startActivity", chooser);
            }
        }
        catch (System.Exception e) { Debug.LogWarning("Paylasim acilamadi: " + e.Message); }
#else
        Debug.Log("PAYLAS_KARTI: " + path + "\n" + text);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.RevealInFinder(path);
#endif
#endif
    }
}
