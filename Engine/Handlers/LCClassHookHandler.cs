using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using LeanCloud.Storage.Internal.Object;
using LeanCloud.Storage;

namespace LeanCloud.Engine {
    public class LCClassHookHandler {
        private static Dictionary<string, MethodInfo> ClassHooks => LCEngine.ClassHooks;

        public static void Hello() {

        }

        public static async Task<object> HandleClassHook(string className, string hookName, HttpRequest request, JsonElement body) {
            LCLogger.Debug($"Hook: {className}#{hookName}");
            LCLogger.Debug(body.ToString());

            LCEngine.CheckHookKey(request);

            string classHookName = GetClassHookName(className, hookName);
            if (ClassHooks.TryGetValue(classHookName, out MethodInfo mi)) {
                Dictionary<string, object> dict = LCEngine.Decode(body);

                LCObjectData objectData = LCObjectData.Decode(dict["object"] as Dictionary<string, object>);
                objectData.ClassName = className;
                LCObject obj = LCObject.Create(className);
                obj.Merge(objectData);

                LCUser user = null;
                if (dict.TryGetValue("user", out object userObj) &&
                    userObj != null) {
                    user = new LCUser();
                    user.Merge(LCObjectData.Decode(userObj as Dictionary<string, object>));
                }

                LCClassHookRequest req = new LCClassHookRequest {
                    Object = obj,
                    CurrentUser = user
                };

                LCObject result = await LCEngine.Invoke(mi, req) as LCObject;
                if (result != null) {
                    return LCCloud.Encode(result);
                }
            }
            return default;
        }

        private static string GetClassHookName(string className, string hookName) {
            switch (hookName) {
                case "beforeSave":
                    return $"__before_save_for_{className}";
                case "afterSave":
                    return $"__after_save_for_{className}";
                case "beforeUpdate":
                    return $"__before_update_for_{className}";
                case "afterUpdate":
                    return $"__after_update_for_{className}";
                case "beforeDelete":
                    return $"__before_delete_for_{className}";
                case "afterDelete":
                    return $"__after_delete_for_{className}";
                default:
                    throw new Exception($"Error hook name: {hookName}");
            }
        }
    }
}
