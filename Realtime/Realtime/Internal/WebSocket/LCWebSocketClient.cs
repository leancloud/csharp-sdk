using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;
using System.Collections.Generic;

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
        private const int SEND_TIMEOUT = 10000;

        public Action<byte[], int> OnMessage;

        public Action OnClose;

        private ClientWebSocket ws;

        private ConcurrentQueue<SendTask> sendQueue;

        public async Task Connect(string server,
            string subProtocol = null,
            Dictionary<string, string> headers = null,
            TimeSpan keepAliveInterval = default) {
            LCLogger.Debug($"Connecting WebSocket: {server}");
            
            ws = new ClientWebSocket();
            ws.Options.SetBuffer(RECV_BUFFER_SIZE, SEND_BUFFER_SIZE);
            if (!string.IsNullOrEmpty(subProtocol)) {
                ws.Options.AddSubProtocol(subProtocol);
            }
            if (headers != null) {
                foreach (KeyValuePair<string, string> kv in headers) {
                    ws.Options.SetRequestHeader(kv.Key, kv.Value);
                }
            }
            if (keepAliveInterval != default) {
                ws.Options.KeepAliveInterval = keepAliveInterval;
            }

            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(CONNECT_TIMEOUT))) {
                try {
                    await ws.ConnectAsync(new Uri(server), cts.Token);
                    if (ws.State != WebSocketState.Open) {
                        // .NET ClientWebSocket 可能在即使连接成功后，状态依然是 CLOSED，所以要先判断，否则会影响后续启动发送/接收监听
                        throw new Exception($"ClientWebSocket connected invalid state: {ws.State}");
                    }
                    LCLogger.Debug($"Connected WebSocket: {server}");
                    // 开启发送和接收
                    _ = StartSend();
                    _ = StartReceive();
                } catch (OperationCanceledException) {
                    throw new TimeoutException("Connect timeout");
                } 
            }
        }

        public async Task Close() {
            LCLogger.Debug("Closing WebSocket");

            OnMessage = null;
            OnClose = null;

            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(CLOSE_TIMEOUT))) {
                try {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cts.Token);
                } catch (Exception e) {
                    LCLogger.Error($"CLOSE EXCEPTION: {e}");
                } finally {
                    try {
                        ws.Abort();
                    } finally {

                    }
                    try {
                        ws.Dispose();
                    } finally {

                    }
                    LCLogger.Debug("Closed WebSocket");
                }
            }

            // 取消缓存的发送队列
            if (sendQueue != null) {
                while (sendQueue.Count > 0) {
                    if (sendQueue.TryDequeue(out SendTask sendTask)) {
                        sendTask.Tcs.TrySetCanceled();
                    }
                }
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
                            if (ws.State != WebSocketState.Open) {
                                // 增加额外判断，避免 .NET 内部异常 NPE
                                throw new Exception($"Error WebSocket state: {ws.State}");
                            }

                            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(SEND_TIMEOUT))) {
                                try {
                                    await ws.SendAsync(sendTask.Bytes, sendTask.MessageType, true, cts.Token);
                                    sendTask.Tcs.TrySetResult(null);
                                } catch (NullReferenceException e) {
                                    // .NET ClientWebSocket 内部错误，[issue](https://github.com/dotnet/runtime/issues/47582)，转化成更清楚的异常信息
                                    Exception ex = new Exception("ClientWebSocket inner exception", e);
                                    sendTask.Tcs.TrySetException(ex);
                                    throw ex;
                                } catch (OperationCanceledException) {
                                    TimeoutException te = new TimeoutException("Send timeout");
                                    sendTask.Tcs.TrySetException(te);
                                    throw te;
                                } catch (Exception e) {
                                    sendTask.Tcs.TrySetException(e);
                                    throw e;
                                }
                            }
                        }
                    }
                }
            } catch (Exception e) {
                LCLogger.Error($"SEND EXCEPTION: {e}");
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
                            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(CLOSE_TIMEOUT))) {
                                try {
                                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cts.Token);
                                } catch (Exception e) {
                                    LCLogger.Error(e);
                                } finally {
                                    HandleExceptionClose();
                                }
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
            OnClose?.Invoke();
        }
    }
}
