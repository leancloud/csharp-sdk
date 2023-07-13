using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using LeanCloud.Storage.Internal.Object;
using LeanCloud.Storage;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Engine {
    [ApiController]
    [Route("{1,1.1}/functions")]
    [EnableCors(LCEngine.LCEngineCORS)]
    public class LCUserHookController : ControllerBase {
        private Dictionary<string, MethodInfo> UserHooks => LCEngine.UserHooks;

        [HttpPost("onVerified/sms")]
        public async Task<object> HookSMSVerification(JsonElement body) {
            return await HandleHook(LCEngine.OnSMSVerified, body);
        }

        [HttpPost("onVerified/email")]
        public async Task<object> HookEmailVerification(JsonElement body) {
            return await HandleHook(LCEngine.OnEmailVerified, body);
        }

        [HttpPost("_User/onLogin")]
        public async Task<object> HookLogin(JsonElement body) {
            return await HandleHook(LCEngine.OnLogin, body);
        }

        [HttpPost("_User/onAuthData")]
        public async Task<object> HookAuthData(JsonElement body) {
            try {
                if (LCLogger.LogDelegate != null) {
                    LCLogger.Debug(LCEngine.OnAuthData);
                    LCLogger.Debug(body.ToString());
                }

                LCEngine.CheckHookKey(Request);

                if (UserHooks.TryGetValue(LCEngine.OnAuthData, out MethodInfo mi)) {
                    LCEngineRequestContext context = new LCEngineRequestContext(Request);

                    Dictionary<string, object> dict = LCEngine.Decode(body);
                    object[] ps = ParseParameters(mi, context, dict["authData"]);
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
                LCEngine.LogException("_User/onAuthData", e);
                return StatusCode(500, LCEngine.ConvertException(e));
            }
        }

        private async Task<object> HandleHook(string hookKey, JsonElement body) {
            try {
                LCLogger.Debug(hookKey);
                LCLogger.Debug(body.ToString());

                LCEngine.CheckHookKey(Request);

                if (UserHooks.TryGetValue(hookKey, out MethodInfo mi)) {
                    LCEngineRequestContext context = new LCEngineRequestContext(Request);
                    Dictionary<string, object> dict = LCEngine.Decode(body);
                    return await Invoke(mi, context, dict);
                }
                return body;
            } catch (LCEngineException e) {
                return StatusCode(e.Status, LCEngine.ConvertException(e));
            } catch (Exception e) {
                LCEngine.LogException(hookKey, e);
                return StatusCode(500, LCEngine.ConvertException(e));
            }
        }

        private static async Task<object> Invoke(MethodInfo mi, LCEngineRequestContext context, Dictionary<string, object> dict) {
            LCObjectData objectData = LCObjectData.Decode(dict["object"] as Dictionary<string, object>);
            objectData.ClassName = "_User";
            LCObject user = LCObject.Create("_User");
            user.Merge(objectData);

            object[] ps = ParseParameters(mi, context, user);
            LCObject result = await LCEngine.Invoke(mi, ps) as LCObject;
            if (result != null) {
                Dictionary<string, object> ret = LCEncoder.EncodeLCObject(result, true) as Dictionary<string, object>;
                dict.Remove("__type");
                dict.Remove("className");
                return ret;
            } else {
                return dict;
            }
        }

        private static object[] ParseParameters(MethodInfo mi, LCEngineRequestContext context, object param) {
            List<object> ps = new List<object>();

            foreach (ParameterInfo pi in mi.GetParameters()) {
                if (pi.ParameterType == typeof(LCEngineRequestContext)) {
                    ps.Add(context);
                } else if (pi.ParameterType == param.GetType()) {
                    ps.Add(param);
                } else {
                    throw new ArgumentException($"{pi.Name} must be instance of {param.GetType()}.");
                }
            }

            return ps.ToArray();
        }
    }
}
