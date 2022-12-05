using System;
using System.Threading.Tasks;
using LeanCloud.Realtime.Internal.Connection;

namespace LeanCloud.LiveQuery.Internal {
    /// <summary>
    /// LiveQuery heartbeat controller
    /// </summary>
    internal class LCLiveQueryHeartBeat : LCHeartBeat {
        private const int KEEP_ALIVE_INTERVAL = 180 * 1000;

        private readonly LCLiveQueryConnection connection;

        internal LCLiveQueryHeartBeat(LCLiveQueryConnection connection,
            Action onTimeout) :
            base(KEEP_ALIVE_INTERVAL, onTimeout) {
            this.connection = connection;
        }

        protected override async Task SendPing() {
            try {
                await connection.SendText("{}");
            } catch (Exception e) {
                LCLogger.Error(e);
            }
        }
    }
}
