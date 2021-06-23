using System.Runtime.InteropServices;

namespace LeanCloud.Storage.Internal.Push {
    public class IOSPushWrapper {
        [DllImport("__Internal")]
        private static extern void _RequestDeviceInfo();

        [DllImport("__Internal")]
        private static extern void _RequestIOSPush();

        public static void RequestDeviceInfo() {
            _RequestDeviceInfo();
        }

        public static void RequestIOSPush() {
            _RequestIOSPush();
        }
    }
}
