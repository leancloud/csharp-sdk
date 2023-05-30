using System.Runtime.InteropServices;

public class LCIOSPushManager {
#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern void _RegisterIOSPush(string teamId);

    [DllImport("__Internal")]
    private static extern void _GetLaunchData(string callbackId);

    [DllImport("__Internal")]
    private static extern void _SetIconBadgeNumber(int number);
#endif

    public static void RegisterIOSPush(string teamId) {
#if UNITY_IOS
        _RegisterIOSPush(teamId);
#endif
    }

    public static void GetLaunchData(string callbackId) {
#if UNITY_IOS
        _GetLaunchData(callbackId);
#endif
    }

    public static void SetIconBadgeNumber(int number) {
#if UNITY_IOS
        _SetIconBadgeNumber(number);
#endif
    }
}