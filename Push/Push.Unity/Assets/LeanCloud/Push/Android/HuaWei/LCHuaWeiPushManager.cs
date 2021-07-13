using UnityEngine;

namespace LeanCloud.Push {
    public class LCHuaWeiPushManager {
        public static void RegisterHuaWeiPush() {
            AndroidJavaClass pushManagerClazz = new AndroidJavaClass("com.leancloud.push.huawei.HuaWeiPushManager");
            pushManagerClazz.CallStatic("registerHuaWeiPush");
        }
    }
}
