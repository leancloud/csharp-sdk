using UnityEngine;

namespace LeanCloud.Play {
    internal static class Utils {
        internal static Client NewClient(string userId) {
            // 华北节点
            var appId = "ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz";
            var appKey = "NUKmuRbdAhg1vrb2wexYo1jo";
            var playServer = "https://ikggdre2.lc-cn-n1-shared.com";
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
