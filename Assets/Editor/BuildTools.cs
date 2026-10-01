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
    private const string Version = "1.0";
    private const string BuildNumber = "6";
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

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
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
