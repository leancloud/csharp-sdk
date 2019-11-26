using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using LeanCloud.Common;

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
            if (!request.Headers.Contains("X-LC-Session") &&
                AVUser.CurrentUser != null &&
                !string.IsNullOrEmpty(AVUser.CurrentUser.SessionToken)) {
                request.Headers.Add("X-LC-Session", AVUser.CurrentUser.SessionToken);
            }
            HttpUtils.PrintRequest(httpClient, request, content);

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            request.Dispose();

            var resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();
            HttpUtils.PrintResponse(response, resultString);

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


        // TODO (hallucinogen): move this out to a class to be used by Analytics
        private const int MaximumBatchSize = 50;

        internal async Task<IList<IDictionary<string, object>>> ExecuteBatchRequests(IList<AVCommand> requests,
            CancellationToken cancellationToken) {
            var results = new List<IDictionary<string, object>>();
            int batchSize = requests.Count;

            IEnumerable<AVCommand> remaining = requests;
            while (batchSize > MaximumBatchSize) {
                var process = remaining.Take(MaximumBatchSize).ToList();
                remaining = remaining.Skip(MaximumBatchSize);

                results.AddRange(await ExecuteBatchRequest(process, cancellationToken));

                batchSize = remaining.Count();
            }
            results.AddRange(await ExecuteBatchRequest(remaining.ToList(), cancellationToken));

            return results;
        }

        internal async Task<IList<IDictionary<string, object>>> ExecuteBatchRequest(IList<AVCommand> requests, CancellationToken cancellationToken) {
            var tasks = new List<Task<IDictionary<string, object>>>();
            int batchSize = requests.Count;
            var tcss = new List<TaskCompletionSource<IDictionary<string, object>>>();
            for (int i = 0; i < batchSize; ++i) {
                var tcs = new TaskCompletionSource<IDictionary<string, object>>();
                tcss.Add(tcs);
                tasks.Add(tcs.Task);
            }

            var encodedRequests = requests.Select(r => {
                var results = new Dictionary<string, object> {
                    { "method", r.Method.Method },
                    { "path", $"/{AVClient.APIVersion}/{r.Path}" },
                };

                if (r.Content != null) {
                    results["body"] = r.Content;
                }
                return results;
            }).Cast<object>().ToList();
            var command = new AVCommand {
                Path = "batch",
                Method = HttpMethod.Post,
                Content = new Dictionary<string, object> {
                    { "requests", encodedRequests }
                }
            };

            try {
                List<IDictionary<string, object>> result = new List<IDictionary<string, object>>();
                var response = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IList<object>>(command, cancellationToken);
                return response.Item2.Cast<IDictionary<string, object>>().ToList();
            } catch (Exception e) {
                throw e;
            }
        }
    }
}
