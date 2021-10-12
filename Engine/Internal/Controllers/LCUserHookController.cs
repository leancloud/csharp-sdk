using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using LeanCloud.Storage.Internal.Object;
using LeanCloud.Storage;

namespace LeanCloud.Engine {
    [ApiController]
    [Route("{1,1.1}/functions")]
    [EnableCors(LCEngine.LCEngineCORS)]
    public class LCUserHookController : ControllerBase {
        private Dictionary<string, MethodInfo> UserHooks => LCEngine.UserHooks;

        [HttpPost("onVerified/sms")]
        public async Task<object> HookSMSVerification(JsonElement body) {
            try {
                LCLogger.Debug(LCEngine.OnSMSVerified);
                LCLogger.Debug(body.ToString());

                LCEngine.CheckHookKey(Request);

                if (UserHooks.TryGetValue(LCEngine.OnSMSVerified, out MethodInfo mi)) {
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

        [HttpPost("onVerified/email")]
        public async Task<object> HookEmailVerification(JsonElement body) {
            try {
                LCLogger.Debug(LCEngine.OnEmailVerified);
                LCLogger.Debug(body.ToString());

                LCEngine.CheckHookKey(Request);

                if (UserHooks.TryGetValue(LCEngine.OnEmailVerified, out MethodInfo mi)) {
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

        [HttpPost("_User/onLogin")]
        public async Task<object> HookLogin(JsonElement body) {
            try {
                LCLogger.Debug(LCEngine.OnLogin);
                LCLogger.Debug(body.ToString());

                LCEngine.CheckHookKey(Request);

                if (UserHooks.TryGetValue(LCEngine.OnLogin, out MethodInfo mi)) {
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

            return await LCEngine.Invoke(mi, new object[] { user }) as LCObject;
        }
    }
}
