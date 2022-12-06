using System;
using System.Threading.Tasks;
using LeanCloud.Common;
using LeanCloud.Realtime.Internal.Protocol;

namespace LeanCloud.Realtime.Internal.Connection {
    public class LCRTMHeartBeat : LCHeartBeat {
        private const int KEEP_ALIVE_INTERVAL = 180 * 1000;

        private readonly LCConnection connection;

        public LCRTMHeartBeat(LCConnection connection, Action onTimeout) :
            base(KEEP_ALIVE_INTERVAL, onTimeout) {
            this.connection = connection;
        }

        protected override async Task SendPing() {
            // 发送 ping 包
            GenericCommand command = new GenericCommand {
                Cmd = CommandType.Echo,
                AppId = LCCore.AppId,
                PeerId = connection.id
            };
            try {
                await connection.SendCommand(command);
            } catch (Exception e) {
                LCLogger.Error(e);
            }
        }
    }
}
