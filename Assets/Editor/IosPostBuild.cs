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

        // İhracat uyumluluğu: oyun özel/standart dışı şifreleme kullanmıyor. Bu anahtar olmadan
        // App Store Connect her yüklemede "Missing Compliance" sorusunu sorar.
        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        plist.WriteToFile(plistPath);

        // iCloud (anahtar-değer deposu) + Game Center yetkileri. SADECE ücretli hesapla:
        // ücretsiz hesapta bu yetkiler varken Xcode imzalayamaz. Anahtar: MisketrCloud.Enabled.
        if (MisketrCloud.Enabled)
        {
            var caps = new ProjectCapabilityManager(projPath, "Unity-iPhone/misko.entitlements", null, main);
            // Sadece anahtar-değer deposu. 3 parametreli sürüm CloudKit + konteyner de ekliyordu,
            // App ID'de CloudKit yok → imzalama sorun çıkarır (2026-09-30).
            caps.AddiCloud(true, false, false, false, null);
            caps.AddGameCenter();
            caps.WriteToFile();
        }
    }
}
#endif
