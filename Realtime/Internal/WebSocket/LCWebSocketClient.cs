using System;
using System.Threading.Tasks;
using System.Net.WebSockets;
using LeanCloud.Common;
using LeanCloud.Realtime.Internal.Router;
using LeanCloud.Realtime.Internal.Connection;

namespace LeanCloud.Realtime.Internal.WebSocket {
    /// <summary>
    /// WebSocket 客户端，只与通信协议相关
    /// </summary>
    internal class LCWebSocketClient {
        // .net standard 2.0 好像在拼合 Frame 时有 bug，所以将这个值调整大一些
        private const int RECV_BUFFER_SIZE = 1024 * 5;

        private const int CLOSE_TIMEOUT = 5000;

        private const int CONNECT_TIMEOUT = 10000;

        internal Action<byte[]> OnMessage;

        internal Action OnDisconnect;

        internal Action OnReconnect;

        private ClientWebSocket ws;

        private readonly LCRTMRouter router;

        private readonly LCHeartBeat heartBeat;

        internal LCWebSocketClient(LCRTMRouter router, LCHeartBeat heartBeat) {
            this.router = router;
            this.heartBeat = heartBeat;
        }

        internal async Task Connect() {
            try {
                LCRTMServer rtmServer = await router.GetServer();
                try {
                    LCLogger.Debug($"Primary Server");
                    await Connect(rtmServer.Primary);
                } catch (Exception e) {
                    LCLogger.Error(e);
                    LCLogger.Debug($"Secondary Server");
                    await Connect(rtmServer.Secondary);
                }
            } catch (Exception e) {
                throw e;
            }
            
            // 接收
            _ = StartReceive();
        }

        private async Task Connect(string server) {
            LCLogger.Debug($"Connecting WebSocket: {server}");
            Task timeoutTask = Task.Delay(CONNECT_TIMEOUT);
            ws = new ClientWebSocket();
            ws.Options.AddSubProtocol("lc.protobuf2.3");
            Task connectTask = ws.ConnectAsync(new Uri(server), default);
            if (await Task.WhenAny(connectTask, timeoutTask) == connectTask) {
                LCLogger.Debug($"Connected WebSocket: {server}");
            } else {
                throw new TimeoutException("Connect timeout");
            }
        }

        internal async Task Close() {
            LCLogger.Debug("Closing WebSocket");
            OnMessage = null;
            OnDisconnect = null;
            OnReconnect = null;
            heartBeat.Stop();
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

        private async Task StartReceive() {
            byte[] buffer = new byte[RECV_BUFFER_SIZE];
            try {
                while (ws.State == WebSocketState.Open) {
                    WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), default);
                    if (result.MessageType == WebSocketMessageType.Close) {
                        // 由服务端发起关闭
                        LCLogger.Debug($"Receive Closed: {result.CloseStatus}");
                        LCLogger.Debug($"ws state: {ws.State}");
                        // 这里有可能是客户端主动关闭，也有可能是服务端主动关闭
                        if (ws.State == WebSocketState.CloseReceived) {
                            // 如果是服务端主动关闭，则挥手关闭，并认为是断线
                            try {
                                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, default);
                            } catch (Exception e) {
                                LCLogger.Error(e);
                            } finally {
                                OnDisconnect?.Invoke();
                            }
                        }
                    } else if (result.MessageType == WebSocketMessageType.Binary) {
                        _ = heartBeat.Update(HandleClose);
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
                OnDisconnect?.Invoke();
            }
        }

        private void HandleClose() {
            try {
                heartBeat.Stop();
                ws.Abort();
                ws.Dispose();
            } catch (Exception e) {
                LCLogger.Error(e);
            }
        }
    }
}
