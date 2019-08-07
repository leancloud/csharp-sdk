using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal {
    public class AVQueryController {
        public Task<IEnumerable<IObjectState>> FindAsync<T>(AVQuery<T> query, AVUser user,
            CancellationToken cancellationToken) where T : AVObject {
            string sessionToken = user != null ? user.SessionToken : null;

            return FindAsync(query.Path, query.BuildParameters(), sessionToken, cancellationToken).OnSuccess(t => {
                var items = t.Result["results"] as IList<object>;

                return (from item in items
                        select AVObjectCoder.Instance.Decode(item as IDictionary<string, object>, AVDecoder.Instance));
            });
        }

        public Task<int> CountAsync<T>(AVQuery<T> query,
            AVUser user,
            CancellationToken cancellationToken) where T : AVObject {
            string sessionToken = user != null ? user.SessionToken : null;
            var parameters = query.BuildParameters();
            parameters["limit"] = 0;
            parameters["count"] = 1;

            return FindAsync(query.Path, parameters, sessionToken, cancellationToken).OnSuccess(t => {
                return Convert.ToInt32(t.Result["count"]);
            });
        }

        public Task<IObjectState> FirstAsync<T>(AVQuery<T> query,
            AVUser user,
            CancellationToken cancellationToken) where T : AVObject {
            string sessionToken = user?.SessionToken;
            var parameters = query.BuildParameters();
            parameters["limit"] = 1;

            return FindAsync(query.Path, parameters, sessionToken, cancellationToken).OnSuccess(t => {
                var items = t.Result["results"] as IList<object>;
                var item = items.FirstOrDefault() as IDictionary<string, object>;

                // Not found. Return empty state.
                if (item == null) {
                    return (IObjectState)null;
                }

                return AVObjectCoder.Instance.Decode(item, AVDecoder.Instance);
            });
        }

        private Task<IDictionary<string, object>> FindAsync(string path,
            IDictionary<string, object> parameters,
            string sessionToken,
            CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = $"{path}?{AVClient.BuildQueryString(parameters)}",
                Method = HttpMethod.Get
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken).OnSuccess(t => {
                return t.Result.Item2;
            });
        }
    }
}
