using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net.WebSockets;
using System.Collections.Generic;
using LeanCloud.Play.Protocol;
using LC.Google.Protobuf;

namespace LeanCloud.Play {
    internal abstract class Connection {
        enum State {
            Init,
            Connecting,
            Connected,
            Disconnected,
            Closed,
        }

        const int RECV_BUFFER_SIZE = 1024;

        protected ClientWebSocket ws;
        readonly Dictionary<int, TaskCompletionSource<ResponseWrapper>> responses;

        internal Action<CommandType, OpType, Body> OnMessage;
        internal Action<int, string> OnClose;
        
        string userId;

        bool isMessageQueueRunning;
        Queue<CommandWrapper> messageQueue;

        internal Connection() {
            responses = new Dictionary<int, TaskCompletionSource<ResponseWrapper>>();
        }

        internal bool IsOpen {
            get {
                return ws != null && ws.State == WebSocketState.Open;
            }
        }

        internal Task<ResponseWrapper> Connect(string appId, string server, string gameVersion, string userId, string sessionToken) {
            this.userId = userId;
            TaskCompletionSource<ResponseWrapper> tcs = new TaskCompletionSource<ResponseWrapper>();
            ws = new ClientWebSocket();
            ws.Options.AddSubProtocol("protobuf.1");
            ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            string newServer = server.Replace("https://", "wss://").Replace("http://", "ws://");
            int i = RequestI;
            string url = GetFastOpenUrl(newServer, appId, gameVersion, userId, sessionToken);
            url = $"{url}&i={i}";
            LCLogger.Debug($"Connect url: {url}");
            responses.Add(i, tcs);
            ws.ConnectAsync(new Uri(url), default).ContinueWith(t => {
                if (t.IsFaulted) {
                    throw t.Exception.InnerException;
                }
                isMessageQueueRunning = true;
                messageQueue = new Queue<CommandWrapper>();
                _ = StartReceive();
            }, TaskScheduler.FromCurrentSynchronizationContext());
            return tcs.Task;
        }

        protected Task<ResponseWrapper> SendRequest(CommandType cmd, OpType op, RequestMessage request) {
            var tcs = new TaskCompletionSource<ResponseWrapper>();
            responses.Add(request.I, tcs);
            _ = Send(cmd, op, new Body {
                Request = request
            });
            return tcs.Task;
        }

        protected void SendDirectCommand(DirectCommand directCommand) {
            _ = Send(CommandType.Direct, OpType.None, new Body {
                Direct = directCommand
            });
        }

        protected async Task Send(CommandType cmd, OpType op, Body body) {
            if (!IsOpen) {
                throw new Exception("WebSocket is not open when send data");
            }
            LCLogger.Debug("{0} => {1}/{2}: {3}", userId, cmd, op, body.ToString());
            var command = new Command {
                Cmd = cmd,
                Op = op,
                Body = body.ToByteString()
            };
            ArraySegment<byte> bytes = new ArraySegment<byte>(command.ToByteArray());
            try {
                await ws.SendAsync(bytes, WebSocketMessageType.Binary, true, default);
            } catch (InvalidOperationException e) {
                OnClose?.Invoke(-2, e.Message);
                _ = Close();
            }
        }   

        protected async Task StartReceive() {
            byte[] buffer = new byte[RECV_BUFFER_SIZE];
            try {
                while (ws.State == WebSocketState.Open) {
                    byte[] data = new byte[0];
                    WebSocketReceiveResult result;
                    do {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Close) {
                            OnClose?.Invoke((int)result.CloseStatus, result.CloseStatusDescription);
                            return;
                        }
                        data = await MergeDataAsync(data, buffer, result.Count);
                    } while (!result.EndOfMessage);
                    try {
                        Command command = Command.Parser.ParseFrom(data);
                        CommandType cmd = command.Cmd;
                        OpType op = command.Op;
                        Body body = Body.Parser.ParseFrom(command.Body);
                        LCLogger.Debug("{0} <= {1}/{2}: {3}", userId, cmd, op, body);
                        if (isMessageQueueRunning) {
                            HandleCommand(cmd, op, body);
                        } else {
                            messageQueue.Enqueue(new CommandWrapper {
                                Cmd = cmd,
                                Op = op,
                                Body = body
                            });
                        }
                    } catch (Exception e) {
                        LCLogger.Error(e.Message);
                        LCLogger.Error(e.StackTrace);
                        throw e;
                    }
                }
            } catch (Exception e) {
                OnClose?.Invoke(-1, e.Message);
            }
        }

        static async Task<byte[]> MergeDataAsync(byte[] oldData, byte[] newData, int newDataLength) {
            return await Task.Run(() => {
                var data = new byte[oldData.Length + newDataLength];
                Array.Copy(oldData, data, oldData.Length);
                Array.Copy(newData, 0, data, oldData.Length, newDataLength);
                return data;
            });
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

        protected Task OpenSession(string appId, string userId, string gameVersion) {
            var request = NewRequest();
            request.SessionOpen = new SessionOpenRequest {
                AppId = appId,
                PeerId = userId,
                SdkVersion = Config.SDKVersion,
                GameVersion = gameVersion
            };
            return SendRequest(CommandType.Session, OpType.Open, request);
        }

        void HandleCommand(CommandType cmd, OpType op, Body body) {
            if (body.Response != null) {
                var res = body.Response;
                
                if (responses.TryGetValue(res.I, out var tcs)) {
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
                    responses.Remove(res.I);
                }
            } else {
                HandleNotification(cmd, op, body);
            }
        }

        protected abstract string GetFastOpenUrl(string server, string appId, string gameVersion, string userId, string sessionToken);

        protected abstract int GetPingDuration();

        protected abstract void HandleNotification(CommandType cmd, OpType op, Body body);

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

        protected void HandleErrorMsg(Body body) {
            
        }

        internal async Task Close() {
            try {
                if (IsOpen) {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "1", CancellationToken.None);
                }
            } catch (Exception e) {
                LCLogger.Error(e.Message);
            }
        }

        internal void Disconnect() {
            _ = Close();
            OnClose?.Invoke(0, string.Empty);
        }
    }
}
