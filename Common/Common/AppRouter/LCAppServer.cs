using System;
using System.Collections.Generic;
using LC.Newtonsoft.Json;

namespace LeanCloud.Common {
    public class LCAppServer {
        [JsonProperty("api_server")]
        public string ApiServer {
            get => apiServer;
            internal set {
                apiServer = GetUrlWithScheme(value);
            }
        }

        private string apiServer;

        [JsonProperty("engine_server")]
        public string EngineServer {
            get => engineServer;
            internal set {
                engineServer = GetUrlWithScheme(value);
            }
        }

        private string engineServer;

        [JsonProperty("push_server")]
        public string PushServer {
            get => pushServer;
            private set {
                pushServer = GetUrlWithScheme(value);
            }
        }

        private string pushServer;

        [JsonProperty("ttl")]
        public int Ttl {
            get; set;
        }

        public string RTMServer {
            get; private set;
        }

        public bool IsValid => Ttl == -1 || DateTimeOffset.Now < createdAt + TimeSpan.FromSeconds(Ttl);

        private readonly DateTimeOffset createdAt;

        public LCAppServer() {
            createdAt = DateTimeOffset.Now;
        }

        private static string GetUrlWithScheme(string url) {
            return url.StartsWith("https://") ? url : $"https://{url}";
        }

        internal static LCAppServer GetInternalFallbackAppServer(string appId) {
            string prefix = appId.Substring(0, 8).ToLower();
            return new LCAppServer {
                ApiServer = $"https://{prefix}.api.lncldglobal.com",
                PushServer = $"https://{prefix}.engine.lncldglobal.com",
                EngineServer = $"https://{prefix}.push.lncldglobal.com",
                Ttl = -1
            };
        }
    }
}
