using UnityEngine;

namespace LeanCloud.Push {
    public class LCHonorPushManager {
        public static void RegisterHonorPush() {
            AndroidJavaClass pushManagerClazz = new AndroidJavaClass("com.leancloud.push.honor.HonorPushManager");
            pushManagerClazz.CallStatic("registerHonorPush");
        }
    }
}