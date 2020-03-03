using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using LeanCloud.Common;

namespace LeanCloud.Storage.Internal.File {
    internal class LCAWSUploader {
        string uploadUrl;

        string mimeType;

        byte[] data;

        internal LCAWSUploader(string uploadUrl, string mimeType, byte[] data) {
            this.uploadUrl = uploadUrl;
            this.mimeType = mimeType;
            this.data = data;
        }

        internal async Task Upload(Action<long, long> onProgress) {
            LCProgressableStreamContent content = new LCProgressableStreamContent(new ByteArrayContent(data), onProgress);

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
            HttpUtils.PrintRequest(client, request);
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            request.Dispose();

            string resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();
            HttpUtils.PrintResponse(response, resultString);

            HttpStatusCode statusCode = response.StatusCode;
            
        }
    }
}
