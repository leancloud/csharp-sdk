using System.IO;
using UnityEngine;
using UnityEditor.Android;
using UnityEditor.Build;

public class AndroidPostBuildProcessor : IPostGenerateGradleAndroidProject {
#if UNITY_ANDROID
    int IOrderedCallback.callbackOrder => 0;

    void IPostGenerateGradleAndroidProject.OnPostGenerateGradleAndroidProject(string path) {
        // 拷贝华为推送配置文件
        string hmsConfigPath = $"{Application.dataPath}/LeanCloud/Push/Android/Config/hms/agconnect-services.json";
        if (File.Exists(hmsConfigPath)) {
            File.Copy(hmsConfigPath, $"{path}/../launcher/agconnect-services.json", true);
        } else {
            Debug.LogWarning($"No agconnect-services.json in {hmsConfigPath} for hms.");
        }
    }
#endif
}
