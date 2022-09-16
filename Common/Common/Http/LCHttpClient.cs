using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Security.Cryptography;
using LC.Newtonsoft.Json;

namespace LeanCloud.Common {
    public class LCHttpClient {
        private readonly string appId;

        readonly string appKey;

        private readonly string server;

        private readonly string sdkVersion;

        readonly string apiVersion;

        readonly HttpClient client;

        readonly MD5 md5;

        private Dictionary<string, Func<Task<string>>> runtimeHeaderTasks = new Dictionary<string, Func<Task<string>>>();

        private Dictionary<string, string> additionalHeaders = new Dictionary<string, string>();

        public LCHttpClient(string appId, string appKey, string server, string sdkVersion, string apiVersion) {
            this.appId = appId;
            this.appKey = appKey;
            this.server = server;
            this.sdkVersion = sdkVersion;
            this.apiVersion = apiVersion;

            client = new HttpClient();
            ProductHeaderValue product = new ProductHeaderValue(LCCore.SDKName, sdkVersion);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(product));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-LC-Id", appId);

            md5 = MD5.Create();
        }

        public void AddRuntimeHeaderTask(string key, Func<Task<string>> task) {
            if (string.IsNullOrEmpty(key)) {
                return;
            }
            if (task == null) {
                return;
            }
            runtimeHeaderTasks[key] = task;
        }

        public void AddAddtionalHeader(string key, string value) {
            if (string.IsNullOrEmpty(key)) {
                return;
            }
            if (string.IsNullOrEmpty(value)) {
                return;
            }
            additionalHeaders[key] = value;
        }

        public Task<T> Get<T>(string path,
            Dictionary<string, object> headers = null,
            Dictionary<string, object> queryParams = null,
            bool withAPIVersion = true) {
            return Request<T>(path, HttpMethod.Get, headers, null, queryParams, withAPIVersion);
        }

        public Task<T> Post<T>(string path,
            Dictionary<string, object> headers = null,
            object data = null,
            Dictionary<string, object> queryParams = null,
            bool withAPIVersion = true) {
            return Request<T>(path, HttpMethod.Post, headers, data, queryParams, withAPIVersion);
        }

        public Task<T> Put<T>(string path,
            Dictionary<string, object> headers = null,
            object data = null,
            Dictionary<string, object> queryParams = null,
            bool withAPIVersion = true) {
            return Request<T>(path, HttpMethod.Put, headers, data, queryParams, withAPIVersion);
        }

        public Task Delete(string path,
            Dictionary<string, object> headers = null,
            object data = null,
            Dictionary<string, object> queryParams = null,
            bool withAPIVersion = true) {
            return Request<Dictionary<string, object>>(path, HttpMethod.Delete, headers, data, queryParams, withAPIVersion);
        }

        async Task<T> Request<T>(string path,
            HttpMethod method,
            Dictionary<string, object> headers = null,
            object data = null,
            Dictionary<string, object> queryParams = null,
            bool withAPIVersion = true) {
            string url = await BuildUrl(path, queryParams, withAPIVersion);
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(url),
                Method = method,
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
                T ret = JsonConvert.DeserializeObject<T>(resultString,
                    LCJsonConverter.Default);
                return ret;
            }
            throw HandleErrorResponse(response.StatusCode, resultString);
        }

        LCException HandleErrorResponse(HttpStatusCode statusCode, string responseContent) {
            int code = (int)statusCode;
            string message = responseContent;
            try {
                // 尝试获取 LeanCloud 返回错误信息
                Dictionary<string, object> error = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent,
                    LCJsonConverter.Default);
                code = (int)error["code"];
                message = error["error"].ToString();
            } catch (Exception e) {
                LCLogger.Error(e);
            }
            return new LCException(code, message);
        }

        async Task<string> BuildUrl(string path, Dictionary<string, object> queryParams, bool withAPIVersion) {
            string apiServer = await LCCore.AppRouter.GetApiServer();
            StringBuilder urlSB = new StringBuilder(apiServer.TrimEnd('/'));
            if (withAPIVersion) {
                urlSB.Append($"/{apiVersion}");
            }
            urlSB.Append($"/{path}");
            string url = urlSB.ToString();
            if (queryParams != null) {
                IEnumerable<string> queryPairs = queryParams.Select(kv => $"{kv.Key}={kv.Value}");
                string queries = string.Join("&", queryPairs);
                url = $"{url}?{queries}";
            }
            return url;
        }

        async Task FillHeaders(HttpRequestHeaders headers, Dictionary<string, object> reqHeaders = null) {
            // 额外 headers
            if (reqHeaders != null) {
                foreach (KeyValuePair<string, object> kv in reqHeaders) {
                    headers.Add(kv.Key, kv.Value.ToString());
                }
            }
            if (LCCore.UseMasterKey && !string.IsNullOrEmpty(LCCore.MasterKey)) {
                // Master Key
                headers.Add("X-LC-Key", $"{LCCore.MasterKey},master");
            } else {
                // 签名
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string data = $"{timestamp}{appKey}";
                string hash = GetMd5Hash(md5, data);
                string sign = $"{hash},{timestamp}";
                headers.Add("X-LC-Sign", sign);
            }
            if (additionalHeaders.Count > 0) {
                foreach (KeyValuePair<string, string> kv in additionalHeaders) {
                    headers.Add(kv.Key, kv.Value);
                }    
            }
            // 服务额外 headers
            foreach (KeyValuePair<string, Func<Task<string>>> kv in runtimeHeaderTasks) {
                if (headers.Contains(kv.Key)) {
                    continue;
                }
                string value = await kv.Value.Invoke();
                if (value == null) {
                    continue;
                }
                headers.Add(kv.Key, value);
            }
        }

        static string GetMd5Hash(MD5 md5Hash, string input) {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            return LCUtils.ToHex(data);
        }
    }
}
