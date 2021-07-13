using UnityEngine;

namespace LeanCloud.Push {
    public class LCOPPOPushManager {
        public static void RegisterOPPOPush(string appKey, string appSecret) {
            AndroidJavaClass pushManagerClazz = new AndroidJavaClass("com.leancloud.push.oppo.OPPOPushManager");
            pushManagerClazz.CallStatic("registerOPPOPush", appKey, appSecret);
        }
    }
}
