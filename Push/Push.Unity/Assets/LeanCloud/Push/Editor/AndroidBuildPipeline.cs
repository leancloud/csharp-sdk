using System.IO;
using UnityEngine;
using UnityEditor.Android;
using UnityEditor.Build;

public class AndroidPostBuildProcessor : IPostGenerateGradleAndroidProject {
    int IOrderedCallback.callbackOrder => 0;

    void IPostGenerateGradleAndroidProject.OnPostGenerateGradleAndroidProject(string path) {
        string LauncherPath = $"{path}/../launcher";
        string LCAndroidPath = $"{Application.dataPath}/LeanCloud/Push/Android";
        // 拷贝华为推送配置文件
        string HMSFilename = "agconnect-services.json";
        string hmsConfigPath = $"{LCAndroidPath}/HuaWei/hms/{HMSFilename}";
        if (File.Exists(hmsConfigPath)) {
            File.Copy(hmsConfigPath, $"{LauncherPath}/{HMSFilename}", true);
        } else {
            Debug.LogWarning($"No {HMSFilename} in {hmsConfigPath} for hms.");
        }
        // 拷贝 FCM 推送配置文件
        string FCMFilename = "google-services.json";
        string fcmConfigPath = $"{LCAndroidPath}/FCM/config/{FCMFilename}";
        if (File.Exists(fcmConfigPath)) {
            File.Copy(fcmConfigPath, $"{LauncherPath}/{FCMFilename}", true);
        } else {
            Debug.LogWarning($"No {FCMFilename} in {fcmConfigPath} for fcm.");
        }
    }
}
