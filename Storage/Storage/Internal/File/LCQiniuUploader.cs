using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using LeanCloud.Common;

namespace LeanCloud.Storage.Internal.File {
    internal class LCQiniuUploader {
        string uploadUrl;

        string token;

        string key;

        byte[] data;

        internal LCQiniuUploader(string uploadUrl, string token, string key, byte[] data) {
            this.uploadUrl = uploadUrl;
            this.token = token;
            this.key = key;
            this.data = data;
        }

        internal async Task Upload(Action<int, int> onProgress) {
            MultipartFormDataContent content = new MultipartFormDataContent();
            content.Add(new StringContent(key), "key");
            content.Add(new StringContent(token), "token");
            content.Add(new ByteArrayContent(data), "file");
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(uploadUrl),
                Method = HttpMethod.Post,
                Content = content
            };
            HttpClient client = new HttpClient();
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
