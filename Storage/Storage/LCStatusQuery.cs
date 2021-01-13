using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Newtonsoft.Json;
using LeanCloud.Storage.Internal.Codec;
using LeanCloud.Storage.Internal.Object;

namespace LeanCloud.Storage {
    public class LCStatusQuery : LCQuery<LCStatus> {
        public string InboxType {
            get; set;
        }

        public int SinceId {
            get; set;
        }

        public int MaxId {
            get; set;
        }

        public LCStatusQuery(string inboxType = LCStatus.InboxTypeDefault) : base("_Status") {
            InboxType = inboxType;
            SinceId = 0;
            MaxId = 0;
        }

        public async Task<ReadOnlyCollection<LCStatus>> Find() {
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
            Dictionary<string, object> response = await LCApplication.HttpClient.Get<Dictionary<string, object>>("subscribe/statuses",
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
