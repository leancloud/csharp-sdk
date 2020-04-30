using System;
using System.Threading.Tasks;
using System.Net.Http;
using LeanCloud.Storage.Internal;
using LeanCloud.Common;
using Newtonsoft.Json;

namespace LeanCloud.Realtime.Internal.Router {
    /// <summary>
    /// RTM Router
    /// </summary>
    internal class LCRTMRouter {
        /// <summary>
        /// 请求超时
        /// </summary>
        private const int REQUEST_TIMEOUT = 10000;

        private LCRTMServer rtmServer;

        internal LCRTMRouter() {
        }

        /// <summary>
        /// 获取服务器地址
        /// </summary>
        /// <returns></returns>
        internal async Task<LCRTMServer> GetServer() {
            if (rtmServer == null || !rtmServer.IsValid) {
                await Fetch();
            }
            return rtmServer;
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

            Task<HttpResponseMessage> requestTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (await Task.WhenAny(requestTask, Task.Delay(REQUEST_TIMEOUT)) != requestTask) {
                throw new TimeoutException("Request timeout.");
            }

            HttpResponseMessage response = await requestTask;
            request.Dispose();
            string resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();
            LCHttpUtils.PrintResponse(response, resultString);

            rtmServer = JsonConvert.DeserializeObject<LCRTMServer>(resultString, new LCJsonConverter());

            return rtmServer;
        }
    }
}
