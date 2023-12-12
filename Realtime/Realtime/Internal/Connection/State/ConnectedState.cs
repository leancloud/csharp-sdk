using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LeanCloud.Realtime.Internal.Protocol;
using LC.Google.Protobuf;

namespace LeanCloud.Realtime.Internal.Connection.State {
    // 请求/应答比对，即 I 相等
    class RequestAndResponseComparer : IEqualityComparer<GenericCommand> {
        public bool Equals(GenericCommand x, GenericCommand y) {
            return true;
        }

        public int GetHashCode(GenericCommand obj) {
            return obj.I;
        }
    }

    public class ConnectedState : BaseState {
        internal Dictionary<GenericCommand, TaskCompletionSource<GenericCommand>> requestToResponses;

        internal LCRTMHeartBeat heartBeat;

        internal int requestI = 1;

        public ConnectedState(LCConnection connection) : base(connection) {
        }

        #region State Event

        public override void Enter() {
            requestToResponses = new Dictionary<GenericCommand, TaskCompletionSource<GenericCommand>>(new RequestAndResponseComparer());
            // 设置回调
            connection.ws.OnMessage = OnMessage;
            connection.ws.OnClose = OnDisconnect;
            // 启动心跳
            heartBeat = new LCRTMHeartBeat(connection);
            heartBeat.Start(OnDisconnect);
        }

        public override Task Connect() {
            return Task.FromResult(true);
        }

        public override async Task<GenericCommand> SendRequest(GenericCommand request) {
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
                    LCLogger.Error($"error request: {request}");
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

        public override async Task SendCommand(GenericCommand command) {
            LCLogger.Debug($"{connection.id} => {FormatCommand(command)}");
            byte[] bytes = command.ToByteArray();
            await connection.ws.Send(bytes);
        }

        public override void Pause() {
            connection.TranslateTo(connection.pausedState);

            CloseConnection();

            connection.HandleDisconnected();
        }

        public override void Close() {
            connection.TranslateTo(connection.initState);

            CloseConnection();
        }

        #endregion

        #region WebSocket Event

        private void OnMessage(byte[] bytes, int length) {
            try {
                GenericCommand command = GenericCommand.Parser.ParseFrom(bytes, 0, length);
                LCLogger.Debug($"{connection.id} <= {FormatCommand(command)}");
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
                        LCLogger.Error($"No request for {requestIndex}");
                    }
                } else {
                    if (command.Cmd == CommandType.Echo) {
                        // 心跳应答
                        heartBeat.Pong();
                    } else if (command.Cmd == CommandType.Goaway) {
                        // 针对连接的消息
                        // 重置 Router 并断线重连
                        connection.router = new Router.LCRTMRouter();
                        connection.TranslateTo(connection.reconnectState);
                    } else {
                        // 通知
                        string peerId = command.HasPeerId ? command.PeerId : connection.defaultClientId;
                        if (connection.idToClients.TryGetValue(peerId, out LCIMClient client)) {
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
            // 断开重连
            connection.TranslateTo(connection.reconnectState);

            CloseConnection();

            connection.HandleDisconnected();
        }

        #endregion

        private void CloseConnection() {
            _ = connection.ws.Close();

            // 取消掉等待中的请求
            foreach (KeyValuePair<GenericCommand, TaskCompletionSource<GenericCommand>> kv in requestToResponses) {
                kv.Value.TrySetCanceled();
            }
            requestToResponses.Clear();
        }

        private static string FormatCommand(GenericCommand command) {
            StringBuilder sb = new StringBuilder($"{command.Cmd}");
            if (command.HasOp) {
                sb.Append($"/{command.Op}");
            }
            sb.Append($"\n{command}");
            return sb.ToString();
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
