using UnityEngine;

namespace LeanCloud.Play {
    internal static class Utils {
        internal static Client NewClient(string userId) {
            // 华东节点，开发版本
            var appId = "FQr8l8LLvdxIwhMHN77sNluX-9Nh9j0Va";
            var appKey = "MJSm46Uu6LjF5eNmqfbuUmt6";
            var playServer = "https://fqr8l8ll.lc-cn-e1-shared.com";
            return new Client(appId, appKey, userId, playServer: playServer);
        }

        internal static Client NewNorthChinaClient(string userId) {
            // 华东节点，开发版本
            var appId = "g2b0X6OmlNy7e4QqVERbgRJR-gzGzoHsz";
            var appKey = "CM91rNV8cPVHKraoFQaopMVT";
            var playServer = "https://g2b0x6om.lc-cn-n1-shared.com";
            return new Client(appId, appKey, userId, playServer: playServer);
        }

        internal static Client NewUSClient(string userId) {
            // 华东节点，开发版本
            var appId = "yR48IPheWK2by2dfouYtlzTU-MdYXbMMI";
            var appKey = "gw3bfkG2EAuN8e9ft5y9kPMq";
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
