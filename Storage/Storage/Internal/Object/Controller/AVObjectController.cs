using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace LeanCloud.Storage.Internal {
    public class AVObjectController {
        public async Task<IObjectState> FetchAsync(IObjectState state,
            IDictionary<string, object> queryString,
            CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = $"classes/{Uri.EscapeDataString(state.ClassName)}/{Uri.EscapeDataString(state.ObjectId)}?{AVClient.BuildQueryString(queryString)}",
                Method = HttpMethod.Get
            };
            var data = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken);
            var objState = AVObjectCoder.Instance.Decode(data.Item2, AVDecoder.Instance);
            return objState;
        }

        public async Task<IObjectState> SaveAsync(IObjectState state,
            IDictionary<string, IAVFieldOperation> operations,
            bool fetchWhenSave,
            AVQuery<AVObject> query,
            CancellationToken cancellationToken) {
            var objectJSON = AVObject.ToJSONObjectForSaving(operations);
            
            var command = new AVCommand {
                Path = state.ObjectId == null ? $"classes/{Uri.EscapeDataString(state.ClassName)}" : $"classes/{Uri.EscapeDataString(state.ClassName)}/{state.ObjectId}",
                Method = state.ObjectId == null ? HttpMethod.Post : HttpMethod.Put,
                Content = objectJSON
            };
            Dictionary<string, object> args = new Dictionary<string, object>();
            if (fetchWhenSave) {
                args.Add("fetchWhenSave", fetchWhenSave);
            }
            // 查询条件
            if (query != null) {
                args.Add("where", query.BuildWhere());
            }
            if (args.Count > 0) {
                string encode = AVClient.BuildQueryString(args);
                command.Path = $"{command.Path}?{encode}";
            }
            var data = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken);
            var serverState = AVObjectCoder.Instance.Decode(data.Item2, AVDecoder.Instance);
            serverState = serverState.MutatedClone(mutableClone => {
                mutableClone.IsNew = data.Item1 == System.Net.HttpStatusCode.Created;
            });
            return serverState;
        }

        public IList<Task<IObjectState>> SaveAllAsync(IList<IObjectState> states,
            IList<IDictionary<string, IAVFieldOperation>> operationsList,
            CancellationToken cancellationToken) {

            var requests = states
              .Zip(operationsList, (item, ops) => new AVCommand {
                  Path = item.ObjectId == null ? $"classes/{Uri.EscapeDataString(item.ClassName)}" : $"classes/{Uri.EscapeDataString(item.ClassName)}/{Uri.EscapeDataString(item.ObjectId)}",
                  Method = item.ObjectId == null ? HttpMethod.Post : HttpMethod.Put,
                  Content = AVObject.ToJSONObjectForSaving(ops)
              })
              .ToList();

            var batchTasks = ExecuteBatchRequests(requests, cancellationToken);
            var stateTasks = new List<Task<IObjectState>>();
            foreach (var task in batchTasks) {
                stateTasks.Add(task.OnSuccess(t => {
                    return AVObjectCoder.Instance.Decode(t.Result, AVDecoder.Instance);
                }));
            }

            return stateTasks;
        }

        public async Task DeleteAsync(IObjectState state, AVQuery<AVObject> query, CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = $"classes/{state.ClassName}/{state.ObjectId}",
                Method = HttpMethod.Delete
            };
            if (query != null) {
                Dictionary<string, object> where = new Dictionary<string, object> {
                    { "where", query.BuildWhere() }
                };
                command.Path = $"{command.Path}?{AVClient.BuildQueryString(where)}";
            }
            await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken);
        }

        public IList<Task> DeleteAllAsync(IList<IObjectState> states,
            CancellationToken cancellationToken) {
            var requests = states
              .Where(item => item.ObjectId != null)
              .Select(item => new AVCommand {
                  Path = $"classes/{Uri.EscapeDataString(item.ClassName)}/{Uri.EscapeDataString(item.ObjectId)}",
                  Method = HttpMethod.Delete
              })
              .ToList();
            return ExecuteBatchRequests(requests, cancellationToken).Cast<Task>().ToList();
        }

        // TODO (hallucinogen): move this out to a class to be used by Analytics
        private const int MaximumBatchSize = 50;

        internal IList<Task<IDictionary<string, object>>> ExecuteBatchRequests(IList<AVCommand> requests,
            CancellationToken cancellationToken) {
            var tasks = new List<Task<IDictionary<string, object>>>();
            int batchSize = requests.Count;

            IEnumerable<AVCommand> remaining = requests;
            while (batchSize > MaximumBatchSize) {
                var process = remaining.Take(MaximumBatchSize).ToList();
                remaining = remaining.Skip(MaximumBatchSize);

                tasks.AddRange(ExecuteBatchRequest(process, cancellationToken));

                batchSize = remaining.Count();
            }
            tasks.AddRange(ExecuteBatchRequest(remaining.ToList(), cancellationToken));

            return tasks;
        }

        private async Task<IList<Task<IDictionary<string, object>>>> ExecuteBatchRequest(IList<AVCommand> requests,
            CancellationToken cancellationToken) {
            var tasks = new List<Task<IDictionary<string, object>>>();
            int batchSize = requests.Count;
            var tcss = new List<TaskCompletionSource<IDictionary<string, object>>>();
            for (int i = 0; i < batchSize; ++i) {
                var tcs = new TaskCompletionSource<IDictionary<string, object>>();
                tcss.Add(tcs);
                tasks.Add(tcs.Task);
            }

            var encodedRequests = requests.Select(r => {
                var results = new Dictionary<string, object> {
                    { "method", r.Method.Method },
                    { "path", $"/{AVClient.APIVersion}/{r.Path}" },
                };

                if (r.Content != null) {
                    results["body"] = r.Content;
                }
                return results;
            }).Cast<object>().ToList();
            var command = new AVCommand {
                Path = "batch",
                Method = HttpMethod.Post,
                Content = new Dictionary<string, object> {
                    { "requests", encodedRequests }
                }
            };

            try {
                var response = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IList<object>>(command, cancellationToken);
                var resultsArray = response.Item2;
                int resultLength = resultsArray.Count;
                if (resultLength != batchSize) {
                    foreach (var tcs in tcss) {
                        tcs.TrySetException(new InvalidOperationException(
                            "Batch command result count expected: " + batchSize + " but was: " + resultLength + "."));
                    }
                }

                for (int i = 0; i < batchSize; ++i) {
                    var result = resultsArray[i] as Dictionary<string, object>;
                    var tcs = tcss[i];

                    if (result.ContainsKey("success")) {
                        tcs.TrySetResult(result["success"] as IDictionary<string, object>);
                    } else if (result.ContainsKey("error")) {
                        var error = result["error"] as IDictionary<string, object>;
                        long errorCode = long.Parse(error["code"].ToString());
                        tcs.TrySetException(new AVException((AVException.ErrorCode)errorCode, error["error"] as string));
                    } else {
                        tcs.TrySetException(new InvalidOperationException(
                            "Invalid batch command response."));
                    }
                }
            } catch (Exception e) {
                foreach (var tcs in tcss) {
                    tcs.TrySetException(e);
                }
            }
        }
    }
}
