using UnityEngine;

// MÜZİK: menüde ve oyunda kesintisiz çalan mahalle müziği.
// Sahne değişince durmaz (DontDestroyOnLoad). Ayarlar'dan açılıp kapanır.
public class MusicPlayer : MonoBehaviour
{
    private const string ClipPath = "Mahalle/Music/MahalleMuzigi";
    private const float Volume = .45f;
    private static MusicPlayer instance;
    private AudioSource source;

    public static void Ensure()
    {
        if (instance != null) { instance.RefreshNow(); return; }
        var clip = Resources.Load<AudioClip>(ClipPath);
        if (clip == null) { Debug.LogWarning("MISKO: muzik dosyasi bulunamadi: " + ClipPath); return; }
        var go = new GameObject("Müzik");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MusicPlayer>();
        instance.source = go.AddComponent<AudioSource>();
        instance.source.clip = clip;
        instance.source.loop = true;
        instance.source.playOnAwake = false;
        instance.source.spatialBlend = 0f;
        instance.source.volume = Volume;
        instance.source.ignoreListenerPause = true;
        instance.RefreshNow();
    }

    public static void Refresh() { if (instance != null) instance.RefreshNow(); }

    private void RefreshNow()
    {
        if (source == null) return;
        bool on = MahalleProfile.Data.music;
        if (on && !source.isPlaying) source.Play();
        else if (!on && source.isPlaying) source.Stop();
    }
}
