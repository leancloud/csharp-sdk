using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using LeanCloud.Storage;
using LeanCloud.Storage.Internal.Codec;

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
                return StatusCode(500, LCEngine.ConvertException(e));
            }
        }

        [HttpPost("functions/{funcName}")]
        public async Task<object> Run(string funcName, JsonElement body) {
            try {
                if (LCLogger.LogDelegate != null) {
                    LCLogger.Debug($"Run: {funcName}");
                    LCLogger.Debug(body.ToString());
                }

                if (Functions.TryGetValue(funcName, out MethodInfo mi)) {
                    LCEngine.InitRequestContext(Request);

                    object[] ps = ParseParameters(mi, body);
                    object result = await LCEngine.Invoke(mi, ps.ToArray());

                    if (result != null) {
                        return new Dictionary<string, object> {
                            { "result", result }
                        };
                    }
                }
                return body;
            } catch (LCEngineException e) {
                return StatusCode(e.Status, LCEngine.ConvertException(e));
            } catch (Exception e) {
                LCEngine.LogException(funcName, e);
                return StatusCode(500, LCEngine.ConvertException(e));
            }
        }

        [HttpPost("call/{funcName}")]
        public async Task<object> RPC(string funcName, JsonElement body) {
            try {
                if (LCLogger.LogDelegate != null) {
                    LCLogger.Debug($"RPC: {funcName}");
                    LCLogger.Debug(body.ToString());
                }

                if (Functions.TryGetValue(funcName, out MethodInfo mi)) {
                    LCEngine.InitRequestContext(Request);

                    object[] ps = ParseParameters(mi, body);
                    object result = await LCEngine.Invoke(mi, ps);

                    if (result != null) {
                        return new Dictionary<string, object> {
                            { "result", LCCloud.Encode(result) }
                        };
                    }
                }
                return body;
            } catch (LCEngineException e) {
                return StatusCode(e.Status, LCEngine.ConvertException(e));
            } catch (Exception e) {
                LCEngine.LogException(funcName, e);
                return StatusCode(500, LCEngine.ConvertException(e));
            }
        }

        private static object[] ParseParameters(MethodInfo mi, JsonElement body) {
            Dictionary<string, object> parameters = LCEngine.Decode(body);
            List<object> ps = new List<object>();

            if (mi.GetParameters().Length > 0) {
                if (Array.Exists(mi.GetParameters(),
                    p => p.GetCustomAttribute<LCEngineFunctionParamAttribute>() != null)) {
                    // 如果包含 LCEngineFunctionParamAttribute 的参数，则按照配对方式传递参数
                    foreach (ParameterInfo pi in mi.GetParameters()) {
                        LCEngineFunctionParamAttribute attr = pi.GetCustomAttribute<LCEngineFunctionParamAttribute>();
                        if (attr != null) {
                            string paramName = attr.ParamName;
                            ps.Add(parameters[paramName]);
                        }
                    }
                } else {
                    ps.Add(LCDecoder.Decode(LCEngine.Decode(body)));
                }
            }

            return ps.ToArray();
        }
    }
}
