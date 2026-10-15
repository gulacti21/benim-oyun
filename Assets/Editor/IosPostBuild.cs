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
    const string AttEn = "This permission is used to make the rewarded ads you choose to watch more relevant to you. The game and ads work the same if you don't allow it.";
    const string AttTr = "Bu izin, izlemeyi seçtiğin ödüllü reklamların sana daha uygun olması için kullanılır. İzin vermesen de oyun ve reklamlar aynı şekilde çalışır.";

    static void WriteLoc(string root, string lang, string att)
    {
        string dir = Path.Combine(root, "MiskoLoc", lang + ".lproj");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "InfoPlist.strings"),
            "\"NSUserTrackingUsageDescription\" = \"" + att.Replace("\"", "\\\"") + "\";\n", new System.Text.UTF8Encoding(false));
    }

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
        // 1.1 ATT: ödüllü reklam için izin penceresinin açıklaması (MiskoAds/MiskoATT.mm).
        // Info.plist'te İngilizce (inceleme ekibi İngilizce cihazla bakar), Türkçe telefonda
        // tr.lproj/InfoPlist.strings'teki metin görünür. Klasör referansı olarak eklenir ki
        // paket içinde tr.lproj/ klasörü korunsun (düz dosya kökte düzleşirdi).
        plist.root.SetString("NSUserTrackingUsageDescription", AttEn);
        var locs = plist.root.CreateArray("CFBundleLocalizations");
        locs.AddString("en"); locs.AddString("tr");
        plist.WriteToFile(plistPath);
        WriteLoc(pathToBuiltProject, "en", AttEn);
        WriteLoc(pathToBuiltProject, "tr", AttTr);
        proj.ReadFromFile(projPath);
        foreach (var lang in new[] { "en", "tr" })
        {
            string guid = proj.AddFolderReference(Path.Combine(pathToBuiltProject, "MiskoLoc", lang + ".lproj"), "MiskoLoc/" + lang + ".lproj");
            proj.AddFileToBuild(main, guid);
        }
        proj.WriteToFile(projPath);

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

            // AddiCloud boş bir "icloud-container-identifiers" dizisi de yazıyor; build 1'de elle
            // silinmişti, profilde CloudKit yok. Her derlemede otomatik temizle.
            string entPath = Path.Combine(pathToBuiltProject, "Unity-iPhone/misko.entitlements");
            if (File.Exists(entPath))
            {
                var ent = new PlistDocument();
                ent.ReadFromFile(entPath);
                ent.root.values.Remove("com.apple.developer.icloud-container-identifiers");
                ent.WriteToFile(entPath);
            }
        }
    }
}
#endif
