package com.leancloud.push;

import android.content.Context;
import android.content.Intent;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.os.Bundle;
import android.support.annotation.NonNull;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import org.json.JSONObject;

import java.util.HashMap;
import java.util.Map;
import java.util.TimeZone;
import java.util.UUID;

public class Utils {
    public final static String TAG = "LCPush";

    private final static String PUSH_BRIDGE = "__LC_PUSH_BRIDGE__";
    private final static String ON_REGISTER_PUSH = "OnRegisterPush";

    public static IntentParser intentParser = null;

    public static boolean isNullOrEmpty(String str) {
        return str == null || str.length() == 0;
    }

    public static String getMetaString(@NonNull Context context, String key) {
        String pkgName = context.getPackageName();
        try {
            ApplicationInfo appInfo = context.getPackageManager().getApplicationInfo(pkgName, PackageManager.GET_META_DATA);
            if (appInfo.metaData.containsKey(key)) {
                Object val = appInfo.metaData.get(key);
                return (String) val;
            }
            return null;
        } catch (PackageManager.NameNotFoundException e) {
            Log.e(Utils.TAG, e.toString());
            return null;
        }
    }

    public static void putMetaString(@NonNull Context context, String key, String value) {
        String pkgName = context.getPackageName();
        try {
            ApplicationInfo appInfo = context.getPackageManager().getApplicationInfo(pkgName, PackageManager.GET_META_DATA);
            appInfo.metaData.putString(key, value);
        } catch (Exception e) {
            Log.e(Utils.TAG, e.toString());
        }
    }

    public static void sendDeviceInfo(String vendor, String regId) {
        Map<String, Object> deviceInfo = new HashMap<>();
        deviceInfo.put("deviceType", "android");
        deviceInfo.put("vendor", vendor);
        deviceInfo.put("registrationId", regId);
        // 这里先每次生成一个 installationId，一是目前不会用 installationId；二是建立长连接后，这个值刷新似乎没什么影响
        UUID uuid = UUID.randomUUID();
        deviceInfo.put("installationId", uuid);
        TimeZone tz = TimeZone.getDefault();
        deviceInfo.put("timeZone", tz.getID());
        String json = (new JSONObject(deviceInfo)).toString();
        UnityPlayer.UnitySendMessage(PUSH_BRIDGE, ON_REGISTER_PUSH, json);
    }

    public static String getLaunchData() {
        Intent intent = UnityPlayer.currentActivity.getIntent();
        if (intent == null) {
            return null;
        }

        if (intentParser != null) {
            return intentParser.Parse();
        }

        if (intent.hasExtra("content")) {
            return intent.getStringExtra("content");
        }

        return null;
    }

    /**
     * 用来解析 vivo, oppo 的通知数据
     */
    public static class IntentParser {
        public String Parse() {
            Intent intent = UnityPlayer.currentActivity.getIntent();
            if (intent == null) {
                return null;
            }

            Bundle bundle = intent.getExtras();
            if (bundle == null) {
                return null;
            }

            Map<String, Object> pushData = new HashMap<>();
            for (String key : bundle.keySet()) {
                Log.i(Utils.TAG, key);
                pushData.put(key, bundle.get(key));
            }
            Log.i(TAG, (new JSONObject(pushData)).toString());
            return (new JSONObject(pushData)).toString();
        }
    }
}
