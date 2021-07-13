package com.leancloud.push.xiaomi;

import android.content.Context;
import android.content.Intent;
import android.util.Log;

import com.leancloud.push.Utils;
import com.xiaomi.mipush.sdk.ErrorCode;
import com.xiaomi.mipush.sdk.MiPushClient;
import com.xiaomi.mipush.sdk.MiPushCommandMessage;
import com.xiaomi.mipush.sdk.MiPushMessage;
import com.xiaomi.mipush.sdk.PushMessageReceiver;

import java.util.List;

public class XiaoMiMessageReceiver extends PushMessageReceiver {
    @Override
    public void onCommandResult(Context context, MiPushCommandMessage miPushCommandMessage) {
        Log.i(Utils.TAG, miPushCommandMessage.toString());
        String command = miPushCommandMessage.getCommand();
        List<String> arguments = miPushCommandMessage.getCommandArguments();
        if (MiPushClient.COMMAND_REGISTER.equals(command)) {
            if (miPushCommandMessage.getResultCode() == ErrorCode.SUCCESS) {
                String regId = arguments.get(0);
                Utils.sendDeviceInfo("mi", regId);
            }
        }
    }

    @Override
    public void onNotificationMessageClicked(Context context, MiPushMessage miPushMessage) {
        super.onNotificationMessageClicked(context, miPushMessage);
        Log.i(Utils.TAG, miPushMessage.toString());
        Intent intent = new Intent();
        intent.putExtra("title", miPushMessage.getTitle());
        intent.putExtra("description", miPushMessage.getDescription());
        intent.putExtra("content", miPushMessage.getContent());
        intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TOP);
        intent.setClass(context, com.unity3d.player.UnityPlayerActivity.class);
        context.startActivity(intent);
    }
}
