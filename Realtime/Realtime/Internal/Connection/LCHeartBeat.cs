using System;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Realtime.Internal.Protocol;

namespace LeanCloud.Realtime.Internal.Connection {
    /// <summary>
    /// 心跳控制器，由于 .Net Standard 2.0 不支持发送 ping frame，所以需要发送逻辑心跳
    /// 1. 每隔 180s 发送 ping 包
    /// 2. 接收到 pong 包刷新上次 pong 时间
    /// 3. 每隔 180s 检测 pong 包间隔，超过 360s 则认为断开
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

        /// <summary>
        /// 启动心跳
        /// </summary>
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
                SendPing();
            }
        }

        protected virtual void SendPing() {
            // 发送 ping 包
            GenericCommand command = new GenericCommand {
                Cmd = CommandType.Echo,
                AppId = LCApplication.AppId,
                PeerId = connection.id
            };
            try {
                _ = connection.SendCommand(command);
            } catch (Exception e) {
                LCLogger.Error(e.Message);
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

        /// <summary>
        /// 停止心跳监听
        /// </summary>
        public void Stop() {
            running = false;
            heartBeatCTS.Cancel();
        }
    }
}
