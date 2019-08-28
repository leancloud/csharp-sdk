using System;

namespace LeanCloud.Storage.Internal
{
    public class AppRouterState
    {
        public long TTL { get; internal set; }
        public string ApiServer { get; internal set; }
        public string EngineServer { get; internal set; }
        public string PushServer { get; internal set; }
        public string RealtimeRouterServer { get; internal set; }
        public string StatsServer { get; internal set; }
        public string Source { get; internal set; }

        public DateTime FetchedAt { get; internal set; }

        private static object mutex = new object();

        public AppRouterState()
        {
            FetchedAt = DateTime.Now;
        }

        /// <summary>
        /// Is this app router state expired.
        /// </summary>
        public bool isExpired()
        {
            return DateTime.Now > FetchedAt + TimeSpan.FromSeconds(TTL);
        }

        /// <summary>
        /// Get the initial usable router state
        /// </summary>
        /// <param name="appId">Current app's appId</param>
        /// <param name="region">Current app's region</param>
        /// <returns>Initial app router state</returns>
        public static AppRouterState GetInitial(string appId, AVClient.Configuration.AVRegion region)
        {
            var regionValue = (int)region;
            var prefix = appId.Substring(0, 8).ToLower();
            switch (regionValue)
            {
                case 0:
                    // 华北
                    return new AppRouterState()
                    {
                        TTL = -1,
                        ApiServer = string.Format("{0}.api.lncld.net", prefix),
                        EngineServer = string.Format("{0}.engine.lncld.net", prefix),
                        PushServer = string.Format("{0}.push.lncld.net", prefix),
                        RealtimeRouterServer = string.Format("{0}.rtm.lncld.net", prefix),
                        StatsServer = string.Format("{0}.stats.lncld.net", prefix),
                        Source = "initial",
                    };
                case 1:
                    // 美国
                    return new AppRouterState()
                    {
                        TTL = -1,
                        ApiServer = string.Format("{0}.api.lncldglobal.com", prefix),
                        EngineServer = string.Format("{0}.engine.lncldglobal.com", prefix),
                        PushServer = string.Format("{0}.push.lncldglobal.com", prefix),
                        RealtimeRouterServer = string.Format("{0}.rtm.lncldglobal.com", prefix),
                        StatsServer = string.Format("{0}.stats.lncldglobal.com", prefix),
                        Source = "initial",
                    };
                case 2:
                    // 华东
                    return new AppRouterState() {
                        TTL = -1,
                        ApiServer = string.Format("{0}.api.lncldapi.com", prefix),
                        EngineServer = string.Format("{0}.engine.lncldapi.com", prefix),
                        PushServer = string.Format("{0}.push.lncldapi.com", prefix),
                        RealtimeRouterServer = string.Format("{0}.rtm.lncldapi.com", prefix),
                        StatsServer = string.Format("{0}.stats.lncldapi.com", prefix),
                        Source = "initial",
                    };
                default:
                    throw new AVException(AVException.ErrorCode.OtherCause, "invalid region");
            }
        }

    }
}