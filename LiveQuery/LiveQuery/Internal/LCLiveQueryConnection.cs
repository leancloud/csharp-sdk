using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using LC.Newtonsoft.Json;
using LeanCloud.Realtime.Internal.Router;
using LeanCloud.Realtime.Internal.WebSocket;
using LeanCloud.Common;
using LeanCloud.Realtime.Internal.Connection;

namespace LeanCloud.LiveQuery.Internal {
    public class LCLiveQueryConnection {
        private const int SEND_TIMEOUT = 10000;

        /// <summary>
        /// After exceeding this limit, will reset the Router cache and try to reconnect again.
        /// </summary>
        private const int MAX_RECONNECT_TIMES = 10;

        private const int RECONNECT_INTERVAL = 10000;

        private const string SUB_PROTOCOL = "lc.json.3";

 
        internal Action<Dictionary<string, object>> OnNotification;

        internal Action OnDisconnect;

        internal Action OnReconnected;

        internal string id;

        /// <summary>
        /// Request callback cache
        /// </summary>
        private readonly Dictionary<int, TaskCompletionSource<Dictionary<string, object>>> responses;

        private int requestI = 1;

        private LCRTMRouter router;

        private LCLiveQueryHeartBeat heartBeat;

        private LCWebSocketClient client;

        public LCLiveQueryConnection(string id) {
            this.id = id;
            responses = new Dictionary<int, TaskCompletionSource<Dictionary<string, object>>>();
            heartBeat = new LCLiveQueryHeartBeat(this, OnPingTimeout);
            router = new LCRTMRouter();
            client = new LCWebSocketClient {
                OnMessage = OnClientMessage,
                OnClose = OnClientDisconnect
            };
        }

        public async Task Connect() {
            try {
                LCRTMServer rtmServer = await router.GetServer();
                try {
                    LCLogger.Debug($"Primary Server");
                    await client.Connect(rtmServer.Primary, SUB_PROTOCOL);
                } catch (Exception e) {
                    LCLogger.Error(e);
                    LCLogger.Debug($"Secondary Server");
                    await client.Connect(rtmServer.Secondary, SUB_PROTOCOL);
                }
                // 启动心跳
                heartBeat.Start();
            } catch (Exception e) {
                throw e;
            }
        }

        /// <summary>
        /// Resets connection
        /// </summary>
        /// <returns></returns>
        internal async Task Reset() {
            heartBeat?.Stop();
            // 关闭就连接
            await client.Close();
            // 重新创建连接组件
            heartBeat = new LCLiveQueryHeartBeat(this, OnPingTimeout);
            router = new LCRTMRouter();
            client = new LCWebSocketClient {
                OnMessage = OnClientMessage,
                OnClose = OnClientDisconnect
            };
            await Reconnect();
        }

        /// <summary>
        /// Sends the request. It will return after receiving a response.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        internal async Task<Dictionary<string, object>> SendRequest(Dictionary<string, object> request) {
            TaskCompletionSource<Dictionary<string, object>> tcs = new TaskCompletionSource<Dictionary<string, object>>();
            int requestIndex = requestI++;
            request["i"] = requestIndex;
            responses.Add(requestIndex, tcs);
            try {
                string json = JsonConvert.SerializeObject(request);
                await SendText(json);
            } catch (Exception e) {
                tcs.TrySetException(e);
            }
            return await tcs.Task;
        }

        /// <summary>
        /// Sends text message.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal async Task SendText(string text) {
            LCLogger.Debug($"{id} => {text}");
            Task sendTask = client.Send(text);
            if (await Task.WhenAny(sendTask, Task.Delay(SEND_TIMEOUT)) == sendTask) {
                await sendTask;
            } else {
                throw new TimeoutException("Send request time out");
            }
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <returns></returns>
        internal async Task Close() {
            OnNotification = null;
            OnDisconnect = null;
            OnReconnected = null;
            heartBeat.Stop();
            await client.Close();
        }

        private void OnClientMessage(byte[] bytes, int length) {
            try {
                string json = Encoding.UTF8.GetString(bytes, 0, length);
                Dictionary<string, object> msg = JsonConvert.DeserializeObject<Dictionary<string, object>>(json,
                    LCJsonConverter.Default);
                LCLogger.Debug($"{id} <= {json}");
                if (msg.TryGetValue("i", out object i)) {
                    int requestIndex = Convert.ToInt32(i);
                    if (responses.TryGetValue(requestIndex, out TaskCompletionSource<Dictionary<string, object>> tcs)) {
                        if (msg.TryGetValue("error", out object error)) {
                            // 错误
                            if (error is Dictionary<string, object> dict) {
                                int code = Convert.ToInt32(dict["code"]);
                                string detail = dict["detail"] as string;
                                tcs.SetException(new LCException(code, detail));
                            } else {
                                tcs.SetException(new Exception(error as string));
                            }
                        } else {
                            tcs.SetResult(msg);
                        }
                        responses.Remove(requestIndex);
                    } else {
                        LCLogger.Error($"No request for {requestIndex}");
                    }
                } else {
                    if (json == "{}") {
                        heartBeat.Pong();
                    } else {
                        // 通知
                        OnNotification?.Invoke(msg);
                    }
                }
            } catch (Exception e) {
                LCLogger.Error(e);
            }
        }

        private void OnClientDisconnect() {
            heartBeat.Stop();
            OnDisconnect?.Invoke();
            // 重连
            _ = Reconnect();
        }

        private async void OnPingTimeout() {
            await client.Close();
            OnDisconnect?.Invoke();
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
                    client.OnMessage = OnClientMessage;
                    client.OnClose = OnClientDisconnect;
                    OnReconnected?.Invoke();
                    break;
                } else {
                    // 重置 Router，继续尝试重连
                    router = new LCRTMRouter();
                }
            }
        }
    }
}
