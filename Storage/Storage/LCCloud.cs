﻿using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage {
    /// <summary>
    /// 云引擎
    /// </summary>
    public static class LCCloud {
        /// <summary>
        /// 调用云函数，结果为 Dictionary 类型
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static async Task<Dictionary<string, object>> Run(string name, Dictionary<string, object> parameters = null) {
            string path = $"functions/{name}";
            Dictionary<string, object> response = await LCApplication.HttpClient.Post<Dictionary<string, object>>(path, data: parameters);
            return response;
        }

        public static async Task<object> RPC(string name, Dictionary<string, object> parameters = null) {
            string path = $"call/{name}";
            Dictionary<string, object> response = await LCApplication.HttpClient.Post<Dictionary<string, object>>(path, data: parameters);
            return LCDecoder.Decode(response["result"]);
        }
    }
}
