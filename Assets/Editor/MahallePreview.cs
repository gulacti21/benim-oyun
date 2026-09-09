using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MahallePreview
{
    static MahallePreview()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                MahalleProfile.EndPreview();
        };
    }

    [MenuItem("MISKETR/Preview/Unlock all levels temporarily")]
    private static void Begin()
    {
        MahalleProfile.BeginPreview();
        OpenMap();
    }

    [MenuItem("MISKETR/Preview/Unlock all levels temporarily", true)]
    private static bool CanBegin() => EditorApplication.isPlaying && !MahalleProfile.PreviewMode;

    [MenuItem("MISKETR/Preview/Return to saved progress")]
    private static void End()
    {
        MahalleProfile.EndPreview();
        OpenMap();
    }

    [MenuItem("MISKETR/Preview/Return to saved progress", true)]
    private static bool CanEnd() => EditorApplication.isPlaying && MahalleProfile.PreviewMode;

    private static void OpenMap()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(GameSession.LevelSelectSceneName);
    }
}
