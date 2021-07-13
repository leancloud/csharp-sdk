using UnityEngine;

namespace LeanCloud.Push {
    public class LCMeiZuPushManager {
        public static void RegisterMeiZuPush(string appId, string appKey) {
            AndroidJavaClass pushManagerClazz = new AndroidJavaClass("com.leancloud.push.meizu.MeiZuPushManager");
            pushManagerClazz.CallStatic("registerMeiZuPush", appId, appKey);
        }
    }
}
