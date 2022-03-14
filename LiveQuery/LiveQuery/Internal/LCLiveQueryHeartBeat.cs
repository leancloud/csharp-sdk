using System;
using System.Threading.Tasks;
using LeanCloud.Realtime.Internal.Connection;

namespace LeanCloud.LiveQuery.Internal {
    /// <summary>
    /// LiveQuery heartbeat controller
    /// </summary>
    internal class LCLiveQueryHeartBeat : LCHeartBeat {
        private readonly LCLiveQueryConnection connection;

        internal LCLiveQueryHeartBeat(LCLiveQueryConnection connection,
            Action onTimeout) : base(onTimeout) {
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
