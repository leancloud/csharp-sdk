using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace LeanCloud.Storage.Internal {
    /// <summary>
    /// AVF ile controller.
    /// </summary>
    public class AVFileController {
        /// <summary>
        /// Saves the async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="state">State.</param>
        /// <param name="dataStream">Data stream.</param>
        /// <param name="sessionToken">Session token.</param>
        /// <param name="progress">Progress.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public virtual async Task<FileState> SaveAsync(FileState state,
            Stream dataStream,
            String sessionToken,
            IProgress<AVUploadProgressEventArgs> progress,
            CancellationToken cancellationToken = default(CancellationToken)) {
            if (state.Url != null) {
                // !isDirty
                return state;
            }

            if (cancellationToken.IsCancellationRequested) {
                return null;
            }

            var oldPosition = dataStream.Position;

            var request = new HttpRequest {
                Uri = new Uri("files/" + state.Name),
                Method = HttpMethod.Post,
                Headers = new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("Content-Type", state.MimeType)
                }
            };
            var ret = await AVPlugins.Instance.HttpClient.ExecuteAsync(request, null, null, CancellationToken.None);
            var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(ret.Item2, new LeanCloudJsonConverter());
            return new FileState {
                Name = jsonData["name"] as string,
                Url = new Uri(jsonData["url"] as string, UriKind.Absolute),
                MimeType = state.MimeType
            };
        }
        public Task DeleteAsync(FileState state, string sessionToken, CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = $"files/{state.ObjectId}",
                Method = HttpMethod.Delete
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken);
        }
        internal Task<Tuple<HttpStatusCode, IDictionary<string, object>>> GetFileToken(FileState fileState, CancellationToken cancellationToken) {
            Task<Tuple<HttpStatusCode, IDictionary<string, object>>> rtn;
            string currentSessionToken = AVUser.CurrentSessionToken;
            string str = fileState.Name;
            IDictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("name", str);
            parameters.Add("key", GetUniqueName(fileState));
            parameters.Add("__type", "File");
            parameters.Add("mime_type", AVFile.GetMIMEType(str));
            parameters.Add("metaData", fileState.MetaData);

            var command = new AVCommand {
                Path = "fileTokens",
                Method = HttpMethod.Post,
                Content = parameters
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
        }
        public Task<FileState> GetAsync(string objectId, string sessionToken, CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = $"files/{objectId}",
                Method = HttpMethod.Get
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken).OnSuccess(_ => {
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
