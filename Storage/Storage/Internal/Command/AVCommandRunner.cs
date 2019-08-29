using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using Newtonsoft.Json;

namespace LeanCloud.Storage.Internal {
    /// <summary>
    /// Command Runner.
    /// </summary>
    public class AVCommandRunner {
        const string APPLICATION_JSON = "application/json";
        const string USE_PRODUCTION = "1";
        const string USE_DEVELOPMENT = "0";

        private readonly HttpClient httpClient;

        public AVCommandRunner() {
            httpClient = new HttpClient();
            ProductHeaderValue product = new ProductHeaderValue(AVClient.Name, AVClient.Version);
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(product));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(APPLICATION_JSON));

            var conf = AVClient.CurrentConfiguration;
            // App ID
            httpClient.DefaultRequestHeaders.Add("X-LC-Id", conf.ApplicationId);
            // App Signature
            long timestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
            if (!string.IsNullOrEmpty(conf.MasterKey) && AVClient.UseMasterKey) {
                string sign = MD5.GetMd5String(timestamp + conf.MasterKey);
                httpClient.DefaultRequestHeaders.Add("X-LC-Sign", $"{sign},{timestamp},master");
            } else {
                string sign = MD5.GetMd5String(timestamp + conf.ApplicationKey);
                httpClient.DefaultRequestHeaders.Add("X-LC-Sign", $"{sign},{timestamp}");
            }
            // TODO Session

            // Production
            httpClient.DefaultRequestHeaders.Add("X-LC-Prod", AVClient.UseProduction ? USE_PRODUCTION : USE_DEVELOPMENT);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Tuple<HttpStatusCode, T>> RunCommandAsync<T>(AVCommand command,CancellationToken cancellationToken = default) {
            string content = JsonConvert.SerializeObject(command.Content);
            var request = new HttpRequestMessage {
                RequestUri = command.Uri,
                Method = command.Method,
                Content = new StringContent(content)
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue(APPLICATION_JSON);
            // 特殊 Headers
            if (command.Headers != null) {
                foreach (KeyValuePair<string, string> header in command.Headers) {
                    request.Headers.Add(header.Key, header.Value);
                }
            }
            // Session Token
            if (AVUser.CurrentUser != null && !string.IsNullOrEmpty(AVUser.CurrentUser.SessionToken)) {
                request.Headers.Add("X-LC-Session", AVUser.CurrentUser.SessionToken);
            }
            PrintRequest(httpClient, request, content);

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            request.Dispose();

            var resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();
            PrintResponse(response, resultString);

            var ret = new Tuple<HttpStatusCode, string>(response.StatusCode, resultString);

            var responseCode = ret.Item1;
            var contentString = ret.Item2;

            if (responseCode >= HttpStatusCode.InternalServerError) {
                // Server error, return InternalServerError.
                throw new AVException(AVException.ErrorCode.InternalServerError, contentString);
            }

            if (responseCode < HttpStatusCode.OK || responseCode > HttpStatusCode.PartialContent) {
                // 错误处理
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(contentString, new LeanCloudJsonConverter());
                if (data.TryGetValue("code", out object codeObj)) {
                    AVException.ErrorCode code = (AVException.ErrorCode)Enum.ToObject(typeof(AVException.ErrorCode), codeObj);
                    string detail = data["error"] as string;
                    throw new AVException(code, detail);
                } else {
                    throw new AVException(AVException.ErrorCode.OtherCause, contentString);
                }
            }

            if (contentString != null) {
                try {
                    var data = JsonConvert.DeserializeObject<object>(contentString, new LeanCloudJsonConverter());
                    return new Tuple<HttpStatusCode, T>(responseCode, (T)data);
                } catch (Exception e) {
                    throw new AVException(AVException.ErrorCode.OtherCause,
                        "Invalid response from server", e);
                }
            }

            return new Tuple<HttpStatusCode, T>(responseCode, default);
        }

        static void PrintRequest(HttpClient client, HttpRequestMessage request, string content) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== HTTP Request Start ===");
            sb.AppendLine($"URL: {request.RequestUri}");
            sb.AppendLine($"Method: {request.Method}");
            sb.AppendLine($"Headers: ");
            foreach (var header in client.DefaultRequestHeaders) {
                sb.AppendLine($"\t{header.Key}: {string.Join(",", header.Value.ToArray())}");
            }
            foreach (var header in request.Headers) {
                sb.AppendLine($"\t{header.Key}: {string.Join(",", header.Value.ToArray())}");
            }
            foreach (var header in request.Content.Headers) {
                sb.AppendLine($"\t{header.Key}: {string.Join(",", header.Value.ToArray())}");
            }
            sb.AppendLine($"Content: {content}");
            sb.AppendLine("=== HTTP Request End ===");
            AVClient.PrintLog(sb.ToString());
        }

        static void PrintResponse(HttpResponseMessage response, string content) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== HTTP Response Start ===");
            sb.AppendLine($"URL: {response.RequestMessage.RequestUri}");
            sb.AppendLine($"Content: {content}");
            sb.AppendLine("=== HTTP Response End ===");
            AVClient.PrintLog(sb.ToString());
        }
    }
}
