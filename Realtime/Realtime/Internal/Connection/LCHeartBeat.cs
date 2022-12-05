using System;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal.Connection {
    /// <summary>
    /// Heartbeat controller is needed because .Net Standard 2.0 does not support sending ping frame.
    /// 1. Ping every 180 seconds.
    /// 2. Receiving a pong packet will refresh the pong interval.
    /// 3. Check pong interval every 180 seconds.
    ///    If the interval is greater than 360 seconds, the connection is considered disconnected.
    /// </summary>
    public abstract class LCHeartBeat {
        private readonly Action onTimeout;

        private CancellationTokenSource heartBeatCTS;

        private bool running = false;

        private DateTimeOffset lastPongTime;

        private readonly int keepAliveInterval;

        protected LCHeartBeat(int keepAliveInterval, Action onTimeout) {
            this.keepAliveInterval = keepAliveInterval;
            this.onTimeout = onTimeout;
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
                    await Task.Delay(keepAliveInterval, heartBeatCTS.Token);
                } catch (TaskCanceledException) {
                    return;
                }
                LCLogger.Debug("Ping ~~~");
                await SendPing();
            }
        }

        private async void StartPong() {
            lastPongTime = DateTimeOffset.Now;
            while (running) {
                try {
                    await Task.Delay(keepAliveInterval / 2, heartBeatCTS.Token);
                } catch (TaskCanceledException) {
                    return;
                }
                // 检查 pong 包时间
                TimeSpan interval = DateTimeOffset.Now - lastPongTime;
                if (interval.TotalMilliseconds > keepAliveInterval * 2) {
                    // 断线
                    Stop();
                    onTimeout.Invoke();
                }
            }
        }

        protected abstract Task SendPing();

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
