using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocketSharp;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{

    /// <summary>
    /// LeanCluod Unity Realtime SDK 内置默认的 WebSocketClient
    /// 开发者可以在初始化的时候指定自定义的 WebSocketClient
    /// </summary>
    public class DefaultWebSocketClient : IWebSocketClient
    {
        WebSocket ws;
        public bool IsOpen
        {
            get
            {
                if (ws == null) { return false; }
                return ws.IsAlive;
            }
        }

        public event Action<int, string, string> OnClosed;
        public event Action<string> OnMessage
        {
            add
            {
                onMesssageCount++;
                AVRealtime.PrintLog("DefaultWebSocketClient.OnMessage event add with " + onMesssageCount + " times");
                m_OnMessage += value;

            }
            remove
            {
                onMesssageCount--;
                AVRealtime.PrintLog("DefaultWebSocketClient.OnMessage event remove with " + onMesssageCount + " times");
                m_OnMessage -= value;
            }
        }
        private Action<string> m_OnMessage;
        private int onMesssageCount = 0;
        public event Action OnOpened;

        public void Close()
        {
            ws.CloseAsync();
            ws.OnOpen -= OnOpen;
            ws.OnMessage -= OnWebSokectMessage;
            ws.OnClose -= OnClose;
        }

        public void Disconnect() {
            ws.CloseAsync();
        }

        public void Open(string url, string protocol = null)
        {
            if (!string.IsNullOrEmpty(protocol))
            {
                url = string.Format("{0}?subprotocol={1}", url, protocol);
            }
            ws = new WebSocket(url);
            ws.OnOpen += OnOpen;
            ws.OnMessage += OnWebSokectMessage;
            ws.OnClose += OnClose;
            ws.ConnectAsync();
        }

        private void OnWebSokectMessage(object sender, MessageEventArgs e)
        {
            AVRealtime.PrintLog("websocket<=" + e.Data);
            m_OnMessage?.Invoke(e.Data);
        }

        private void OnClose(object sender, CloseEventArgs e) {
            AVRealtime.PrintLog(string.Format("Unity websocket closed with {0}, {1}", e.Code, e.Reason));
            OnClosed?.Invoke(e.Code, e.Reason, null);
        }

        void OnWebSocketError(object sender, ErrorEventArgs e) {
            AVRealtime.PrintLog($"PCL websocket error:  {e.Message}");
            ws?.Close();
        }

        private void OnOpen(object sender, EventArgs e) {
            OnOpened?.Invoke();
        }

        public void Send(string message)
        {
			ws.SendAsync(message, (b) =>
			{

			});
        }

        public Task<bool> Connect(string url, string protocol = null) {
            var tcs = new TaskCompletionSource<bool>();
            EventHandler onOpen = null;
            EventHandler<CloseEventArgs> onClose = null;
            EventHandler<ErrorEventArgs> onError = null;
            onOpen = (sender, e) => {
                AVRealtime.PrintLog("PCL websocket opened");
                ws.OnOpen -= onOpen;
                ws.OnClose -= onClose;
                ws.OnError -= onError;
                // 注册事件
                ws.OnMessage += OnWebSokectMessage;
                ws.OnClose += OnClose;
                ws.OnError += OnWebSocketError;
                tcs.SetResult(true);
            };
            onClose = (sender, e) => {
                ws.OnOpen -= onOpen;
                ws.OnClose -= onClose;
                ws.OnError -= onError;
                tcs.SetException(new Exception("连接关闭"));
            };
            onError = (sender, e) => {
                AVRealtime.PrintLog(string.Format("连接错误：{0}", e.Message));
                ws.OnOpen -= onOpen;
                ws.OnClose -= onClose;
                ws.OnError -= onError;
                try {
                    ws.Close();
                } catch (Exception ex) {
                    AVRealtime.PrintLog(string.Format("关闭错误的 WebSocket 异常：{0}", ex.Message));
                } finally {
                    tcs.SetException(new Exception(string.Format("连接错误：{0}", e.Message)));
                }
            };

            // 在每次打开时，重新创建 WebSocket 对象
            if (!string.IsNullOrEmpty(protocol)) {
                url = string.Format("{0}?subprotocol={1}", url, protocol);
            }
            ws = new WebSocket(url);
            ws.OnOpen += onOpen;
            ws.OnClose += onClose;
            ws.OnError += onError;
            ws.ConnectAsync();
            return tcs.Task;
        }
    }
}
