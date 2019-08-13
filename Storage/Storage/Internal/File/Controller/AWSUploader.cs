using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;

namespace LeanCloud.Storage.Internal {
    internal class AWSUploader : IFileUploader {
        public Task<FileState> Upload(FileState state, Stream dataStream, IDictionary<string, object> fileToken, IProgress<AVUploadProgressEventArgs> progress,
            CancellationToken cancellationToken) {
            var uploadUrl = fileToken["upload_url"].ToString();
            state.ObjectId = fileToken["objectId"].ToString();
            string url = fileToken["url"] as string;
            state.Url = new Uri(url, UriKind.Absolute);
            return PutFile(state, uploadUrl, dataStream).OnSuccess(s => {
                return s.Result;
            });
        }

        internal async Task<FileState> PutFile(FileState state, string uploadUrl, Stream dataStream) {
            IList<KeyValuePair<string, string>> makeBlockHeaders = new List<KeyValuePair<string, string>>();
            makeBlockHeaders.Add(new KeyValuePair<string, string>("Content-Type", state.MimeType));

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(uploadUrl),
                Method = HttpMethod.Put,
                Content = new StreamContent(dataStream)
            };
            foreach (var header in makeBlockHeaders) {
                request.Headers.Add(header.Key, header.Value);
            }
            await client.SendAsync(request);
            client.Dispose();
            request.Dispose();
            return state;
        }
    }
}
