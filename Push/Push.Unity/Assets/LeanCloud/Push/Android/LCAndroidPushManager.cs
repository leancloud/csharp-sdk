using System;
using System.Collections.Generic;
using UnityEngine;
using LC.Newtonsoft.Json;
using LeanCloud;
using LeanCloud.Common;

public class LCAndroidPushManager {
    public static Dictionary<string, object> GetLaunchData() {
        AndroidJavaClass utilsClazz = new AndroidJavaClass("com.leancloud.push.Utils");
        string json = utilsClazz.CallStatic<string>("getLaunchData");
        if (string.IsNullOrEmpty(json)) {
            return null;
        }
        try {
            Dictionary<string, object> pushData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json,
            LCJsonConverter.Default);
            return pushData;
        } catch (Exception e) {
            LCLogger.Error(e);
            return null;
        }
    }
}
