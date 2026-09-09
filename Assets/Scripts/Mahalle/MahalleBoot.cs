using UnityEngine;
using UnityEngine.SceneManagement;

public static class MahalleBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;
        Application.targetFrameRate=60;
    }
    private static void Loaded(Scene scene,LoadSceneMode mode)
    {
        if(scene.name!=GameSession.GameSceneName && scene.name!=GameSession.LevelSelectSceneName)return;
        foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {if(canvas.isRootCanvas){canvas.gameObject.SetActive(false);Object.Destroy(canvas.gameObject);}}
        var go=new GameObject("MİSKETR • Mahalle",typeof(RectTransform),typeof(MahalleUI));
    }
}
