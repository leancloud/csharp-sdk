using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

namespace LeanCloud.Storage.Internal
{
    /// <summary>
    /// Command Runner.
    /// </summary>
    public class AVCommandRunner : IAVCommandRunner {
        public const string APPLICATION_JSON = "application/json";

        private readonly System.Net.Http.HttpClient httpClient;
        private readonly IInstallationIdController installationIdController;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="installationIdController"></param>
        public AVCommandRunner(IHttpClient httpClient, IInstallationIdController installationIdController)
        {
            this.httpClient = new System.Net.Http.HttpClient();
            this.installationIdController = installationIdController;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="uploadProgress"></param>
        /// <param name="downloadProgress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Tuple<HttpStatusCode, IDictionary<string, object>>> RunCommandAsync(AVCommand command,
            IProgress<AVUploadProgressEventArgs> uploadProgress = null,
            IProgress<AVDownloadProgressEventArgs> downloadProgress = null,
            CancellationToken cancellationToken = default(CancellationToken)) {
            
            var request = new HttpRequestMessage {
                RequestUri = command.Uri,
                Method = command.Method,
                Content = new StringContent(Json.Encode(command.Content))
            };

            var headers = await GetHeadersAsync();
            foreach (var header in headers) {
                if (!string.IsNullOrEmpty(header.Value)) {
                    request.Headers.Add(header.Key, header.Value);
                }
            }
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(APPLICATION_JSON);

            PrintRequest(command, headers);
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

            if (contentString != null) {
                IDictionary<string, object> contentJson = null;
                try {
                    if (contentString.StartsWith("[", StringComparison.Ordinal)) {
                        var arrayJson = Json.Parse(contentString);
                        contentJson = new Dictionary<string, object> { { "results", arrayJson } };
                    } else {
                        contentJson = Json.Parse(contentString) as IDictionary<string, object>;
                    }
                } catch (Exception e) {
                    throw new AVException(AVException.ErrorCode.OtherCause,
                        "Invalid response from server", e);
                }
                if (responseCode < HttpStatusCode.OK || responseCode > HttpStatusCode.PartialContent) {
                    AVClient.PrintLog("error response code:" + responseCode);
                    int code = (int)(contentJson.ContainsKey("code") ? (int)contentJson["code"] : (int)AVException.ErrorCode.OtherCause);
                    string error = contentJson.ContainsKey("error") ?
                        contentJson["error"] as string : contentString;
                    AVException.ErrorCode ec = (AVException.ErrorCode)code;
                    throw new AVException(ec, error);
                }
                return new Tuple<HttpStatusCode, IDictionary<string, object>>(responseCode, contentJson);
            }

            return new Tuple<HttpStatusCode, IDictionary<string, object>>(responseCode, null);
        }

        private const string revocableSessionTokenTrueValue = "1";

        async Task<Dictionary<string, string>> GetHeadersAsync() {
            var headers = new Dictionary<string, string>();
            var installationId = await installationIdController.GetAsync();
            headers.Add("X-LC-Installation-Id", installationId.ToString());
            var conf = AVClient.CurrentConfiguration;
            headers.Add("X-LC-Id", conf.ApplicationId);
            long timestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
            if (!string.IsNullOrEmpty(conf.MasterKey) && AVClient.UseMasterKey) {
                string sign = MD5.GetMd5String(timestamp + conf.MasterKey);
                headers.Add("X-LC-Sign", $"{sign},{timestamp},master");
            } else {
                string sign = MD5.GetMd5String(timestamp + conf.ApplicationKey);
                headers.Add("X-LC-Sign", $"{sign},{timestamp}");
            }
            // TODO 重新设计版本号
            headers.Add("X-LC-Client-Version", AVClient.VersionString);
            headers.Add("X-LC-App-Build-Version", conf.VersionInfo.BuildVersion);
            headers.Add("X-LC-App-Display-Version", conf.VersionInfo.DisplayVersion);
            headers.Add("X-LC-OS-Version", conf.VersionInfo.OSVersion);
            headers.Add("X-LeanCloud-Revocable-Session", revocableSessionTokenTrueValue);
            return headers;
        }

        static void PrintRequest(AVCommand request, Dictionary<string, string> headers) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== HTTP Request Start ===");
            sb.AppendLine($"URL: {request.Uri}");
            sb.AppendLine($"Method: {request.Method}");
            sb.AppendLine($"Headers: {JsonConvert.SerializeObject(headers)}");
            sb.AppendLine($"Content: {JsonConvert.SerializeObject(request.Content)}");
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
