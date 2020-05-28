using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage {
    /// <summary>
    /// 云引擎
    /// </summary>
    public static class LCCloud {
        /// <summary>
        /// 调用云函数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <returns>返回类型为 Dictionary<string, object></returns>
        public static async Task<Dictionary<string, object>> Run(string name,
            Dictionary<string, object> parameters = null) {
            string path = $"functions/{name}";
            object encodeParams = LCEncoder.Encode(parameters);
            Dictionary<string, object> response = await LCApplication.HttpClient.Post<Dictionary<string, object>>(path,
                data: encodeParams);
            return response;
        }

        /// <summary>
        /// 调用 RPC 云函数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <returns>返回类型为 LCObject 容器类型</returns>
        public static async Task<object> RPC(string name, object parameters = null) {
            string path = $"call/{name}";
            object encodeParams = LCEncoder.Encode(parameters);
            Dictionary<string, object> response = await LCApplication.HttpClient.Post<Dictionary<string, object>>(path,
                data: encodeParams);
            return LCDecoder.Decode(response["result"]);
        }
    }
}
