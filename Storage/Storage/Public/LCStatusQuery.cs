using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using LC.Newtonsoft.Json;
using LeanCloud.Common;
using LeanCloud.Storage.Internal.Codec;
using LeanCloud.Storage.Internal.Object;

namespace LeanCloud.Storage {
    /// <summary>
    /// LCStatusQuery is the query of LCStatus.
    /// </summary>
    public class LCStatusQuery : LCQuery<LCStatus> {
        /// <summary>
        /// The inboxType for query.
        /// </summary>
        public string InboxType {
            get; set;
        }

        /// <summary>
        /// The id since the query.
        /// </summary>
        public int SinceId {
            get; set;
        }

        /// <summary>
        /// The max id for the query.
        /// </summary>
        public int MaxId {
            get; set;
        }

        /// <summary>
        /// Constructs a LCStatusQuery with inboxType.
        /// </summary>
        /// <param name="inboxType"></param>
        public LCStatusQuery(string inboxType = LCStatus.InboxTypeDefault) : base("_Status") {
            InboxType = inboxType;
            SinceId = 0;
            MaxId = 0;
        }

        /// <summary>
        /// Retrieves a list of LCStatus that satisfy the query from Server.
        /// </summary>
        /// <returns></returns>
        public new async Task<ReadOnlyCollection<LCStatus>> Find() {
            LCUser user = await LCUser.GetCurrent();
            if (user == null) {
                throw new ArgumentNullException("current user");
            }

            Dictionary<string, object> queryParams = new Dictionary<string, object> {
                { LCStatus.OwnerKey, JsonConvert.SerializeObject(LCEncoder.Encode(user)) },
                { LCStatus.InboxTypeKey, InboxType },
                { "where", BuildWhere() },
                { "sinceId", SinceId },
                { "maxId", MaxId },
                { "limit", Condition.Limit }
            };
            Dictionary<string, object> response = await LCCore.HttpClient.Get<Dictionary<string, object>>("subscribe/statuses",
                queryParams: queryParams);
            List<object> results = response["results"] as List<object>;
            List<LCStatus> statuses = new List<LCStatus>();
            foreach (object item in results) {
                LCObjectData objectData = LCObjectData.Decode(item as IDictionary);
                LCStatus status = new LCStatus();
                status.Merge(objectData);
                status.MessageId = (int)objectData.CustomPropertyDict[LCStatus.MessageIdKey];
                status.Data = objectData.CustomPropertyDict;
                status.InboxType = objectData.CustomPropertyDict[LCStatus.InboxTypeKey] as string;
                statuses.Add(status);
            }

            return statuses.AsReadOnly();
        }
    }
}
