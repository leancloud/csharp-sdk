using System;
using Newtonsoft.Json;

namespace LeanCloud.Realtime.Internal.Router {
    internal class LCRTMServer {
        [JsonProperty("groupId")]
        internal string GroupId {
            get; set;
        }

        [JsonProperty("groupUrl")]
        internal string GroupUrl {
            get; set;
        }

        [JsonProperty("server")]
        internal string Primary {
            get; set;
        }

        [JsonProperty("secondary")]
        internal string Secondary {
            get; set;
        }

        [JsonProperty("ttl")]
        internal int Ttl {
            get; set;
        }

        DateTimeOffset createdAt;

        internal LCRTMServer() {
            createdAt = DateTimeOffset.Now;
        }

        internal bool IsValid => DateTimeOffset.Now < createdAt + TimeSpan.FromSeconds(Ttl);
    }
}
