using System;
using System.Threading.Tasks;
using System.Net.Http;
using LeanCloud.Storage.Internal;
using LeanCloud.Common;
using Newtonsoft.Json;

namespace LeanCloud.Realtime.Internal.Router {
    internal class LCRTMRouter {
        private LCRTMServer rtmServer;

        internal LCRTMRouter() {
        }

        internal async Task<string> GetServer() {
            if (rtmServer == null || !rtmServer.IsValid) {
                await Fetch();
            }
            return rtmServer.Server;
        }

        internal void Reset() {
            rtmServer = null;
        }

        async Task<LCRTMServer> Fetch() {
            string server = await LCApplication.AppRouter.GetRealtimeServer();
            string url = $"{server}/v1/route?appId={LCApplication.AppId}&secure=1";

            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get
            };
            HttpClient client = new HttpClient();
            LCHttpUtils.PrintRequest(client, request);
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            request.Dispose();

            string resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();
            LCHttpUtils.PrintResponse(response, resultString);

            rtmServer = JsonConvert.DeserializeObject<LCRTMServer>(resultString, new LCJsonConverter());

            return rtmServer;
        }
    }
}
