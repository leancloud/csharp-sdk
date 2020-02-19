using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using LeanCloud.Common;

namespace LeanCloud.Storage.Internal.Http {
    internal class LCHttpClient {
        string appId;

        string appKey;

        string server;

        string sdkVersion;

        string apiVersion;

        HttpClient client;

        internal LCHttpClient(string appId, string appKey, string server, string sdkVersion, string apiVersion) {
            this.appId = appId;
            this.appKey = appKey;
            this.server = server;
            this.sdkVersion = sdkVersion;
            this.apiVersion = apiVersion;

            client = new HttpClient();
            ProductHeaderValue product = new ProductHeaderValue("LeanCloud-CSharp-SDK", LeanCloud.SDKVersion);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(product));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-LC-Id", appId);
            // TODO
            client.DefaultRequestHeaders.Add("X-LC-Key", appKey);
        }

        internal Task Get() {
            return null;
        }

        internal async Task<Dictionary<string, object>> Post(string path,
            Dictionary<string, object> headers = null,
            Dictionary<string, object> data = null,
            Dictionary<string, object> queryParams = null) {
            string content = (data != null) ? JsonConvert.SerializeObject(data) : null;
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri($"{server}/{apiVersion}/{path}"),
                Method = HttpMethod.Post,
                Content = new StringContent(content)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpUtils.PrintRequest(client, request, content);
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            request.Dispose();

            string resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();
            HttpUtils.PrintResponse(response, resultString);

            HttpStatusCode statusCode = response.StatusCode;
            if (response.IsSuccessStatusCode) {
                Dictionary<string, object> ret = JsonConvert.DeserializeObject<Dictionary<string, object>>(resultString, new LeanCloudJsonConverter());
                return ret;
            }
            int code = (int)statusCode;
            string message = resultString;
            try {
                // 尝试获取 LeanCloud 返回错误信息
                Dictionary<string, object> error = JsonConvert.DeserializeObject<Dictionary<string, object>>(resultString, new LeanCloudJsonConverter());
                code = (int)error["code"];
                message = error["error"].ToString();
            } catch (Exception e) {
                Logger.Error(e.Message);
            } finally {
                throw new LCException(code, message);
            }
        }
    }
}
