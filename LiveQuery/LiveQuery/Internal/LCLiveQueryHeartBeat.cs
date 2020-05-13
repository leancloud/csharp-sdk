using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LeanCloud.LiveQuery.Internal {
    /// <summary>
    /// LiveQuery 心跳控制器
    /// </summary>
    internal class LCLiveQueryHeartBeat {
        private const int PING_INTERVAL = 5000;
        private const int PONG_INTERVAL = 5000;

        private readonly LCLiveQueryConnection connection;

        private CancellationTokenSource pingCTS;
        private CancellationTokenSource pongCTS;

        internal LCLiveQueryHeartBeat(LCLiveQueryConnection connection) {
            this.connection = connection;
        }

        internal async Task Refresh(Action onTimeout) {
            LCLogger.Debug("LiveQuery HeartBeat refresh");
            Stop();

            pingCTS = new CancellationTokenSource();
            Task delayTask = Task.Delay(PING_INTERVAL, pingCTS.Token);
            await delayTask;
            if (delayTask.IsCanceled) {
                return;
            }
            // 发送 Ping 包
            LCLogger.Debug("Ping ~~~");
            _ = connection.SendText("{}");

            // 等待 Pong
            pongCTS = new CancellationTokenSource();
            Task timeoutTask = Task.Delay(PONG_INTERVAL, pongCTS.Token);
            await timeoutTask;
            if (timeoutTask.IsCanceled) {
                return;
            }

            // 超时
            LCLogger.Debug("Ping timeout");
            onTimeout?.Invoke();
        }

        internal void Stop() {
            pingCTS?.Cancel();
            pongCTS?.Cancel();
        }
    }
}
