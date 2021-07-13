using UnityEngine;

namespace LeanCloud.Push {
    public class LCXiaoMiPushManager {
        public static void RegisterXiaoMiPush(string appId, string appKey) {
            AndroidJavaClass pushManagerClazz = new AndroidJavaClass("com.leancloud.push.xiaomi.XiaoMiPushManager");
            pushManagerClazz.CallStatic("registerXiaoMiPush", appId, appKey);
        }
    }
}
