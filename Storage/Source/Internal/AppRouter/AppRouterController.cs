using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal;

namespace LeanCloud.Storage.Internal
{
    public class AppRouterController : IAppRouterController
    {
        private AppRouterState currentState;
        private object mutex = new object();

        /// <summary>
        /// Get current app's router state
        /// </summary>
        /// <returns></returns>
        public AppRouterState Get()
        {
            if (string.IsNullOrEmpty(AVClient.CurrentConfiguration.ApplicationId))
            {
                throw new AVException(AVException.ErrorCode.NotInitialized, "ApplicationId can not be null.");
            }
            AppRouterState state;
            state = AppRouterState.GetInitial(AVClient.CurrentConfiguration.ApplicationId, AVClient.CurrentConfiguration.Region);

            lock (mutex)
            {
                if (currentState != null)
                {
                    if (!currentState.isExpired())
                        state = currentState;
                }
            }

            if (state.isExpired())
            {
                lock (mutex)
                {
                    state.FetchedAt = DateTime.Now + TimeSpan.FromMinutes(10);
                }
                Task.Factory.StartNew(RefreshAsync);
            }
            return state;
        }

        public Task RefreshAsync()
        {
            return QueryAsync(CancellationToken.None).ContinueWith(t =>
            {
                if (!t.IsFaulted && !t.IsCanceled)
                {
                    lock (mutex)
                    {
                        currentState = t.Result;
                    }
                }
            });
        }

        public Task<AppRouterState> QueryAsync(CancellationToken cancellationToken)
        {
            string appId = AVClient.CurrentConfiguration.ApplicationId;
            string url = string.Format("https://app-router.leancloud.cn/2/route?appId={0}", appId);

            return AVClient.HttpGetAsync(new Uri(url)).ContinueWith(t =>
            {
                var tcs = new TaskCompletionSource<AppRouterState>();
                if (t.Result.Item1 != HttpStatusCode.OK)
                {
                    tcs.SetException(new AVException(AVException.ErrorCode.ConnectionFailed, "can not reach router.", null));
                }
                else
                {
                    var result = Json.Parse(t.Result.Item2) as IDictionary<String, Object>;
                    tcs.SetResult(ParseAppRouterState(result));
                }
                return tcs.Task;
            }).Unwrap();
        }

        private static AppRouterState ParseAppRouterState(IDictionary<string, object> jsonObj)
        {
            var state = new AppRouterState()
            {
                TTL = (int)jsonObj["ttl"],
                StatsServer = jsonObj["stats_server"] as string,
                RealtimeRouterServer = jsonObj["rtm_router_server"] as string,
                PushServer = jsonObj["push_server"] as string,
                EngineServer = jsonObj["engine_server"] as string,
                ApiServer = jsonObj["api_server"] as string,
                Source = "network",
            };
            return state;
        }

        public void Clear()
        {
            currentState = null;
        }
    }
}
