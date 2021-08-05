package com.leancloud.push.fcm;

import android.util.Log;

import androidx.annotation.NonNull;

import com.google.firebase.messaging.FirebaseMessagingService;
import com.google.firebase.messaging.RemoteMessage;
import com.leancloud.push.Utils;

public class FCMMessageService extends FirebaseMessagingService {
    @Override
    public void onNewToken(@NonNull String s) {
        Log.i(Utils.TAG, "regId: " + s);
        Utils.sendDeviceInfo("fcm", s);
    }
}
