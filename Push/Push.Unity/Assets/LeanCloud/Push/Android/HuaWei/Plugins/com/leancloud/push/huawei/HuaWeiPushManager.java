package com.leancloud.push.huawei;

import android.util.Log;

import com.huawei.agconnect.config.AGConnectServicesConfig;
import com.huawei.hmf.tasks.OnCompleteListener;
import com.huawei.hmf.tasks.Task;
import com.huawei.hms.aaid.HmsInstanceId;
import com.huawei.hms.common.ApiException;
import com.huawei.hms.push.HmsMessaging;
import com.leancloud.push.Utils;
import com.unity3d.player.UnityPlayer;

public class HuaWeiPushManager {
    public static void registerHuaWeiPush() {
        new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    String appId = AGConnectServicesConfig.fromContext(UnityPlayer.currentActivity).getString("client/app_id");
                    Log.i(Utils.TAG, "app id: " + appId);
                    String regId = HmsInstanceId.getInstance(UnityPlayer.currentActivity).getToken(appId, HmsMessaging.DEFAULT_TOKEN_SCOPE);
                    Log.i(Utils.TAG, "reg id: " + regId);
                    Utils.sendDeviceInfo("HMS", regId);

                    HmsMessaging.getInstance(UnityPlayer.currentActivity).turnOnPush().addOnCompleteListener(new OnCompleteListener<Void>() {
                        @Override
                        public void onComplete(Task<Void> task) {
                            if (task.isSuccessful()) {
                                Log.i(Utils.TAG, "turn on successfully");
                            } else {
                                Log.i(Utils.TAG, "turn on failed");
                            }
                        }
                    });
                } catch (ApiException e) {
                    e.printStackTrace();
                }
            }
        }).start();
    }
}
