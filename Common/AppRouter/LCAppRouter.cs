using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LeanCloud.Common {
    public class LCAppRouter {
        private readonly string appId;

        private readonly string server;

        private LCAppServer appServer;

        public LCAppRouter(string appId, string server) {
            if (!IsInternalApp(appId) && string.IsNullOrEmpty(server)) {
                // 国内节点必须配置自定义域名
                throw new Exception("Please init with your server url.");
            }
            this.appId = appId;
            this.server = server;
        }

        public async Task<string> GetApiServer() {
            // 优先返回用户自定义域名
            if (!string.IsNullOrEmpty(server)) {
                return server;
            }
            LCAppServer appServ = await FetchAppServer();
            return appServ.ApiServer;
        }

        public async Task<string> GetRealtimeServer() {
            if (!string.IsNullOrEmpty(server)) {
                return server;
            }
            LCAppServer appServ = await FetchAppServer();
            return appServ.PushServer;
        }

        async Task<LCAppServer> FetchAppServer() {
            // 判断节点地区
            if (!IsInternalApp(appId)) {
                // 国内节点必须配置自定义域名
                throw new Exception("Please init with your server url.");
            }
            // 向 App Router 请求地址
            if (appServer == null || !appServer.IsValid) {
                try {
                    HttpRequestMessage request = new HttpRequestMessage {
                        RequestUri = new Uri($"https://app-router.com/2/route?appId={appId}"),
                        Method = HttpMethod.Get
                    };
                    HttpClient client = new HttpClient();
                    LCHttpUtils.PrintRequest(client, request);
                    HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    request.Dispose();

                    string resultString = await response.Content.ReadAsStringAsync();
                    response.Dispose();
                    LCHttpUtils.PrintResponse(response, resultString);

                    Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(resultString);
                    appServer = new LCAppServer(data);
                } catch (Exception e) {
                    LCLogger.Error(e.Message);
                    // 拉取服务地址失败后，使用国际节点的默认服务地址
                    appServer = LCAppServer.GetInternalFallbackAppServer(appId);
                }
            }
            return appServer;
        }

        private static bool IsInternalApp(string appId) {
            if (appId.Length < 9) {
                return false;
            }
            string suffix = appId.Substring(appId.Length - 9);
            return suffix == "-MdYXbMMI";
        }
    }
}