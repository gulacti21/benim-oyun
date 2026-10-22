using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// HARİTA 3 ALÇAK KALDIRIM ölçümü: yuvarlanan misket (yarıçap .25) farklı yükseklikte bir
// kaldırıma farklı hızlarla gelir; aşıp aşmadığı yazılır. Kaldırım yüksekliği buradan seçildi.
// Çalıştırma (editör açıkken): CurbVerify.Schedule()  → log: CURB_MEASURE
public static class CurbVerify
{
    public static void Schedule() { EditorApplication.delayCall += Measure; }

    public static void Measure()
    {
        var marbleMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/MarbleMaterialPhysics.asset");
        var groundMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/GroundMaterialPhysics.asset");
        var sb = new StringBuilder("CURB_MEASURE yükseklik: hız→aştı mı (+ aştı, - geri döndü/durdu)\n");
        float[] heights = { .08f, .10f, .12f, .14f, .16f };
        float[] speeds = { .5f, 1f, 1.5f, 2f, 2.5f, 3f, 4f, 6f, 10f };
        foreach (float h in heights)
        {
            sb.Append("  h=" + h.ToString("F2") + " ");
            foreach (float v in speeds)
            {
                var scene = EditorSceneManager.NewPreviewScene();
                var ps = scene.GetPhysicsScene();
                try
                {
                    var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    SceneManager(ground, scene); ground.transform.position = new Vector3(0, -.25f, 0); ground.transform.localScale = new Vector3(30, .5f, 30);
                    ground.GetComponent<Collider>().sharedMaterial = groundMat;
                    var curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    SceneManager(curb, scene); curb.transform.position = new Vector3(0, h * .5f, 1f); curb.transform.localScale = new Vector3(4f, h, .3f);
                    var m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    SceneManager(m, scene); m.transform.position = new Vector3(0, .25f, 0f); m.transform.localScale = Vector3.one * .5f;
                    m.GetComponent<Collider>().sharedMaterial = marbleMat;
                    var b = m.AddComponent<Rigidbody>(); b.mass = .05f; b.linearDamping = .6f; b.angularDamping = .5f;
                    b.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    b.linearVelocity = new Vector3(0, 0, v); b.angularVelocity = new Vector3(v / .25f, 0, 0);
                    float maxY = 0f;
                    for (int f = 0; f < 400; f++) { ps.Simulate(.02f); maxY = Mathf.Max(maxY, b.position.y); }
                    bool over = b.position.z > 1.2f;
                    sb.Append(v.ToString("0.#") + (over ? "+" : "-") + (maxY > .6f ? "^" : "") + " ");
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
            sb.AppendLine();
        }
        Debug.Log(sb.ToString() + "CURB_MEASURE_DONE");
    }

    private static void SceneManager(GameObject go, UnityEngine.SceneManagement.Scene scene)
    {
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
    }
}
