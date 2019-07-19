using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using LeanCloud;
using System.Reflection;
using LeanCloud.Realtime.Internal;
using LeanCloud.Storage.Internal;
using System.Threading;

#if UNITY
using UnityEngine;
#endif
namespace LeanCloud.Realtime
{

    /// <summary>
    /// 实时消息的框架类
    /// 包含了 WebSocket 连接以及事件通知的管理
    /// </summary>
    public class AVRealtime
    {
        internal static IDictionary<string, AVIMClient> clients = null;

        private static readonly object mutex = new object();
        private string _wss;
        private string _secondaryWss;
        private string _sesstionToken;
        private long _sesstionTokenExpire;
        private string _clientId;
        private string _deviceId;
        private bool _secure;
        private string _tag;
        private string subprotocolPrefix = "lc.json.";

        static readonly int RECONNECT_DELAY = 5 * 1000;
        static readonly int RECONNECT_USE_SECONDARY_TIMES = 6;
        static readonly int RECONNECT_FROM_APP_ROUTER = 12;

        int reconnectTimes;

        public bool IsSesstionTokenExpired
        {
            get
            {
                return DateTime.Now.ToUnixTimeStamp() > _sesstionTokenExpire;
            }
        }



        private IAVIMCommandRunner avIMCommandRunner;


        /// <summary>
        /// 
        /// </summary>
        public IAVIMCommandRunner AVIMCommandRunner
        {
            get
            {
                lock (mutex)
                {
                    avIMCommandRunner = avIMCommandRunner ?? new AVIMCommandRunner(this.AVWebSocketClient);
                    return avIMCommandRunner;
                }
            }
        }

        private IWebSocketClient webSocketController;
        internal IWebSocketClient AVWebSocketClient
        {
            get
            {
                lock (mutex)
                {
                    webSocketController = webSocketController ?? new DefaultWebSocketClient();
                    return webSocketController;
                }
            }
            set
            {
                lock (mutex)
                {
                    webSocketController = value;
                }
            }
        }

        internal static IAVRouterController RouterController
        {
            get
            {
                return AVIMCorePlugins.Instance.RouterController;
            }
        }

        internal static IFreeStyleMessageClassingController FreeStyleMessageClassingController
        {
            get
            {
                return AVIMCorePlugins.Instance.FreeStyleClassingController;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<AVIMMessageEventArgs> OnOfflineMessageReceived;

        /// <summary>
        /// 与云端通讯的状态
        /// </summary>
        public enum Status
        {
            /// <summary>
            /// 未初始化
            /// </summary>
            None = -1,

            /// <summary>
            /// 正在连接
            /// </summary>
            Connecting = 0,

            /// <summary>
            /// 已连接
            /// </summary>
            Online = 1,

            /// <summary>
            /// 连接已断开
            /// </summary>
            Offline = 2,

            /// <summary>
            /// 正在重连
            /// </summary>
            Reconnecting = 3,

            /// <summary>
            /// websocket 连接已被打开
            /// </summary>
            Opened = 98,

            /// <summary>
            /// 已主动关闭
            /// </summary>
            Closed = 99,
        }

        private AVRealtime.Status state = Status.None;
        public AVRealtime.Status State
        {
            get
            {
                return state;
            }
            private set
            {
                state = value;
            }
        }

        private struct NetworkStateOptions
        {
            public bool Available { get; set; }
        }

        private NetworkStateOptions NetworkState { get; set; }

        private struct WebSocketStateOptions
        {
            public int ClosedCode { get; set; }
        }

        private WebSocketStateOptions WebSocketState { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public struct AVIMReconnectOptions
        {
            /// <summary>
            /// 重连的时间间隔，单位是秒
            /// </summary>
            public long Interval { get; set; }

            /// <summary>
            /// 重连的次数
            /// </summary>
            public int Retry { get; set; }
        }

        internal string Subprotocol
        {
            get
            {
                return subprotocolPrefix + (int)CurrentConfiguration.OfflineMessageStrategy;
            }
        }

        /// <summary>
        /// 重连选项
        /// </summary>
        public AVIMReconnectOptions ReconnectOptions { get; set; }

        private ISignatureFactory _signatureFactory;

        /// <summary>
        /// 签名接口
        /// </summary>
        public ISignatureFactory SignatureFactory
        {
            get
            {
                _signatureFactory = _signatureFactory ?? new DefaulSiganatureFactory();
                return _signatureFactory;
            }
            set
            {
                _signatureFactory = value;
            }
        }

        private bool useLeanEngineSignaturFactory;
        /// <summary>
        /// 启用 LeanEngine 云函数签名
        /// </summary>
        public void UseLeanEngineSignatureFactory()
        {
            useLeanEngineSignaturFactory = true;
            this.SignatureFactory = new LeanEngineSignatureFactory();
        }

        private EventHandler<AVIMDisconnectEventArgs> m_OnDisconnected;
        /// <summary>
        /// 连接断开触发的事件
        /// <remarks>如果其他客户端使用了相同的 Tag 登录，就会导致当前用户被服务端断开</remarks>
        /// </summary>
        public event EventHandler<AVIMDisconnectEventArgs> OnDisconnected
        {
            add
            {
                m_OnDisconnected += value;
            }
            remove
            {
                m_OnDisconnected -= value;
            }
        }

        private EventHandler<AVIMReconnectingEventArgs> m_OnReconnecting;
        /// <summary>
        /// 正在重连时触发的事件
        /// </summary>
        public event EventHandler<AVIMReconnectingEventArgs> OnReconnecting
        {
            add
            {
                m_OnReconnecting += value;
            }
            remove
            {
                m_OnReconnecting -= value;
            }
        }

        private EventHandler<AVIMReconnectedEventArgs> m_OnReconnected;
        /// <summary>
        /// 重连之后触发的事件
        /// </summary>
        public event EventHandler<AVIMReconnectedEventArgs> OnReconnected
        {
            add
            {
                m_OnReconnected += value;
            }
            remove
            {
                m_OnReconnected -= value;
            }
        }

        private EventHandler<AVIMReconnectFailedArgs> m_OnReconnectFailed;

        /// <summary>
        /// 重连失败之后触发的事件
        /// </summary>
        public event EventHandler<AVIMReconnectFailedArgs> OnReconnectFailed
        {
            add
            {
                m_OnReconnectFailed += value;
            }
            remove
            {
                m_OnReconnectFailed -= value;
            }
        }

        /// <summary>
        /// Invokes the state of the network.
        /// </summary>
        /// <param name="available">If set to <c>true</c> broken.</param>
        internal void InvokeNetworkState(bool available = false)
        {
            if (this.NetworkState.Available == available) return;
            SetNetworkState(available);
            PrintLog(string.Format("network connectivity is {0} now", available));
            // 如果断线产生的原因是客户端掉线而不是服务端踢下线，则应该开始自动重连
            var reasonShouldReconnect = new int[] { 0, 1006, 4107 };
            if (this.NetworkState.Available && reasonShouldReconnect.Contains(this.WebSocketState.ClosedCode))
            {
                StartAutoReconnect();
            }
        }

        internal void SetNetworkState(bool available = true)
        {
            this.NetworkState = new NetworkStateOptions()
            {
                Available = available
            };
        }

        private EventHandler<AVIMNotice> m_NoticeReceived;
        public event EventHandler<AVIMNotice> NoticeReceived
        {
            add
            {
                m_NoticeReceived += value;
            }
            remove
            {
                m_NoticeReceived -= value;
            }
        }

        private void WebSocketClient_OnMessage(string obj)
        {
            try
            {
                var estimatedData = Json.Parse(obj) as IDictionary<string, object>;
                var validator = AVIMNotice.IsValidLeanCloudProtocol(estimatedData);
                if (validator)
                {
                    var notice = new AVIMNotice(estimatedData);
                    m_NoticeReceived?.Invoke(this, notice);
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    PrintLog(ex.InnerException.Source);
                }
                if (ex.Source != null)
                {
                    PrintLog(ex.Source);
                }

                PrintLog(ex.Message);
            }
        }

        /// <summary>
        /// 设定监听者
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="runtimeHook"></param>
        public void SubscribeNoticeReceived(IAVIMListener listener, Func<AVIMNotice, bool> runtimeHook = null)
        {
            this.NoticeReceived += new EventHandler<AVIMNotice>((sender, notice) =>
            {
                var approved = runtimeHook == null ? listener.ProtocolHook(notice) : runtimeHook(notice) && listener.ProtocolHook(notice);
                if (approved)
                {
                    listener.OnNoticeReceived(notice);
                }
            });
        }

        /// <summary>
        /// 初始化配置项
        /// </summary>
        public struct Configuration
        {
            /// <summary>
            /// 签名工厂
            /// </summary>
            public ISignatureFactory SignatureFactory { get; set; }
            /// <summary>
            /// 自定义 WebSocket 客户端
            /// </summary>
            public IWebSocketClient WebSocketClient { get; set; }
            /// <summary>
            /// LeanCloud App Id
            /// </summary>
            public string ApplicationId { get; set; }
            /// <summary>
            /// LeanCloud App Key
            /// </summary>
            public string ApplicationKey { get; set; }

            /// <summary>
            /// 登录的时候告知服务器，本次登录所使用的离线消息策略
            /// </summary>
            public OfflineMessageStrategy OfflineMessageStrategy { get; set; }

            /// <summary>
            /// Gets or sets the realtime server.
            /// </summary>
            /// <value>The realtime server.</value>
            public Uri RealtimeServer { get; set; }

            /// <summary>
            /// Gets or sets the push router server.
            /// </summary>
            /// <value>The push router server.</value>
            public Uri RTMRouter { get; set; }
        }

        /// <summary>
        ///登录时的离线消息下发策略
        /// </summary>
        public enum OfflineMessageStrategy
        {
            /// <summary>
            /// 服务器将所有离线消息一次性在登录之后马上下发下来
            /// </summary>
            Default = 1,

            /// <summary>
            /// 不再下发未读消息，而是下发对话的未读通知，告知客户端有哪些对话处于未读状态
            /// </summary>
            [Obsolete("该策略已被废弃，请使用 UnreadAck")]
            UnreadNotice = 2,

            /// <summary>
            /// ack 和 read 分离, ack 不会清理未读消息
            /// </summary>
            UnreadAck = 3
        }

        /// <summary>
        /// 当前配置
        /// </summary>
        public Configuration CurrentConfiguration { get; internal set; }
        /// <summary>
        /// 初始化实时消息客户端
        /// </summary>
        /// <param name="config"></param>
        public AVRealtime(Configuration config)
        {
            lock (mutex)
            {
                if ((int)config.OfflineMessageStrategy == 0)
                {
                    config.OfflineMessageStrategy = OfflineMessageStrategy.UnreadAck;
                }

                CurrentConfiguration = config;
                if (CurrentConfiguration.WebSocketClient != null)
                {
                    webSocketController = CurrentConfiguration.WebSocketClient;
                }
                if (CurrentConfiguration.SignatureFactory != null)
                {
                    this.SignatureFactory = CurrentConfiguration.SignatureFactory;
                }
                ReconnectOptions = new AVIMReconnectOptions()
                {
                    Interval = 5,
                    Retry = 120
                };


                RegisterMessageType<AVIMMessage>();
                RegisterMessageType<AVIMTypedMessage>();
                RegisterMessageType<AVIMTextMessage>();
                RegisterMessageType<AVIMImageMessage>();
                RegisterMessageType<AVIMAudioMessage>();
                RegisterMessageType<AVIMVideoMessage>();
                RegisterMessageType<AVIMFileMessage>();
                RegisterMessageType<AVIMLocationMessage>();
                RegisterMessageType<AVIMRecalledMessage>();

                // 注册服务端 goaway 指令
                var goAwayListener = new GoAwayListener();
                goAwayListener.OnGoAway += () => {
                    RouterController.ClearCache().ContinueWith(_ => {
                        reborn = true;
                        // 关闭 WebSocket
                        AVWebSocketClient.Disconnect();
                    });
                };
                SubscribeNoticeReceived(goAwayListener);

                reconnectTimes = 0;
            }
        }

        /// <summary>
        /// 初始化实时消息客户端
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="applicationKey"></param>
        public AVRealtime(string applicationId, string applicationKey)
            : this(new Configuration()
            {
                ApplicationId = applicationId,
                ApplicationKey = applicationKey,
                OfflineMessageStrategy = OfflineMessageStrategy.UnreadAck
            })
        {

        }

        #region websocket log
        internal static Action<string> LogTracker { get; private set; }
        /// <summary>
        /// 打开 WebSocket 日志
        /// </summary>
        /// <param name="trace"></param>
        public static void WebSocketLog(Action<string> trace)
        {
            LogTracker = trace;
        }
        public static void PrintLog(string log)
        {
            if (AVRealtime.LogTracker != null)
            {
                AVRealtime.LogTracker(log);
            }
        }
        #endregion

        /// <summary>
        /// 创建 Client
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="tag"></param>
        /// <param name="deviceId">设备唯一的 Id。如果是 iOS 设备，需要将 iOS 推送使用的 DeviceToken 作为 deviceId 传入</param>
        /// <param name="secure">是否强制加密 wss 链接</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<AVIMClient> CreateClientAsync(string clientId,
            string tag = null,
            string deviceId = null,
            bool secure = true,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            lock (mutex)
            {
                var client = PreLogIn(clientId, tag, deviceId);

                AVRealtime.PrintLog("begin OpenAsync.");
                return OpenAsync(secure, Subprotocol, true, cancellationToken).OnSuccess(t =>
                  {
                      if (!t.Result)
                      {
                          return Task.FromResult<AVIMCommand>(null);
                      }
                      AVRealtime.PrintLog("websocket server connected, begin to open sesstion.");
                      SetNetworkState();
                      var cmd = new SessionCommand()
                         .UA(VersionString)
                         .Tag(tag)
                         .DeviceId(deviceId)
                         .Option("open")
                         .PeerId(clientId);

                      ToggleNotification(true);
                      return AttachSignature(cmd, this.SignatureFactory.CreateConnectSignature(clientId));

                  }).Unwrap().OnSuccess(x =>
                  {
                      var cmd = x.Result;
                      if (cmd == null)
                      {
                          return Task.FromResult<Tuple<int, IDictionary<string, object>>>(null);
                      }
                      return this.RunCommandAsync(cmd);
                  }).Unwrap().OnSuccess(s =>
                   {
                       if (s.Result == null)
                       {
                           return null;
                       }
                       AVRealtime.PrintLog("sesstion opened.");
                       state = Status.Online;
                       ToggleHeartBeating(true);
                       var response = s.Result.Item2;
                       if (response.ContainsKey("st"))
                       {
                           _sesstionToken = response["st"] as string;
                       }
                       if (response.ContainsKey("stTtl"))
                       {
                           var stTtl = long.Parse(response["stTtl"].ToString());
                           _sesstionTokenExpire = DateTime.Now.ToUnixTimeStamp() + stTtl * 1000;
                       }
                       AfterLogIn(client);
                       return client;
                   });
            }
        }



        /// <summary>
        /// Creates the client async.
        /// </summary>
        /// <returns>The client async.</returns>
        /// <param name="user">User.</param>
        /// <param name="tag">Tag.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="secure">If set to <c>true</c> secure.</param>
        public Task<AVIMClient> CreateClientAsync(AVUser user = null,
                                                  string tag = null,
                                                  string deviceId = null,
                                                  bool secure = true,
                                                  CancellationToken cancellationToken = default(CancellationToken))
        {
            AVIMClient client = null;
            AVRealtime.PrintLog("begin OpenAsync.");
            return OpenAsync(secure, Subprotocol, true, cancellationToken).OnSuccess(openTask =>
             {
                 AVRealtime.PrintLog("OpenAsync OnSuccess. begin send open sesstion cmd.");
                 var userTask = Task.FromResult(user);
                 if (user == null)
                     userTask = AVUser.GetCurrentUserAsync();

                 return userTask;
             }).Unwrap().OnSuccess(u =>
             {
                 var theUser = u.Result;
                 return AVCloud.RequestRealtimeSignatureAsync(theUser);
             }).Unwrap().OnSuccess(signTask =>
             {
                 var signResult = signTask.Result;
                 var clientId = signResult.ClientId;
                 var nonce = signResult.Nonce;
                 var singnature = signResult.Signature;
                 var ts = signResult.Timestamp;

                 client = PreLogIn(clientId, tag, deviceId);
                 ToggleNotification(true);
                 return this.OpenSessionAsync(clientId, tag, deviceId, nonce, ts, singnature, secure);
             }).Unwrap().OnSuccess(s =>
             {
                 ToggleHeartBeating(true);
                 AfterLogIn(client);
                 return client;
             });
        }

        #region pre-login
        internal AVIMClient PreLogIn(string clientId,
            string tag = null,
            string deviceId = null)
        {
            var client = new AVIMClient(clientId, tag, this);
            if (this.OnOfflineMessageReceived != null)
            {
                client.OnOfflineMessageReceived += this.OnOfflineMessageReceived;
            }
            _clientId = clientId;
            _tag = tag;
            _deviceId = deviceId;
            if (_tag != null)
            {
                if (deviceId == null)
                    throw new ArgumentNullException(deviceId, "当 tag 不为空时，必须传入当前设备不变的唯一 id(deviceId)");
            }

            if (string.IsNullOrEmpty(clientId)) throw new Exception("当前 ClientId 为空，无法登录服务器。");

            return client;
        }

        internal void AfterLogIn(AVIMClient client)
        {
            if (clients == null) clients = new Dictionary<string, AVIMClient>();
            client.OnSessionClosed += (sender, e) => {
                string clientId = (sender as AVIMClient).ClientId;
                clients.Remove(clientId);
                if (clients.Count == 0) {
                    LogOut();
                }
            };
            clients[client.ClientId] = client;
        }

        #endregion

        #region after-login


        #endregion

        /// <summary>
        /// 创建 Client
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="tag"></param>
        /// <param name="deviceId">设备唯一的 Id。如果是 iOS 设备，需要将 iOS 推送使用的 DeviceToken 作为 deviceId 传入</param>
        /// <param name="secure">是否强制加密 wss 链接</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Obsolete("CreateClient is deprecated, please use CreateClientAsync instead.")]
        public Task<AVIMClient> CreateClient(
            string clientId,
            string tag = null,
            string deviceId = null,
            bool secure = true,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.CreateClientAsync(clientId, tag, deviceId, secure, cancellationToken);
        }

        private bool _listening = false;
        /// <summary>
        /// websocket 事件的监听的开关
        /// </summary>
        /// <param name="toggle">是否打开</param>
        public void ToggleNotification(bool toggle)
        {
            AVRealtime.PrintLog("ToggleNotification| toggle:" + toggle + "|listening: " + _listening);
            if (toggle && !_listening)
            {
                AVWebSocketClient.OnClosed += WebsocketClient_OnClosed;
                AVWebSocketClient.OnMessage += WebSocketClient_OnMessage;
                _listening = true;
            }
            else if (!toggle && _listening)
            {
                AVWebSocketClient.OnClosed -= WebsocketClient_OnClosed;
                AVWebSocketClient.OnMessage -= WebSocketClient_OnMessage;
                _listening = false;
            }
        }

        //public void ToggleOfflineNotification(bool toggle)
        //{
        //    if (toggle)
        //    {
        //        PCLWebsocketClient.OnMessage += WebSocketClient_OnMessage_On_Session_Opening;
        //    }
        //    else
        //    {
        //        PCLWebsocketClient.OnMessage -= WebSocketClient_OnMessage_On_Session_Opening;
        //    }
        //}

        //private void WebSocketClient_OnMessage_On_Session_Opening(string obj)
        //{
        //    AVRealtime.PrintLog("offline<=" + obj);
        //}


        string _beatPacket = "{}";
        bool _heartBeatingToggle = true;
        IAVTimer _heartBeatingTimer;
        /// <summary>
        /// 主动发送心跳包
        /// </summary>
        /// <param name="toggle">是否开启</param>
        /// <param name="interval">时间间隔</param>
        /// <param name="beatPacket">心跳包的内容，默认是个空的 {}</param>
        public void ToggleHeartBeating(bool toggle = true, double interval = 60000, string beatPacket = "{}")
        {
            this._heartBeatingToggle = toggle;
            if (!string.Equals(_beatPacket, beatPacket)) _beatPacket = beatPacket;

            if (_heartBeatingTimer == null && this._heartBeatingToggle)
            {
                _heartBeatingTimer = new AVTimer();
                _heartBeatingTimer.Elapsed += SendHeartBeatingPacket;
                _heartBeatingTimer.Interval = interval;
                _heartBeatingTimer.Start();
                PrintLog("auto heart beating started.");
            }
            if (!this._heartBeatingToggle && _heartBeatingTimer != null)
            {
                _heartBeatingTimer.Stop();
                _heartBeatingTimer = null;
            }
        }

        void SendHeartBeatingPacket(object sender, TimerEventArgs e)
        {
            PrintLog("auto heart beating ticked by timer.");
#if MONO || UNITY
            Dispatcher.Instance.Post(() =>
            {
                KeepAlive();
            });
#else
            KeepAlive();
#endif
        }

        /// <summary>
        /// Keeps the alive.
        /// </summary>
        public void KeepAlive()
        {
            try
            {
                var cmd = new AVIMCommand();
                RunCommandAsync(cmd).ContinueWith(t =>
                {
                    if (t.IsCanceled || t.IsFaulted || t.Exception != null)
                    {
                        InvokeNetworkState();
                    }
                });
            }
            catch (Exception)
            {
                InvokeNetworkState();
            }
        }

        internal bool sessionConflict = false;
        internal bool loggedOut = false;

        internal bool CanReconnect
        {
            get
            {
                return !sessionConflict && !loggedOut && state == Status.Offline;
            }
        }

        /// <summary>
        /// 开始自动重连
        /// </summary>
        public void StartAutoReconnect()
        {

        }
        internal bool useSecondary = false;
        internal bool reborn = false;

        internal Task LogInAsync(string clientId,
            string tag = null,
            string deviceId = null,
            bool secure = true,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            lock (mutex)
            {
                var cmd = new SessionCommand()
                           .UA(VersionString)
                           .Tag(tag)
                           .DeviceId(deviceId)
                           .Option("open")
                           .PeerId(clientId);

                var result = AttachSignature(cmd, this.SignatureFactory.CreateConnectSignature(clientId)).OnSuccess(_ =>
                {
                    return RunCommandAsync(cmd);
                }).Unwrap().OnSuccess(t =>
                {
                    AVRealtime.PrintLog("sesstion opened.");
                    if (t.Exception != null)
                    {
                        var imException = t.Exception.InnerException as AVIMException;
                        throw imException;
                    }
                    state = Status.Online;
                    var response = t.Result.Item2;
                    if (response.ContainsKey("st"))
                    {
                        _sesstionToken = response["st"] as string;
                    }
                    if (response.ContainsKey("stTtl"))
                    {
                        var stTtl = long.Parse(response["stTtl"].ToString());
                        _sesstionTokenExpire = DateTime.Now.ToUnixTimeStamp() + stTtl * 1000;
                    }
                    return t.Result;
                });

                return result;
            }

        }

        internal Task OpenSessionAsync(string clientId,
            string tag = null,
            string deviceId = null,
            string nonce = null,
            long timestamp = 0,
            string signature = null,
            bool secure = true)
        {
            var cmd = new SessionCommand()
                .UA(VersionString)
                .Tag(tag)
                .DeviceId(deviceId)
                .Option("open")
                .PeerId(clientId)
                .Argument("n", nonce)
                .Argument("t", timestamp)
                .Argument("s", signature);

            return RunCommandAsync(cmd).OnSuccess(t =>
            {
                AVRealtime.PrintLog("sesstion opened.");
                if (t.Exception != null)
                {
                    var imException = t.Exception.InnerException as AVIMException;
                    throw imException;
                }
                state = Status.Online;
                var response = t.Result.Item2;
                if (response.ContainsKey("st"))
                {
                    _sesstionToken = response["st"] as string;
                }
                if (response.ContainsKey("stTtl"))
                {
                    var stTtl = long.Parse(response["stTtl"].ToString());
                    _sesstionTokenExpire = DateTime.Now.ToUnixTimeStamp() + stTtl * 1000;
                }
                return t.Result;
            });

        }

        /// <summary>
        /// 自动重连
        /// </summary>
        /// <returns></returns>
        Task AutoReconnect()
        {
            AVRealtime.PrintLog("AutoReconnect started.");
            var reconnectingArgs = new AVIMReconnectingEventArgs()
            {
                ClientId = _clientId,
                IsAuto = true,
                SessionToken = _sesstionToken
            };
            m_OnReconnecting?.Invoke(this, reconnectingArgs);

            var tcs = new TaskCompletionSource<bool>();
            Task<bool> task;
            if (reborn)
            {
                AVRealtime.PrintLog("both preferred and secondary websockets are expired, so try to request RTM router to get a new pair");
                task = OpenAsync(this._secure, Subprotocol, true);
            } else {
                var websocketServer = _wss;
                if (useSecondary) {
                    AVRealtime.PrintLog(string.Format("preferred websocket server ({0}) network broken, take secondary server({1}) :", _wss, _secondaryWss));
                    websocketServer = _secondaryWss;
                }
                task = OpenAsync(websocketServer, Subprotocol, true);
            }

            task.ContinueWith(t =>
            {
                if (t.IsFaulted || t.IsCanceled) {
                    state = Status.Reconnecting;
                    var reconnectFailedArgs = new AVIMReconnectFailedArgs() {
                        ClientId = _clientId,
                        IsAuto = true,
                        SessionToken = _sesstionToken,
                        FailedCode = 0// network broken.
                    };
                    m_OnReconnectFailed?.Invoke(this, reconnectFailedArgs);
                    state = Status.Offline;
                    tcs.SetException(t.Exception);
                    throw t.Exception;
                } else {
                    state = Status.Opened;
                    SetNetworkState();

                    void onClose(int code, string reason, string detail) {
                        AVRealtime.PrintLog("disconnect when open session");
                        var ex = new Exception("connection is closed");
                        tcs.SetException(ex);
                        AVWebSocketClient.OnClosed -= onClose;
                        throw ex;
                    };
                    AVWebSocketClient.OnClosed += onClose;

                    if (this.IsSesstionTokenExpired) {
                        AVRealtime.PrintLog("session is expired, auto relogin with clientId :" + _clientId);
                        return this.LogInAsync(_clientId, this._tag, this._deviceId, this._secure).ContinueWith(o => {
                            AVWebSocketClient.OnClosed -= onClose;
                            return !o.IsFaulted;
                        });
                    } else {
                        var sessionCMD = new SessionCommand().UA(VersionString).R(1);

                        if (string.IsNullOrEmpty(_tag)) {
                            sessionCMD = sessionCMD.Tag(_tag).SessionToken(this._sesstionToken);
                        }

                        var cmd = sessionCMD.Option("open") 
                         .PeerId(_clientId);

                        AVRealtime.PrintLog("reopen session with session token :" + _sesstionToken);
                        return RunCommandAsync(cmd).ContinueWith(o => {
                            AVWebSocketClient.OnClosed -= onClose;
                            return !o.IsFaulted;
                        });
                    }
                }
            }).Unwrap().ContinueWith(s =>
            {
                if (s.IsFaulted || s.Exception != null)
                {
                    var reconnectFailedArgs = new AVIMReconnectFailedArgs()
                    {
                        ClientId = _clientId,
                        IsAuto = true,
                        SessionToken = _sesstionToken,
                        FailedCode = 1
                    };
                    m_OnReconnectFailed?.Invoke(this, reconnectFailedArgs);
                    state = Status.Offline;
                    tcs.SetException(s.Exception);
                }
                else
                {
                    var reconnectedArgs = new AVIMReconnectedEventArgs() {
                        ClientId = _clientId,
                        IsAuto = true,
                        SessionToken = _sesstionToken,
                    };
                    state = Status.Online;
                    m_OnReconnected?.Invoke(this, reconnectedArgs);
                    ToggleNotification(true);
                    ToggleHeartBeating(true);
                    tcs.SetResult(true);
                }
            });

            return tcs.Task;
        }



        #region register IAVIMMessage
        /// <summary>
        /// Registers the subtype of the message.
        /// </summary>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public void RegisterMessageType<T>() where T : IAVIMMessage
        {
            AVIMCorePlugins.Instance.FreeStyleClassingController.RegisterSubclass(typeof(T));
        }
        #endregion

        /// <summary>
        /// open websocket with default configurations.
        /// </summary>
        /// <returns></returns>
        public Task<bool> OpenAsync(bool secure = true)
        {
            return this.OpenAsync(secure, null);
        }

        /// <summary>
        /// Open websocket connection.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="secure">If set to <c>true</c> secure.</param>
        /// <param name="subprotocol">Subprotocol.</param>
        /// <param name="enforce">If set to <c>true</c> enforce.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<bool> OpenAsync(bool secure, string subprotocol = null, bool enforce = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            _secure = secure;
            if (state == Status.Online && !enforce)
            {
                AVRealtime.PrintLog("state is Status.Online.");
                return Task.FromResult(true);
            }

            if (CurrentConfiguration.RealtimeServer != null)
            {
                _wss = CurrentConfiguration.RealtimeServer.ToString();
                AVRealtime.PrintLog("use configuration realtime server with url: " + _wss);
                return OpenAsync(_wss, subprotocol, enforce);
            }
            var routerUrl = CurrentConfiguration.RTMRouter != null ? CurrentConfiguration.RTMRouter.ToString() : null;
            return RouterController.GetAsync(routerUrl, secure, cancellationToken).OnSuccess(r =>
                {
                    var routerState = r.Result;
                    if (routerState == null)
                    {
                        return Task.FromResult(false);
                    }
                    _wss = routerState.server;
                    _secondaryWss = routerState.secondary;
                    state = Status.Connecting;
                    AVRealtime.PrintLog("push router give a url :" + _wss);
                    return OpenAsync(routerState.server, subprotocol, enforce);
                }).Unwrap();
        }

        /// <summary>
        /// open webcoket connection with cloud.
        /// </summary>
        /// <param name="url">wss address</param>
        /// <param name="subprotocol">subprotocol for websocket</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<bool> OpenAsync(string url, string subprotocol = null, bool enforce = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (AVWebSocketClient.IsOpen && !enforce)
            {
                AVRealtime.PrintLog(url + "is already connectd.");
                return Task.FromResult(true);
            }

            AVRealtime.PrintLog("websocket try to connect url :" + url + " with subprotocol: " + subprotocol);
            AVRealtime.PrintLog(url + " \tconnecting...");

            return AVWebSocketClient.Connect(url, subprotocol);
        }

        /// <summary>
        /// send websocket command to Realtime server.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public Task<Tuple<int, IDictionary<string, object>>> RunCommandAsync(AVIMCommand command)
        {
            command.AppId(this.CurrentConfiguration.ApplicationId);
            return this.AVIMCommandRunner.RunCommandAsync(command);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        public void RunCommand(AVIMCommand command)
        {
            command.AppId(this.CurrentConfiguration.ApplicationId);
            this.AVIMCommandRunner.RunCommand(command);
        }

        internal Task<AVIMCommand> AttachSignature(AVIMCommand command, Task<AVIMSignature> SignatureTask)
        {
            AVRealtime.PrintLog("begin to attach singature.");
            var tcs = new TaskCompletionSource<AVIMCommand>();
            if (SignatureTask == null)
            {
                tcs.SetResult(command);
                return tcs.Task;
            }
            return SignatureTask.OnSuccess(_ =>
            {
                if (_.Result != null)
                {
                    var signature = _.Result;
                    command.Argument("t", signature.Timestamp);
                    command.Argument("n", signature.Nonce);
                    command.Argument("s", signature.SignatureContent);
                    AVRealtime.PrintLog("AttachSignature ended.t:" + signature.Timestamp + ";n:" + signature.Nonce + ";s:" + signature.SignatureContent);
                }
                return command;
            });
        }

        #region log out and clean event subscribtion
        private void WebsocketClient_OnClosed(int errorCode, string reason, string detail)
        {
            PrintLog(string.Format("websocket closed with code is {0},reason is {1} and detail is {2}", errorCode, reason, detail));
            state = Status.Offline;

            ToggleNotification(false);
            ToggleHeartBeating(false);

            var disconnectEventArgs = new AVIMDisconnectEventArgs(errorCode, reason, detail);
            m_OnDisconnected?.Invoke(this, disconnectEventArgs);

            this.WebSocketState = new WebSocketStateOptions()
            {
                ClosedCode = errorCode
            };
            PrepareReconnect();
        }

        private void WebsocketClient_OnError(string obj)
        {
            PrintLog("error:" + obj);
            // 如果遇到 WebSocket 错误之后，先关闭，再按断线处理
            AVWebSocketClient.Close();
            WebsocketClient_OnClosed(0, obj, string.Empty);
        }

        void PrepareReconnect() {
            AVRealtime.PrintLog("Prepare Reconnect");
            Task.Delay(RECONNECT_DELAY).ContinueWith(_ => {
                // 开启重连
                AutoReconnect().ContinueWith(t => {
                    if (t.IsFaulted) {
                        // 重连失败，延迟再次重连
                        reconnectTimes++;
                        AVRealtime.PrintLog(String.Format("reconnect {0} times", reconnectTimes));
                        if (reconnectTimes >= RECONNECT_FROM_APP_ROUTER) {
                            // 如果大于当前服务地址的最大重连次数，则清空 Router 后重新重连
                            RouterController.ClearCache().ContinueWith(__ => {
                                reborn = true;
                                PrepareReconnect();
                            });

                        } else if (reconnectTimes >= RECONNECT_USE_SECONDARY_TIMES) {
                            // 如果大于单台 IM 服务器的重连次数，则启用备用服务器
                            useSecondary = true;
                            PrepareReconnect();
                        } else {
                            PrepareReconnect();
                        }
                    } else {
                        // 重连成功
                        reconnectTimes = 0;
                        reborn = false;
                        useSecondary = false;
                    }
                });
            });
        }

        internal void LogOut()
        {
            State = Status.Closed;
            loggedOut = true;
            Dispose();
            AVWebSocketClient.Close();
        }

        internal void Dispose()
        {
            var toggle = false;
            ToggleNotification(toggle);
            ToggleHeartBeating(toggle);

            if (m_NoticeReceived != null)
            {
                foreach (Delegate d in m_NoticeReceived.GetInvocationList())
                {
                    m_NoticeReceived -= (EventHandler<AVIMNotice>)d;
                }
            }
            if (m_OnDisconnected != null)
            {
                foreach (Delegate d in m_OnDisconnected.GetInvocationList())
                {
                    m_OnDisconnected -= (EventHandler<AVIMDisconnectEventArgs>)d;
                }
            }
        }
        #endregion

        static AVRealtime()
        {
#if MONO || UNITY
            versionString = "net-unity/" + Version;
#else
            versionString = "net-universal/" + Version;
#endif
        }

        private static readonly string versionString;
        internal static string VersionString
        {
            get
            {
                return versionString;
            }
        }

        internal static System.Version Version
        {
            get
            {
                AssemblyName assemblyName = new AssemblyName(typeof(AVRealtime).GetTypeInfo().Assembly.FullName);
                return assemblyName.Version;
            }
        }
    }
}
