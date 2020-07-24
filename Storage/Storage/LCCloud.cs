using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage {
    /// <summary>
    /// LeanEngine
    /// </summary>
    public static class LCCloud {
        private const string PRODUCTION_KEY = "X-LC-Prod";

        /// <summary>
        /// Whether using production environment (default) or staging environment.
        /// </summary>
        public static bool IsProduction {
            get; set;
        } = true;

        /// <summary>
        /// Invokes a cloud function.
        /// </summary>
        /// <param name="name">Cloud function name.</param>
        /// <param name="parameters">Parameters of cloud function.</param>
        /// <returns>Dictionary<string, object> or List<object>.</returns>
        public static async Task<Dictionary<string, object>> Run(string name,
            Dictionary<string, object> parameters = null) {
            string path = $"functions/{name}";
            Dictionary<string, object> headers = new Dictionary<string, object> {
                { PRODUCTION_KEY, IsProduction ? 1 : 0 }
            };
            object encodeParams = LCEncoder.Encode(parameters);
            Dictionary<string, object> response = await LCApplication.HttpClient.Post<Dictionary<string, object>>(path,
                headers: headers,
                data: encodeParams);
            return response;
        }

        /// <summary>
        /// Invokes a cloud function as a remote procedure call.
        /// </summary>
        /// <param name="name">Cloud function name.</param>
        /// <param name="parameters">Parameters of cloud function.</param>
        /// <returns>LCObject, List<LCObject>, or Map<string, LCObject>.</returns>
        public static async Task<object> RPC(string name, object parameters = null) {
            string path = $"call/{name}";
            Dictionary<string, object> headers = new Dictionary<string, object> {
                { PRODUCTION_KEY, IsProduction ? 1 : 0 }
            };
            object encodeParams = LCEncoder.Encode(parameters);
            Dictionary<string, object> response = await LCApplication.HttpClient.Post<Dictionary<string, object>>(path,
                headers: headers,
                data: encodeParams);
            return LCDecoder.Decode(response["result"]);
        }
    }
}
