using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildTools
{
    // Yayın kimliği (2026-09-30). Eski test kimliği com.gulacti.misketr idi; değişince telefondaki
    // test uygulaması ayrı kalır, yeni uygulama sıfırdan başlar.
    private const string BundleIdentifier = "com.gulacti.misko";
    private const string TeamId = "69W6P52PA2";
    private const string Version = "1.2";
    private const string BuildNumber = "21";
    // ANDROID: versionCode her Play yüklemesinde bir artar (iOS build numarasından bağımsız).
    private const int AndroidVersionCode = 3;
    // İmza anahtarı repo DIŞINDA: ~/.misko-android/keystore.properties (şifre burada, commit edilmez).
    private static readonly string KeystoreProps =
        Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), ".misko-android", "keystore.properties");
    // Ana ekrandaki ad. Mağaza adı "Misko: Misket Oyunu" (App Store Connect).
    private const string ProductName = "Misko";
    private const string CompanyName = "Gulacti";

    private static readonly string[] Scenes =
    {
        "Assets/Scenes/LevelSelect.unity",
        "Assets/Scenes/Game.unity"
    };

    public static void ApplyPlayerSettings()
    {
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = ProductName;

        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleIdentifier);

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        PlayerSettings.iOS.appleDeveloperTeamID = TeamId;
        // Mağaza sürümü. Her yeni TestFlight yüklemesinde BuildNumber'ı bir artır (aynı numara reddedilir).
        PlayerSettings.bundleVersion = Version;
        PlayerSettings.iOS.buildNumber = BuildNumber;
        // 1.0 sadece iPhone (2026-09-25): Xcode 26'da UIRequiresFullScreen geçmiyor, iPad'i destekleyen
        // portre uygulama ITMS-90474 ile yüklenemiyor. iPad'de iPhone uygulaması olarak açılır.
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
        // 1.2 (build 21): uygulama içi etkinlik (In-App Event) deep link ister; misko://lunapark oyunu açar.
        PlayerSettings.iOS.iOSUrlSchemes = new[] { "misko" };

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        // Google Play: 31 Ağustos 2026'dan beri yeni uygulamalar API 36 (Android 16) hedeflemeli.
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
        PlayerSettings.Android.bundleVersionCode = AndroidVersionCode;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;   // Play 64-bit şartı
        PlayerSettings.Android.useCustomKeystore = false;   // imza, BuildAndroid içinde dosyadan
    }

    // Google Play için AAB (bundle). apk=true: telefona doğrudan kurmak için test APK'sı.
    [MenuItem("MISKETR/Build Android (AAB)")] public static void BuildAndroidBundle() => BuildAndroid(false);
    [MenuItem("MISKETR/Build Android (test APK)")] public static void BuildAndroidApk() => BuildAndroid(true);

    public static void BuildAndroid(bool apk)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.LogError("[MISKETR] Aktif platform Android degil. Once Android'e gec.");
            return;
        }
        ApplyPlayerSettings();

        if (!File.Exists(KeystoreProps))
        {
            Debug.LogError("[MISKETR] Imza anahtari yok: " + KeystoreProps);
            return;
        }
        var props = new System.Collections.Generic.Dictionary<string, string>();
        foreach (var line in File.ReadAllLines(KeystoreProps))
        {
            int i = line.IndexOf('=');
            if (i > 0) props[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
        }
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = props["storeFile"];
        PlayerSettings.Android.keystorePass = props["storePassword"];
        PlayerSettings.Android.keyaliasName = props["keyAlias"];
        PlayerSettings.Android.keyaliasPass = props["keyPassword"];

        EditorUserBuildSettings.buildAppBundle = !apk;
        // Play Console çökme raporları için sembol dosyası (Play "debug symbols yok" uyarısı veriyordu).
        UnityEditor.Android.UserBuildSettings.DebugSymbols.level = Unity.Android.Types.DebugSymbolLevel.SymbolTable;
        UnityEditor.Android.UserBuildSettings.DebugSymbols.format = Unity.Android.Types.DebugSymbolFormat.Zip | Unity.Android.Types.DebugSymbolFormat.IncludeInBundle;
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string dir = Path.Combine(projectRoot, "Builds", "Android");
        Directory.CreateDirectory(dir);
        string outputPath = Path.Combine(dir, "Misko-" + Version + "-" + AndroidVersionCode + (apk ? ".apk" : ".aab"));

        try
        {
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                Debug.LogError("[MISKETR] Android build failed: " + report.summary.result);
            else
                Debug.Log("[MISKETR] Android build: " + outputPath);
        }
        finally
        {
            // Şifreler ProjectSettings'e yazılmasın (Unity zaten şifreleri kaydetmez; yol da temizlensin).
            PlayerSettings.Android.keystorePass = "";
            PlayerSettings.Android.keyaliasPass = "";
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystoreName = "";
            PlayerSettings.Android.keyaliasName = "";
        }
    }

    [MenuItem("MISKETR/Build iOS Xcode Project")]
    public static void BuildIos()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
        {
            EditorUtility.DisplayDialog(
                "Once platform degistir",
                "Aktif platform iOS degil. File > Build Profiles ekranindan iOS'a gecip tekrar dene.",
                "Tamam");
            return;
        }

        ApplyPlayerSettings();

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputPath = Path.Combine(projectRoot, "Builds", "iOS");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = outputPath,
            target = BuildTarget.iOS,
            targetGroup = BuildTargetGroup.iOS,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("[MISKETR] iOS export failed: " + report.summary.result + ". See Console for build errors.");
            return;
        }

        Debug.Log("[MISKETR] Xcode project written to: " + outputPath);
        EditorUtility.RevealInFinder(outputPath);
    }
}
