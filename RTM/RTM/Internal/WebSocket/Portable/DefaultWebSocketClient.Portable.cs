using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace LeanCloud.Realtime.Internal {
    /// <summary>
    /// LeanCloud Realtime SDK for .NET Portable 内置默认的 WebSocketClient
    /// </summary>
    public class DefaultWebSocketClient {
        const int RECV_BUFFER_SIZE = 1024;

        ClientWebSocket client;

        public event Action<int, string> OnClose;
        public event Action OnOpened;
        public event Action<string> OnMessage;

        public bool IsOpen {
            get {
                return client != null && client.State == WebSocketState.Open;
            }
        }

        public async Task Close() {
            if (IsOpen) {
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "1", CancellationToken.None);
            }
        }

        public void Disconnect() {
            OnClose?.Invoke(0, string.Empty);
            _ = Close();
        }

        public async Task Send(string message) {
            if (!IsOpen) {
                throw new Exception("WebSocket is not open when send data.");
            }
            ArraySegment<byte> bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            try {
                await client.SendAsync(bytes, WebSocketMessageType.Text, true, default);
            } catch (InvalidOperationException e) {
                OnClose?.Invoke(-2, e.Message);
                _ = Close();
                throw e;
            }
        }

        public async Task Connect(string url, string protocol = null) {
            client = new ClientWebSocket();
            client.Options.AddSubProtocol(protocol);
            client.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            try {
                await client.ConnectAsync(new Uri(url), default);
                // 开始接收
                _ = StartReceive();
            } catch (Exception e) {

                throw e;
            }
        }

        async Task StartReceive() {
            byte[] buffer = new byte[RECV_BUFFER_SIZE];
            try {
                while (client.State == WebSocketState.Open) {
                    byte[] data = new byte[0];
                    WebSocketReceiveResult result;
                    do {
                        result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Close) {
                            // 断开事件
                            AVRealtime.PrintLog($"-------------------- WebSocket Close: {result.CloseStatus}, {result.CloseStatusDescription}");
                            OnClose?.Invoke((int)result.CloseStatus, result.CloseStatusDescription);
                            return;
                        }
                        data = await MergeData(data, buffer, result.Count);
                    } while (!result.EndOfMessage);
                    // 一个 WebSocket 消息体接收完成
                    try {
                        string message = Encoding.UTF8.GetString(data);
                        OnMessage(message);
                    } catch (Exception e) {
                        AVRealtime.PrintLog($"************************* Parse command error: {e.Message}");
                    }
                }
            } catch (Exception e) {
                AVRealtime.PrintLog($"-------------------- WebSocket Receive Exception: {e.Message}");
                AVRealtime.PrintLog(e.StackTrace);
                // 断线事件
                OnClose?.Invoke(-1, e.Message);
            }
        }

        static async Task<byte[]> MergeData(byte[] oldData, byte[] newData, int newDataLength) {
            return await Task.Run(() => {
                var data = new byte[oldData.Length + newDataLength];
                Array.Copy(oldData, data, oldData.Length);
                Array.Copy(newData, 0, data, oldData.Length, newDataLength);
                AVRealtime.PrintLog($"merge: {data.Length}");
                return data;
            });
        }
    }
}
