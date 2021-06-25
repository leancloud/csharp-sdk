package com.leancloud.push.oppo;

import android.content.Context;
import android.util.Log;

import com.heytap.msp.push.HeytapPushManager;
import com.heytap.msp.push.callback.ICallBackResultService;
import com.leancloud.push.Utils;
import com.unity3d.player.UnityPlayer;

public class OPPOPushManager {
    public static void registerOPPOPush(String appKey, String appSecret) {
        if (Utils.isNullOrEmpty(appKey)) {
            Log.e(Utils.TAG, "OPPO appKey is empty");
            return;
        }
        if (Utils.isNullOrEmpty(appSecret)) {
            Log.e(Utils.TAG, "OPPO appSecret is empty");
            return;
        }
        Context context = UnityPlayer.currentActivity.getApplicationContext();
        HeytapPushManager.init(context, true);
        if (!HeytapPushManager.isSupportPush()) {
            Log.e(Utils.TAG, "This device doesn't support OPPO push.");
            return;
        }
        HeytapPushManager.register(context, appKey, appSecret,
                new ICallBackResultService() {
                    @Override
                    public void onRegister(int i, String s) {
                        Log.i(Utils.TAG, "register: " + i + ", " + s);
                        if (i == 0) {
                            Utils.sendDeviceInfo("oppo", s);
                        }
                    }

                    @Override
                    public void onUnRegister(int i) {

                    }

                    @Override
                    public void onSetPushTime(int i, String s) {

                    }

                    @Override
                    public void onGetPushStatus(int i, int i1) {

                    }

                    @Override
                    public void onGetNotificationStatus(int i, int i1) {

                    }
                });
        HeytapPushManager.requestNotificationPermission();
    }
}
