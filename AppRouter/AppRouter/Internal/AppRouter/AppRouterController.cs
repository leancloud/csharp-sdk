using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace LeanCloud.Storage.Internal {
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
            Console.WriteLine("QueryAsync");

            string url = string.Format("https://app-router.com/2/route?appId={0}", appId);

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

            AppRouterState state = JsonConvert.DeserializeObject<AppRouterState>(content);
            state.Source = "router";
            return state;
        }

        public void Clear() {
            currentState = null;
        }
    }
}
