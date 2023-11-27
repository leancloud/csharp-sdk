using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using LC.Google.Protobuf;
using LeanCloud.Realtime.Internal.Router;
using LeanCloud.Realtime.Internal.WebSocket;
using LeanCloud.Realtime.Internal.Protocol;

namespace LeanCloud.Realtime.Internal.Connection {
    /// <summary>
    /// Connection layer
    /// </summary>
    public class LCConnection {
        // 请求/应答比对，即 I 相等
        class RequestAndResponseComparer : IEqualityComparer<GenericCommand> {
            public bool Equals(GenericCommand x, GenericCommand y) {
                return true;
            }

            public int GetHashCode(GenericCommand obj) {
                return obj.I;
            }
        }

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

        private const int MAX_RECONNECT_TIMES = 10;

        private const int RECONNECT_INTERVAL = 10000;

        private const string SUB_PROTOCOL = "lc.protobuf2.3";

        internal string id;

        private readonly Dictionary<GenericCommand, TaskCompletionSource<GenericCommand>> requestToResponses;

        private int requestI = 1;

        private LCRTMRouter router;

        private LCHeartBeat heartBeat;

        private LCWebSocketClient ws;

        private State state;
        // 可以在 connecting 状态时拿到 Task，并在重连成功后继续操作
        private Task connectTask;

        // 共享这条连接的 IM Client
        private readonly Dictionary<string, LCIMClient> idToClients;
        // 默认 Client id
        private string defaultClientId;

        internal LCConnection(string id) {
            this.id = id;
            requestToResponses = new Dictionary<GenericCommand, TaskCompletionSource<GenericCommand>>(new RequestAndResponseComparer());

            heartBeat = new LCRTMHeartBeat(this, OnDisconnect);
            router = new LCRTMRouter();
            ws = new LCWebSocketClient {
                OnMessage = OnMessage,
                OnClose = OnDisconnect
            };
            idToClients = new Dictionary<string, LCIMClient>();
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
                LCRTMServer rtmServer = await router.GetServer();
                try {
                    LCLogger.Debug($"Primary Server");
                    await ws.Connect(rtmServer.Primary, SUB_PROTOCOL);
                } catch (Exception e) {
                    LCLogger.Error(e);
                    LCLogger.Debug($"Secondary Server");
                    await ws.Connect(rtmServer.Secondary, SUB_PROTOCOL);
                }
                // 启动心跳
                heartBeat.Start();
                state = State.Open;
            } catch (Exception e) {
                state = State.Closed;
                throw e;
            }
        }

        internal async Task<GenericCommand> SendRequest(GenericCommand request) {
            if (IsIdempotentCommand(request)) {
                GenericCommand sendingReq = requestToResponses.Keys.FirstOrDefault(item => {
                    // TRICK 除了 I 其他字段相等
                    request.I = item.I;
                    return Equals(request, item);
                });
                if (sendingReq != null) {
                    LCLogger.Warn("duplicated request");
                    if (requestToResponses.TryGetValue(sendingReq, out TaskCompletionSource<GenericCommand> waitingTcs)) {
                        return await waitingTcs.Task;
                    }
                    if (LCLogger.LogDelegate != null) {
                        LCLogger.Error($"error request: {request}");
                    }
                }
            }

            TaskCompletionSource<GenericCommand> tcs = new TaskCompletionSource<GenericCommand>();
            request.I = requestI++;
            requestToResponses.Add(request, tcs);
            try {
                await SendCommand(request);
            } catch (Exception e) {
                tcs.TrySetException(e);
            }
            return await tcs.Task;
        }

        internal async Task SendCommand(GenericCommand command) {
            if (LCLogger.LogDelegate != null) {
                LCLogger.Debug($"{id} => {FormatCommand(command)}");
            }
            byte[] bytes = command.ToByteArray();
            await ws.Send(bytes);
        }

        private void Disconnect() {
            defaultClientId = null;
            state = State.Closed;
            heartBeat.Stop();
            _ = ws.Close();
            foreach (LCIMClient client in idToClients.Values) {
                client.HandleDisconnected();
            }
        }

        private void OnMessage(byte[] bytes, int length) {
            try {
                GenericCommand command = GenericCommand.Parser.ParseFrom(bytes, 0, length);
                if (LCLogger.LogDelegate != null) {
                    LCLogger.Debug($"{id} <= {FormatCommand(command)}");
                }
                if (command.HasI) {
                    // 应答
                    int requestIndex = command.I;
                    if (requestToResponses.TryGetValue(command, out TaskCompletionSource<GenericCommand> tcs)) {
                        requestToResponses.Remove(command);
                        if (command.ErrorMessage != null) {
                            // 错误
                            ErrorCommand error = command.ErrorMessage;
                            int code = error.Code;
                            string reason = error.Reason;
                            int appCode = error.AppCode;
                            string appMsg = error.AppMsg;
                            // 包装成异常抛出
                            LCIMException exception = new LCIMException(code, reason, appCode, appMsg);
                            tcs.TrySetException(exception);
                        } else {
                            tcs.TrySetResult(command);
                        }
                    } else {
                        if (LCLogger.LogDelegate != null) {
                            LCLogger.Error($"No request for {requestIndex}");
                        }
                    }
                } else {
                    if (command.Cmd == CommandType.Echo) {
                        // 心跳应答
                        heartBeat.Pong();
                    } else if (command.Cmd == CommandType.Goaway) {
                        // 针对连接的消息
                        Reset();
                    } else {
                        // 通知
                        string peerId = command.HasPeerId ? command.PeerId : defaultClientId;
                        if (idToClients.TryGetValue(peerId, out LCIMClient client)) {
                            // 通知具体客户端
                            client.HandleNotification(command);
                        }
                    }
                }
            } catch (Exception e) {
                LCLogger.Error(e);
            }
        }

        private void OnDisconnect() {
            Disconnect();
            _ = Reconnect();
        }

        internal void Reset() {
            Disconnect();
            // 重新创建连接组件
            heartBeat = new LCRTMHeartBeat(this, OnDisconnect);
            router = new LCRTMRouter();
            ws = new LCWebSocketClient {
                OnMessage = OnMessage,
                OnClose = OnDisconnect
            };
            _ = Reconnect();
        }

        private async Task Reconnect() {
            while (true) {
                int reconnectCount = 0;
                // 重连策略
                while (reconnectCount < MAX_RECONNECT_TIMES) {
                    try {
                        if (LCLogger.LogDelegate != null) {
                            LCLogger.Debug($"Reconnecting... {reconnectCount}");
                        }
                        await Connect();
                        break;
                    } catch (Exception e) {
                        reconnectCount++;
                        if (LCLogger.LogDelegate != null) {
                            LCLogger.Error(e);
                            LCLogger.Debug($"Reconnect after {RECONNECT_INTERVAL}ms");
                        }
                        await Task.Delay(RECONNECT_INTERVAL);
                    }
                }
                if (reconnectCount < MAX_RECONNECT_TIMES) {
                    // 重连成功
                    LCLogger.Debug("Reconnected");
                    ws.OnMessage = OnMessage;
                    ws.OnClose = OnDisconnect;
                    foreach (LCIMClient client in idToClients.Values) {
                        client.HandleReconnected();
                    }
                    break;
                } else {
                    // 重置 Router，继续尝试重连
                    router = new LCRTMRouter();
                }
            }
        }

        private static string FormatCommand(GenericCommand command) {
            StringBuilder sb = new StringBuilder($"{command.Cmd}");
            if (command.HasOp) {
                sb.Append($"/{command.Op}");
            }
            sb.Append($"\n{command}");
            return sb.ToString();
        }

        internal void Register(LCIMClient client) {
            idToClients[client.Id] = client;
            if (defaultClientId == null) {
                defaultClientId = client.Id;
            }
        }

        internal void UnRegister(LCIMClient client) {
            idToClients.Remove(client.Id);
            if (idToClients.Count == 0) {
                Disconnect();
                LCRealtime.RemoveConnection(this);
            }
        }

        internal void Pause() {
            Disconnect();
        }

        internal void Resume() {
            _ = Reconnect();
        }

        private static bool IsIdempotentCommand(GenericCommand command) {
            return !(
                command.Cmd == CommandType.Direct ||
                (command.Cmd == CommandType.Session && command.Op == OpType.Open) ||
                (command.Cmd == CommandType.Conv &&
                (command.Op == OpType.Start ||
                command.Op == OpType.Update ||
                command.Op == OpType.Members))
            );
        }
    }
}
