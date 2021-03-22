using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Cors;
using LeanCloud.Storage.Internal.Codec;
using LeanCloud.Storage;

namespace LeanCloud.Engine {
    [ApiController]
    [Route("{1,1.1}")]
    [EnableCors(LCEngine.LCEngineCORS)]
    public class LCFunctionController : ControllerBase {
        private Dictionary<string, MethodInfo> Functions => LCEngine.Functions;

        [HttpGet("functions/_ops/metadatas")]
        public object GetFunctions() {
            try {
                return LCEngine.GetFunctions(Request);
            } catch (Exception e) {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost("functions/{funcName}")]
        public async Task<object> Run(string funcName, JsonElement body) {
            try {
                LCLogger.Debug($"Run: {funcName}");
                LCLogger.Debug(body.ToString());

                if (Functions.TryGetValue(funcName, out MethodInfo mi)) {
                    LCUser currentUser = null;
                    if (Request.Headers.TryGetValue("x-lc-session", out StringValues session)) {
                        currentUser = await LCUser.BecomeWithSessionToken(session);
                    }
                    LCCloudFunctionRequest req = new LCCloudFunctionRequest {
                        Meta = new LCCloudFunctionRequestMeta {
                            RemoteAddress = LCEngine.GetIP(Request)
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
            } catch (Exception e) {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost("call/{funcName}")]
        public async Task<object> RPC(string funcName, JsonElement body) {
            try {
                LCLogger.Debug($"RPC: {funcName}");
                LCLogger.Debug(body.ToString());

                if (Functions.TryGetValue(funcName, out MethodInfo mi)) {
                    LCUser currentUser = null;
                    if (Request.Headers.TryGetValue("x-lc-session", out StringValues session)) {
                        currentUser = await LCUser.BecomeWithSessionToken(session);
                    }
                    LCCloudRPCRequest req = new LCCloudRPCRequest {
                        Meta = new LCCloudFunctionRequestMeta {
                            RemoteAddress = LCEngine.GetIP(Request)
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
            } catch (Exception e) {
                return StatusCode(500, e.Message);
            }
        }
    }
}
