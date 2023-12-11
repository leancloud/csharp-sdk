using System;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Common;
using LeanCloud.Realtime.Internal.Protocol;

namespace LeanCloud.Realtime.Internal.Connection {
    public class LCRTMHeartBeat {
        private const int KEEP_ALIVE_INTERVAL = 180 * 1000;

        private readonly LCConnection connection;

        private Action onTimeout;

        private CancellationTokenSource heartBeatCTS;

        private bool running = false;

        private DateTimeOffset lastPongTime;

        public LCRTMHeartBeat(LCConnection connection) {
            this.connection = connection;
        }

        public void Start(Action onTimeout) {
            this.onTimeout = onTimeout;

            running = true;
            heartBeatCTS = new CancellationTokenSource();

            StartPing();
            StartPong();
        }

        private async void StartPing() {
            while (running) {
                try {
                    await Task.Delay(KEEP_ALIVE_INTERVAL, heartBeatCTS.Token);
                } catch (TaskCanceledException) {
                    return;
                }

                if (running) {
                    LCLogger.Debug("Ping ~~~");
                    await SendPing();
                }
            }
        }

        private async void StartPong() {
            lastPongTime = DateTimeOffset.Now;
            while (running) {
                try {
                    await Task.Delay(KEEP_ALIVE_INTERVAL / 2, heartBeatCTS.Token);
                } catch (TaskCanceledException) {
                    return;
                }

                if (running) {
                    // 检查 pong 包时间
                    TimeSpan interval = DateTimeOffset.Now - lastPongTime;
                    if (interval.TotalMilliseconds > KEEP_ALIVE_INTERVAL * 2) {
                        // 断线
                        Stop();
                        onTimeout?.Invoke();
                    }
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
            onTimeout = null;
            heartBeatCTS.Cancel();
        }

        protected async Task SendPing() {
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
