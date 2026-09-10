using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MahallePreview
{
    private const string UnlockMenu = "MISKETR/Preview/Unlock all levels temporarily";
    private const string RestoreMenu = "MISKETR/Preview/Return to saved progress";
    // Yalnızca bu bilgisayardaki bu proje için; oyun kaydına ve iOS build'e yazılmaz.
    private static string PreferenceKey => "MISKETR.Preview.KeepUnlocked." + Application.dataPath;
    private static bool KeepUnlocked => EditorPrefs.GetBool(PreferenceKey, false);

    static MahallePreview()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += RestoreAfterReload;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void PreparePreview()
    {
        if (KeepUnlocked && !MahalleProfile.TestMode)
            MahalleProfile.BeginPreview();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
            MahalleProfile.EndPreview();
        else if (state == PlayModeStateChange.EnteredPlayMode)
            RestoreAfterReload();
    }

    private static void RestoreAfterReload()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling ||
            !KeepUnlocked || MahalleProfile.TestMode || MahalleProfile.PreviewMode)
            return;

        MahalleProfile.BeginPreview();
        OpenMap();
    }

    [MenuItem(UnlockMenu)]
    private static void Begin()
    {
        EditorPrefs.SetBool(PreferenceKey, true);
        if (EditorApplication.isPlaying)
        {
            MahalleProfile.BeginPreview();
            OpenMap();
        }
        Debug.Log("MAHALLE_PREVIEW_ON: 60 bölüm açık. Ayar kapatılana kadar her Play oturumunda etkin; oyun kaydı korunur.");
    }

    [MenuItem(UnlockMenu, true)]
    private static bool CanBegin()
    {
        Menu.SetChecked(UnlockMenu, KeepUnlocked);
        return !KeepUnlocked || (EditorApplication.isPlaying && !MahalleProfile.PreviewMode);
    }

    [MenuItem(RestoreMenu)]
    private static void End()
    {
        EditorPrefs.SetBool(PreferenceKey, false);
        MahalleProfile.EndPreview();
        if (EditorApplication.isPlaying) OpenMap();
        Debug.Log("MAHALLE_PREVIEW_OFF: Normal bölüm kilitleri ve kayıt düzeni geri yüklendi.");
    }

    [MenuItem(RestoreMenu, true)]
    private static bool CanEnd() => KeepUnlocked || MahalleProfile.PreviewMode;

    private static void OpenMap()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(GameSession.LevelSelectSceneName);
    }
}
