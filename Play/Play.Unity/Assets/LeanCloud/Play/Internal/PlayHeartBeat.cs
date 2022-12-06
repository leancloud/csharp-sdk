using System;
using System.Threading.Tasks;
using LeanCloud.Play.Protocol;
using LeanCloud.Realtime.Internal.Connection;

namespace LeanCloud.Play {
    public class PlayHeartBeat : LCHeartBeat {
        private readonly BaseConnection connection;

        internal PlayHeartBeat(BaseConnection connection, int keepAliveInterval, Action onTimeout) :
            base(keepAliveInterval, onTimeout) {
            this.connection = connection;
        }

        protected override async Task SendPing() {
            try {
                Command ping = new Command {
                    Cmd = CommandType.Echo,
                };
                await connection.SendCommand(CommandType.Echo, OpType.None, new Body());
            } catch (Exception e) {
                LCLogger.Error(e);
            }
        }
    }
}
