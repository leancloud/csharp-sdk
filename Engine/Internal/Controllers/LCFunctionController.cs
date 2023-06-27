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
                    LCEngineRequestContext context = new LCEngineRequestContext(Request);
                    object[] ps = ParseParameters(mi, context, body);
                    object result = await LCEngine.Invoke(mi, ps);

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
                    LCEngineRequestContext context = new LCEngineRequestContext(Request);
                    object[] ps = ParseParameters(mi, context, body);
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

        private static object[] ParseParameters(MethodInfo mi, LCEngineRequestContext context, JsonElement body) {
            List<object> ps = new List<object>();

            Dictionary<string, object> parameters = LCEngine.Decode(body);

            foreach (ParameterInfo pi in mi.GetParameters()) {
                if (pi.ParameterType == typeof(LCEngineRequestContext)) {
                    ps.Add(context);
                } else {
                    LCEngineFunctionParamAttribute attr = pi.GetCustomAttribute<LCEngineFunctionParamAttribute>();
                    if (attr == null) {
                        throw new ArgumentException($"{pi.Name} needs LCEngineFunctionParamAttribute.");
                    }

                    string paramName = attr.ParamName;
                    ps.Add(parameters[paramName]);
                }
            }

            return ps.ToArray();
        }
    }
}
