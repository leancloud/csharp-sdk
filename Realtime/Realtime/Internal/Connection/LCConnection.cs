using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;
using LeanCloud.Realtime.Internal.Router;
using LeanCloud.Realtime.Internal.WebSocket;
using LeanCloud.Realtime.Internal.Protocol;

namespace LeanCloud.Realtime.Internal.Connection {
    /// <summary>
    /// 连接层，只与数据协议相关
    /// </summary>
    public class LCConnection {
        /// <summary>
        /// 连接状态
        /// </summary>
        enum State {
            /// <summary>
            /// 初始状态
            /// </summary>
            None,
            /// <summary>
            /// 连接中
            /// </summary>
            Connecting,
            /// <summary>
            /// 连接成功
            /// </summary>
            Open,
            /// <summary>
            /// 关闭的
            /// </summary>
            Closed,
        }

        /// <summary>
        /// 发送超时
        /// </summary>
        private const int SEND_TIMEOUT = 10000;

        /// <summary>
        /// 最大重连次数，超过后重置 Router 缓存后再次尝试重连
        /// </summary>
        private const int MAX_RECONNECT_TIMES = 10;

        /// <summary>
        /// 重连间隔
        /// </summary>
        private const int RECONNECT_INTERVAL = 10000;

        /// <summary>
        /// 子协议
        /// </summary>
        private const string SUB_PROTOCOL = "lc.protobuf2.3";

        internal string id;

        /// <summary>
        /// 请求回调缓存
        /// </summary>
        private readonly Dictionary<int, TaskCompletionSource<GenericCommand>> responses;

        private int requestI = 1;

        private LCRTMRouter router;

        private LCHeartBeat heartBeat;

        private LCWebSocketClient ws;

        private State state;
        private Task connectTask;

        private readonly Dictionary<string, LCIMClient> idToClients;

        internal LCConnection(string id) {
            this.id = id;
            responses = new Dictionary<int, TaskCompletionSource<GenericCommand>>();
            heartBeat = new LCHeartBeat(this, OnPingTimeout);
            router = new LCRTMRouter();
            ws = new LCWebSocketClient {
                OnMessage = OnClientMessage,
                OnClose = OnClientDisconnect
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
                throw e;
            }
        }

        /// <summary>
        /// 重置连接
        /// </summary>
        /// <returns></returns>
        internal async Task Reset() {
            state = State.Closed;
            heartBeat?.Stop();
            // 关闭就连接
            await ws.Close();
            // 重新创建连接组件
            heartBeat = new LCHeartBeat(this, OnPingTimeout);
            router = new LCRTMRouter();
            ws = new LCWebSocketClient {
                OnMessage = OnClientMessage,
                OnClose = OnClientDisconnect
            };
            await Reconnect();
        }   

        /// <summary>
        /// 发送请求，会在收到应答后返回
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        internal async Task<GenericCommand> SendRequest(GenericCommand request) {
            TaskCompletionSource<GenericCommand> tcs = new TaskCompletionSource<GenericCommand>();
            request.I = requestI++;
            responses.Add(request.I, tcs);
            try {
                await SendCommand(request);
            } catch (Exception e) {
                tcs.TrySetException(e);
            }
            return await tcs.Task;
        }

        /// <summary>
        /// 发送命令
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        internal async Task SendCommand(GenericCommand command) {
            LCLogger.Debug($"{id} => {FormatCommand(command)}");
            byte[] bytes = command.ToByteArray();
            Task sendTask = ws.Send(bytes);
            if (await Task.WhenAny(sendTask, Task.Delay(SEND_TIMEOUT)) == sendTask) {
                await sendTask;
            } else {
                throw new TimeoutException("Send request");
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <returns></returns>
        internal async Task Close() {
            LCRealtime.RemoveConnection(this);
            heartBeat.Stop();
            await ws.Close();
        }

        private void OnClientMessage(byte[] bytes) {
            try {
                GenericCommand command = GenericCommand.Parser.ParseFrom(bytes);
                LCLogger.Debug($"{id} <= {FormatCommand(command)}");
                if (command.HasI) {
                    // 应答
                    int requestIndex = command.I;
                    if (responses.TryGetValue(requestIndex, out TaskCompletionSource<GenericCommand> tcs)) {
                        if (command.HasErrorMessage) {
                            // 错误
                            ErrorCommand error = command.ErrorMessage;
                            int code = error.Code;
                            string detail = error.Detail;
                            // 包装成异常抛出
                            LCException exception = new LCException(code, detail);
                            tcs.TrySetException(exception);
                        } else {
                            tcs.TrySetResult(command);
                        }
                        responses.Remove(requestIndex);
                    } else {
                        LCLogger.Error($"No request for {requestIndex}");
                    }
                } else {
                    if (command.Cmd == CommandType.Echo) {
                        heartBeat.Pong();
                    } else if (command.Cmd == CommandType.Goaway) {
                        _ = Reset();
                    } else {
                        // 通知
                        if (idToClients.TryGetValue(command.PeerId, out LCIMClient client)) {
                            // 通知具体客户端
                            client.HandleNotification(command);
                        }
                    }
                }
            } catch (Exception e) {
                LCLogger.Error(e);
            }
        }

        private void OnClientDisconnect() {
            state = State.Closed;
            heartBeat.Stop();
            foreach (LCIMClient client in idToClients.Values) {
                client.HandleDisconnected();
            }
            // 重连
            _ = Reconnect();
        }

        private void OnPingTimeout() {
            state = State.Closed;
            _ = ws.Close();
            foreach (LCIMClient client in idToClients.Values) {
                client.HandleDisconnected();
            }
            // 重连
            _ = Reconnect();
        }

        private async Task Reconnect() {
            while (true) {
                int reconnectCount = 0;
                // 重连策略
                while (reconnectCount < MAX_RECONNECT_TIMES) {
                    try {
                        LCLogger.Debug($"Reconnecting... {reconnectCount}");
                        await Connect();
                        break;
                    } catch (Exception e) {
                        reconnectCount++;
                        LCLogger.Error(e);
                        LCLogger.Debug($"Reconnect after {RECONNECT_INTERVAL}ms");
                        await Task.Delay(RECONNECT_INTERVAL);
                    }
                }
                if (reconnectCount < MAX_RECONNECT_TIMES) {
                    // 重连成功
                    LCLogger.Debug("Reconnected");
                    ws.OnMessage = OnClientMessage;
                    ws.OnClose = OnClientDisconnect;
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
        }

        internal void UnRegister(LCIMClient client) {
            idToClients.Remove(client.Id);
            if (idToClients.Count == 0) {
                _ = Close();
            }
        }

        /// <summary>
        /// 暂停连接
        /// </summary>
        internal void Pause() {

        }

        /// <summary>
        ///  恢复连接
        /// </summary>
        internal void Resume() {

        }
    }
}
