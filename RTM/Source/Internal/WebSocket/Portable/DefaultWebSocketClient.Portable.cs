using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Websockets;

namespace LeanCloud.Realtime.Internal
{
    /// <summary>
    /// LeanCloud Realtime SDK for .NET Portable 内置默认的 WebSocketClient
    /// </summary>
    public class DefaultWebSocketClient : IWebSocketClient
    {
        internal IWebSocketConnection connection;
        /// <summary>
        /// LeanCluod .NET Realtime SDK 内置默认的 WebSocketClient
        /// 开发者可以在初始化的时候指定自定义的 WebSocketClient
        /// </summary>
        public DefaultWebSocketClient()
        {
           
        }

        public event Action<int, string, string> OnClosed;
        public event Action OnOpened;
        public event Action<string> OnMessage;

        public bool IsOpen
        {
            get
            {
                return connection != null && connection.IsOpen;
            }
        }

        public void Close()
        {
            if (connection != null)
            {
                connection.OnOpened -= Connection_OnOpened;
                connection.OnMessage -= Connection_OnMessage;
                connection.OnClosed -= Connection_OnClosed;
                connection.OnError -= Connection_OnError;
                try {
                    connection.Close();
                } catch (Exception e) {
                    AVRealtime.PrintLog(string.Format("close websocket error: {0}", e.Message));
                }
            }
        }

        public void Disconnect() {
            connection?.Close();
        }

        public void Open(string url, string protocol = null)
        {
            // 在每次打开时，重新创建 WebSocket 对象
            connection = WebSocketFactory.Create();
            connection.OnOpened += Connection_OnOpened;
            connection.OnMessage += Connection_OnMessage;
            connection.OnClosed += Connection_OnClosed;
            connection.OnError += Connection_OnError;
            if (!string.IsNullOrEmpty(protocol))
            {
                url = string.Format("{0}?subprotocol={1}", url, protocol);
            }
            connection.Open(url, protocol);
        }

        private void Connection_OnOpened()
        {
            OnOpened?.Invoke();
        }

        private void Connection_OnMessage(string obj)
        {
            AVRealtime.PrintLog("websocket<=" + obj);
            OnMessage?.Invoke(obj);
        }

        private void Connection_OnClosed()
        {
            AVRealtime.PrintLog("PCL websocket closed without parameters..");
            OnClosed?.Invoke(0, "", "");
        }

        private void Connection_OnError(string obj)
        {
            AVRealtime.PrintLog($"PCL websocket error:  {obj}");
            connection?.Close();
        }

        public void Send(string message)
        {
            if (connection != null)
            {
                if (this.IsOpen)
                {
                    connection.Send(message);
                }
                else
                {
                    var log = "Connection is NOT open when send message";
                    AVRealtime.PrintLog(log);
                    connection?.Close();
                }
            }
            else {
                AVRealtime.PrintLog("Connection is NULL");
            }
        }

        public Task<bool> Connect(string url, string protocol = null) {
            var tcs = new TaskCompletionSource<bool>();
            Action onOpen = null;
            Action onClose = null;
            Action<string> onError = null;
            onOpen = () => {
                AVRealtime.PrintLog("PCL websocket opened");
                connection.OnOpened -= onOpen;
                connection.OnClosed -= onClose;
                connection.OnError -= onError;
                // 注册事件
                connection.OnMessage += Connection_OnMessage;
                connection.OnClosed += Connection_OnClosed;
                connection.OnError += Connection_OnError;
                tcs.SetResult(true);
            };
            onClose = () => {
                connection.OnOpened -= onOpen;
                connection.OnClosed -= onClose;
                connection.OnError -= onError;
                tcs.SetException(new Exception("连接关闭"));
            };
            onError = (err) => {
                AVRealtime.PrintLog(string.Format("连接错误：{0}", err));
                connection.OnOpened -= onOpen;
                connection.OnClosed -= onClose;
                connection.OnError -= onError;
                try {
                    connection.Close();
                } catch (Exception e) {
                    AVRealtime.PrintLog(string.Format("关闭错误的 WebSocket 异常：{0}", e.Message));
                } finally {
                    tcs.SetException(new Exception(string.Format("连接错误：{0}", err)));
                }
            };

            // 在每次打开时，重新创建 WebSocket 对象
            connection = WebSocketFactory.Create();
            connection.OnOpened += onOpen;
            connection.OnClosed += onClose;
            connection.OnError += onError;
            // 
            if (!string.IsNullOrEmpty(protocol)) {
                url = string.Format("{0}?subprotocol={1}", url, protocol);
            }
            connection.Open(url, protocol);
            return tcs.Task;
        }
    }
}
