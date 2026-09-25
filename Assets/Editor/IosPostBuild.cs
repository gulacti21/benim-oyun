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
        // MisketrCloud.mm GameKit kullanıyor: yetki kapalı olsa da bağlanmalı, yoksa link hatası.
        proj.AddFrameworkToProject(framework, "GameKit.framework", false);

        proj.WriteToFile(projPath);

        // iCloud (anahtar-değer deposu) + Game Center yetkileri. SADECE ücretli hesapla:
        // ücretsiz hesapta bu yetkiler varken Xcode imzalayamaz. Anahtar: MisketrCloud.Enabled.
        if (MisketrCloud.Enabled)
        {
            var caps = new ProjectCapabilityManager(projPath, "Unity-iPhone/misko.entitlements", null, main);
            caps.AddiCloud(true, false, null);
            caps.AddGameCenter();
            caps.WriteToFile();
        }
    }
}
#endif
