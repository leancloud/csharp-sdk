using UnityEngine;

namespace LeanCloud.Push {
    public class LCVIVOPushManager {
        public static void RegisterVIVOPush() {
            AndroidJavaClass pushManagerClazz = new AndroidJavaClass("com.leancloud.push.vivo.VIVOPushManager");
            pushManagerClazz.CallStatic("registerVIVOPush");
        }
    }
}
