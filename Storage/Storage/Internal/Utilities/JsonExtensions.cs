using System;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal {
    /// <summary>
    /// 为 Json 解析提供异步接口
    /// </summary>
    public static class JsonUtils {
        public static async Task<string> SerializeObjectAsync(object obj) {
            string str = null;
            await Task.Run(() => {
                str = JsonConvert.SerializeObject(obj);
            });
            return str;
        }

        public static Task<string> SerializeAsync(object obj) {
            return Task.Run(() => {
                return JsonConvert.SerializeObject(obj);
            });
        }

        public static async Task<T> DeserializeObjectAsync<T>(string str, params JsonConverter[] converters) {
            T obj = default;
            await Task.Run(() => {
                obj = JsonConvert.DeserializeObject<T>(str, converters);
            });
            return obj;
        }

        public static Task<T> DeserializeAsync<T>(string str, params JsonConverter[] converts) {
            return Task.Run(() => {
                return JsonConvert.DeserializeObject<T>(str, converts);
            });
        }
    }
}
