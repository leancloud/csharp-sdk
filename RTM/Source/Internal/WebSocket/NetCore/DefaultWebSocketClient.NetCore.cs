using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    public class DefaultWebSocketClient : IWebSocketClient
    {
        private const int ReceiveChunkSize = 1024;
        private const int SendChunkSize = 1024;

        private ClientWebSocket _ws;
        private Uri _uri;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Occurs when on closed.
        /// </summary>
        public event Action<int, string, string> OnClosed;
        /// <summary>
        /// Occurs when on error.
        /// </summary>
        public event Action<string> OnError;
        /// <summary>
        /// Occurs when on log.
        /// </summary>
        public event Action<string> OnLog;
        /// <summary>
        /// Occurs when on opened.
        /// </summary>
        public event Action OnOpened;

        public bool IsOpen => _ws.State == WebSocketState.Open;

        public DefaultWebSocketClient()
        {
            _ws = NewWebSocket();
            _cancellationToken = _cancellationTokenSource.Token;
        }

        public event Action<string> OnMessage;

        private async void StartListen()
        {
            var buffer = new byte[8192];

            try
            {
                while (_ws.State == WebSocketState.Open)
                {
                    var stringResult = new StringBuilder();

                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await
                                _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            CallOnDisconnected();
                        }
                        else
                        {
                            var str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            stringResult.Append(str);
                        }

                    } while (!result.EndOfMessage);

                    CallOnMessage(stringResult);

                }
            }
            catch (Exception)
            {
                CallOnDisconnected();
            }
            finally
            {
                _ws.Dispose();
            }
        }

        private void CallOnMessage(StringBuilder stringResult)
        {
            if (OnMessage != null)
                RunInTask(() => OnMessage(stringResult.ToString()));
        }

        private void CallOnDisconnected()
        {
            AVRealtime.PrintLog("PCL websocket closed without parameters.");
            if (OnClosed != null)
                RunInTask(() => this.OnClosed(0, "", ""));
        }

        private void CallOnConnected()
        {
            if (OnOpened != null)
                RunInTask(() => this.OnOpened());
        }

        private static void RunInTask(Action action)
        {
            Task.Factory.StartNew(action);
        }

        public async void Close()
        {
            if (_ws != null)
            {
                try
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    CallOnDisconnected();
                }
                catch (Exception ex)
                {
                    CallOnDisconnected();
                }
            }
        }

        public void Disconnect() {
            _ws?.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }

        public async void Open(string url, string protocol = null)
        {
            _uri = new Uri(url);
            if (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.Connecting)
            {
                _ws = NewWebSocket();
            }
            try
            {
                await _ws.ConnectAsync(_uri, _cancellationToken);
                CallOnConnected();
                StartListen();
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException)
                {
                    OnError($"can NOT connect server with url: {url}");
                }
            }
        }

        public async void Send(string message)
        {
            if (_ws.State != WebSocketState.Open)
            {
                throw new Exception("Connection is not open.");
            }

            var encoded = Encoding.UTF8.GetBytes(message);
            var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);
            await _ws.SendAsync(buffer, WebSocketMessageType.Text, true, _cancellationToken);
        }

        ClientWebSocket NewWebSocket()
        {
            var result = new ClientWebSocket();
            result.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
            return result;
        }
    }
}