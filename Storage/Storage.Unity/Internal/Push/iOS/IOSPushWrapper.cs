using System.Runtime.InteropServices;

namespace LeanCloud.Storage.Internal.Push {
    public class IOSPushWrapper {
        [DllImport("__Internal")]
        private static extern void _RegisterIOSPush(string teamId);

        public static void RegisterIOSPush(string teamId) {
            _RegisterIOSPush(teamId);
        }
    }
}
