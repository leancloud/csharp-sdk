package com.leancloud.push.xiaomi;

import android.content.Context;
import android.util.Log;

import com.leancloud.push.Utils;
import com.unity3d.player.UnityPlayer;
import com.xiaomi.mipush.sdk.MiPushClient;

public class XiaoMiPushManager {
    public static void registerXiaoMiPush(String appId, String appKey) {
        if (Utils.isNullOrEmpty(appId)) {
            Log.e(Utils.TAG, "XiaoMi appId is empty");
            return;
        }
        if (Utils.isNullOrEmpty(appKey)) {
            Log.e(Utils.TAG, "XiaoMi appKey is empty");
            return;
        }
        Context context = UnityPlayer.currentActivity.getApplicationContext();
        MiPushClient.registerPush(context, appId, appKey);
        MiPushClient.turnOnPush(context, new MiPushClient.UPSTurnCallBack() {
            @Override
            public void onResult(MiPushClient.CodeResult codeResult) {
                Log.i(Utils.TAG, "result: " + codeResult.getResultCode());
                if (codeResult.getResultCode() == 0) {
                    String regId = MiPushClient.getRegId(context);
                    Log.i(Utils.TAG, "regId: " + regId);
                }
            }
        });
    }
}
