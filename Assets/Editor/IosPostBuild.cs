#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

// iOS build'inden sonra Xcode projesine gerekli framework'leri ekler.
// MisketrNotify.mm UserNotifications kullaniyor; Unity bunu kendiliginden baglamiyor,
// baglanmazsa "Undefined symbol: _OBJC_CLASS_$_UNUserNotificationCenter" hatasi alinir.
public static class IosPostBuild
{
    [PostProcessBuild(100)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        var proj = new PBXProject();
        proj.ReadFromFile(projPath);

        string framework = proj.GetUnityFrameworkTargetGuid();
        string main = proj.GetUnityMainTargetGuid();

        proj.AddFrameworkToProject(framework, "UserNotifications.framework", false);
        proj.AddFrameworkToProject(main, "UserNotifications.framework", false);

        proj.WriteToFile(projPath);
    }
}
#endif
