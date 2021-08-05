using UnityEngine;

public class LCFCMPushManager {
    public static void RegisterFCMPush() {
        AndroidJavaClass pushManagerClazz = new AndroidJavaClass("com.leancloud.push.fcm.FCMPushManager");
        pushManagerClazz.CallStatic("registerFCMPush");
    }
}
