package com.leancloud.push.meizu;

import android.content.Context;
import android.util.Log;

import com.leancloud.push.Utils;
import com.meizu.cloud.pushsdk.MzPushMessageReceiver;
import com.meizu.cloud.pushsdk.platform.message.PushSwitchStatus;
import com.meizu.cloud.pushsdk.platform.message.RegisterStatus;
import com.meizu.cloud.pushsdk.platform.message.SubAliasStatus;
import com.meizu.cloud.pushsdk.platform.message.SubTagsStatus;
import com.meizu.cloud.pushsdk.platform.message.UnRegisterStatus;

public class MeiZuMessageReceiver extends MzPushMessageReceiver {

    @Override
    public void onRegisterStatus(Context context, RegisterStatus registerStatus) {
        if (context == null || registerStatus == null) {
            return;
        }
        Log.i(Utils.TAG, registerStatus.toString());
        String regId = registerStatus.getPushId();
        if (regId == null || regId.length() == 0) {
            return;
        }
        Utils.sendDeviceInfo("mz", regId);
    }

    @Override
    public void onUnRegisterStatus(Context context, UnRegisterStatus unRegisterStatus) {

    }

    @Override
    public void onPushStatus(Context context, PushSwitchStatus pushSwitchStatus) {

    }

    @Override
    public void onSubTagsStatus(Context context, SubTagsStatus subTagsStatus) {

    }

    @Override
    public void onSubAliasStatus(Context context, SubAliasStatus subAliasStatus) {

    }
}
