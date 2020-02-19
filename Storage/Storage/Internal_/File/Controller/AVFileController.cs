using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;

namespace LeanCloud.Storage.Internal {
    public interface IFileUploader {
        Task<FileState> Upload(FileState state, Stream dataStream, IDictionary<string, object> fileToken, IProgress<AVUploadProgressEventArgs> progress,
            CancellationToken cancellationToken);
    }

    public class AVFileController {
        const string QCloud = "qcloud";
        const string AWS = "s3";

        //public async Task<FileState> SaveAsync(FileState state,
        //    Stream dataStream,
        //    IProgress<AVUploadProgressEventArgs> progress,
        //    CancellationToken cancellationToken = default) {
        //    if (state.Url != null) {
        //        return await SaveWithUrl(state);
        //    }

        //    var data = await GetFileToken(state, cancellationToken);
        //    var fileToken = data.Item2;
        //    var provider = fileToken["provider"] as string;
        //    switch (provider) {
        //        case QCloud:
        //            return await new QCloudUploader().Upload(state, dataStream, fileToken, progress, cancellationToken);
        //        case AWS:
        //            return await new AWSUploader().Upload(state, dataStream, fileToken, progress, cancellationToken);
        //        default:
        //            return await new QiniuUploader().Upload(state, dataStream, fileToken, progress, cancellationToken);
        //    }
        //}

        public async Task DeleteAsync(FileState state, CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = $"files/{state.ObjectId}",
                Method = HttpMethod.Delete
            };
            await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken);
        }

        internal async Task<FileState> SaveWithUrl(FileState state) {
            Dictionary<string, object> strs = new Dictionary<string, object> {
                { "url", state.Url.ToString() },
                { "name", state.Name },
                { "mime_type", state.MimeType },
                { "metaData", state.MetaData }
            };
            AVCommand cmd = null;

            if (!string.IsNullOrEmpty(state.ObjectId)) {
                cmd = new AVCommand {
                    Path = $"files/{state.ObjectId}",
                    Method = HttpMethod.Put,
                    Content = strs
                };
            } else {
                cmd = new AVCommand {
                    Path = "files",
                    Method = HttpMethod.Post,
                    Content = strs
                };
            }

            var data = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(cmd);
            var result = data.Item2;
            state.ObjectId = result["objectId"].ToString();
            return state;
        }

        internal async Task<Tuple<HttpStatusCode, IDictionary<string, object>>> GetFileToken(string name, IDictionary<string, object> metaData, CancellationToken cancellationToken = default) {
            IDictionary<string, object> parameters = new Dictionary<string, object> {
                { "name", name },
                { "key", GetUniqueName(name) },
                { "__type", "File" },
                { "mime_type", AVFile.GetMIMEType(name) },
                { "metaData", metaData }
            };

            var command = new AVCommand {
                Path = "fileTokens",
                Method = HttpMethod.Post,
                Content = parameters
            };
            return await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken);
        }

        public async Task<FileState> GetAsync(string objectId, CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = $"files/{objectId}",
                Method = HttpMethod.Get
            };
            var data = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken);
            var jsonData = data.Item2;
            cancellationToken.ThrowIfCancellationRequested();
            return new FileState {
                ObjectId = jsonData["objectId"] as string,
                Name = jsonData["name"] as string,
                Url = new Uri(jsonData["url"] as string, UriKind.Absolute),
            };
        }

        internal static string GetUniqueName(string name) {
            string key = Guid.NewGuid().ToString();
            string extension = Path.GetExtension(name);
            key += extension;
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
