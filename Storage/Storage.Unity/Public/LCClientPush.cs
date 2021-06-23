using LeanCloud.Storage.Internal.Push;

namespace LeanCloud.Push {
    public partial class LCPush {
        public static void RegisterIOSPush(string teamId) {
            IOSPushWrapper.RegisterIOSPush(teamId);
        }

        public static void RegisterHuaWeiPush() {
            AndroidPushWrapper.RegisterHuaWeiPush();
        }

        public static void RegisterXiaoMiPush(string appId, string appKey) {
            AndroidPushWrapper.RegisterXiaoMiPush(appId, appKey);
        }

        public static void RegisterVIVOPush() {
            AndroidPushWrapper.RegisterVIVOPush();
        }

        public static void RegisterOPPOPush(string appKey, string appSecret) {
            AndroidPushWrapper.RegisterOPPOPush(appKey, appSecret);
        }

        public static void RegisterMeiZuPush(string appId, string appKey) {
            AndroidPushWrapper.RegisterMeiZuPush(appId, appKey);
        }
    }
}
