using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Utilities;
using System.Net.Http;

namespace LeanCloud.Storage.Internal {
    public class AVCloudCodeController {
        public async Task<T> CallFunctionAsync<T>(string name,
            IDictionary<string, object> parameters,
            CancellationToken cancellationToken) {
            var command = new EngineCommand {
                Path = $"functions/{Uri.EscapeUriString(name)}",
                Method = HttpMethod.Post,
                Content = parameters ?? new Dictionary<string, object>()
            };
            var data = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken);
            var decoded = AVDecoder.Instance.Decode(data.Item2) as IDictionary<string, object>;
            if (!decoded.ContainsKey("result")) {
                return default;
            }
            return Conversion.To<T>(decoded["result"]);
        }

        public async Task<T> RPCFunction<T>(string name, IDictionary<string, object> parameters, CancellationToken cancellationToken) {
            var command = new EngineCommand {
                Path = $"call/{Uri.EscapeUriString(name)}",
                Method = HttpMethod.Post,
                Content = parameters ?? new Dictionary<string, object>()
            };
            var data = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken);
            var decoded = AVDecoder.Instance.Decode(data.Item2) as IDictionary<string, object>;
            if (!decoded.ContainsKey("result")) {
                return default;
            }
            return Conversion.To<T>(decoded["result"]);
        }
    }
}
