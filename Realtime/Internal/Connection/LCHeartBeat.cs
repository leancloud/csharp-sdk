using System;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Common;
using LeanCloud.Realtime.Internal.Protocol;

namespace LeanCloud.Realtime.Internal.Connection {
    /// <summary>
    /// 心跳控制器，由于 .Net Standard 2.0 不支持发送 ping frame，所以需要发送逻辑心跳
    /// 1. 每次接收到消息后开始监听，如果在 pingInterval 时间内没有再次接收到消息，则发送 ping 请求；
    /// 2. 发送后等待 pongInterval 时间，如果在此时间内接收到了任何消息，则取消并重新开始监听 1；
    /// 3. 如果没收到消息，则认为超时并回调，连接层接收回调后放弃当前连接，以断线逻辑处理
    /// </summary>
    internal class LCHeartBeat {
        private readonly LCConnection connection;

        /// <summary>
        /// ping 间隔
        /// </summary>
        private readonly int pingInterval;

        /// <summary>
        /// pong 间隔
        /// </summary>
        private readonly int pongInterval;

        private CancellationTokenSource pingCTS;
        private CancellationTokenSource pongCTS;

        internal LCHeartBeat(LCConnection connection,
            int pingInterval,
            int pongInterval) {
            this.connection = connection;
            this.pingInterval = pingInterval;
            this.pongInterval = pongInterval;
        }

        /// <summary>
        /// 更新心跳监听
        /// </summary>
        /// <returns></returns>
        internal async Task Update(Action onTimeout) {
            LCLogger.Debug("HeartBeat update");
            pingCTS?.Cancel();
            pongCTS?.Cancel();

            // 计时准备 ping
            pingCTS = new CancellationTokenSource();
            Task delayTask = Task.Delay(pingInterval, pingCTS.Token);
            await delayTask;
            if (delayTask.IsCanceled) {
                return;
            }
            // 发送 ping 包
            LCLogger.Debug("Ping ~~~");
            GenericCommand command = new GenericCommand {
                Cmd = CommandType.Echo,
                AppId = LCApplication.AppId,
                PeerId = connection.id
            };
            _ = connection.SendRequest(command);
            pongCTS = new CancellationTokenSource();
            Task timeoutTask = Task.Delay(pongInterval, pongCTS.Token);
            await timeoutTask;
            if (timeoutTask.IsCanceled) {
                return;
            }
            // timeout
            LCLogger.Error("Ping timeout");
            onTimeout.Invoke();
        }

        /// <summary>
        /// 停止心跳监听
        /// </summary>
        internal void Stop() {
            pingCTS?.Cancel();
            pongCTS?.Cancel();
        }
    }
}
