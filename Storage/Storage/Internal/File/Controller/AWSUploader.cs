using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace LeanCloud.Storage.Internal {
    internal class AWSUploader : IFileUploader {
        public async Task<FileState> Upload(FileState state, Stream dataStream, IDictionary<string, object> fileToken, IProgress<AVUploadProgressEventArgs> progress,
            CancellationToken cancellationToken) {
            var uploadUrl = fileToken["upload_url"].ToString();
            state.ObjectId = fileToken["objectId"].ToString();
            string url = fileToken["url"] as string;
            state.Url = new Uri(url, UriKind.Absolute);
            return await PutFile(state, uploadUrl, dataStream);
        }

        internal async Task<FileState> PutFile(FileState state, string uploadUrl, Stream dataStream) {
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(uploadUrl),
                Method = HttpMethod.Put,
                Content = new StreamContent(dataStream)
            };
            request.Headers.Add("Cache-Control", "public, max-age=31536000");
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(state.MimeType);
            request.Content.Headers.ContentLength = dataStream.Length;
            await client.SendAsync(request);
            client.Dispose();
            request.Dispose();
            return state;
        }
    }
}
