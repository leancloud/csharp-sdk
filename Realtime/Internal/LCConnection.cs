using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.WebSockets;
using Google.Protobuf;
using LeanCloud.Realtime.Protocol;
using LeanCloud.Storage;

namespace LeanCloud.Realtime.Internal {
    internal class LCConnection {
        private const int KEEP_ALIVE_INTERVAL = 10;
        private const int RECV_BUFFER_SIZE = 1024;

        private ClientWebSocket ws;

        private volatile int requestI = 1;

        private readonly object requestILock = new object();

        private readonly Dictionary<int, TaskCompletionSource<GenericCommand>> responses;

        internal LCConnection() {
            responses = new Dictionary<int, TaskCompletionSource<GenericCommand>>();
        }

        internal async Task Connect() {
            ws = new ClientWebSocket();
            ws.Options.AddSubProtocol("lc.protobuf2.3");
            ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(KEEP_ALIVE_INTERVAL);
            await ws.ConnectAsync(new Uri(""), default);
        }

        internal async Task SendRequest(GenericCommand request) {
            request.I = RequestI;
            ArraySegment<byte> bytes = new ArraySegment<byte>(request.ToByteArray());
            try {
                await ws.SendAsync(bytes, WebSocketMessageType.Binary, true, default);
            } catch (Exception e) {
                // TODO 发送消息异常

            }
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
                            // TODO 区分主动断开和被动断开

                            return;
                        }
                        // 拼合 WebSocket Frame
                        byte[] oldData = data;
                        data = new byte[data.Length + result.Count];
                        Array.Copy(oldData, data, oldData.Length);
                        Array.Copy(buffer, 0, data, oldData.Length, result.Count);
                    } while (!result.EndOfMessage);
                    try {
                        GenericCommand command = GenericCommand.Parser.ParseFrom(data);
                        HandleCommand(command);
                    } catch (Exception e) {
                        // 解析消息错误

                    }
                }
            } catch (Exception e) {
                // TODO 连接断开

            }
        }

        private void HandleCommand(GenericCommand command) {
            if (command.HasI) {
                // 应答
                if (responses.TryGetValue(command.I, out TaskCompletionSource<GenericCommand> tcs)) {
                    if (command.HasErrorMessage) {
                        // 错误
                        ErrorCommand error = command.ErrorMessage;
                        int code = error.Code;
                        string detail = error.Detail;
                        // TODO 包装成异常抛出
                        LCException exception = new LCException(code, detail);
                        tcs.SetException(exception);
                    } else {
                        tcs.SetResult(command);
                    }
                }
            } else {
                // 通知

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
