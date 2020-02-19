using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal {
    public class AVQueryController {
        public async Task<IEnumerable<IObjectState>> FindAsync<T>(AVQuery<T> query, CancellationToken cancellationToken) where T : AVObject {
            IList<object> items = await FindAsync<IList<object>>(query.Path, query.BuildParameters(), "results", cancellationToken);
            return items.Select(item => AVObjectCoder.Instance.Decode(item as IDictionary<string, object>, AVDecoder.Instance));
        }

        public async Task<int> CountAsync<T>(AVQuery<T> query, CancellationToken cancellationToken) where T : AVObject {
            var parameters = query.BuildParameters();
            parameters["limit"] = 0;
            parameters["count"] = 1;
            long ret = await FindAsync<long>(query.Path, parameters, "count", cancellationToken);
            return Convert.ToInt32(ret);
        }

        public async Task<IObjectState> FirstAsync<T>(AVQuery<T> query, CancellationToken cancellationToken) where T : AVObject {
            var parameters = query.BuildParameters();
            parameters["limit"] = 1;
            IList<object> items = await FindAsync<IList<object>>(query.Path, query.BuildParameters(), "results", cancellationToken);
            // Not found. Return empty state.
            if (!(items.FirstOrDefault() is IDictionary<string, object> item)) {
                return (IObjectState)null;
            }
            return AVObjectCoder.Instance.Decode(item, AVDecoder.Instance);
        }

        private async Task<T> FindAsync<T>(string path,
            IDictionary<string, object> parameters,
            string key,
            CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = $"{path}?{AVClient.BuildQueryString(parameters)}",
                Method = HttpMethod.Get
            };
            var result = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken);
            return (T)result.Item2[key];
        }
    }
}
