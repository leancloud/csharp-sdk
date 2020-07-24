using System;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text;

namespace LeanCloud.Realtime.Internal.WebSocket {
    public class LCWebSocketClient {
        // .net standard 2.0 好像在拼合 Frame 时有 bug，所以将这个值调整大一些
        private const int RECV_BUFFER_SIZE = 1024 * 5;

        private const int CLOSE_TIMEOUT = 5000;

        private const int CONNECT_TIMEOUT = 10000;

        public Action<byte[]> OnMessage;

        public Action OnClose;

        private ClientWebSocket ws;

        public async Task Connect(string server,
            string subProtocol = null) {
            LCLogger.Debug($"Connecting WebSocket: {server}");
            Task timeoutTask = Task.Delay(CONNECT_TIMEOUT);
            ws = new ClientWebSocket();
            if (!string.IsNullOrEmpty(subProtocol)) {
                ws.Options.AddSubProtocol(subProtocol);
            }
            Task connectTask = ws.ConnectAsync(new Uri(server), default);
            if (await Task.WhenAny(connectTask, timeoutTask) == connectTask) {
                LCLogger.Debug($"Connected WebSocket: {server}");
                await connectTask;
                // 接收
                _ = StartReceive();
            } else {
                throw new TimeoutException("Connect timeout");
            }
        }

        public async Task Close() {
            LCLogger.Debug("Closing WebSocket");
            OnMessage = null;
            OnClose = null;
            try {
                // 发送关闭帧可能会很久，所以增加超时
                // 主动挥手关闭，不会再收到 Close Frame
                Task closeTask = ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, default);
                Task delayTask = Task.Delay(CLOSE_TIMEOUT);
                await Task.WhenAny(closeTask, delayTask);
            } catch (Exception e) {
                LCLogger.Error(e);
            } finally {
                ws.Abort();
                ws.Dispose();
                LCLogger.Debug("Closed WebSocket");
            }
        }

        public async Task Send(byte[] data,
            WebSocketMessageType messageType = WebSocketMessageType.Binary) {
            ArraySegment<byte> bytes = new ArraySegment<byte>(data);
            if (ws.State == WebSocketState.Open) {
                try {
                    await ws.SendAsync(bytes, messageType, true, default);
                } catch (Exception e) {
                    LCLogger.Error(e);
                    throw e;
                }
            } else {
                string message = $"Error Websocket state: {ws.State}";
                LCLogger.Error(message);
                throw new Exception(message);
            }
        }

        public async Task Send(string text) {
            await Send(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text);
        }

        private async Task StartReceive() {
            byte[] buffer = new byte[RECV_BUFFER_SIZE];
            try {
                while (ws.State == WebSocketState.Open) {
                    WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), default);
                    if (result.MessageType == WebSocketMessageType.Close) {
                        LCLogger.Debug($"Receive Closed: {result.CloseStatus}");
                        if (ws.State == WebSocketState.CloseReceived) {
                            // 如果是服务端主动关闭，则挥手关闭，并认为是断线
                            try {
                                Task closeTask = ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, default);
                                await Task.WhenAny(closeTask, Task.Delay(CLOSE_TIMEOUT));
                            } catch (Exception e) {
                                LCLogger.Error(e);
                            } finally {
                                HandleExceptionClose();
                            }
                        }
                    } else {
                        // 拼合 WebSocket Message
                        int length = result.Count;
                        byte[] data = new byte[length];
                        Array.Copy(buffer, data, length);
                        OnMessage?.Invoke(data);
                    }
                }
            } catch (Exception e) {
                // 客户端网络异常
                LCLogger.Error(e);
                HandleExceptionClose();
            }
        }

        private void HandleExceptionClose() {
            try {
                ws.Abort();
                ws.Dispose();
            } catch (Exception e) {
                LCLogger.Error(e);
            } finally {
                OnClose?.Invoke();
            }
        }
    }
}
