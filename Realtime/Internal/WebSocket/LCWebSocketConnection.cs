using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.WebSockets;
using LeanCloud.Realtime.Protocol;
using LeanCloud.Storage;
using LeanCloud.Realtime.Internal.Router;
using LeanCloud.Common;
using Google.Protobuf;

namespace LeanCloud.Realtime.Internal.WebSocket {
    internal class LCWebSocketConnection {
        private const int KEEP_ALIVE_INTERVAL = 10;
        // .net standard 2.0 好像在拼合 Frame 时有 bug，所以将这个值调整大一些
        private const int RECV_BUFFER_SIZE = 1024 * 5;

        private ClientWebSocket ws;

        private volatile int requestI = 1;

        private readonly object requestILock = new object();

        private readonly Dictionary<int, TaskCompletionSource<GenericCommand>> responses;

        private readonly string id;

        internal LCRTMRouter Router {
            get; private set;
        }

        internal Func<GenericCommand, Task> OnNotification {
            get; set;
        }

        internal Action<int, string> OnClose {
            get; set;
        }

        internal LCWebSocketConnection(string id) {
            Router = new LCRTMRouter();

            this.id = id;
            responses = new Dictionary<int, TaskCompletionSource<GenericCommand>>();
        }

        internal async Task Connect() {
            string rtmServer = await Router.GetServer();

            ws = new ClientWebSocket();
            ws.Options.AddSubProtocol("lc.protobuf2.3");
            ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(KEEP_ALIVE_INTERVAL);
            await ws.ConnectAsync(new Uri(rtmServer), default);
            _ = StartReceive();
        }

        internal Task<GenericCommand> SendRequest(GenericCommand request) {
            TaskCompletionSource<GenericCommand> tcs = new TaskCompletionSource<GenericCommand>();
            request.I = RequestI;
            responses.Add(request.I, tcs);
            LCLogger.Debug($"{id} => {request.Cmd}/{request.Op}: {request.ToString()}");
            ArraySegment<byte> bytes = new ArraySegment<byte>(request.ToByteArray());
            try {
                ws.SendAsync(bytes, WebSocketMessageType.Binary, true, default);
            } catch (Exception) {
                // TODO 发送消息异常

            }
            return tcs.Task;
        }

        internal async Task Close() {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "1", default);
        }

        private async Task StartReceive() {
            byte[] buffer = new byte[RECV_BUFFER_SIZE];
            try {
                while (ws.State == WebSocketState.Open) {
                    byte[] data = new byte[0];
                    WebSocketReceiveResult result;
                    do {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), default);
                        if (result.MessageType == WebSocketMessageType.Close) {
                            OnClose?.Invoke(-1, null);
                            return;
                        }
                        // 拼合 WebSocket Frame
                        byte[] oldData = data;
                        data = new byte[oldData.Length + result.Count];
                        Array.Copy(oldData, data, oldData.Length);
                        Array.Copy(buffer, 0, data, oldData.Length, result.Count);
                    } while (!result.EndOfMessage);
                    try {
                        GenericCommand command = GenericCommand.Parser.ParseFrom(data);
                        LCLogger.Debug($"{id} <= {command.Cmd}/{command.Op}: {command.ToString()}");
                        _ = HandleCommand(command);
                    } catch (Exception e) {
                        // 解析消息错误
                        LCLogger.Error(e.Message);
                    }
                }
            } catch (Exception e) {
                // 连接断开
                LCLogger.Error(e.Message);
                await ws.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "read error", default);
            }
        }

        private async Task HandleCommand(GenericCommand command) {
            try {
                if (command.HasI) {
                    // 应答
                    if (responses.TryGetValue(command.I, out TaskCompletionSource<GenericCommand> tcs)) {
                        if (command.HasErrorMessage) {
                            // 错误
                            ErrorCommand error = command.ErrorMessage;
                            int code = error.Code;
                            string detail = error.Detail;
                            // 包装成异常抛出
                            LCException exception = new LCException(code, detail);
                            tcs.SetException(exception);
                        } else {
                            tcs.SetResult(command);
                        }
                    }
                } else {
                    // 通知
                    await OnNotification?.Invoke(command);
                }
            } catch (Exception e) {
                LCLogger.Error(e.Message);
            }
        }

        private int RequestI {
            get {
                lock (requestILock) {
                    return requestI++;
                };
            }
        }
    }
}
