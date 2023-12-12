using System;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Realtime.Internal.Router;

namespace LeanCloud.Realtime.Internal.Connection.State {
    public class ReconnectState : BaseState {
        private const int MAX_RECONNECT_TIMES = 10;

        private const int RECONNECT_INTERVAL = 10000;

        private CancellationTokenSource cts;

        public ReconnectState(LCConnection connection) : base(connection) {

        }

        #region State Event

        public override async void Enter() {
            // 处理取消
            cts = new CancellationTokenSource();
            while (!cts.IsCancellationRequested) {
                int reconnectCount = 0;
                // 重连策略
                while (reconnectCount < MAX_RECONNECT_TIMES) {
                    if (cts.IsCancellationRequested) {
                        break;
                    }
                    try {
                        LCLogger.Debug($"Reconnecting... {reconnectCount}");
                        await ConnectInternal();
                        break;
                    } catch (Exception e) {
                        reconnectCount++;
                        LCLogger.Warn($"Connect exception: {e}");
                        LCLogger.Debug($"Reconnect after {RECONNECT_INTERVAL}ms");
                        await Task.Delay(RECONNECT_INTERVAL);
                    }
                }

                // 如果取消
                if (cts.IsCancellationRequested) {
                    if (reconnectCount < MAX_RECONNECT_TIMES) {
                        // 如果重试次数小于最大重试次数，说明连接成功了，则需要关闭 WebSocket
                        _ = connection.ws.Close();
                    }
                    break;
                }

                if (reconnectCount < MAX_RECONNECT_TIMES) {
                    // 重连成功
                    LCLogger.Debug("Reconnected");
                    connection.TranslateTo(connection.connectedState);
                    connection.HandleReconnected();
                    break;
                } else {
                    // 重置 Router，继续尝试重连
                    connection.router = new LCRTMRouter();
                }
            }
        }

        public override void Pause() {
            connection.TranslateTo(connection.pausedState);

            cts.Cancel();
        }

        #endregion

    }
}
