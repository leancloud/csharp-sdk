﻿using System;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Realtime.Internal.Router;

namespace LeanCloud.Realtime.Internal.Connection.State {
    public class ReconnectState : BaseState {
        private const int MAX_RECONNECT_TIMES = 10;

        private const int RECONNECT_INTERVAL = 10000;

        private readonly CancellationTokenSource cancellationTokenSource;

        public ReconnectState(LCConnection connection) : base(connection) {
            cancellationTokenSource = new CancellationTokenSource();
        }

        #region State Event

        public override async void Enter() {
            // 处理取消
            while (!cancellationTokenSource.Token.IsCancellationRequested) {
                int reconnectCount = 0;
                // 重连策略
                while (reconnectCount < MAX_RECONNECT_TIMES) {
                    if (cancellationTokenSource.Token.IsCancellationRequested) {
                        break;
                    }

                    try {
                        LCLogger.Debug($"Reconnecting... {reconnectCount}");
                        await ConnectInternal(cancellationTokenSource.Token);
                        break;
                    } catch (Exception e) {
                        reconnectCount++;
                        LCLogger.Warn($"Connect exception: {e}");
                        LCLogger.Debug($"Reconnect after {RECONNECT_INTERVAL}ms");
                        await Task.Delay(RECONNECT_INTERVAL);
                    }
                }

                // 如果取消
                if (cancellationTokenSource.Token.IsCancellationRequested) {
                    break;
                }

                if (reconnectCount < MAX_RECONNECT_TIMES) {
                    // 重连成功
                    LCLogger.Debug("Reconnected");
                    connection.TranslateTo(LCConnection.State.Connected);
                    connection.HandleReconnected();
                    break;
                } else {
                    // 重置 Router，继续尝试重连
                    connection.router = new LCRTMRouter();
                }
            }
        }

        public override void Pause() {
            connection.TranslateTo(LCConnection.State.Paused);

            cancellationTokenSource.Cancel();
        }

        public override void Close() {
            connection.TranslateTo(LCConnection.State.Init);

            cancellationTokenSource.Cancel();
        }

        #endregion

    }
}
