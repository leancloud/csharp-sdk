using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LeanCloud;
using LeanCloud.Storage;
using LeanCloud.Push;
using System.Threading.Tasks;
using LC.Newtonsoft.Json;

public class PushDemo : MonoBehaviour {
    public const string IOS_TEAM_ID = "Y6X8P44TM5";

    public const string XIAOMI_APP_ID = "2882303761517894746";
    public const string XIAOMI_APP_KEY = "5981789429746";

    public const string OPPO_APP_KEY = "95d0685734ad470c9b4e3df6d537d562";
    public const string OPPO_APP_SECRET = "58d84d82a0a14aa1b52e66e3224c35a3";

    public const string MEIZU_APP_ID = "";
    public const string MEIZU_APP_KEY = "";

    public Text deviceInfoText;
    public Text launchDataText;

    public InputField pushDataInputField;

    public Toggle badgeToggle;
    public Toggle soundToggle;
    public Toggle alertToggle;
    public Toggle listToggle;
    public Toggle bannerToggle;

    private void Awake() {
        LCLogger.LogDelegate = (level, message) => {
            string fullLog = $"LEANCLOUD: {message}";
            switch (level) {
                case LCLogLevel.Debug:
                    Debug.Log(fullLog);
                    break;
                case LCLogLevel.Warn:
                    Debug.LogWarning(fullLog);
                    break;
                case LCLogLevel.Error:
                    Debug.LogError(fullLog);
                    break;
                default:
                    break;
            }
        };

        deviceInfoText.text = $"{SystemInfo.deviceModel}, {SystemInfo.deviceName}, {SystemInfo.deviceType}, {SystemInfo.operatingSystem}";

        InitChineseApp();
        //InitInternationalApp();
    }

    private void InitInternationalApp() {
        LCApplication.Initialize("HudJvWWmAuGMifwxByDVLmQi-MdYXbMMI", "YjoQr1X8wHoFIfsSGXzeJaAM");

        LCFCMPushManager.RegisterFCMPush();
    }

    private void InitChineseApp() {
        LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");

        if (Application.platform == RuntimePlatform.IPhonePlayer) {
            LCIOSPushManager.RegisterIOSPush(IOS_TEAM_ID);
            LCIOSPushManager.SetIconBadgeNumber(0);
        } else if (Application.platform == RuntimePlatform.Android) {
            string deviceModel = SystemInfo.deviceModel.ToLower();
            if (deviceModel.Contains("huawei")) {
                LCHuaWeiPushManager.RegisterHuaWeiPush();
            } else if (deviceModel.Contains("oppo")) {
                LCOPPOPushManager.RegisterOPPOPush(OPPO_APP_KEY, OPPO_APP_SECRET);
            } else if (deviceModel.Contains("vivo")) {
                LCVIVOPushManager.RegisterVIVOPush();
            } else if (deviceModel.Contains("meizu")) {
                LCMeiZuPushManager.RegisterMeiZuPush(MEIZU_APP_ID, MEIZU_APP_KEY);
            } else /*if (deviceModel.Contains("xiaomi"))*/ {
                // 其他的厂商可以尝试注册小米推送
                LCXiaoMiPushManager.RegisterXiaoMiPush(XIAOMI_APP_ID, XIAOMI_APP_KEY);
            }
        }
    }

    private async void Start() {
        LCPushBridge.Instance.OnReceiveNotification += notification => {
            launchDataText.text = $"Receive: {JsonConvert.SerializeObject(notification)}";
        };

        Dictionary<string, object> launchData = await LCPushBridge.Instance.GetLaunchData();
        if (launchData != null && launchData.Count > 0) {
            launchDataText.text = $"Launch: {JsonConvert.SerializeObject(launchData)}";
        }

        _ = UpdateDeviceInfo();
    }

    async Task UpdateDeviceInfo() {
        while (true) {
            await Task.Delay(1000);

            LCInstallation installation = await LCInstallation.GetCurrent();
            deviceInfoText.text = installation.ToString();
        }
    }

    public async void OnSendClicked() {
        string pushData = pushDataInputField.text;
        if (string.IsNullOrEmpty(pushData)) {
            return;
        }

        try {
            LCPush push = new LCPush {
                Data = new Dictionary<string, object> {
                    { "alert", pushData }
                },
                IOSEnvironment = LCPush.IOSEnvironmentDev,
            };

            LCInstallation installation = await LCInstallation.GetCurrent();
            push.Query.WhereEqualTo("objectId", installation.ObjectId);

            await push.Send();
        } catch (Exception e) {
            Debug.LogError(e);
        }
    }

    public async void OnGetLaunchDataClicked() {
        Dictionary<string, object> launchData = await LCPushBridge.Instance.GetLaunchData();
        if (launchData == null) {
            return;
        }
        launchDataText.text = JsonConvert.SerializeObject(launchData);
        Debug.Log("-----------------------------------------------");
        foreach (KeyValuePair<string, object> kv in launchData) {
            Debug.Log($"{kv.Key} : {kv.Value}");
        }
    }

    public void OnSetNotificationPresentOptionClicked() {
#if UNITY_IOS
        LCIOSNotificationPresentationOption option = LCIOSNotificationPresentationOption.None;
        if (badgeToggle.isOn) {
            option |= LCIOSNotificationPresentationOption.Badge;
        }
        if (soundToggle.isOn) {
            option |= LCIOSNotificationPresentationOption.Sound;
        }
        if (alertToggle.isOn) {
            option |= LCIOSNotificationPresentationOption.Alert;
        }
        if (listToggle.isOn) {
            option |= LCIOSNotificationPresentationOption.List;
        }
        if (bannerToggle.isOn) {
            option |= LCIOSNotificationPresentationOption.Banner;
        }
        Debug.Log($"option: {option}");
        LCIOSPushManager.SetNotificationPresentationOption(option);
#endif
    }
}
