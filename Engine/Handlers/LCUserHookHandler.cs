using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using LeanCloud.Storage.Internal.Object;
using LeanCloud.Storage;

namespace LeanCloud.Engine {
    public class LCUserHookHandler {
        private static Dictionary<string, MethodInfo> UserHooks => LCEngine.UserHooks;

        public static async Task<object> HandleVerifiedSMS(HttpRequest request, JsonElement body) {
            LCLogger.Debug(LCEngine.OnSMSVerified);
            LCLogger.Debug(body.ToString());

            LCEngine.CheckHookKey(request);

            if (UserHooks.TryGetValue(LCEngine.OnSMSVerified, out MethodInfo mi)) {
                Dictionary<string, object> dict = LCEngine.Decode(body);
                return await Invoke(mi, dict);
            }
            return default;
        }

        public static async Task<object> HandleVerifiedEmail(HttpRequest request, JsonElement body) {
            LCLogger.Debug(LCEngine.OnEmailVerified);
            LCLogger.Debug(body.ToString());

            LCEngine.CheckHookKey(request);

            if (UserHooks.TryGetValue(LCEngine.OnEmailVerified, out MethodInfo mi)) {
                Dictionary<string, object> dict = LCEngine.Decode(body);
                return await Invoke(mi, dict);
            }
            return default;
        }

        public static async Task<object> HandleLogin(HttpRequest request, JsonElement body) {
            LCLogger.Debug(LCEngine.OnLogin);
            LCLogger.Debug(body.ToString());

            LCEngine.CheckHookKey(request);

            if (UserHooks.TryGetValue(LCEngine.OnLogin, out MethodInfo mi)) {
                Dictionary<string, object> dict = LCEngine.Decode(body);
                return await Invoke(mi, dict);
            }
            return default;
        }

        private static async Task<object> Invoke(MethodInfo mi, Dictionary<string, object> dict) {
            LCObjectData objectData = LCObjectData.Decode(dict["object"] as Dictionary<string, object>);
            objectData.ClassName = "_User";

            LCObject obj = LCObject.Create("_User");
            obj.Merge(objectData);

            LCUserHookRequest req = new LCUserHookRequest {
                CurrentUser = obj as LCUser
            };

            return await LCEngine.Invoke(mi, req) as LCObject;
        }
    }
}
