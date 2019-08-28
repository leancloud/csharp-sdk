using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Utilities;
using System.Net.Http;

namespace LeanCloud.Storage.Internal {
    public class AVObjectController {
        public Task<IObjectState> FetchAsync(IObjectState state,
            string sessionToken,
            CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = $"classes/{Uri.EscapeDataString(state.ClassName)}/{Uri.EscapeDataString(state.ObjectId)}",
                Method = HttpMethod.Get
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken).OnSuccess(t => {
                return AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
            });
        }

        public Task<IObjectState> FetchAsync(IObjectState state,
            IDictionary<string, object> queryString,
            string sessionToken,
            CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = $"classes/{Uri.EscapeDataString(state.ClassName)}/{Uri.EscapeDataString(state.ObjectId)}?{AVClient.BuildQueryString(queryString)}",
                Method = HttpMethod.Get
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken).OnSuccess(t => {
                return AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
            });
        }

        public Task<IObjectState> SaveAsync(IObjectState state,
            IDictionary<string, IAVFieldOperation> operations,
            AVQuery<AVObject> query,
            string sessionToken,
            CancellationToken cancellationToken) {
            var objectJSON = AVObject.ToJSONObjectForSaving(operations);
            
            var command = new AVCommand {
                Path = state.ObjectId == null ? $"classes/{Uri.EscapeDataString(state.ClassName)}" : $"classes/{Uri.EscapeDataString(state.ClassName)}/{state.ObjectId}",
                Method = state.ObjectId == null ? HttpMethod.Post : HttpMethod.Put,
                Content = objectJSON
            };
            // 查询条件
            if (query != null && query.where != null) {
                Dictionary<string, object> where = new Dictionary<string, object> {
                    { "where", PointerOrLocalIdEncoder.Instance.Encode(query.where) }
                };
                string encode = AVClient.BuildQueryString(where);
                command.Path = $"{command.Path}?{encode}";
            }
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken).OnSuccess(t => {
                var serverState = AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
                serverState = serverState.MutatedClone(mutableClone => {
                    mutableClone.IsNew = t.Result.Item1 == System.Net.HttpStatusCode.Created;
                });
                return serverState;
            });
        }

        public IList<Task<IObjectState>> SaveAllAsync(IList<IObjectState> states,
            IList<IDictionary<string, IAVFieldOperation>> operationsList,
            string sessionToken,
            CancellationToken cancellationToken) {

            var requests = states
              .Zip(operationsList, (item, ops) => new AVCommand {
                  Path = item.ObjectId == null ? $"classes/{Uri.EscapeDataString(item.ClassName)}" : $"classes/{Uri.EscapeDataString(item.ClassName)}/{Uri.EscapeDataString(item.ObjectId)}",
                  Method = item.ObjectId == null ? HttpMethod.Post : HttpMethod.Put,
                  Content = AVObject.ToJSONObjectForSaving(ops)
              })
              .ToList();

            var batchTasks = ExecuteBatchRequests(requests, sessionToken, cancellationToken);
            var stateTasks = new List<Task<IObjectState>>();
            foreach (var task in batchTasks) {
                stateTasks.Add(task.OnSuccess(t => {
                    return AVObjectCoder.Instance.Decode(t.Result, AVDecoder.Instance);
                }));
            }

            return stateTasks;
        }

        public Task DeleteAsync(IObjectState state,
            string sessionToken,
            CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = $"classes/{state.ClassName}/{state.ObjectId}",
                Method = HttpMethod.Delete
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken);
        }

        public IList<Task> DeleteAllAsync(IList<IObjectState> states,
            string sessionToken,
            CancellationToken cancellationToken) {
            var requests = states
              .Where(item => item.ObjectId != null)
              .Select(item => new AVCommand {
                  Path = $"classes/{Uri.EscapeDataString(item.ClassName)}/{Uri.EscapeDataString(item.ObjectId)}",
                  Method = HttpMethod.Delete
              })
              .ToList();
            return ExecuteBatchRequests(requests, sessionToken, cancellationToken).Cast<Task>().ToList();
        }

        // TODO (hallucinogen): move this out to a class to be used by Analytics
        private const int MaximumBatchSize = 50;
        internal IList<Task<IDictionary<string, object>>> ExecuteBatchRequests(IList<AVCommand> requests,
            string sessionToken,
            CancellationToken cancellationToken) {
            var tasks = new List<Task<IDictionary<string, object>>>();
            int batchSize = requests.Count;

            IEnumerable<AVCommand> remaining = requests;
            while (batchSize > MaximumBatchSize) {
                var process = remaining.Take(MaximumBatchSize).ToList();
                remaining = remaining.Skip(MaximumBatchSize);

                tasks.AddRange(ExecuteBatchRequest(process, sessionToken, cancellationToken));

                batchSize = remaining.Count();
            }
            tasks.AddRange(ExecuteBatchRequest(remaining.ToList(), sessionToken, cancellationToken));

            return tasks;
        }

        private IList<Task<IDictionary<string, object>>> ExecuteBatchRequest(IList<AVCommand> requests,
            string sessionToken,
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
                    { "method", r.Method },
                    { "path", r.Path },
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
            AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken).ContinueWith(t => {
                if (t.IsFaulted || t.IsCanceled) {
                    foreach (var tcs in tcss) {
                        if (t.IsFaulted) {
                            tcs.TrySetException(t.Exception);
                        } else if (t.IsCanceled) {
                            tcs.TrySetCanceled();
                        }
                    }
                    return;
                }

                var resultsArray = Conversion.As<IList<object>>(t.Result.Item2["results"]);
                int resultLength = resultsArray.Count;
                if (resultLength != batchSize) {
                    foreach (var tcs in tcss) {
                        tcs.TrySetException(new InvalidOperationException(
                            "Batch command result count expected: " + batchSize + " but was: " + resultLength + "."));
                    }
                    return;
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
            });

            return tasks;
        }
    }
}
