using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// ANDROID KARE HIZI (2026-10-16, ölçüldü): Redmi Note 11'de (1080x2400) oyun 60'a yetişemeyip
// sabit 30 karede kaldı. Android'de 3D sahnenin çizim çözünürlüğü (URP renderScale) kare hızına
// göre kademe kademe düşürülür. UI (Screen Space Overlay) bundan etkilenmez, yazılar net kalır.
// Güçlü telefonda hiçbir şey değişmez. iOS ve editör dokunulmaz (RP asset diskte değişmesin).
public class AndroidPerf : MonoBehaviour
{
    static readonly float[] Steps = { 0.65f, 0.55f };
    const float Window = 3f;        // ölçüm penceresi (sn)
    const float MinFps = 50f;       // altında kalırsa bir kademe düşür

#if UNITY_ANDROID && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        var go = new GameObject("AndroidPerf");
        DontDestroyOnLoad(go);
        go.AddComponent<AndroidPerf>();
    }
#endif

    int step = -1, frames;
    float t, warmup = 4f;

    void Update()
    {
        if (step >= Steps.Length - 1) { enabled = false; return; }
        if (warmup > 0f) { warmup -= Time.unscaledDeltaTime; return; }
        t += Time.unscaledDeltaTime; frames++;
        if (t < Window) return;
        float fps = frames / t;
        t = 0f; frames = 0;
        if (fps >= MinFps) return;
        var rp = (QualitySettings.renderPipeline ?? GraphicsSettings.currentRenderPipeline) as UniversalRenderPipelineAsset;
        if (rp == null) { enabled = false; return; }
        step++;
        if (rp.renderScale > Steps[step]) rp.renderScale = Steps[step];
        Debug.Log("AndroidPerf: fps " + fps.ToString("0") + " -> renderScale " + rp.renderScale);
        warmup = 1.5f;
    }
}
