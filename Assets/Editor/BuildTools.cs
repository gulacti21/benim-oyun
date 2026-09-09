using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildTools
{
    private const string BundleIdentifier = "com.gulacti.misketr";
    private const string ProductName = "MISKETR";
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

        PlayerSettings.iOS.targetOSVersionString = "14.0";
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;

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
