using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;
using LeanCloud.Realtime.Internal.Router;
using LeanCloud.Realtime.Internal.WebSocket;
using LeanCloud.Realtime.Protocol;
using LeanCloud.Common;
using LeanCloud.Storage;

namespace LeanCloud.Realtime.Internal.Connection {
    /// <summary>
    /// 连接层，只与数据协议相关
    /// </summary>
    internal class LCConnection {
        private const int SEND_TIMEOUT = 10000;

        private const int MAX_RECONNECT_TIMES = 3;

        private const int RECONNECT_INTERVAL = 5000;

        private const int HEART_BEAT_INTERVAL = 5000;

        internal Action<GenericCommand> OnNotification;

        internal Action OnDisconnect;

        internal Action OnReconnecting;

        internal Action OnReconnected;

        internal string id;

        private readonly Dictionary<int, TaskCompletionSource<GenericCommand>> responses;

        private int requestI = 1;

        private LCRTMRouter router;

        private LCHeartBeat heartBeat;

        private LCWebSocketClient client;

        internal LCConnection(string id) {
            this.id = id;
            responses = new Dictionary<int, TaskCompletionSource<GenericCommand>>();
            heartBeat = new LCHeartBeat(this, HEART_BEAT_INTERVAL, HEART_BEAT_INTERVAL);
            router = new LCRTMRouter();
            client = new LCWebSocketClient(router, heartBeat) {
                OnMessage = OnClientMessage,
                OnDisconnect = OnClientDisconnect
            };
        }

        internal async Task Connect() {
            await client.Connect();
        }

        internal async Task Reset() {
            router.Reset();
            await client.Close();
            heartBeat = new LCHeartBeat(this, HEART_BEAT_INTERVAL, HEART_BEAT_INTERVAL);
            router = new LCRTMRouter();
            client = new LCWebSocketClient(router, heartBeat) {
                OnMessage = OnClientMessage,
                OnDisconnect = OnClientDisconnect
            };
            await Reconnect();
        }

        internal async Task<GenericCommand> SendRequest(GenericCommand request) {
            TaskCompletionSource<GenericCommand> tcs = new TaskCompletionSource<GenericCommand>();
            request.I = requestI++;
            responses.Add(request.I, tcs);
            LCLogger.Debug($"{id} => {FormatCommand(request)}");
            byte[] bytes = request.ToByteArray();
            Task sendTask = client.Send(bytes);
            if (await Task.WhenAny(sendTask, Task.Delay(SEND_TIMEOUT)) == sendTask) {
                try {
                    await sendTask;
                } catch (Exception e) {
                    tcs.TrySetException(e);
                }
            } else {
                tcs.TrySetException(new TimeoutException("Send request"));
            }
            return await tcs.Task;
        }

        internal async Task Close() {
            OnNotification = null;
            OnDisconnect = null;
            heartBeat.Stop();
            await client.Close();
        }

        private void OnClientMessage(byte[] bytes) {
            try {
                GenericCommand command = GenericCommand.Parser.ParseFrom(bytes);
                LCLogger.Debug($"{id} <= {FormatCommand(command)}");
                if (command.HasI) {
                    // 应答
                    int requestIndex = command.I;
                    if (responses.TryGetValue(requestIndex, out TaskCompletionSource<GenericCommand> tcs)) {
                        if (command.HasErrorMessage) {
                            // 错误
                            ErrorCommand error = command.ErrorMessage;
                            int code = error.Code;
                            string detail = error.Detail;
                            // 包装成异常抛出
                            LCException exception = new LCException(code, detail);
                            tcs.TrySetException(exception);
                        } else {
                            tcs.TrySetResult(command);
                        }
                        responses.Remove(requestIndex);
                    } else {
                        LCLogger.Error($"No request for {requestIndex}");
                    }
                } else {
                    // 通知
                    OnNotification?.Invoke(command);
                }
            } catch (Exception e) {
                LCLogger.Error(e);
            }
        }

        private void OnClientDisconnect() {
            OnDisconnect?.Invoke();
            OnReconnecting?.Invoke();
            // 重连
            _ = Reconnect();
        }

        private async Task Reconnect() {
            while (true) {
                int reconnectCount = 0;
                // 重连策略
                while (reconnectCount < MAX_RECONNECT_TIMES) {
                    try {
                        LCLogger.Debug($"Reconnecting... {reconnectCount}");
                        await client.Connect();
                        client.OnMessage = OnClientMessage;
                        client.OnDisconnect = OnClientDisconnect;
                        break;
                    } catch (Exception e) {
                        reconnectCount++;
                        LCLogger.Error(e);
                        LCLogger.Debug($"Reconnect after {RECONNECT_INTERVAL}ms");
                        await Task.Delay(RECONNECT_INTERVAL);
                    }
                }
                if (reconnectCount < MAX_RECONNECT_TIMES) {
                    // 重连成功
                    LCLogger.Debug("Reconnected");
                    OnReconnected?.Invoke();
                    break;
                } else {
                    // 重置 Router，继续尝试重连
                    router.Reset();
                }
            }
        }

        private static string FormatCommand(GenericCommand command) {
            StringBuilder sb = new StringBuilder($"{command.Cmd}");
            if (command.HasOp) {
                sb.Append($"/{command.Op}");
            }
            sb.Append($"\n{command}");
            return sb.ToString();
        }
    }
}
