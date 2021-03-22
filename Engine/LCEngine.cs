using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.Extensions.Primitives;
using LeanCloud.Common;
using Microsoft.Extensions.DependencyInjection;

namespace LeanCloud.Engine {
    public class LCEngine {
        public const string LCEngineCORS = "LCEngineCORS";

        const string LCMasterKeyName = "x-avoscloud-master-key";
        const string LCHookKeyName = "x-lc-hook-key";

        const string BeforeSave = "__before_save_for_";
        const string AfterSave = "__after_save_for_";
        const string BeforeUpdate = "__before_update_for_";
        const string AfterUpdate = "__after_update_for_";
        const string BeforeDelete = "__before_delete_for_";
        const string AfterDelete = "__after_delete_for_";

        internal const string OnSMSVerified = "__on_verified_sms";
        internal const string OnEmailVerified = "__on_verified_email";
        internal const string OnLogin = "__on_login__User";

        const string ClientOnline = "_clientOnline";
        const string ClientOffline = "_clientOffline";

        const string MessageSent = "_messageSent";
        const string MessageReceived = "_messageReceived";
        const string ReceiversOffline = "_receiversOffline";
        const string MessageUpdate = "_messageUpdate";

        const string ConversationStart = "_conversationStart";
        const string ConversationStarted = "_conversationStarted";
        const string ConversationAdd = "_conversationAdd";
        const string ConversationAdded = "_conversationAdded";
        const string ConversationRemove = "_conversationRemove";
        const string ConversationRemoved = "_conversationRemoved";
        const string ConversationUpdate = "_conversationUpdate";

        static readonly string[] LCEngineCORSMethods = new string[] {
            "PUT",
            "GET",
            "POST",
            "DELETE",
            "OPTIONS"
        };
        static readonly string[] LCEngineCORSHeaders = new string[] {
            "Content-Type",
            "X-AVOSCloud-Application-Id",
            "X-AVOSCloud-Application-Key",
            "X-AVOSCloud-Application-Production",
            "X-AVOSCloud-Client-Version",
            "X-AVOSCloud-Request-Sign",
            "X-AVOSCloud-Session-Token",
            "X-AVOSCloud-Super-Key",
            "X-LC-Hook-Key",
            "X-LC-Id",
            "X-LC-Key",
            "X-LC-Prod",
            "X-LC-Session",
            "X-LC-Sign",
            "X-LC-UA",
            "X-Requested-With",
            "X-Uluru-Application-Id",
            "X-Uluru-Application-Key",
            "X-Uluru-Application-Production",
            "X-Uluru-Client-Version",
            "X-Uluru-Session-Token"
        };

        public static Dictionary<string, MethodInfo> Functions = new Dictionary<string, MethodInfo>();
        public static Dictionary<string, MethodInfo> ClassHooks = new Dictionary<string, MethodInfo>();
        public static Dictionary<string, MethodInfo> UserHooks = new Dictionary<string, MethodInfo>();

        public static void Initialize(IServiceCollection services) {
            // 获取环境变量
            LCLogger.Debug("-------------------------------------------------");
            PrintEnvironmentVar("LEANCLOUD_APP_ID");
            PrintEnvironmentVar("LEANCLOUD_APP_KEY");
            PrintEnvironmentVar("LEANCLOUD_APP_MASTER_KEY");
            PrintEnvironmentVar("LEANCLOUD_APP_HOOK_KEY");
            PrintEnvironmentVar("LEANCLOUD_API_SERVER");
            PrintEnvironmentVar("LEANCLOUD_APP_PROD");
            PrintEnvironmentVar("LEANCLOUD_APP_ENV");
            PrintEnvironmentVar("LEANCLOUD_APP_INSTANCE");
            PrintEnvironmentVar("LEANCLOUD_REGION");
            PrintEnvironmentVar("LEANCLOUD_APP_ID");
            PrintEnvironmentVar("LEANCLOUD_APP_DOMAIN");
            PrintEnvironmentVar("LEANCLOUD_APP_PORT");
            LCLogger.Debug("-------------------------------------------------");

            LCApplication.Initialize(Environment.GetEnvironmentVariable("LEANCLOUD_APP_ID"),
                Environment.GetEnvironmentVariable("LEANCLOUD_APP_KEY"),
                Environment.GetEnvironmentVariable("LEANCLOUD_API_SERVER"));
            LCApplication.AddHeader(LCHookKeyName, Environment.GetEnvironmentVariable("LEANCLOUD_APP_HOOK_KEY"));

            Assembly assembly = Assembly.GetCallingAssembly();
            ClassHooks = assembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttribute<LCEngineClassHookAttribute>() != null)
                .ToDictionary(mi => {
                    LCEngineClassHookAttribute attr = mi.GetCustomAttribute<LCEngineClassHookAttribute>();
                    switch (attr.HookType) {
                        case LCEngineObjectHookType.BeforeSave:
                            return $"{BeforeSave}{attr.ClassName}";
                        case LCEngineObjectHookType.AfterSave:
                            return $"{AfterSave}{attr.ClassName}";
                        case LCEngineObjectHookType.BeforeUpdate:
                            return $"{BeforeUpdate}{attr.ClassName}";
                        case LCEngineObjectHookType.AfterUpdate:
                            return $"{AfterUpdate}{attr.ClassName}";
                        case LCEngineObjectHookType.BeforeDelete:
                            return $"{BeforeDelete}{attr.ClassName}";
                        case LCEngineObjectHookType.AfterDelete:
                            return $"{AfterDelete}{attr.ClassName}";
                        default:
                            throw new Exception($"Error hook type: {attr.HookType}");
                    }
                });

            UserHooks = assembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttribute<LCEngineUserHookAttribute>() != null)
                .ToDictionary(mi => {
                    LCEngineUserHookAttribute attr = mi.GetCustomAttribute<LCEngineUserHookAttribute>();
                    switch (attr.HookType) {
                        case LCEngineUserHookType.OnSMSVerified:
                            return OnSMSVerified;
                        case LCEngineUserHookType.OnEmailVerified:
                            return OnEmailVerified;
                        case LCEngineUserHookType.OnLogin:
                            return OnLogin;
                        default:
                            throw new Exception($"Error hook type: {attr.HookType}");
                    }
                });

            Functions = assembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttribute<LCEngineFunctionAttribute>() != null)
                .ToDictionary(mi => mi.GetCustomAttribute<LCEngineFunctionAttribute>().FunctionName);

            assembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttribute<LCEngineRealtimeHookAttribute>() != null)
                .ToDictionary(mi => {
                    LCEngineRealtimeHookAttribute attr = mi.GetCustomAttribute<LCEngineRealtimeHookAttribute>();
                    switch (attr.HookType) {
                        case LCEngineRealtimeHookType.ClientOnline:
                            return ClientOnline;
                        case LCEngineRealtimeHookType.ClientOffline:
                            return ClientOffline;
                        case LCEngineRealtimeHookType.MessageSent:
                            return MessageSent;
                        case LCEngineRealtimeHookType.MessageReceived:
                            return MessageReceived;
                        case LCEngineRealtimeHookType.ReceiversOffline:
                            return ReceiversOffline;
                        case LCEngineRealtimeHookType.MessageUpdate:
                            return MessageUpdate;
                        case LCEngineRealtimeHookType.ConversationStart:
                            return ConversationStart;
                        case LCEngineRealtimeHookType.ConversationStarted:
                            return ConversationStarted;
                        case LCEngineRealtimeHookType.ConversationAdd:
                            return ConversationAdd;
                        case LCEngineRealtimeHookType.ConversationAdded:
                            return ConversationAdded;
                        case LCEngineRealtimeHookType.ConversationRemove:
                            return ConversationRemove;
                        case LCEngineRealtimeHookType.ConversationRemoved:
                            return ConversationRemoved;
                        case LCEngineRealtimeHookType.ConversationUpdate:
                            return ConversationUpdate;
                        default:
                            throw new Exception($"Error hook type: {attr.HookType}");
                    }
                })
                .ToList()
                .ForEach(item => {
                    Functions.TryAdd(item.Key, item.Value);
                });

            services.AddCors(options => {
                options.AddPolicy(LCEngineCORS, builder => {
                    builder.AllowAnyOrigin()
                        .WithMethods(LCEngineCORSMethods)
                        .WithHeaders(LCEngineCORSHeaders)
                        .SetPreflightMaxAge(TimeSpan.FromSeconds(86400));
                });
            });
        }

        public static void PrintEnvironmentVar(string key) {
            LCLogger.Debug($"{key} : {Environment.GetEnvironmentVariable(key)}");
        }

        internal static async Task<object> Invoke(MethodInfo mi, object request) {
            try {
                object[] ps = new object[] { request };
                if (mi.ReturnType == typeof(Task) ||
                    (mi.ReturnType.IsGenericType && mi.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))) {
                    Task task = mi.Invoke(null, ps) as Task;
                    await task;
                    return task.GetType().GetProperty("Result")?.GetValue(task);
                }
                return mi.Invoke(null, ps);
            } catch (TargetInvocationException e) {
                Exception ex = e.InnerException;
                if (ex is LCException lcEx) {
                    throw new Exception(JsonConvert.SerializeObject(new Dictionary<string, object> {
                        { "code", lcEx.Code },
                        { "message", lcEx.Message }
                    }));
                }
                throw ex;
            }
        }

        internal static Dictionary<string, object> Decode(JsonElement jsonElement) {
            string json = System.Text.Json.JsonSerializer.Serialize(jsonElement);
            Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json,
                LCJsonConverter.Default);
            return dict;
        }

        internal static string GetIP(HttpRequest request) {
            if (request.Headers.TryGetValue("x-real-ip", out StringValues ip)) {
                return ip.ToString();
            }
            if (request.Headers.TryGetValue("x-forwarded-for", out StringValues forward)) {
                return forward.ToString();
            }
            return request.HttpContext.Connection.RemoteIpAddress.ToString();
        }

        internal static void CheckMasterKey(HttpRequest request) {
            if (!request.Headers.TryGetValue(LCMasterKeyName, out StringValues masterKey)) {
                throw new Exception("No master key");
            }
            if (!masterKey.Equals(Environment.GetEnvironmentVariable("LEANCLOUD_APP_MASTER_KEY"))) {
                throw new Exception("Mismatch master key");
            }
        }

        internal static void CheckHookKey(HttpRequest request) {
            if (!request.Headers.TryGetValue(LCHookKeyName, out StringValues hookKey)) {
                throw new Exception("No hook key");
            }
            if (!hookKey.Equals(Environment.GetEnvironmentVariable("LEANCLOUD_APP_HOOK_KEY"))) {
                throw new Exception("Mismatch hook key");
            }
        }

        public static object GetFunctions(HttpRequest request) {
            CheckMasterKey(request);

            List<string> functions = new List<string>();
            functions.AddRange(Functions.Keys);
            functions.AddRange(ClassHooks.Keys);
            functions.AddRange(UserHooks.Keys);
            foreach (string func in functions) {
                LCLogger.Debug(func);
            }

            return new Dictionary<string, List<string>> {
                { "result", functions }
            };
        }
    }
}
