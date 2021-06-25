package com.leancloud.push.meizu;

import android.content.Context;
import android.util.Log;

import com.leancloud.push.Utils;
import com.meizu.cloud.pushsdk.PushManager;
import com.unity3d.player.UnityPlayer;

public class MeiZuPushManager {
    public static void registerMeiZuPush(String appId, String appKey) {
        if (Utils.isNullOrEmpty(appId)) {
            Log.e(Utils.TAG, "MeiZu appId is empty");
            return;
        }
        if (Utils.isNullOrEmpty(appKey)) {
            Log.e(Utils.TAG, "MeiZu appKey is empty");
            return;
        }
        Context context = UnityPlayer.currentActivity.getApplicationContext();
        PushManager.register(context, appId, appKey);
    }
}
