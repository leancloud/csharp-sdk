using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace LeanCloud.Storage.Internal {
    public class AppRouterController {
        private AppRouterState currentState;
        private readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        public AppRouterState Get(string appId) {
            if (string.IsNullOrEmpty(appId)) {
                throw new ArgumentNullException(nameof(appId));
            }

            try {
                locker.EnterUpgradeableReadLock();
                if (currentState != null && !currentState.IsExpired) {
                    return currentState;
                }
                // 从 AppRouter 获取服务器地址，只触发，不等待
                QueryAsync(appId).ContinueWith(t => {
                    if (t.IsFaulted) {

                    } else {
                        locker.EnterWriteLock();
                        currentState = t.Result;
                        currentState.Source = "router";
                        locker.ExitWriteLock();
                    }
                });
                return AppRouterState.GetFallbackServers(appId);
            } finally {
                locker.ExitUpgradeableReadLock();
            }
        }

        public async Task<AppRouterState> QueryAsync(string appId) {
            string url = string.Format("https://app-router.leancloud.cn/2/route?appId={0}", appId);

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get
            };

            HttpResponseMessage response = await client.SendAsync(request);
            client.Dispose();
            request.Dispose();

            string content = await response.Content.ReadAsStringAsync();
            response.Dispose();

            return JsonConvert.DeserializeObject<AppRouterState>(content);
        }

        public void Clear() {
            locker.EnterWriteLock();
            currentState = null;
            locker.ExitWriteLock();
        }
    }
}
