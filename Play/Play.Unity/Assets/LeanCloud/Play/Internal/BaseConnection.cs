using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using LC.Google.Protobuf;
using LeanCloud.Realtime.Internal.WebSocket;
using LeanCloud.Play.Protocol;
using LeanCloud.Play.kcp2k;

namespace LeanCloud.Play {
    public abstract class BaseConnection {
        enum State {
            /// <summary>
            /// Initial
            /// </summary>
            None,
            Connecting,
            /// <summary>
            /// Connected
            /// </summary>
            Open,
            Closed,
        }

        internal Action<CommandType, OpType, Body> OnNotification;
        internal Action OnDisconnected;

        private const int SEND_TIMEOUT = 10000;

        private const string SUB_PROTOCOL = "protobuf.1";

        private readonly Dictionary<int, TaskCompletionSource<ResponseWrapper>> requestToResponses;

        private PlayHeartBeat heartBeat;

        // private LCWebSocketClient ws;
        private KcpClientWrapper kcp;

        private State state;
        // 可以在 connecting 状态时拿到 Task，并在重连成功后继续操作
        private Task connectTask;

        private bool isMessageQueueRunning;
        private Queue<CommandWrapper> messageQueue;

        private readonly string appId;
        private readonly string server;
        private readonly string gameVersion;
        private readonly string userId;
        private readonly string sessionToken;

        internal BaseConnection(string appId, string server, string gameVersion, string userId, string sessionToken) {
            this.appId = appId;
            this.server = server;
            this.gameVersion = gameVersion;
            this.userId = userId;
            this.sessionToken = sessionToken;

            requestToResponses = new Dictionary<int, TaskCompletionSource<ResponseWrapper>>();

            heartBeat = new PlayHeartBeat(this, KeepAliveInterval, OnDisconnect);
            // ws = new LCWebSocketClient {
            //     OnMessage = OnMessage,
            //     OnClose = OnDisconnect
            // };
            kcp = new KcpClientWrapper
            {
                OnMessage = OnMessage,
                OnClose = OnDisconnect
            };
            state = State.None;
        }

        internal Task Connect() {
            if (state == State.Open) {
                return Task.FromResult<object>(null);
            }
            if (state == State.Connecting) {
                return connectTask;
            }
            connectTask = ConnectInternal();
            return connectTask;
        }

        internal async Task ConnectInternal() {
            state = State.Connecting;
            try {
                // string newServer = server.Replace("https://", "wss://").Replace("http://", "ws://");
                // int i = RequestI;
                // string url = GetFastOpenUrl(newServer, appId, gameVersion, userId, sessionToken);
                // await ws.Connect(url, SUB_PROTOCOL);
                LCLogger.Debug($"ConnectInternal {server}");
                string[] parts = server.Split(':');
                await kcp.Connect(parts[0], ushort.Parse(parts[1]));
                messageQueue = new Queue<CommandWrapper>();
                isMessageQueueRunning = true;
                // 启动心跳
                heartBeat.Start();
                state = State.Open;
                var request = NewRequest();
                request.SessionOpen = new SessionOpenRequest
                {
                    AppId = appId,
                    PeerId = userId,
                    GameVersion = gameVersion,
                    SessionToken = sessionToken,
                    ProtocolVersion = Config.ProtocolVersion,
                    SdkVersion = Config.SDKVersion,
                };
                var resp = await SendRequest(CommandType.Session, OpType.Open, request);
                if (resp.Cmd != CommandType.Session && resp.Op != OpType.Opened)
                {
                    throw new Exception("session not opened");
                }
            } catch (Exception e) {
                state = State.Closed;
                throw e;
            }
        }

        internal async Task<ResponseWrapper> SendRequest(CommandType cmd, OpType op, RequestMessage request) {
            TaskCompletionSource<ResponseWrapper> tcs = new TaskCompletionSource<ResponseWrapper>();
            request.I = RequestI;
            requestToResponses.Add(request.I, tcs);
            try {
                await SendCommand(cmd, op, new Body {
                    Request = request
                });
            } catch (Exception e) {
                tcs.TrySetException(e);
            }
            return await tcs.Task;
        }

        internal async Task SendCommand(CommandType cmd, OpType op, Body body) {
            if (LCLogger.LogDelegate != null) {
                LCLogger.Debug($"{userId} => {FormatCommand(cmd, op, body)}");
            }
            Command command;
            if (body == null)
            {
                command = new Command
                {
                    Cmd = cmd,
                    Op = op,
                };
            }
            else
            {
                command = new Command
                {
                    Cmd = cmd,
                    Op = op,
                    Body = body.ToByteString()
                };
            }
            byte[] bytes = command.ToByteArray();
            // Task sendTask = ws.Send(bytes);
            Task sendTask = kcp.Send(bytes);
            if (await Task.WhenAny(sendTask, Task.Delay(SEND_TIMEOUT)) == sendTask) {
                await sendTask;
            } else {
                throw new TimeoutException("Send request");
            }
        }

        internal void Disconnect() {
            state = State.Closed;
            heartBeat.Stop();
            // _ = ws.Close();
        }

        private void OnMessage(byte[] bytes, int length) {
            try {
                Command command = Command.Parser.ParseFrom(bytes, 0, length);

                CommandType cmd = command.Cmd;
                OpType op = command.Op;
                Body body = Body.Parser.ParseFrom(command.Body);
                if (LCLogger.LogDelegate != null) {
                    LCLogger.Debug($"{userId} <= {FormatCommand(cmd, op, body)}");
                }
                if (command.Cmd == CommandType.Echo) {
                    // 心跳应答
                    heartBeat.Pong();
                } else if (command.Cmd == CommandType.Conn && command.Op == OpType.Closed) {
                    kcp.Stop();
                    OnDisconnect();
                } else {
                    if (isMessageQueueRunning) {
                        HandleCommand(cmd, op, body);
                    } else {
                        messageQueue.Enqueue(new CommandWrapper {
                            Cmd = cmd,
                            Op = op,
                            Body = body
                        });
                    }
                }
            } catch (Exception e) {
                LCLogger.Error(e);
            }
        }

        void HandleCommand(CommandType cmd, OpType op, Body body) {
            if (body.Response != null) {
                ResponseMessage res = body.Response;

                if (requestToResponses.TryGetValue(res.I, out var tcs)) {
                    if (res.ErrorInfo != null) {
                        var errorInfo = res.ErrorInfo;
                        tcs.SetException(new PlayException(errorInfo.ReasonCode, errorInfo.Detail));
                    } else {
                        tcs.SetResult(new ResponseWrapper {
                            Cmd = cmd,
                            Op = op,
                            Response = res
                        });
                    }
                    requestToResponses.Remove(res.I);
                }
            } else {
                HandleNotification(cmd, op, body);
            }
        }

        private void OnDisconnect() {
            Disconnect();
            OnDisconnected?.Invoke();
        }

        internal async Task Close() {
            try {
                heartBeat.Stop();
                // await ws.Close();
                await SendCommand(CommandType.Conn, OpType.Close, null);
                await Task.Delay(1000);
                kcp.Stop();
            } catch (Exception e) {
                LCLogger.Error(e.Message);
            }
        }

        private static string FormatCommand(CommandType cmd, OpType op, Body body) {
            StringBuilder sb = new StringBuilder($"{cmd}");
            if (op != OpType.None) {
                sb.Append($"/{op}");
            }
            sb.Append($"\n{body}");
            return sb.ToString();
        }

        volatile int requestI = 1;
        readonly object requestILock = new object();

        protected int RequestI {
            get {
                lock (requestILock) {
                    return requestI++;
                }
            }
        }

        protected RequestMessage NewRequest() {
            var request = new RequestMessage {
                I = RequestI
            };
            return request;
        }

        internal void PauseMessageQueue() {
            isMessageQueueRunning = false;
        }

        internal void ResumeMessageQueue() {
            while (messageQueue.Count > 0) {
                CommandWrapper command = messageQueue.Dequeue();
                HandleCommand(command.Cmd, command.Op, command.Body);
                LCLogger.Debug("Delay Handle {0} <= {1}/{2}: {3}", userId, command.Cmd, command.Op, command.Body);
            }
            isMessageQueueRunning = true;
        }

        protected abstract string GetFastOpenUrl(string server, string appId, string gameVersion, string userId, string sessionToken);

        protected abstract int KeepAliveInterval { get; }

        protected abstract void HandleNotification(CommandType cmd, OpType op, Body body);
    }
}