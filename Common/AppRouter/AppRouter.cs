using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LeanCloud.Common {
    public class AppRouter {
        private readonly string appId;

        private readonly string server;

        private AppServer appServer;

        public AppRouter(string appId, string server) {
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
            // 判断节点地区
            if (!IsInternalApp(appId)) {
                // 国内节点必须配置自定义域名
                throw new Exception("Please init with your server url.");
            }
            // 向 App Router 请求地址
            if (appServer == null || appServer.IsExpired) {
                try {
                    HttpRequestMessage request = new HttpRequestMessage {
                        RequestUri = new Uri($"https://app-router.com/2/route?appId={appId}"),
                        Method = HttpMethod.Get
                    };
                    HttpClient client = new HttpClient();
                    HttpUtils.PrintRequest(client, request);
                    HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    request.Dispose();

                    string resultString = await response.Content.ReadAsStringAsync();
                    response.Dispose();
                    HttpUtils.PrintResponse(response, resultString);

                    Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(resultString);
                    appServer = new AppServer(data);
                } catch (Exception e) {
                    Logger.Error(e.Message);
                    // 拉取服务地址失败后，使用国际节点的默认服务地址
                    appServer = AppServer.GetInternalFallbackAppServer(appId);
                }
            }
            return appServer.ApiServer;
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