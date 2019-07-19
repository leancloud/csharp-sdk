using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal
{
    public interface IAVFileController
    {
        Task<FileState> SaveAsync(FileState state,
            Stream dataStream,
            String sessionToken,
            IProgress<AVUploadProgressEventArgs> progress,
            CancellationToken cancellationToken);

        Task DeleteAsync(FileState state,
         string sessionToken,
         CancellationToken cancellationToken);

        Task<FileState> GetAsync(string objectId,
            string sessionToken,
            CancellationToken cancellationToken);
    }
}
