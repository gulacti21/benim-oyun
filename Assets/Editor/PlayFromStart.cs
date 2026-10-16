using UnityEditor;
using UnityEditor.SceneManagement;

// PLAY HEP BAŞTAN: Editor'da hangi sahne açık olursa olsun Play'e basınca oyun,
// telefondaki gibi LevelSelect'ten (ana menü) başlar. Game sahnesini tek başına
// denemek için menüden kapat: MISKETR → Play Baştan Başlasın.
[InitializeOnLoad]
public static class PlayFromStart
{
    private const string Key = "MISKO_PLAY_FROM_START";
    private const string MenuPath = "MISKETR/Play Baştan Başlasın";
    private const string StartScene = "Assets/Scenes/LevelSelect.unity";

    static PlayFromStart() { EditorApplication.delayCall += Apply; }

    private static bool Enabled
    {
        get => EditorPrefs.GetBool(Key, true);
        set => EditorPrefs.SetBool(Key, value);
    }

    private static void Apply()
    {
        EditorSceneManager.playModeStartScene = Enabled
            ? AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScene)
            : null;
        Menu.SetChecked(MenuPath, Enabled);
    }

    [MenuItem(MenuPath)]
    private static void Toggle() { Enabled = !Enabled; Apply(); }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate() { Menu.SetChecked(MenuPath, Enabled); return true; }
}
