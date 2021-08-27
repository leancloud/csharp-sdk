using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud.Common;
using LeanCloud.Storage.Internal.Codec;
using LeanCloud.Storage.Internal.Object;

namespace LeanCloud.Storage {
    /// <summary>
    /// LCCloud contains static functions that call LeanEngine cloud functions.
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
            object encodeParams = LCEncoder.Encode(parameters ?? new Dictionary<string, object>());
            Dictionary<string, object> response = await LCCore.HttpClient.Post<Dictionary<string, object>>(path,
                headers: headers,
                data: encodeParams);
            return response;
        }

        /// <summary>
        /// Invokes a cloud function.
        /// </summary>
        /// <typeparam name="T">The type of return value.</typeparam>
        /// <param name="name">Cloud function name.</param>
        /// <param name="parameters">Parameters of the cloud function.</param>
        /// <returns></returns>
        public static async Task<T> Run<T>(string name,
            Dictionary<string, object> parameters = null) {
            Dictionary<string, object> response = await Run(name, parameters);
            if (response.TryGetValue("result", out object result)) {
                return (T)result;
            }
            return default;
        }

        /// <summary>
        /// Invokes a cloud function as a remote procedure call.
        /// </summary>
        /// <param name="name">Cloud function name.</param>
        /// <param name="parameters">Parameters of cloud function.</param>
        /// <returns>LCObject, List<LCObject>, or Map<string, LCObject>.</returns>
        public static async Task<object> RPC(string name,
            object parameters = null) {
            string path = $"call/{name}";
            Dictionary<string, object> headers = new Dictionary<string, object> {
                { PRODUCTION_KEY, IsProduction ? 1 : 0 }
            };
            object encodeParams = Encode(parameters);
            Dictionary<string, object> response = await LCCore.HttpClient.Post<Dictionary<string, object>>(path,
                headers: headers,
                data: encodeParams);
            return LCDecoder.Decode(response["result"]);
        }

        public static object Encode(object parameters) {
            if (parameters == null) {
                return new Dictionary<string, object>();
            }

            if (parameters is LCObject lcObj) {
                return EncodeLCObject(lcObj);
            }

            if (parameters is IList<LCObject> list) {
                List<object> l = new List<object>();
                foreach (LCObject obj in list) {
                    l.Add(EncodeLCObject(obj));
                }
                return l;
            }

            if (parameters is IDictionary<string, LCObject> dict) {
                Dictionary<string, object> d = new Dictionary<string, object>();
                foreach (KeyValuePair<string, LCObject> item in dict) {
                    d[item.Key] = EncodeLCObject(item.Value);
                }
                return d;
            }

            return parameters;
        }

        static object EncodeLCObject(LCObject obj) {
            Dictionary<string, object> dict = LCEncoder.Encode(obj, true) as Dictionary<string, object>;
            dict["__type"] = "Object";
            return dict;
        }
    }
}
