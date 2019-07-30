using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using NetHttpClient = System.Net.Http.HttpClient;

namespace LeanCloud.Storage.Internal {
    public class HttpClient : IHttpClient {
        static readonly HashSet<string> HttpContentHeaders = new HashSet<string> {
            { "Allow" },
            { "Content-Disposition" },
            { "Content-Encoding" },
            { "Content-Language" },
            { "Content-Length" },
            { "Content-Location" },
            { "Content-MD5" },
            { "Content-Range" },
            { "Content-Type" },
            { "Expires" },
            { "Last-Modified" }
        };

        readonly NetHttpClient client;

        public HttpClient() {
            client = new NetHttpClient();
            // TODO 设置版本号
            client.DefaultRequestHeaders.Add("User-Agent", "LeanCloud-dotNet-SDK/" + "2.0.0");
        }

        public HttpClient(NetHttpClient client) {
            this.client = client;
        }

        public async Task<Tuple<HttpStatusCode, string>> ExecuteAsync(HttpRequest httpRequest,
            IProgress<AVUploadProgressEventArgs> uploadProgress,
            IProgress<AVDownloadProgressEventArgs> downloadProgress,
            CancellationToken cancellationToken) {

            HttpMethod httpMethod = new HttpMethod(httpRequest.Method);
            HttpRequestMessage message = new HttpRequestMessage(httpMethod, httpRequest.Uri);

            // Fill in zero-length data if method is post.
            Stream data = httpRequest.Data;
            if (httpRequest.Data == null && httpRequest.Method.ToLower().Equals("post")) {
                data = new MemoryStream(new byte[0]);
            }

            if (data != null) {
                message.Content = new StreamContent(data);
            }

            if (httpRequest.Headers != null) {
                foreach (var header in httpRequest.Headers) {
                    if (!string.IsNullOrEmpty(header.Value)) {
                        if (HttpContentHeaders.Contains(header.Key)) {
                            message.Content.Headers.Add(header.Key, header.Value);
                        } else {
                            message.Headers.Add(header.Key, header.Value);
                        }
                    }
                }
            }

            // Avoid aggressive caching on Windows Phone 8.1.
            message.Headers.Add("Cache-Control", "no-cache");
            message.Headers.IfModifiedSince = DateTimeOffset.UtcNow;

            uploadProgress?.Report(new AVUploadProgressEventArgs { Progress = 0 });
            var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            uploadProgress?.Report(new AVUploadProgressEventArgs { Progress = 1 });

            var resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();

            downloadProgress?.Report(new AVDownloadProgressEventArgs { Progress = 1 });

            return new Tuple<HttpStatusCode, string>(response.StatusCode, resultString);
        }
    }
}
