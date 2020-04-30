using System;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace LeanCloud.Realtime.Internal.WebSocket {
    /// <summary>
    /// WebSocket 客户端，负责底层连接和事件，只与通信协议相关
    /// </summary>
    internal class LCWebSocketClient {
        // .net standard 2.0 好像在拼合 Frame 时有 bug，所以将这个值调整大一些
        private const int RECV_BUFFER_SIZE = 1024 * 5;

        /// <summary>
        /// 关闭超时
        /// </summary>
        private const int CLOSE_TIMEOUT = 5000;

        /// <summary>
        /// 连接超时
        /// </summary>
        private const int CONNECT_TIMEOUT = 10000;

        /// <summary>
        /// 消息事件
        /// </summary>
        internal Action<byte[]> OnMessage;

        /// <summary>
        /// 连接关闭
        /// </summary>
        internal Action OnClose;

        private ClientWebSocket ws;

        /// <summary>
        /// 连接指定 ws 服务器
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        internal async Task Connect(string server) {
            LCLogger.Debug($"Connecting WebSocket: {server}");
            Task timeoutTask = Task.Delay(CONNECT_TIMEOUT);
            ws = new ClientWebSocket();
            ws.Options.AddSubProtocol("lc.protobuf2.3");
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

        /// <summary>
        /// 主动关闭连接
        /// </summary>
        /// <returns></returns>
        internal async Task Close() {
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

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal async Task Send(byte[] data) {
            ArraySegment<byte> bytes = new ArraySegment<byte>(data);
            if (ws.State == WebSocketState.Open) {
                try {
                    await ws.SendAsync(bytes, WebSocketMessageType.Binary, true, default);
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

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <returns></returns>
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
                    } else if (result.MessageType == WebSocketMessageType.Binary) {
                        // 拼合 WebSocket Message
                        int length = result.Count;
                        byte[] data = new byte[length];
                        Array.Copy(buffer, data, length);
                        OnMessage?.Invoke(data);
                    } else {
                        LCLogger.Error($"Error message type: {result.MessageType}");
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
