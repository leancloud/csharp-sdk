using System;
using Newtonsoft.Json;

namespace LeanCloud.Storage.Internal
{
    public class AppRouterState
    {
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

        public string Source {
            get; internal set;
        }

        public DateTime FetchedAt {
            get; internal set;
        }

        public AppRouterState() {
            FetchedAt = DateTime.Now;
        }

        /// <summary>
        /// Is this app router state expired.
        /// </summary>
        public bool IsExpired {
            get {
                return DateTime.Now > FetchedAt + TimeSpan.FromSeconds(TTL);
            }
        }

        /// <summary>
        /// Get the initial usable router state
        /// </summary>
        /// <param name="appId">Current app's appId</param>
        /// <param name="region">Current app's region</param>
        /// <returns>Initial app router state</returns>
        public static AppRouterState GetFallbackServers(string appId, AVClient.Configuration.AVRegion region) {
            var regionValue = (int)region;
            var prefix = appId.Substring(0, 8).ToLower();
            switch (regionValue)
            {
                case 0:
                    // 华北
                    return new AppRouterState {
                        TTL = -1,
                        ApiServer = $"{prefix}.api.lncld.net",
                        EngineServer = $"{prefix}.engine.lncld.net",
                        PushServer = $"{prefix}.push.lncld.net",
                        RTMServer = $"{prefix}.rtm.lncld.net",
                        StatsServer = $"{prefix}.stats.lncld.net",
                        Source = "fallback",
                    };
                case 1:
                    // 美国
                    return new AppRouterState {
                        TTL = -1,
                        ApiServer = $"{prefix}.api.lncldglobal.com",
                        EngineServer = $"{prefix}.engine.lncldglobal.com",
                        PushServer = $"{prefix}.push.lncldglobal.com",
                        RTMServer = $"{prefix}.rtm.lncldglobal.com",
                        StatsServer = $"{prefix}.stats.lncldglobal.com",
                        Source = "fallback",
                    };
                case 2:
                    // 华东
                    return new AppRouterState {
                        TTL = -1,
                        ApiServer = $"{prefix}.api.lncldapi.com",
                        EngineServer = $"{prefix}.engine.lncldapi.com",
                        PushServer = $"{prefix}.push.lncldapi.com",
                        RTMServer = $"{prefix}.rtm.lncldapi.com",
                        StatsServer = $"{prefix}.stats.lncldapi.com",
                        Source = "fallback",
                    };
                default:
                    throw new AVException(AVException.ErrorCode.OtherCause, "invalid region");
            }
        }

    }
}