using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using LeanCloud.Storage;

namespace LeanCloud.Engine {
    [ApiController]
    [Route("{1,1.1}")]
    [EnableCors(LCEngine.LCEngineCORS)]
    public class LCRTMHookController : ControllerBase {
        private Dictionary<string, MethodInfo> RTMHooks => LCEngine.RTMHooks;

        [HttpPost("functions/{funcName:regex(^_messageReceived$|" +
            "^_messageSent$|" +
            "^_receiversOffline$|" +
            "^_messageUpdate$|" +
            "^_conversationStart$|" +
            "^_conversationStarted$|" +
            "^_conversationAdd$|" +
            "^_conversationRemove$|" +
            "^_conversationAdded$|" +
            "^_conversationRemoved$|" +
            "^_conversationUpdate$|" +
            "^_clientOnline$|" +
            "^_clientOffline$)}")]
        public async Task<object> Run(string funcName, JsonElement body) {
            try {
                if (LCLogger.LogDelegate != null) {
                    LCLogger.Debug($"Run: {funcName}");
                    LCLogger.Debug(body.ToString());
                }

                if (RTMHooks.TryGetValue(funcName, out MethodInfo mi)) {
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

        private static object[] ParseParameters(MethodInfo mi, LCEngineRequestContext context, JsonElement body) {
            List<object> ps = new List<object>();

            Dictionary<string, object> parameter = LCEngine.Decode(body);

            foreach (ParameterInfo pi in mi.GetParameters()) {
                if (pi.ParameterType == typeof(LCEngineRequestContext)) {
                    ps.Add(context);
                } else {
                    ps.Add(parameter);
                }
            }

            return ps.ToArray();
        }
    }
}
