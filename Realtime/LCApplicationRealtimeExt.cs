using System;
using System.Threading.Tasks;
using LeanCloud.Realtime;
using LeanCloud.Realtime.Internal;

namespace LeanCloud {
    public static class LCApplicationRealtimeExt {

        public static async Task<LCIMClient> CreateIMClient(this LCApplication application, string clientId) {
            if (string.IsNullOrEmpty(clientId)) {
                throw new ArgumentNullException(nameof(clientId));
            }

            LCIMClient client = new LCIMClient(clientId);
            return client;
        }
    }
}
