package com.leancloud.push.honor;

import android.util.Log;

import com.hihonor.push.sdk.HonorPushCallback;
import com.hihonor.push.sdk.HonorPushClient;
import com.leancloud.push.Utils;
import com.unity3d.player.UnityPlayer;

public class HonorPushManager {
    public static void registerHonorPush() {
        HonorPushClient.getInstance().init(UnityPlayer.currentActivity, true);
    }
}
