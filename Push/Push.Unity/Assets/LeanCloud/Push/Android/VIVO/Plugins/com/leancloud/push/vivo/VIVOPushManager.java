package com.leancloud.push.vivo;

import android.content.Context;
import android.util.Log;

import com.leancloud.push.Utils;
import com.unity3d.player.UnityPlayer;
import com.vivo.push.IPushActionListener;
import com.vivo.push.PushClient;

public class VIVOPushManager {
    public static void registerVIVOPush() {
        Context context = UnityPlayer.currentActivity.getApplicationContext();
        PushClient pushClient = PushClient.getInstance(context);
        pushClient.initialize();
        if (!pushClient.isSupport()) {
            Log.e(Utils.TAG, "This device doesn't support VIVO push.");
            return;
        }
        pushClient.turnOnPush(new IPushActionListener() {
            @Override
            public void onStateChanged(int i) {
                Log.i(Utils.TAG, "code: " + i);
                if (i == 0) {
                    String regId = PushClient.getInstance(context).getRegId();
                    Utils.sendDeviceInfo("vivo", regId);
                }
            }
        });
        Utils.intentParser = new Utils.IntentParser();
    }
}
