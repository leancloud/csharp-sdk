using System;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Common;
using LeanCloud.Realtime.Internal.Protocol;

namespace LeanCloud.Realtime.Internal.Connection {
    /// <summary>
    /// Heartbeat controller is needed because .Net Standard 2.0 does not support sending ping frame.
    /// 1. Ping every 180 seconds.
    /// 2. Receiving a pong packet will refresh the pong interval.
    /// 3. Check pong interval every 180 seconds.
    ///    If the interval is greater than 360 seconds, the connection is considered disconnected.
    /// </summary>
    public class LCHeartBeat {
        private const int PING_INTERVAL = 180 * 1000;

        private readonly LCConnection connection;

        private readonly Action onTimeout;

        private CancellationTokenSource heartBeatCTS;

        private bool running = false;

        private DateTimeOffset lastPongTime;

        public LCHeartBeat(Action onTimeout) {
            this.onTimeout = onTimeout;
        }

        internal LCHeartBeat(LCConnection connection,
            Action onTimeout) : this(onTimeout) {
            this.connection = connection;
        }

        public void Start() {
            running = true;
            heartBeatCTS = new CancellationTokenSource();
            StartPing();
            StartPong();
        }

        private async void StartPing() {
            while (running) {
                try {
                    await Task.Delay(PING_INTERVAL, heartBeatCTS.Token);
                } catch (TaskCanceledException) {
                    return;
                }
                LCLogger.Debug("Ping ~~~");
                await SendPing();
            }
        }

        protected virtual async Task SendPing() {
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

        private async void StartPong() {
            lastPongTime = DateTimeOffset.Now;
            while (running) {
                try {
                    await Task.Delay(PING_INTERVAL / 2, heartBeatCTS.Token);
                } catch (TaskCanceledException) {
                    return;
                }
                // 检查 pong 包时间
                TimeSpan interval = DateTimeOffset.Now - lastPongTime;
                if (interval.TotalMilliseconds > PING_INTERVAL * 2) {
                    // 断线
                    Stop();
                    onTimeout.Invoke();
                }
            }
        }

        public void Pong() {
            LCLogger.Debug("Pong ~~~");
            // 刷新最近 pong 时间戳
            lastPongTime = DateTimeOffset.Now;
        }

        public void Stop() {
            running = false;
            heartBeatCTS.Cancel();
        }
    }
}
