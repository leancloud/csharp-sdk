using System;
using LC.Newtonsoft.Json;

namespace LeanCloud.Realtime.Internal.Router {
    public class LCRTMServer {
        [JsonProperty("groupId")]
        public string GroupId {
            get; set;
        }

        [JsonProperty("groupUrl")]
        public string GroupUrl {
            get; set;
        }

        [JsonProperty("server")]
        public string Primary {
            get; set;
        }

        [JsonProperty("secondary")]
        public string Secondary {
            get; set;
        }

        [JsonProperty("ttl")]
        public int Ttl {
            get; set;
        }

        DateTimeOffset createdAt;

        public LCRTMServer() {
            createdAt = DateTimeOffset.Now;
        }

        public bool IsValid => DateTimeOffset.Now < createdAt + TimeSpan.FromSeconds(Ttl);
    }
}
