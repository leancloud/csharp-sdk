using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LeanCloud.Common;
using LeanCloud.Storage;
using LeanCloud.Storage.Internal.Object;
using LeanCloud.LiveQuery.Internal;

namespace LeanCloud.LiveQuery {
    /// <summary>
    /// LeanCloud LiveQuery.
    /// </summary>
    public class LCLiveQuery {
        /// <summary>
        /// A new LCObject which fulfills the LCQuery you subscribe is created. 
        /// </summary>
        public Action<LCObject> OnCreate;
        /// <summary>
        /// An existing LCObject which fulfills the LCQuery you subscribe is updated.
        /// </summary>
        public Action<LCObject, ReadOnlyCollection<string>> OnUpdate;
        /// <summary>
        /// An existing LCObject which fulfills the LCQuery you subscribe is deleted.
        /// </summary>
        public Action<string> OnDelete;
        /// <summary>
        /// An existing LCObject which doesn't fulfill the LCQuery is updated and now it fulfills the LCQuery.
        /// </summary>
        public Action<LCObject, ReadOnlyCollection<string>> OnEnter;
        /// <summary>
        /// An existing LCObject which fulfills the LCQuery is updated and now it doesn't fulfill the LCQuery.
        /// </summary>
        public Action<LCObject, ReadOnlyCollection<string>> OnLeave;
        /// <summary>
        /// A LCUser logged in successfully.
        /// </summary>
        public Action<LCUser> OnLogin;

        public string Id {
            get; private set;
        }

        public LCQuery Query {
            get; internal set;
        }

        private static LCLiveQueryConnection connection;

        private static Dictionary<string, WeakReference<LCLiveQuery>> liveQueries = new Dictionary<string, WeakReference<LCLiveQuery>>(); 

        internal LCLiveQuery() {

        }

        private static readonly string DeviceId = Guid.NewGuid().ToString();


        public async Task Subscribe() {
            // TODO 判断当前连接情况
            if (connection == null) {
                connection = new LCLiveQueryConnection(DeviceId) {
                    OnReconnected = OnReconnected,
                    OnNotification = OnNotification
                };
                await connection.Connect();
                await Login();
            }
            Dictionary<string, object> queryData = new Dictionary<string, object> {
                { "className", Query.ClassName },
                { "where", Query.Condition.Encode() }
            };
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "query", queryData },
                { "id", DeviceId },
                { "clientTimestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() }
            };
            LCUser user = await LCUser.GetCurrent();
            if (user != null && !string.IsNullOrEmpty(user.SessionToken)) {
                data.Add("sessionToken", user.SessionToken);
            }
            string path = "LiveQuery/subscribe";
            Dictionary<string, object> result = await LCCore.HttpClient.Post<Dictionary<string, object>>(path,
                data: data);
            if (result.TryGetValue("query_id", out object id)) {
                Id = id as string;
                WeakReference<LCLiveQuery> weakRef = new WeakReference<LCLiveQuery>(this);
                liveQueries[Id] = weakRef;
            }
        }

        public async Task Unsubscribe() {
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "id", DeviceId },
                { "query_id", Id }
            };
            string path = "LiveQuery/unsubscribe";
            await LCCore.HttpClient.Post<Dictionary<string, object>>(path,
                data: data);
            // 移除
            liveQueries.Remove(Id);
        }

        private static async Task Login() {
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "cmd", "login" },
                { "appId", LCCore.AppId },
                { "installationId", DeviceId },
                { "clientTs", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                { "service", 1 }
            };
            await connection.SendRequest(data);
        }

        private static async void OnReconnected() {
            await Login();
            Dictionary<string, WeakReference<LCLiveQuery>> oldLiveQueries = liveQueries;
            liveQueries = new Dictionary<string, WeakReference<LCLiveQuery>>();
            foreach (WeakReference<LCLiveQuery> weakRef in oldLiveQueries.Values) {
                if (weakRef.TryGetTarget(out LCLiveQuery liveQuery)) {
                    await liveQuery.Subscribe();
                }
            }
        }

        private static void OnNotification(Dictionary<string, object> notification) {
            if (!notification.TryGetValue("cmd", out object cmd) ||
                !"data".Equals(cmd)) {
                return;
            }
            if (!notification.TryGetValue("msg", out object msg) ||
                !(msg is IEnumerable<object> list)) {
                return;
            }
                
            foreach (object item in list) {
                if (item is Dictionary<string, object> dict) {
                    if (!dict.TryGetValue("op", out object op)) {
                        continue;
                    }
                    switch (op as string) {
                        case "create":
                            OnCreateNotification(dict);
                            break;
                        case "update":
                            OnUpdateNotification(dict);
                            break;
                        case "enter":
                            OnEnterNotification(dict);
                            break;
                        case "leave":
                            OnLeaveNotification(dict);
                            break;
                        case "delete":
                            OnDeleteNotification(dict);
                            break;
                        case "login":
                            OnLoginNotification(dict);
                            break;
                        default:
                            LCLogger.Debug($"Not support: {op}");
                            break;
                    }
                }
            }
        }

        private static void OnCreateNotification(Dictionary<string, object> data) {
            if (TryGetLiveQuery(data, out LCLiveQuery liveQuery) &&
                TryGetObject(data, out LCObject obj)) {
                liveQuery.OnCreate?.Invoke(obj);
            }
        }

        private static void OnUpdateNotification(Dictionary<string, object> data) {
            if (TryGetLiveQuery(data, out LCLiveQuery liveQuery) &&
                TryGetObject(data, out LCObject obj) &&
                TryGetUpdatedKeys(data, out ReadOnlyCollection<string> keys)) {
                liveQuery.OnUpdate?.Invoke(obj, keys);
            }
        }

        private static void OnEnterNotification(Dictionary<string, object> data) {
            if (TryGetLiveQuery(data, out LCLiveQuery liveQuery) &&
                TryGetObject(data, out LCObject obj) &&
                TryGetUpdatedKeys(data, out ReadOnlyCollection<string> keys)) {
                liveQuery.OnEnter?.Invoke(obj, keys);
            }
        }

        private static void OnLeaveNotification(Dictionary<string, object> data) {
            if (TryGetLiveQuery(data, out LCLiveQuery liveQuery) &&
                TryGetObject(data, out LCObject obj) &&
                TryGetUpdatedKeys(data, out ReadOnlyCollection<string> keys)) {
                liveQuery.OnLeave?.Invoke(obj, keys);
            }
        }

        private static void OnDeleteNotification(Dictionary<string, object> data) {
            if (TryGetLiveQuery(data, out LCLiveQuery liveQuery) &&
                TryGetObject(data, out LCObject obj)) {
                liveQuery.OnDelete?.Invoke(obj.ObjectId);
            }
        }

        private static void OnLoginNotification(Dictionary<string, object> data) {
            if (TryGetLiveQuery(data, out LCLiveQuery liveQuery) &&
                data.TryGetValue("object", out object obj) &&
                obj is Dictionary<string, object> dict) {
                LCObjectData objectData = LCObjectData.Decode(dict);
                LCUser user = new LCUser(objectData);
                liveQuery.OnLogin?.Invoke(user);
            }
        }

        private static bool TryGetLiveQuery(Dictionary<string, object> data, out LCLiveQuery liveQuery) {
            if (!data.TryGetValue("query_id", out object i) ||
                !(i is string id)) {
                liveQuery = null;
                return false;
            }

            if (!liveQueries.TryGetValue(id, out WeakReference<LCLiveQuery> weakRef) ||
                !weakRef.TryGetTarget(out LCLiveQuery lq)) {
                liveQuery = null;
                return false;
            }

            liveQuery = lq;
            return true;
        }

        private static bool TryGetObject(Dictionary<string, object> data, out LCObject obj) {
            if (!data.TryGetValue("object", out object o) ||
                !(o is Dictionary<string, object> dict)) {
                obj = null;
                return false;
            }

            LCObjectData objectData = LCObjectData.Decode(dict);
            obj = LCObject.Create(dict["className"] as string);
            obj.Merge(objectData);
            return true;
        }

        private static bool TryGetUpdatedKeys(Dictionary<string, object> data, out ReadOnlyCollection<string> keys) {
            if (!data.TryGetValue("updatedKeys", out object uks) ||
                !(uks is List<object> list)) {
                keys = null;
                return false;
            }

            keys = list.Cast<string>().ToList()
                    .AsReadOnly();
            return true;
        }
    }
}
