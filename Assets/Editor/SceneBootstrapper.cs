using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SceneBootstrapper
{
    private const string TargetSceneName = "Game";
    private const string ShooterName = "ShooterMarble";
    private const string GroundName = "Ground";
    private const string AimIndicatorName = "AimIndicator";
    private const string SessionKey = "MISKETR_BootstrapAttempted";
    private const string VersionKey = "MISKETR_BootstrapVersion";
    private const string BootstrapVersion = "8";
    private const string ShooterLineName = "ShooterLine";
    private const string GameManagerName = "GameManager";
    private const string LevelsFolder = "Assets/ScriptableObjects/Levels";
    private const string ArenaName = "Arena";
    private const string ScoreHudName = "ScoreHud";
    private const string PrefabsFolder = "Assets/Prefabs";
    private const string MaterialsFolder = "Assets/Materials";

    static SceneBootstrapper()
    {
        EditorApplication.delayCall += AutoBuild;
    }

    private static void AutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.name != TargetSceneName)
        {
            return;
        }

        string versionKey = VersionKey + "_" + Application.dataPath;

        if (EditorPrefs.GetString(versionKey, string.Empty) == BootstrapVersion)
        {
            return;
        }

        Build(true);
        EditorPrefs.SetString(versionKey, BootstrapVersion);
    }

    [MenuItem("MISKETR/Rebuild Gameplay Scene")]
    private static void RebuildFromMenu()
    {
        Build(true);
    }

    private static void Build(bool saveScene)
    {
        try
        {
            Scene scene = EditorSceneManager.GetActiveScene();

            Material groundMaterial = CreateLitMaterial("GroundLitMaterial", new Color(0.55f, 0.45f, 0.33f));
            Material marbleMaterial = CreateLitMaterial("MarbleLitMaterial", new Color(0.25f, 0.55f, 0.9f));
            Material targetMaterial = CreateLitMaterial("TargetMarbleLitMaterial", new Color(0.85f, 0.35f, 0.75f));
            Material aimLineMaterial = CreateAimLineMaterial();

            PhysicsMaterial groundPhysics = CreatePhysicsMaterial("GroundMaterial", 0.5f, 0.5f, 0.1f);
            PhysicsMaterial marblePhysics = CreatePhysicsMaterial("MarbleMaterial", 0.3f, 0.3f, 0.4f);

            GameObject ground = FindInScene(scene, GroundName);
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = GroundName;
            }

            ground.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            ground.transform.localScale = new Vector3(2f, 1f, 2f);

            MeshRenderer groundRenderer = ground.GetComponent<MeshRenderer>();
            if (groundRenderer != null && groundMaterial != null)
            {
                groundRenderer.sharedMaterial = groundMaterial;
            }

            Collider groundCollider = ground.GetComponent<Collider>();
            if (groundCollider != null)
            {
                groundCollider.sharedMaterial = groundPhysics;
            }

            GameObject marble = FindInScene(scene, ShooterName);
            if (marble == null)
            {
                marble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marble.name = ShooterName;
            }

            marble.transform.SetPositionAndRotation(new Vector3(0f, 0.25f, -4f), Quaternion.identity);
            marble.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            MeshRenderer marbleRenderer = marble.GetComponent<MeshRenderer>();
            if (marbleRenderer != null && marbleMaterial != null)
            {
                marbleRenderer.sharedMaterial = marbleMaterial;
            }

            Collider marbleCollider = marble.GetComponent<Collider>();
            if (marbleCollider != null)
            {
                marbleCollider.sharedMaterial = marblePhysics;
            }

            Rigidbody body = marble.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = marble.AddComponent<Rigidbody>();
            }

            body.mass = 0.05f;
            body.linearDamping = 0.6f;
            body.angularDamping = 0.5f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            GameObject aimObject = FindInScene(scene, AimIndicatorName);
            if (aimObject == null)
            {
                aimObject = new GameObject(AimIndicatorName);
            }

            aimObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            LineRenderer line = aimObject.GetComponent<LineRenderer>();
            if (line == null)
            {
                line = aimObject.AddComponent<LineRenderer>();
            }

            line.positionCount = 2;
            line.useWorldSpace = true;
            line.widthMultiplier = 0.12f;
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;

            if (aimLineMaterial != null)
            {
                line.sharedMaterial = aimLineMaterial;
            }

            AimIndicator indicator = aimObject.GetComponent<AimIndicator>();
            if (indicator == null)
            {
                indicator = aimObject.AddComponent<AimIndicator>();
            }

            ShotController shot = marble.GetComponent<ShotController>();
            if (shot == null)
            {
                shot = marble.AddComponent<ShotController>();
            }

            GameObject targetPrefab = CreateTargetMarblePrefab(targetMaterial, marblePhysics);

            GameObject arenaObject = FindInScene(scene, ArenaName);
            if (arenaObject == null)
            {
                arenaObject = new GameObject(ArenaName);
            }

            arenaObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            arenaObject.transform.localScale = Vector3.one;

            LineRenderer arenaLine = arenaObject.GetComponent<LineRenderer>();
            if (arenaLine == null)
            {
                arenaLine = arenaObject.AddComponent<LineRenderer>();
            }

            arenaLine.useWorldSpace = false;
            arenaLine.loop = true;
            arenaLine.numCapVertices = 2;
            arenaLine.numCornerVertices = 2;
            arenaLine.alignment = LineAlignment.View;
            arenaLine.shadowCastingMode = ShadowCastingMode.Off;
            arenaLine.receiveShadows = false;

            if (aimLineMaterial != null)
            {
                arenaLine.sharedMaterial = aimLineMaterial;
            }

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(arenaObject);

            MarbleArena arena = arenaObject.GetComponent<MarbleArena>();
            if (arena == null)
            {
                arena = arenaObject.AddComponent<MarbleArena>();
            }

            if (targetPrefab != null)
            {
                SerializedObject arenaSerialized = new SerializedObject(arena);
                SerializedProperty prefabProperty = arenaSerialized.FindProperty("targetMarblePrefab");

                if (prefabProperty != null)
                {
                    prefabProperty.objectReferenceValue = targetPrefab;
                }

                arenaSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject lineObject = FindInScene(scene, ShooterLineName);
            if (lineObject == null)
            {
                lineObject = new GameObject(ShooterLineName);
            }

            lineObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, -4f), Quaternion.identity);

            LineRenderer shooterLineRenderer = lineObject.GetComponent<LineRenderer>();
            if (shooterLineRenderer == null)
            {
                shooterLineRenderer = lineObject.AddComponent<LineRenderer>();
            }

            shooterLineRenderer.alignment = LineAlignment.View;
            shooterLineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            shooterLineRenderer.receiveShadows = false;

            if (aimLineMaterial != null)
            {
                shooterLineRenderer.sharedMaterial = aimLineMaterial;
            }

            ShooterLine shooterLine = lineObject.GetComponent<ShooterLine>();
            if (shooterLine == null)
            {
                shooterLine = lineObject.AddComponent<ShooterLine>();
            }

            SerializedObject shotSerialized = new SerializedObject(shot);
            SetObjectReference(shotSerialized, "shooterLine", shooterLine);
            shotSerialized.ApplyModifiedPropertiesWithoutUndo();

            LevelDatabase levelDatabase = CreateLevelDatabase();
            LevelData firstLevel = levelDatabase != null ? levelDatabase.Get(0) : null;

            GameObject managerObject = FindInScene(scene, GameManagerName);
            if (managerObject == null)
            {
                managerObject = new GameObject(GameManagerName);
            }

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(managerObject);

            LevelController levelController = managerObject.GetComponent<LevelController>();
            if (levelController == null)
            {
                levelController = managerObject.AddComponent<LevelController>();
            }

            SerializedObject controllerSerialized = new SerializedObject(levelController);
            SetObjectReference(controllerSerialized, "database", levelDatabase);
            SetObjectReference(controllerSerialized, "level", firstLevel);
            SetObjectReference(controllerSerialized, "arena", arena);
            SetObjectReference(controllerSerialized, "shooter", shot);
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject hudObject = FindInScene(scene, ScoreHudName);
            if (hudObject == null)
            {
                hudObject = new GameObject(ScoreHudName);
            }

            ScoreHud hud = hudObject.GetComponent<ScoreHud>();
            if (hud == null)
            {
                hud = hudObject.AddComponent<ScoreHud>();
            }

            SerializedObject hudSerialized = new SerializedObject(hud);
            SetObjectReference(hudSerialized, "controller", levelController);
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.transform.SetPositionAndRotation(new Vector3(0f, 13f, -8.5f), Quaternion.Euler(56f, 0f, 0f));
                camera.fieldOfView = 50f;
            }

            SerializedObject serialized = new SerializedObject(shot);
            SerializedProperty cameraProperty = serialized.FindProperty("gameCamera");
            SerializedProperty indicatorProperty = serialized.FindProperty("aimIndicator");

            if (cameraProperty != null)
            {
                cameraProperty.objectReferenceValue = camera;
            }

            if (indicatorProperty != null)
            {
                indicatorProperty.objectReferenceValue = indicator;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                EnsureLevelSelectScene(levelDatabase);
            }
            catch (System.Exception sceneException)
            {
                WriteLog("EnsureLevelSelectScene failed: " + sceneException);
                Debug.LogError("[MISKETR] EnsureLevelSelectScene failed: " + sceneException.Message);
            }

            try
            {
                EnsureBuildSettings();
            }
            catch (System.Exception buildException)
            {
                WriteLog("EnsureBuildSettings failed: " + buildException);
                Debug.LogError("[MISKETR] EnsureBuildSettings failed: " + buildException.Message);
            }

            EditorSceneManager.MarkSceneDirty(scene);

            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("[MISKETR] Gameplay scene built: Ground, ShooterMarble, AimIndicator, Arena, ShooterLine, GameManager, ScoreHud, 6 levels and LevelSelect scene are ready.");
        }
        catch (System.Exception exception)
        {
            WriteLog("Build failed: " + exception);
            Debug.LogError("[MISKETR] Scene bootstrap failed: " + exception);
        }
    }

    private static GameObject CreateTargetMarblePrefab(Material visualMaterial, PhysicsMaterial physicsMaterial)
    {
        string path = PrefabsFolder + "/TargetMarble.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (existing != null)
        {
            return existing;
        }

        if (!AssetDatabase.IsValidFolder(PrefabsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        temp.name = "TargetMarble";
        temp.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        MeshRenderer renderer = temp.GetComponent<MeshRenderer>();
        if (renderer != null && visualMaterial != null)
        {
            renderer.sharedMaterial = visualMaterial;
        }

        Collider collider = temp.GetComponent<Collider>();
        if (collider != null && physicsMaterial != null)
        {
            collider.sharedMaterial = physicsMaterial;
        }

        Rigidbody body = temp.AddComponent<Rigidbody>();
        body.mass = 0.05f;
        body.linearDamping = 0.6f;
        body.angularDamping = 0.5f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        temp.AddComponent<TargetMarble>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);

        return prefab;
    }

    private static void SetObjectReference(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static LevelDatabase CreateLevelDatabase()
    {
        EnsureLevelFolders();

        LevelData[] levels = new LevelData[6];

        levels[0] = CreateLevel("Level_01", "Mors", ArenaShape.Triangle, 2.6f, 3, null, 5, 2, 4, 6);
        levels[1] = CreateLevel("Level_02", "Genis Mors", ArenaShape.Triangle, 3f, 4, null, 5, 3, 6, 9);
        levels[2] = CreateLevel("Level_03", "Cember", ArenaShape.Circle, 3f, 0, CircleRings(1, 6), 4, 2, 4, 6);
        levels[3] = CreateLevel("Level_04", "Kalabalik Mors", ArenaShape.Triangle, 3.4f, 5, null, 5, 5, 9, 13);
        levels[4] = CreateLevel("Level_05", "Buyuk Cember", ArenaShape.Circle, 3.6f, 0, CircleRings(1, 6, 8), 5, 5, 9, 13);
        levels[5] = CreateLevel("Level_06", "Usta Mors", ArenaShape.Triangle, 3.8f, 5, null, 4, 5, 8, 12);

        string databasePath = LevelsFolder + "/LevelDatabase.asset";
        LevelDatabase database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(databasePath);

        if (database == null)
        {
            database = ScriptableObject.CreateInstance<LevelDatabase>();
            AssetDatabase.CreateAsset(database, databasePath);
        }

        database.levels = levels;
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        return database;
    }

    private static MarbleRing[] CircleRings(params int[] counts)
    {
        MarbleRing[] rings = new MarbleRing[counts.Length];
        float[] factors = new float[] { 0f, 0.38f, 0.72f, 0.9f };

        for (int i = 0; i < counts.Length; i++)
        {
            rings[i] = new MarbleRing
            {
                count = counts[i],
                radiusFactor = i < factors.Length ? factors[i] : 0.9f,
                angleOffset = i % 2 == 0 ? 0f : 22.5f
            };
        }

        return rings;
    }

    private static void EnsureLevelFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
        {
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        }

        if (!AssetDatabase.IsValidFolder(LevelsFolder))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Levels");
        }
    }

    private static LevelData CreateLevel(string fileName, string displayName, ArenaShape shape, float size, int rows, MarbleRing[] rings, int shots, int oneStar, int twoStar, int threeStar)
    {
        string path = LevelsFolder + "/" + fileName + ".asset";
        LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        bool created = false;

        if (level == null)
        {
            level = ScriptableObject.CreateInstance<LevelData>();
            created = true;
        }

        level.levelName = displayName;
        level.shape = shape;
        level.arenaSize = size;
        level.triangleRows = rows > 0 ? rows : level.triangleRows;
        level.shotCount = shots;
        level.oneStarTarget = oneStar;
        level.twoStarTarget = twoStar;
        level.threeStarTarget = threeStar;
        level.shooterStartPosition = new Vector3(0f, 0.25f, -4f);

        if (rings != null && rings.Length > 0)
        {
            level.rings = rings;
        }

        if (created)
        {
            AssetDatabase.CreateAsset(level, path);
        }
        else
        {
            EditorUtility.SetDirty(level);
        }

        return level;
    }

    private static void EnsureLevelSelectScene(LevelDatabase database)
    {
        string path = "Assets/Scenes/LevelSelect.unity";

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
        {
            return;
        }

        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera menuCamera = cameraObject.AddComponent<Camera>();
        menuCamera.clearFlags = CameraClearFlags.SolidColor;
        menuCamera.backgroundColor = new Color(0.11f, 0.12f, 0.16f, 1f);
        SceneManager.MoveGameObjectToScene(cameraObject, newScene);

        GameObject selectObject = new GameObject("LevelSelect");
        LevelSelectController selectController = selectObject.AddComponent<LevelSelectController>();

        SerializedObject selectSerialized = new SerializedObject(selectController);
        SetObjectReference(selectSerialized, "database", database);
        selectSerialized.ApplyModifiedPropertiesWithoutUndo();

        SceneManager.MoveGameObjectToScene(selectObject, newScene);

        EditorSceneManager.SaveScene(newScene, path);
        EditorSceneManager.CloseScene(newScene, true);
    }

    private static void EnsureBuildSettings()
    {
        string selectPath = "Assets/Scenes/LevelSelect.unity";
        string gamePath = "Assets/Scenes/Game.unity";

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(selectPath) == null)
        {
            WriteLog("EnsureBuildSettings skipped: LevelSelect scene asset missing.");
            return;
        }

        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(selectPath, true),
            new EditorBuildSettingsScene(gamePath, true)
        };

        WriteLog("Build settings set: LevelSelect + Game.");
    }

    private static void WriteLog(string message)
    {
        try
        {
            string directory = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Logs");
            System.IO.Directory.CreateDirectory(directory);
            string file = System.IO.Path.Combine(directory, "misketr-bootstrap.log");
            System.IO.File.AppendAllText(file, System.DateTime.Now.ToString("HH:mm:ss") + "  " + message + "\n");
        }
        catch
        {
        }
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }

        return null;
    }

    private static Material CreateLitMaterial(string assetName, Color color)
    {
        string path = MaterialsFolder + "/" + assetName + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else
        {
            material.color = color;
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.6f);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material CreateAimLineMaterial()
    {
        string path = MaterialsFolder + "/AimLineMaterial.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static PhysicsMaterial CreatePhysicsMaterial(string assetName, float dynamicFriction, float staticFriction, float bounciness)
    {
        string path = MaterialsFolder + "/" + assetName + "Physics.asset";
        PhysicsMaterial existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (existing != null)
        {
            return existing;
        }

        PhysicsMaterial material = new PhysicsMaterial(assetName)
        {
            dynamicFriction = dynamicFriction,
            staticFriction = staticFriction,
            bounciness = bounciness,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Maximum
        };

        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
