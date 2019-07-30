using System;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal {
    public static class JsonUtils {
        public static Task<T> DeserializeObjectAsync<T>(string str) {
            var tcs = new TaskCompletionSource<T>();
            Task.Run(() => {
                try {
                    tcs.SetResult(JsonConvert.DeserializeObject<T>(str));
                } catch (Exception e) {
                    tcs.SetException(e);
                }
            });
            return tcs.Task;
        }
    }
}
