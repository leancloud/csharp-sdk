using System;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Realtime.Internal.Connection;
using System.Collections.Generic;

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

        protected override void SendPing() {
            try {
                _ = connection.SendText("{}");
            } catch (Exception) {

            }
        }
    }
}
