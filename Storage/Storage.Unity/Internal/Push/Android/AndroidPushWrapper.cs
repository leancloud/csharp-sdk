using UnityEngine;

namespace LeanCloud.Storage.Internal.Push {
    public class AndroidPushWrapper {
        private const string PUSH_MANAGER_CLASS_NAME = "com.leancloud.push.PushWrapper";

        public static void RegisterHuaWeiPush() {
            AndroidJavaClass pushManagerClazz = new AndroidJavaClass(PUSH_MANAGER_CLASS_NAME);
            pushManagerClazz.CallStatic("registerHuaWeiPush");
        }

        public static void RegisterXiaoMiPush(string appId, string appKey) {
            AndroidJavaClass pushManagerClazz = new AndroidJavaClass(PUSH_MANAGER_CLASS_NAME);
            pushManagerClazz.CallStatic("registerXiaoMiPush", appId, appKey);
        }

        public static void RegisterOPPOPush(string appKey, string appSecret) {
            AndroidJavaClass pushManagerClazz = new AndroidJavaClass(PUSH_MANAGER_CLASS_NAME);
            pushManagerClazz.CallStatic("registerOPPOPush", appKey, appSecret);
        }

        public static void RegisterVIVOPush() {
            AndroidJavaClass pushManagerClazz = new AndroidJavaClass(PUSH_MANAGER_CLASS_NAME);
            pushManagerClazz.CallStatic("registerVIVOPush");
        }

        public static void RegisterMeiZuPush(string appId, string appKey) {
            AndroidJavaClass pushManagerClazz = new AndroidJavaClass(PUSH_MANAGER_CLASS_NAME);
            pushManagerClazz.CallStatic("registerMeiZuPush", appId, appKey);
        }
    }
}
