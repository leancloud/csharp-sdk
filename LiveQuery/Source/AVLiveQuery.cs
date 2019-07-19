using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Storage.Internal;
using LeanCloud.Realtime;
using LeanCloud.Realtime.Internal;
using System.Linq;
using System.Linq.Expressions;

namespace LeanCloud.LiveQuery
{
    /// <summary>
    /// AVLiveQuery 类
    /// </summary>
    public static class AVLiveQuery
    {
        /// <summary>
        /// LiveQuery 传输数据的 AVRealtime 实例
        /// </summary>
        public static AVRealtime Channel {
            get; set;
        }

        internal static long ClientTs {
            get; set;
        }

        internal static bool Inited {
            get; set;
        }

        internal static string InstallationId { 
            get; set; 
        }
    }

    /// <summary>
    /// AVLiveQuery 对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AVLiveQuery<T> where T : AVObject
    {
        internal static Dictionary<string, WeakReference<AVLiveQuery<T>>> liveQueryDict = new Dictionary<string, WeakReference<AVLiveQuery<T>>>();


        /// <summary>
        /// 当前 AVLiveQuery 对象的 Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 根据 AVQuery 创建 AVLiveQuery 对象
        /// </summary>
        /// <param name="query"></param>
        public AVLiveQuery(AVQuery<T> query) {
            this.Query = query;
        }
        /// <summary>
        /// AVLiveQuery 对应的 AVQuery 对象
        /// </summary>
        public AVQuery<T> Query { get; set; }

        /// <summary>
        /// 数据推送的触发的事件通知
        /// </summary>
        public event EventHandler<AVLiveQueryEventArgs<T>> OnLiveQueryReceived;

        /// <summary>
        /// 推送抵达时触发事件通知
        /// </summary>
        /// <param name="scope">产生这条推送的原因。
        /// <remarks>
        /// create:符合查询条件的对象创建;
        /// update:符合查询条件的对象属性修改。
        /// enter:对象修改事件，从不符合查询条件变成符合。
        /// leave:对象修改时间，从符合查询条件变成不符合。
        /// delete:对象删除
        /// login:只对 _User 对象有效，表示用户登录。
        /// </remarks>
        /// </param>
        /// <param name="onRecevived"></param>
        public void On(string scope, Action<T> onRecevived)
        {
            this.OnLiveQueryReceived += (sender, e) =>
            {
                if (e.Scope == scope)
                {
                    onRecevived.Invoke(e.Payload);
                }
            };
        }

        /// <summary>
        /// 订阅操作
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<AVLiveQuery<T>> SubscribeAsync(CancellationToken cancellationToken = default(CancellationToken)) {
            if (Query == null) {
                throw new Exception("Query can not be null when subcribe.");
            }
            if (!AVLiveQuery.Inited) {
                await Login();
                AVLiveQuery.Channel.OnReconnected += OnChannelReconnected;
                AVLiveQuery.Channel.NoticeReceived += OnChannelNoticeReceived;
                AVLiveQuery.Inited = true;
            }
            await InternalSubscribe();
            var liveQueryRef = new WeakReference<AVLiveQuery<T>>(this);
            liveQueryDict.Add(Id, liveQueryRef);
            return this;
        }

        static async void OnChannelReconnected(object sender, AVIMReconnectedEventArgs e) {
            await Login();
            lock (liveQueryDict) { 
                foreach (var kv in liveQueryDict) { 
                    if (kv.Value.TryGetTarget(out var liveQuery)) {
                        liveQuery.InternalSubscribe().ContinueWith(_ => { });
                    }
                }
            }
        }

        static async Task Login() {
            var installation = await AVPlugins.Instance.InstallationIdController.GetAsync();
            AVLiveQuery.InstallationId = installation.ToString();
            AVLiveQuery.Channel.ToggleNotification(true);
            await AVLiveQuery.Channel.OpenAsync();
            AVLiveQuery.ClientTs = (long) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            var liveQueryLogInCmd = new AVIMCommand().Command("login")
                     .Argument("installationId", AVLiveQuery.InstallationId)
                     .Argument("clientTs", AVLiveQuery.ClientTs)
                     .Argument("service", 1).AppId(AVClient.CurrentConfiguration.ApplicationId);
            // open the session for LiveQuery.
            try {
                await AVLiveQuery.Channel.AVIMCommandRunner.RunCommandAsync(liveQueryLogInCmd);
            } catch (Exception e) {
                AVRealtime.PrintLog(e.Message);
            }
        }

        static void OnChannelNoticeReceived(object sender, AVIMNotice e) {
            if (e.CommandName == "data") {
                var ids = AVDecoder.Instance.DecodeList<string>(e.RawData["ids"]);
                if (e.RawData["msg"] is IEnumerable<object> msg) {
                    var receivedPayloads = from item in msg
                                           select item as Dictionary<string, object>;
                    if (receivedPayloads != null) {
                        foreach (var payload in receivedPayloads) {
                            var liveQueryId = payload["query_id"] as string;
                            if (liveQueryDict.TryGetValue(liveQueryId, out var liveQueryRef) &&
                                liveQueryRef.TryGetTarget(out var liveQuery)) {
                                var scope = payload["op"] as string;
                                var objectPayload = payload["object"] as Dictionary<string, object>;
                                string[] keys = null;
                                if (payload.TryGetValue("updatedKeys", out object updatedKeys)) {
                                    // enter, leave, update
                                    keys = (updatedKeys as List<object>).Select(x => x.ToString()).ToArray();
                                }
                                liveQuery.Emit(scope, objectPayload, keys);
                            }
                        }
                    }
                }
            }
        }

        async Task InternalSubscribe() {
            var queryMap = new Dictionary<string, object> {
                { "where", Query.Condition},
                { "className", Query.GetClassName()}
            };

            Dictionary<string, object> data = new Dictionary<string, object> {
                { "query", queryMap },
                { "id", AVLiveQuery.InstallationId },
                { "clientTimestamp", AVLiveQuery.ClientTs }
            };
            string sessionToken = AVUser.CurrentUser != null ? AVUser.CurrentUser.SessionToken : string.Empty;
            if (!string.IsNullOrEmpty(sessionToken)) {
                data.Add("sessionToken", sessionToken);
            }
            var command = new AVCommand("LiveQuery/subscribe",
                                        "POST",
                                        sessionToken,
                                        data: data);
            var res = await AVPlugins.Instance.CommandRunner.RunCommandAsync(command);
            Id = res.Item2["query_id"] as string;
        }

        /// <summary>
        /// 取消对当前 LiveQuery 对象的订阅
        /// </summary>
        /// <returns></returns>
        public async Task UnsubscribeAsync() {
            Dictionary<string, object> strs = new Dictionary<string, object> {
                { "id", AVLiveQuery.InstallationId },
                { "query_id", Id },
            };
            string sessionToken = AVUser.CurrentUser != null ? AVUser.CurrentUser.SessionToken : string.Empty;
            var command = new AVCommand("LiveQuery/unsubscribe",
                          "POST",
                          sessionToken,
                          data: strs);
            await AVPlugins.Instance.CommandRunner.RunCommandAsync(command);
            lock (liveQueryDict) {
                liveQueryDict.Remove(Id);
            }
        }

        void Emit(string scope, IDictionary<string, object> payloadMap, string[] keys) {
            var objectState = AVObjectCoder.Instance.Decode(payloadMap, AVDecoder.Instance);
            var payloadObject = AVObject.FromState<T>(objectState, Query.GetClassName<T>());
            var args = new AVLiveQueryEventArgs<T> {
                Scope = scope,
                Keys = keys,
                Payload = payloadObject
            };
            OnLiveQueryReceived?.Invoke(this, args);
        }
    }
}
