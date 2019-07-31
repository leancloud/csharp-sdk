using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using LeanCloud.Storage.Internal;
using System.Collections.Generic;
using System.Net.Http;

namespace LeanCloud.Storage.Internal
{
    internal class AWSS3FileController : AVFileController
    {

        private object mutex = new object();


        public AWSS3FileController(IAVCommandRunner commandRunner) : base(commandRunner)
        {

        }

        public override Task<FileState> SaveAsync(FileState state, Stream dataStream, string sessionToken, IProgress<AVUploadProgressEventArgs> progress, CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            if (state.Url != null)
            {
                return Task.FromResult(state);
            }

            return GetFileToken(state, cancellationToken).OnSuccess(t =>
            {
                var fileToken = t.Result.Item2;
                var uploadUrl = fileToken["upload_url"].ToString();
                state.ObjectId = fileToken["objectId"].ToString();
                string url = fileToken["url"] as string;
                state.Url = new Uri(url, UriKind.Absolute);
                return PutFile(state, uploadUrl, dataStream);

            }).Unwrap().OnSuccess(s =>
            {
                return s.Result;
            });
        }

        internal async Task<FileState> PutFile(FileState state, string uploadUrl, Stream dataStream)
        {
            IList<KeyValuePair<string, string>> makeBlockHeaders = new List<KeyValuePair<string, string>>();
            makeBlockHeaders.Add(new KeyValuePair<string, string>("Content-Type", state.MimeType));
            var request = new HttpRequest {
                Uri = new Uri(uploadUrl),
                Method = HttpMethod.Put,
                Headers = makeBlockHeaders,
                Data = dataStream
            };
            await AVPlugins.Instance.HttpClient.ExecuteAsync(request, null, null, CancellationToken.None);
            return state;
        }
    }
}
