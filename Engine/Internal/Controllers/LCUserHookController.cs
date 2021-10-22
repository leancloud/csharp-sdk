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
                LCLogger.Debug(LCEngine.OnAuthData);
                LCLogger.Debug(body.ToString());

                LCEngine.CheckHookKey(Request);

                if (UserHooks.TryGetValue(LCEngine.OnAuthData, out MethodInfo mi)) {
                    LCEngine.InitRequestContext(Request);

                    Dictionary<string, object> dict = LCEngine.Decode(body);
                    object result = await LCEngine.Invoke(mi, new object[] { dict["authData"] });
                    if (result != null) {
                        return new Dictionary<string, object> {
                            { "result", result }
                        };
                    }
                }
                return body;
            } catch (LCException e) {
                return StatusCode(400, LCEngine.ConvertException(e));
            } catch (Exception e) {
                return StatusCode(500, LCEngine.ConvertException(e));
            }
        }

        private async Task<object> HandleHook(string hookKey, JsonElement body) {
            try {
                LCLogger.Debug(hookKey);
                LCLogger.Debug(body.ToString());

                LCEngine.CheckHookKey(Request);

                if (UserHooks.TryGetValue(hookKey, out MethodInfo mi)) {
                    LCEngine.InitRequestContext(Request);

                    Dictionary<string, object> dict = LCEngine.Decode(body);
                    return await Invoke(mi, dict);
                }
                return body;
            } catch (LCException e) {
                return StatusCode(400, LCEngine.ConvertException(e));
            } catch (Exception e) {
                return StatusCode(500, LCEngine.ConvertException(e));
            }
        }

        private static async Task<object> Invoke(MethodInfo mi, Dictionary<string, object> dict) {
            LCObjectData objectData = LCObjectData.Decode(dict["object"] as Dictionary<string, object>);
            objectData.ClassName = "_User";

            LCObject user = LCObject.Create("_User");
            user.Merge(objectData);

            LCObject result = await LCEngine.Invoke(mi, new object[] { user }) as LCObject;
            if (result != null) {
                Dictionary<string, object> ret = LCEncoder.EncodeLCObject(result, true) as Dictionary<string, object>;
                dict.Remove("__type");
                dict.Remove("className");
                return ret;
            } else {
                return dict;
            }
        }
    }
}
