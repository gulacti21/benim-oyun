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
#else
        Debug.Log("PAYLAS_KARTI: " + path + "\n" + text);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.RevealInFinder(path);
#endif
#endif
    }
}
