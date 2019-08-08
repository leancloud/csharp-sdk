using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Utilities;
using System.Net.Http;

namespace LeanCloud.Storage.Internal {
    public class AVCloudCodeController {
        public Task<T> CallFunctionAsync<T>(String name,
            IDictionary<string, object> parameters,
            string sessionToken,
            CancellationToken cancellationToken) {
            var command = new EngineCommand {
                Path = $"functions/{Uri.EscapeUriString(name)}",
                Method = HttpMethod.Post,
                Content = parameters
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken).OnSuccess(t => {
                var decoded = AVDecoder.Instance.Decode(t.Result.Item2) as IDictionary<string, object>;
                if (!decoded.ContainsKey("result")) {
                    return default(T);
                }
                return Conversion.To<T>(decoded["result"]);
            });
        }

        public Task<T> RPCFunction<T>(string name, IDictionary<string, object> parameters, string sessionToken, CancellationToken cancellationToken) {
            var command = new EngineCommand {
                Path = $"call/{Uri.EscapeUriString(name)}",
                Method = HttpMethod.Post,
                Content = parameters
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken).OnSuccess(t => {
                var decoded = AVDecoder.Instance.Decode(t.Result.Item2) as IDictionary<string, object>;
                if (!decoded.ContainsKey("result")) {
                    return default(T);
                }
                return Conversion.To<T>(decoded["result"]);
            });
        }
    }
}
