using System;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;

namespace LeanCloud.Realtime.Internal.WebSocket {
    public class LCWebSocketClient {
        class SendTask {
            internal TaskCompletionSource<object> Tcs { get; set; }
            internal ArraySegment<byte> Bytes { get; set; }
            internal WebSocketMessageType MessageType { get; set; }
        }

        // .net standard 2.0 好像在拼合 Frame 时有 bug，所以将这个值调整大一些
        private const int SEND_BUFFER_SIZE = 1024 * 5;
        private const int RECV_BUFFER_SIZE = 1024 * 8;
        private const int MSG_BUFFER_SIZE = 1024 * 10;

        private const int CLOSE_TIMEOUT = 5000;

        private const int CONNECT_TIMEOUT = 10000;

        public Action<byte[], int> OnMessage;

        public Action OnClose;

        private ClientWebSocket ws;

        private ConcurrentQueue<SendTask> sendQueue;

        public async Task Connect(string server,
            string subProtocol = null) {
            LCLogger.Debug($"Connecting WebSocket: {server}");
            Task timeoutTask = Task.Delay(CONNECT_TIMEOUT);
            ws = new ClientWebSocket();
            ws.Options.SetBuffer(RECV_BUFFER_SIZE, SEND_BUFFER_SIZE);
            if (!string.IsNullOrEmpty(subProtocol)) {
                ws.Options.AddSubProtocol(subProtocol);
            }
            Task connectTask = ws.ConnectAsync(new Uri(server), default);
            if (await Task.WhenAny(connectTask, timeoutTask) == connectTask) {
                LCLogger.Debug($"Connected WebSocket: {server}");
                await connectTask;
                // 接收
                _ = StartSend();
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

        public Task Send(byte[] data,
            WebSocketMessageType messageType = WebSocketMessageType.Binary) {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            sendQueue.Enqueue(new SendTask {
                Tcs = tcs,
                Bytes = new ArraySegment<byte>(data),
                MessageType = messageType
            });

            return tcs.Task;
        }

        public async Task Send(string text) {
            await Send(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text);
        }

        private async Task StartSend() {
            sendQueue = new ConcurrentQueue<SendTask>();
            try {
                while (ws.State == WebSocketState.Open) {
                    if (sendQueue.Count == 0) {
                        await Task.Delay(10);
                    }
                    while (sendQueue.Count > 0) {
                        if (sendQueue.TryDequeue(out SendTask sendTask)) {
                            try {
                                await ws.SendAsync(sendTask.Bytes, sendTask.MessageType, true, default);
                                sendTask.Tcs.TrySetResult(null);
                            } catch (Exception e) {
                                LCLogger.Error(e);
                                sendTask.Tcs.TrySetException(e);
                                throw e;
                            }
                        }
                    }
                }
            } catch (Exception e) {
                LCLogger.Error(e);
                HandleExceptionClose();
            }
        }

        private async Task StartReceive() {
            byte[] recvBuffer = new byte[RECV_BUFFER_SIZE];
            byte[] msgBuffer = new byte[MSG_BUFFER_SIZE];
            int offset = 0;
            try {
                while (ws.State == WebSocketState.Open) {
                    WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(recvBuffer), default);
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
                        if (offset + length > msgBuffer.Length) {
                            // 反序列化数组大小不够，则以 2x 扩充
                            byte[] newBuffer = new byte[msgBuffer.Length * 2];
                            Array.Copy(msgBuffer, newBuffer, msgBuffer.Length);
                            msgBuffer = newBuffer;
                        }
                        Array.Copy(recvBuffer, 0, msgBuffer, offset, length);
                        offset += length;
                        if (result.EndOfMessage) {
                            OnMessage?.Invoke(msgBuffer, offset);
                            offset = 0;
                        }
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
