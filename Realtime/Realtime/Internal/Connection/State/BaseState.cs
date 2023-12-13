using System;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Realtime.Internal.Protocol;
using LeanCloud.Realtime.Internal.Router;
using LeanCloud.Realtime.Internal.WebSocket;

namespace LeanCloud.Realtime.Internal.Connection.State {
    public abstract class BaseState {
        private const string SUB_PROTOCOL = "lc.protobuf2.3";

        protected LCConnection connection;

        public BaseState(LCConnection connection) {
            this.connection = connection;
        }

        public virtual void Enter() { }

        public virtual void Exit() { }

        public virtual Task Connect() {
            throw new Exception($"Connect on invalid state: {GetType().Name}");
        }

        public virtual Task<GenericCommand> SendRequest(GenericCommand request) {
            throw new Exception($"SendRequest on invalid state: {GetType().Name}");
        }

        public virtual Task SendCommand(GenericCommand command) {
            throw new Exception($"SendCommand on invalid state: {GetType().Name}");
        }

        public virtual void Pause() {
            LCLogger.Warn($"Pause on invalid state: {GetType().Name}");
        }

        public virtual void Resume() {
            LCLogger.Warn($"Resume on invalid state: {GetType().Name}");
        }

        public virtual void Close() {
            LCLogger.Warn($"Close on invalid state: {GetType().Name}");
        }

        protected async Task ConnectInternal(CancellationToken cancellationToken) {
            try {
                LCRTMServer rtmServer = await connection.router.GetServer();
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                connection.ws = new LCWebSocketClient();
                try {
                    LCLogger.Debug($"Primary Server");
                    await connection.ws.Connect(rtmServer.Primary, SUB_PROTOCOL);

                    if (cancellationToken.IsCancellationRequested) {
                        _ = connection.ws.Close();
                        return;
                    }
                } catch (Exception e) {
                    if (cancellationToken.IsCancellationRequested) {
                        return;
                    }

                    LCLogger.Warn($"Connect {rtmServer.Primary} exception: {e}");
                    LCLogger.Debug($"Secondary Server");
                    await connection.ws.Connect(rtmServer.Secondary, SUB_PROTOCOL);

                    if (cancellationToken.IsCancellationRequested) {
                        _ = connection.ws.Close();
                        return;
                    }
                }
            } catch (Exception e) {
                throw e;
            }
        }
    }
}
