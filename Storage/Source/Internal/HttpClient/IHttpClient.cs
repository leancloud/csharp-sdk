using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal
{
    public interface IHttpClient
    {
        /// <summary>
        /// Executes HTTP request to a <see cref="HttpRequest.Uri"/> with <see cref="HttpRequest.Method"/> HTTP verb
        /// and <see cref="HttpRequest.Headers"/>.
        /// </summary>
        /// <param name="httpRequest">The HTTP request to be executed.</param>
        /// <param name="uploadProgress">Upload progress callback.</param>
        /// <param name="downloadProgress">Download progress callback.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that resolves to Htt</returns>
        Task<Tuple<HttpStatusCode, string>> ExecuteAsync(HttpRequest httpRequest,
            IProgress<AVUploadProgressEventArgs> uploadProgress,
            IProgress<AVDownloadProgressEventArgs> downloadProgress,
            CancellationToken cancellationToken);
    }
}
