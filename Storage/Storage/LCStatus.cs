using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;
using LeanCloud.Storage.Internal.Object;
using Newtonsoft.Json;

namespace LeanCloud.Storage {
    public class LCStatus : LCObject {
        public const string CLASS_NAME = "_Status";

        /// Public, shown on followees' timeline.
        public const string InboxTypeDefault = "default";

        /// Private.
        public const string InboxTypePrivate = "private";

        /// Keys
        public const string SourceKey = "source";
        public const string InboxTypeKey = "inboxType";
        public const string OwnerKey = "owner";
        public const string MessageIdKey = "messageId";

        public int MessageId {
            get; internal set;
        }

        public string InboxType {
            get; internal set;
        }

        private LCQuery query;

        public Dictionary<string, object> Data {
            get; set;
        }

        public LCStatus() : base(CLASS_NAME) {
            InboxType = InboxTypeDefault;
            Data = new Dictionary<string, object>();
        }

        public static async Task<LCStatus> SendToFollowers(LCStatus status) {
            if (status == null) {
                throw new ArgumentNullException(nameof(status));
            }
            LCUser user = await LCUser.GetCurrent();
            if (user == null) {
                throw new ArgumentNullException("current user");
            }

            status.Data[SourceKey] = user;

            LCQuery<LCObject> query = new LCQuery<LCObject>("_Follower")
                .WhereEqualTo("user", user)
                .Select("follower");
            status.query = query;

            status.InboxType = InboxTypeDefault;

            return await status.Send();
        }

        public static async Task<LCStatus> SendPrivately(LCStatus status, string targetId) {
            if (status == null) {
                throw new ArgumentNullException(nameof(status));
            }
            if (string.IsNullOrEmpty(targetId)) {
                throw new ArgumentNullException(nameof(targetId));
            }
            LCUser user = await LCUser.GetCurrent();
            if (user == null) {
                throw new ArgumentNullException("current user");
            }

            status.Data[SourceKey] = user;
            LCQuery<LCObject> query = new LCQuery<LCObject>("_User")
                .WhereEqualTo("objectId", targetId);
            status.query = query;

            status.InboxType = InboxTypePrivate;

            return await status.Send();
        }

        public async Task<LCStatus> Send() {
            LCUser user = await LCUser.GetCurrent();
            if (user == null) {
                throw new ArgumentNullException("current user");
            }

            Dictionary<string, object> formData = new Dictionary<string, object> {
                { InboxTypeKey, InboxType }
            };
            if (Data != null) {
                formData["data"] = LCEncoder.Encode(Data);
            }
            if (query != null) {
                Dictionary<string, object> queryData = new Dictionary<string, object> {
                    { "className", query.ClassName }
                };
                Dictionary<string, object> ps = query.BuildParams();
                if (ps.TryGetValue("where", out object whereObj) &&
                    whereObj is string where) {
                    queryData["where"] = JsonConvert.DeserializeObject(where);
                }
                if (ps.TryGetValue("keys", out object keys)) {
                    queryData["keys"] = keys;
                }
                formData["query"] = queryData;
            }
            Dictionary<string, object> response = await LCApplication.HttpClient.Post<Dictionary<string, object>>("statuses",
                data: formData);
            LCObjectData objectData = LCObjectData.Decode(response);
            Merge(objectData);

            return this;
        }

        public new async Task Delete() {
            LCUser user = await LCUser.GetCurrent();
            if (user == null) {
                throw new ArgumentNullException("current user");
            }

            LCUser source = (Data[SourceKey] ?? this[SourceKey]) as LCUser;
            if (source != null && source.ObjectId == user.ObjectId) {
                await LCApplication.HttpClient.Delete($"statuses/{ObjectId}");
            } else {
                Dictionary<string, object> data = new Dictionary<string, object> {
                    { OwnerKey, JsonConvert.SerializeObject(LCEncoder.Encode(user)) },
                    { InboxTypeKey, InboxType },
                    { MessageIdKey, MessageId }
                };
                await LCApplication.HttpClient.Delete("subscribe/statuses/inbox", queryParams: data);
            }
        }

        public static async Task<LCStatusCount> GetCount(string inboxType) {
            LCUser user = await LCUser.GetCurrent();
            if (user == null) {
                throw new ArgumentNullException("current user");
            }

            Dictionary<string, object> queryParams = new Dictionary<string, object> {
                { OwnerKey, JsonConvert.SerializeObject(LCEncoder.Encode(user)) }
            };
            if (!string.IsNullOrEmpty(inboxType)) {
                queryParams[InboxTypeKey] = inboxType;
            }
            Dictionary<string, object> response = await LCApplication.HttpClient.Get<Dictionary<string, object>>("subscribe/statuses/count",
                queryParams: queryParams);
            LCStatusCount statusCount = new LCStatusCount {
                Total = (int)response["total"],
                Unread = (int)response["unread"]
            };
            return statusCount;
        }

        public static async Task ResetUnreadCount(string inboxType = null) {
            LCUser user = await LCUser.GetCurrent();
            if (user == null) {
                throw new ArgumentNullException("current user");
            }

            Dictionary<string, object> queryParams = new Dictionary<string, object> {
                { OwnerKey, JsonConvert.SerializeObject(LCEncoder.Encode(user)) }
            };
            if (!string.IsNullOrEmpty(inboxType)) {
                queryParams[InboxTypeKey] = inboxType;
            }
            await LCApplication.HttpClient.Post<Dictionary<string, object>>("subscribe/statuses/resetUnreadCount",
                queryParams:queryParams);
        }
    }
}
