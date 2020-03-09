using System;
using System.Collections.Generic;

namespace LeanCloud.Common {
    public class AppServer {
        public string ApiServer {
            get; private set;
        }

        public string EngineServer {
            get; private set;
        }

        public string PushServer {
            get; private set;
        }

        public string RTMServer {
            get; private set;
        }

        public bool IsExpired {
            get {
                return ttl != -1 && DateTime.Now > expiredAt;
            }
        }

        private readonly DateTime expiredAt;

        private readonly int ttl;

        public AppServer(Dictionary<string, object> data) {
            ApiServer = GetUrlWithScheme(data["api_server"] as string);
            PushServer = GetUrlWithScheme(data["push_server"] as string);
            EngineServer = GetUrlWithScheme(data["engine_server"] as string);
            ttl = (int)(long)data["ttl"];
            expiredAt = DateTime.Now.AddSeconds(ttl);
        }

        private static string GetUrlWithScheme(string url) {
            return url.StartsWith("https://") ? url : $"https://{url}";
        }

        internal static AppServer GetInternalFallbackAppServer(string appId) {
            string prefix = appId.Substring(0, 8).ToLower();
            return new AppServer(new Dictionary<string, object> {
                { "api_server", $"https://{prefix}.api.lncldglobal.com" },
                { "push_server", $"https://{prefix}.engine.lncldglobal.com" },
                { "engine_server", $"https://{prefix}.push.lncldglobal.com" },
                { "ttl", -1 }
            });
        }
    }
}
