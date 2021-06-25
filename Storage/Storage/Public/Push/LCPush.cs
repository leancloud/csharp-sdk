using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Storage;

namespace LeanCloud.Push {
    public class LCPush {
        public Dictionary<string, object> Data {
            get; set;
        }

        public LCQuery<LCInstallation> Query {
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

        }
    }
}
