using System;
using System.IO;

namespace LeanCloud.Storage.Internal {
    /// <summary>
    /// 临时方案，后面将有 Android 和 iOS 提供 device token
    /// </summary>
    public class InstallationIdController {
        private string installationId;
        private readonly object mutex = new object();

        public string Get() {
            if (installationId == null) {
                lock (mutex) {
                    if (installationId == null) {
                        string installationPath = "installation.conf";
                        // 文件读取或从 Native 平台读取
                        if (File.Exists(installationPath)) {
                            using (StreamReader reader = new StreamReader(installationPath)) {
                                installationId = reader.ReadToEnd();
                                if (installationId != null) {
                                    return installationId;
                                }
                            }
                        }
                        // 生成新的 device token
                        Guid newInstallationId = Guid.NewGuid();
                        installationId = newInstallationId.ToString();
                        // 写回文件
                        using (StreamWriter writer = new StreamWriter(installationPath)) {
                            writer.Write(installationId);
                        }
                    }
                }
            }
            return installationId;
        }
    }
}
