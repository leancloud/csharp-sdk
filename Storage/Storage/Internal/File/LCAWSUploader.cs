using System;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using LeanCloud.Common;

namespace LeanCloud.Storage.Internal.File {
    internal class LCAWSUploader {
        private string uploadUrl;

        private string mimeType;

        private Stream stream;

        internal LCAWSUploader(string uploadUrl, string mimeType, Stream stream) {
            this.uploadUrl = uploadUrl;
            this.mimeType = mimeType;
            this.stream = stream;
        }

        internal async Task Upload(Action<long, long> onProgress) {
            LCProgressableStreamContent content = new LCProgressableStreamContent(new StreamContent(stream), onProgress);

            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(uploadUrl),
                Method = HttpMethod.Put,
                Content = content
            };
            HttpClient client = new HttpClient();
            request.Headers.CacheControl = new CacheControlHeaderValue {
                Public = true,
                MaxAge = TimeSpan.FromMilliseconds(31536000)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            LCHttpUtils.PrintRequest(client, request);
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            request.Dispose();

            string resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();
            LCHttpUtils.PrintResponse(response, resultString);

            HttpStatusCode statusCode = response.StatusCode;
            
        }
    }
}
