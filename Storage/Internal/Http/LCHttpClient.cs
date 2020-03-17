using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using LeanCloud.Common;

namespace LeanCloud.Storage.Internal.Http {
    public class LCHttpClient {
        private readonly string appId;

        readonly string appKey;

        private readonly string server;

        private readonly string sdkVersion;

        readonly string apiVersion;

        readonly HttpClient client;

        readonly MD5 md5;

        public LCHttpClient(string appId, string appKey, string server, string sdkVersion, string apiVersion) {
            this.appId = appId;
            this.appKey = appKey;
            this.server = server;
            this.sdkVersion = sdkVersion;
            this.apiVersion = apiVersion;

            client = new HttpClient();
            ProductHeaderValue product = new ProductHeaderValue("LeanCloud-CSharp-SDK", sdkVersion);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(product));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-LC-Id", appId);

            md5 = MD5.Create();
        }

        public async Task<T> Get<T>(string path,
            Dictionary<string, object> headers = null,
            Dictionary<string, object> queryParams = null) {
            string url = await BuildUrl(path, queryParams);
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get
            };
            await FillHeaders(request.Headers, headers);
            
            LCHttpUtils.PrintRequest(client, request);
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            request.Dispose();

            string resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();
            LCHttpUtils.PrintResponse(response, resultString);

            if (response.IsSuccessStatusCode) {
                T ret = JsonConvert.DeserializeObject<T>(resultString, new LeanCloudJsonConverter());
                return ret;
            }
            throw HandleErrorResponse(response.StatusCode, resultString);
        }

        internal async Task<T> Post<T>(string path,
            Dictionary<string, object> headers = null,
            Dictionary<string, object> data = null,
            Dictionary<string, object> queryParams = null) {
            string url = await BuildUrl(path, queryParams);
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
            };
            await FillHeaders(request.Headers, headers);

            string content = null;
            if (data != null) {
                content = JsonConvert.SerializeObject(data);
                StringContent requestContent = new StringContent(content);
                requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = requestContent;
            }
            LCHttpUtils.PrintRequest(client, request, content);
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            request.Dispose();

            string resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();
            LCHttpUtils.PrintResponse(response, resultString);

            if (response.IsSuccessStatusCode) {
                T ret = JsonConvert.DeserializeObject<T>(resultString, new LeanCloudJsonConverter());
                return ret;
            }
            throw HandleErrorResponse(response.StatusCode, resultString);
        }

        internal async Task<T> Put<T>(string path,
            Dictionary<string, object> headers = null,
            Dictionary<string, object> data = null,
            Dictionary<string, object> queryParams = null) {
            string url = await BuildUrl(path, queryParams);
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(url),
                Method = HttpMethod.Put,
            };
            await FillHeaders(request.Headers, headers);

            string content = null;
            if (data != null) {
                content = JsonConvert.SerializeObject(data);
                StringContent requestContent = new StringContent(content);
                requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = requestContent;
            }
            LCHttpUtils.PrintRequest(client, request, content);
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            request.Dispose();

            string resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();
            LCHttpUtils.PrintResponse(response, resultString);

            if (response.IsSuccessStatusCode) {
                T ret = JsonConvert.DeserializeObject<T>(resultString, new LeanCloudJsonConverter());
                return ret;
            }
            throw HandleErrorResponse(response.StatusCode, resultString);
        }

        internal async Task Delete(string path) {
            string url = await BuildUrl(path);
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(url),
                Method = HttpMethod.Delete
            };
            await FillHeaders(request.Headers);

            LCHttpUtils.PrintRequest(client, request);
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            request.Dispose();

            string resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();
            LCHttpUtils.PrintResponse(response, resultString);

            if (response.IsSuccessStatusCode) {
                Dictionary<string, object> ret = JsonConvert.DeserializeObject<Dictionary<string, object>>(resultString, new LeanCloudJsonConverter());
                return;
            }
            throw HandleErrorResponse(response.StatusCode, resultString);
        }

        LCException HandleErrorResponse(HttpStatusCode statusCode, string responseContent) {
            int code = (int)statusCode;
            string message = responseContent;
            try {
                // 尝试获取 LeanCloud 返回错误信息
                Dictionary<string, object> error = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent, new LeanCloudJsonConverter());
                code = (int)error["code"];
                message = error["error"].ToString();
            } catch (Exception e) {
                LCLogger.Error(e.Message);
            }
            return new LCException(code, message);
        }

        async Task<string> BuildUrl(string path, Dictionary<string, object> queryParams = null) {
            string apiServer = await LCApplication.AppRouter.GetApiServer();
            string url = $"{apiServer}/{apiVersion}/{path}";
            if (queryParams != null) {
                IEnumerable<string> queryPairs = queryParams.Select(kv => $"{kv.Key}={kv.Value}");
                string queries = string.Join("&", queryPairs);
                url = $"{url}?{queries}";
            }
            return url;
        }

        async Task FillHeaders(HttpRequestHeaders headers, Dictionary<string, object> additionalHeaders = null) {
            // 额外 headers
            if (additionalHeaders != null) {
                foreach (KeyValuePair<string, object> kv in additionalHeaders) {
                    headers.Add(kv.Key, kv.Value.ToString());
                }
            }
            // 签名
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string data = $"{timestamp}{appKey}";
            string hash = GetMd5Hash(md5, data);
            string sign = $"{hash},{timestamp}";
            headers.Add("X-LC-Sign", sign);
            // 当前用户 Session Token
            LCUser currentUser = await LCUser.GetCurrent();
            if (currentUser != null) {
                headers.Add("X-LC-Session", currentUser.SessionToken);
            }
        }

        static string GetMd5Hash(MD5 md5Hash, string input) {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++) {
                sb.Append(data[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
