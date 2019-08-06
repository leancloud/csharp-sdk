using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal
{
    public interface IAVCommandRunner
    {
        /// <summary>
        /// Executes <see cref="AVCommand"/> and convert the result into Dictionary.
        /// </summary>
        /// <param name="command">The command to be run.</param>
        /// <param name="uploadProgress">Upload progress callback.</param>
        /// <param name="downloadProgress">Download progress callback.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns></returns>
        Task<Tuple<HttpStatusCode, T>> RunCommandAsync<T>(AVCommand command,
        IProgress<AVUploadProgressEventArgs> uploadProgress = null,
        IProgress<AVDownloadProgressEventArgs> downloadProgress = null,
        CancellationToken cancellationToken = default(CancellationToken));
    }
}
