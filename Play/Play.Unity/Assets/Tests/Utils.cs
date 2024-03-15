using UnityEngine;

namespace LeanCloud.Play {
    internal static class Utils {
        internal static Client NewClient(string userId) {
            // 华北节点
            var appId = "6dQxWcEFfC9eMnLQw6IG4Vbm-vofoqvbj";
            var appKey = "hEyVtlxRhcAxGNvHbnExpMBm";
            var playServer = "https://6dqxwcef.api.uc-test1.lncldapi.com";
            return new Client(appId, appKey, userId, playServer: playServer);
        }

        internal static Client NewNorthChinaClient(string userId) {
            return NewClient(userId);
        }

        internal static Client NewUSClient(string userId) {
            // 海外节点
            var appId = "ldCRr8t23k3ydo7FxmJlKQmn-MdYXbMMI";
            var appKey = "GwQDHkmsQTSF2ZXWegzXio5F";
            return new Client(appId, appKey, userId);
        }

        internal static void Log(LCLogLevel level, string info) { 
            switch (level) {
                case LCLogLevel.Debug:
                    Debug.LogFormat("[DEBUG] {0}", info);
                    break;
                case LCLogLevel.Warn:
                    Debug.LogFormat("[WARNING] {0}", info);
                    break;
                case LCLogLevel.Error:
                    Debug.LogFormat("[ERROR] {0}", info);
                    break;
                default:
                    Debug.Log(info);
                    break;
            }
        }
    }
}
