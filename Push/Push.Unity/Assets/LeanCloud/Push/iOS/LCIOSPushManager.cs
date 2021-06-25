using System.Runtime.InteropServices;

public class LCIOSPushManager {
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _RegisterIOSPush(string teamId);
#endif

    public static void RegisterIOSPush(string teamId) {
#if UNITY_IOS && !UNITY_EDITOR
        _RegisterIOSPush(teamId);
#endif
    }
}
