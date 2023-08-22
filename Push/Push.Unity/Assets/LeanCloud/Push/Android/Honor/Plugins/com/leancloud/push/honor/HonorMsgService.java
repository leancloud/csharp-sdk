package com.leancloud.push.honor;

import android.util.Log;

import com.hihonor.push.sdk.HonorMessageService;
import com.leancloud.push.Utils;

public class HonorMsgService extends HonorMessageService {
    @Override
    public void onNewToken(String token) {
        super.onNewToken(token);
        Log.i(Utils.TAG, "Honor onNewToken: " + token);
        Utils.sendDeviceInfo("honor", token);
    }
}
