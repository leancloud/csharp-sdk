package com.leancloud.push.huawei;

import android.util.Log;

import com.huawei.hms.push.HmsMessageService;
import com.leancloud.push.Utils;

public class HuaWeiMessageService extends HmsMessageService {
    @Override
    public void onNewToken(String token) {
        super.onNewToken(token);
        Log.i(Utils.TAG, "Huawei onNewToken: " + token);
        Utils.sendDeviceInfo("HMS", token);
    }
}
