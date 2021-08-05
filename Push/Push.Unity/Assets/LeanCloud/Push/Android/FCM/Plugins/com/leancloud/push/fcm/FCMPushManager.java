package com.leancloud.push.fcm;

import com.google.firebase.messaging.FirebaseMessaging;
import com.leancloud.push.Utils;

public class FCMPushManager {
    public static void registerFCMPush() {
        FirebaseMessaging.getInstance().getToken();
        Utils.intentParser = new Utils.IntentParser();
    }
}
