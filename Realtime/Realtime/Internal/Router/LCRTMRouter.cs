using System;
using System.Threading.Tasks;
using System.Net.Http;
using LeanCloud.Common;
using LC.Newtonsoft.Json;

namespace LeanCloud.Realtime.Internal.Router {

    public class LCRTMRouter {
        private const int REQUEST_TIMEOUT = 10000;

        private LCRTMServer rtmServer;

        private readonly HttpClient httpClient;

        public LCRTMRouter() {
            httpClient = new HttpClient {
                Timeout = TimeSpan.FromMilliseconds(REQUEST_TIMEOUT)
            };
        }

        public async Task<LCRTMServer> GetServer() {
            if (rtmServer != null && rtmServer.IsValid) {
                return rtmServer;
            }
            return await Fetch();
        }

        async Task<LCRTMServer> Fetch() {
            string server = await LCCore.AppRouter.GetRealtimeServer();
            string url = $"{server}/v1/route?appId={LCCore.AppId}&secure=1";

            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get
            };

            LCHttpUtils.PrintRequest(httpClient, request);

            HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            request.Dispose();

            string resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();

            LCHttpUtils.PrintResponse(response, resultString);

            rtmServer = JsonConvert.DeserializeObject<LCRTMServer>(resultString, LCJsonConverter.Default);

            return rtmServer;
        }

        public void Reset() {
            rtmServer = null;
        }
    }
}
