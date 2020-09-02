using System;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using LeanCloud.Common;

namespace LeanCloud.Storage.Internal.File {
    internal class LCQiniuUploader {
        private string uploadUrl;

        private string token;

        private string key;

        private Stream stream;

        internal LCQiniuUploader(string uploadUrl, string token, string key, Stream stream) {
            this.uploadUrl = uploadUrl;
            this.token = token;
            this.key = key;
            this.stream = stream;
        }

        internal async Task Upload(Action<long, long> onProgress) {
            MultipartFormDataContent dataContent = new MultipartFormDataContent();
            dataContent.Add(new StringContent(key), "key");
            dataContent.Add(new StringContent(token), "token");
            dataContent.Add(new StreamContent(stream), "file");

            LCProgressableStreamContent content = new LCProgressableStreamContent(dataContent, onProgress);

            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(uploadUrl),
                Method = HttpMethod.Post,
                Content = content
            };
            HttpClient client = new HttpClient();
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
