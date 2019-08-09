using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;

namespace LeanCloud.Storage.Internal {
    public abstract class AVFileController {
        public abstract Task<FileState> SaveAsync(FileState state,
            Stream dataStream,
            String sessionToken,
            IProgress<AVUploadProgressEventArgs> progress,
            CancellationToken cancellationToken = default(CancellationToken));

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
