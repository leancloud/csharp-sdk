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

        public async Task<IList<IObjectState>> SaveAllAsync(IList<IObjectState> states,
            IList<IDictionary<string, IAVFieldOperation>> operationsList,
            CancellationToken cancellationToken) {
            var requests = states
              .Zip(operationsList, (item, ops) => new AVCommand {
                  Path = item.ObjectId == null ? $"classes/{Uri.EscapeDataString(item.ClassName)}" : $"classes/{Uri.EscapeDataString(item.ClassName)}/{Uri.EscapeDataString(item.ObjectId)}",
                  Method = item.ObjectId == null ? HttpMethod.Post : HttpMethod.Put,
                  Content = AVObject.ToJSONObjectForSaving(ops)
              })
              .ToList();
            IList<IObjectState> list = new List<IObjectState>();
            var result = await AVPlugins.Instance.CommandRunner.ExecuteBatchRequests(requests, cancellationToken);
            foreach (var data in result) {
                if (data.TryGetValue("success", out object val)) {
                    IObjectState obj = AVObjectCoder.Instance.Decode(val as IDictionary<string, object>, AVDecoder.Instance);
                    list.Add(obj);
                }
            }
            return list;
        }

        public async Task<IList<IObjectState>> SaveAllAsync(IList<AVObject> avObjects, CancellationToken cancellationToken) {
            List<AVCommand> commandList = new List<AVCommand>();
            foreach (AVObject avObj in avObjects) {
                AVCommand command = new AVCommand {
                    Path = avObj.ObjectId == null ? $"classes/{Uri.EscapeDataString(avObj.ClassName)}" : $"classes/{Uri.EscapeDataString(avObj.ClassName)}/{Uri.EscapeDataString(avObj.ObjectId)}",
                    Method = avObj.ObjectId == null ? HttpMethod.Post : HttpMethod.Put,
                    Content = AVObject.ToJSONObjectForSaving(avObj.operationDict)
                };
                commandList.Add(command);
            }
            IList<IObjectState> list = new List<IObjectState>();
            var result = await AVPlugins.Instance.CommandRunner.ExecuteBatchRequests(commandList, cancellationToken);
            foreach (var data in result) {
                if (data.TryGetValue("success", out object val)) {
                    IObjectState obj = AVObjectCoder.Instance.Decode(val as IDictionary<string, object>, AVDecoder.Instance);
                    list.Add(obj);
                }
            }
            return list;
        }
    }
}
