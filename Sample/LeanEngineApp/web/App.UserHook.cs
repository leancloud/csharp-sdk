using System;
using System.Collections.Generic;
using LeanCloud;
using LeanCloud.Storage;
using LeanCloud.Engine;

namespace web {
    public partial class App {
        // User Hook
        [LCEngineUserHook(LCEngineUserHookType.OnLogin)]
        public static LCUser OnLogin(LCEngineRequestContext context, LCUser user) {
            Console.WriteLine($"On login from {context.RemoteAddress}");
            if (user.Username == "forbidden") {
                throw new Exception("Forbidden");
            }
            return user;
        }

        [LCEngineUserHook(LCEngineUserHookType.OnAuthData)]
        public static Dictionary<string, object> OnAuthData(LCEngineRequestContext context, Dictionary<string, object> authData) {
            Console.WriteLine($"On authdata from {context.RemoteAddress}");
            if (authData.TryGetValue("fake_platform", out object tokenObj)) {
                if (tokenObj is Dictionary<string, object> token) {
                    // 模拟校验
                    if (token["openid"] as string == "123" &&
                        token["access_token"] as string == "haha") {
                        LCLogger.Debug("Auth data Verified OK.");
                    } else {
                        throw new Exception("Invalid auth data.");
                    }
                } else {
                    throw new Exception("Invalid auth data");
                }
            }
            return authData;
        }
    }
}
