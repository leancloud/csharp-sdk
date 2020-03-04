using System;
using Newtonsoft.Json;

namespace LeanCloud.Common {
    public class AppRouter {
        // 华东应用 App Id 后缀
        const string EAST_CHINA_SUFFIX = "-9Nh9j0Va";
        // 美国应用 App Id 后缀
        const string US_SUFFIX = "-MdYXbMMI";

        [JsonProperty("ttl")]
        public long TTL {
            get; internal set;
        }

        [JsonProperty("api_server")]
        public string ApiServer {
            get; internal set;
        }

        [JsonProperty("engine_server")]
        public string EngineServer {
            get; internal set;
        }

        [JsonProperty("push_server")]
        public string PushServer {
            get; internal set;
        }

        [JsonProperty("rtm_router_server")]
        public string RTMServer {
            get; internal set;
        }

        [JsonProperty("stats_server")]
        public string StatsServer {
            get; internal set;
        }

        [JsonProperty("play_server")]
        public string PlayServer {
            get; internal set;
        }

        public string Source {
            get; internal set;
        }

        public DateTimeOffset FetchedAt {
            get; internal set;
        }

        public AppRouter() {
            FetchedAt = DateTimeOffset.Now;
        }

        public bool IsExpired {
            get {
                if (TTL == -1) {
                    return false;
                }
                return DateTimeOffset.Now > FetchedAt.AddSeconds(TTL);
            }
        }

        public static AppRouter GetFallbackServers(string appId) {
            var prefix = appId.Substring(0, 8).ToLower();
            var suffix = appId.Substring(appId.Length - 9);
            switch (suffix) {
                case EAST_CHINA_SUFFIX:
                    // 华东
                    return new AppRouter {
                        TTL = -1,
                        ApiServer = $"{prefix}.api.lncldapi.com",
                        EngineServer = $"{prefix}.engine.lncldapi.com",
                        PushServer = $"{prefix}.push.lncldapi.com",
                        RTMServer = $"{prefix}.rtm.lncldapi.com",
                        StatsServer = $"{prefix}.stats.lncldapi.com",
                        PlayServer = $"{prefix}.play.lncldapi.com",
                        Source = "fallback",
                    };
                case US_SUFFIX:
                    // 美国
                    return new AppRouter {
                        TTL = -1,
                        ApiServer = $"{prefix}.api.lncldglobal.com",
                        EngineServer = $"{prefix}.engine.lncldglobal.com",
                        PushServer = $"{prefix}.push.lncldglobal.com",
                        RTMServer = $"{prefix}.rtm.lncldglobal.com",
                        StatsServer = $"{prefix}.stats.lncldglobal.com",
                        PlayServer = $"{prefix}.play.lncldglobal.com",
                        Source = "fallback",
                    };
                default:
                    // 华北
                    return new AppRouter {
                        TTL = -1,
                        ApiServer = $"{prefix}.api.lncld.net",
                        EngineServer = $"{prefix}.engine.lncld.net",
                        PushServer = $"{prefix}.push.lncld.net",
                        RTMServer = $"{prefix}.rtm.lncld.net",
                        StatsServer = $"{prefix}.stats.lncld.net",
                        PlayServer = $"{prefix}.play.lncld.net",
                        Source = "fallback",
                    };
            }
        }
    }
}