using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal;
using System.Net;
using System.Collections.Generic;
using System.Linq;

namespace LeanCloud.Storage.Internal {
    /// <summary>
    /// AVF ile controller.
    /// </summary>
    public class AVFileController : IAVFileController {
        private readonly IAVCommandRunner commandRunner;
        /// <summary>
        /// Initializes a new instance of the <see cref="T:LeanCloud.Storage.Internal.AVFileController"/> class.
        /// </summary>
        /// <param name="commandRunner">Command runner.</param>
        public AVFileController(IAVCommandRunner commandRunner) {
            this.commandRunner = commandRunner;
        }
        /// <summary>
        /// Saves the async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="state">State.</param>
        /// <param name="dataStream">Data stream.</param>
        /// <param name="sessionToken">Session token.</param>
        /// <param name="progress">Progress.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public virtual Task<FileState> SaveAsync(FileState state,
            Stream dataStream,
            string sessionToken,
            IProgress<AVUploadProgressEventArgs> progress,
            CancellationToken cancellationToken = default) {
            if (state.Url != null) {
                // !isDirty
                return Task<FileState>.FromResult(state);
            }

            if (cancellationToken.IsCancellationRequested) {
                var tcs = new TaskCompletionSource<FileState>();
                tcs.TrySetCanceled();
                return tcs.Task;
            }

            var oldPosition = dataStream.Position;
            var command = new AVCommand("files/" + state.Name,
                method: "POST",
                sessionToken: sessionToken,
                contentType: state.MimeType,
                stream: dataStream);

            return commandRunner.RunCommandAsync(command,
                uploadProgress: progress,
                cancellationToken: cancellationToken).OnSuccess(uploadTask => {
                    var result = uploadTask.Result;
                    var jsonData = result.Item2;
                    cancellationToken.ThrowIfCancellationRequested();

                    return new FileState {
                        Name = jsonData["name"] as string,
                        Url = new Uri(jsonData["url"] as string, UriKind.Absolute),
                        MimeType = state.MimeType
                    };
                }).ContinueWith(t => {
                    // Rewind the stream on failure or cancellation (if possible)
                    if ((t.IsFaulted || t.IsCanceled) && dataStream.CanSeek) {
                        dataStream.Seek(oldPosition, SeekOrigin.Begin);
                    }
                    return t;
                }).Unwrap();
        }
        public Task DeleteAsync(FileState state, string sessionToken, CancellationToken cancellationToken) {
            var command = new AVCommand("files/" + state.ObjectId,
               method: "DELETE",
               sessionToken: sessionToken,
               data: null);

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken);
        }
        internal static Task<Tuple<HttpStatusCode, IDictionary<string, object>>> GetFileToken(FileState fileState, CancellationToken cancellationToken) {
            Task<Tuple<HttpStatusCode, IDictionary<string, object>>> rtn;
            string currentSessionToken = AVUser.CurrentSessionToken;
            string str = fileState.Name;
            IDictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("name", str);
            parameters.Add("key", GetUniqueName(fileState));
            parameters.Add("__type", "File");
            parameters.Add("mime_type", AVFile.GetMIMEType(str));
            parameters.Add("metaData", fileState.MetaData);

            rtn = AVClient.RequestAsync("POST", new Uri("fileTokens", UriKind.Relative), currentSessionToken, parameters, cancellationToken);

            return rtn;
        }
        public Task<FileState> GetAsync(string objectId, string sessionToken, CancellationToken cancellationToken) {
            var command = new AVCommand("files/" + objectId,
                method: "GET",
                sessionToken: sessionToken,
                data: null);

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(_ => {
                var result = _.Result;
                var jsonData = result.Item2;
                cancellationToken.ThrowIfCancellationRequested();
                return new FileState {
                    ObjectId = jsonData["objectId"] as string,
                    Name = jsonData["name"] as string,
                    Url = new Uri(jsonData["url"] as string, UriKind.Absolute),
                };
            });
        }
        internal static string GetUniqueName(FileState fileState) {
            string key = Random(12);
            string extension = Path.GetExtension(fileState.Name);
            key += extension;
            fileState.CloudName = key;
            return key;
        }
        internal static string Random(int length) {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        internal static double CalcProgress(double already, double total) {
            var pv = (1.0 * already / total);
            return Math.Round(pv, 3);
        }
    }
}
