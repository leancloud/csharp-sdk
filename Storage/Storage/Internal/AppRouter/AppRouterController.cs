using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;

namespace LeanCloud.Storage.Internal
{
    public class AppRouterController : IAppRouterController
    {
        private AppRouterState currentState;
        private readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        /// <summary>
        /// Get current app's router state
        /// </summary>
        /// <returns></returns>
        public AppRouterState Get() {
            if (string.IsNullOrEmpty(AVClient.CurrentConfiguration.ApplicationId)) {
                throw new AVException(AVException.ErrorCode.NotInitialized, "ApplicationId can not be null.");
            }

            try {
                locker.EnterUpgradeableReadLock();
                if (currentState != null && !currentState.IsExpired) {
                    return currentState;
                }
                // 从 AppRouter 获取服务器地址，只触发，不等待
                QueryAsync(CancellationToken.None).OnSuccess(t => {
                    locker.EnterWriteLock();
                    currentState = t.Result;
                    currentState.Source = "router";
                    locker.ExitWriteLock();
                });
                return AppRouterState.GetFallbackServers(AVClient.CurrentConfiguration.ApplicationId, AVClient.CurrentConfiguration.Region);
            } finally {
                locker.ExitUpgradeableReadLock();
            }
        }

        public async Task<AppRouterState> QueryAsync(CancellationToken cancellationToken) {
            string appId = AVClient.CurrentConfiguration.ApplicationId;
            string url = string.Format("https://app-router.leancloud.cn/2/route?appId={0}", appId);

            var request = new HttpRequest {
                Uri = new Uri(url),
                Method = HttpMethod.Get,
                Headers = null,
                Data = null
            };
            var ret = await AVPlugins.Instance.HttpClient.ExecuteAsync(request, null, null, CancellationToken.None);
            if (ret.Item1 != HttpStatusCode.OK) {
                throw new AVException(AVException.ErrorCode.ConnectionFailed, "can not reach router.", null);
            }

            return await JsonUtils.DeserializeObjectAsync<AppRouterState>(ret.Item2);
        }

        public void Clear() {
            locker.EnterWriteLock();
            currentState = null;
            locker.ExitWriteLock();
        }
    }
}
