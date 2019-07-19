using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    internal class AVRouterController : IAVRouterController
    {
        const string routerUrl = "http://router.g0.push.leancloud.cn/v1/route?appId={0}";
        const string routerKey = "LeanCloud_RouterState";
        public Task<PushRouterState> GetAsync(string pushRouter = null, bool secure = true, CancellationToken cancellationToken = default(CancellationToken))
        {
            //return Task.FromResult(new PushRouterState()
            //{
            //    server = "wss://rtm57.leancloud.cn/"
            //});
            return LoadAysnc(cancellationToken).OnSuccess(_ =>
             {
                 var cache = _.Result;
                 var task = Task.FromResult<PushRouterState>(cache);

                 if (cache == null || cache.expire < DateTime.Now.ToUnixTimeStamp())
                 {
                     task = QueryAsync(pushRouter, secure, cancellationToken);
                 }

                 return task;
             }).Unwrap();
        }

        /// <summary>
        /// 清理地址缓存
        /// </summary>
        /// <returns>The cache.</returns>
        public Task ClearCache() {
            var tcs = new TaskCompletionSource<bool>();
            AVPlugins.Instance.StorageController.LoadAsync().ContinueWith(t => {
                if (t.IsFaulted) {
                    tcs.SetResult(true);
                } else {
                    var storage = t.Result;
                    if (storage.ContainsKey(routerKey)) {
                        storage.RemoveAsync(routerKey).ContinueWith(_ => tcs.SetResult(true));
                    } else {
                        tcs.SetResult(true);
                    }
                }
            });
            return tcs.Task;
        }

        Task<PushRouterState> LoadAysnc(CancellationToken cancellationToken)
        {
            try
            {
                return AVPlugins.Instance.StorageController.LoadAsync().OnSuccess(_ =>
                 {
                     var currentCache = _.Result;
                     object routeCacheStr = null;
                     if (currentCache.TryGetValue(routerKey, out routeCacheStr))
                     {
                         var routeCache = routeCacheStr as IDictionary<string, object>;
                         var routerState = new PushRouterState()
                         {
                             groupId = routeCache["groupId"] as string,
                             server = routeCache["server"] as string,
                             secondary = routeCache["secondary"] as string,
                             ttl = long.Parse(routeCache["ttl"].ToString()),
                             expire = long.Parse(routeCache["expire"].ToString()),
                             source = "localCache"
                         };
                         return routerState;
                     }
                     return null;
                 });
            }
            catch
            {
                return Task.FromResult<PushRouterState>(null);
            }
        }

        Task<PushRouterState> QueryAsync(string pushRouter, bool secure, CancellationToken cancellationToken)
        {
            var routerHost = pushRouter;
            if (routerHost == null) {
                var appRouter = AVPlugins.Instance.AppRouterController.Get();
                routerHost = string.Format("https://{0}/v1/route?appId={1}", appRouter.RealtimeRouterServer, AVClient.CurrentConfiguration.ApplicationId) ?? appRouter.RealtimeRouterServer ?? string.Format(routerUrl, AVClient.CurrentConfiguration.ApplicationId);
            }
            AVRealtime.PrintLog($"router: {routerHost}");
            AVRealtime.PrintLog($"push: {pushRouter}");
            if (!string.IsNullOrEmpty(pushRouter))
            {
                var rtmUri = new Uri(pushRouter);
                if (!string.IsNullOrEmpty(rtmUri.Scheme))
                {
                    var url = new Uri(rtmUri, "v1/route").ToString();
                    routerHost = string.Format("{0}?appId={1}", url, AVClient.CurrentConfiguration.ApplicationId);
                }
                else
                {
                    routerHost = string.Format("https://{0}/v1/route?appId={1}", pushRouter, AVClient.CurrentConfiguration.ApplicationId);
                }
            }
            if (secure)
            {
                routerHost += "&secure=1";
            }

            AVRealtime.PrintLog("use push router url:" + routerHost);

            return AVClient.RequestAsync(uri: new Uri(routerHost),
                method: "GET",
                headers: null,
                data: null,
                contentType: "application/json",
                cancellationToken: CancellationToken.None).ContinueWith<PushRouterState>(t =>
                {
                    if (t.Exception != null)
                    {
                        var innnerException = t.Exception.InnerException;
                        AVRealtime.PrintLog(innnerException.Message);
                        throw innnerException;
                    }
                    var httpStatus = (int)t.Result.Item1;
                    if (httpStatus != 200)
                    {
                        return null;
                    }
                    try
                    {
                        var result = t.Result.Item2;

                        var routerState = Json.Parse(result) as IDictionary<string, object>;
                        if (routerState.Keys.Count == 0)
                        {
                            throw new KeyNotFoundException("Can not get websocket url from server,please check the appId.");
                        }
                        var ttl = long.Parse(routerState["ttl"].ToString());
                        var expire = DateTime.Now.AddSeconds(ttl);
                        routerState["expire"] = expire.ToUnixTimeStamp();

                        //save to local cache async.
                        AVPlugins.Instance.StorageController.LoadAsync().OnSuccess(storage => storage.Result.AddAsync(routerKey, routerState));
                        var routerStateObj = new PushRouterState()
                        {
                            groupId = routerState["groupId"] as string,
                            server = routerState["server"] as string,
                            secondary = routerState["secondary"] as string,
                            ttl = long.Parse(routerState["ttl"].ToString()),
                            expire = expire.ToUnixTimeStamp(),
                            source = "online"
                        };

                        return routerStateObj;
                    }
                    catch (Exception e)
                    {
                        if (e is KeyNotFoundException)
                        {
                            throw e;
                        }
                        return null;
                    }

                });
        }
    }
}
