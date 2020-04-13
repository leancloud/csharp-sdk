using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;
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

        private const int MAX_RECONNECT_TIMES = 10;

        internal Action<GenericCommand> OnNotification;

        internal Action OnDisconnect;

        internal Action OnReconnecting;

        internal Action OnReconnected;

        private LCHeartBeat heartBeat;

        internal string id;

        private readonly Dictionary<int, TaskCompletionSource<GenericCommand>> responses;

        private int requestI = 1;

        private LCWebSocketClient client;

        internal LCConnection(string id) {
            this.id = id;
            responses = new Dictionary<int, TaskCompletionSource<GenericCommand>>();
            heartBeat = new LCHeartBeat(this, 10000, 10000, () => {

            });
            client = new LCWebSocketClient {
                OnMessage = OnMessage,
                OnDisconnect = OnClientDisconnect
            };
        }

        internal async Task Connect() {
            await client.Connect();
        }

        internal async Task<GenericCommand> SendRequest(GenericCommand request) {
            TaskCompletionSource<GenericCommand> tcs = new TaskCompletionSource<GenericCommand>();
            request.I = requestI++;
            responses.Add(request.I, tcs);
            LCLogger.Debug($"{id} => {FormatCommand(request)}");
            byte[] bytes = request.ToByteArray();
            Task sendTask = client.Send(bytes);
            Task timeoutTask = Task.Delay(SEND_TIMEOUT);
            try {
                Task doneTask = await Task.WhenAny(sendTask, timeoutTask);
                if (timeoutTask == doneTask) {
                    tcs.TrySetException(new TimeoutException("Send request"));
                }
            } catch (Exception e) {
                tcs.TrySetException(e);
            }
            return await tcs.Task;
        }

        internal async Task Close() {
            OnNotification = null;
            OnDisconnect = null;
            heartBeat.Stop();
            await client.Close();
        }

        private void OnMessage(byte[] bytes) {
            _ = heartBeat.Update();
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
                LCLogger.Error(e.Message);
            }
        }

        private void OnClientDisconnect() {
            OnDisconnect?.Invoke();
            OnReconnecting?.Invoke();
            // TODO 重连
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
                        break;
                    } catch (Exception e) {
                        reconnectCount++;
                        LCLogger.Error(e.Message);
                        int delay = 10;
                        LCLogger.Debug($"Reconnect after {delay}s");
                        await Task.Delay(1000 * delay);
                    }
                }
                if (reconnectCount < MAX_RECONNECT_TIMES) {
                    // 重连成功
                    LCLogger.Debug("Reconnected");
                    OnReconnected?.Invoke();
                    break;
                } else {
                    // TODO 重置连接
                    client = new LCWebSocketClient();
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
