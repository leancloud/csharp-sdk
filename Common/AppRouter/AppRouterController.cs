using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace LeanCloud.Common {
    public class AppRouterController {
        readonly string appId;

        AppRouter currentState;

        readonly SemaphoreSlim locker = new SemaphoreSlim(1);

        public AppRouterController(string appId, string server) {
            if (!IsInternationalApp(appId) && string.IsNullOrEmpty(server)) {
                // 国内 App 必须设置域名
                throw new ArgumentException("You must init with your domain.");
            }
            if (!string.IsNullOrEmpty(server)) {
                currentState = new AppRouter {
                    ApiServer = server,
                    EngineServer = server,
                    PushServer = server,
                    RTMServer = server,
                    StatsServer = server,
                    PlayServer = server,
                    TTL = -1
                };
            }
            this.appId = appId;
        }

        public async Task<AppRouter> Get() {
            if (string.IsNullOrEmpty(appId)) {
                throw new ArgumentNullException(nameof(appId));
            }

            if (currentState != null && !currentState.IsExpired) {
                return currentState;
            }

            await locker.WaitAsync();
            try {
                if (currentState == null) {
                    try {
                        currentState = await QueryAsync();
                    } catch (Exception) {
                        currentState = AppRouter.GetFallbackServers(appId);
                    }
                }
                return currentState;
            } finally {
                locker.Release();
            }
        }

        async Task<AppRouter> QueryAsync() {
            HttpClient client = null;
            HttpRequestMessage request = null;
            HttpResponseMessage response = null;

            try {
                string url = string.Format("https://app-router.com/2/route?appId={0}", appId);

                client = new HttpClient();
                request = new HttpRequestMessage {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get
                };
                HttpUtils.PrintRequest(client, request);

                response = await client.SendAsync(request);
                string content = await response.Content.ReadAsStringAsync();
                HttpUtils.PrintResponse(response, content);
                
                AppRouter state = JsonConvert.DeserializeObject<AppRouter>(content);
                state.Source = "router";

                return state;
            } finally {
                if (client != null) {
                    client.Dispose();
                }
                if (request != null) {
                    request.Dispose();
                }
                if (response != null) {
                    response.Dispose();
                }
            }
        }

        public void Clear() {
            currentState = null;
        }

        static bool IsInternationalApp(string appId) {
            if (appId.Length < 9) {
                return false;
            }
            string suffix = appId.Substring(appId.Length - 9);
            return suffix == "-MdYXbMMI";
        }
    }
}
