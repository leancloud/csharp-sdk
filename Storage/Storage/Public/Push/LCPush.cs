using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Globalization;
using LeanCloud.Storage;
using LeanCloud.Common;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Push {
    public class LCPush {
        public static readonly string IOSEnvironmentDev = "dev";
        public static readonly string IOSEnvironmentProd = "prod";

        public Dictionary<string, object> Data {
            get; set;
        }

        public LCQuery<LCInstallation> Query {
            get; set;
        }

        public string CQL {
            get; set;
        }

        public HashSet<string> Channels {
            get; set;
        }

        public DateTime ExpirationTime {
            get; set;
        }

        public TimeSpan ExpirationInterval {
            get; set;
        }

        public LCObject Notification {
            get; set;
        }

        public HashSet<string> Target {
            get; set;
        }

        public DateTime PushDate {
            get; set;
        }

        public int FlowControl {
            get; set;
        }

        public string IOSEnvironment {
            get; set;
        }

        public string APNsTopic {
            get; set;
        }

        public string APNsTeamId {
            get; set;
        }

        public string NotificationId {
            get; set;
        }

        public string RequestId {
            get; set;
        }

        public LCPush() {
            Data = new Dictionary<string, object>();
            Target = new HashSet<string>(new string[] { "android", "ios" });
            Query = LCInstallation.GetQuery();
        }

        public async Task Send() {
            Dictionary<string, object> body = new Dictionary<string, object>();
            if (Query != null) {
                if (Target.Count == 0) {
                    Query.WhereNotContainedIn("deviceType", new string[] { "android", "ios" });
                } else if (Target.Count == 1) {
                    Query.WhereEqualTo("deviceType", Target.GetEnumerator().Current);
                }
                string condition = Query.BuildWhere();
                if (!string.IsNullOrEmpty(condition)) {
                    body["where"] = condition;
                }
            }
            if (!string.IsNullOrEmpty(CQL)) {
                body["cql"] = CQL;
            }
            if (body.ContainsKey("where") && body.ContainsKey("cql")) {
                throw new Exception("You can't use AVQuery and Cloud query at the same time.");
            }

            if (Channels != null && Channels.Count > 0) {
                body["channels"] = Channels.ToList();
            }
            if (ExpirationTime != default) {
                body["expiration_time"] = ExpirationTime;
            }
            if (ExpirationInterval != default) {
                body["push_time"] = DateTime.UtcNow.ToString(LCEncoder.DefaultDateTimeFormat, CultureInfo.InvariantCulture);
                body["expiration_interval"] = (long)ExpirationInterval.TotalSeconds;
            }
            if (PushDate != default) {
                body["push_time"] = PushDate.ToUniversalTime().ToString(LCEncoder.DefaultDateTimeFormat, CultureInfo.InvariantCulture);
            }
            if (FlowControl > 0) {
                body["flow_control"] = FlowControl;
            }
            if (!string.IsNullOrEmpty(IOSEnvironment)) {
                body["prod"] = IOSEnvironment;
            }
            if (!string.IsNullOrEmpty(APNsTopic)) {
                body["topic"] = APNsTopic;
            }
            if (!string.IsNullOrEmpty(APNsTeamId)) {
                body["apns_team_id"] = APNsTeamId;
            }
            if (!string.IsNullOrEmpty(NotificationId)) {
                body["notification_id"] = NotificationId;
            }

            if (Data != null) {
                body["data"] = Data;
            }

            await LCCore.HttpClient.Post<Dictionary<string, object>>("push", data: body);
        }
    }
}
