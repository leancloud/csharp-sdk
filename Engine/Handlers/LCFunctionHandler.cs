using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using LeanCloud.Storage.Internal.Codec;
using LeanCloud.Storage;

namespace LeanCloud.Engine {
    public class LCFunctionHandler {
        private static Dictionary<string, MethodInfo> Functions => LCEngine.Functions;
        
        /// <summary>
        /// 云函数
        /// </summary>
        /// <param name="funcName"></param>
        /// <param name="request"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static async Task<object> HandleRun(string funcName, HttpRequest request, JsonElement body) {
            LCLogger.Debug($"Run: {funcName}");
            LCLogger.Debug(body.ToString());

            if (Functions.TryGetValue(funcName, out MethodInfo mi)) {
                LCUser currentUser = null;
                if (request.Headers.TryGetValue("x-lc-session", out StringValues session)) {
                    currentUser = await LCUser.BecomeWithSessionToken(session);
                }
                LCCloudFunctionRequest req = new LCCloudFunctionRequest {
                    Meta = new LCCloudFunctionRequestMeta {
                        RemoteAddress = LCEngine.GetIP(request)
                    },
                    Params = LCEngine.Decode(body),
                    SessionToken = session.ToString(),
                    User = currentUser
                };
                object result = await LCEngine.Invoke(mi, req);
                if (result != null) {
                    return new Dictionary<string, object> {
                        { "result", result }
                    };
                }
            }
            return default;
        }

        /// <summary>
        /// RPC
        /// </summary>
        /// <param name="funcName"></param>
        /// <param name="request"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static async Task<object> HandleRPC(string funcName, HttpRequest request, JsonElement body) {
            LCLogger.Debug($"RPC: {funcName}");
            LCLogger.Debug(body.ToString());

            if (Functions.TryGetValue(funcName, out MethodInfo mi)) {
                LCUser currentUser = null;
                if (request.Headers.TryGetValue("x-lc-session", out StringValues session)) {
                    currentUser = await LCUser.BecomeWithSessionToken(session);
                }
                LCCloudRPCRequest req = new LCCloudRPCRequest {
                    Meta = new LCCloudFunctionRequestMeta {
                        RemoteAddress = LCEngine.GetIP(request)
                    },
                    Params = LCDecoder.Decode(LCEngine.Decode(body)),
                    SessionToken = session.ToString(),
                    User = currentUser
                };
                object result = await LCEngine.Invoke(mi, req);
                if (result != null) {
                    return new Dictionary<string, object> {
                        { "result", LCCloud.Encode(result) }
                    };
                }
            }
            return default;
        }
    }
}
