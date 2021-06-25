using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

using UnityEditor.iOS.Xcode;

public static class IOSBuildProcessor {
#if UNITY_IOS
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path) {
        // 开启远程推送宏
        string preprocessorPath = path + "/Classes/Preprocessor.h";
        string text = File.ReadAllText(preprocessorPath);
        text = text.Replace("UNITY_USES_REMOTE_NOTIFICATIONS 0", "UNITY_USES_REMOTE_NOTIFICATIONS 1");
        File.WriteAllText(preprocessorPath, text);

        // 添加推送能力
        string pbxPath = PBXProject.GetPBXProjectPath(path);
        Debug.Log($"pbxPath: {pbxPath}");
        PBXProject proj = new PBXProject();
        proj.ReadFromFile(pbxPath);

        string guid = proj.GetUnityMainTargetGuid();
        Debug.Log($"guid: {guid}");
        string[] ids = Application.identifier.Split('.');
        string entitlementsPath = $"Unity-iPhone/{ids[ids.Length - 1]}.entitlements";
        Debug.Log($"entitlementsPath: {entitlementsPath}");

        ProjectCapabilityManager capManager = new ProjectCapabilityManager(pbxPath, entitlementsPath, null, guid);
        capManager.AddPushNotifications(true);
        capManager.WriteToFile();
    }
#endif
}
