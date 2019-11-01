using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace LeanCloud.Common {
    public class AppRouterController {
        private AppRouterState currentState;

        private readonly SemaphoreSlim locker = new SemaphoreSlim(1);

        public async Task<AppRouterState> Get(string appId) {
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
                        currentState = await QueryAsync(appId);
                    } catch (Exception) {
                        currentState = AppRouterState.GetFallbackServers(appId);
                    }
                }
                return currentState;
            } finally {
                locker.Release();
            }
        }

        async Task<AppRouterState> QueryAsync(string appId) {
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
                HttpUtils.PrintResponse(response);

                AppRouterState state = JsonConvert.DeserializeObject<AppRouterState>(content);
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
    }
}
